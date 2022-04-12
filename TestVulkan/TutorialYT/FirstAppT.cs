using Evergine.Bindings.Vulkan;
using SDL2;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	public class FirstAppT
	{
		public const int MAX_LIGHTS = 10;

		private const int WIDTH = 960;
		private const int HEIGHT = 540;

		private bool Minimized = false;

		private WindowT Window;
		private DeviceT Device;
		private RendererT Renderer;
		private DescriptorPoolT GlobalPool;

		public FirstAppT()
		{
			Window = new(WIDTH, HEIGHT, "Hello Vulkan!");
			Device = new(ref Window);
			Renderer = new(ref Window, ref Device);
			GlobalPool = new DescriptorPoolT.Builder(ref Device)
				.SetMaxSets(SwapChainT.MAX_FRAMES_IN_FLIGHT)
				.AddPoolSize( VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, SwapChainT.MAX_FRAMES_IN_FLIGHT)
				.Build();


			LoadGameObjects();
		}

		unsafe public void Run() 
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

			DescriptorSetLayoutT globalSetLayout = new DescriptorSetLayoutT.Builder(ref Device)
				.AddBinding(0, VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT)
				.Build();

			VkDescriptorSet[] globalDescriptorSers = new VkDescriptorSet[SwapChainT.MAX_FRAMES_IN_FLIGHT];
			for (int i = 0; i < globalDescriptorSers.Length; i++)
			{
				VkDescriptorBufferInfo bufferInfo = uboBuffers[i].DescriptorInfo();
				new DescriptorWriterT(ref globalSetLayout, ref GlobalPool)
					.WriteBuffer(0, ref bufferInfo)
					.Build(ref globalDescriptorSers[i]);
			}

			SimpleRenderSystemT simpleRenderSystem = new(ref Device, Renderer.GetSwapchainRenderPass, globalSetLayout.GetDescriptorSetLayout);
			
			PointLightSystemT pointLightSystem = new(ref Device, Renderer.GetSwapchainRenderPass, globalSetLayout.GetDescriptorSetLayout);

			CameraT camera = new();
			//camera.SetViewTarget(new Vector3(-1.0f, -2.0f, 2.0f), new Vector3(0.0f, 0.0f, -2.5f), null);
			
			GameObjectT viewerObject = new();
			viewerObject.Transform.Translation.Z = -2.5f;
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

				while (SDL.SDL_PollEvent(out SDL.SDL_Event test_event) != 0)
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

				float time = sw.ElapsedTicks;
				float frameTime = (time - lastTime) / 10000000.0f;
				lastTime = time;

				cameraController.MovePlaneXZ3(frameTime, ref viewerObject);
				camera.SetViewYXZ(viewerObject.Transform.Translation, viewerObject.Transform.Rotation);
				
				float aspect = Renderer.GetAspectRatio;
				camera.SetPerspectiveProjection((MathF.PI * 5) / 18, aspect, 0.1f, 100.0f);
				//camera.SetPerspectiveProjection(39.6f * (MathF.PI / 180), aspect, 0.1f, 100.0f);

				//camera.SetOrthographicProjection(-aspect, aspect, -1, 1, -1, 10);
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
					frameInfo.GlobalDescriptorSet = globalDescriptorSers[frameIndex];

					//update
					GlobalUbo ubo = new();
					ubo.Projection = camera.GetProjection;
					ubo.View = camera.GetView;
					ubo.InverseView = camera.GetInverseView;
					//PointLight[] bytes = new PointLight[10];
					//fixed (PointLight* ptr = bytes)
					//{
					//	ubo.PointLights = ptr;
					//}
					pointLightSystem.Update(ref frameInfo, ref ubo);
					uboBuffers[frameIndex].WriteToBufferU(ref ubo);
					uboBuffers[frameIndex].Flush();
					//globalUboBuffer.WriteToIndexU(ref ubo, frameIndex);
					//globalUboBuffer.FlushIndex(frameIndex);

					//render
					Renderer.BeginSwapChainRenderPass(cB);
					simpleRenderSystem.RenderGameObjects(ref frameInfo);
					pointLightSystem.Render(ref frameInfo);
					Renderer.EndSwapChainRenderPass(cB);
					Renderer.EndFrame();
				}
				
			}

			VulkanNative.vkDeviceWaitIdle(Device.Device);

			foreach (BufferT item in uboBuffers)
			{
				item.DestroyBuffer();
			}

			sw.Stop();
			simpleRenderSystem.DestroySRS();
			Destroy();
		}

		private void LoadGameObjects() 
		{
			ModelT model = ModelT.CreateModelFromFile(ref Device, Program.Directory + @"\TestVulkan\models\flat_vase.obj");

			GameObjectT flatVase = new();
			flatVase.Model = model;

			flatVase.Transform.Translation = new Vector3(-0.5f, 0.5f, 0f);
			flatVase.Transform.Scale = new Vector3(3f, 1.5f, 3f);

			GameObjectT.Map.Add(flatVase.GetId(), flatVase);

			model = ModelT.CreateModelFromFile(ref Device, Program.Directory + @"\TestVulkan\models\smooth_vase.obj");

			GameObjectT smoothVase = new();
			smoothVase.Model = model;

			smoothVase.Transform.Translation = new Vector3(0.5f, 0.5f, 0f);
			smoothVase.Transform.Scale = new Vector3(3f, 1.5f, 3f);

			GameObjectT.Map.Add(smoothVase.GetId(), smoothVase);

			model = ModelT.CreateModelFromFile(ref Device, Program.Directory + @"\TestVulkan\models\quad.obj");

			GameObjectT gameObjectFloor = new();
			gameObjectFloor.Model = model;

			gameObjectFloor.Transform.Translation = new Vector3(0f, 0.5f, 0f);
			gameObjectFloor.Transform.Scale = new Vector3(3f, 1f, 3f);

			GameObjectT.Map.Add(gameObjectFloor.GetId(), gameObjectFloor);

			Vector3[] lightColors = new Vector3[6] 
			{
				new Vector3(1.0f, 0.1f, 0.1f),
				new Vector3(0.1f, 0.1f, 1.0f),
				new Vector3(0.1f, 1.0f, 0.1f),
				new Vector3(1.0f, 1.0f, 0.1f),
				new Vector3(0.1f, 1.0f, 1.0f),
				new Vector3(1.0f, 1.0f, 1.0f)
			};

			for (int i = 0; i < lightColors.Length; i++)
			{
				GameObjectT pointLight = new(Vector3.One, 0.1f + (i/10f));
				pointLight.Color = lightColors[i];
				Matrix4x4 rotateLight = Matrix4x4.CreateRotationY(i * (MathF.PI * 2) / lightColors.Length, -Vector3.UnitY);
				pointLight.Transform.Translation = Vector3.Transform(new Vector3(-1.0f, -1.0f, -1.0f), rotateLight);
				GameObjectT.Map.Add(pointLight.GetId(), pointLight);
			}

			//test
			BuilderT b = new();
			b.Vertices = new VertexT[4]
			{
			new VertexT
			{
				Position = new Vector3(0f, 0f, 0f),
				Color = new Vector3(1.0f, 0.0f, 0.0f),
				Normal = new Vector3(0.0f, -1.0f, 0.0f)
			},
			new VertexT
			{
				Position = new Vector3(0f, 0.49026123963255896f, 0.0f),
				Color = new Vector3(0.0f, 1.0f, 0.0f),
				Normal = new Vector3(0.0f, -1.0f, 0.0f)
			},
			new VertexT
			{
				Position = new Vector3(0.8715755371245493f, 0.49026123963255896f, 0.0f),
				Color = new Vector3(0.0f, 0.0f, 1.0f),
				Normal = new Vector3(0.0f, -1.0f, 0.0f)
			},
			new VertexT
			{
				Position = new Vector3(0.8715755371245493f, 0f, 0.0f),
				Color = new Vector3(1.0f, 1.0f, 1.0f),
				Normal = new Vector3(0.0f, -1.0f, 0.0f)
			}
			};
			b.Indices = new uint[6] 
			{
				0, 1, 2, 2, 3, 0,
			};

			model = new ModelT(ref Device, ref b);

			GameObjectT gameObjectTest = new();
			gameObjectTest.Model = model;

			gameObjectTest.Transform.Translation = new Vector3(0f, -1f, 0f);
			gameObjectTest.Transform.Scale = new Vector3(3f, 3f, 1f);

			GameObjectT.Map.Add(gameObjectTest.GetId(), gameObjectTest);
		}

		unsafe public void Destroy() 
		{
			GlobalPool.DestroyDescriptorPool();
			Renderer.DestroyRenderer();
			Device.DestroyDebugMessenger();
			Window.DestroyWindow();
		}
	}
}
