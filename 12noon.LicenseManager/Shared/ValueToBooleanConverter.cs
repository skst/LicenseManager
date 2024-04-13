using System;
using System.Windows.Data;

namespace Shared;

/// <summary>
/// This converter determines if the passed object has a value.
/// If it does, it returns true; it returns DoNothing.
/// </summary>
[ValueConversion(typeof(object), typeof(bool))]
public class ValueToBooleanConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		//if (targetType.IsByRef || targetType.IsClass || !targetType.IsValueType)
		if (!targetType.IsValueType)
		{
			if (value is null)
			{
				return Binding.DoNothing;
			}
			else
			{
				return true;
			}
		}
		else if ((value is int v) && (v == default))
		{
			return false;
		}
		else if ((value is double d) && (d == default))
		{
			return false;
		}
		else if ((value is string s) && string.IsNullOrEmpty(s))
		{
			return false;
		}
		else
		{
			return (value is not null);
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
