using Evergine.Bindings.Vulkan;
using SDL2;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	public struct GlobalUbo 
	{
		public Matrix4x4 projectionView = Matrix4x4.Identity;
		public Vector3 ligthDiraction =Vector3.Normalize(new Vector3(1.0f, -3.0f, -1.0f));
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
			BufferT[] uboBuffers = new BufferT[SwapChainT.MAX_FRAMES_IN_FLIGHT];
			for (int i = 0; i < uboBuffers.Length; i++)
			{
				uboBuffers[i] = new BufferT(ref Device,
				(ulong)Marshal.SizeOf<GlobalUbo>(),
				1,
				VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT,
				VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT);

				uboBuffers[i].Map();
			}

			SimpleRenderSystemT simpleRenderSystem = new(ref Device, Renderer.GetSwapchainRenderPass);
			CameraT camera = new();
			camera.SetViewTarget(new Vector3(-1.0f, -2.0f, 2.0f), new Vector3(0.0f, 0.0f, 2.5f), null);

			GameObjectT viewerObject = new();
			KeyboardMovementController cameraController = new();

			//long currentTime = DateTime.Now.Ticks;
			float lastTime = 0;

			Stopwatch sw = new();
			sw.Start();

			bool run = true;
			while (run)
			{
				//long newTime = DateTime.Now.Ticks;
				//float frameTime = (float)(TimeSpan.FromTicks(newTime).TotalSeconds - TimeSpan.FromTicks(currentTime).TotalSeconds);
				//currentTime = newTime;
				float time = sw.ElapsedTicks;
				float frameTime = (time - lastTime) / 10000000.0f;
				lastTime = time;

				if (SDL.SDL_PollEvent(out SDL.SDL_Event test_event) != 0)
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
									//Trace.WriteLine(SDL.SDL_Keycode.SDLK_LEFT);
									//Trace.WriteLine(test_event.key.repeat);
									//Frame--;
									break;
								}
							case SDL.SDL_Keycode.SDLK_RIGHT:
								//Trace.WriteLine(SDL.SDL_Keycode.SDLK_RIGHT);
								//Frame++;
								break;
							default:
								break;
						}

						//cameraController.MovePlaneXZ(ref test_event, frameTime, ref viewerObject);
						//cameraController.MovePlaneXZ2(ref test_event, ref viewerObject);
					}
					
					cameraController.MovePlaneXZ2(ref test_event, ref viewerObject);
				}

				cameraController.MovePlaneXZ3(frameTime, ref viewerObject);
				camera.SetViewYXZ(viewerObject.Transform.Translation, viewerObject.Transform.Rotation);
				
				float aspect = Renderer.GetAspectRatio;
				camera.SetPerspectiveProjection((MathF.PI * 5) / 18, aspect, 0.1f, 10.0f);

				VkCommandBuffer? commandBuffer = Renderer.BeginFrame();
				if (commandBuffer != null)
				{
					int frameIndex = Renderer.GetFrameIndex;
					VkCommandBuffer cB = (VkCommandBuffer)commandBuffer;
					FrameInfo frameInfo = new();
					frameInfo.FrameIndex = frameIndex;
					frameInfo.FrameTime = frameTime;
					frameInfo.CommandBuffer =cB;
					frameInfo.Camera = camera;

					//update
					GlobalUbo ubo = new();
					ubo.projectionView = camera.GetView * camera.GetProjection;
					uboBuffers[frameIndex].WriteToBufferU(ref ubo);
					uboBuffers[frameIndex].Flush();
					//globalUboBuffer.WriteToIndexU(ref ubo, frameIndex);
					//globalUboBuffer.FlushIndex(frameIndex);

					//render
					Renderer.BeginSwapChainRenderPass(cB);
					simpleRenderSystem.RenderGameObjects(ref frameInfo, ref GameObjects);
					Renderer.EndSwapChainRenderPass(cB);
					Renderer.EndFrame();
				}
				
			}
			foreach (BufferT item in uboBuffers)
			{
				item.DestroyBuffer();
			}

			sw.Stop();
			VulkanNative.vkDeviceWaitIdle(Device.Device);
			simpleRenderSystem.DestroySRS();
			Destroy();
		}

		private void LoadGameObjects() 
		{
			ModelT model = ModelT.CreateModelFromFile(ref Device, Program.Directory + @"\TestVulkan\models\flat_vase.obj");

			GameObjectT flatVase = new();
			flatVase.Model = model;

			flatVase.Transform.Translation = new Vector3(-0.5f, 0.5f, 2.5f);
			flatVase.Transform.Scale = new Vector3(3f, 1.5f, 3f);

			GameObjects.Add(flatVase);

			model = ModelT.CreateModelFromFile(ref Device, Program.Directory + @"\TestVulkan\models\smooth_vase.obj");

			GameObjectT smoothVase = new();
			smoothVase.Model = model;

			smoothVase.Transform.Translation = new Vector3(0.5f, 0.5f, 2.5f);
			smoothVase.Transform.Scale = new Vector3(3f, 1.5f, 3f);

			GameObjects.Add(smoothVase);
		}

		unsafe public void Destroy() 
		{
			Renderer.DestroyRenderer();
			Device.DestroyDebugMessenger();
			Window.DestroyWindow();
		}
	}
}
