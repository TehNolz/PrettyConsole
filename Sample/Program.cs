using PrettyConsole;
using System;
using System.Threading;

namespace Test {
	class Program {
		static void Main() {
			new Thread(Thread1).Start();
			new Thread(Thread2).Start();
			new Thread(Thread3).Start();
		}

		static void Thread1() {
			LogTab Tab = new LogTab("InfoTab");
			Logger Log = Tab.GetLogger();
			Thread.Sleep(1000);
			int i = 0;
			while (true) {
				Log.Info(i);
				Thread.Sleep(1000);
				i++;
			}
		}

		static void Thread2() {
			LogTab Tab = new LogTab("DebugTab");
			Logger Log = Tab.GetLogger();
			Thread.Sleep(1000);
			int i = 0;
			while (true) {
				Log.Debug(i);
				i++;
				Thread.Sleep(200);
			}
		}

		static void Thread3() {
			LogTab Tab = new LogTab("WarningTabLol");
			Logger Log = Tab.GetLogger();
			Thread.Sleep(1000);
			int i = 0;
			while (true) {
				Log.Warning(i);
				i++;
				Thread.Sleep(20);
			}
		}
	}
}
