﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

			int MinWidth = 10;
			int Height = Console.WindowHeight;
			if (Height < MinWidth) Console.WindowHeight = MinWidth;
			int Width = Console.BufferWidth;
			if (Width < MinWidth) Console.BufferWidth = MinWidth;

			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Clear();
			while (true) {
				Console.CursorVisible = false;
				//Clear screen on resize
				if (Height != Console.WindowHeight || Width != Console.BufferWidth) {
					Height = Console.WindowHeight;
					Width = Console.BufferWidth;
					Console.Clear();
				}
				if(Console.WindowHeight < 10) Console.SetWindowSize(Console.BufferWidth, 10);
				if (MinWidth > Width) Console.SetWindowSize(MinWidth, Console.WindowHeight);

				//Execute all commands until the queue is empty.
				while (!CommandQueue.IsEmpty) {
					if (CommandQueue.TryDequeue(out Command CMD)) CMD.Execute();
				}

				//Draw header
				WriteColored("═══", ConsoleColor.DarkBlue);
				WriteColored(CurrentTab.Name, ConsoleColor.Yellow, ConsoleColor.Black);
				WriteColored(new string('═', Console.BufferWidth - CurrentTab.Name.Length - 3), ConsoleColor.DarkBlue);

				//Draw the lines this tab wants to show.
				int Allowed = Console.WindowHeight - 4;
				List<string> ToDraw = CurrentTab.Draw(Allowed);
				if (ToDraw.Count > Allowed) throw new IndexOutOfRangeException("Tab attempted to draw more lines than allowed");
				foreach(string Line in ToDraw) {
					Console.WriteLine(Line + new string(' ', Math.Max(Console.BufferWidth-Line.Length-1, 0)));
				}

				//Set cursor to footer position if necessary
				if (ToDraw.Count < Console.WindowHeight - 3) Console.SetCursorPosition(0, Console.WindowHeight - 3);

				//Construct footer
				WriteColored("╔" + new string('═', Console.BufferWidth - 3) + "╗", ConsoleColor.DarkBlue);
				Console.Write(' ');
				List<string> Tabs = TabList.Keys.ToList();
				Tabs.Sort();
				WriteColored("║", ConsoleColor.DarkBlue);
				int NewMinWidth = 4;
				foreach (string Tab in Tabs) {
					bool isCurrentTab = CurrentTab.Name == Tab;
					NewMinWidth += Tab.Length + 2;
					WriteColored(" "+Tab+" ", isCurrentTab ? ConsoleColor.Yellow : ConsoleColor.DarkBlue, isCurrentTab ? ConsoleColor.Black : ConsoleColor.White);
				}
				MinWidth = NewMinWidth;
				WriteColored(new string(' ', Math.Max(Console.BufferWidth - 2 - Console.CursorLeft, 0)) + "║", ConsoleColor.DarkBlue);
				Console.Write(' ');
				WriteColored("╚" + new string('═', Console.BufferWidth - 3) + "╝", ConsoleColor.DarkBlue);

				//Return cursor to top of the screen, then restart.
				Console.SetCursorPosition(0, 0);
				Thread.Sleep(1);
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
				Console.WriteLine(new string(' ', Console.BufferWidth-1));
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
		public bool Debug { get; }

		/// <summary>
		/// Whether this tab allows the user to switch tabs using the arrow keys.
		/// Should never be permanently set to false.
		/// </summary>
		public bool AllowArrowTabSwitch { get; set; } = true;

		public string Name { get; }

		/// <summary>
		/// Creates a new console tab.
		/// Automatically starts the console thread if it isn't already running.
		/// </summary>
		/// <param name="Name"></param>
		public ConsoleTab(string Name, bool Debug = false) {
			this.Debug = Debug;
			this.Name = Name;

			if (!Debug && !ConsoleThread.Running) ConsoleThread.Start();
			if (ConsoleThread.TabList.ContainsKey(Name)) throw new ArgumentException("A tab already exists with this name");
			ConsoleThread.TabList[Name] = this;
		}

		/// <summary>
		/// Returns the lines that the tab should draw on the current frame.
		/// </summary>
		/// <param name="AllowedLines">The amount of lines the tab is allowed to draw.</param>
		/// <returns></returns>
		public abstract List<string> Draw(int AllowedLines);
	}
}
