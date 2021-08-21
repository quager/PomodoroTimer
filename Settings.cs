using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Baml2006;
using System.Windows.Markup;
using System.Xml.Linq;

namespace PomodoroTimer
{
	public class Settings : BindableObject
	{
		private const string SettingsFileName = "Settings.xml";
		private const int DefaultLongPauseTime = 15;
		private const int DefaultTaskTime = 25;

		private string _notificationPath;
		private string _currentTheme;
		private int _longPauseInterval = DefaultLongPauseTime;
		private int _taskInterval = DefaultTaskTime;
		private Rect _windowArea;

		public Rect WindowArea
		{
			get => _windowArea;
			set
			{
				_windowArea = value;
				TrySaveSettings();
			}
		}

		public int TaskInterval
		{
			get => _taskInterval;
			set
			{
				if (SetProperty(ref _taskInterval, value))
					TrySaveSettings();
			}
		}

		public int LongPauseInterval
		{
			get => _longPauseInterval;
			set
			{
				if (SetProperty(ref _longPauseInterval, value))
					TrySaveSettings();
			}
		}

		public string NotificationPath
		{ 
			get => _notificationPath;
			set
			{
				if (SetProperty(ref _notificationPath, value))
					TrySaveSettings();
			} 
		}

		public string CurrentTheme
		{
			get => _currentTheme;
			set
			{
				string old = _currentTheme;
				if (SetProperty(ref _currentTheme, value))
				{
					if (!string.IsNullOrEmpty(old) && Themes.TryGetValue(old, out ResourceDictionary resourceDict))
						Application.Current.Resources.MergedDictionaries.Remove(resourceDict);

					if (!string.IsNullOrEmpty(value) && Themes.TryGetValue(value, out resourceDict))
						Application.Current.Resources.MergedDictionaries.Insert(0, resourceDict);

					TrySaveSettings();
				}
			}
		}

		public Dictionary<string, ResourceDictionary> Themes { get; } = new Dictionary<string, ResourceDictionary>();

		public Settings()
		{
			if (!TryLoadSettings())
				CurrentTheme = Themes.First(t => t.Key.StartsWith("default", StringComparison.InvariantCultureIgnoreCase)).Key;// "Default";
		}

		private bool TryLoadSettings()
		{
			Themes.Clear();

			Application.Current.Resources.MergedDictionaries.Clear();
			LoadThemes();

			if (!File.Exists(SettingsFileName))
				return false;

			XDocument xml = XDocument.Load(SettingsFileName);
			CurrentTheme = xml.Root.Attribute("Theme")?.Value;
			NotificationPath = xml.Root.Element("NotificationSound")?.Value;

			XElement intervals = xml.Root.Element("Intervals");
			TaskInterval = int.Parse(intervals?.Attribute("TaskInterval")?.Value ?? "0");
			LongPauseInterval = int.Parse(intervals?.Attribute("LongPauseInterval")?.Value ?? "0");

			XElement window = xml.Root.Element("Window");
			int left = int.Parse(window?.Attribute("Left")?.Value ?? "0");
			int top = int.Parse(window?.Attribute("Top")?.Value ?? "0");
			int width = int.Parse(window?.Attribute("Width")?.Value ?? "0");
			int height = int.Parse(window?.Attribute("Height")?.Value ?? "0");
			WindowArea = new Rect(left, top, width, height);

			return true;
		}

		private void TrySaveSettings()
		{
			XDocument xml = new XDocument(new XElement("Settings", new XAttribute("Theme", CurrentTheme)));

			if (!string.IsNullOrWhiteSpace(NotificationPath) && File.Exists(NotificationPath))
				xml.Root.Add(new XElement("NotificationSound", NotificationPath));

			xml.Root.Add(new XElement("Intervals",
				new XAttribute("TaskInterval", TaskInterval),
				new XAttribute("LongPauseInterval", LongPauseInterval)));

			xml.Root.Add(new XElement("Window",
				new XAttribute("Left", (int)WindowArea.Left),
				new XAttribute("Top", (int)WindowArea.Top),
				new XAttribute("Width", (int)WindowArea.Width),
				new XAttribute("Height", (int)WindowArea.Height)));

			xml.Save(SettingsFileName);
		}

		private void LoadThemes()
		{
			Assembly assembly = GetType().Assembly;
			Stream stream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".g.resources");

			using (ResourceReader reader = new ResourceReader(stream))
			{
				foreach (DictionaryEntry entry in reader)
				{
					string resourceName = entry.Key.ToString();
					if (!resourceName.StartsWith("themes/", StringComparison.InvariantCultureIgnoreCase))
						continue;

					string name = Path.GetFileNameWithoutExtension(resourceName);
					var readStream = entry.Value as Stream;
					Baml2006Reader bamlReader = new Baml2006Reader(readStream);
					var loadedObject = XamlReader.Load(bamlReader);

					if (loadedObject is ResourceDictionary resourceDict)
						Themes.Add(name.Replace("theme", null), resourceDict);
				}
			}
		}
	}
}
