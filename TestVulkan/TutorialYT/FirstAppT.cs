using Evergine.Bindings.Vulkan;
using SDL2;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestVulkan
{
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
			CameraT camera = new();
			//camera.SetViewDirection(Vector3.Zero, new Vector3(0.5f, 0.0f, 1.0f), null);
			camera.SetViewTarget(new Vector3(-1.0f, -2.0f, 2.0f), new Vector3(0.0f, 0.0f, 2.5f), null);

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

				float aspect = Renderer.GetAspectRatio;
				//camera.SetOrthographicProjection(-aspect, aspect, -1.0f, 1.0f, -1.0f, 1.0f);
				//
				camera.SetPerspectiveProjection((MathF.PI * 5)/18, aspect, 0.1f, 10.0f);
				
				VkCommandBuffer? commandBuffer = Renderer.BeginFrame();
				if (commandBuffer != null)
				{
					VkCommandBuffer cB = (VkCommandBuffer)commandBuffer;
					Renderer.BeginSwapChainRenderPass(cB);
					simpleRenderSystem.RenderGameObjects(cB, ref GameObjects, ref camera);
					Renderer.EndSwapChainRenderPass(cB);
					Renderer.EndFrame();
				}
			}

			VulkanNative.vkDeviceWaitIdle(Device.Device);
			simpleRenderSystem.DestroySRS();
			Destroy();
		}
		/*
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
		}*/

		private ModelT CreateCubeModel(ref DeviceT device , Vector3 offset)
		{
			List<VertexT> vertices = new()
			{
				// left face (white)
				new VertexT() 
				{ 
					Position = new Vector3(-0.5f, -0.5f, -0.5f),
					Color = new Vector3(0.9f, 0.9f, 0.9f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, 0.5f, 0.5f),
					Color = new Vector3(0.9f, 0.9f, 0.9f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, -0.5f, 0.5f),
					Color = new Vector3(0.9f, 0.9f, 0.9f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, -0.5f, -0.5f),
					Color = new Vector3(0.9f, 0.9f, 0.9f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, 0.5f, -0.5f),
					Color = new Vector3(0.9f, 0.9f, 0.9f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, 0.5f, 0.5f),
					Color = new Vector3(0.9f, 0.9f, 0.9f)
				},
				// right face (yellow)
				new VertexT()
				{
					Position = new Vector3(0.5f, -0.5f, -0.5f),
					Color = new Vector3(0.8f, 0.8f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, 0.5f, 0.5f),
					Color = new Vector3(0.8f, 0.8f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, -0.5f, 0.5f),
					Color = new Vector3(0.8f, 0.8f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, -0.5f, -0.5f),
					Color = new Vector3(0.8f, 0.8f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, 0.5f, -0.5f),
					Color = new Vector3(0.8f, 0.8f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, 0.5f, 0.5f),
					Color = new Vector3(0.8f, 0.8f, 0.1f)
				},
				// top face (orange, remember y axis points down)
				new VertexT()
				{
					Position = new Vector3(-0.5f, -0.5f, -0.5f),
					Color = new Vector3(0.9f, 0.6f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, -0.5f, 0.5f),
					Color = new Vector3(0.9f, 0.6f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, -0.5f, 0.5f),
					Color = new Vector3(0.9f, 0.6f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, -0.5f, -0.5f),
					Color = new Vector3(0.9f, 0.6f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, -0.5f, -0.5f),
					Color = new Vector3(0.9f, 0.6f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, -0.5f, 0.5f),
					Color = new Vector3(0.9f, 0.6f, 0.1f)
				},
				// bottom face (red)
				new VertexT()
				{
					Position = new Vector3(-0.5f, 0.5f, -0.5f),
					Color = new Vector3(0.8f, 0.1f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, 0.5f, 0.5f),
					Color = new Vector3(0.8f, 0.1f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, 0.5f, 0.5f),
					Color = new Vector3(0.8f, 0.1f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, 0.5f, -0.5f),
					Color = new Vector3(0.8f, 0.1f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, 0.5f, -0.5f),
					Color = new Vector3(0.8f, 0.1f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, 0.5f, 0.5f),
					Color = new Vector3(0.8f, 0.1f, 0.1f)
				},
				// nose face (blue)
				new VertexT()
				{
					Position = new Vector3(-0.5f, -0.5f, 0.5f),
					Color = new Vector3(0.1f, 0.1f, 0.8f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, 0.5f, 0.5f),
					Color = new Vector3(0.1f, 0.1f, 0.8f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, 0.5f, 0.5f),
					Color = new Vector3(0.1f, 0.1f, 0.8f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, -0.5f, 0.5f),
					Color = new Vector3(0.1f, 0.1f, 0.8f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, -0.5f, 0.5f),
					Color = new Vector3(0.1f, 0.1f, 0.8f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, 0.5f, 0.5f),
					Color = new Vector3(0.1f, 0.1f, 0.8f)
				},
				// tail face (green)
				new VertexT()
				{
					Position = new Vector3(-0.5f, -0.5f, -0.5f),
					Color = new Vector3(0.1f, 0.8f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, 0.5f, -0.5f),
					Color = new Vector3(0.1f, 0.8f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, 0.5f, -0.5f),
					Color = new Vector3(0.1f, 0.8f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(-0.5f, -0.5f, -0.5f),
					Color = new Vector3(0.1f, 0.8f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, -0.5f, -0.5f),
					Color = new Vector3(0.1f, 0.8f, 0.1f)
				},
				new VertexT()
				{
					Position = new Vector3(0.5f, 0.5f, -0.5f),
					Color = new Vector3(0.1f, 0.8f, 0.1f)
				}
			};

			VertexT[] arr = vertices.ToArray();

			for (int i = 0; i < arr.Length; i++)
			{
				arr[i].Position += offset;
			}

			ModelT Model = new(ref device, ref arr);

			return Model;
		}

		private void LoadGameObjects() 
		{
			ModelT model = CreateCubeModel(ref Device, new Vector3(0.0f, 0.0f, 0.0f));

			GameObjectT cube = new();
			cube.Model = model;

			cube.Transform.Translation = new Vector3(0.0f, 0.0f, 2.5f);
			cube.Transform.Scale = new Vector3(0.5f, 0.5f, 0.5f);

			GameObjects.Add(cube);
		}

		unsafe public void Destroy() 
		{
			Renderer.DestroyRenderer();
			Device.DestroyDebugMessenger();
			Window.DestroyWindow();
		}
	}
}
