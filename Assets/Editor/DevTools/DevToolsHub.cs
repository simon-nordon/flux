// Assets/Runtime/DevTools/DevToolsHub.cs
using System;

namespace Flux.DevTools
{
    /// <summary>Process-wide notifier so the window can find whatever DevToolsMiddleware is active.</summary>
    public static class DevToolsHub
    {
        public static event Action<object> Changed;
        internal static void RaiseChanged(object source) => Changed?.Invoke(source);
    }
}