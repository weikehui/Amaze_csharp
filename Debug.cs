using System;

namespace Amaze
{
	public static class Debug
	{
		public static void Log (string log)
		{
#if DEBUG
			Console.WriteLine (log);
#endif
		}

		public static void Log (object obj)
		{
#if DEBUG
			Console.WriteLine (obj);
#endif
		}
	}
}