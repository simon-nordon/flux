using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flux
{
    /// <summary>
    /// A Unity-centric implementation of the IStore interface.
    /// This class manages state (Features), side-effects (Effects), and middleware.
    /// It is designed to be safe for single-threaded environments like WebGL.
    /// </summary>
    public class Store : IStore, IDisposable
    {
        public IReadOnlyDictionary<string, IFeature> Features => _featuresByName;
        public Task Initialized => _initializedCompletionSource.Task;

        private readonly object _syncRoot = new object();
        private readonly IDispatcher _dispatcher;
        private readonly Dictionary<string, IFeature> _featuresByName = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly List<IEffect> _effects = new();
        private readonly List<IMiddleware> _middlewares = new();
        private readonly Queue<IAction> _queuedActions = new();
        private readonly TaskCompletionSource<bool> _initializedCompletionSource = new();
        private readonly ActionSubscriber _actionSubscriber;

        private bool _isDisposed;
        private volatile bool _isDispatching;
        private volatile int _beginMiddlewareChangeCount;
        private bool _isInitialized;
        private bool IsInsideMiddlewareChange => _beginMiddlewareChangeCount > 0;

        public event EventHandler<UnhandledExceptionEventArgs> UnhandledException;

        public Store(IDispatcher dispatcher)
        {
            _actionSubscriber = new ActionSubscriber();
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _dispatcher.ActionDispatched += OnActionDispatched;
            _dispatcher.Dispatch(new StoreInitializedAction());
        }

        public IEnumerable<IMiddleware> GetMiddlewares() => _middlewares;

        public void AddFeature(IFeature feature)
        {
            if (feature is null) throw new ArgumentNullException(nameof(feature));
            lock (_syncRoot)
            {
                _featuresByName.Add(feature.GetName(), feature);
            }
        }

        public void AddEffect(IEffect effect)
        {
            if (effect is null) throw new ArgumentNullException(nameof(effect));
            lock (_syncRoot)
            {
                _effects.Add(effect);
            }
        }

        public void AddMiddleware(IMiddleware middleware)
        {
            if (middleware is null) throw new ArgumentNullException(nameof(middleware));
            lock (_syncRoot)
            {
                _middlewares.Add(middleware);
                if (_isInitialized)
                {
                    InitializeMiddlewareAsync(middleware);
                }
            }
        }
        
        private async void InitializeMiddlewareAsync(IMiddleware middleware)
        {
            try
            {
                await middleware.InitializeAsync(_dispatcher, this);
                middleware.AfterInitializeAllMiddlewares();
            }
            catch (Exception e)
            {
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
            }
        }

        public IDisposable BeginInternalMiddlewareChange()
        {
            IDisposable[] disposables;
            lock (_syncRoot)
            {
                _beginMiddlewareChangeCount++;
                disposables = _middlewares
                    .Select(x => x.BeginInternalMiddlewareChange())
                    .Where(x => x != null)
                    .ToArray();
            }

            return new DisposableCallback(
                id: $"{nameof(Store)}.{nameof(BeginInternalMiddlewareChange)}",
                () => EndMiddlewareChange(disposables));
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            foreach (var middleware in _middlewares)
            {
                await middleware.InitializeAsync(_dispatcher, this);
            }
            _middlewares.ForEach(x => x.AfterInitializeAllMiddlewares());

            lock (_syncRoot)
            {
                _isInitialized = true;
                DequeueActions();
                _initializedCompletionSource.TrySetResult(true);
            }
        }

        #region IActionSubscriber Implementation
        // UNITY NOTE: This region has been changed to use explicit interface implementation
        // to resolve the compiler error.

        void IActionSubscriber.SubscribeToAction<TAction>(object subscriber, Action<TAction> callback)
        {
            // Because ActionSubscriber also uses an explicit implementation for this method,
            // we must cast it to the interface to call it.
            ((IActionSubscriber)_actionSubscriber).SubscribeToAction<TAction>(subscriber, callback);
        }

        void IActionSubscriber.UnsubscribeFromAllActions(object subscriber)
        {
            _actionSubscriber.UnsubscribeFromAllActions(subscriber);
        }

        IDisposable IActionSubscriber.GetActionUnsubscriberAsIDisposable(object subscriber) =>
            _actionSubscriber.GetActionUnsubscriberAsIDisposable(subscriber);
        #endregion

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _dispatcher.ActionDispatched -= OnActionDispatched;
        }

        private void OnActionDispatched(object sender, ActionDispatchedEventArgs e)
        {
            if (IsInsideMiddlewareChange) return;

            lock(_syncRoot)
            {
                _queuedActions.Enqueue(e.Action);
            }

            if (!_isInitialized) return;
            if (_isDispatching) return;
            
            DequeueActions();
        }

        private void DequeueActions()
        {
            lock (_syncRoot)
            {
                if (_isDispatching) return;
                _isDispatching = true;
            }

            try
            {
                while (_queuedActions.TryDequeue(out IAction actionToProcess))
                {
                    if (_middlewares.All(m => m.MayDispatchAction(actionToProcess)))
                    {
                        _middlewares.ForEach(m => m.BeforeDispatch(actionToProcess));

                        foreach (var feature in _featuresByName.Values)
                            feature.ReceiveDispatchNotificationFromStore(actionToProcess);
                        
                        _actionSubscriber.Notify(actionToProcess); 
                        
                        _middlewares.ForEach(m => m.AfterDispatch(actionToProcess));

                        TriggerEffects(actionToProcess);
                    }
                }
            }
            finally
            {
                _isDispatching = false;
            }
        }
        
        private async void TriggerEffects(IAction action)
        {
            var effectsToExecute = _effects
                .Where(e => e.ShouldReactToAction(action))
                .ToList();

            if (!effectsToExecute.Any()) return;

            try
            {
                var effectTasks = effectsToExecute.Select(e => e.HandleAsync(action, _dispatcher));
                await Task.WhenAll(effectTasks);
            }
            catch (Exception e)
            {
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
            }
        }

        private void EndMiddlewareChange(IDisposable[] disposables)
        {
            lock (_syncRoot)
            {
                _beginMiddlewareChangeCount--;
                if (_beginMiddlewareChangeCount != 0) return;
                foreach (var disposable in disposables)
                    disposable.Dispose();
            }
        }
    }
}

