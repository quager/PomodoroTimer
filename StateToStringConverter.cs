using System;
using System.Globalization;
using System.Windows.Data;

namespace PomodoroTimer
{
	public class StateToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			TimerState state = (TimerState)value;
			string result = Resources.Localization.ResourceManager.GetString(state.ToString());
			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
