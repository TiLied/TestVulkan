using System;
using Evergine.Bindings.Vulkan;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SDL2;

namespace TestVulkan
{
	public struct SwapChainSupportDetailsT
	{
		public VkSurfaceCapabilitiesKHR Capabilities;
		public VkSurfaceFormatKHR[] Formats;
		public VkPresentModeKHR[] PresentModes;
	};

	public struct QueueFamilyIndicesT
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

	public class DeviceT
	{

#if Debug
		public const bool EnableValidationLayers = false;
#else
		public const bool EnableValidationLayers = true;
#endif

		private VkInstance Instance;
		private VkDebugUtilsMessengerEXT DebugMessenger;
		public VkPhysicalDevice PhysicalDevice = VkPhysicalDevice.Null;
		private WindowT Window;
		private VkCommandPool CommandPool;

		public VkDevice Device;
		public VkSurfaceKHR Surface;
		private VkQueue GraphicsQueue;
		private VkQueue PresentQueue;

		private string[] ValidationLayers = { "VK_LAYER_KHRONOS_validation" };
		private string[] DeviceExtensions = { "VK_KHR_swapchain" };

		public DeviceT(ref WindowT window)
		{
			Window = window;
			CreateInstance();
			SetupDebugMessenger();
			CreateSurface();
			PickPhysicalDevice();
			CreateLogicalDevice();
			CreateCommandPool();
		}

		unsafe private void CreateInstance()
		{
			if (EnableValidationLayers && !CheckValidationLayerSupport())
			{
				throw new Exception("validation layers requested, but not available!");
			}

			VkApplicationInfo appInfo = new();
			appInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO;
			appInfo.pApplicationName = (byte*)"LittleVulkanEngine App".ReturnIntPtr();
			//https://github.com/EvergineTeam/Vulkan.NET/blob/705d28f4088bc94f89e3664ae036b839309c4122/VulkanGen/HelloTriangle/Helpers.cs#L14
			appInfo.applicationVersion = (0 << 22) | (0 << 12) | 2;
			appInfo.pEngineName = (byte*)"No Engine".ReturnIntPtr();
			appInfo.engineVersion = (0 << 22) | (0 << 12) | 2;
			appInfo.apiVersion = (1 << 22) | (2 << 12) | 0;

			VkInstanceCreateInfo createInfo = new();
			createInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO;
			createInfo.pApplicationInfo = &appInfo;

			string[] extensions = GetRequiredExtensions();
			createInfo.enabledExtensionCount = (uint)extensions.Length;
			createInfo.ppEnabledExtensionNames = (byte**)Help.ReturnIntPtrPointerArray(extensions);

			VkDebugUtilsMessengerCreateInfoEXT debugCreateInfo;
			if (EnableValidationLayers)
			{
				createInfo.enabledLayerCount = (uint)ValidationLayers.Length;
				createInfo.ppEnabledLayerNames = (byte**)Help.ReturnIntPtrPointerArray(ValidationLayers);

				PopulateDebugMessengerCreateInfo(out debugCreateInfo);
				createInfo.pNext = &debugCreateInfo;
			}
			else
			{
				createInfo.enabledLayerCount = 0;
				createInfo.pNext = null;
			}

			fixed (VkInstance* instance = &Instance)
			{
				if (VulkanNative.vkCreateInstance(&createInfo, null, instance) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to create instance!");
				}
			}

			GetAllInstanceExtensions();
		}

		unsafe private bool CheckValidationLayerSupport()
		{
			uint layerCount;
			VulkanNative.vkEnumerateInstanceLayerProperties(&layerCount, null);

			VkLayerProperties* availableLayers = stackalloc VkLayerProperties[(int)layerCount];
			VulkanNative.vkEnumerateInstanceLayerProperties(&layerCount, availableLayers);

			foreach (string layerName in ValidationLayers)
			{
				bool layerFound = false;

				for (int i = 0; i < layerCount; i++)
				{
					string? name = Marshal.PtrToStringAnsi((IntPtr)availableLayers[i].layerName);
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
			if (SDL.SDL_Vulkan_GetInstanceExtensions(Window.Window, out uint extCount, IntPtr.Zero) == SDL.SDL_bool.SDL_FALSE)
			{
				Trace.TraceError("Unable to SDL_Vulkan_GetInstanceExtensions 1: " + SDL.SDL_GetError());
				throw new Exception(SDL.SDL_GetError());
			}

			IntPtr[] intPtrsExt = new IntPtr[extCount];

			if (SDL.SDL_Vulkan_GetInstanceExtensions(Window.Window, out extCount, intPtrsExt) == SDL.SDL_bool.SDL_FALSE)
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

		unsafe private void GetAllInstanceExtensions()
		{
			uint extensionCount;
			VulkanNative.vkEnumerateInstanceExtensionProperties(null, &extensionCount, null);
			VkExtensionProperties* extensions = stackalloc VkExtensionProperties[(int)extensionCount];
			VulkanNative.vkEnumerateInstanceExtensionProperties(null, &extensionCount, extensions);

			for (int i = 0; i < extensionCount; i++)
			{
				Trace.WriteLine($"Extension: {Marshal.PtrToStringAnsi((IntPtr)extensions[i].extensionName)} | version: {extensions[i].specVersion}");
			}
		}

		private void SetupDebugMessenger() 
		{
			if (!EnableValidationLayers) return;

			//VkDebugUtilsMessengerCreateInfoEXT createInfo = new();
			//PopulateDebugMessengerCreateInfo(out createInfo);
			CreateDebugUtilsMessengerEXT();
		}

		unsafe private void CreateSurface() 
		{
			Window.CreateWindowSurface(Instance, out Surface);
		}

		unsafe private void PickPhysicalDevice() 
		{
			uint deviceCount = 0;
			VulkanNative.vkEnumeratePhysicalDevices(Instance, &deviceCount, null);
			if (deviceCount == 0)
			{
				throw new Exception("failed to find GPUs with Vulkan support!");
			}
			Trace.WriteLine("Device count: " + deviceCount);

			VkPhysicalDevice* devices = stackalloc VkPhysicalDevice[(int)deviceCount];
			VulkanNative.vkEnumeratePhysicalDevices(Instance, &deviceCount, devices);

			for (int i = 0; i < deviceCount; i++)
			{
				if (IsDeviceSuitable(devices[i]))
				{
					PhysicalDevice = devices[i];
					break;
				}
			}

			if (PhysicalDevice == VkPhysicalDevice.Null)
			{
				throw new Exception("failed to find a suitable GPU!");
			}


			VkPhysicalDeviceProperties deviceProperties;
			VulkanNative.vkGetPhysicalDeviceProperties(PhysicalDevice, &deviceProperties);

			Trace.WriteLine($"Device Name: {Marshal.PtrToStringAnsi((IntPtr)deviceProperties.deviceName)}");
		}

		unsafe private bool IsDeviceSuitable(VkPhysicalDevice device)
		{
			QueueFamilyIndicesT indices = FindQueueFamilies(device);

			bool extensionsSupported = CheckDeviceExtensionSupport(device);

			bool swapChainAdequate = false;
			if (extensionsSupported)
			{
				SwapChainSupportDetailsT swapChainSupport = QuerySwapChainSupport(device);
				swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
			}

			VkPhysicalDeviceFeatures supportedFeatures;
			VulkanNative.vkGetPhysicalDeviceFeatures(device, &supportedFeatures);

			return indices.IsComplete() && extensionsSupported && swapChainAdequate && supportedFeatures.samplerAnisotropy;
		}

		unsafe public QueueFamilyIndicesT FindQueueFamilies(VkPhysicalDevice device)
		{
			QueueFamilyIndicesT indices = new();

			uint queueFamilyCount = 0;
			VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

			VkQueueFamilyProperties* queueFamilies = stackalloc VkQueueFamilyProperties[(int)queueFamilyCount];
			VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies);

			for (int i = 0; i < queueFamilyCount; i++)
			{
				if (queueFamilies[i].queueFlags.HasFlag( VkQueueFlags.VK_QUEUE_GRAPHICS_BIT))
				{
					indices.GraphicsFamily = (uint)i;
					indices.GraphicsFamilyHasValue = true;
				}

				VkBool32 presentSupport = false;
				VulkanNative.vkGetPhysicalDeviceSurfaceSupportKHR(device, (uint)i, Surface, &presentSupport);
				if (queueFamilies[i].queueCount > 0 && presentSupport)
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

		unsafe private bool CheckDeviceExtensionSupport(VkPhysicalDevice device)
		{
			uint extensionCount;
			VulkanNative.vkEnumerateDeviceExtensionProperties(device, null, &extensionCount, null);

			VkExtensionProperties* availableExtensions = stackalloc VkExtensionProperties[(int)extensionCount];
			VulkanNative.vkEnumerateDeviceExtensionProperties(
				device,
				null,
				&extensionCount,
				availableExtensions);

			List<string> requiredExtensions = DeviceExtensions.ToList();

			for (int i = 0; i < extensionCount; i++)
			{
				requiredExtensions.Remove(Marshal.PtrToStringAnsi((IntPtr)availableExtensions[i].extensionName));
			}

			if (requiredExtensions.Count == 0)
				return true;
			else
				return false;
		}

		unsafe public SwapChainSupportDetailsT QuerySwapChainSupport(VkPhysicalDevice device)
		{
			SwapChainSupportDetailsT details = new();
			VulkanNative.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(device, Surface, &details.Capabilities);

			uint formatCount;
			VulkanNative.vkGetPhysicalDeviceSurfaceFormatsKHR(device, Surface, &formatCount, null);

			if (formatCount != 0)
			{
				details.Formats = new VkSurfaceFormatKHR[formatCount];
				fixed (VkSurfaceFormatKHR* formatsPtr = &details.Formats[0])
				{
					VulkanNative.vkGetPhysicalDeviceSurfaceFormatsKHR(device, Surface, &formatCount, formatsPtr);
				}
			}

			uint presentModeCount;
			VulkanNative.vkGetPhysicalDeviceSurfacePresentModesKHR(device, Surface, &presentModeCount, null);

			if (presentModeCount != 0)
			{
				details.PresentModes = new VkPresentModeKHR[presentModeCount];
				fixed (VkPresentModeKHR* presentModesPtr = &details.PresentModes[0])
				{
					VulkanNative.vkGetPhysicalDeviceSurfacePresentModesKHR(
					device,
					Surface,
					&presentModeCount,
					presentModesPtr);
				}
			}

			return details;
		}

		unsafe public VkFormat FindSupportedFormat(VkFormat[] candidates, VkImageTiling tiling, VkFormatFeatureFlags features)
		{
			foreach (VkFormat format in candidates)
			{
				VkFormatProperties props;
				VulkanNative.vkGetPhysicalDeviceFormatProperties(PhysicalDevice, format, &props);

				if (tiling == VkImageTiling.VK_IMAGE_TILING_LINEAR && (props.linearTilingFeatures & features) == features)
				{
					return format;
				}
				else if (tiling == VkImageTiling.VK_IMAGE_TILING_OPTIMAL && (props.optimalTilingFeatures & features) == features)
				{
					return format;
				}
			}

			throw new Exception("failed to find supported format!");
		}

		unsafe private void CreateLogicalDevice() 
		{
			QueueFamilyIndicesT indices = FindQueueFamilies(PhysicalDevice);

			List<VkDeviceQueueCreateInfo> queueCreateInfos = new();

			uint[] uniqueQueueFamilies;

			if (indices.GraphicsFamily == indices.PresentFamily)
				uniqueQueueFamilies = new[] { indices.GraphicsFamily };
			else
				uniqueQueueFamilies = new[] { indices.GraphicsFamily, indices.PresentFamily };

			float queuePriority = 1.0f;
			foreach (uint queueFamily in uniqueQueueFamilies)
			{
				VkDeviceQueueCreateInfo queueCreateInfo = new();
				queueCreateInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
				queueCreateInfo.queueFamilyIndex = queueFamily;
				queueCreateInfo.queueCount = 1;
				queueCreateInfo.pQueuePriorities = &queuePriority;

				queueCreateInfos.Add(queueCreateInfo);
			}

			VkPhysicalDeviceFeatures deviceFeatures = new();
			deviceFeatures.samplerAnisotropy = true;

			VkDeviceCreateInfo createInfo = new();
			createInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO;

			createInfo.queueCreateInfoCount = (uint)queueCreateInfos.Count;

			VkDeviceQueueCreateInfo[] queueCreateInfosArray = queueCreateInfos.ToArray();
			fixed (VkDeviceQueueCreateInfo* queueCreateInfosArrayPtr = &queueCreateInfosArray[0])
			{
				createInfo.pQueueCreateInfos = queueCreateInfosArrayPtr;
			}

			createInfo.pEnabledFeatures = &deviceFeatures;
			createInfo.enabledExtensionCount = (uint)DeviceExtensions.Length;
			createInfo.ppEnabledExtensionNames = (byte**)DeviceExtensions.ReturnIntPtrPointerArray();


			// might not really be necessary anymore because device specific validation layers
			// have been deprecated
			if (EnableValidationLayers)
			{
				createInfo.enabledLayerCount = (uint)ValidationLayers.Length;
				createInfo.ppEnabledLayerNames = (byte**)ValidationLayers.ReturnIntPtrPointerArray();
			}
			else
			{
				createInfo.enabledLayerCount = 0;
			}

			fixed (VkDevice* device = &Device)
			{
				if (VulkanNative.vkCreateDevice(PhysicalDevice, &createInfo, null, device) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to create logical device!");
				}
				
			}

			fixed (VkQueue* graphicsQueue = &GraphicsQueue, presentQueue = &PresentQueue)
			{
				VulkanNative.vkGetDeviceQueue(Device, indices.GraphicsFamily, 0, graphicsQueue);
				VulkanNative.vkGetDeviceQueue(Device, indices.PresentFamily, 0, presentQueue);
			}

		}

		unsafe private void CreateCommandPool() 
		{
			QueueFamilyIndicesT queueFamilyIndices = FindQueueFamilies(PhysicalDevice);

			VkCommandPoolCreateInfo poolInfo = new();
			poolInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO;
			poolInfo.queueFamilyIndex = queueFamilyIndices.GraphicsFamily;
			poolInfo.flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_TRANSIENT_BIT | VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;

			fixed (VkCommandPool* commandPool = &CommandPool)
			{
				if (VulkanNative.vkCreateCommandPool(Device, &poolInfo, null, commandPool) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to create command pool!");
				}
			}
		}

		unsafe public void CreateImageWithInfo(VkImageCreateInfo imageInfo,
				VkMemoryPropertyFlags properties,
				ref VkImage image,
				ref VkDeviceMemory imageMemory)
		{
			fixed (VkImage* imageL = &image)
			{
				if (VulkanNative.vkCreateImage(Device, &imageInfo, null, imageL) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to create image!");
				}
			}

			VkMemoryRequirements memRequirements;
			VulkanNative.vkGetImageMemoryRequirements(Device, image, &memRequirements);

			VkMemoryAllocateInfo allocInfo = new();
			allocInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO;
			allocInfo.allocationSize = memRequirements.size;
			allocInfo.memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, properties);

			fixed (VkDeviceMemory* imageMemoryL = &imageMemory)
			{
				if (VulkanNative.vkAllocateMemory(Device, &allocInfo, null, imageMemoryL) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to allocate image memory!");
				}
			}

			if (VulkanNative.vkBindImageMemory(Device, image, imageMemory, 0) != VkResult.VK_SUCCESS)
			{
				throw new Exception("failed to bind image memory!");
			}
		}

		unsafe private uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
		{
			VkPhysicalDeviceMemoryProperties memProperties;
			VulkanNative.vkGetPhysicalDeviceMemoryProperties(PhysicalDevice, &memProperties);
			for (int ii = 0; ii < memProperties.memoryTypeCount; ii++)
			{
				if (((typeFilter & (1 << ii)) != 0)
					&& (GetMemoryType(memProperties, (uint)ii).propertyFlags & properties) == properties)
				{
					return (uint)ii;
				}
			}

			throw new Exception("failed to find suitable memory type!");
		}

		//debug
		//https://github.com/EvergineTeam/Vulkan.NET/blob/master/VulkanGen/HelloTriangle/0-Setup/HelloTriangle_ValidationLayer.cs

		unsafe public static VkBool32 DebugCallback(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity, VkDebugUtilsMessageTypeFlagsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT pCallbackData, void* pUserData)
		{
			if (messageSeverity > VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_VERBOSE_BIT_EXT)
			{
				Trace.Indent();
				Trace.WriteLine($"<<Vulkan Validation Layer>> {Marshal.PtrToStringAnsi((IntPtr)pCallbackData.pMessage)}");
				Trace.WriteLine("");
				Trace.Unindent();
			}
			return false;
		}

		unsafe public delegate VkBool32 DebugCallbackDelegate(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity, VkDebugUtilsMessageTypeFlagsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT pCallbackData, void* pUserData);
		unsafe public static DebugCallbackDelegate CallbackDelegate = new DebugCallbackDelegate(DebugCallback);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		unsafe private delegate VkResult vkCreateDebugUtilsMessengerEXTDelegate(VkInstance instance, VkDebugUtilsMessengerCreateInfoEXT* pCreateInfo, VkAllocationCallbacks* pAllocator, VkDebugUtilsMessengerEXT* pMessenger);
		private static vkCreateDebugUtilsMessengerEXTDelegate vkCreateDebugUtilsMessengerEXT_ptr;
		unsafe public static VkResult vkCreateDebugUtilsMessengerEXT(VkInstance instance, VkDebugUtilsMessengerCreateInfoEXT* pCreateInfo, VkAllocationCallbacks* pAllocator, VkDebugUtilsMessengerEXT* pMessenger)
			=> vkCreateDebugUtilsMessengerEXT_ptr(instance, pCreateInfo, pAllocator, pMessenger);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		unsafe private delegate void vkDestroyDebugUtilsMessengerEXTDelegate(VkInstance instance, VkDebugUtilsMessengerEXT messenger, VkAllocationCallbacks* pAllocator);
		private static vkDestroyDebugUtilsMessengerEXTDelegate vkDestroyDebugUtilsMessengerEXT_ptr;
		unsafe public static void vkDestroyDebugUtilsMessengerEXT(VkInstance instance, VkDebugUtilsMessengerEXT messenger, VkAllocationCallbacks* pAllocator)
			=> vkDestroyDebugUtilsMessengerEXT_ptr(instance, messenger, pAllocator);

		unsafe public void CreateDebugUtilsMessengerEXT() 
			{
				fixed (VkDebugUtilsMessengerEXT* debugMessengerPtr = &DebugMessenger)
				{
					var funcPtr = VulkanNative.vkGetInstanceProcAddr(Instance, (byte*)"vkCreateDebugUtilsMessengerEXT".ReturnIntPtr());
					if (funcPtr != IntPtr.Zero)
					{
						vkCreateDebugUtilsMessengerEXT_ptr = Marshal.GetDelegateForFunctionPointer<vkCreateDebugUtilsMessengerEXTDelegate>(funcPtr);

						VkDebugUtilsMessengerCreateInfoEXT createInfo;
						PopulateDebugMessengerCreateInfo(out createInfo);
						vkCreateDebugUtilsMessengerEXT(Instance, &createInfo, null, debugMessengerPtr);
					 }
				}
			}
		unsafe private void PopulateDebugMessengerCreateInfo(out VkDebugUtilsMessengerCreateInfoEXT createInfo)
		{
			createInfo = default;
			createInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT;
			createInfo.messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_VERBOSE_BIT_EXT | VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_WARNING_BIT_EXT | VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT;
			createInfo.messageType = VkDebugUtilsMessageTypeFlagsEXT.VK_DEBUG_UTILS_MESSAGE_TYPE_GENERAL_BIT_EXT | VkDebugUtilsMessageTypeFlagsEXT.VK_DEBUG_UTILS_MESSAGE_TYPE_PERFORMANCE_BIT_EXT | VkDebugUtilsMessageTypeFlagsEXT.VK_DEBUG_UTILS_MESSAGE_TYPE_VALIDATION_BIT_EXT;
			createInfo.pfnUserCallback = Marshal.GetFunctionPointerForDelegate(CallbackDelegate);
			createInfo.pUserData = null;
		}
		unsafe public void DestroyDebugMessenger()
		{
			var funcPtr = VulkanNative.vkGetInstanceProcAddr(Instance, (byte*)"vkDestroyDebugUtilsMessengerEXT".ReturnIntPtr());
			if (funcPtr != IntPtr.Zero)
			{
				vkDestroyDebugUtilsMessengerEXT_ptr = Marshal.GetDelegateForFunctionPointer<vkDestroyDebugUtilsMessengerEXTDelegate>(funcPtr);
				vkDestroyDebugUtilsMessengerEXT(Instance, DebugMessenger, null);
			}
		}

		unsafe public static VkMemoryType GetMemoryType(VkPhysicalDeviceMemoryProperties memoryProperties, uint index)
		{
			return (&memoryProperties.memoryTypes_0)[index];
		}
	}
}
