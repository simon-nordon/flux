// Reducers.cs
using Flux;

namespace Flux.Samples.Counter
{
    // Reducer<TState, TAction> is from your core
    public sealed class IncrementReducer : Reducer<CounterState, IncrementAction>
    {
        public override CounterState Reduce(CounterState state, IncrementAction action)
            => new CounterState { Value = state.Value + 1 };
    }

    public sealed class DecrementReducer : Reducer<CounterState, DecrementAction>
    {
        public override CounterState Reduce(CounterState state, DecrementAction action)
            => new CounterState { Value = state.Value - 1 };
    }

    public sealed class SetAmountReducer : Reducer<CounterState, SetAmountAction>
    {
        public override CounterState Reduce(CounterState state, SetAmountAction action)
            => new CounterState { Value = action.Amount };
    }
}