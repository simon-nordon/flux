// CounterTestDriver.cs
using Flux;
using Flux.Unity;
using UnityEngine;

namespace Flux.Samples.Counter
{
    /// <summary>
    /// Simple driver so you can bash keys and see DevTools update.
    /// Bind a StoreObject in the inspector.
    /// I = increment, K = decrement, P = async increment, O = set 10.
    /// </summary>
    public sealed class CounterTestDriver : MonoBehaviour
    {
        [SerializeField] private StoreObject store;

        void Update()
        {
            if (store == null) return;

            if (Input.GetKeyDown(KeyCode.I)) store.Dispatch(new IncrementAction());
            if (Input.GetKeyDown(KeyCode.K)) store.Dispatch(new DecrementAction());
            if (Input.GetKeyDown(KeyCode.P)) store.Dispatch(new IncrementAsyncAction());
            if (Input.GetKeyDown(KeyCode.O)) store.Dispatch(new SetAmountAction(10));
        }
    }
}