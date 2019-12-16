using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PrettyConsole {

	public class LogTab : ConsoleTab {
		internal readonly BlockingCollection<string> MessageBuffer = new BlockingCollection<string>();

		public LogTab(string Name) : base(Name) {
			if (!ConsoleThread.Running) ConsoleThread.Start();

			//Check if a tab already exists with this name
			foreach(ConsoleTab Tab in ConsoleThread.TabList.Values) {
				if (Tab.Name == Name) throw new ArgumentException("Tab already exists with this name");
			}

			ConsoleThread.TabList[Name] = this;
		}

		public override List<string> Draw(int AllowedLines) => MessageBuffer.Skip(Math.Max(0, MessageBuffer.Count() - AllowedLines)).ToList();

		public Logger GetLogger() => new Logger(this);
	}

	public class Logger {
		public LogTab Tab;
		public void Debug(object Message) => Log(LogLevel.DEBUG, Message);
		public void Info(object Message) => Log(LogLevel.INFO, Message);
		public void Warning(object Message) => Log(LogLevel.WARNING, Message);
		public void Error(object Message) => Log(LogLevel.ERROR, Message);
		public void Fatal(object Message) => Log(LogLevel.FATAL, Message);
		private void Log(LogLevel Level, object Message) => ConsoleThread.CommandQueue.Enqueue(new LogMessage(Tab, Level, Message, DateTime.Now));

		public Logger(LogTab Tab) => this.Tab = Tab;
	}

	internal class LogMessage : Command {
		internal string FormattedMessage;
		public LogMessage(LogTab Tab, LogLevel Level, object Message, DateTime Timestamp) : base(Tab) {
			this.Level = Level;
			this.Message = Message.ToString();
			this.Timestamp = Timestamp;
			this.FormattedMessage = string.Format("{0} [{1}] {2}", Timestamp.ToString("HH:mm:ss fff"), Level.ToString(), Message);
		}
		internal LogLevel Level { get; set; }
		internal string Message { get; set; }
		internal DateTime Timestamp { get; set; }

		internal override void Execute() => ((LogTab)Tab).MessageBuffer.Add(FormattedMessage);
	}

	internal enum LogLevel {
		DEBUG,
		INFO,
		WARNING,
		ERROR,
		FATAL
	}
}
