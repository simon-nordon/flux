// ===== FILE: FeatureObject.cs =====
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flux.Unity
{
    /// <summary>
    /// Base ScriptableObject for all Features. Implement your features by inheriting from the generic FeatureObject<TState>.
    /// </summary>
    public abstract class FeatureObject : ScriptableObject, IFeature 
    {
        public abstract string GetName();
        public virtual bool DebuggerBrowsable => true;
        public byte MaximumStateChangedNotificationsPerSecond { get; set; }
        public abstract object GetState();
        public abstract Type GetStateType();
        public abstract void RestoreState(object value);
        public abstract void ReceiveDispatchNotificationFromStore(IAction action);
        public abstract event EventHandler StateChanged;
    }

    /// <summary>
    /// An abstract, typed ScriptableObject representing a slice of application state (a Feature).
    /// Inherit from this class to create your own Feature assets that can be added to a StoreObject.
    /// </summary>
    public abstract class FeatureObject<TState> : FeatureObject, IFeature<TState>
    {
        // This delegates the actual runtime state management to a standard Feature class instance.
        // This is critical to ensure state is NOT serialized into the asset and persists between play sessions.
        private IFeature<TState> _runtimeFeature;

        private void OnEnable()
        {
            _runtimeFeature = new RuntimeFeature(this);
        }

        // --- Interface Delegation ---
        public override string GetName() => _runtimeFeature.GetName();
        public override object GetState() => _runtimeFeature.GetState();
        public override Type GetStateType() => _runtimeFeature.GetStateType();
        public override void RestoreState(object value) => _runtimeFeature.RestoreState(value);
        public override void ReceiveDispatchNotificationFromStore(IAction action) => _runtimeFeature.ReceiveDispatchNotificationFromStore(action);
        public override event EventHandler StateChanged
        {
            add => _runtimeFeature.StateChanged += value;
            remove => _runtimeFeature.StateChanged -= value;
        }
        public TState State => _runtimeFeature.State;
        public void AddReducer(IReducer<TState> reducer) => _runtimeFeature.AddReducer(reducer);

        // --- User Implementation ---
        protected abstract string FeatureName { get; }
        protected abstract TState GetInitialState();
        protected virtual void AddReducers(List<IReducer<TState>> reducers) { }

        /// <summary>
        /// An inner class that holds the actual runtime state, which is reset whenever the game starts.
        /// </summary>
        private class RuntimeFeature : Feature<TState>
        {
            private readonly FeatureObject<TState> _so;

            public RuntimeFeature(FeatureObject<TState> so)
            {
                _so = so;
                var reducerList = new List<IReducer<TState>>();
                _so.AddReducers(reducerList);
                foreach(var reducer in reducerList)
                {
                    AddReducer(reducer);
                }
            }

            public override string GetName() => _so.FeatureName;
            protected override TState GetInitialState() => _so.GetInitialState();
        }
    }
}