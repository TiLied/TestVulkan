using Evergine.Bindings.Vulkan;
using SDL2;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	[StructLayout(LayoutKind.Explicit)]
	public struct SimplePushConstantData 
	{
		[FieldOffset(0)]
		public Matrix4x4 Transform = Matrix4x4.Identity;
		[FieldOffset(64)]
		public Vector2 Offset;
		[FieldOffset(64+16)]
		public Vector3 Color;

	}

	public class FirstAppT
	{
		private const int WIDTH = 960;
		private const int HEIGHT = 540;

		private bool Minimized = false;

		private WindowT Window;
		private DeviceT Device;
		private List<GameObjectT> GameObjects = new();
		private RendererT Renderer;

		public FirstAppT()
		{
			Window = new(WIDTH, HEIGHT, "Hello Vulkan!");
			Device = new(ref Window);
			Renderer = new(ref Window, ref Device);

			LoadGameObjects();
		}

		public void Run() 
		{
			SimpleRenderSystemT simpleRenderSystem = new(ref Device, Renderer.GetSwapchainRenderPass);

			bool run = true;
			while (run)
			{
				if (SDL.SDL_PollEvent(out SDL.SDL_Event test_event) == 1)
				{
					if (test_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE || test_event.type == SDL.SDL_EventType.SDL_QUIT)
					{
						Trace.WriteLine($"Window {test_event.window.windowID} closed");
						run = false;
					}
					if (test_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED)
					{
						Minimized = true;
						//Trace.WriteLine($"SDL_WINDOWEVENT_MINIMIZED {Minimazed}");
					}
					if (test_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED)
					{
						Minimized = false;
						//Trace.WriteLine($"SDL_WINDOWEVENT_MINIMIZED {Minimazed}");
					}


					//test! nice!
					if (test_event.type == SDL.SDL_EventType.SDL_KEYDOWN)
					{
						switch (test_event.key.keysym.sym)
						{
							case SDL.SDL_Keycode.SDLK_LEFT:
								{
									if (test_event.key.repeat == 1)
										break;
									Trace.WriteLine(SDL.SDL_Keycode.SDLK_LEFT);
									Trace.WriteLine(test_event.key.repeat);
									//Frame--;
									break;
								}
							case SDL.SDL_Keycode.SDLK_RIGHT:
								Trace.WriteLine(SDL.SDL_Keycode.SDLK_RIGHT);
								//Frame++;
								break;
							default:
								break;
						}
					}
				}

				VkCommandBuffer? commandBuffer = Renderer.BeginFrame();
				if (commandBuffer != null)
				{
					VkCommandBuffer cB = (VkCommandBuffer)commandBuffer;
					Renderer.BeginSwapChainRenderPass(cB);
					simpleRenderSystem.RenderGameObjects(cB, ref GameObjects);
					Renderer.EndSwapChainRenderPass(cB);
					Renderer.EndFrame();
				}
			}

			VulkanNative.vkDeviceWaitIdle(Device.Device);
			simpleRenderSystem.DestroySRS();
			Destroy();
		}

		private void Sierpinski(ref List<VertexT> vertices, int depth, Vector2 left, Vector2 right, Vector2 top)
		{
			if (depth <= 0)
			{
				vertices.Add(new VertexT() { Position = top });
				vertices.Add(new VertexT() { Position = right });
				vertices.Add(new VertexT() { Position = left });
			}
			else
			{
				Vector2 leftTop = 0.5f * (left + top);
				Vector2 rightTop = 0.5f * (right + top);
				Vector2 leftRight = 0.5f * (left + right);
				Sierpinski(ref vertices, depth - 1, left, leftRight, leftTop);
				Sierpinski(ref vertices, depth - 1, leftRight, right, rightTop);
				Sierpinski(ref vertices, depth - 1, leftTop, rightTop, top);
			}
		}

		private void LoadGameObjects() 
		{
			List<VertexT> vertices = new();

			vertices.Add(new VertexT() 
			{ 
				Position = new Vector2(0.0f,-0.5f),
				Color = new Vector3(1.0f,0.0f,0.0f)
			});
			vertices.Add(new VertexT() 
			{ 
				Position = new Vector2(0.5f, 0.5f),
				Color = new Vector3(0.0f, 1.0f, 0.0f)
			});
			vertices.Add(new VertexT() 
			{ 
				Position = new Vector2(-0.5f, 0.5f),
				Color = new Vector3(0.0f, 0.0f, 1.0f)
			});

			//Sierpinski(ref vertices, 5, new Vector2(-0.5f, 0.5f), new Vector2( 0.5f, 0.5f), new Vector2(0.0f, -0.5f));

			VertexT[] arrVertices = vertices.ToArray();

			ModelT Model = new(ref Device, ref arrVertices);

			GameObjectT triangle = new();
			triangle.Model = Model;
			triangle.Color = new Vector3(0.1f, 0.8f, 0.1f);
			triangle.Transform2D.Translation.X = 0.2f;
			triangle.Transform2D.Scale = new Vector2(2.0f, 0.5f);
			triangle.Transform2D.Rotation = 0.25f * (MathF.PI * 2);

			GameObjects.Add(triangle);
		}

		unsafe public void Destroy() 
		{
			Renderer.DestroyRenderer();
			Device.DestroyDebugMessenger();
			Window.DestroyWindow();
		}
	}
}
