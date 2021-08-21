using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PomodoroTimer
{
	public abstract class BindableObject: INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName]string propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		protected bool SetProperty<T>(ref T instance, T value, [CallerMemberName]string propertyName = null)
		{
			if (instance?.Equals(value) == true)
				return false;

			instance = value;
			OnPropertyChanged(propertyName);
			return true;
		}
	}
}
