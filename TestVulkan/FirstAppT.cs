using SDL2;
using System;
using System.Diagnostics;

namespace TestVulkan
{
	public class FirstAppT
	{
		private const int WIDTH = 960;
		private const int HEIGHT = 540;

		private LiveWindowT LiveWindow = new(WIDTH, HEIGHT, "Hello Vulkan!");
		private PipelineT Pipeline = new(Program.Directory + @"\TestVulkan\shaders\Svert.spv", Program.Directory + @"\TestVulkan\shaders\Sfrag.spv");

		public FirstAppT()
		{
		}

		public void Run() 
		{
			bool run = true;
			while (run)
			{
				if (SDL.SDL_PollEvent(out SDL.SDL_Event test_event) == 1)
				{
					if (test_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
					{
						Trace.WriteLine($"Window {test_event.window.windowID} closed");
						run = false;
					}
				}
			}

			LiveWindow.DestroyWindow();
		}
	}
}
