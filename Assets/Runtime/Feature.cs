using System;
using System.Collections.Generic;
using System.Linq;

namespace Flux
{
// <summary>
    /// An abstract base class for a Feature, which represents a slice of application state.
    /// This implementation is adapted for Unity and is WebGL-safe.
    /// </summary>
    /// <typeparam name="TState">The type of state managed by this Feature.</typeparam>
    public abstract class Feature<TState> : IFeature<TState>
    {
        public event EventHandler StateChanged;

        /// <summary>
        /// If greater than 0, the feature will not execute state changes
        /// more often than this many times per second.
        /// UNITY NOTE: This is ignored in the Unity-safe version to ensure immediate UI updates.
        /// </summary>
        public byte MaximumStateChangedNotificationsPerSecond { get; set; }

        /// <summary>
        /// Gets a value indicating whether the property should be visible in any attached debugger.
        /// </summary>
        public virtual bool DebuggerBrowsable { get; set; } = true;
        
        public abstract string GetName();
        protected abstract TState GetInitialState();

        public Type GetStateType() => typeof(TState);
        public object GetState() => State;
        
        /// <summary>
        /// Sets the state of the feature. This is intended for use by dev tools or a save-game system.
        /// </summary>
        /// <param name="value">The state to restore.</param>
        public virtual void RestoreState(object value) => State = (TState)value;

        protected readonly List<IReducer<TState>> Reducers = new();
        private readonly object _syncRoot = new();
        private bool _isInitialized;
        private TState _state;

        public TState State
        {
            get
            {
                if (_isInitialized) return _state;
                lock (_syncRoot)
                {
                    if (_isInitialized) return _state;
                    _state = GetInitialState();
                    _isInitialized = true;
                }
                return _state;
            }
            private set
            {
                var stateHasChanged = !EqualityComparer<TState>.Default.Equals(_state, value);
                if (!stateHasChanged) return;
                lock (_syncRoot)
                {
                    _state = value;
                    if (!_isInitialized)
                        _isInitialized = true;
                }
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        public void AddReducer(IReducer<TState> reducer)
        {
            if (reducer is null)
                throw new ArgumentNullException(nameof(reducer));
            Reducers.Add(reducer);
        }

        public void ReceiveDispatchNotificationFromStore(IAction action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            var applicableReducers = Reducers.Where(r => r.ShouldReduceStateForAction(action));

            var newState = applicableReducers.Aggregate(State, (current, reducer) => reducer.Reduce(current, action));

            State = newState;
        }
    }
}

