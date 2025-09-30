// Assets/Editor/DevTools/FluxDevToolsWindow.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Flux;
using Flux.DevTools;
using Flux.Unity;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;

public sealed class FluxDevToolsWindow : EditorWindow
{
    [MenuItem("Window/Flux/Redux DevTools")]
    public static void Open() => GetWindow<FluxDevToolsWindow>("Flux DevTools");

    // UI
    ListView _actionList;
    ToolbarToggle _liveToggle;
    SliderInt _speedSlider; // 1x..4x
    Button _revertBtn, _sweepBtn, _commitBtn;
    Tabs _tabs;
    VisualElement _statePanel, _diffPanel, _actionPanel;
    ObjectField _storeField;

    DevToolsMiddleware _middleware;
    IStore _store;
    IDispatcher _dispatcher;

    int _selectedIndex = -1;
    double _lastStep;
    int _playCursor;

    void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.flexDirection = FlexDirection.Column;

        // Top toolbar
        var tb = new Toolbar();
        _storeField = new ObjectField("Store (StoreObject)") { objectType = typeof(StoreObject) };
        _storeField.RegisterValueChangedCallback(e => HookStore(e.newValue as StoreObject));
        _liveToggle = new ToolbarToggle() { text = "Live" };
        _liveToggle.value = true;
        _speedSlider = new SliderInt(1, 4) { value = 1 };
        _revertBtn = new Button(() => TimeTravelTo(0)) { text = "Reset" };
        _sweepBtn = new Button(() => { /* future: sweep */ }) { text = "Sweep" };
        _commitBtn = new Button(() => { /* future: commit baseline */ }) { text = "Commit" };
        tb.Add(_storeField); tb.Add(_liveToggle); tb.Add(new Label("Speed")); tb.Add(_speedSlider);
        tb.Add(_revertBtn); tb.Add(_sweepBtn); tb.Add(_commitBtn);
        root.Add(tb);

        // Body split: left actions, right details
        var body = new TwoPaneSplitView(0, 320, TwoPaneSplitViewOrientation.Horizontal);
        _actionList = new ListView();
        _actionList.selectionType = SelectionType.Single;
        _actionList.makeItem = () => new Label();
        _actionList.bindItem = (e, i) =>
        {
            if (_middleware == null) { (e as Label).text = ""; return; }
            var entry = _middleware.Entries.ElementAtOrDefault(i);
            if (entry == null) { (e as Label).text = ""; return; }
            (e as Label).text = $"{entry.index,4}  {entry.timestampUtc:HH:mm:ss.fff}  {Short(entry.actionType)}";
        };
        _actionList.itemsSource = new List<int>(); // we’ll repoint
        _actionList.onSelectionChange += sel =>
        {
            foreach (var _ in sel) { } // enumerator forces selection
            _selectedIndex = _actionList.selectedIndex;
            ShowSelection();
        };
        body.Add(_actionList);

        var right = new VisualElement() { style = { flexGrow = 1 } };

        _tabs = new Tabs(); // simple local tabs
        _tabs.AddTab("Action", out _actionPanel);
        _tabs.AddTab("State", out _statePanel);
        _tabs.AddTab("Diff", out _diffPanel);
        right.Add(_tabs);
        body.Add(right);

        root.Add(body);

        // Action browser at bottom
        var actionBrowser = new ActionBrowserView(() => _store, () => _dispatcher);
        root.Add(actionBrowser);

        EditorApplication.update += UpdatePlayback;
    }

    void OnDisable() => EditorApplication.update -= UpdatePlayback;

    void HookStore(StoreObject so)
    {
        if (so == null) { _store = null; _dispatcher = null; _middleware = null; RefreshList(); return; }
        // Ensure DevTools middleware exists
        _store = so; _dispatcher = so;
        // Check if already added
        var existing = _store.GetMiddlewares().OfType<DevToolsMiddleware>().FirstOrDefault();
        if (existing == null)
        {
            // You can keep a shared settings asset or null for defaults
            var settings = AssetDatabase.FindAssets("t:DevToolsSettings")
                .Select(g => AssetDatabase.LoadAssetAtPath<DevToolsSettings>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault();

            existing = new DevToolsMiddleware(settings);
            _store.AddMiddleware(existing);
            _store.InitializeAsync(); // in case not yet
        }
        _middleware = existing;
        RefreshList();
    }

    void RefreshList()
    {
        if (_middleware == null)
        {
            _actionList.itemsSource = new List<int>();
            _actionList.Rebuild();
            return;
        }
        _actionList.itemsSource = Enumerable.Range(0, _middleware.Entries.Count).ToList();
        _actionList.Rebuild();
        if (_liveToggle.value) _actionList.ScrollToItem(_middleware.Entries.Count - 1);
    }

    void ShowSelection()
    {
        if (_middleware == null || _selectedIndex < 0 || _selectedIndex >= _middleware.Entries.Count) return;
        var e = _middleware.Entries[_selectedIndex];

        _actionPanel.Clear();
        _actionPanel.Add(new Label(e.actionType));
        _actionPanel.Add(new TextField() { value = SafeJson(e.action) });

        _statePanel.Clear();
        _statePanel.Add(new TextField() { multiline = true, value = e.stateJson ?? SafeJson(e.state), style = { flexGrow = 1 } });

        _diffPanel.Clear();
        if (e.diff != null)
        {
            _diffPanel.Add(new Label("Added:")); _diffPanel.Add(new TextField(){ value = SafeJson(e.diff.added) });
            _diffPanel.Add(new Label("Removed:")); _diffPanel.Add(new TextField(){ value = SafeJson(e.diff.removed) });
            _diffPanel.Add(new Label("Changed:")); _diffPanel.Add(new TextField(){ value = SafeJson(e.diff.changed) });
        }
    }

    void UpdatePlayback()
    {
        if (_middleware == null) return;

        // Keep list live
        RefreshList();

        if (!_liveToggle.value) // time travel mode
        {
            var now = EditorApplication.timeSinceStartup;
            var step = (1.0 / _speedSlider.value);
            if (now - _lastStep >= step)
            {
                _lastStep = now;
                _playCursor = Mathf.Clamp(_playCursor + 1, 0, _middleware.Entries.Count - 1);
                _middleware.ReplayTo(_playCursor);
                _actionList.SetSelection(_playCursor);
                ShowSelection();
            }
        }
        else
        {
            _playCursor = _middleware.Entries.Count - 1;
        }
    }

    void TimeTravelTo(int index)
    {
        if (_middleware == null) return;
        _liveToggle.value = false;
        _playCursor = index;
        _middleware.ReplayTo(index);
        _actionList.SetSelection(index);
        ShowSelection();
    }

    static string Short(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var i = s.LastIndexOf('.');
        return i >= 0 ? s[(i + 1)..] : s;
    }

    static string SafeJson(object o)
    {
        if (o == null)
            return "null";

        try
        {
            return JsonConvert.SerializeObject(
                o,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });
        }
        catch
        {
            return o.ToString();
        }
    }
}

// --- tiny tab control & action browser views (runtime-compatible) ---
public sealed class Tabs : VisualElement
{
    Toolbar _bar = new Toolbar();
    VisualElement _content = new VisualElement();
    readonly List<(string, VisualElement)> _tabs = new();

    public Tabs()
    {
        style.flexGrow = 1;
        _content.style.flexGrow = 1;
        Add(_bar); Add(_content);
    }

    public void AddTab(string title, out VisualElement panel)
    {
        panel = new ScrollView();
        panel.style.flexGrow = 1;
        var idx = _tabs.Count;
        var btn = new ToolbarButton(() => Show(idx)) { text = title };
        _bar.Add(btn);
        _tabs.Add((title, panel));
        if (idx == 0) _content.Add(panel);
    }

    void Show(int idx)
    {
        _content.Clear();
        _content.Add(_tabs[idx].Item2);
    }
}

public sealed class ActionBrowserView : VisualElement
{
    readonly Func<IStore> _getStore;
    readonly Func<IDispatcher> _getDispatcher;
    readonly List<Type> _actionTypes = new();
    readonly ListView _list;
    readonly Button _dispatchButton;

    public ActionBrowserView(Func<IStore> getStore, Func<IDispatcher> getDispatcher)
    {
        style.borderTopWidth = 1; style.borderTopColor = new Color(0,0,0,0.2f);
        Add(new Label("Action Browser"));

        _getStore = getStore;
        _getDispatcher = getDispatcher;

        _list = new ListView();
        _list.makeItem = () => new Label();
        _list.bindItem = (e, i) => (e as Label).text = _actionTypes[i].FullName;
        _list.itemsSource = _actionTypes;
        _list.style.height = 120;
        Add(_list);

        _dispatchButton = new Button(DispatchSelected) { text = "Dispatch Selected (default ctor only)" };
        Add(_dispatchButton);

        var refresh = new Button(RefreshActions) { text = "Refresh" };
        Add(refresh);
    }

    void RefreshActions()
    {
        _actionTypes.Clear();
        var store = _getStore();
        if (store == null) { _list.Rebuild(); return; }

        foreach (var f in store.Features.Values)
        {
            // Heuristic: scan reducers to find TAction generic args
            var featureType = f.GetType();
            var field = featureType.GetField("Reducers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field?.GetValue(f) is System.Collections.IEnumerable reducers)
            {
                foreach (var r in reducers)
                {
                    var t = r.GetType();
                    // Expect Reducer<TState, TAction>
                    var baseT = t.BaseType;
                    while (baseT != null)
                    {
                        if (baseT.IsGenericType && baseT.GetGenericTypeDefinition().Name.StartsWith("Reducer`"))
                        {
                            var args = baseT.GetGenericArguments();
                            if (args.Length == 2) _actionTypes.Add(args[1]);
                            break;
                        }
                        baseT = baseT.BaseType;
                    }
                }
            }
        }
        _actionTypes.Sort((a,b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
        _list.Rebuild();
    }

    void DispatchSelected()
    {
        var dispatcher = _getDispatcher();
        if (dispatcher == null) return;
        var i = _list.selectedIndex;
        if (i < 0 || i >= _actionTypes.Count) return;

        var t = _actionTypes[i];

        // MVP: only value types/parameterless ctor
        object action = null;
        if (t.IsValueType)
            action = Activator.CreateInstance(t);
        else
        {
            var ctor = t.GetConstructor(Type.EmptyTypes);
            if (ctor != null) action = Activator.CreateInstance(t);
        }

        if (action is IAction ia)
            dispatcher.Dispatch(ia);
        else
            Debug.LogWarning($"Cannot create action {t.FullName}. Provide a parameterless ctor or make it a readonly struct.");
    }
}
#endif
