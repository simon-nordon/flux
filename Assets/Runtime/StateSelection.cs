using System;

namespace Flux
{
/// <summary>
/// A class that is injected into Blazor components/pages that provides access
/// to an <see cref="IFeature{TState}"/> state.
/// </summary>
/// <typeparam name="TState"></typeparam>
public class StateSelection<TState, TValue> : IStateSelection<TState, TValue>
{
	private readonly IFeature<TState> _feature;
	private readonly object _syncRoot = new();
	private bool _hasSetSelector;
	private TValue _previousValue;
	private Func<TState, TValue> _selector;
	private Action<TValue> _selectedValueChangedAction;
	private Func<TValue, TValue, bool> _valueEquals;
	private bool ShouldBeSubscribedToFeature =>
		_selectedValueChanged is not null
		|| _stateChanged is not null;

	/// <summary>
	/// Creates an instance of the state holder
	/// </summary>
	/// <param name="feature">The feature that contains the state</param>
	public StateSelection(IFeature<TState> feature)
	{
		if (feature is null)
			throw new ArgumentNullException(nameof(feature));

		_feature = feature;
		_selector =
			_ => throw new InvalidOperationException($"Must call {nameof(Select)} before accessing {nameof(Value)}");
		_valueEquals = DefaultValueEquals;
	}

	/// <see cref="IState{TState}.Value"/>
	public TValue Value => _selector(_feature.State);

	/// <see cref="IStateSelection{TState, TValue}.Select(Func{TState, TValue}, Func{TValue, TValue, bool}))"/>
	public void Select(
		Func<TState, TValue> selector,
		Func<TValue, TValue, bool> valueEquals = null,
		Action<TValue> selectedValueChanged = null)
	{
		if (selector is null)
			throw new ArgumentNullException(nameof(selector));

		lock (_syncRoot)
		{
			if (_hasSetSelector)
				throw new InvalidOperationException("Selector has already been set");

			bool wasSubscribedToFeature = ShouldBeSubscribedToFeature;
			_selector = selector;
			_selectedValueChangedAction = selectedValueChanged;
			_hasSetSelector = true;
			if (valueEquals is not null)
				_valueEquals = valueEquals;
			_previousValue = Value;

			if (!wasSubscribedToFeature && ShouldBeSubscribedToFeature)
				_feature.StateChanged += FeatureStateChanged;
		}
	}

	private EventHandler<TValue> _selectedValueChanged;
	/// <see cref="IStateSelection{TState, TValue}.SelectedValueChanged"/>
	public event EventHandler<TValue> SelectedValueChanged
	{
		add
		{
			lock (_syncRoot)
			{
				bool wasSubscribedToFeature = ShouldBeSubscribedToFeature;
				_selectedValueChanged += value;
				if (!wasSubscribedToFeature)
					_feature.StateChanged += FeatureStateChanged;
			}
		}
		remove
		{
			lock (_syncRoot)
			{
				_selectedValueChanged -= value;
				if (!ShouldBeSubscribedToFeature)
					_feature.StateChanged -= FeatureStateChanged;
			}
		}
	}

	private EventHandler _stateChanged;
	/// <see cref="IStateChangedNotifier.StateChanged"/>
	public event EventHandler StateChanged
	{
		add
		{
			lock (_syncRoot)
			{
				bool wasSubscribedToFeature = ShouldBeSubscribedToFeature;
				_stateChanged += value;
				if (!wasSubscribedToFeature)
					_feature.StateChanged += FeatureStateChanged;
			}
		}
		remove
		{
			lock (_syncRoot)
			{
				_stateChanged -= value;
				if (!ShouldBeSubscribedToFeature)
					_feature.StateChanged -= FeatureStateChanged;
			}
		}
	}

	private void FeatureStateChanged(object sender, EventArgs e)
	{
		if (!_hasSetSelector)
			return;

		var newValue = _selector(_feature.State);
		if (_valueEquals(newValue, _previousValue))
			return;
		_previousValue = newValue;

		_selectedValueChangedAction?.Invoke(newValue);
		_selectedValueChanged?.Invoke(this, newValue);
		_stateChanged?.Invoke(this, EventArgs.Empty);
	}

	private static bool DefaultValueEquals(TValue x, TValue y) =>
		ReferenceEquals(x, y)
		|| (x as IEquatable<TValue>)?.Equals(y) == true
		|| Equals(x, y);
}
}