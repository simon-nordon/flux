// ===== FILE: EffectObject.cs =====
using System.Threading.Tasks;
using UnityEngine;

namespace Flux.Unity
{
    /// <summary>
    /// Base ScriptableObject for all Effects. Inherit from the generic EffectObject<TTriggerAction>
    /// </summary>
    public abstract class EffectObject : ScriptableObject, IEffect
    {
        public abstract Task HandleAsync(IAction action, IDispatcher dispatcher);
        public abstract bool ShouldReactToAction(IAction action);
    }
    
    /// <summary>
    /// An abstract, typed ScriptableObject for creating Effects.
    /// Inherit from this to create an Effect asset that can be added to a StoreObject.
    /// </summary>
    /// <typeparam name="TTriggerAction">The specific action type this effect listens for.</typeparam>
    public abstract class EffectObject<TTriggerAction> : EffectObject where TTriggerAction : IAction
    {
        // This is the method users will override in their concrete effect asset script.
        protected abstract Task HandleAsync(TTriggerAction action, IDispatcher dispatcher);

        public override bool ShouldReactToAction(IAction action)
        {
            return action is TTriggerAction;
        }

        public sealed override Task HandleAsync(IAction action, IDispatcher dispatcher)
        {
            // The cast is safe because ShouldReactToAction is checked first by the Store.
            return HandleAsync((TTriggerAction)action, dispatcher);
        }
    }
}