using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Shared;

/// <summary>
/// Converts a DateTime to a DateOnly--and back.
/// </summary>
/// <example>
///	xmlns:sys="clr-namespace:System;assembly=mscorlib"
///
/// <DatePicker SelectedDate="{Binding PublishDate, Converter={StaticResource DateTimeDateOnlyConverter}}" />
///
/// public DateOnly? PublishDate { get; set; }
///
/// </example>
/// <see cref="https://michlg.wordpress.com/2009/10/13/wpf-bind-value-to-binding-converterparameter/"/>
public sealed class DateTimeToDateOnlyConverter : IValueConverter
{
	/// <summary>
	/// </summary>
	/// <param name="value">DateTime</param>
	/// <param name="targetType"></param>
	/// <param name="parameter"></param>
	/// <param name="culture"></param>
	/// <returns>DateOnly</returns>
	public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is null)
		{
			return null;
		}

		if (value == DependencyProperty.UnsetValue)
		{
			return Binding.DoNothing;
		}

		DateOnly dt = (DateOnly)value;
		return dt.ToDateTime(new TimeOnly());
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="value">DateOnly</param>
	/// <param name="targetType"></param>
	/// <param name="parameter"></param>
	/// <param name="culture"></param>
	/// <returns>DateTime</returns>
	public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is null)
		{
			return null;
		}

		if (value == DependencyProperty.UnsetValue)
		{
			return Binding.DoNothing;
		}

		DateTime dt = (DateTime)value;
		return DateOnly.FromDateTime(dt);
	}
}
