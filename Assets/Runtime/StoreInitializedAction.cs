namespace Flux
{
    /// <summary>
    /// Dispatched by the store once it has been initialised. This is usually the first
    /// action dispatched, although it might not be because Middlewares may dispatch
    /// actions earlier on.
    /// </summary>
    public class StoreInitializedAction : IAction
    {
        internal StoreInitializedAction() { }
    }
}