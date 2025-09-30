// Assets/Runtime/DevTools/DevToolsModels.cs
using System;
using System.Collections.Generic;

namespace Flux.DevTools
{
    [Serializable]
    public sealed class DevToolsEntry
    {
        public int index;
        public DateTime timestampUtc;
        public string actionType;
        public object action;                 // stored for replay (boxed)
        public Dictionary<string, object> state; // composite feature state snapshot
        public string stateJson;              // optional (for quick tree rendering)
        public DevToolsDiff diff;             // optional
        public long ticks => timestampUtc.Ticks;
    }

    [Serializable]
    public sealed class DevToolsDiff
    {
        public Dictionary<string, object> added = new();
        public Dictionary<string, object> removed = new();
        public Dictionary<string, object> changed = new();
    }

    internal static class DevToolsUtil
    {
        public static Dictionary<string, object> ComposeState(Flux.IStore store)
        {
            var dict = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var kv in store.Features)
                dict[kv.Key] = kv.Value.GetState();
            return dict;
        }
    }
}