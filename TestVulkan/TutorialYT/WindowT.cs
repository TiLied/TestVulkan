using Evergine.Bindings.Vulkan;
using SDL2;
using System;
using System.Diagnostics;

namespace TestVulkan
{
	public class WindowT
	{
		public IntPtr Window;

		private int Width;
		private int Height;

		private string WindowName;

		public WindowT(int w, int h, string n)
		{
			Width = w;
			Height = h;
			WindowName = n;

			InitWindow();
		}

		private void InitWindow() 
		{
			if (SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING) != 0)
			{
				Trace.TraceError("Unable to initialize SDL: " + SDL.SDL_GetError());
				throw new Exception(SDL.SDL_GetError());
			}

			SDL.SDL_VERSION(out SDL.SDL_version compiled);
			SDL.SDL_GetVersion(out SDL.SDL_version linked);
			Trace.WriteLine($"We compiled against SDL version: {compiled.major}.{compiled.minor}.{compiled.patch}");
			Trace.WriteLine($"But we are linking against SDL version: {linked.major}.{linked.minor}.{linked.patch}");

			Window = SDL.SDL_CreateWindow(WindowName, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, Width, Height, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_VULKAN);
			
			SDL.SDL_SetWindowResizable(Window, SDL.SDL_bool.SDL_TRUE);
			
		}

		unsafe public void CreateWindowSurface(VkInstance instance, out VkSurfaceKHR surface) 
		{
			ulong _handle;

			if (SDL.SDL_Vulkan_CreateSurface(Window, instance.Handle, out _handle) == SDL.SDL_bool.SDL_FALSE)
			{
				Trace.TraceError("Failed to create window surface!: " + SDL.SDL_GetError());
				throw new Exception(SDL.SDL_GetError());
			}

			surface = new VkSurfaceKHR(_handle);
		}

		public VkExtent2D GetExtent()
		{
			VkExtent2D vkExtent2D = new() 
			{ 
				height = (uint)Height,
				width = (uint)Width
			};

			return vkExtent2D;
		}

		public void DestroyWindow() 
		{
			SDL.SDL_DestroyWindow(Window);

			SDL.SDL_Quit();
		}
	}
}
