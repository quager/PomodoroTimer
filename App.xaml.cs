using System.Threading;
using System.Windows;

namespace PomodoroTimer
{
	public partial class App : Application
	{
		private Mutex _mutex;

		protected override void OnStartup(StartupEventArgs e)
		{
			_mutex = new Mutex(true, GetType().ToString(), out bool createNew);
			if (!createNew)
				Shutdown();

			base.OnStartup(e);
		}

		private void ProcessExceptions(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(e.Exception.Message);
			e.Handled = true;

			if (!MainWindow.IsActive)
				Current.Shutdown();
		}
	}
}
