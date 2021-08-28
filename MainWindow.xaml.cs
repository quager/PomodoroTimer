using System;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace PomodoroTimer
{
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private const int FlashInterval = 150;
		private const int SlideInterval = 300;
		private const int MinDistance = 20;
		private const int FlashTime = 3;

		private Timer _timer;
		private NotifyIcon _notifyIcon;
		private ContextMenuStrip _contextMenu;
		private Storyboard _flashStoryboard;
		private Storyboard _settingsShowStoryboard;
		private Storyboard _settingsHideStoryboard;
		private int _longPauseMinTime = 15;
		private int _longPauseMaxTime = 30;
		private int _taskMinTime = 20;
		private int _taskMaxTime = 25;
		private bool _isSettingsVisible;
		private bool _ignoreMoving;

		public Settings Settings { get; }

		public bool IsSettingsVisible
		{
			get => _isSettingsVisible;
			set => SetProperty(ref _isSettingsVisible, value);
		}

		public string Notification
		{
			get => Settings.NotificationPath;
			set
			{
				Settings.NotificationPath = value;
				OnPropertyChanged();
			}
		}

		public Timer Timer
		{
			get => _timer;
			set => SetProperty(ref _timer, value);
		}

		public int LongPauseTime
		{
			get => Settings.LongPauseInterval;
			set
			{
				Settings.LongPauseInterval = value;
				if (Timer != null)
					Timer.LongPauseInterval = value;

				OnPropertyChanged();
			}
		}

		public int LongPauseMinTime
		{
			get => _longPauseMinTime;
			set => SetProperty(ref _longPauseMinTime, value);
		}

		public int LongPauseMaxTime
		{
			get => _longPauseMaxTime;
			set => SetProperty(ref _longPauseMaxTime, value);
		}

		public int TaskTime
		{
			get => Settings.TaskInterval;
			set
			{
				Settings.TaskInterval = value;
				if (Timer != null)
					Timer.TaskInterval = value;

				OnPropertyChanged();
			}
		}

		public int TaskMinTime
		{
			get => _taskMinTime;
			set => SetProperty(ref _taskMinTime, value);
		}

		public int TaskMaxTime
		{
			get => _taskMaxTime;
			set => SetProperty(ref _taskMaxTime, value);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;

			_contextMenu = new ContextMenuStrip();
			var openMenuItem = new ToolStripMenuItem
			{
				Text = PomodoroTimer.Resources.Localization.Open
			};
			openMenuItem.Click += new EventHandler(OpenMenuItem_Click);

			var exitMenuItem = new ToolStripMenuItem
			{
				Text = PomodoroTimer.Resources.Localization.Exit
			};
			exitMenuItem.Click += new EventHandler(ExitMenuItem_Click);

			_contextMenu.Items.AddRange(new[] { openMenuItem, exitMenuItem });

			_notifyIcon = new NotifyIcon();
			_notifyIcon.MouseClick += new MouseEventHandler(OnNotifyIconClick);
			_notifyIcon.Icon = PomodoroTimer.Resources.CommonResources.Icon;
			_notifyIcon.ContextMenuStrip = _contextMenu;
			_notifyIcon.Text = System.Windows.Application.Current.MainWindow.Title;
			_notifyIcon.Visible = true;

			Settings = new Settings();
			ApplySettings();

			CreateFlashAnimation();
			CreateShowSettingsAnimation();
			CreateHideSettingsAnimation();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			Timer?.Dispose();
			_contextMenu.Dispose();
			_notifyIcon.Dispose();
			base.OnClosing(e);
		}

		private void ApplySettings()
		{
			Notification = Settings.NotificationPath;

			if (Settings.TaskInterval > 0)
				TaskTime = Settings.TaskInterval;

			if (Settings.LongPauseInterval > 0)
				LongPauseTime = Settings.LongPauseInterval;

			if (Settings.WindowArea.Height > 0 && Settings.WindowArea.Width > 0)
			{
				Left = Settings.WindowArea.Left;
				Top = Settings.WindowArea.Top;
				Width = Settings.WindowArea.Width;
				Height = Settings.WindowArea.Height;
			}
			else
			{
				Left = (Screen.PrimaryScreen.WorkingArea.Width - Width) / 2;
				Top = (Screen.PrimaryScreen.WorkingArea.Height - Height) / 2;
			}
		}

		private void CreateFlashAnimation()
		{
			var animation = new DoubleAnimation
			{
				From = 0.0,
				To = 1.0,
				AutoReverse = true,
				RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(FlashTime)),
				Duration = new Duration(TimeSpan.FromMilliseconds(FlashInterval))
			};

			_flashStoryboard = CreateAnimation(FlashingGrid, animation, "Opacity");
		}

		private void CreateShowSettingsAnimation()
		{
			var animation = new ThicknessAnimation
			{
				From = new Thickness(10, -130, 10, 0),
				To = new Thickness(10, 0, 10, 0),
				RepeatBehavior = new RepeatBehavior(1),
				Duration = new Duration(TimeSpan.FromMilliseconds(SlideInterval))
			};

			_settingsShowStoryboard = CreateAnimation(SettingsPanel, animation, "Margin");
		}

		private void CreateHideSettingsAnimation()
		{
			var animation = new ThicknessAnimation
			{
				From = new Thickness(10, 0, 10, 0),
				To = new Thickness(10, -130, 10, 0),
				RepeatBehavior = new RepeatBehavior(1),
				Duration = new Duration(TimeSpan.FromMilliseconds(SlideInterval))
			};

			_settingsHideStoryboard = CreateAnimation(SettingsPanel, animation, "Margin");
		}

		private Storyboard CreateAnimation(DependencyObject animatedObject, AnimationTimeline animation, string propertyName)
		{
			var storyboard = new Storyboard();
			storyboard.Children.Add(animation);
			Storyboard.SetTarget(animation, animatedObject);
			Storyboard.SetTargetProperty(animation, new PropertyPath(propertyName));

			return storyboard;
		}

		private void OnNotifyIconClick(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
				return;

			if (WindowState != WindowState.Minimized)
			{
				OnHide(sender, null);
				return;
			}

			RestoreWindow();
		}

		private void RestoreWindow()
		{
			_ignoreMoving = true;
			Show();
			WindowState = WindowState.Normal;
			_ignoreMoving = false;
		}

		private void Start(object sender, RoutedEventArgs e)
		{
			if (Timer == null || Timer.State == TimerState.None)
			{
				Timer = Timer.StartNew(TaskTime, LongPauseTime);
				Timer.OnStateChanged += OnTimerStateChanged;
			}
			else
			{
				if (Timer.IsActive)
					Timer.Stop();
				else
					Timer.Start();
			}
		}

		private void OnTimerStateChanged(TimerState state)
		{
			Dispatcher.Invoke(() =>
			{
				if (WindowState == WindowState.Minimized)
					RestoreWindow();

				_flashStoryboard.Begin(FlashingGrid);
			});

			if (string.IsNullOrWhiteSpace(Notification) || !File.Exists(Notification))
				return;

			SoundPlayer player = new SoundPlayer(Notification);
			player.Play();
		}

		private void SelectNotification(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Filter = "Wav Files|*.wav|MP3 Files|*.mp3|All Files|*.*",
				CheckFileExists = true,
				Title = PomodoroTimer.Resources.Localization.SelectNotification
			};

			if (dialog.ShowDialog() != true)
				return;

			Notification = dialog.FileName;
		}

		private void OnSwitchTheme(object sender, RoutedEventArgs e) =>
			Settings.CurrentTheme = Settings.CurrentTheme == "default" ? "dark" : "default";

		private bool SetProperty<T>(ref T instance, T value, [CallerMemberName]string propertyName = null)
		{
			if (instance?.Equals(value) == true)
				return false;

			instance = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		private void OnPropertyChanged([CallerMemberName]string propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		private void OpenMenuItem_Click(object sender, EventArgs e) =>
			RestoreWindow();

		private void ExitMenuItem_Click(object sender, EventArgs e) =>
			Close();

		private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
			DragMove();

		private void OnLoaded(object sender, RoutedEventArgs e) =>
			LocationChanged += OnLocationChanged;

		private void OnHide(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
			Hide();
		}

		private void OnShowSettings(object sender, RoutedEventArgs e)
		{
			_settingsShowStoryboard.Begin(SettingsPanel);
			IsSettingsVisible = true;
		}

		private void OnHideSettings(object sender, RoutedEventArgs e)
		{
			_settingsHideStoryboard.Begin(SettingsPanel);
			IsSettingsVisible = false;
		}

		private void OnLocationChanged(object sender, EventArgs e)
		{
			var point = PointToScreen(new Point(0d, 0d));
			Screen screen = Screen.FromPoint(new System.Drawing.Point((int)Left, (int)Top));
			double left = point.X + Width - screen.WorkingArea.Left;
			double top = point.Y + Height - screen.WorkingArea.Top;
			
			if (point.X - screen.WorkingArea.Left < MinDistance)
				Left = screen.WorkingArea.Left;
			else
			if (screen.WorkingArea.Width - left < MinDistance && left - screen.WorkingArea.Width < MinDistance)
				Left = screen.WorkingArea.Width + screen.WorkingArea.Left - Width;

			if (point.Y - screen.WorkingArea.Top < MinDistance)
				Top = screen.WorkingArea.Top;
			else
			if (screen.WorkingArea.Height - top < MinDistance && top - screen.WorkingArea.Height < MinDistance)
				Top = screen.WorkingArea.Height + screen.WorkingArea.Top - Height;

			if (!_ignoreMoving)
				Settings.WindowArea = new Rect(Left, Top, Width, Height);
		}
	}
}
