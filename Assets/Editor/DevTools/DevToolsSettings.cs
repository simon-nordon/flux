using UnityEngine;

namespace Flux.DevTools
{
    [CreateAssetMenu(fileName = "FluxDevToolsSettings", menuName = "Flux/DevTools Settings")]
    public sealed class DevToolsSettings : ScriptableObject
    {
        [Min(100)] public int maxEntries = 2000;
        public bool captureStateDiffs = true;
        public bool captureRawStateJson = true; // faster tree view initially
    }
}