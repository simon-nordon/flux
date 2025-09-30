// Assets/Editor/DevTools/DevToolsBootstrapper.cs
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Flux.DevTools;
using Flux.Unity;

[InitializeOnLoad]
public static class DevToolsBootstrapper
{
    static DevToolsBootstrapper()
    {
        DevToolsHub.Changed += _ => EditorApplication.delayCall += RepaintIfOpen;
    }

    static void RepaintIfOpen()
    {
        var wnd = Resources.FindObjectsOfTypeAll<FluxDevToolsWindow>().FirstOrDefault();
        wnd?.Repaint();
    }
}
#endif