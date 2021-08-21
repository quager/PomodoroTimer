using System;
using System.Globalization;
using System.Windows.Data;

namespace PomodoroTimer
{
	public class StateToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			TimerState state = (TimerState)value;
			return state != TimerState.None;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
