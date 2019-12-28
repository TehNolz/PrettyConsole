using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PrettyConsole {
	/// <summary>
	/// A MonitorTab, which monitors various values within your program.
	/// </summary>
	public class MonitorTab : ConsoleTab {
		/// <summary>
		/// The list of value watchers.
		/// </summary>
		public readonly ConcurrentDictionary<string, Watcher> WatcherList = new ConcurrentDictionary<string, Watcher>();
		/// <summary>
		/// Create a new MonitorTab, which lets you monitor certain values within your program.
		/// </summary>
		public MonitorTab(string Name, bool Debug = false) : base(Name, Debug) {}

		/// <summary>
		/// Get the lines that this monitor tab should show right now.
		/// </summary>
		/// <param name="AllowedLines">The amount of lines this tab is allowed to return</param>
		/// <returns></returns>
		public override List<string> Draw(int AllowedLines) {
			//Sort watchers alphabetically
			List<string> WatcherNames = new List<string>(WatcherList.Keys);
			WatcherNames.Sort();

			//Get a list of all watcher output
			List<string> Values = new List<string>();
			for (int i = 0; i < (AllowedLines * 2); i++) {
				if (WatcherNames.Count == 0) break;
				Values.Add(WatcherList[WatcherNames[0]].ConstructLine(Console.WindowWidth / 2 - 2));
				WatcherNames.RemoveAt(0);
			}

			//Add empty entries if necessary
			int EmptyLineCount = (AllowedLines * 2) - Values.Count;
			for (int i = 0; i < EmptyLineCount; i++) {
				Values.Add(new string(' ', Console.WindowWidth / 2 - 2));
			}

			//Format the values into a proper menu
			List<string> Lines = new List<string>();
			for (int i = 0; i < Values.Count; i++) {
				if (i % 2 == 1) continue;
				Lines.Add(Values[i] + "│" + Values[i + 1]);
			}

			return Lines;
		}

		/// <summary>
		/// Creates a new NumWatcher, which tracks a numeric value.
		/// The watcher is automatically tied to this tab.
		/// </summary>
		/// <param name="ValueName">The name the value should have when shown in the tab.</param>
		/// <param name="ShowCurrent">Whether to last known value.</param>
		/// <param name="ShowMin">Whether to show the least known value</param>
		/// <param name="ShowAverage">Whether to show the average value</param>
		/// <param name="ShowMax">Whether to show the highest known value</param>
		/// <returns></returns>
		public NumWatcher CreateNumWatcher(
			string ValueName,
			bool ShowCurrent = true,
			bool ShowMin = false,
			bool ShowAverage = false,
			bool ShowMax = false
		) => new NumWatcher(
			this,
			ValueName,
			ShowCurrent,
			ShowMin,
			ShowAverage,
			ShowMax
		);
	}

	/// <summary>
	/// Abstract class for watchers.
	/// </summary>
	public abstract class Watcher {
		/// <summary>
		/// Whether to last known value
		/// </summary>
		public bool ShowCurrent { get; }
		/// <summary>
		/// The name the value should have when shown in the tab
		/// </summary>
		public string ValueName { get; }
		/// <summary>
		/// The monitor tab this watcher belongs to.
		/// </summary>
		public MonitorTab Tab { get; }

		/// <summary>
		/// Creates a new Watcher, which tracks a value.
		/// </summary>
		/// <param name="Tab">The tab this watcher will be associated with.</param>
		/// <param name="ValueName">The name the value should have when shown in the tab.</param>
		/// <param name="ShowCurrent">Whether to last known value.</param>
		public Watcher(MonitorTab Tab, string ValueName, bool ShowCurrent = true) {
			this.Tab = Tab;
			if(!Tab.WatcherList.TryAdd(ValueName, this)) throw new Exception("TryAdd failed");
			this.ValueName = ValueName;
			this.ShowCurrent = ShowCurrent;
		}

		/// <summary>
		/// Returns a string representing this watcher.
		/// The function should respect ShowCurrent.
		/// </summary>
		/// <param name="AllowedWidth">The maximum width of the string</param>
		/// <returns></returns>
		public abstract string ConstructLine(int AllowedWidth);
	}

	/// <summary>
	/// Tracks a number for change
	/// </summary>
	public class NumWatcher : Watcher {
		/// <summary>
		/// Whether to show the least known value
		/// </summary>
		public bool ShowMin { get; }
		/// <summary>
		/// Whether to show the average value
		/// </summary>
		public bool ShowAverage { get; }
		/// <summary>
		/// Whether to show the highest known value
		/// </summary>
		public bool ShowMax { get; }

		/// <summary>
		/// Creates a new NumWatcher, which tracks a numeric value.
		/// </summary>
		/// <param name="Tab">The tab this watcher will be associated with.</param>
		/// <param name="ValueName">The name the value should have when shown in the tab.</param>
		/// <param name="ShowCurrent">Whether to last known value.</param>
		/// <param name="ShowMin">Whether to show the least known value</param>
		/// <param name="ShowAverage">Whether to show the average value</param>
		/// <param name="ShowMax">Whether to show the highest known value</param>
		public NumWatcher(MonitorTab Tab, string ValueName, bool ShowCurrent = true, bool ShowMin = false, bool ShowAverage = false, bool ShowMax = false) : base(Tab, ValueName, ShowCurrent) {
			this.ShowMin = ShowMin;
			this.ShowAverage = ShowAverage;
			this.ShowMax = ShowMax;
		}

		/// <summary>
		/// The value history
		/// </summary>
		private readonly BlockingCollection<decimal> ValueHistory = new BlockingCollection<decimal>();

		/// <summary>
		/// Update this watcher, adding a new value to its history.
		/// </summary>
		/// <param name="Value"></param>
		public void Update(decimal Value) => ValueHistory.Add(Value);

		/// <summary>
		/// Returns the least known number.
		/// </summary>
		/// <returns></returns>
		public decimal Min() => ValueHistory.Count > 0 ? ValueHistory.Min() : default;
		/// <summary>
		/// Returns the highest known number.
		/// </summary>
		/// <returns></returns>
		public decimal Max() => ValueHistory.Count > 0 ? ValueHistory.Max() : default;
		/// <summary>
		/// Returns the average of all known numbers.
		/// </summary>
		/// <returns></returns>
		public decimal Average() => ValueHistory.Count > 0 ? Math.Round(ValueHistory.Average(), 2) : default;
		/// <summary>
		/// Returns a string representing this watcher.
		/// The function should respect ShowCurrent.
		/// </summary>
		/// <param name="AllowedWidth">The maximum width of the string</param>
		/// <returns></returns>
		public override string ConstructLine(int AllowedWidth) {
			string Value =
				(ShowCurrent ? "Current: " + ValueHistory.LastOrDefault() + " " : "") +
				(ShowMin ? "Min: " + Min() + " " : "") +
				(ShowAverage ? "Avg: " + Average() + " " : "") +
				(ShowMax ? "Max: " + Max() + " " : "");
			int Size = ValueName.Length + Value.Length;
			if (Size > AllowedWidth) {
				return new string(' ', AllowedWidth);
			}
			return ValueName + new string(' ', AllowedWidth - Size) + Value;
		}
	}
}
