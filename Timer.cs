using System;
using System.Threading;
using System.Collections.ObjectModel;
using Clock = System.Threading.Timer;

namespace PomodoroTimer
{
	public enum TimerState
	{
		None = 0,
		TaskTime = 1,
		PauseTime = 2
	}

	public class Pomodoro : BindableObject
	{
		private bool _isFinished;

		public bool IsFinished
		{
			get => _isFinished;
			set => SetProperty(ref _isFinished, value);
		}
	}

	public class Timer : BindableObject, IDisposable
	{
		private const int OneSecond = 1000;
		private const int MinuteSeconds = 60;
		private const int PauseInterval = 5;
		private const int MaxPomodoros = 4;

		private int _pomodorosNumber = MaxPomodoros;
		private int _longPauseInterval;
		private int _taskInterval;
		private TimerState _state;
		private bool _disposed;
		private bool _isActive;
		private int _timeLeft;
		private Clock _timer;

		public bool IsActive
		{
			get => _isActive;
			set => SetProperty(ref _isActive, value);
		}

		public int TimeLeft
		{
			get => _timeLeft;
			set => SetProperty(ref _timeLeft, value);
		}

		public int TaskInterval
		{
			get => _taskInterval;
			set => SetProperty(ref _taskInterval, value);
		}

		public int LongPauseInterval
		{
			get => _longPauseInterval;
			set => SetProperty(ref _longPauseInterval, value);
		}

		public TimerState State
		{
			get => _state;
			set
			{
				if (SetProperty(ref _state, value))
					OnStateChanged?.Invoke(_state);
			}
		}

		public ObservableCollection<Pomodoro> Pomodoros { get; }

		public event Action<TimerState> OnStateChanged;

		public Timer(int taskInterval, int longPauseInterval)
		{
			TaskInterval = taskInterval;
			LongPauseInterval = longPauseInterval;
			Pomodoros = new ObservableCollection<Pomodoro>();

			for (int i = 0; i < MaxPomodoros; i++)
				Pomodoros.Add(new Pomodoro());
		}

		public static Timer StartNew(int taskInterval, int longPauseInterval)
		{
			var result = new Timer(taskInterval, longPauseInterval);
			result.Start();
			return result;
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			DisposeTimer();
			_disposed = true;
		}

		public void Stop()
		{
			DisposeTimer();
			IsActive = false;
		}

		public void Reset()
		{
			IsActive = false;
			State = TimerState.None;
			DisposeTimer();

			_pomodorosNumber = MaxPomodoros;
			foreach (Pomodoro item in Pomodoros)
				item.IsFinished = false;
		}

		public void Start()
		{
			IsActive = true;
			if (State == TimerState.None || TimeLeft == 0)
			{
				State = TimerState.TaskTime;
				TimeLeft = TaskInterval * MinuteSeconds;
			}

			DisposeTimer();
			_timer = new Clock(new TimerCallback(OnTaskTimer), null, 0, OneSecond);
		}

		private void Pause()
		{
			if (State == TimerState.None || TimeLeft == 0)
			{
				_pomodorosNumber--;
				Pomodoros[_pomodorosNumber].IsFinished = true;
				State = TimerState.PauseTime;
				TimeLeft = (_pomodorosNumber == 0 ? LongPauseInterval : PauseInterval) * MinuteSeconds;
			}

			if (_pomodorosNumber == 0)
			{
				_pomodorosNumber = MaxPomodoros;
				foreach (Pomodoro item in Pomodoros)
					item.IsFinished = false;
			}

			DisposeTimer();
			_timer = new Clock(new TimerCallback(OnPauseTimer), null, 0, OneSecond);
		}

		private void OnTaskTimer(object state)
		{
			if (TimeLeft == 0)
				Pause();
			else
				TimeLeft--;
		}

		private void OnPauseTimer(object state)
		{
			if (TimeLeft == 0)
				Start();
			else
				TimeLeft--;
		}

		private void DisposeTimer()
		{
			_timer?.Dispose();
			_timer = null;
		}
	}
}
