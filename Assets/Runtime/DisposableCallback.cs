using System;

namespace Flux
{
    /// <summary>
    /// A helper class that executes a callback action when disposed.
    /// </summary>
    internal class DisposableCallback : IDisposable
    {
        private readonly Action _action;
        public string Id { get; }
        public DisposableCallback(string id, Action action)
        {
            Id = id;
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }
        public void Dispose() => _action();
    }
}