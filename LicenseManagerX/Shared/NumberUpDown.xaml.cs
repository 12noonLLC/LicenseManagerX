using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shared;

/// <summary>
/// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
///
/// Step 1a) Using this custom control in a XAML file that exists in the current project.
/// Add this XmlNamespace attribute to the root element of the markup file where it is
/// to be used:
///
///     xmlns:MyNamespace="clr-namespace:Shared"
///
/// Step 1b) Using this custom control in a XAML file that exists in a different project.
/// Add this XmlNamespace attribute to the root element of the markup file where it is
/// to be used:
///
///     xmlns:MyNamespace="clr-namespace:Shared;assembly=Shared"
///
/// You will also need to add a project reference from the project where the XAML file lives
/// to this project and Rebuild to avoid compilation errors:
///
///     Right click on the target project in the Solution Explorer and
///     "Add Reference"->"Projects"->[Browse to and select this project]
///
/// Step 2)
/// Add this control's XAML to the project's Themes\Generic.xaml file:
///
/// <ResourceDictionary
/// 	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
/// 	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
/// >
/// 	<ResourceDictionary.MergedDictionaries>
/// 		<ResourceDictionary Source="/MyProject;component/Shared/NumberUpDown.xaml" />
/// 	</ResourceDictionary.MergedDictionaries>
/// </ResourceDictionary>
///
/// Step 3)
/// Add this to App.xaml.cs:
///
///    [assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
///
/// Step 4)
/// Go ahead and use your control in the XAML file.
///
///     <MyNamespace:NumberUpDown />
///
/// </summary>
/// <remarks>
/// Positive values only: Minimum="0"
/// Negative values only: Maximum="0"
/// </remarks>
/// <example>
///	<shared:NumberUpDown Value="{Binding MyNumber}" AllowNull="True" ValueChanged="MyNumber_ValueChanged"
///		Padding="2" HorizontalContentAlignment="Right" VerticalContentAlignment="Center" VerticalAlignment="Center"
///	/>
///
///	private void MyNumber_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int?> e)
///	{
///	}
/// </example>
/// <see cref="https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/control-authoring-overview"/>
/// <seealso cref="https://putridparrot.com/blog/basics-of-extending-a-wpf-control/"/>
/// <seealso cref="https://www.codeproject.com/Articles/575645/Inheriting-from-a-Look-Less-WPF-Control"/>
[TemplatePart(Name="PART_ContentHost", Type=typeof(TextBox))]
[TemplatePart(Name="PART_ValueUpButton", Type=typeof(Button))]
[TemplatePart(Name="PART_ValueDownButton", Type=typeof(Button))]
public class NumberUpDown : TextBox
{
	static NumberUpDown()
	{
		DefaultStyleKeyProperty.OverrideMetadata(typeof(NumberUpDown), new FrameworkPropertyMetadata(typeof(NumberUpDown)));
	}


	/// <summary>
	/// Add a routed event that is raised when the control's value changes.
	/// </summary>
	public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(nameof(ValueChanged),
																														RoutingStrategy.Direct,
																														typeof(RoutedPropertyChangedEventHandler<int?>),
																														typeof(NumberUpDown));
	public event RoutedPropertyChangedEventHandler<int?> ValueChanged
	{
		add => AddHandler(ValueChangedEvent, value);
		remove => RemoveHandler(ValueChangedEvent, value);
	}

	protected virtual void RaiseValueChanged(int? oldValue, int? newValue)
	{
		RaiseEvent(new RoutedPropertyChangedEventArgs<int?>(oldValue, newValue, ValueChangedEvent));
	}

	#region AllowNull

	/// <summary>
	/// Indicate if the Value property is allowed to be null.
	/// </summary>
	public bool AllowNull
	{
		get => (bool)GetValue(AllowNullProperty);
		set => SetValue(AllowNullProperty, value);
	}

	public static readonly DependencyProperty AllowNullProperty = DependencyProperty.Register(
		nameof(AllowNull),
		typeof(bool),
		typeof(NumberUpDown),
		new FrameworkPropertyMetadata(defaultValue: false,
												propertyChangedCallback: OnDPAllowNullChanged)
	);

	private static void OnDPAllowNullChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		var theControl = (NumberUpDown)sender;

		/// if null is no longer allowed, Value cannot be null.
		if (!(bool)e.NewValue && (theControl.Value is null))
		{
			theControl.Value = default(int);
		}
	}

	#endregion AllowNull

	#region MinimumProperty

	/// <summary>
	/// Specify the minimum possible value for the Value property.
	/// </summary>
	public int Minimum
	{
		get => (int)GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, value);
	}

	public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
		nameof(Minimum),
		typeof(int),
		typeof(NumberUpDown),
		new FrameworkPropertyMetadata(defaultValue: int.MinValue,
												propertyChangedCallback: OnDPMinimumChanged)
	);

	private static void OnDPMinimumChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		var theControl = (NumberUpDown)sender;
		int newMinimum = (int)e.NewValue;

		if (newMinimum >= theControl.Maximum)
		{
			throw new ArgumentOutOfRangeException(nameof(Minimum), "Minimum must be less than Maximum.");
		}

		sender.CoerceValue(ValueProperty);
	}

	#endregion MinimumProperty

	#region MaximumProperty

	/// <summary>
	/// Specify the maximum possible value for the Value property.
	/// </summary>
	public int Maximum
	{
		get => (int)GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, value);
	}

	public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
		nameof(Maximum),
		typeof(int),
		typeof(NumberUpDown),
		new FrameworkPropertyMetadata(defaultValue: int.MaxValue,
												propertyChangedCallback: OnDPMaximumChanged)
	);

	private static void OnDPMaximumChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		var theControl = (NumberUpDown)sender;
		int newMaximum = (int)e.NewValue;

		if (newMaximum <= theControl.Minimum)
		{
			throw new ArgumentOutOfRangeException(nameof(Maximum), "Maximum must be greater than Minimum.");
		}

		sender.CoerceValue(ValueProperty);
	}

	#endregion MaximumProperty

	#region IncrementLargeProperty

	/// <summary>
	/// Specify the large increment value (used when the user presses page-up/down arrows).
	/// </summary>
	public int IncrementLarge
	{
		get => (int)GetValue(IncrementLargeProperty);
		set => SetValue(IncrementLargeProperty, value);
	}

	public static readonly DependencyProperty IncrementLargeProperty = DependencyProperty.Register(
		nameof(IncrementLarge),
		typeof(int),
		typeof(NumberUpDown),
		new FrameworkPropertyMetadata(defaultValue: 10)
	);

	#endregion IncrementLargeProperty

	#region IncrementSmallProperty

	/// <summary>
	/// Specify the small increment value (used when the user presses up/down arrows).
	/// </summary>
	public int IncrementSmall
	{
		get => (int)GetValue(IncrementSmallProperty);
		set => SetValue(IncrementSmallProperty, value);
	}

	public static readonly DependencyProperty IncrementSmallProperty = DependencyProperty.Register(
		nameof(IncrementSmall),
		typeof(int),
		typeof(NumberUpDown),
		new FrameworkPropertyMetadata(defaultValue: 1)
	);

	#endregion IncrementSmallProperty

	#region ValueProperty

	/// <summary>
	/// Add Value property to the control.
	/// </summary>
	public int? Value
	{
		get => (int?)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			nameof(Value),
			typeof(int?),
			typeof(NumberUpDown),
			/// Note: If the default value is not different from the initial value bound to the
			/// Value dependency property, WPF will not change it and the control will be blank.
			new FrameworkPropertyMetadata(defaultValue: null,
													FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
													propertyChangedCallback: OnDPValueChanged,
													coerceValueCallback: OnDPCoerceValue
			)
		);

	private static void OnDPValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		var theControl = (NumberUpDown)sender;
		int? oldValue = (int?)e.OldValue;
		int? newValue = (int?)e.NewValue;

		SetTextWithNumber(newValue, theControl);
		theControl.RaiseValueChanged(oldValue, newValue);
	}

	/// <summary>
	/// Ensure value obeys the minimum/maximum constraints.
	/// </summary>
	/// <param name="d"></param>
	/// <param name="baseValue"></param>
	/// <returns></returns>
	private static object OnDPCoerceValue(DependencyObject d, object baseValue)
	{
		var theControl = (NumberUpDown)d;
		var value = (int?)baseValue;

		if (value is null)
		{
			return baseValue;
		}

		int? coercedValue = value;
		if (value.Value < theControl.Minimum)
		{
			coercedValue = theControl.Minimum;
		}
		if (value.Value > theControl.Maximum)
		{
			coercedValue = theControl.Maximum;
		}

		if (coercedValue != value)
		{
			/// Using this instead of <see cref="SetValue(DependencyProperty, object)"/> keeps bindings intact.
			d.SetCurrentValue(ValueProperty, coercedValue);
			return coercedValue;
		}

		return value;
	}

	#endregion ValueProperty

	public NumberUpDown()
	{
		DataObject.AddPastingHandler(this, OnPaste);
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		/// We can also change WPF properties here. For example, Padding = new Thickness(2).
		/// But best to do it in WPF and access the value through TemplateBinding.

		/// This binding works, but we lose the caret position, so we set Text manually instead.
		/// <see cref="SetTextWithNumber(int? newValue, NumberUpDown textControl)"/>
		/// <seealso cref="https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/control-authoring-overview"/>
		//SetBinding(TextProperty, new Binding(nameof(DateTimeNullable)) { Source = this, StringFormat = "t" });

		TextChanged += TextBox_TextChanged;

		PreviewKeyDown += TextBox_PreviewKeyDown;
		PreviewMouseWheel += TextBox_PreviewMouseWheel;

		// Same as Template.FindName("PART_HoursUpButton", this);
		var buttonValueUp = (Button)GetTemplateChild("PART_ValueUpButton");
		buttonValueUp.Click += ValueUpButton_Click;

		var buttonValueDown = (Button)GetTemplateChild("PART_ValueDownButton");
		buttonValueDown.Click += ValueDownButton_Click;
	}

	private static void SetTextWithNumber(int? newValue, NumberUpDown textControl)
	{
		if (newValue is null)
		{
			if (textControl.AllowNull)
			{
				textControl.Text = string.Empty;
				return;
			}

			newValue = default(int);
		}

		/// Save the caret position
		var caretIndex = textControl.CaretIndex;
		var selectionStart = textControl.SelectionStart;
		var selectionLength = textControl.SelectionLength;

		textControl.Text = newValue.Value.ToString(CultureInfo.InvariantCulture);

		/// Restore the caret position
		textControl.CaretIndex = caretIndex;
		textControl.SelectionStart = selectionStart;
		textControl.SelectionLength = selectionLength;
	}


	/// <summary>
	/// The user has typed in the control. We parse the text, validate it,
	/// and--if valid--set the Value property.
	/// If it's not valid, we leave it because the user might be fixing the value.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (string.IsNullOrEmpty(Text))
		{
			if (AllowNull)
			{
				Value = null;
			}
			else
			{
				Value = Value.GetValueOrDefault();
			}
			return;
		}

		if (int.TryParse(Text, out int valueParsed))
		{
			if (valueParsed != Value)
			{
				Value = valueParsed;
			}
		}
	}

	private void ValueUpButton_Click(object /*Button*/ sender, RoutedEventArgs e)
	{
		IncrementValue(IncrementSmall);
	}

	private void ValueDownButton_Click(object /*Button*/ sender, RoutedEventArgs e)
	{
		IncrementValue(-IncrementSmall);
	}

	private void TextBox_PreviewKeyDown(object /*TextBox*/ sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
			case Key.Up:
				IncrementValue(IncrementSmall);
				break;
			case Key.Down:
				IncrementValue(-IncrementSmall);
				break;

			case Key.PageUp:
				IncrementValue(IncrementLarge);
				break;
			case Key.PageDown:
				IncrementValue(-IncrementLarge);
				break;

			default:
				return;
		}
	}

	private void TextBox_PreviewMouseWheel(object /*TextBox*/ sender, MouseWheelEventArgs e)
	{
		IncrementValue(IncrementSmall * Math.Sign(e.Delta));
	}

	private void IncrementValue(int delta)
	{
		if (Value is null)
		{
			Value = delta;
		}
		else
		{
			/*
			 * When this is bound to a UInt32, attempting to set a value less than 0 raises this exception.
			 *
			 * System.Windows.Data Error: 7 : ConvertBack cannot convert value '-9' (type 'Int32').
			 * BindingExpression:Path=TheAlarm.Chronos.Duration.NumberOfTimes; DataItem='WindowAlarm' (Name='');
			 * target element is 'NumberUpDown' (Name=''); target property is 'Value' (type 'Nullable`1')
			 * OverflowException:'System.OverflowException: Value was either too large or too small for a UInt32.
			 */
			Value += delta;
		}
	}


	private void OnPaste(object sender, DataObjectPastingEventArgs e)
	{
		if (!e.DataObject.GetDataPresent(typeof(string)))
		{
			e.CancelCommand();
			return;
		}

		string pasteText = (string)e.DataObject.GetData(typeof(string));
		if (!int.TryParse(pasteText, out int newValue))
		{
			e.CancelCommand();
			return;
		}

		Value = newValue;

		// We have to cancel the command so the normal paste does not change the value.
		e.CancelCommand();
	}
}
