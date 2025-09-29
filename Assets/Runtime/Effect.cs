using System.Threading.Tasks;

namespace Flux
{
    /// <summary>
    /// A generic class that can be used as a base for effects.
    /// </summary>
    /// <typeparam name="TTriggerAction"></typeparam>
    public abstract class Effect<TTriggerAction> : IEffect
    {
        /// <summary>
        /// <see cref="IEffect.HandleAsync(IAction, IDispatcher)"/>
        /// </summary>
        protected abstract Task HandleAsync(TTriggerAction action, IDispatcher dispatcher);

        /// <summary>
        /// <see cref="IEffect.ShouldReactToAction(IAction)"/>
        /// </summary>
        public bool ShouldReactToAction(IAction action) =>
            action is TTriggerAction;

        Task IEffect.HandleAsync(IAction action, IDispatcher dispatcher) =>
            HandleAsync((TTriggerAction)action, dispatcher);
    }
}