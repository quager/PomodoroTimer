using System;
using System.Globalization;
using System.Windows.Data;

namespace PomodoroTimer
{
	public class TimeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int time = (int)value;
			return $"{time / 3600:D2}:{time / 60 % 60:D2}:{time % 60:D2}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
