// ===== FILE: StoreObject.cs =====
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Flux.Unity
{
    /// <summary>
    /// A ScriptableObject that acts as the central hub for the Flux state management system.
    /// It holds references to all Features, Middleware, and Effects, providing a drag-and-drop setup workflow.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStore", menuName = "Flux/Store Object")]
    public sealed class StoreObject : ScriptableObject, IStore, IDispatcher, IDisposable
    {
        [Header("Configuration")]
        [Tooltip("Features (slices of state) to register with the store.")]
        [SerializeField] private List<FeatureObject> _features = new List<FeatureObject>();

        [Tooltip("Middleware to intercept actions and add side-effects.")]
        [SerializeField] private List<MiddlewareObject> _middlewares = new List<MiddlewareObject>();

        [Tooltip("Effects to handle asynchronous operations and other side-effects.")]
        [SerializeField] private List<EffectObject> _effects = new List<EffectObject>();

        private IDispatcher _dispatcher;
        private IStore _store;
        private bool _isInitialized = false;

        // Initializes the store automatically when the game starts.
        private void OnEnable()
        {
            // This is crucial for handling domain reloads in the Unity Editor
            _isInitialized = false;
            Initialize();
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            // The core logic is delegated to the non-Unity Store and Dispatcher.
            _dispatcher = new Dispatcher();
            _store = new Store(_dispatcher);

            // Register all assigned ScriptableObject components
            foreach (var feature in _features.Where(feature => feature != null))
            {
                _store.AddFeature(feature);
            }
            foreach (var middleware in _middlewares.Where(middleware => middleware != null))
            {
                _store.AddMiddleware(middleware);
            }
            foreach (var effect in _effects.Where(effect => effect != null))
            {
                _store.AddEffect(effect);
            }

            // Fire and forget initialization
            _store.InitializeAsync();
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (_store is IDisposable disposableStore)
            {
                disposableStore.Dispose();
            }
            _isInitialized = false;
        }

        #region IDispatcher Implementation
        public void Dispatch(IAction action)
        {
            if (!_isInitialized) Initialize();
            _dispatcher.Dispatch(action);
        }

        public event EventHandler<ActionDispatchedEventArgs> ActionDispatched
        {
            add => _dispatcher.ActionDispatched += value;
            remove => _dispatcher.ActionDispatched -= value;
        }
        #endregion

        #region IStore Implementation
        public IReadOnlyDictionary<string, IFeature> Features => _store.Features;
        public Task Initialized => _store.Initialized;
        public event EventHandler<UnhandledExceptionEventArgs> UnhandledException
        {
            add => _store.UnhandledException += value;
            remove => _store.UnhandledException -= value;
        }

        public void AddEffect(IEffect effect) => _store.AddEffect(effect);
        public void AddFeature(IFeature feature) => _store.AddFeature(feature);
        public void AddMiddleware(IMiddleware middleware) => _store.AddMiddleware(middleware);
        public IDisposable BeginInternalMiddlewareChange() => _store.BeginInternalMiddlewareChange();
        public IEnumerable<IMiddleware> GetMiddlewares() => _store.GetMiddlewares();
        public Task InitializeAsync() => _store.InitializeAsync();

        // Pass through IActionSubscriber implementation to the internal store
        void IActionSubscriber.SubscribeToAction<TAction>(object subscriber, Action<TAction> callback)
        {
            ((IActionSubscriber)_store).SubscribeToAction(subscriber, callback);
        }

        void IActionSubscriber.UnsubscribeFromAllActions(object subscriber)
        {
            ((IActionSubscriber)_store).UnsubscribeFromAllActions(subscriber);
        }

        IDisposable IActionSubscriber.GetActionUnsubscriberAsIDisposable(object subscriber)
        {
            return ((IActionSubscriber)_store).GetActionUnsubscriberAsIDisposable(subscriber);
        }
        #endregion
    }
}