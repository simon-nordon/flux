using System;

namespace Flux
{
    /// <summary>
    /// A simple, Unity-centric dispatcher that immediately notifies subscribers of a dispatched action.
    /// It is stripped of complex queueing logic, as that responsibility lies with the Store.
    /// This implementation is inherently safe for single-threaded environments like WebGL.
    /// </summary>
    public class Dispatcher : IDispatcher
    {
        /// <summary>
        /// This event is triggered whenever an action is dispatched. The Store is the primary subscriber.
        /// </summary>
        public event EventHandler<ActionDispatchedEventArgs> ActionDispatched;

        /// <summary>
        /// Dispatches an action to all subscribers (primarily the Store).
        /// </summary>
        /// <param name="action">The action to dispatch.</param>
        public void Dispatch(IAction action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            // Immediately invoke the event with the dispatched action.
            // There is no internal queueing; the dispatcher's job is simply to pass the message on.
            // The ?.Invoke pattern is thread-safe and handles cases where there are no subscribers.
            ActionDispatched?.Invoke(this, new ActionDispatchedEventArgs(action));
        }
    }
}