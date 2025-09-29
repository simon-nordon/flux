using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flux
{
    /// <summary>
/// Identifies a store, which is a collection of features. It is recommended that you do not create your
/// own classes that implement this interface as the <see cref="Store"/> class does this for you.
/// </summary>
public interface IStore : IActionSubscriber
{
	/// <summary>
	/// This method will register an effect so that it
	/// is executed whenever an action dispatched via the store.
	/// </summary>
	/// <param name="effect">The instance of the effect to call back</param>
	/// <seealso cref="IEffect.HandleAsync(IAction, IDispatcher)"/>
	void AddEffect(IEffect effect);

	/// <summary>
	/// Adds a feature to the store. Once added, the feature will be notified of all actions dispatched
	/// via the store so that it can keep its state up to date.
	/// </summary>
	/// <param name="feature">The feature to add</param>
	void AddFeature(IFeature feature);

	/// <summary>
	/// Adds a Middleware instance to the store. The Middleware will be notified of various events ocurring
	/// in the store and be able to influence what happens as a result.
	/// </summary>
	/// <param name="middleware">The instance of the Middleware to hook into the store</param>
	void AddMiddleware(IMiddleware middleware);

	/// <summary>
	/// Creates a temporary "do not disturb" context for middleware. This prevents feedback loops,
	/// such as an auto-save middleware reacting to state changes caused by a "load game" action.
	/// Middleware can check <see cref="IMiddleware.IsInsideMiddlewareChange"/> to temporarily ignore actions.
	/// </summary>
	IDisposable BeginInternalMiddlewareChange();

	/// <summary>
	/// All of the features added to the store, keyed by their unique name.
	/// </summary>
	IReadOnlyDictionary<string, IFeature> Features { get; }

	/// <summary>
	/// This method should be executed when the store is first ready to be initialized.
	/// It will, in turn, initialise any middleware. This method can safely be executed
	/// more than once.
	/// </summary>
	/// <returns>Task</returns>
	Task InitializeAsync();

	/// <summary>
	/// Await this task if you need to asynchronously wait for the store to initialise
	/// </summary>
	/// <see cref="InitializeAsync()"/>
	Task Initialized { get; }

	/// <summary>
	/// Returns a list of registered middleware
	/// </summary>
	/// <returns>Middleware instances currently registered</returns>
	IEnumerable<IMiddleware> GetMiddlewares();
	
	/// <summary>
	/// Executed when an exception is not handled
	/// </summary>
	event EventHandler<UnhandledExceptionEventArgs> UnhandledException;
}
}