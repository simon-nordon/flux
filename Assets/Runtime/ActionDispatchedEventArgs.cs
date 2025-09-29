using System;

namespace Flux
{
    public class ActionDispatchedEventArgs : EventArgs
    {
        public IAction Action { get; private set; }

        public ActionDispatchedEventArgs(IAction action)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }
    }
}