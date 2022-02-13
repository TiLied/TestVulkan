using SDL2;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Core;

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

			//everything went fine
			_isInitialized = true;
		}

		//shuts down the engine
		unsafe public void Cleanup() 
		{
			if (_isInitialized)
			{
				//make sure the GPU has stopped doing its things
				_vk.WaitForFences(_device, 1, in _renderFence, true, 1000000000);

				deletors.Reverse();
				foreach (Action action in deletors) 
				{
					action.Invoke();
				}

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
			ClearValue clearValue;
			float flash = MathF.Abs(MathF.Sin(_frameNumber / 120.0f));
			clearValue.Color = new ClearColorValue(0.0f, 0.0f, flash, 1.0f);

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
			rpInfo.ClearValueCount = 1;
			rpInfo.PClearValues = &clearValue;

			_vk.CmdBeginRenderPass(cmd, in rpInfo, SubpassContents.Inline);

			//once we start adding rendering commands, they will go here

			if (_selectedShader == 0)
				_vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, _trianglePipeline);
			else
				_vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, _redTrianglePipeline);

			_vk.CmdDraw(cmd, 3, 1, 0, 0);

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

			//we are going to create 1 subpass, which is the minimum you can do
			SubpassDescription subpass = new();
			subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
			subpass.ColorAttachmentCount = 1;
			subpass.PColorAttachments = &color_attachment_ref;

			RenderPassCreateInfo render_pass_info = new();
			render_pass_info.SType = StructureType.RenderPassCreateInfo;

			//connect the color attachment to the info
			render_pass_info.AttachmentCount = 1;
			render_pass_info.PAttachments = &color_attachment;
			//connect the subpass to the info
			render_pass_info.SubpassCount = 1;
			render_pass_info.PSubpasses = &subpass;

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
				fixed (ImageView* _swapchainImageView = &_swapchainImageViews[i])
				{
					fb_info.PAttachments = _swapchainImageView;
				}

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

			//build the stage-create-info for both vertex and fragment stages. This lets the pipeline know the shader modules per stage
			PipelineBuilderVKG pipelineBuilder = new();

			pipelineBuilder._shaderStages = new PipelineShaderStageCreateInfo[2];

			pipelineBuilder._shaderStages[0] = InitializersVKG.PipelineShaderStageCreateInfo(ShaderStageFlags.ShaderStageVertexBit, triangleVertexShader);
			pipelineBuilder._shaderStages[1] = InitializersVKG.PipelineShaderStageCreateInfo(ShaderStageFlags.ShaderStageFragmentBit, triangleFragShader);

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

			//finally build the pipeline
			_trianglePipeline = pipelineBuilder.BuildPipeline(_vk, _device, _renderPass);

			//add the other shaders
			pipelineBuilder._shaderStages[0] = InitializersVKG.PipelineShaderStageCreateInfo(ShaderStageFlags.ShaderStageVertexBit, redTriangleVertShader);
			pipelineBuilder._shaderStages[1] = InitializersVKG.PipelineShaderStageCreateInfo(ShaderStageFlags.ShaderStageFragmentBit, redTriangleFragShader);

			//build the red triangle pipeline
			_redTrianglePipeline = pipelineBuilder.BuildPipeline(_vk, _device, _renderPass);

			//destroy all shader modules, outside of the queue
			_vk.DestroyShaderModule(_device, redTriangleVertShader, null);
			_vk.DestroyShaderModule(_device, redTriangleFragShader, null);
			_vk.DestroyShaderModule(_device, triangleFragShader, null);
			_vk.DestroyShaderModule(_device, triangleVertexShader, null);

			deletors.Enqueue(new Action(() =>
			{
				//destroy the 2 pipelines we have created
				_vk.DestroyPipeline(_device, _redTrianglePipeline, null);
				_vk.DestroyPipeline(_device, _trianglePipeline, null);

				//destroy the pipeline layout that they use
				_vk.DestroyPipelineLayout(_device, _trianglePipelineLayout, null);
			}));
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
				if (availablePresentMode == PresentModeKHR.PresentModeMailboxKhr)
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
