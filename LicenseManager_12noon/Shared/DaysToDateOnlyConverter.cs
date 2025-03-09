using LicenseManager_12noon.Client;
using System;
using System.Windows;
using System.Windows.Data;

namespace Shared;

/// <summary>
/// Converts a DateTime to a DateOnly--and back.
/// </summary>
/// <example>
///	xmlns:sys="clr-namespace:System;assembly=mscorlib"
///
/// <TextBox	Text="{Binding ExpirationDays, UpdateSourceTrigger=PropertyChanged}" />
/// <TextBlock Text="{Binding ExpirationDays, Converter={StaticResource DaysToDateOnlyConverter}}" />
/// <TextBlock Text="{Binding ExpirationDays, Converter={StaticResource DaysToDateOnlyConverter}, ConverterParameter=Never}" />
/// <TextBlock Text="{Binding ExpirationDays, Converter={StaticResource DaysToDateOnlyConverter}, ConverterParameter=''}" />
///
/// public DateOnly? PublishDate { get; set; }
///
/// </example>
/// <see cref="https://michlg.wordpress.com/2009/10/13/wpf-bind-value-to-binding-converterparameter/"/>
public sealed class DaysToDateOnlyConverter : IValueConverter
{
	/// <summary>
	/// </summary>
	/// <param name="value">Number of days offset from today</param>
	/// <param name="targetType">int</param>
	/// <param name="parameter">Optional text to use when value is zero</param>
	/// <param name="culture"></param>
	/// <returns>DateOnly</returns>
	public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		string? textForZero = parameter as string;

		if (value is null)
		{
			return null;
		}

		if (value == DependencyProperty.UnsetValue)
		{
			return Binding.DoNothing;
		}

		int days = (int)value;
		if ((days == 0) && (textForZero is not null))
		{
			return textForZero;
		}

		return DateOnly.FromDateTime(MyNow.Now().AddDays(days));
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="value">DateOnly</param>
	/// <param name="targetType">Int</param>
	/// <param name="parameter"></param>
	/// <param name="culture"></param>
	/// <returns>Number of days offset from today (int)</returns>
	public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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
		return MyNow.Now().Subtract(dt.ToDateTime(new TimeOnly())).TotalDays;
	}
}
