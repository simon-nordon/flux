// Assets/Runtime/DevTools/DevToolsMiddleware.cs
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

using System.Threading.Tasks;

namespace Flux.DevTools
{
    /// <summary>Records action timeline and state snapshots. Also enables time-travel restore.</summary>
    public sealed class DevToolsMiddleware : IMiddleware
    {
        readonly List<DevToolsEntry> _entries = new();
        readonly object _sync = new();
        readonly JsonSerializerSettings _json = new JsonSerializerSettings
        {
            Formatting = Formatting.None,                 // equivalent to WriteIndented = false
            NullValueHandling = NullValueHandling.Ignore, // equivalent to DefaultIgnoreCondition = WhenWritingNull
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore // safer for Unity object graphs
            // Newtonsoft serializes fields & props by default, so IncludeFields isn't needed.
        };

        IDispatcher _dispatcher;
        IStore _store;
        DevToolsSettings _settings;
        int _nextIndex;
        bool _isReplaying;

        public IReadOnlyList<DevToolsEntry> Entries
        {
            get { lock (_sync) return _entries.AsReadOnly(); }
        }

        public DevToolsMiddleware(DevToolsSettings settings = null)
        {
            _settings = settings;
        }

        public Task InitializeAsync(IDispatcher dispatcher, IStore store)
        {
            _dispatcher = dispatcher;
            _store = store;
            return Task.CompletedTask;
        }

        public void AfterInitializeAllMiddlewares() { }

        public bool MayDispatchAction(IAction action) => true;

        public void BeforeDispatch(IAction action) { }

        public void AfterDispatch(IAction action)
        {
            if (_isReplaying) return;

            var entry = new DevToolsEntry
            {
                index = _nextIndex++,
                timestampUtc = DateTime.UtcNow,
                actionType = action.GetType().FullName,
                action = action,
            };

            var state = DevToolsUtil.ComposeState(_store);
            entry.state = state;

            var settings = _settings;
            if (settings?.captureRawStateJson == true)
                entry.stateJson = SafeToJson(state);

            if (settings?.captureStateDiffs == true && _entries.Count > 0)
            {
                entry.diff = ComputeDiff(_entries[^1].state, state);
            }

            lock (_sync)
            {
                _entries.Add(entry);
                var max = settings?.maxEntries ?? 1000;
                if (_entries.Count > max) _entries.RemoveAt(0);
            }

            DevToolsHub.RaiseChanged(this);
        }

        public IDisposable BeginInternalMiddlewareChange() => new Flux.DisposableCallback("DevToolsMiddleware", () => { });

        public void ReplayTo(int targetIndex)
        {
            // Prevent middleware feedback while restoring
            _isReplaying = true;
            using (_store.BeginInternalMiddlewareChange())
            {
                // 1) Clear features to initial (call RestoreState on each from snapshot 0)
                var snap = FindClosestSnapshot(targetIndex);
                if (snap == null) { _isReplaying = false; return; }

                // Restore state directly
                foreach (var kv in _store.Features)
                {
                    if (snap.state.TryGetValue(kv.Key, out var val))
                        kv.Value.RestoreState(val);
                }
            }
            _isReplaying = false;
            DevToolsHub.RaiseChanged(this);
        }

        DevToolsEntry FindClosestSnapshot(int idx)
        {
            lock (_sync)
            {
                if (_entries.Count == 0) return null;
                idx = Math.Clamp(idx, 0, _entries[^1].index);
                // binary search by index
                int lo = 0, hi = _entries.Count - 1;
                while (lo <= hi)
                {
                    int mid = (lo + hi) / 2;
                    var m = _entries[mid].index;
                    if (m == idx) return _entries[mid];
                    if (m < idx) lo = mid + 1; else hi = mid - 1;
                }
                return _entries[Math.Max(0, hi)];
            }
        }

        DevToolsDiff ComputeDiff(Dictionary<string, object> a, Dictionary<string, object> b)
        {
            var diff = new DevToolsDiff();
            foreach (var kv in b)
            {
                if (!a.ContainsKey(kv.Key)) { diff.added[kv.Key] = kv.Value; continue; }
                var av = a[kv.Key]; var bv = kv.Value;
                if (!Equals(av, bv)) diff.changed[kv.Key] = bv;
            }
            foreach (var kv in a)
                if (!b.ContainsKey(kv.Key)) diff.removed[kv.Key] = kv.Value;
            return diff;
        }

        string SafeToJson(object o)
        {
            if (o == null)
                return "{}";

            try
            {
                return JsonConvert.SerializeObject(o, _json);
            }
            catch
            {
                return "{}";
            }
        }
    }
}
