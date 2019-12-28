using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace PrettyConsole {

	public class LogTab : ConsoleTab {
		private static readonly Thread WriterThread = new Thread(LogWriter.Run);
		public DateTime LastMsg = DateTime.Now;

		internal readonly BlockingCollection<string> MessageBuffer = new BlockingCollection<string>();

		/// <summary>
		/// Creates a new log tab in the console.
		/// </summary>
		/// <param name="Name">The name this tab should have.</param>
		/// <param name="Path">The file that messages will be logged to. 
		/// If the file already exists, the old file will be archived and a new one will be created. 
		/// Set to null to disable log saving for this tab.</param>
		/// <param name="Debug">Prevents the ConsoleThread from starting, as it breaks unit tests</param>
		public LogTab(string Name, bool Debug = false) : base(Name, Debug) {
			if (string.IsNullOrEmpty(Name)) throw new ArgumentException("message", nameof(Name));

			//Start the logwriter if necessary, and if it isn't already running
			if(!Debug && WriterThread.ThreadState == System.Threading.ThreadState.Unstarted) {
				WriterThread.Start();
			}
		}

		/// <summary>
		/// Returns the messages that this tab will draw in the current frame.
		/// </summary>
		/// <param name="AllowedLines">The amount of lines that this tab is allowed to draw.</param>
		/// <returns></returns>
		public override List<string> Draw(int AllowedLines) {
			List<string> LastMessages = new List<string>(MessageBuffer.Skip(Math.Max(0, MessageBuffer.Count() - AllowedLines)));
			List<int> Lengths = new List<int>();

			foreach(string Msg in LastMessages) {
				Lengths.Add((Msg.Length / Console.BufferWidth) + 1);
			}
			int LinesRequired = Lengths.Sum();
			while (LinesRequired > AllowedLines && LastMessages.Count > 0) {
				Lengths.RemoveAt(0);
				LastMessages.RemoveAt(0);
				LinesRequired = Lengths.Sum();
			}
			
			return LastMessages;
		}

		/// <summary>
		/// Create a new logger for this tab.
		/// </summary>
		/// <returns></returns>
		public Logger GetLogger() => new Logger(this);
	}

	public class Logger {
		public LogTab Tab { get; set; }

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

		/// <summary>
		/// Send a formatted log message to the tab message buffer.
		/// Also sends itself to the log writer if necessary.
		/// </summary>
		internal override void Execute() {
			((LogTab)Tab).MessageBuffer.Add(FormattedMessage);
			LogWriter.Queue.Add(this);
		}
	}

	internal enum LogLevel {
		DEBUG,
		INFO,
		WARNING,
		ERROR,
		FATAL
	}
}
