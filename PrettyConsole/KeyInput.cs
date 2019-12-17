using System;

namespace PrettyConsole {
	static class KeyInput {
		internal static void Run() {
			while (true) {
				ConsoleThread.CommandQueue.Enqueue(new KeyInputCommand(Console.ReadKey(true), ConsoleThread.CurrentTab));
			}
		}
	}

	internal class KeyInputCommand : Command {
		public ConsoleKeyInfo Key;
		internal KeyInputCommand(ConsoleKeyInfo Key, ConsoleTab Tab) : base(Tab) => this.Key = Key;

		internal override void Execute() {
			switch (this.Key.Key) {
				case ConsoleKey.LeftArrow:
					if (Tab.AllowArrowTabSwitch) {
						ConsoleThread.SwitchTab(true);
					}
					break;
				case ConsoleKey.RightArrow:
					if (Tab.AllowArrowTabSwitch) {
						ConsoleThread.SwitchTab();
					}
					break;

				case ConsoleKey.UpArrow:
					break;
				case ConsoleKey.DownArrow:
					break;
			}
		}
	}
}
