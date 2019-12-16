using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PrettyConsole {
	internal class ConsoleThread {
		internal static bool Running = false;
		internal static Thread CThread;
		internal static Thread KeyInputThread;

		internal static ConsoleTab CurrentTab;
		internal static ConcurrentDictionary<string, ConsoleTab> TabList = new ConcurrentDictionary<string, ConsoleTab>();

		internal static void Start() {
			if (Running) return;

			Running = true;

			//Start threads
			CThread = new Thread(Run);
			CThread.Start();
			KeyInputThread = new Thread(KeyInput.Run);
			KeyInputThread.Start();
		}

		internal static readonly ConcurrentQueue<Command> CommandQueue = new ConcurrentQueue<Command>();

		private static void Run() {
			while (TabList.Count == 0);
			CurrentTab = TabList[TabList.Keys.ToList()[0]];
			
			int Height = Console.WindowHeight;
			if (Height < 10) Console.WindowHeight = 10;
			int Width = Console.WindowWidth;
			if (Width < 10) Console.WindowWidth = 10;

			while (true) {
				Console.CursorVisible = false;
				//Clear screen on resize
				if (Height != Console.WindowHeight || Width != Console.WindowWidth) {
					Height = Console.WindowHeight;
					Width = Console.WindowWidth;
					if (Height < 10) Console.WindowHeight = 10;
					if (Width < 10) Console.WindowWidth = 10;
					Console.Clear();
				}

				//Execute all commands until the queue is empty.
				while (!CommandQueue.IsEmpty) {
					if (CommandQueue.TryDequeue(out Command CMD)) {
						CMD.Execute();
					};
				}

				//Draw header
				WriteColored("═══", ConsoleColor.DarkBlue);
				WriteColored(CurrentTab.Name, ConsoleColor.Yellow, ConsoleColor.Black);
				WriteColored(new string('═', Console.BufferWidth - CurrentTab.Name.Length - 3), ConsoleColor.DarkBlue);

				//Draw the lines this tab wants to show.
				List<string> ToDraw = CurrentTab.Draw(Console.WindowHeight - 4);
				foreach(string Line in ToDraw) {
					Console.WriteLine(Line + new string(' ', Math.Max(Console.BufferWidth-Line.Length, 0)));
				}
				//Set cursor to footer position if necessary
				if (ToDraw.Count < Console.WindowHeight - 3) Console.SetCursorPosition(0, Console.WindowHeight - 3);

				//Construct footer
				WriteColored("╔" + new string('═', Console.BufferWidth-2) + "╗", ConsoleColor.DarkBlue);
				List<string> Tabs = TabList.Keys.ToList();
				Tabs.Sort();
				WriteColored("║", ConsoleColor.DarkBlue);
				foreach (string Tab in Tabs) {
					bool isCurrentTab = CurrentTab.Name == Tab;
					WriteColored(" "+Tab+" ", isCurrentTab ? ConsoleColor.Yellow : ConsoleColor.DarkBlue, isCurrentTab ? ConsoleColor.Black : ConsoleColor.White);
				}
				WriteColored(new string(' ', Math.Max(Console.BufferWidth - 1 - Console.CursorLeft, 0)) + "║", ConsoleColor.DarkBlue);
				WriteColored("╚" + new string('═', Console.BufferWidth-2) + "╝", ConsoleColor.DarkBlue);
				Console.BackgroundColor = ConsoleColor.Black;

				//Return cursor to top of the screen, wait a bit, then restart.
				Console.SetCursorPosition(0, 0);
			}
		}

		public static void WriteColored(object Text, ConsoleColor BackgroundColor = ConsoleColor.Black, ConsoleColor ForegroundColor = ConsoleColor.White) {
			ConsoleColor CurrentBackground = Console.BackgroundColor;
			ConsoleColor CurrentForeground = Console.ForegroundColor;
			Console.BackgroundColor = BackgroundColor;
			Console.ForegroundColor = ForegroundColor;
			Console.Write(Text);
			Console.BackgroundColor = CurrentBackground;
			Console.ForegroundColor = CurrentForeground;
		}

		public static void SwitchTab(bool Back = false) {
			//Pick next tab.
			List<string> Tabs = TabList.Keys.ToList();
			Tabs.Sort();
			int CurrentIndex = Tabs.IndexOf(CurrentTab.Name);
			int NewIndex = Back ? CurrentIndex - 1 : CurrentIndex + 1;
			if (NewIndex == Tabs.Count) NewIndex = 0;
			if (NewIndex == -1) NewIndex = Tabs.Count - 1;
			SwitchTab(TabList[Tabs[NewIndex]]);
		}

		public static void SwitchTab(ConsoleTab Tab) {
			CurrentTab = Tab;

			//Clear previous frame
			for (int i = 0; i < Console.WindowHeight; i++) {
				Console.WriteLine(new string(' ', Console.WindowWidth));
			}
			Console.SetCursorPosition(0, 0);
		}
	}

	public abstract class Command {
		internal ConsoleTab Tab;

		public Command(ConsoleTab Tab) => this.Tab = Tab;

		internal abstract void Execute();
	}

	public abstract class ConsoleTab {
		public readonly string Name;
		public bool AllowArrowTabSwitch { get; set; } = true;

		private int _VerticalOffset = 0;
		public virtual int VerticalOffset { get => _VerticalOffset; set => _VerticalOffset = value; }

		private int _HorizontalOffset = 0;
		public virtual int HorizontalOffset {
			get => _HorizontalOffset;
			set => _HorizontalOffset = value;
		}

		public ConsoleTab(string Name) => this.Name = Name;

		public abstract List<string> Draw(int AllowedLines);
	}
}
