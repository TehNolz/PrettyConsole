using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PrettyConsole {
	static class LogWriter {
		public static BlockingCollection<LogMessage> Queue = new BlockingCollection<LogMessage>();
		public static void Run() {
			if (!Directory.Exists("Logs")) Directory.CreateDirectory("Logs");
			if (Directory.GetFiles("Logs", "*.log").Length > 0) CompressLogs();

			while (true) {
				LogMessage Msg = Queue.Take();
				LogTab Tab = (LogTab)Msg.Tab;

				//Write to streams
				string SafeName = GetSafeTabName(Tab);
				StreamWriter stdout = new StreamWriter("Logs\\Log_" + SafeName + "_latest.log", true);
				stdout.WriteLine(Msg.FormattedMessage);
				stdout.Close();

				if (Msg.Level >= LogLevel.WARNING) {
					StreamWriter stderr = new StreamWriter("Logs\\Log_" + SafeName + "_error.log", true);
					stderr.WriteLine(Msg.FormattedMessage);
					stderr.Close();
				}
			}
		}

		public static void CompressLogs() {
			//If the temp directory already exists (leftovers from a crash or whatever), delete it first
			if (Directory.Exists("Logs\\temp")) {
				Directory.Delete("Logs\\temp", true);
			}
			Directory.CreateDirectory("Logs\\temp");

			//Move all log files to temp
			DirectoryInfo LogsFolder = new DirectoryInfo("Logs");
			foreach(FileInfo File in LogsFolder.GetFiles()) {
				if (File.Extension != ".log") continue;
				File.MoveTo("Logs\\temp\\" + File.Name);
			}

			//Compress
			string Timestamp = LogsFolder.GetFileSystemInfos().OrderBy(fi => fi.CreationTime).First().CreationTime.ToString("yyyy-MM-dd_HH-mm");
			int FileCount = Directory.GetFiles("Logs", "Log_" + Timestamp + "_*.zip").Length;
			ZipFile.CreateFromDirectory("Logs\\temp", "Logs\\Log_" + Timestamp + "_" + FileCount + ".zip");

			Directory.Delete("Logs\\temp", true);
		}

		public static string GetSafeTabName(LogTab Tab) => new Regex("[\\<>:\"/\\|?*]").Replace(Tab.Name, "_");
	}
}
