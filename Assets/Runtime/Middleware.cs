using System;
using System.Threading.Tasks;

namespace Flux
{
    /// <summary>
    /// A Unity-safe abstract base class for creating Middleware. It provides default
    /// implementations for all IMiddleware methods.
    /// </summary>
    public abstract class Middleware : IMiddleware
    {
        // UNITY NOTE: The 'volatile' keyword and 'Interlocked' operations were removed.
        // In Unity's single-threaded model, these are unnecessary and imply a multi-threading
        // context that doesn't exist. Simple integer operations are sufficient and more performant.
        private int _beginMiddlewareChangeCount;

        /// <summary>
        /// Gets a value indicating if the store is currently inside a "do not disturb" state change.
        /// Middleware should check this property to avoid creating feedback loops (e.g., an auto-save
        /// middleware should not save the state when it's being restored by a "load game" middleware).
        /// </summary>
        public bool IsInsideMiddlewareChange => _beginMiddlewareChangeCount > 0;

        public virtual Task InitializeAsync(IDispatcher dispatcher, IStore store) => Task.CompletedTask;

        public virtual void AfterInitializeAllMiddlewares() { }

        public virtual bool MayDispatchAction(IAction action) => true;

        public virtual void BeforeDispatch(IAction action) { }

        public virtual void AfterDispatch(IAction action) { }
        
        /// <summary>
        /// A callback executed just before the internal middleware change context ends.
        /// </summary>
        protected virtual void OnInternalMiddlewareChangeEnding() { }

        IDisposable IMiddleware.BeginInternalMiddlewareChange()
        {
            _beginMiddlewareChangeCount++;
            return new DisposableCallback(
                id: $"{GetType().Name}.{nameof(IMiddleware.BeginInternalMiddlewareChange)}",
                () =>
                {
                    // Only trigger the callback on the very last decrement, just before it becomes 0
                    if (_beginMiddlewareChangeCount == 1)
                        OnInternalMiddlewareChangeEnding();
                    _beginMiddlewareChangeCount--;
                });
        }
    }
}