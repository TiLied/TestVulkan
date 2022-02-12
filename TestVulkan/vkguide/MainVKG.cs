using System;
namespace TestVulkan
{
	public class MainVKG
	{
		public MainVKG()
		{

		}

		public void Run()
		{
			EngineVKG engine = new();

			engine.Init();

			engine.Run();

			engine.Cleanup();
		}
	}
}
