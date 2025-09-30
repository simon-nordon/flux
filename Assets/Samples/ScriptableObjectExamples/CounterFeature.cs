// CounterFeatureObject.cs
using System.Collections.Generic;
using Flux;
using Flux.Unity;
using UnityEngine;

namespace Flux.Samples.Counter
{
    [CreateAssetMenu(fileName = "CounterFeature", menuName = "Flux/Features/Counter")]
    public sealed class CounterFeatureObject : FeatureObject<CounterState>
    {
        protected override string FeatureName => "counter";

        protected override CounterState GetInitialState() => new CounterState { Value = 0 };

        protected override void AddReducers(List<IReducer<CounterState>> reducers)
        {
            reducers.Add(new IncrementReducer());
            reducers.Add(new DecrementReducer());
            reducers.Add(new SetAmountReducer());
        }
    }
}