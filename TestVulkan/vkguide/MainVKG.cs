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

			engine.Init(ref engine);

			engine.Run();

			engine.Cleanup();
		}
	}
}
