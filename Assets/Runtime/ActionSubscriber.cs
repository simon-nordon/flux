using System;
using System.Collections.Generic;
using System.Linq;

namespace Flux
{
    /// <summary>
    /// Handles subscribing, unsubscribing, and notifying listeners of actions. This implementation
    /// has been adapted for a Unity context with strong typing via IAction.
    /// </summary>
    internal class ActionSubscriber : IActionSubscriber
    {
        private readonly object _syncRoot = new();
        private readonly Dictionary<object, List<ActionSubscription>> _subscriptionsForInstance = new();
        private readonly Dictionary<Type, List<ActionSubscription>> _subscriptionsForType = new();
        
        public IDisposable GetActionUnsubscriberAsIDisposable(object subscriber) =>
            new DisposableCallback(
                id: $"{nameof(ActionSubscriber)}.{nameof(GetActionUnsubscriberAsIDisposable)}",
                action: () => UnsubscribeFromAllActions(subscriber));

        public void Notify(IAction action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            Action<IAction>[] callbacks;
            lock (_syncRoot)
            {
                // Find all subscription types that are compatible with the dispatched action's type.
                callbacks = _subscriptionsForType
                    .Where(x => x.Key.IsAssignableFrom(action.GetType()))
                    .SelectMany(x => x.Value)
                    .Select(x => x.Callback)
                    .ToArray<Action<IAction>>();
            }

            foreach (var callback in callbacks)
            {
                // UNITY NOTE: Exceptions in subscribers should not stop other subscribers from being notified.
                try
                {
                    callback(action);
                }
                catch (Exception e)
                {
                   throw new Exception($"[ActionSubscriber] Exception in subscriber: {callback}", e);
                }
            }
        }

        void IActionSubscriber.SubscribeToAction<TAction>(object subscriber, Action<TAction> callback)
        {
            if (subscriber is null) throw new ArgumentNullException(nameof(subscriber));
            if (callback is null) throw new ArgumentNullException(nameof(callback));

            // Create a wrapper callback that accepts the base IAction and performs a safe cast.
            var subscription = new ActionSubscription(
                subscriber: subscriber,
                actionType: typeof(TAction),
                callback: (action) => callback((TAction)action));

            lock (_syncRoot)
            {
                if (!_subscriptionsForInstance.TryGetValue(subscriber, out var instanceSubscriptions))
                {
                    instanceSubscriptions = new List<ActionSubscription>();
                    _subscriptionsForInstance[subscriber] = instanceSubscriptions;
                }
                instanceSubscriptions.Add(subscription);

                if (!_subscriptionsForType.TryGetValue(typeof(TAction), out var typeSubscriptions))
                {
                    typeSubscriptions = new List<ActionSubscription>();
                    _subscriptionsForType[typeof(TAction)] = typeSubscriptions;
                }
                typeSubscriptions.Add(subscription);
            };
        }

        public void UnsubscribeFromAllActions(object subscriber)
        {
            if (subscriber is null)
                throw new ArgumentNullException(nameof(subscriber));

            lock (_syncRoot)
            {
                if (!_subscriptionsForInstance.TryGetValue(subscriber, out var instanceSubscriptions))
                    return;

                // For each subscription being removed, find it in the type-based dictionary and remove it.
                foreach (var subscriptionToRemove in instanceSubscriptions.ToList()) // ToList creates a copy to iterate over
                {
                    if (_subscriptionsForType.TryGetValue(subscriptionToRemove.ActionType, out var typeSubscriptions))
                    {
                        typeSubscriptions.Remove(subscriptionToRemove);
                        if (typeSubscriptions.Count == 0)
                            _subscriptionsForType.Remove(subscriptionToRemove.ActionType);
                    }
                }

                // Finally, remove the subscriber instance itself.
                _subscriptionsForInstance.Remove(subscriber);
            }
        }
    }
}
