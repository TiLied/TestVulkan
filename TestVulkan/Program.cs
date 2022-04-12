using System.Diagnostics;

namespace TestVulkan
{
	public class Program
	{
		public static string Directory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
		unsafe private static void Main()
		{
			int worker = 0;
			int io = 0;
			ThreadPool.GetAvailableThreads(out worker, out io);

			Console.WriteLine("Thread pool threads available at startup: ");
			Console.WriteLine("   Worker threads: {0:N0}", worker);
			Console.WriteLine("   Asynchronous I/O threads: {0:N0}", io);

			if (File.Exists("log.log"))
				File.Delete("log.log");

			Trace.Listeners.AddRange(new TextWriterTraceListener[] {
				new TextWriterTraceListener("log.log"),
				new TextWriterTraceListener(Console.Out)
			});
			Trace.AutoFlush = true;
			Trace.Indent();
			Trace.WriteLine("Hello World.");
			Trace.Unindent();

			string a = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
			string[] b = a.Split("\\");

			for (int i = b.Length - 1; i >= 0; i--)
			{
				if (b[i] == "TestVulkan")
				{
					string[] d = new string[i];
					for (int j = 0; j < i; j++)
					{
						d[j] = b[j];
					}
					Directory = string.Join("\\", d);
					break;
				}
			}

			Trace.WriteLine(Directory);

			//VulkanTutorial v = new();
			//v.Run();

			FirstAppT v2 = new();
			v2.Run();

			//MainVKG v3 = new();
			//v3.Run();

			Console.ReadKey();
		}
	}
}