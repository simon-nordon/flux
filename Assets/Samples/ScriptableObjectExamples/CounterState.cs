// CounterState.cs
using System;

namespace Flux.Samples.Counter
{
    [Serializable]
    public struct CounterState
    {
        public int Value;
        public override string ToString() => $"CounterState {{ Value = {Value} }}";
    }
}