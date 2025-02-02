using System;
using System.Windows.Data;

namespace Shared;

/// <summary>
/// This converter toggles the passed boolean value.
///
/// If the passed value is null, it returns true.
/// If the passed value isn't a boolean, it returns false.
/// </summary>
[ValueConversion(typeof(Boolean), typeof(Boolean))]
public class BooleanNotConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		return value switch
		{
			null => true,
			bool b => !b,
			_ => false
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		// Toggle the incoming value, too.
		return Convert(value, targetType, parameter, culture);
	}
}
