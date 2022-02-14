using SDL2;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Core;
using System.Numerics;

namespace TestVulkan
{
	public class EngineVKG
	{

#if Debug
		private const bool EnableValidationLayers = false;
#else
		private const bool EnableValidationLayers = true;
#endif
		private Vk _vk = Vk.GetApi();
		private KhrSurface? _vkSurface;
		private KhrSwapchain? _vkSwapchain;

		private Queue<Action> deletors = new();

		private VulkanMemory2 memory2;

		public bool _isInitialized = false;
		public int _frameNumber = 0;

		public Extent2D _windowExtent = new Extent2D(960, 540);

		public IntPtr _window = IntPtr.Zero;

		public Instance _instance; // Vulkan library handle
		public DebugUtilsMessengerEXT _debug_messenger; // Vulkan debug output handle
		public PhysicalDevice _chosenGPU; // GPU chosen as the default device
		public Device _device; // Vulkan device for commands
		public SurfaceKHR _surface; // Vulkan window surface

		public SwapchainKHR _swapchain; // from other articles

		// image format expected by the windowing system
		public Format _swapchainImageFormat;

		//array of images from the swapchain
		public Image[] _swapchainImages;

		//array of image-views from the swapchain
		public ImageView[] _swapchainImageViews;

		public Queue _graphicsQueue; //queue we will submit to
		public uint _graphicsQueueFamily; //family of that queue

		public CommandPool _commandPool; //the command pool for our commands
		public CommandBuffer _mainCommandBuffer; //the buffer we will record into

		public RenderPass _renderPass;

		public Framebuffer[] _framebuffers;

		public Silk.NET.Vulkan.Semaphore _presentSemaphore, _renderSemaphore;
		public Fence _renderFence;

		public PipelineLayout _trianglePipelineLayout;
		public Pipeline _trianglePipeline;
		public Pipeline _redTrianglePipeline;
		
		public int _selectedShader = 0;

		public PipelineLayout _meshPipelineLayout;
		public Pipeline _meshPipeline;

		public MeshVKG _triangleMesh;
		public MeshVKG _monkeyMesh;

		public ImageView _depthImageView;
		public AllocatedImageVKG _depthImage;

		//the format for the depth image
		public Format _depthFormat;

		//default array of renderable objects
		public List<RenderObjectVKG> _renderables = new();

		public Dictionary<string, MaterialVKG> _materials = new();
		public Dictionary<string, MeshVKG> _meshes = new();

		private readonly string[] DeviceExtensions = new string[] { "VK_KHR_swapchain" };
		private readonly string[] ValidationLayers = new string[] { "VK_LAYER_KHRONOS_validation", "VK_LAYER_LUNARG_monitor" };
		
		public Queue GraphicsQueue;
		public Queue PresentQueue;
		public EngineVKG()
		{
		}

		//initializes everything in the engine
		public void Init() 
		{
			// We initialize SDL and create a window with it. 
			//SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
			if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
			{
				Trace.TraceError("Unable to initialize SDL: " + SDL.SDL_GetError());
				throw new Exception("Unable to initialize SDL: " + SDL.SDL_GetError());
				return;
			}

			SDL.SDL_WindowFlags window_flags = SDL.SDL_WindowFlags.SDL_WINDOW_VULKAN;

			//create blank SDL window for our application
			_window = SDL.SDL_CreateWindow(
				"Vulkan Engine", //window title
				SDL.SDL_WINDOWPOS_UNDEFINED, //window position x (don't care)
				SDL.SDL_WINDOWPOS_UNDEFINED, //window position y (don't care)
				(int)_windowExtent.Width,  //window width in pixels
				(int)_windowExtent.Height, //window height in pixels
				window_flags
			);

			//load the core Vulkan structures
			InitVulkan();

			//create the swapchain
			InitSwapchain();

			InitCommands();

			InitDefaultRenderpass();

			InitFramebuffers();

			InitSyncStructures();

			InitPipelines();

			LoadMeshes();

			InitScene();

			//everything went fine
			_isInitialized = true;
		}

		//shuts down the engine
		unsafe public void Cleanup() 
		{
			if (_isInitialized)
			{
				//make sure the GPU has stopped doing its things
				_vk.DeviceWaitIdle(_device);

				deletors.Reverse();
				foreach (Action action in deletors) 
				{
					action.Invoke();
				}

				memory2.FreeAll(ref _vk, ref _device);

				_vkSurface.DestroySurface(_instance, _surface, null);

				_vk.DestroyDevice(_device, null);

				if (EnableValidationLayers)
					DestroyDebugMessenger();

				_vk.DestroyInstance(_instance, null);

				SDL.SDL_DestroyWindow(_window);
			}
		}

		//draw loop
		unsafe public void Draw() 
		{
			//wait until the GPU has finished rendering the last frame. Timeout of 1 second
			if (_vk.WaitForFences(_device, 1, in _renderFence, true, 1000000000) != Result.Success)
			{
				throw new Exception("failed to Wait For Fences!");
			}

			if (_vk.ResetFences(_device, 1, in _renderFence) != Result.Success)
			{
				throw new Exception("failed to Reset Fences!");
			}

			//request image from the swapchain, one second timeout
			uint swapchainImageIndex = 0;

			if (_vkSwapchain.AcquireNextImage(_device, _swapchain, 1000000000, _presentSemaphore, default, ref swapchainImageIndex) != Result.Success)
			{
				throw new Exception("failed to Acquire Next Image!");
			}

			//now that we are sure that the commands finished executing, we can safely reset the command buffer to begin recording again.
			if (_vk.ResetCommandBuffer(_mainCommandBuffer, 0) != Result.Success)
			{
				throw new Exception("failed to Reset Command Buffer!");
			}

			//naming it cmd for shorter writing
			CommandBuffer cmd = _mainCommandBuffer;

			//begin the command buffer recording. We will use this command buffer exactly once, so we want to let Vulkan know that
			CommandBufferBeginInfo cmdBeginInfo = new();
			cmdBeginInfo.SType = StructureType.CommandBufferBeginInfo;
			cmdBeginInfo.PNext = null;

			cmdBeginInfo.PInheritanceInfo = null;
			cmdBeginInfo.Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit;

			if (_vk.BeginCommandBuffer(cmd, in cmdBeginInfo) != Result.Success)
			{
				throw new Exception("failed to Begin Command Buffer!");
			}

			//make a clear-color from frame number. This will flash with a 120*pi frame period.
			ClearValue clearValue = new();
			float flash = MathF.Abs(MathF.Sin(_frameNumber / 120.0f));
			clearValue.Color = new ClearColorValue(0.0f, 0.0f, flash, 1.0f);

			//clear depth at 1
			ClearValue depthClear = new();
			depthClear.DepthStencil.Depth = 1.0f;


			//start the main renderpass.
			//We will use the clear color from above, and the framebuffer of the index the swapchain gave us
			RenderPassBeginInfo rpInfo = new();
			rpInfo.SType = StructureType.RenderPassBeginInfo;
			rpInfo.PNext = null;

			rpInfo.RenderPass = _renderPass;
			rpInfo.RenderArea.Offset.X = 0;
			rpInfo.RenderArea.Offset.Y = 0;
			rpInfo.RenderArea.Extent = _windowExtent;
			rpInfo.Framebuffer = _framebuffers[swapchainImageIndex];

			//connect clear values
			rpInfo.ClearValueCount = 2;
			ClearValue[] clearValues = new ClearValue[2] 
			{
				clearValue,
				depthClear
			};
			fixed (ClearValue* clearValuesPtr = &clearValues[0]) 
			{
				rpInfo.PClearValues = clearValuesPtr;
			}

			_vk.CmdBeginRenderPass(cmd, in rpInfo, SubpassContents.Inline);

			//once we start adding rendering commands, they will go here
			/*
			_vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, _meshPipeline);

			//bind the mesh vertex buffer with offset 0
			ulong offset = 0;
			_vk.CmdBindVertexBuffers(cmd, 0, 1, in _monkeyMesh._vertexBuffer._buffer,in offset);


			//make a model view matrix for rendering the object
			//camera position
			Vector3 camPos = new(0.0f, 0.0f, -2.0f);

			Matrix4x4 view = Matrix4x4.CreateTranslation(camPos);

			//camera projection
			Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(70 * (MathF.PI / 180), 1700.0f / 900.0f, 0.1f, 200.0f);
			projection.M22 *= -1;
			//model rotation
			Matrix4x4 model = Matrix4x4.CreateRotationY((_frameNumber * 0.4f) * (MathF.PI / 180));

			//calculate final mesh matrix
			Matrix4x4 mesh_matrix = model * view * projection;

			MeshPushConstantsVKG constants = new();
			constants.render_matrix = mesh_matrix;

			//upload the matrix to the GPU via push constants
			_vk.CmdPushConstants(cmd, _meshPipelineLayout, ShaderStageFlags.ShaderStageVertexBit, 0, (uint)Marshal.SizeOf<MeshPushConstantsVKG>(),ref constants);
			*/

			DrawObjects(ref cmd, ref _renderables, _renderables.Count);

			//finalize the render pass
			_vk.CmdEndRenderPass(cmd);
			//finalize the command buffer (we can no longer add commands, but it can now be executed)
			if (_vk.EndCommandBuffer(cmd) != Result.Success)
			{
				throw new Exception("failed to End Command Buffer!");
			}

			//prepare the submission to the queue.
			//we want to wait on the _presentSemaphore, as that semaphore is signaled when the swapchain is ready
			//we will signal the _renderSemaphore, to signal that rendering has finished

			SubmitInfo submit = new();
			submit.SType = StructureType.SubmitInfo;
			submit.PNext = null;

			PipelineStageFlags waitStage = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;

			submit.PWaitDstStageMask = &waitStage;

			fixed (Silk.NET.Vulkan.Semaphore* _presentSemaphorePtr = &_presentSemaphore, _renderSemaphorePtr = &_renderSemaphore)
			{
				submit.WaitSemaphoreCount = 1;
				submit.PWaitSemaphores = _presentSemaphorePtr;

				submit.SignalSemaphoreCount = 1;
				submit.PSignalSemaphores = _renderSemaphorePtr;
			}

			submit.CommandBufferCount = 1;
			submit.PCommandBuffers = &cmd;

			//submit command buffer to the queue and execute it.
			// _renderFence will now block until the graphic commands finish execution
			if (_vk.QueueSubmit(_graphicsQueue, 1, in submit, _renderFence) != Result.Success)
			{
				throw new Exception("failed to Queue Submit!");
			}

			// this will put the image we just rendered into the visible window.
			// we want to wait on the _renderSemaphore for that,
			// as it's necessary that drawing commands have finished before the image is displayed to the user
			PresentInfoKHR presentInfo = new();
			presentInfo.SType = StructureType.PresentInfoKhr;
			presentInfo.PNext = null;

			fixed (SwapchainKHR* _swapchainPtr = &_swapchain)
			{
				presentInfo.PSwapchains = _swapchainPtr;
				presentInfo.SwapchainCount = 1;
			}

			fixed (Silk.NET.Vulkan.Semaphore* _renderSemaphorePtr = &_renderSemaphore)
			{
				presentInfo.PWaitSemaphores = _renderSemaphorePtr;
				presentInfo.WaitSemaphoreCount = 1;
			}

			presentInfo.PImageIndices = &swapchainImageIndex;

			if (_vkSwapchain.QueuePresent(_graphicsQueue, in presentInfo) != Result.Success)
			{
				throw new Exception("failed to Queue Present!");
			}

			//increase the number of frames drawn
			_frameNumber++;
		}

		//run main loop
		public void Run() 
		{
			bool bQuit = false;

			//main loop
			while (!bQuit)
			{
				//Handle events on queue
				while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
				{
					//close the window when user clicks the X button or alt-f4s
					if (e.type == SDL.SDL_EventType.SDL_QUIT) 
						bQuit = true;
					else if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
					{
						if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_SPACE)
						{
							_selectedShader += 1;
							if (_selectedShader > 1)
								_selectedShader = 0;
						}
					}
				}

				Draw();
			}
		}

		unsafe private void InitVulkan() 
		{
			if (EnableValidationLayers && !CheckValidationLayerSupport())
			{
				throw new Exception("validation layers requested, but not available!");
			}

			ApplicationInfo appInfo = new();
			appInfo.SType = StructureType.ApplicationInfo;
			appInfo.PApplicationName = (byte*)Help.ReturnIntPtr("Example Vulkan Application");
			appInfo.ApplicationVersion = Vk.MakeVersion(0, 0, 1);
			appInfo.PEngineName = (byte*)Help.ReturnIntPtr("No Engine");
			appInfo.EngineVersion = Vk.MakeVersion(0, 0, 1);
			appInfo.ApiVersion = Vk.Version11;

			InstanceCreateInfo createInfo = new();
			createInfo.SType = StructureType.InstanceCreateInfo;
			createInfo.PApplicationInfo = &appInfo;

			string[] extensions = GetRequiredExtensions();
			createInfo.EnabledExtensionCount = (uint)extensions.Length;
			createInfo.PpEnabledExtensionNames = (byte**)Help.ReturnIntPtrPointerArray(extensions);

			DebugUtilsMessengerCreateInfoEXT debugCreateInfo;
			if (EnableValidationLayers)
			{
				createInfo.EnabledLayerCount = (uint)ValidationLayers.Length;
				createInfo.PpEnabledLayerNames = (byte**)Help.ReturnIntPtrPointerArray(ValidationLayers);

				PopulateDebugMessengerCreateInfo(out debugCreateInfo);

				ValidationFeatureEnableEXT* enables = stackalloc ValidationFeatureEnableEXT[3];
				enables[0] = ValidationFeatureEnableEXT.ValidationFeatureEnableBestPracticesExt;
				enables[1] = ValidationFeatureEnableEXT.ValidationFeatureEnableSynchronizationValidationExt;
				enables[2] = ValidationFeatureEnableEXT.ValidationFeatureEnableGpuAssistedExt;

				ValidationFeaturesEXT features = new();

				features.SType = StructureType.ValidationFeaturesExt;
				features.EnabledValidationFeatureCount = 3;
				features.PEnabledValidationFeatures = enables;

				debugCreateInfo.PNext = &features;

				createInfo.PNext = &debugCreateInfo;
			}
			else
			{
				createInfo.EnabledLayerCount = 0;
				createInfo.PNext = null;
			}

			if (_vk.CreateInstance(&createInfo, null, out _instance) != Result.Success)
			{
				throw new Exception("failed to create instance!");
			}

			if (!_vk.TryGetInstanceExtension(_instance, out _vkSurface))
			{
				throw new NotSupportedException("KHR_surface extension not found.");
			}

			// get the surface of the window we opened with SDL
			ulong surface;
			SDL.SDL_Vulkan_CreateSurface(_window, _instance.Handle, out surface);
			_surface = new SurfaceKHR(surface);

			//PhysicalDevice
			uint deviceCount = 0;
			_vk.EnumeratePhysicalDevices(_instance, &deviceCount, null);
			if (deviceCount == 0)
			{
				throw new Exception("failed to find GPUs with Vulkan support!");
			}

			Trace.WriteLine("Device count: " + deviceCount);

			PhysicalDevice* devices = stackalloc PhysicalDevice[(int)deviceCount];
			_vk.EnumeratePhysicalDevices(_instance, &deviceCount, devices);

			for (int i = 0; i < deviceCount; i++)
			{
				if (IsDeviceSuitable(devices[i]))
				{
					_chosenGPU = devices[i];
					break;
				}
			}

			if (_chosenGPU.Handle == 0)
			{
				throw new Exception("failed to find a suitable GPU!");
			}


			PhysicalDeviceProperties deviceProperties;
			_vk.GetPhysicalDeviceProperties(_chosenGPU, out deviceProperties);
			//Properties = deviceProperties;

			Trace.WriteLine($"Device Name: {Marshal.PtrToStringAnsi((IntPtr)deviceProperties.DeviceName)}");

			//logical device
			QueueFamilyIndicesVKG indices = FindQueueFamilies(_chosenGPU);

			List<DeviceQueueCreateInfo> queueCreateInfos = new();

			uint[] uniqueQueueFamilies;

			if (indices.GraphicsFamily == indices.PresentFamily)
				uniqueQueueFamilies = new[] { indices.GraphicsFamily };
			else
				uniqueQueueFamilies = new[] { indices.GraphicsFamily, indices.PresentFamily };

			float queuePriority = 1.0f;
			foreach (uint queueFamily in uniqueQueueFamilies)
			{
				DeviceQueueCreateInfo queueCreateInfo = new();
				queueCreateInfo.SType = StructureType.DeviceQueueCreateInfo;
				queueCreateInfo.QueueFamilyIndex = queueFamily;
				queueCreateInfo.QueueCount = 1;
				queueCreateInfo.PQueuePriorities = &queuePriority;

				queueCreateInfos.Add(queueCreateInfo);
			}

			PhysicalDeviceFeatures deviceFeatures = new();
			deviceFeatures.SamplerAnisotropy = true;

			DeviceCreateInfo createInfoD = new();
			createInfoD.SType = StructureType.DeviceCreateInfo;

			createInfoD.QueueCreateInfoCount = (uint)queueCreateInfos.Count;

			DeviceQueueCreateInfo[] queueCreateInfosArray = queueCreateInfos.ToArray();
			fixed (DeviceQueueCreateInfo* queueCreateInfosArrayPtr = &queueCreateInfosArray[0])
			{
				createInfoD.PQueueCreateInfos = queueCreateInfosArrayPtr;
			}

			createInfoD.PEnabledFeatures = &deviceFeatures;
			createInfoD.EnabledExtensionCount = (uint)DeviceExtensions.Length;
			createInfoD.PpEnabledExtensionNames = (byte**)DeviceExtensions.ReturnIntPtrPointerArray();


			// might not really be necessary anymore because device specific validation layers
			// have been deprecated
			if (EnableValidationLayers)
			{
				createInfoD.EnabledLayerCount = (uint)ValidationLayers.Length;
				createInfoD.PpEnabledLayerNames = (byte**)ValidationLayers.ReturnIntPtrPointerArray();
			}
			else
			{
				createInfoD.EnabledLayerCount = 0;
			}

			if (_vk.CreateDevice(_chosenGPU, &createInfoD, null, out _device) != Result.Success)
			{
				throw new Exception("failed to create logical device!");
			}

			// use vkbootstrap to get a Graphics queue
			_vk.GetDeviceQueue(_device, indices.GraphicsFamily, 0, out _graphicsQueue);
			_graphicsQueueFamily = indices.GraphicsFamily;

			PhysicalDeviceMemoryProperties physicalDeviceMemoryProperties;
			_vk.GetPhysicalDeviceMemoryProperties(_chosenGPU, &physicalDeviceMemoryProperties);

			memory2 = new(deviceProperties, physicalDeviceMemoryProperties);

			_vk.GetDeviceQueue(_device, indices.PresentFamily, 0, out PresentQueue);
		}

		unsafe private void InitSwapchain() 
		{
			SwapChainSupportDetailsVKG swapChainSupport = QuerySwapChainSupport(_chosenGPU);

			SurfaceFormatKHR surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
			PresentModeKHR presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
			Extent2D extent = ChooseSwapExtent(swapChainSupport.Capabilities);

			uint imageCount = swapChainSupport.Capabilities.MinImageCount + 1;

			if (swapChainSupport.Capabilities.MaxImageCount > 0 &&
				imageCount > swapChainSupport.Capabilities.MaxImageCount)
			{
				imageCount = swapChainSupport.Capabilities.MaxImageCount;
			}

			SwapchainCreateInfoKHR createInfo = new();
			createInfo.SType = StructureType.SwapchainCreateInfoKhr;
			createInfo.Surface = _surface;

			createInfo.MinImageCount = imageCount;
			createInfo.ImageFormat = surfaceFormat.Format;
			createInfo.ImageColorSpace = surfaceFormat.ColorSpace;
			createInfo.ImageExtent = extent;
			createInfo.ImageArrayLayers = 1;
			createInfo.ImageUsage = ImageUsageFlags.ImageUsageColorAttachmentBit;

			QueueFamilyIndicesVKG indices = FindQueueFamilies(_chosenGPU);

			uint* queueFamilyIndices = stackalloc uint[2];
			queueFamilyIndices[0] = indices.GraphicsFamily;
			queueFamilyIndices[1] = indices.PresentFamily;

			if (indices.GraphicsFamily != indices.PresentFamily)
			{
				createInfo.ImageSharingMode = SharingMode.Concurrent;
				createInfo.QueueFamilyIndexCount = 2;
				createInfo.PQueueFamilyIndices = queueFamilyIndices;
			}
			else
			{
				createInfo.ImageSharingMode = SharingMode.Exclusive;
				createInfo.QueueFamilyIndexCount = 0;      // Optional
				createInfo.PQueueFamilyIndices = null;  // Optional
			}

			createInfo.PreTransform = swapChainSupport.Capabilities.CurrentTransform;
			createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr;

			createInfo.PresentMode = presentMode;
			createInfo.Clipped = true;

			//if (OldSwapChain == null)
				createInfo.OldSwapchain = default;
			//else
			//createInfo.oldSwapchain = OldSwapChain.SwapChain;

			if (!_vk.TryGetDeviceExtension(_instance, _vk.CurrentDevice.Value, out _vkSwapchain))
			{
				Trace.TraceError("KHR_swapchain extension not found.");
				Console.ReadKey();
				return;
			}

			if (_vkSwapchain.CreateSwapchain(_device, &createInfo, null, out _swapchain) != Result.Success)
			{
				throw new Exception("failed to create swap chain!");
			}

			// we only specified a minimum number of images in the swap chain, so the implementation is
			// allowed to create a swap chain with more. That's why we'll first query the final number of
			// images with vkGetSwapchainImagesKHR, then resize the container and finally call it again to
			// retrieve the handles.
			_vkSwapchain.GetSwapchainImages(_device, _swapchain, &imageCount, null);

			_swapchainImages = new Image[imageCount];

			fixed (Image* swapChainImages = &_swapchainImages[0])
			{
				_vkSwapchain.GetSwapchainImages(_device, _swapchain, &imageCount, swapChainImages);
			}
			_swapchainImageFormat = surfaceFormat.Format;

			deletors.Enqueue(new Action(() => 
			{ 
				_vkSwapchain.DestroySwapchain(_device, _swapchain, null); 
			}));

			_swapchainImageViews = new ImageView[_swapchainImages.Length];

			for (int i = 0; i < _swapchainImages.Length; i++)
			{
				ImageViewCreateInfo viewInfo = new();
				viewInfo.SType = StructureType.ImageViewCreateInfo;
				viewInfo.Image = _swapchainImages[i];
				viewInfo.ViewType = ImageViewType.ImageViewType2D;
				viewInfo.Format = _swapchainImageFormat;
				viewInfo.SubresourceRange.AspectMask = ImageAspectFlags.ImageAspectColorBit;
				viewInfo.SubresourceRange.BaseMipLevel = 0;
				viewInfo.SubresourceRange.LevelCount = 1;
				viewInfo.SubresourceRange.BaseArrayLayer = 0;
				viewInfo.SubresourceRange.LayerCount = 1;

				if (_vk.CreateImageView(_device, &viewInfo, null, out _swapchainImageViews[i]) != Result.Success)
				{
					throw new Exception("failed to create texture image view!");
				}
			}

			//depth image size will match the window
			Extent3D depthImageExtent = new(_windowExtent.Width, _windowExtent.Height, 1);

			//hardcoding the depth format to 32 bit float
			_depthFormat = Format.D32Sfloat;

			//the depth image will be an image with the format we selected and Depth Attachment usage flag
			ImageCreateInfo dimg_info = InitializersVKG.ImageCreateInfo(_depthFormat, ImageUsageFlags.ImageUsageDepthStencilAttachmentBit, depthImageExtent);

			if (_vk.CreateImage(_device, in dimg_info, null, out _depthImage._image) != Result.Success)
			{
				throw new Exception("failed to create image!");
			}

			_depthImage._allocation = memory2.BindImageOrBuffer(ref _vk, ref _device, _depthImage._image, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);


			//build an image-view for the depth image to use for rendering
			ImageViewCreateInfo dview_info = InitializersVKG.ImageviewCreateInfo(_depthFormat, _depthImage._image, ImageAspectFlags.ImageAspectDepthBit);

			if (_vk.CreateImageView(_device,in dview_info, null, out _depthImageView) != Result.Success)
				throw new Exception("failed to create texture image view!");

			//add to deletion queues
			deletors.Enqueue(new Action(() =>
			{
				_vk.DestroyImageView(_device, _depthImageView, null);

				VulkanMemoryChunk2 chunk = memory2.ReturnChunk(_depthImage._allocation);
				_vk.DestroyImage(_device, _depthImage._image, null);
				memory2.FreeOne(ref _vk, ref _device, chunk, _depthImage._allocation);
			}));
		}

		unsafe private void InitCommands() 
		{
			//create a command pool for commands submitted to the graphics queue.
			//we also want the pool to allow for resetting of individual command buffers
			CommandPoolCreateInfo commandPoolInfo = InitializersVKG.CommandPoolCreateInfo(_graphicsQueueFamily, CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit);
			
			if (_vk.CreateCommandPool(_device, in commandPoolInfo, null, out _commandPool) != Result.Success)
			{
				throw new Exception("failed to create command pool!");
			}

			//allocate the default command buffer that we will use for rendering
			CommandBufferAllocateInfo cmdAllocInfo = InitializersVKG.CommandBufferAllocateInfo(_commandPool, 1);

			if (_vk.AllocateCommandBuffers(_device, in cmdAllocInfo, out _mainCommandBuffer) != Result.Success)
			{
				throw new Exception("failed to Allocate Command Buffer!");
			}

			deletors.Enqueue(new Action(() =>
			{
				_vk.DestroyCommandPool(_device, _commandPool, null);
			}));
		}

		unsafe private void InitDefaultRenderpass() 
		{
			// the renderpass will use this color attachment.
			AttachmentDescription color_attachment = new();
			//the attachment will have the format needed by the swapchain
			color_attachment.Format = _swapchainImageFormat;
			//1 sample, we won't be doing MSAA
			color_attachment.Samples = SampleCountFlags.SampleCount1Bit;
			// we Clear when this attachment is loaded
			color_attachment.LoadOp = AttachmentLoadOp.Clear;
			// we keep the attachment stored when the renderpass ends
			color_attachment.StoreOp = AttachmentStoreOp.Store;
			//we don't care about stencil
			color_attachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			color_attachment.StencilStoreOp = AttachmentStoreOp.DontCare;

			//we don't know or care about the starting layout of the attachment
			color_attachment.InitialLayout = ImageLayout.Undefined;

			//after the renderpass ends, the image has to be on a layout ready for display
			color_attachment.FinalLayout = ImageLayout.PresentSrcKhr;

			AttachmentReference color_attachment_ref = new();
			//attachment number will index into the pAttachments array in the parent renderpass itself
			color_attachment_ref.Attachment = 0;
			color_attachment_ref.Layout = ImageLayout.AttachmentOptimal;

			AttachmentDescription depth_attachment = new();
			// Depth attachment
			depth_attachment.Flags = 0;
			depth_attachment.Format = _depthFormat;
			depth_attachment.Samples = SampleCountFlags.SampleCount1Bit;
			depth_attachment.LoadOp = AttachmentLoadOp.Clear;
			depth_attachment.StoreOp = AttachmentStoreOp.Store;
			depth_attachment.StencilLoadOp = AttachmentLoadOp.Clear;
			depth_attachment.StencilStoreOp = AttachmentStoreOp.DontCare;
			depth_attachment.InitialLayout = ImageLayout.Undefined;
			depth_attachment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

			AttachmentReference depth_attachment_ref = new();
			depth_attachment_ref.Attachment = 1;
			depth_attachment_ref.Layout = ImageLayout.DepthStencilAttachmentOptimal;

			//we are going to create 1 subpass, which is the minimum you can do
			SubpassDescription subpass = new();
			subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
			subpass.ColorAttachmentCount = 1;
			subpass.PColorAttachments = &color_attachment_ref;
			//hook the depth attachment into the subpass
			subpass.PDepthStencilAttachment = &depth_attachment_ref;

			//array of 2 attachments, one for the color, and other for depth
			AttachmentDescription[] attachments = new AttachmentDescription[2] 
			{ 
				color_attachment, 
				depth_attachment 
			};

			RenderPassCreateInfo render_pass_info = new();
			render_pass_info.SType = StructureType.RenderPassCreateInfo;

			//connect the color attachment to the info
			render_pass_info.AttachmentCount = (uint)attachments.Length;

			fixed (AttachmentDescription* attachmentsPtr = &attachments[0]) 
			{
				render_pass_info.PAttachments = attachmentsPtr;
			}

			//connect the subpass to the info
			render_pass_info.SubpassCount = 1;
			render_pass_info.PSubpasses = &subpass;

			//1 dependency, which is from "outside" into the subpass. And we can read or write color
			SubpassDependency dependency = new();
			dependency.SrcSubpass = Vk.SubpassExternal;
			dependency.DstSubpass = 0;
			dependency.SrcStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
			dependency.SrcAccessMask = 0;
			dependency.DstStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
			dependency.DstAccessMask = AccessFlags.AccessColorAttachmentWriteBit;

			//dependency from outside to the subpass, making this subpass dependent on the previous renderpasses
			SubpassDependency depth_dependency = new();
			depth_dependency.SrcSubpass = Vk.SubpassExternal;
			depth_dependency.DstSubpass = 0;
			depth_dependency.SrcStageMask = PipelineStageFlags.PipelineStageEarlyFragmentTestsBit| PipelineStageFlags.PipelineStageLateFragmentTestsBit;
			depth_dependency.SrcAccessMask = 0;
			depth_dependency.DstStageMask = PipelineStageFlags.PipelineStageEarlyFragmentTestsBit | PipelineStageFlags.PipelineStageLateFragmentTestsBit;
			depth_dependency.DstAccessMask = AccessFlags.AccessDepthStencilAttachmentWriteBit;

			SubpassDependency[] dependencies = new SubpassDependency[2] 
			{ 
				dependency, 
				depth_dependency
			};

			render_pass_info.DependencyCount = (uint)dependencies.Length;
			fixed (SubpassDependency* dependenciesPtr = &dependencies[0])
			{
				render_pass_info.PDependencies = dependenciesPtr;
			}

			if (_vk.CreateRenderPass(_device, &render_pass_info, null, out _renderPass) != Result.Success)
			{
				throw new Exception("failed to Create Render Pass!");
			}

			deletors.Enqueue(new Action(() =>
			{
				_vk.DestroyRenderPass(_device, _renderPass, null);
			}));
		}

		unsafe private void InitFramebuffers() 
		{
			//create the framebuffers for the swapchain images. This will connect the render-pass to the images for rendering
			FramebufferCreateInfo fb_info = new();
			fb_info.SType = StructureType.FramebufferCreateInfo;
			fb_info.PNext = null;

			fb_info.RenderPass = _renderPass;
			fb_info.AttachmentCount = 1;
			fb_info.Width = _windowExtent.Width;
			fb_info.Height = _windowExtent.Height;
			fb_info.Layers = 1;

			//grab how many images we have in the swapchain
			uint swapchain_imagecount = (uint)_swapchainImages.Length;
			_framebuffers = new Framebuffer[swapchain_imagecount];

			//create framebuffers for each of the swapchain image views
			for (int i = 0; i < swapchain_imagecount; i++)
			{
				ImageView[] attachments = new ImageView[2] 
				{ 
					_swapchainImageViews[i],
					_depthImageView 
				};

				fixed (ImageView* attachmentsPtr = &attachments[0])
				{
					fb_info.PAttachments = attachmentsPtr;
				}
				fb_info.AttachmentCount = (uint)attachments.Length;

				if (_vk.CreateFramebuffer(_device, &fb_info, null, out _framebuffers[i]) != Result.Success)
				{
					throw new Exception("failed to Create Framebuffer!");
				}
			}

			deletors.Enqueue(new Action(() =>
			{
				for (int i = 0; i < swapchain_imagecount; i++)
				{
					_vk.DestroyFramebuffer(_device, _framebuffers[i], null);
					_vk.DestroyImageView(_device, _swapchainImageViews[i], null);
				}
			}));
		}

		unsafe private void InitSyncStructures() 
		{
			//create synchronization structures
			FenceCreateInfo fenceCreateInfo = InitializersVKG.FenceCreateInfo(FenceCreateFlags.FenceCreateSignaledBit);

			if (_vk.CreateFence(_device, &fenceCreateInfo, null, out _renderFence) != Result.Success)
			{
				throw new Exception("failed to Create Fence!");
			}

			//enqueue the destruction of the fence
			deletors.Enqueue(new Action(() =>
			{
				_vk.DestroyFence(_device, _renderFence, null);
			}));

			//for the semaphores we don't need any flags
			SemaphoreCreateInfo semaphoreCreateInfo = InitializersVKG.SemaphoreCreateInfo();
			
			if (_vk.CreateSemaphore(_device, &semaphoreCreateInfo, null, out _presentSemaphore) != Result.Success)
			{
				throw new Exception("failed to Create Semaphore!");
			}

			if (_vk.CreateSemaphore(_device, &semaphoreCreateInfo, null, out _renderSemaphore) != Result.Success)
			{
				throw new Exception("failed to Create Semaphore!");
			}

			deletors.Enqueue(new Action(() =>
			{
				_vk.DestroySemaphore(_device, _presentSemaphore, null);
				_vk.DestroySemaphore(_device, _renderSemaphore, null);
			}));
		}

		unsafe private void InitPipelines()
		{
			ShaderModule triangleFragShader = new();
			if (!LoadShaderModule(Program.Directory + @"\TestVulkan\shaders\colored_triangleF.spv", ref triangleFragShader))
			{
				Trace.WriteLine("Error when building the triangle fragment shader module");
			}
			else
			{
				Trace.WriteLine("Triangle fragment shader successfully loaded");
			}

			ShaderModule triangleVertexShader = new();
			if (!LoadShaderModule(Program.Directory + @"\TestVulkan\shaders\colored_triangleV.spv", ref triangleVertexShader))
			{
				Trace.WriteLine("Error when building the triangle vertex shader module");
			}
			else
			{
				Trace.WriteLine("Triangle vertex shader successfully loaded");
			}

			//compile red triangle modules
			ShaderModule redTriangleFragShader = new();
			if (!LoadShaderModule(Program.Directory + @"\TestVulkan\shaders\triangleF.spv", ref redTriangleFragShader))
			{
				Trace.WriteLine("Error when building the triangle fragment shader module");
			}
			else
			{
				Trace.WriteLine("Triangle fragment shader successfully loaded");
			}

			ShaderModule redTriangleVertShader = new();
			if (!LoadShaderModule(Program.Directory + @"\TestVulkan\shaders\triangleV.spv", ref redTriangleVertShader))
			{
				Trace.WriteLine("Error when building the triangle vertex shader module");
			}
			else
			{
				Trace.WriteLine("Triangle vertex shader successfully loaded");
			}

			//build the pipeline layout that controls the inputs/outputs of the shader
			//we are not using descriptor sets or other systems yet, so no need to use anything other than empty default
			PipelineLayoutCreateInfo pipeline_layout_info = InitializersVKG.PipelineLayoutCreateInfo();

			if (_vk.CreatePipelineLayout(_device,in pipeline_layout_info, null, out _trianglePipelineLayout) != Result.Success)
			{
				throw new Exception("failed to Create Pipeline Layout!");
			}

			//we start from just the default empty pipeline layout info
			PipelineLayoutCreateInfo mesh_pipeline_layout_info = InitializersVKG.PipelineLayoutCreateInfo();

			//setup push constants
			PushConstantRange push_constant;
			//this push constant range starts at the beginning
			push_constant.Offset = 0;
			//this push constant range takes up the size of a MeshPushConstants struct
			push_constant.Size = (uint)Marshal.SizeOf<MeshPushConstantsVKG>();
			//this push constant range is accessible only in the vertex shader
			push_constant.StageFlags = ShaderStageFlags.ShaderStageVertexBit;

			mesh_pipeline_layout_info.PPushConstantRanges = &push_constant;
			mesh_pipeline_layout_info.PushConstantRangeCount = 1;

			if (_vk.CreatePipelineLayout(_device,in mesh_pipeline_layout_info, null,out _meshPipelineLayout) != Result.Success)
			{
				throw new Exception("failed to Create Pipeline Layout!");
			}

			//build the stage-create-info for both vertex and fragment stages. This lets the pipeline know the shader modules per stage
			PipelineBuilderVKG pipelineBuilder = new();

			pipelineBuilder._shaderStages = new PipelineShaderStageCreateInfo[2];

			//pipelineBuilder._shaderStages[0] = InitializersVKG.PipelineShaderStageCreateInfo(ShaderStageFlags.ShaderStageVertexBit, triangleVertexShader);
			//pipelineBuilder._shaderStages[1] = InitializersVKG.PipelineShaderStageCreateInfo(ShaderStageFlags.ShaderStageFragmentBit, triangleFragShader);

			//vertex input controls how to read vertices from vertex buffers. We aren't using it yet
			pipelineBuilder._vertexInputInfo = InitializersVKG.VertexInputStateCreateInfo();

			//input assembly is the configuration for drawing triangle lists, strips, or individual points.
			//we are just going to draw triangle list
			pipelineBuilder._inputAssembly = InitializersVKG.InputAssemblyCreateInfo(PrimitiveTopology.TriangleList);

			//build viewport and scissor from the swapchain extents
			pipelineBuilder._viewport.X = 0.0f;
			pipelineBuilder._viewport.Y = 0.0f;
			pipelineBuilder._viewport.Width = _windowExtent.Width;
			pipelineBuilder._viewport.Height = _windowExtent.Height;
			pipelineBuilder._viewport.MinDepth = 0.0f;
			pipelineBuilder._viewport.MaxDepth = 1.0f;

			pipelineBuilder._scissor.Offset = new Offset2D(0, 0);
			pipelineBuilder._scissor.Extent = _windowExtent;

			//configure the rasterizer to draw filled triangles
			pipelineBuilder._rasterizer = InitializersVKG.RasterizationStateCreateInfo(PolygonMode.Fill);

			//we don't use multisampling, so just run the default one
			pipelineBuilder._multisampling = InitializersVKG.MultisamplingStateCreateInfo();

			//a single blend attachment with no blending and writing to RGBA
			pipelineBuilder._colorBlendAttachment = InitializersVKG.ColorBlendAttachmentState();

			//use the triangle layout we created
			pipelineBuilder._pipelineLayout = _trianglePipelineLayout;

			//default depthtesting
			pipelineBuilder._depthStencil = InitializersVKG.DepthStencilCreateInfo(true, true, CompareOp.LessOrEqual);

			//finally build the pipeline
			//_trianglePipeline = pipelineBuilder.BuildPipeline(_vk, _device, _renderPass);

			/*
			//add the other shaders
			pipelineBuilder._shaderStages[0] = InitializersVKG.PipelineShaderStageCreateInfo(ShaderStageFlags.ShaderStageVertexBit, redTriangleVertShader);
			pipelineBuilder._shaderStages[1] = InitializersVKG.PipelineShaderStageCreateInfo(ShaderStageFlags.ShaderStageFragmentBit, redTriangleFragShader);

			//build the red triangle pipeline
			_redTrianglePipeline = pipelineBuilder.BuildPipeline(_vk, _device, _renderPass);
			*/
			//build the mesh pipeline

			VertexInputDescriptionVKG vertexDescription = VertexVKG.GetVertexSescription();

			//connect the pipeline builder vertex input info to the one we get from Vertex
			fixed (VertexInputAttributeDescription* pAD = &vertexDescription.attributes[0])
			{
				pipelineBuilder._vertexInputInfo.PVertexAttributeDescriptions = pAD;
				pipelineBuilder._vertexInputInfo.VertexAttributeDescriptionCount = (uint)vertexDescription.attributes.Length;
			}
			fixed (VertexInputBindingDescription* pBD = &vertexDescription.bindings[0])
			{
				pipelineBuilder._vertexInputInfo.PVertexBindingDescriptions = pBD;
				pipelineBuilder._vertexInputInfo.VertexBindingDescriptionCount = (uint)vertexDescription.bindings.Length;

			}

			//clear the shader stages for the builder
			pipelineBuilder._shaderStages = new PipelineShaderStageCreateInfo[2];

			//compile mesh vertex shader
			ShaderModule meshVertShader = new();
			if (!LoadShaderModule(Program.Directory + @"\TestVulkan\shaders\tri_meshV.spv",ref meshVertShader))
			{
				Trace.WriteLine("Error when building the meshVertShader vertex shader module");
			}
			else
			{
				Trace.WriteLine("meshVertShader vertex shader successfully loaded");
			}

			//add the other shaders
			pipelineBuilder._shaderStages[0] = InitializersVKG.PipelineShaderStageCreateInfo(ShaderStageFlags.ShaderStageVertexBit, meshVertShader);

			//make sure that triangleFragShader is holding the compiled colored_triangle.frag
			pipelineBuilder._shaderStages[1] = InitializersVKG.PipelineShaderStageCreateInfo(ShaderStageFlags.ShaderStageFragmentBit, triangleFragShader);

			pipelineBuilder._pipelineLayout = _meshPipelineLayout;

			//build the mesh triangle pipeline
			_meshPipeline = pipelineBuilder.BuildPipeline(_vk, _device, _renderPass);

			CreateMaterial(_meshPipeline, _meshPipelineLayout, "defaultmesh");

			//destroy all shader modules, outside of the queue
			_vk.DestroyShaderModule(_device, meshVertShader, null);
			_vk.DestroyShaderModule(_device, redTriangleVertShader, null);
			_vk.DestroyShaderModule(_device, redTriangleFragShader, null);
			_vk.DestroyShaderModule(_device, triangleFragShader, null);
			_vk.DestroyShaderModule(_device, triangleVertexShader, null);

			deletors.Enqueue(new Action(() =>
			{
				//destroy the 3 pipelines we have created
				//_vk.DestroyPipeline(_device, _redTrianglePipeline, null);
				//_vk.DestroyPipeline(_device, _trianglePipeline, null);
				_vk.DestroyPipeline(_device, _meshPipeline, null);

				//destroy the pipeline layout that they use
				_vk.DestroyPipelineLayout(_device, _trianglePipelineLayout, null);
				_vk.DestroyPipelineLayout(_device, _meshPipelineLayout, null);
			}));
		}

		unsafe private void LoadMeshes() 
		{
			//make the array 3 vertices long
			_triangleMesh._vertices = new VertexVKG[3];

			//vertex positions
			_triangleMesh._vertices[0].position = new Vector3(1.0f, 1.0f, 0.0f);
			_triangleMesh._vertices[1].position = new Vector3(-1.0f, 1.0f, 0.0f);
			_triangleMesh._vertices[2].position = new Vector3(0.0f,-1.0f, 0.0f);

			//vertex colors, all green
			_triangleMesh._vertices[0].color = new Vector3( 0.0f, 1.0f, 0.0f ); //pure green
			_triangleMesh._vertices[1].color = new Vector3( 0.0f, 1.0f, 0.0f ); //pure green
			_triangleMesh._vertices[2].color = new Vector3( 0.0f, 1.0f, 0.0f ); //pure green

			//load the monkey
			_monkeyMesh.LoadFromObj(Program.Directory + @"\TestVulkan\models\monkey_smooth.obj");

			//make sure both meshes are sent to the GPU
			UploadMesh(ref _triangleMesh);
			UploadMesh(ref _monkeyMesh);

			//note that we are copying them. Eventually we will delete the hardcoded _monkey and _triangle meshes, so it's no problem now.
			_meshes["monkey"] = _monkeyMesh;
			_meshes["triangle"] = _triangleMesh;

			deletors.Enqueue(new Action(() =>
			{
				VulkanMemoryChunk2 chunk = memory2.ReturnChunk(_triangleMesh._vertexBuffer._allocation);
				_vk.DestroyBuffer(_device, _triangleMesh._vertexBuffer._buffer, null);
				memory2.FreeOne(ref _vk, ref _device, chunk, _triangleMesh._vertexBuffer._allocation);

				chunk = memory2.ReturnChunk(_monkeyMesh._vertexBuffer._allocation);
				_vk.DestroyBuffer(_device, _monkeyMesh._vertexBuffer._buffer, null);
				memory2.FreeOne(ref _vk, ref _device, chunk, _monkeyMesh._vertexBuffer._allocation);
			}));
		}

		private void InitScene() 
		{
			RenderObjectVKG monkey = new();
			monkey.mesh = (MeshVKG)GetMesh("monkey");
			monkey.material = (MaterialVKG)GetMaterial("defaultmesh");
			monkey.transformMatrix = Matrix4x4.Identity;

			_renderables.Add(monkey);

			for (int x = -30; x <= 30; x++)
			{
				for (int y = -30; y <= 30; y++)
				{
					RenderObjectVKG tri;
					tri.mesh = (MeshVKG)GetMesh("triangle");
					tri.material = (MaterialVKG)GetMaterial("defaultmesh");

					Matrix4x4 translation = Matrix4x4.CreateTranslation(new Vector3(x, 0, y));

					Matrix4x4 scale = Matrix4x4.CreateScale(0.2f, 0.2f, 0.2f);
					tri.transformMatrix = scale * translation;

					_renderables.Add(tri);
				}
			}
		}

		unsafe private void UploadMesh(ref MeshVKG mesh) 
		{
			//allocate vertex buffer
			BufferCreateInfo bufferInfo = new();
			bufferInfo.SType = StructureType.BufferCreateInfo;
			//this is the total size, in bytes, of the buffer we are allocating
			bufferInfo.Size = (ulong)(mesh._vertices.Length * Marshal.SizeOf<VertexVKG>());
			//this buffer is going to be used as a Vertex Buffer
			bufferInfo.Usage = BufferUsageFlags.BufferUsageVertexBufferBit;

			if (_vk.CreateBuffer(_device, in bufferInfo, null, out mesh._vertexBuffer._buffer) != Result.Success)
			{
				throw new Exception("failed to create vertex buffer!");
			}

			mesh._vertexBuffer._allocation = memory2.BindImageOrBuffer(ref _vk, ref _device, mesh._vertexBuffer._buffer, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit);
			VulkanMemoryChunk2 chunk = memory2.ReturnChunk(mesh._vertexBuffer._allocation);

			void* data;
			_vk.MapMemory(_device, chunk.DeviceMemory, mesh._vertexBuffer._allocation.StartOffset, (ulong)(mesh._vertices.Length * Marshal.SizeOf<VertexVKG>()), 0, &data);

			mesh._vertices.AsSpan().CopyTo(new Span<VertexVKG>(data, mesh._vertices.Length));

			_vk.UnmapMemory(_device, chunk.DeviceMemory);
		}

		unsafe private bool LoadShaderModule(string filePath, ref ShaderModule outShaderModule) 
		{
			byte[] fileBytes = Help.ReadFile(filePath);
			
			if(fileBytes == null)
				return false;

			//create a new shader module, using the buffer we loaded
			ShaderModuleCreateInfo createInfo = new();
			createInfo.SType = StructureType.ShaderModuleCreateInfo;
			createInfo.PNext = null;

			//codeSize has to be in bytes, so multiply the ints in the buffer by size of int to know the real size of the buffer
			createInfo.CodeSize = (nuint)fileBytes.Length;
			fixed (byte* codePtr = fileBytes)
			{
				createInfo.PCode = (uint*)codePtr;
			}


			//check that the creation goes well.
			ShaderModule shaderModule;
			if (_vk.CreateShaderModule(_device, in createInfo, null,out shaderModule) != Result.Success)
				return false;

			outShaderModule = shaderModule;
			return true;
		}

		private MaterialVKG CreateMaterial(Pipeline pipeline, PipelineLayout layout, string name)
		{
			MaterialVKG mat = new();
			mat.pipeline = pipeline;
			mat.pipelineLayout = layout;

			_materials.Add(name,mat);

			return _materials[name];
		}

		private MaterialVKG? GetMaterial(string name)
		{
			//search for the object, and return nullptr if not found
			if (!_materials.ContainsKey(name))
			{
				return null;
			}
			else 
			{
				return _materials[name];
			}
		}

		private MeshVKG? GetMesh(string name)
		{
			if (!_meshes.ContainsKey(name)) 
			{
				return null;
			}
			else 
			{
				return _meshes[name];
			}
		}

		private void DrawObjects(ref CommandBuffer cmd, ref List<RenderObjectVKG> first, int count)
		{
			//make a model view matrix for rendering the object
			//camera view
			Vector3 camPos = new Vector3(0.0f, -6.0f, -10.0f);

			Matrix4x4 view = Matrix4x4.CreateTranslation(camPos);
			//camera projection
			Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(70 * (MathF.PI / 180), 1700.0f / 900.0f, 0.1f, 200.0f);
			
			projection.M22 *= -1;

			MeshVKG? lastMesh = null;
			MaterialVKG? lastMaterial = null;

			for (int i = 0; i < count; i++)
			{
				RenderObjectVKG obj = first[i];

				//only bind the pipeline if it doesn't match with the already bound one
				if (!obj.material.Equals(lastMaterial))
				{
					_vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, obj.material.Value.pipeline);
					lastMaterial = obj.material;
				}

				Matrix4x4 model = obj.transformMatrix;
				//final render matrix, that we are calculating on the cpu
				Matrix4x4 mesh_matrix = model * view * projection;

				MeshPushConstantsVKG constants = new();
				constants.render_matrix = mesh_matrix;

				//upload the mesh to the GPU via push constants
				_vk.CmdPushConstants(cmd, obj.material.Value.pipelineLayout, ShaderStageFlags.ShaderStageVertexBit, 0, (uint)Marshal.SizeOf<MeshPushConstantsVKG>(), ref constants);

				//only bind the mesh if it's a different one from last bind
				if (!obj.mesh.Equals(lastMesh))
				{
					//bind the mesh vertex buffer with offset 0
					ulong offset = 0;
					_vk.CmdBindVertexBuffers(cmd, 0, 1, in obj.mesh._vertexBuffer._buffer, in offset);
					lastMesh = obj.mesh;
				}

				//we can now draw
				_vk.CmdDraw(cmd, (uint)obj.mesh._vertices.Length, 1, 0, 0);
			}
		}

		private SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] availableFormats)
		{
			SurfaceFormatKHR returnFormat = availableFormats[0];
			foreach (SurfaceFormatKHR availableFormat in availableFormats)
			{
				Trace.WriteLine($"Available Swap Surface Format: {availableFormat.Format}");
				if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.ColorSpaceSrgbNonlinearKhr)
					returnFormat = availableFormat;
			}

			Trace.WriteLine($"Return Format: {returnFormat.Format}");

			return returnFormat;
		}

		private PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] availablePresentModes)
		{
			PresentModeKHR returnPresentMode = PresentModeKHR.PresentModeFifoKhr;

			foreach (PresentModeKHR availablePresentMode in availablePresentModes)
			{
				Trace.WriteLine($"Available Swap Present Mode: {availablePresentMode}");
				if (availablePresentMode == PresentModeKHR.PresentModeFifoKhr)
					returnPresentMode = availablePresentMode;
			}
			Trace.WriteLine($"Return Present Mode: {returnPresentMode}");

			return returnPresentMode;
		}

		private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
		{
			if (capabilities.CurrentExtent.Width != uint.MaxValue)
			{
				Trace.WriteLine("CurrentExtent: " + capabilities.CurrentExtent.Height + ":" + capabilities.CurrentExtent.Width);
				return capabilities.CurrentExtent;
			}
			else
			{
				Extent2D actualExtent = _windowExtent;

				actualExtent.Width = Math.Max(capabilities.MinImageExtent.Width, Math.Min(capabilities.MaxImageExtent.Width, actualExtent.Width));
				actualExtent.Height = Math.Max(capabilities.MinImageExtent.Height, Math.Min(capabilities.MaxImageExtent.Height, actualExtent.Height));

				Trace.WriteLine("ActualExtent: " + actualExtent.Height + ":" + actualExtent.Width);

				return actualExtent;
			}
		}

		unsafe private bool CheckValidationLayerSupport()
		{
			uint layerCount;
			_vk.EnumerateInstanceLayerProperties(&layerCount, null);
			//Vulkan.vkInitialize();
			LayerProperties* availableLayers = stackalloc LayerProperties[(int)layerCount];
			_vk.EnumerateInstanceLayerProperties(&layerCount, availableLayers);

			foreach (string layerName in ValidationLayers)
			{
				bool layerFound = false;

				for (int i = 0; i < layerCount; i++)
				{
					string? name = Marshal.PtrToStringAnsi((IntPtr)availableLayers[i].LayerName);
					if (name == layerName)
					{
						layerFound = true;
						break;
					}
				}

				if (!layerFound)
				{
					return false;
				}
			}

			return true;
		}

		unsafe private string[] GetRequiredExtensions()
		{
			if (SDL.SDL_Vulkan_GetInstanceExtensions(_window, out uint extCount, IntPtr.Zero) == SDL.SDL_bool.SDL_FALSE)
			{
				Trace.TraceError("Unable to SDL_Vulkan_GetInstanceExtensions 1: " + SDL.SDL_GetError());
				throw new Exception(SDL.SDL_GetError());
			}

			IntPtr[] intPtrsExt = new IntPtr[extCount];

			if (SDL.SDL_Vulkan_GetInstanceExtensions(_window, out extCount, intPtrsExt) == SDL.SDL_bool.SDL_FALSE)
			{
				Trace.TraceError("Unable to SDL_Vulkan_GetInstanceExtensions 2: " + SDL.SDL_GetError());
				throw new Exception(SDL.SDL_GetError());
			}

			//https://github.com/Amatsugu/Meteora/blob/92b4fba8fbf39199894398c2221f537175d9fb37/Meteora/View/MeteoraWindow.cs#L67
			IEnumerable<string?> exts0 = intPtrsExt.Select(ptr => Marshal.PtrToStringAnsi(ptr));
			string[] exts;

			if (EnableValidationLayers)
				exts = exts0.Append("VK_EXT_debug_utils").ToArray();
			else
				exts = exts0.ToArray();

			return exts;
		}

		unsafe private bool IsDeviceSuitable(PhysicalDevice device)
		{
			QueueFamilyIndicesVKG indices = FindQueueFamilies(device);

			bool extensionsSupported = CheckDeviceExtensionSupport(device);

			bool swapChainAdequate = false;
			if (extensionsSupported)
			{
				SwapChainSupportDetailsVKG swapChainSupport = QuerySwapChainSupport(device);
				swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
			}

			_vk.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures supportedFeatures);

			return indices.IsComplete() && extensionsSupported && swapChainAdequate && supportedFeatures.SamplerAnisotropy;
		}

		unsafe public QueueFamilyIndicesVKG FindQueueFamilies(PhysicalDevice device)
		{
			QueueFamilyIndicesVKG indices = new();

			uint queueFamilyCount = 0;
			_vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

			QueueFamilyProperties* queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
			_vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies);

			for (int i = 0; i < queueFamilyCount; i++)
			{
				if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
				{
					indices.GraphicsFamily = (uint)i;
					indices.GraphicsFamilyHasValue = true;
				}

				Bool32 presentSupport = false;
				_vkSurface.GetPhysicalDeviceSurfaceSupport(device, (uint)i, _surface, out presentSupport);
				if (queueFamilies[i].QueueCount > 0 && presentSupport)
				{
					indices.PresentFamily = (uint)i;
					indices.PresentFamilyHasValue = true;
				}

				if (indices.IsComplete())
				{
					break;
				}

				i++;
			}

			return indices;
		}

		unsafe private bool CheckDeviceExtensionSupport(PhysicalDevice device)
		{
			uint extensionCount;
			_vk.EnumerateDeviceExtensionProperties(device, "", &extensionCount, null);

			ExtensionProperties* availableExtensions = stackalloc ExtensionProperties[(int)extensionCount];
			_vk.EnumerateDeviceExtensionProperties(
				device,
				"",
				&extensionCount,
				availableExtensions);

			List<string> requiredExtensions = DeviceExtensions.ToList();

			for (int i = 0; i < extensionCount; i++)
			{
				requiredExtensions.Remove(Marshal.PtrToStringAnsi((IntPtr)availableExtensions[i].ExtensionName));
			}

			if (requiredExtensions.Count == 0)
				return true;
			else
				return false;
		}

		unsafe public SwapChainSupportDetailsVKG QuerySwapChainSupport(PhysicalDevice device)
		{
			SwapChainSupportDetailsVKG details = new();
			_vkSurface.GetPhysicalDeviceSurfaceCapabilities(device, _surface, out details.Capabilities);

			uint formatCount;
			_vkSurface.GetPhysicalDeviceSurfaceFormats(device, _surface, &formatCount, null);

			if (formatCount != 0)
			{
				details.Formats = new SurfaceFormatKHR[formatCount];
				fixed (SurfaceFormatKHR* formatsPtr = &details.Formats[0])
				{
					_vkSurface.GetPhysicalDeviceSurfaceFormats(device, _surface, &formatCount, formatsPtr);
				}
			}

			uint presentModeCount;
			_vkSurface.GetPhysicalDeviceSurfacePresentModes(device, _surface, &presentModeCount, null);

			if (presentModeCount != 0)
			{
				details.PresentModes = new PresentModeKHR[presentModeCount];
				fixed (PresentModeKHR* presentModesPtr = &details.PresentModes[0])
				{
					_vkSurface.GetPhysicalDeviceSurfacePresentModes(
					device,
					_surface,
					&presentModeCount,
					presentModesPtr);
				}
			}

			return details;
		}


		//
		//
		//debug https://github.com/EvergineTeam/Vulkan.NET/blob/master/VulkanGen/HelloTriangle/0-Setup/HelloTriangle_ValidationLayer.cs
		unsafe public static Bool32 DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageType, DebugUtilsMessengerCallbackDataEXT pCallbackData, void* pUserData)
		{
			if (messageSeverity > DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt)
			{
				Trace.Indent();
				Trace.WriteLine($"{messageSeverity} {messageType}: {Marshal.PtrToStringAnsi((IntPtr)pCallbackData.PMessage)}");
				Trace.WriteLine("");
				Trace.Unindent();
			}
			return false;
		}

		unsafe public delegate Bool32 DebugCallbackDelegate(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageType, DebugUtilsMessengerCallbackDataEXT pCallbackData, void* pUserData);
		unsafe public static DebugCallbackDelegate CallbackDelegate = new DebugCallbackDelegate(DebugCallback);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		unsafe private delegate Result vkCreateDebugUtilsMessengerEXTDelegate(Instance instance, DebugUtilsMessengerCreateInfoEXT* pCreateInfo, AllocationCallbacks* pAllocator, DebugUtilsMessengerEXT* pMessenger);
		private static vkCreateDebugUtilsMessengerEXTDelegate vkCreateDebugUtilsMessengerEXT_ptr;
		unsafe public static Result vkCreateDebugUtilsMessengerEXT(Instance instance, DebugUtilsMessengerCreateInfoEXT* pCreateInfo, AllocationCallbacks* pAllocator, DebugUtilsMessengerEXT* pMessenger)
			=> vkCreateDebugUtilsMessengerEXT_ptr(instance, pCreateInfo, pAllocator, pMessenger);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		unsafe private delegate void vkDestroyDebugUtilsMessengerEXTDelegate(Instance instance, DebugUtilsMessengerEXT messenger, AllocationCallbacks* pAllocator);
		private static vkDestroyDebugUtilsMessengerEXTDelegate vkDestroyDebugUtilsMessengerEXT_ptr;
		unsafe public static void vkDestroyDebugUtilsMessengerEXT(Instance instance, DebugUtilsMessengerEXT messenger, AllocationCallbacks* pAllocator)
			=> vkDestroyDebugUtilsMessengerEXT_ptr(instance, messenger, pAllocator);

		unsafe public void CreateDebugUtilsMessengerEXT()
		{
			fixed (DebugUtilsMessengerEXT* debugMessengerPtr = &_debug_messenger)
			{
				var funcPtr = _vk.GetInstanceProcAddr(_instance, (byte*)"vkCreateDebugUtilsMessengerEXT".ReturnIntPtr());
				if (funcPtr.Handle != null)
				{
					vkCreateDebugUtilsMessengerEXT_ptr = Marshal.GetDelegateForFunctionPointer<vkCreateDebugUtilsMessengerEXTDelegate>((IntPtr)funcPtr);

					DebugUtilsMessengerCreateInfoEXT createInfo;
					PopulateDebugMessengerCreateInfo(out createInfo);
					vkCreateDebugUtilsMessengerEXT(_instance, &createInfo, null, debugMessengerPtr);
				}
			}
		}
		unsafe private void PopulateDebugMessengerCreateInfo(out DebugUtilsMessengerCreateInfoEXT createInfo)
		{
			createInfo = default;
			createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
			createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt;
			createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt;
			createInfo.PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT((delegate* unmanaged[Cdecl]<DebugUtilsMessageSeverityFlagsEXT, DebugUtilsMessageTypeFlagsEXT, DebugUtilsMessengerCallbackDataEXT*, void*, Bool32>)Marshal.GetFunctionPointerForDelegate(CallbackDelegate));
			createInfo.PUserData = null;
		}
		unsafe public void DestroyDebugMessenger()
		{
			var funcPtr = _vk.GetInstanceProcAddr(_instance, (byte*)"vkDestroyDebugUtilsMessengerEXT".ReturnIntPtr());
			if (funcPtr.Handle != null)
			{
				vkDestroyDebugUtilsMessengerEXT_ptr = Marshal.GetDelegateForFunctionPointer<vkDestroyDebugUtilsMessengerEXTDelegate>((IntPtr)funcPtr);
				vkDestroyDebugUtilsMessengerEXT(_instance, _debug_messenger, null);
			}
		}

		public struct SwapChainSupportDetailsVKG
		{
			public SurfaceCapabilitiesKHR Capabilities;
			public SurfaceFormatKHR[] Formats;
			public PresentModeKHR[] PresentModes;
		};

		public struct QueueFamilyIndicesVKG
		{
			public uint GraphicsFamily;
			public uint PresentFamily;
			public bool GraphicsFamilyHasValue = false;
			public bool PresentFamilyHasValue = false;
			public bool IsComplete()
			{
				return GraphicsFamilyHasValue && PresentFamilyHasValue;
			}
		};

		public struct MeshPushConstantsVKG
		{
			public Vector4 data;
			public Matrix4x4 render_matrix;
		};

		//note that we store the VkPipeline and layout by value, not pointer.
		//They are 64 bit handles to internal driver structures anyway so storing pointers to them isn't very useful


		public struct MaterialVKG
		{
			public Pipeline pipeline;
			public PipelineLayout pipelineLayout;
		};

		public struct RenderObjectVKG
		{
			public MeshVKG mesh;

			public MaterialVKG? material;

			public Matrix4x4 transformMatrix;
		};

	}

	public class PipelineBuilderVKG
	{
		public PipelineShaderStageCreateInfo[] _shaderStages;
		public PipelineVertexInputStateCreateInfo _vertexInputInfo;
		public PipelineInputAssemblyStateCreateInfo _inputAssembly;
		public Viewport _viewport;
		public Rect2D _scissor;
		public PipelineRasterizationStateCreateInfo _rasterizer;
		public PipelineColorBlendAttachmentState _colorBlendAttachment;
		public PipelineMultisampleStateCreateInfo _multisampling;
		public PipelineLayout _pipelineLayout;
		public PipelineDepthStencilStateCreateInfo _depthStencil;

		unsafe public Pipeline BuildPipeline(Vk _vk, Device device, RenderPass pass) 
		{
			//make viewport state from our stored viewport and scissor.
			//at the moment we won't support multiple viewports or scissors
			PipelineViewportStateCreateInfo viewportState = new();
			viewportState.SType = StructureType.PipelineViewportStateCreateInfo;
			viewportState.PNext = null;

			viewportState.ViewportCount = 1;
			fixed (Viewport* _viewportPtr = &_viewport) 
			{
				viewportState.PViewports = _viewportPtr;
			}
			viewportState.ScissorCount = 1;
			fixed (Rect2D* _scissorPtr = &_scissor)
			{
				viewportState.PScissors = _scissorPtr;
			}

			//setup dummy color blending. We aren't using transparent objects yet
			//the blending is just "no blend", but we do write to the color attachment
			PipelineColorBlendStateCreateInfo colorBlending = new();
			colorBlending.SType = StructureType.PipelineColorBlendStateCreateInfo;
			colorBlending.PNext = null;

			colorBlending.LogicOpEnable = false;
			colorBlending.LogicOp = LogicOp.Copy;
			colorBlending.AttachmentCount = 1;
			fixed (PipelineColorBlendAttachmentState* _colorBlendAttachmentPtr = &_colorBlendAttachment)
			{
				colorBlending.PAttachments = _colorBlendAttachmentPtr;
			}

			//build the actual pipeline
			//we now use all of the info structs we have been writing into into this one to create the pipeline
			GraphicsPipelineCreateInfo pipelineInfo = new();
			pipelineInfo.SType = StructureType.GraphicsPipelineCreateInfo;
			pipelineInfo.PNext = null;

			pipelineInfo.StageCount = (uint)_shaderStages.Length;
			fixed (PipelineShaderStageCreateInfo* _shaderStagePtr = &_shaderStages[0])
			{
				pipelineInfo.PStages = _shaderStagePtr;
			}
			fixed (PipelineVertexInputStateCreateInfo* _vertexInputInfoPtr = &_vertexInputInfo)
			{
				pipelineInfo.PVertexInputState = _vertexInputInfoPtr;
			}
			fixed (PipelineInputAssemblyStateCreateInfo* _inputAssemblyPtr = &_inputAssembly)
			{
				pipelineInfo.PInputAssemblyState = _inputAssemblyPtr;
			}
			pipelineInfo.PViewportState = &viewportState;
			fixed (PipelineRasterizationStateCreateInfo* _rasterizerPtr = &_rasterizer)
			{
				pipelineInfo.PRasterizationState = _rasterizerPtr;
			}
			fixed (PipelineMultisampleStateCreateInfo* _multisamplingPtr = &_multisampling)
			{
				pipelineInfo.PMultisampleState = _multisamplingPtr;
			}

			//other states
			fixed (PipelineDepthStencilStateCreateInfo* _depthStencilPtr = &_depthStencil)
			{
				pipelineInfo.PDepthStencilState = _depthStencilPtr;
			}
			pipelineInfo.PColorBlendState = &colorBlending;
			pipelineInfo.Layout = _pipelineLayout;
			pipelineInfo.RenderPass = pass;
			pipelineInfo.Subpass = 0;
			pipelineInfo.BasePipelineHandle = default;

			//it's easy to error out on create graphics pipeline, so we handle it a bit better than the common VK_CHECK case
			Pipeline newPipeline;
			if (_vk.CreateGraphicsPipelines(device, default, 1, in pipelineInfo, null, out newPipeline) != Result.Success)
			{
				Trace.WriteLine("failed to create pipeline");
				return default; // failed to create graphics pipeline
			}
			else
			{
				return newPipeline;
			}
		}
	};

}
