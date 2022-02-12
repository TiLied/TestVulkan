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

			//everything went fine
			_isInitialized = true;
		}

		//shuts down the engine
		unsafe public void Cleanup() 
		{
			if (_isInitialized)
			{
				_vkSwapchain.DestroySwapchain(_device, _swapchain, null);

				//destroy swapchain resources
				for (int i = 0; i < _swapchainImageViews.Length; i++)
				{
					_vk.DestroyImageView(_device, _swapchainImageViews[i], null);
				}

				_vk.DestroyDevice(_device, null);
				_vkSurface.DestroySurface(_instance, _surface, null);

				if (EnableValidationLayers)
					DestroyDebugMessenger();

				_vk.DestroyInstance(_instance, null);
				SDL.SDL_DestroyWindow(_window);
			}
		}

		//draw loop
		public void Draw() { }

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
					if (e.type == SDL.SDL_EventType.SDL_QUIT) bQuit = true;
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

			_vk.GetDeviceQueue(_device, indices.GraphicsFamily, 0, out GraphicsQueue);
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
}
