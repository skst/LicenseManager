using System;
using System.Globalization;
using System.Windows.Data;

namespace Shared;

// Optionally add a parameter to the converter to specify if All, Any, or None must be true.
public class MultiValueToBooleanConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		// Ensure all values are boolean
		foreach (var value in values)
		{
			if (value is bool booleanValue)
			{
				if (!booleanValue)
				{
					return false;
				}
			}
			else if ((value is null) || string.IsNullOrEmpty(value.ToString()))
			{
				return false;
			}
		}
		return true;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
