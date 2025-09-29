// ===== FILE: MiddlewareObject.cs =====
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Flux.Unity
{
    /// <summary>
    /// An abstract ScriptableObject for creating Middleware.
    /// Inherit from this to create a Middleware asset that can be added to a StoreObject.
    /// </summary>
    public abstract class MiddlewareObject : ScriptableObject, IMiddleware
    {
        private int _beginMiddlewareChangeCount;
        protected bool IsInsideMiddlewareChange => _beginMiddlewareChangeCount > 0;
        
        public virtual Task InitializeAsync(IDispatcher dispatcher, IStore store) => Task.CompletedTask;
        public virtual void AfterInitializeAllMiddlewares() { }
        public virtual bool MayDispatchAction(IAction action) => true;
        public virtual void BeforeDispatch(IAction action) { }
        public virtual void AfterDispatch(IAction action) { }
        protected virtual void OnInternalMiddlewareChangeEnding() { }

        IDisposable IMiddleware.BeginInternalMiddlewareChange()
        {
            _beginMiddlewareChangeCount++;
            return new DisposableCallback(
                id: $"{GetType().Name}.{nameof(IMiddleware.BeginInternalMiddlewareChange)}",
                () =>
                {
                    if (_beginMiddlewareChangeCount == 1)
                        OnInternalMiddlewareChangeEnding();
                    _beginMiddlewareChangeCount--;
                });
        }
    }
}