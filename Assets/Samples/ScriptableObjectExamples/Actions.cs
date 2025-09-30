using Flux;

namespace Flux.Samples.Counter
{
    public readonly struct IncrementAction : IAction { }
    public readonly struct DecrementAction : IAction { }
    public readonly struct SetAmountAction : IAction
    {
        public readonly int Amount;
        public SetAmountAction(int amount) => Amount = amount;
    }

    // Will trigger the async effect which then dispatches IncrementAction
    public readonly struct IncrementAsyncAction : IAction { }
}