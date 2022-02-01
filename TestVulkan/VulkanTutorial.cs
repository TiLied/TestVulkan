using Silk.NET.Core;
using Silk.NET.Vulkan;
using SDL2;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Core.Native;
using System.Runtime.CompilerServices;
using System.Numerics;
using StbImageSharp;
using System.Globalization;

namespace TestVulkan
{
	public class VulkanTutorial
	{
		//private VulkanMemory VulkanMemoryLocal;
		private VulkanMemory2 VulkanMemoryLocal2;

		private Vk _vk = Vk.GetApi();
		private KhrSurface? _vkSurface;
		private KhrSwapchain? _vkSwapchain;

		private const int WIDTH = 960;
		private const int HEIGHT = 540;

		private const int MAX_FRAMES_IN_FLIGHT = 2;

		private string MODEL_PATH = Program.Directory + @"\TestVulkan\models\viking_room.obj";
		private string TEXTURE_PATH = Program.Directory + @"\TestVulkan\textures\viking_room.png";

		/*
		private List<Vertex> Vertices = new()
		{
			new Vertex
			{ 
				Pos = new Vector3(-0.8f, -0.6f, 0.0f), 
				Color = new Vector3(1.0f, 0.0f, 0.0f),
				TexCoord = new Vector2(1.0f, 0.0f)
			},
			new Vertex
			{ 
				Pos = new Vector3(0.8f, -0.6f, 0.0f), 
				Color = new Vector3(0.0f, 1.0f, 0.0f),
				TexCoord = new Vector2(0.0f, 0.0f)
			},
			new Vertex
			{ 
				Pos = new Vector3(0.8f, 0.6f, 0.0f), 
				Color = new Vector3(0.0f, 0.0f, 1.0f),
				TexCoord = new Vector2(0.0f, 1.0f)
			},
			new Vertex
			{ 
				Pos = new Vector3(-0.8f, 0.6f, 0.0f), 
				Color = new Vector3(1.0f, 1.0f, 1.0f),
				TexCoord = new Vector2(1.0f, 1.0f)
			},

			new Vertex
			{
				Pos = new Vector3(-0.8f, -0.6f, -0.5f),
				Color = new Vector3(1.0f, 0.0f, 0.0f),
				TexCoord = new Vector2(1.0f, 0.0f)
			},
			new Vertex
			{
				Pos = new Vector3(0.8f, -0.6f, -0.5f),
				Color = new Vector3(0.0f, 1.0f, 0.0f),
				TexCoord = new Vector2(0.0f, 0.0f)
			},
			new Vertex
			{
				Pos = new Vector3(0.8f, 0.6f, -0.5f),
				Color = new Vector3(0.0f, 0.0f, 1.0f),
				TexCoord = new Vector2(0.0f, 1.0f)
			},
			new Vertex
			{
				Pos = new Vector3(-0.8f, 0.6f, -0.5f),
				Color = new Vector3(1.0f, 1.0f, 1.0f),
				TexCoord = new Vector2(1.0f, 1.0f)
			}
		};
		private List<short> Indices = new()
		{
			0, 1, 2, 2, 3, 0,
			4, 5, 6, 6, 7, 4
		};
		*/

		private List<Vertex> Vertices = new();
		private List<uint> Indices = new();

		private PushConstantData Data = new() 
		{
			Color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
			Position = new Vector4(-0.5f, -0.5f, -0.5f, 1.0f)
		};

		private IntPtr Window;

		unsafe private Instance VulkanInstance;

		private PhysicalDevice PhysicalDevice;

		private Device Device;

		private Queue GraphicsQueue;
		private Queue PresentQueue;

		private DebugUtilsMessengerEXT DebugMessenger;

		private SurfaceKHR Surface;

		private SwapchainKHR SwapChain;
		private Image[] SwapChainImages;
		private Format SwapChainImageFormat;
		private Extent2D SwapChainExtent;
		private ImageView[] SwapChainImageViews;
		private Framebuffer[] SwapChainFramebuffers;

		private RenderPass RenderPass;
		private PipelineLayout PipelineLayout;
		private Pipeline GraphicsPipeline;

		private DescriptorSetLayout DescriptorSetLayout;
		private DescriptorPool DescriptorPool;
		private DescriptorSet[] DescriptorSets;

		private CommandPool CommandPool;
		private CommandPool CommandPoolSecond;
		private CommandBuffer[] CommandBuffers;

		private Silk.NET.Vulkan.Buffer VertexBuffer;
		private DeviceMemory VertexBufferMemory;

		private Silk.NET.Vulkan.Buffer IndexBuffer;
		private DeviceMemory IndexBufferMemory;

		private Silk.NET.Vulkan.Buffer[] UniformBuffers;
		private DeviceMemory[] UniformBuffersMemory;
		//private (VulkanMemoryChunk, VulkanMemoryItem)[] UniformBuffersChunksAndItens;
		private VulkanMemoryItem2[] UniformBuffersItems;
		private VulkanMemoryChunk2[] UniformBuffersChunks;
		
		private Silk.NET.Vulkan.Semaphore[] ImageAvailableSemaphores;
		private Silk.NET.Vulkan.Semaphore[] RenderFinishedSemaphores;

		private Fence[] InFlightFences;
		private Fence[] ImagesInFlight;

		private uint MipLevels;

		private Image TextureImage;
		private DeviceMemory TextureImageMemory;

		private ImageView TextureImageView;
		private Sampler TextureSampler;

		private Image DepthImage;
		private DeviceMemory DepthImageMemory;
		private ImageView DepthImageView;

		private int CurrentFrame = 0;

		private bool Minimized = false;

		private readonly string[] DeviceExtensions = new string[] { "VK_KHR_swapchain" };

		//TODO ! write your own https://vulkan-tutorial.com/en/Drawing_a_triangle/Setup/Validation_layers
		//Not sure how :( so using silknet
		//CreateDebugUtilsMessengerEXT!!!!
		private ExtDebugUtils? _debugUtils;

		private readonly string[] ValidationLayers = new string[] { "VK_LAYER_KHRONOS_validation", "VK_LAYER_LUNARG_monitor" };

		private const bool EnableValidationLayers = true;

		private float MaxSamplerAnisotropy = 0f;

		private SampleCountFlags MsaaSamples = SampleCountFlags.SampleCount1Bit;

		private Image ColorImage;
		private DeviceMemory ColorImageMemory;
		private ImageView ColorImageView;

		private bool Test = true;
		private bool Test2 = true;
		public void Run()
		{
			InitWindow();
			InitVulkan();
			MainLoop();
			Cleanup();
		}
		private void InitWindow()
		{
			if (SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING) != 0)
			{
				Trace.TraceError("Unable to initialize SDL: " + SDL.SDL_GetError());
				Console.ReadKey();
				return;
			}

			SDL.SDL_VERSION(out SDL.SDL_version compiled);
			SDL.SDL_GetVersion(out SDL.SDL_version linked);
			Trace.WriteLine($"We compiled against SDL version: {compiled.major}.{compiled.minor}.{compiled.patch}");
			Trace.WriteLine($"But we are linking against SDL version: {linked.major}.{linked.minor}.{linked.patch}");

			Window = SDL.SDL_CreateWindow("Vulkan", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, WIDTH, HEIGHT, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_VULKAN);

			SDL.SDL_SetWindowResizable(Window, SDL.SDL_bool.SDL_TRUE);
		}

		private void InitVulkan()
		{
			CreateInstance();
			SetupDebugMessenger();
			CreateSurface();
			PickPhysicalDevice();
			CreateLogicalDevice();
			CreateSwapChain();
			CreateImageViews();
			CreateRenderPass();
			CreateDescriptorSetLayout();
			CreateGraphicsPipeline();
			CreateCommandPool();
			CreateColorResources();
			CreateDepthResources();
			CreateFramebuffers();
			CreateTextureImage();
			CreateTextureImageView();
			CreateTextureSampler();
			LoadModel();
			CreateVertexBuffer();
			CreateIndexBuffer();
			CreateUniformBuffers();
			CreateDescriptorPool();
			CreateDescriptorSets();
			CreateCommandBuffers();
			CreateSyncObjects();
		}

		unsafe private void CreateInstance()
		{
			ApplicationInfo appInfo;
			appInfo.SType = StructureType.ApplicationInfo;
			appInfo.PApplicationName = (byte*)Help.ReturnIntPtr("Hello Triangle");
			appInfo.ApplicationVersion = Vk.MakeVersion(0, 0, 1);
			appInfo.PEngineName = (byte*)Help.ReturnIntPtr("No Engine");
			appInfo.EngineVersion = Vk.MakeVersion(0, 0, 1);
			appInfo.ApiVersion = Vk.Version12;

			InstanceCreateInfo createInfo;
			createInfo.SType = StructureType.InstanceCreateInfo;
			createInfo.PApplicationInfo = &appInfo;

			if (EnableValidationLayers && !CheckValidationLayerSupport())
			{
				Trace.TraceError("validation layers requested, but not available!");
				Console.ReadKey();
				return;
			}

			if (SDL.SDL_Vulkan_GetInstanceExtensions(Window, out uint extCount, IntPtr.Zero) == SDL.SDL_bool.SDL_FALSE)
			{
				Trace.TraceError("Unable to SDL_Vulkan_GetInstanceExtensions 1: " + SDL.SDL_GetError());
				Console.ReadKey();
				return;
			}

			IntPtr[] intPtrsExt = new IntPtr[extCount];

			if (SDL.SDL_Vulkan_GetInstanceExtensions(Window, out extCount, intPtrsExt) == SDL.SDL_bool.SDL_FALSE)
			{
				Trace.TraceError("Unable to SDL_Vulkan_GetInstanceExtensions 2: " + SDL.SDL_GetError());
				Console.ReadKey();
				return;
			}

			//https://github.com/Amatsugu/Meteora/blob/92b4fba8fbf39199894398c2221f537175d9fb37/Meteora/View/MeteoraWindow.cs#L67
			IEnumerable<string?> exts0 = intPtrsExt.Select(ptr => Marshal.PtrToStringAnsi(ptr));
			string?[] exts;

			if (EnableValidationLayers)
			{
				extCount++;
				exts = exts0.Append("VK_EXT_debug_utils").ToArray();
			}
			else
				exts = exts0.ToArray();


			Trace.WriteLine("SDL_Vulkan_GetInstanceExtensions count:" + extCount);

			foreach (string ext in exts)
			{
				Trace.WriteLine("Extension sdl2: " + ext);
			}

			GetAllInstanceExtensions();

			createInfo.EnabledExtensionCount = extCount;
			createInfo.PpEnabledExtensionNames = (byte**)Help.ReturnIntPtrPointerArray(exts);

			if (EnableValidationLayers)
			{
				createInfo.EnabledLayerCount = (uint)ValidationLayers.Length;

				foreach (string validationLayer in ValidationLayers)
				{
					Trace.WriteLine("Validation Layers: " + validationLayer);
				}

				createInfo.PpEnabledLayerNames = (byte**)Help.ReturnIntPtrPointerArray(ValidationLayers);

				DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();

				PopulateDebugMessengerCreateInfo(ref debugCreateInfo);

				ValidationFeatureEnableEXT* enables = stackalloc ValidationFeatureEnableEXT[1];
				enables[0] = ValidationFeatureEnableEXT.ValidationFeatureEnableBestPracticesExt;
				enables[1] = ValidationFeatureEnableEXT.ValidationFeatureEnableSynchronizationValidationExt;
				enables[2] = ValidationFeatureEnableEXT.ValidationFeatureEnableGpuAssistedExt;

				ValidationFeaturesEXT features = new();

				features.SType = StructureType.ValidationFeaturesExt;
				features.EnabledValidationFeatureCount = 1;
				features.PEnabledValidationFeatures = enables;

				debugCreateInfo.PNext = &features;

				createInfo.PNext = &debugCreateInfo;
			}
			else
			{
				createInfo.EnabledLayerCount = 0;
				createInfo.PNext = null;
			}

			fixed (Instance* instance = &VulkanInstance)
			{
				if (_vk.CreateInstance(&createInfo, null, instance) != Result.Success)
				{
					Trace.TraceError("Unable to _vk.CreateInstance");
					Console.ReadKey();
					return;
				}
			}

			//https://github.com/dotnet/Silk.NET/blob/22696597d134d809c8ed5f35f181cda980cd8520/src/Lab/Experiments/VulkanTriangle/HelloTriangleApplication.cs#L364
			if (!_vk.TryGetInstanceExtension(VulkanInstance, out _vkSurface))
			{
				throw new NotSupportedException("KHR_surface extension not found.");
			}

			Help.FreeMemory();
		}

		unsafe private void SetupDebugMessenger()
		{
			if (!EnableValidationLayers) return;
			if (!_vk.TryGetInstanceExtension(VulkanInstance, out _debugUtils)) return;

			DebugUtilsMessengerCreateInfoEXT createInfo = new();
			PopulateDebugMessengerCreateInfo(ref createInfo);

			fixed (DebugUtilsMessengerEXT* debugMessenger = &DebugMessenger)
			{
				if (_debugUtils?.CreateDebugUtilsMessenger(VulkanInstance, &createInfo, null, debugMessenger) != Result.Success)
				{
					Trace.TraceError("Failed to create debug messenger.");
					Console.ReadKey();
					return;
				}
			}
		}

		private void CreateSurface()
		{
			if (SDL.SDL_Vulkan_CreateSurface(Window, VulkanInstance.Handle, out Surface.Handle) == SDL.SDL_bool.SDL_FALSE)
			{
				Trace.TraceError("Failed to create window surface!: " + SDL.SDL_GetError());
				Console.ReadKey();
				return;
			}
		}

		unsafe private void PickPhysicalDevice()
		{
			uint deviceCount = 0;
			_vk.EnumeratePhysicalDevices(VulkanInstance, &deviceCount, null);

			if (deviceCount == 0)
			{
				Trace.TraceError("failed to find GPUs with Vulkan support!");
				Console.ReadKey();
				return;
			}

			PhysicalDevice[] devices = new PhysicalDevice[deviceCount];
			_vk.EnumeratePhysicalDevices(VulkanInstance, &deviceCount, devices);

			foreach (PhysicalDevice device in devices)
			{
				if (IsDeviceSuitable(device))
				{
					PhysicalDevice = device;
					MsaaSamples = GetMaxUsableSampleCount();

					PhysicalDeviceProperties deviceProperties;
					_vk.GetPhysicalDeviceProperties(device, &deviceProperties);

					PhysicalDeviceMemoryProperties physicalDeviceMemoryProperties;
					_vk.GetPhysicalDeviceMemoryProperties(device, &physicalDeviceMemoryProperties);

					//VulkanMemoryLocal = new(deviceProperties, physicalDeviceMemoryProperties);
					VulkanMemoryLocal2 = new(deviceProperties, physicalDeviceMemoryProperties);
					break;
				}
			}

			//https://github.com/dotnet/Silk.NET/blob/c1c1922fb0d5ecf4e49dbaceb753fb058e42d788/src/Lab/Experiments/VulkanTriangle/HelloTriangleApplication.cs#L456
			if (PhysicalDevice.Handle == 0)
			{
				Trace.TraceError("failed to find a suitable GPU!");
				Console.ReadKey();
				return;
			}
		}

		unsafe private void CreateLogicalDevice()
		{
			QueueFamilyIndices indices = FindQueueFamilies(PhysicalDevice);

			uint?[] uniqueQueueFamilies;
			if (indices.GraphicsFamily == indices.PresentFamily)
				uniqueQueueFamilies = new[] { indices.GraphicsFamily };
			else
				uniqueQueueFamilies = new[] { indices.GraphicsFamily, indices.PresentFamily };

			float queuePriority = 1.0f;

			//https://github.com/dotnet/Silk.NET/blob/22696597d134d809c8ed5f35f181cda980cd8520/src/Lab/Experiments/VulkanTriangle/HelloTriangleApplication.cs#L574
			using GlobalMemory mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
			DeviceQueueCreateInfo* queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

			for (int i = 0; i < uniqueQueueFamilies.Length; i++)
			{
				DeviceQueueCreateInfo queueCreateInfo = new();

				queueCreateInfo.SType = StructureType.DeviceQueueCreateInfo;
				queueCreateInfo.QueueFamilyIndex = (uint)uniqueQueueFamilies[i];
				queueCreateInfo.QueueCount = 1;
				queueCreateInfo.PQueuePriorities = &queuePriority;

				queueCreateInfos[i] = queueCreateInfo;
			}

			PhysicalDeviceFeatures deviceFeatures = new();
			deviceFeatures.SamplerAnisotropy = true;
			deviceFeatures.SampleRateShading = true;

			DeviceCreateInfo createInfo = new();
			createInfo.SType = StructureType.DeviceCreateInfo;
			createInfo.QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length;
			createInfo.PQueueCreateInfos = queueCreateInfos;

			createInfo.PEnabledFeatures = &deviceFeatures;

			createInfo.EnabledExtensionCount = (uint)DeviceExtensions.Length;

			foreach (string deviceExtension in DeviceExtensions)
			{
				Trace.WriteLine("Device Extensions: " + deviceExtension);
			}

			createInfo.PpEnabledExtensionNames = (byte**)Help.ReturnIntPtrPointerArray(DeviceExtensions);

			if (EnableValidationLayers)
			{
				createInfo.EnabledLayerCount = (uint)ValidationLayers.Length;
				createInfo.PpEnabledLayerNames = (byte**)Help.ReturnIntPtrPointerArray(ValidationLayers);
			}
			else
			{
				createInfo.EnabledLayerCount = 0;
			}

			fixed (Device* device = &Device)
			{
				if (_vk.CreateDevice(PhysicalDevice, &createInfo, null, device) != Result.Success)
				{
					Trace.TraceError("failed to create logical device!");
					Console.ReadKey();
					return;
				}
			}

			fixed (Queue* graphicsQueue = &GraphicsQueue, presentQueue = &PresentQueue)
			{
				_vk.GetDeviceQueue(Device, (uint)indices.GraphicsFamily, 0, graphicsQueue);
				_vk.GetDeviceQueue(Device, (uint)indices.PresentFamily, 0, presentQueue);
			}

		}

		unsafe private void CreateSwapChain()
		{
			SwapChainSupportDetails swapChainSupport = QuerySwapChainSupport(PhysicalDevice);

			SurfaceFormatKHR surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
			PresentModeKHR presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
			Extent2D extent = ChooseSwapExtent(swapChainSupport.Capabilities);

			Trace.WriteLine("swapChainSupport.Capabilities.MinImageCount: " + swapChainSupport.Capabilities.MinImageCount);
			Trace.WriteLine("swapChainSupport.Capabilities.MaxImageCount: " + swapChainSupport.Capabilities.MaxImageCount);

			uint imageCount = swapChainSupport.Capabilities.MaxImageCount / 2;

			if (imageCount < swapChainSupport.Capabilities.MinImageCount)
				imageCount = swapChainSupport.Capabilities.MinImageCount;

			if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
				imageCount = swapChainSupport.Capabilities.MaxImageCount;

			SwapchainCreateInfoKHR createInfo;
			createInfo.SType = StructureType.SwapchainCreateInfoKhr;
			createInfo.Surface = Surface;
			createInfo.MinImageCount = imageCount;
			createInfo.ImageFormat = surfaceFormat.Format;
			createInfo.ImageColorSpace = surfaceFormat.ColorSpace;
			createInfo.ImageExtent = extent;
			createInfo.ImageArrayLayers = 1;
			createInfo.ImageUsage = ImageUsageFlags.ImageUsageColorAttachmentBit;

			QueueFamilyIndices indices = FindQueueFamilies(PhysicalDevice);
			uint[] queueFamilyIndices = { (uint)indices.GraphicsFamily, (uint)indices.PresentFamily };

			if (indices.GraphicsFamily != indices.PresentFamily)
			{
				createInfo.ImageSharingMode = SharingMode.Concurrent;
				createInfo.QueueFamilyIndexCount = 2;
				fixed (uint* qfiPtr = queueFamilyIndices)
				{
					createInfo.PQueueFamilyIndices = qfiPtr;
				}
			}
			else
			{
				createInfo.ImageSharingMode = SharingMode.Exclusive;
				createInfo.QueueFamilyIndexCount = 0; // Optional
				createInfo.PQueueFamilyIndices = null; // Optional
			}

			createInfo.PreTransform = swapChainSupport.Capabilities.CurrentTransform;
			createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr;
			createInfo.PresentMode = presentMode;
			createInfo.Clipped = Vk.True;

			//https://github.com/dotnet/Silk.NET/blob/22696597d134d809c8ed5f35f181cda980cd8520/src/Lab/Experiments/VulkanTriangle/HelloTriangleApplication.cs#L693
			createInfo.OldSwapchain = default;

			if (!_vk.TryGetDeviceExtension(VulkanInstance, _vk.CurrentDevice.Value, out _vkSwapchain))
			{
				Trace.TraceError("KHR_swapchain extension not found.");
				Console.ReadKey();
				return;
			}

			fixed (SwapchainKHR* swapchain = &SwapChain)
			{
				if (_vkSwapchain.CreateSwapchain(Device, &createInfo, null, swapchain) != Result.Success)
				{
					Trace.TraceError("failed to create swap chain!");
					Console.ReadKey();
					return;
				}
			}

			_vkSwapchain.GetSwapchainImages(Device, SwapChain, &imageCount, null);
			SwapChainImages = new Image[imageCount];
			_vkSwapchain.GetSwapchainImages(Device, SwapChain, &imageCount, SwapChainImages);

			SwapChainImageFormat = surfaceFormat.Format;
			SwapChainExtent = extent;
		}

		unsafe private void CreateImageViews()
		{
			SwapChainImageViews = new ImageView[SwapChainImages.Length];

			//Parallel.For(0, SwapChainImages.Length, (i) =>
			//{
				//CreateImageView(SwapChainImages[i], SwapChainImageFormat, ImageAspectFlags.ImageAspectColorBit, 1, ref SwapChainImageViews[i]);
			//});
			for (int i = 0; i < SwapChainImages.Length; i++)
			{
				CreateImageView(SwapChainImages[i], SwapChainImageFormat, ImageAspectFlags.ImageAspectColorBit, 1, ref SwapChainImageViews[i]);
			}
		}

		unsafe private void CreateRenderPass()
		{
			AttachmentDescription depthAttachment = new();
			depthAttachment.Format = FindDepthFormat();
			depthAttachment.Samples = MsaaSamples;
			depthAttachment.LoadOp = AttachmentLoadOp.Clear;
			depthAttachment.StoreOp = AttachmentStoreOp.DontCare;
			depthAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			depthAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
			depthAttachment.InitialLayout = ImageLayout.Undefined;
			depthAttachment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

			AttachmentReference depthAttachmentRef = new();
			depthAttachmentRef.Attachment = 1;
			depthAttachmentRef.Layout = ImageLayout.DepthStencilAttachmentOptimal;

			AttachmentDescription colorAttachment = new();
			colorAttachment.Format = SwapChainImageFormat;
			colorAttachment.Samples = MsaaSamples;

			colorAttachment.LoadOp = AttachmentLoadOp.Clear;

			//
			//colorAttachment.StoreOp = AttachmentStoreOp.Store;
			colorAttachment.StoreOp = AttachmentStoreOp.DontCare;

			colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;

			colorAttachment.InitialLayout = ImageLayout.Undefined;
			colorAttachment.FinalLayout = ImageLayout.ColorAttachmentOptimal;

			AttachmentReference colorAttachmentRef = new();
			colorAttachmentRef.Attachment = 0;
			colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;

			AttachmentDescription colorAttachmentResolve = new();
			colorAttachmentResolve.Format = SwapChainImageFormat;
			colorAttachmentResolve.Samples = SampleCountFlags.SampleCount1Bit;
			colorAttachmentResolve.LoadOp = AttachmentLoadOp.DontCare;
			colorAttachmentResolve.StoreOp = AttachmentStoreOp.Store;
			colorAttachmentResolve.StencilLoadOp = AttachmentLoadOp.DontCare;
			colorAttachmentResolve.StencilStoreOp = AttachmentStoreOp.DontCare;
			colorAttachmentResolve.InitialLayout = ImageLayout.Undefined;
			colorAttachmentResolve.FinalLayout = ImageLayout.PresentSrcKhr;

			
			AttachmentReference colorAttachmentResolveRef = new();
			colorAttachmentResolveRef.Attachment = 2;
			colorAttachmentResolveRef.Layout = ImageLayout.ColorAttachmentOptimal;

			SubpassDescription subpass = new();
			subpass.PipelineBindPoint = PipelineBindPoint.Graphics;

			subpass.ColorAttachmentCount = 1;
			subpass.PColorAttachments = &colorAttachmentRef;
			subpass.PDepthStencilAttachment = &depthAttachmentRef;
			subpass.PResolveAttachments = &colorAttachmentResolveRef;

			SubpassDependency dependency = new();
			dependency.SrcSubpass = Vk.SubpassExternal;
			dependency.DstSubpass = 0;

			dependency.SrcStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit | PipelineStageFlags.PipelineStageEarlyFragmentTestsBit;
			dependency.SrcAccessMask = 0;

			dependency.DstStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit | PipelineStageFlags.PipelineStageEarlyFragmentTestsBit;
			dependency.DstAccessMask = AccessFlags.AccessColorAttachmentWriteBit | AccessFlags.AccessDepthStencilAttachmentWriteBit;

			AttachmentDescription* attachments = stackalloc AttachmentDescription[3];
			attachments[0] = colorAttachment;
			attachments[1] = depthAttachment;
			attachments[2] = colorAttachmentResolve;

			RenderPassCreateInfo renderPassInfo = new();
			renderPassInfo.SType = StructureType.RenderPassCreateInfo;
			renderPassInfo.AttachmentCount = 3;
			renderPassInfo.PAttachments = attachments;
			renderPassInfo.SubpassCount = 1;
			renderPassInfo.PSubpasses = &subpass;
			renderPassInfo.DependencyCount = 1;
			renderPassInfo.PDependencies = &dependency;

			fixed (RenderPass* renderPass = &RenderPass)
			{
				if (_vk.CreateRenderPass(Device, &renderPassInfo, null, renderPass) != Result.Success)
				{
					Trace.TraceError("failed to create render pass!");
					Console.ReadKey();
					return;
				}
			}

		}

		unsafe private void CreateDescriptorSetLayout()
		{
			DescriptorSetLayoutBinding uboLayoutBinding = new();
			uboLayoutBinding.Binding = 0;
			uboLayoutBinding.DescriptorType = DescriptorType.UniformBuffer;
			uboLayoutBinding.DescriptorCount = 1;

			uboLayoutBinding.StageFlags = ShaderStageFlags.ShaderStageVertexBit;
			uboLayoutBinding.PImmutableSamplers = null; // Optional

			DescriptorSetLayoutBinding samplerLayoutBinding = new();
			samplerLayoutBinding.Binding = 1;
			samplerLayoutBinding.DescriptorCount = 1;
			samplerLayoutBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			samplerLayoutBinding.PImmutableSamplers = null;
			samplerLayoutBinding.StageFlags = ShaderStageFlags.ShaderStageFragmentBit;

			DescriptorSetLayoutBinding* bindings = stackalloc DescriptorSetLayoutBinding[2];
			bindings[0] = uboLayoutBinding;
			bindings[1] = samplerLayoutBinding;

			DescriptorSetLayoutCreateInfo layoutInfo = new();
			layoutInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
			layoutInfo.BindingCount = 2;
			layoutInfo.PBindings = bindings;

			fixed (DescriptorSetLayout* descriptorSetLayout = &DescriptorSetLayout)
			{
				if (_vk.CreateDescriptorSetLayout(Device, &layoutInfo, null, descriptorSetLayout) != Result.Success)
				{
					Trace.TraceError("failed to create descriptor set layout!");
					Console.ReadKey();
					return;
				}

			}
		}

		unsafe private void CreateGraphicsPipeline()
		{
			byte[] vertShaderCode = Help.ReadFile(Program.Directory + @"\TestVulkan\shaders\vert.spv");
			byte[] fragShaderCode = Help.ReadFile(Program.Directory + @"\TestVulkan\shaders\frag.spv");

			ShaderModule vertShaderModule = CreateShaderModule(vertShaderCode);
			ShaderModule fragShaderModule = CreateShaderModule(fragShaderCode);

			PipelineShaderStageCreateInfo vertShaderStageInfo = new();
			vertShaderStageInfo.SType = StructureType.PipelineShaderStageCreateInfo;
			vertShaderStageInfo.Stage = ShaderStageFlags.ShaderStageVertexBit;
			vertShaderStageInfo.Module = vertShaderModule;
			vertShaderStageInfo.PName = (byte*)Help.ReturnIntPtr("main");

			PipelineShaderStageCreateInfo fragShaderStageInfo = new();
			fragShaderStageInfo.SType = StructureType.PipelineShaderStageCreateInfo;
			fragShaderStageInfo.Stage = ShaderStageFlags.ShaderStageFragmentBit;
			fragShaderStageInfo.Module = fragShaderModule;
			fragShaderStageInfo.PName = (byte*)Help.ReturnIntPtr("main");

			PipelineShaderStageCreateInfo* shaderStages = stackalloc PipelineShaderStageCreateInfo[2];
			shaderStages[0] = vertShaderStageInfo;
			shaderStages[1] = fragShaderStageInfo;

			VertexInputBindingDescription bindingDescription = Vertex.GetBindingDescription();
			VertexInputAttributeDescription[] attributeDescriptionsM = Vertex.GetAttributeDescriptions();

			VertexInputAttributeDescription* attributeDescriptions = stackalloc VertexInputAttributeDescription[attributeDescriptionsM.Length];

			Parallel.For(0, attributeDescriptionsM.Length, (i) =>
			{
				attributeDescriptions[i] = attributeDescriptionsM[i];
			});
			/*
			for (int i = 0; i < attributeDescriptionsM.Length; i++)
			{
				attributeDescriptions[i] = attributeDescriptionsM[i];
			}*/

			PipelineVertexInputStateCreateInfo vertexInputInfo = new();
			vertexInputInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;
			vertexInputInfo.VertexBindingDescriptionCount = 1;
			vertexInputInfo.PVertexBindingDescriptions = &bindingDescription;
			vertexInputInfo.VertexAttributeDescriptionCount = (uint)attributeDescriptionsM.Length;
			vertexInputInfo.PVertexAttributeDescriptions = attributeDescriptions;

			PipelineInputAssemblyStateCreateInfo inputAssembly = new();
			inputAssembly.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
			inputAssembly.Topology = PrimitiveTopology.TriangleList;
			inputAssembly.PrimitiveRestartEnable = false;

			Viewport viewport = new();
			viewport.X = 0.0f;
			viewport.Y = 0.0f;
			viewport.Width = SwapChainExtent.Width;
			viewport.Height = SwapChainExtent.Height;
			viewport.MinDepth = 0.0f;
			viewport.MaxDepth = 1.0f;

			Rect2D scissor = new();
			scissor.Offset = new Offset2D { X = 0, Y = 0 };
			scissor.Extent = SwapChainExtent;

			PipelineViewportStateCreateInfo viewportState = new();
			viewportState.SType = StructureType.PipelineViewportStateCreateInfo;
			viewportState.ViewportCount = 1;
			viewportState.PViewports = &viewport;
			viewportState.ScissorCount = 1;
			viewportState.PScissors = &scissor;

			PipelineRasterizationStateCreateInfo rasterizer = new();
			rasterizer.SType = StructureType.PipelineRasterizationStateCreateInfo;
			rasterizer.DepthClampEnable = false;

			rasterizer.RasterizerDiscardEnable = false;

			rasterizer.PolygonMode = PolygonMode.Fill;

			rasterizer.LineWidth = 1.0f;

			rasterizer.CullMode = CullModeFlags.CullModeBackBit;
			rasterizer.FrontFace = FrontFace.CounterClockwise;

			rasterizer.DepthBiasEnable = false;
			rasterizer.DepthBiasConstantFactor = 0.0f; // Optional
			rasterizer.DepthBiasClamp = 0.0f; // Optional
			rasterizer.DepthBiasSlopeFactor = 0.0f; // Optional

			PipelineMultisampleStateCreateInfo multisampling = new();
			multisampling.SType = StructureType.PipelineMultisampleStateCreateInfo;
			multisampling.SampleShadingEnable = true;
			multisampling.RasterizationSamples = MsaaSamples;
			multisampling.MinSampleShading = 0.2f;
			multisampling.PSampleMask = null; // Optional
			multisampling.AlphaToCoverageEnable = false; // Optional
			multisampling.AlphaToOneEnable = false; // Optional

			PipelineColorBlendAttachmentState colorBlendAttachment = new();
			colorBlendAttachment.ColorWriteMask = ColorComponentFlags.ColorComponentRBit | ColorComponentFlags.ColorComponentGBit | ColorComponentFlags.ColorComponentBBit | ColorComponentFlags.ColorComponentABit;
			colorBlendAttachment.BlendEnable = false;
			colorBlendAttachment.SrcColorBlendFactor = BlendFactor.One; // Optional
			colorBlendAttachment.DstColorBlendFactor = BlendFactor.Zero; // Optional
			colorBlendAttachment.ColorBlendOp = BlendOp.Add; // Optional
			colorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One; // Optional
			colorBlendAttachment.DstAlphaBlendFactor = BlendFactor.Zero; // Optional
			colorBlendAttachment.AlphaBlendOp = BlendOp.Add; // Optional

			PipelineColorBlendStateCreateInfo colorBlending = new();
			colorBlending.SType = StructureType.PipelineColorBlendStateCreateInfo;
			colorBlending.LogicOpEnable = false;
			colorBlending.LogicOp = LogicOp.Copy; // Optional
			colorBlending.AttachmentCount = 1;
			colorBlending.PAttachments = &colorBlendAttachment;
			colorBlending.BlendConstants[0] = 0.0f; // Optional
			colorBlending.BlendConstants[1] = 0.0f; // Optional
			colorBlending.BlendConstants[2] = 0.0f; // Optional
			colorBlending.BlendConstants[3] = 0.0f; // Optional

			DynamicState* dynamicStates = stackalloc DynamicState[2];
			dynamicStates[0] = DynamicState.Viewport;
			dynamicStates[1] = DynamicState.LineWidth;

			PipelineDynamicStateCreateInfo dynamicState = new();
			dynamicState.SType = StructureType.PipelineDynamicStateCreateInfo;
			dynamicState.DynamicStateCount = 2;
			dynamicState.PDynamicStates = dynamicStates;

			PipelineDepthStencilStateCreateInfo depthStencil = new();
			depthStencil.SType = StructureType.PipelineDepthStencilStateCreateInfo;
			depthStencil.DepthTestEnable = true;
			depthStencil.DepthWriteEnable = true;

			depthStencil.DepthCompareOp = CompareOp.Less;

			depthStencil.DepthBoundsTestEnable = false;
			depthStencil.MinDepthBounds = 0.0f; // Optional
			depthStencil.MaxDepthBounds = 1.0f; // Optional

			depthStencil.StencilTestEnable = false;
			//depthStencil.Front = { }; // Optional
			//depthStencil.Back = { }; // Optional

			//
			//
			//
			PushConstantRange pushConstantRange = new();
			pushConstantRange.StageFlags = ShaderStageFlags.ShaderStageVertexBit;
			pushConstantRange.Offset = 0;
			pushConstantRange.Size = (uint)Marshal.SizeOf<PushConstantData>();

			PipelineLayoutCreateInfo pipelineLayoutInfo = new();
			pipelineLayoutInfo.SType = StructureType.PipelineLayoutCreateInfo;
			pipelineLayoutInfo.SetLayoutCount = 1;
			fixed (DescriptorSetLayout* descriptorSetLayout = &DescriptorSetLayout)
			{
				pipelineLayoutInfo.PSetLayouts = descriptorSetLayout;
			}

			pipelineLayoutInfo.PushConstantRangeCount = 1;
			pipelineLayoutInfo.PPushConstantRanges = &pushConstantRange;


			fixed (PipelineLayout* pipelineLayout = &PipelineLayout)
			{
				if (_vk.CreatePipelineLayout(Device, &pipelineLayoutInfo, null, pipelineLayout) != Result.Success)
				{
					Trace.TraceError("failed to create pipeline layout!");
					Console.ReadKey();
					return;
				}
			}

			GraphicsPipelineCreateInfo pipelineInfo = new();
			pipelineInfo.SType = StructureType.GraphicsPipelineCreateInfo;
			pipelineInfo.StageCount = 2;
			pipelineInfo.PStages = shaderStages;

			pipelineInfo.PVertexInputState = &vertexInputInfo;
			pipelineInfo.PInputAssemblyState = &inputAssembly;
			pipelineInfo.PViewportState = &viewportState;
			pipelineInfo.PRasterizationState = &rasterizer;
			pipelineInfo.PMultisampleState = &multisampling;
			pipelineInfo.PDepthStencilState = &depthStencil;
			pipelineInfo.PColorBlendState = &colorBlending;
			pipelineInfo.PDynamicState = null; // Optional

			pipelineInfo.Layout = PipelineLayout;

			pipelineInfo.RenderPass = RenderPass;
			pipelineInfo.Subpass = 0;

			pipelineInfo.BasePipelineHandle = default; // Optional
			pipelineInfo.BasePipelineIndex = -1; // Optional


			fixed (Pipeline* graphicsPipeline = &GraphicsPipeline)
			{
				if (_vk.CreateGraphicsPipelines(Device, default, 1, &pipelineInfo, null, graphicsPipeline) != Result.Success)
				{
					Trace.TraceError("failed to create graphics pipeline!");
					Console.ReadKey();
					return;
				}
			}

			_vk.DestroyShaderModule(Device, fragShaderModule, null);
			_vk.DestroyShaderModule(Device, vertShaderModule, null);
		}

		unsafe private void CreateFramebuffers()
		{
			SwapChainFramebuffers = new Framebuffer[SwapChainImageViews.Length];

			Parallel.For(0, SwapChainImageViews.Length, (i) =>
			{
				ImageView* attachments = stackalloc ImageView[3];
				attachments[0] = ColorImageView;
				attachments[1] = DepthImageView;
				attachments[2] = SwapChainImageViews[i];

				FramebufferCreateInfo framebufferInfo = new();
				framebufferInfo.SType = StructureType.FramebufferCreateInfo;
				framebufferInfo.RenderPass = RenderPass;
				framebufferInfo.AttachmentCount = 3;

				framebufferInfo.PAttachments = attachments;

				framebufferInfo.Width = SwapChainExtent.Width;
				framebufferInfo.Height = SwapChainExtent.Height;
				framebufferInfo.Layers = 1;

				fixed (Framebuffer* swapChainFramebuffer = &SwapChainFramebuffers[i])
				{
					if (_vk.CreateFramebuffer(Device, &framebufferInfo, null, swapChainFramebuffer) != Result.Success)
					{
						Trace.TraceError("failed to create framebuffer!");
						Console.ReadKey();
						return;
					}
				}
			});
			/*
			for (int i = 0; i < SwapChainImageViews.Length; i++)
			{
				ImageView* attachments = stackalloc ImageView[3];
				attachments[0] = ColorImageView;
				attachments[1] = DepthImageView;
				attachments[2] = SwapChainImageViews[i];

				FramebufferCreateInfo framebufferInfo = new();
				framebufferInfo.SType = StructureType.FramebufferCreateInfo;
				framebufferInfo.RenderPass = RenderPass;
				framebufferInfo.AttachmentCount = 3;

				framebufferInfo.PAttachments = attachments;

				framebufferInfo.Width = SwapChainExtent.Width;
				framebufferInfo.Height = SwapChainExtent.Height;
				framebufferInfo.Layers = 1;

				fixed (Framebuffer* swapChainFramebuffer = &SwapChainFramebuffers[i])
				{
					if (_vk.CreateFramebuffer(Device, &framebufferInfo, null, swapChainFramebuffer) != Result.Success)
					{
						Trace.TraceError("failed to create framebuffer!");
						Console.ReadKey();
						return;
					}
				}
			}*/
		}

		unsafe private void CreateCommandPool()
		{
			QueueFamilyIndices queueFamilyIndices = FindQueueFamilies(PhysicalDevice);

			CommandPoolCreateInfo poolInfo = new();
			poolInfo.SType = StructureType.CommandPoolCreateInfo;
			poolInfo.QueueFamilyIndex = (uint)queueFamilyIndices.GraphicsFamily;
			poolInfo.Flags = 0; // Optional

			fixed (CommandPool* commandPool = &CommandPool)
			{
				if (_vk.CreateCommandPool(Device, &poolInfo, null, commandPool) != Result.Success)
				{
					Trace.TraceError("failed to create command pool!");
					Console.ReadKey();
					return;
				}
			}

			//second!
			CommandPoolCreateInfo poolInfoSecond = new();
			poolInfoSecond.SType = StructureType.CommandPoolCreateInfo;
			poolInfoSecond.QueueFamilyIndex = (uint)queueFamilyIndices.GraphicsFamily;
			poolInfoSecond.Flags = CommandPoolCreateFlags.CommandPoolCreateTransientBit;

			fixed (CommandPool* commandPool = &CommandPoolSecond)
			{
				if (_vk.CreateCommandPool(Device, &poolInfoSecond, null, commandPool) != Result.Success)
				{
					Trace.TraceError("failed to create command pool second!");
					Console.ReadKey();
					return;
				}
			}
		}

		private void CreateColorResources()
		{
			Format colorFormat = SwapChainImageFormat;

			CreateImage((int)SwapChainExtent.Width, (int)SwapChainExtent.Height, 1, MsaaSamples, colorFormat, ImageTiling.Optimal, ImageUsageFlags.ImageUsageTransientAttachmentBit | ImageUsageFlags.ImageUsageColorAttachmentBit, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit, ref ColorImage, ref ColorImageMemory);
			CreateImageView(ColorImage, colorFormat, ImageAspectFlags.ImageAspectColorBit, 1, ref ColorImageView);
		}

		private void CreateDepthResources()
		{
			Format depthFormat = FindDepthFormat();

			CreateImage((int)SwapChainExtent.Width, (int)SwapChainExtent.Height, 1, MsaaSamples, depthFormat, ImageTiling.Optimal, ImageUsageFlags.ImageUsageDepthStencilAttachmentBit, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit, ref DepthImage, ref DepthImageMemory);

			CreateImageView(DepthImage, depthFormat, ImageAspectFlags.ImageAspectDepthBit, 1, ref DepthImageView);

			//TransitionImageLayout(DepthImage, depthFormat, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal, 1);
		}

		unsafe private void CreateTextureImage()
		{
			byte[] buffer = File.ReadAllBytes(TEXTURE_PATH);
			ImageResult image = ImageResult.FromMemory(buffer, ColorComponents.RedGreenBlueAlpha);

			ulong imageSize = (ulong)(image.Width * image.Height * 4);

			Silk.NET.Vulkan.Buffer stagingBuffer = new();
			DeviceMemory stagingBufferMemory = new();

			VulkanMemoryItem2 item = CreateBuffer(imageSize, BufferUsageFlags.BufferUsageTransferSrcBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

			VulkanMemoryChunk2 chunk = VulkanMemoryLocal2.ReturnChunk(item);

			void* data;
			//_vk.MapMemory(Device, stagingBufferMemory, 0, imageSize, 0, &data);
			_vk.MapMemory(Device, chunk.DeviceMemory, item.StartOffset, imageSize, 0, &data);
			Marshal.Copy(image.Data, 0, (IntPtr)data, image.Data.Length);
			//_vk.UnmapMemory(Device, stagingBufferMemory);
			_vk.UnmapMemory(Device, chunk.DeviceMemory);

			MipLevels = (uint)(Math.Floor(Math.Log2(Math.Max(image.Width, image.Height)))) + 1;

			CreateImage(image.Width, image.Height, MipLevels, SampleCountFlags.SampleCount1Bit, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageTransferDstBit | ImageUsageFlags.ImageUsageSampledBit, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit, ref TextureImage, ref TextureImageMemory);

			TransitionImageLayout(TextureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, MipLevels);

			CopyBufferToImage(stagingBuffer, TextureImage, image.Width, image.Height);

			//TransitionImageLayout(TextureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, MipLevels);

			_vk.DestroyBuffer(Device, stagingBuffer, null);
			//_vk.FreeMemory(Device, stagingBufferMemory, null);

			VulkanMemoryLocal2.FreeOne(ref _vk, ref Device, chunk, item);

			GenerateMipmaps(TextureImage, Format.R8G8B8A8Srgb, image.Width, image.Height, MipLevels);
		}

		unsafe private void CreateTextureImageView()
		{
			CreateImageView(TextureImage, Format.R8G8B8A8Srgb, ImageAspectFlags.ImageAspectColorBit, MipLevels, ref TextureImageView);
		}

		unsafe private void CreateTextureSampler()
		{
			SamplerCreateInfo samplerInfo = new();
			samplerInfo.SType = StructureType.SamplerCreateInfo;
			samplerInfo.MagFilter = Filter.Linear;
			samplerInfo.MinFilter = Filter.Linear;

			samplerInfo.AddressModeU = SamplerAddressMode.Repeat;
			samplerInfo.AddressModeV = SamplerAddressMode.Repeat;
			samplerInfo.AddressModeW = SamplerAddressMode.Repeat;

			samplerInfo.AnisotropyEnable = true;
			samplerInfo.MaxAnisotropy = MaxSamplerAnisotropy;

			samplerInfo.BorderColor = BorderColor.IntOpaqueBlack;

			samplerInfo.UnnormalizedCoordinates = false;

			samplerInfo.CompareEnable = false;
			samplerInfo.CompareOp = CompareOp.Always;

			samplerInfo.MipmapMode = SamplerMipmapMode.Linear;
			samplerInfo.MipLodBias = 0.0f;
			samplerInfo.MinLod = 0.0f;
			samplerInfo.MaxLod = MipLevels;

			fixed (Sampler* textureSampler = &TextureSampler)
			{
				if (_vk.CreateSampler(Device, &samplerInfo, null, textureSampler) != Result.Success)
				{
					Trace.TraceError("failed to create texture sampler!");
					Console.ReadKey();
					return;
				}
			}
		}

		unsafe private void LoadModel()
		{
			Stopwatch sw = new();
			sw.Start();
			Trace.WriteLine(sw.Elapsed);

			string[] lines = File.ReadAllLines(MODEL_PATH);

			int offsetV = Array.FindIndex(lines, row => row.StartsWith("v ")) - 1;
			int offsetT = Array.FindIndex(lines, row => row.StartsWith("vt ")) - 1;
			int offsetF = Array.FindIndex(lines, row => row.StartsWith("f "));
			int fCount = lines.Count(f => f.StartsWith("f "));

			Dictionary<Vertex, uint> vertexMapTrue = new();

			for (int i = 0; i < fCount; i++)
			{
				string[] line;
				line = lines[i + offsetF].Split(" ");
				if (line[0].Contains("s"))
				{
					offsetF++;
					line = lines[i + offsetF].Split(" ");
				}

				foreach (string s in line)
				{
					if (s.StartsWith("f"))
						continue;

					string[] el = s.Split("/");
					int index = offsetV + int.Parse(el[0]);
					int indexT = offsetT + int.Parse(el[1]);

					string[] vertexL = lines[index].Split(" ");
					string[] vertexTextL = lines[indexT].Split(" ");

					Vertex vertex;

					vertex.Pos = new Vector3()
					{
						X = float.Parse(vertexL[1], NumberStyles.Any, CultureInfo.InvariantCulture),
						Y = float.Parse(vertexL[2], NumberStyles.Any, CultureInfo.InvariantCulture),
						Z = float.Parse(vertexL[3], NumberStyles.Any, CultureInfo.InvariantCulture)
					};

					vertex.TexCoord = new Vector2
					{
						X = float.Parse(vertexTextL[1], NumberStyles.Any, CultureInfo.InvariantCulture),
						Y = 1.0f - float.Parse(vertexTextL[2], NumberStyles.Any, CultureInfo.InvariantCulture)
					};

					vertex.Color = new Vector3
					{
						X = 1.0f,
						Y = 1.0f,
						Z = 1.0f
					};

					if (vertexMapTrue.TryGetValue(vertex, out var meshIndex))
					{
						Indices.Add(meshIndex);
					}
					else
					{
						Indices.Add((uint)Vertices.Count);
						vertexMapTrue[vertex] = (uint)Vertices.Count;
						Vertices.Add(vertex);
					}
				}
			}
			/*
			Dictionary<Vertex, uint> vertexMapTrue = new();

			// Initialize
			Obj obj = new();
			// Read Wavefront OBJ file
			obj.LoadObj(MODEL_PATH);

			foreach (ObjParser.Types.Face face in obj.FaceList)
			{
				for (int i = 0; i < face.VertexIndexList.Length; i++)
				{
					int index = face.VertexIndexList[i];
					int indexText = face.TextureVertexIndexList[i];
					ObjParser.Types.Vertex vertexL = obj.VertexList.Find(v => v.Index == index);
					ObjParser.Types.TextureVertex vertexTextL = obj.TextureList.Find(v => v.Index == indexText);

					Vertex vertex;

					vertex.Pos = new Vector3()
					{
						X = (float)vertexL.X,
						Y = (float)vertexL.Y,
						Z = (float)vertexL.Z
					};

					vertex.TexCoord = new Vector2
					{
						X = (float)vertexTextL.X,
						Y = 1.0f - (float)vertexTextL.Y
					};

					vertex.Color = new Vector3
					{
						X = 1.0f,
						Y = 1.0f,
						Z = 1.0f
					};

					if (vertexMapTrue.TryGetValue(vertex, out var meshIndex))
					{
						Indices.Add(meshIndex);
					}
					else
					{
						Indices.Add((uint)Vertices.Count);
						vertexMapTrue[vertex] = (uint)Vertices.Count;
						Vertices.Add(vertex);
					}

				}
			}*/

			sw.Stop();
			Trace.WriteLine(sw.Elapsed);
		}

		unsafe private void CreateVertexBuffer()
		{
			ulong bufferSize = (ulong)(Marshal.SizeOf<Vertex>() * Vertices.Count);

			Silk.NET.Vulkan.Buffer stagingBuffer = new();
			DeviceMemory stagingBufferMemory = new();

			VulkanMemoryItem2 item = CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageTransferSrcBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);
			
			VulkanMemoryChunk2 chunk = VulkanMemoryLocal2.ReturnChunk(item);
			
			void* data;
			//_vk.MapMemory(Device, stagingBufferMemory, 0, bufferSize, 0, &data);
			_vk.MapMemory(Device, chunk.DeviceMemory, item.StartOffset, bufferSize, 0, &data);
			//https://github.com/dfkeenan/SilkVulkanTutorial/blob/1ca065ab812475262db24dc3629dc4ed0ec7111a/Source/27_ModelLoading/Program.cs#L1306
			Vertices.ToArray().AsSpan().CopyTo(new Span<Vertex>(data, Vertices.Count));

			//_vk.UnmapMemory(Device, stagingBufferMemory);
			_vk.UnmapMemory(Device, chunk.DeviceMemory);

			CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageTransferDstBit | BufferUsageFlags.BufferUsageVertexBufferBit, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit, ref VertexBuffer, ref VertexBufferMemory);

			CopyBuffer(stagingBuffer, VertexBuffer, bufferSize);

			_vk.DestroyBuffer(Device, stagingBuffer, null);

			//_vk.FreeMemory(Device, stagingBufferMemory, null);

			VulkanMemoryLocal2.FreeOne(ref _vk, ref Device, chunk, item);
		}

		unsafe private void CreateIndexBuffer()
		{
			ulong bufferSize = (ulong)(Marshal.SizeOf<uint>() * Indices.Count);

			Silk.NET.Vulkan.Buffer stagingBuffer = new();
			DeviceMemory stagingBufferMemory = new();
			
			VulkanMemoryItem2 item = CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageTransferSrcBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);
			
			VulkanMemoryChunk2 chunk = VulkanMemoryLocal2.ReturnChunk(item);

			void* data;
			//_vk.MapMemory(Device, stagingBufferMemory, 0, bufferSize, 0, &data);
			_vk.MapMemory(Device, chunk.DeviceMemory, item.StartOffset, bufferSize, 0, &data);
			Indices.ToArray().AsSpan().CopyTo(new Span<uint>(data, Indices.Count));

			//_vk.UnmapMemory(Device, stagingBufferMemory);
			_vk.UnmapMemory(Device, chunk.DeviceMemory);

			CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageTransferDstBit | BufferUsageFlags.BufferUsageIndexBufferBit, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit, ref IndexBuffer, ref IndexBufferMemory);

			CopyBuffer(stagingBuffer, IndexBuffer, bufferSize);

			_vk.DestroyBuffer(Device, stagingBuffer, null);
			//_vk.FreeMemory(Device, stagingBufferMemory, null);

			VulkanMemoryLocal2.FreeOne(ref _vk, ref Device, chunk, item);
		}

		unsafe private void CreateUniformBuffers()
		{
			ulong bufferSize = (ulong)Marshal.SizeOf<UniformBufferObject>();

			UniformBuffers = new Silk.NET.Vulkan.Buffer[SwapChainImages.Length];
			UniformBuffersMemory = new DeviceMemory[SwapChainImages.Length];
			//UniformBuffersChunksAndItens = new (VulkanMemoryChunk, VulkanMemoryItem)[SwapChainImages.Length];
			UniformBuffersItems = new VulkanMemoryItem2[SwapChainImages.Length];
			UniformBuffersChunks = new VulkanMemoryChunk2[SwapChainImages.Length];

			//Parallel.For(0, SwapChainImages.Length, (i) => 
			//{
			//	UniformBuffersChunksAndItens[i] = CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageUniformBufferBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit, ref UniformBuffers[i], ref UniformBuffersMemory[i]);
			//});

			for (int i = 0; i < SwapChainImages.Length; i++)
			{
				UniformBuffersItems[i] = CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageUniformBufferBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit, ref UniformBuffers[i], ref UniformBuffersMemory[i]);
				UniformBuffersChunks[i] = VulkanMemoryLocal2.ReturnChunk(UniformBuffersItems[i]); 
				//CreateBuffer(bufferSize, BufferUsageFlags.BufferUsageUniformBufferBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit, ref UniformBuffers[i], ref UniformBuffersMemory[i]);
			}
		}

		unsafe private void CreateDescriptorPool()
		{
			DescriptorPoolSize* poolSizes = stackalloc DescriptorPoolSize[2];
			poolSizes[0].Type = DescriptorType.UniformBuffer;
			poolSizes[0].DescriptorCount = (uint)SwapChainImages.Length;
			poolSizes[1].Type = DescriptorType.CombinedImageSampler;
			poolSizes[1].DescriptorCount = (uint)SwapChainImages.Length;

			DescriptorPoolCreateInfo poolInfo = new();
			poolInfo.SType = StructureType.DescriptorPoolCreateInfo;
			poolInfo.PoolSizeCount = 2;
			poolInfo.PPoolSizes = poolSizes;

			poolInfo.MaxSets = (uint)SwapChainImages.Length;

			fixed (DescriptorPool* descriptorPool = &DescriptorPool)
			{
				if (_vk.CreateDescriptorPool(Device, &poolInfo, null, descriptorPool) != Result.Success)
				{
					Trace.TraceError("failed to create descriptor pool!");
					Console.ReadKey();
					return;
				}
			}
		}

		unsafe private void CreateDescriptorSets()
		{
			DescriptorSetLayout* layouts = stackalloc DescriptorSetLayout[SwapChainImages.Length];

			Parallel.For(0, SwapChainImages.Length, (i) =>
			{
				layouts[i] = DescriptorSetLayout;
			});
			/*
			for (int i = 0; i < SwapChainImages.Length; i++)
			{
				layouts[i] = DescriptorSetLayout;
			}
			*/
			DescriptorSetAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
			allocInfo.DescriptorPool = DescriptorPool;
			allocInfo.DescriptorSetCount = (uint)SwapChainImages.Length;
			allocInfo.PSetLayouts = layouts;

			DescriptorSets = new DescriptorSet[SwapChainImages.Length];

			if (_vk.AllocateDescriptorSets(Device, &allocInfo, DescriptorSets) != Result.Success)
			{
				Trace.TraceError("failed to allocate descriptor sets!");
				Console.ReadKey();
				return;
			}

			Parallel.For(0, SwapChainImages.Length, (i) =>
			{
				DescriptorBufferInfo bufferInfo = new();
				bufferInfo.Buffer = UniformBuffers[i];
				bufferInfo.Offset = 0;
				bufferInfo.Range = (ulong)Marshal.SizeOf<UniformBufferObject>();

				DescriptorImageInfo imageInfo = new();
				imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
				imageInfo.ImageView = TextureImageView;
				imageInfo.Sampler = TextureSampler;

				int length = 2;

				WriteDescriptorSet* descriptorWrites = stackalloc WriteDescriptorSet[length];
				descriptorWrites[0].SType = StructureType.WriteDescriptorSet;
				descriptorWrites[0].DstSet = DescriptorSets[i];
				descriptorWrites[0].DstBinding = 0;
				descriptorWrites[0].DstArrayElement = 0;

				descriptorWrites[0].DescriptorType = DescriptorType.UniformBuffer;
				descriptorWrites[0].DescriptorCount = 1;

				descriptorWrites[0].PBufferInfo = &bufferInfo;

				descriptorWrites[1].SType = StructureType.WriteDescriptorSet;
				descriptorWrites[1].DstSet = DescriptorSets[i];
				descriptorWrites[1].DstBinding = 1;
				descriptorWrites[1].DstArrayElement = 0;

				descriptorWrites[1].DescriptorType = DescriptorType.CombinedImageSampler;
				descriptorWrites[1].DescriptorCount = 1;

				descriptorWrites[1].PImageInfo = &imageInfo;

				_vk.UpdateDescriptorSets(Device, (uint)length, descriptorWrites, 0, null);
			});
			/*
			for (int i = 0; i < SwapChainImages.Length; i++)
			{
				DescriptorBufferInfo bufferInfo = new();
				bufferInfo.Buffer = UniformBuffers[i];
				bufferInfo.Offset = 0;
				bufferInfo.Range = (ulong)Marshal.SizeOf<UniformBufferObject>();

				DescriptorImageInfo imageInfo = new();
				imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
				imageInfo.ImageView = TextureImageView;
				imageInfo.Sampler = TextureSampler;

				int length = 2;

				WriteDescriptorSet* descriptorWrites = stackalloc WriteDescriptorSet[length];
				descriptorWrites[0].SType = StructureType.WriteDescriptorSet;
				descriptorWrites[0].DstSet = DescriptorSets[i];
				descriptorWrites[0].DstBinding = 0;
				descriptorWrites[0].DstArrayElement = 0;

				descriptorWrites[0].DescriptorType = DescriptorType.UniformBuffer;
				descriptorWrites[0].DescriptorCount = 1;

				descriptorWrites[0].PBufferInfo = &bufferInfo;

				descriptorWrites[1].SType = StructureType.WriteDescriptorSet;
				descriptorWrites[1].DstSet = DescriptorSets[i];
				descriptorWrites[1].DstBinding = 1;
				descriptorWrites[1].DstArrayElement = 0;

				descriptorWrites[1].DescriptorType = DescriptorType.CombinedImageSampler;
				descriptorWrites[1].DescriptorCount = 1;

				descriptorWrites[1].PImageInfo = &imageInfo;

				_vk.UpdateDescriptorSets(Device, (uint)length, descriptorWrites, 0, null);
			}*/

		}

		unsafe private void CreateCommandBuffers()
		{
			CommandBuffers = new CommandBuffer[SwapChainFramebuffers.Length];

			CommandBufferAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.CommandBufferAllocateInfo;
			allocInfo.CommandPool = CommandPool;
			allocInfo.Level = CommandBufferLevel.Primary;
			allocInfo.CommandBufferCount = (uint)CommandBuffers.Length;

			if (_vk.AllocateCommandBuffers(Device, &allocInfo, CommandBuffers) != Result.Success)
			{
				Trace.TraceError("failed to allocate command buffers!");
				Console.ReadKey();
				return;
			}

			for (int i = 0; i < CommandBuffers.Length; i++)
			{
				CommandBufferBeginInfo beginInfo = new();
				beginInfo.SType = StructureType.CommandBufferBeginInfo;
				beginInfo.Flags = 0; // Optional
				beginInfo.PInheritanceInfo = null; // Optional

				if (_vk.BeginCommandBuffer(CommandBuffers[i], &beginInfo) != Result.Success)
				{
					Trace.TraceError("failed to begin recording command buffer!");
					Console.ReadKey();
					return;
				}

				RenderPassBeginInfo renderPassInfo = new();
				renderPassInfo.SType = StructureType.RenderPassBeginInfo;
				renderPassInfo.RenderPass = RenderPass;
				renderPassInfo.Framebuffer = SwapChainFramebuffers[i];

				renderPassInfo.RenderArea.Offset = new Offset2D { X = 0, Y = 0 };
				renderPassInfo.RenderArea.Extent = SwapChainExtent;

				ClearValue* clearValues = stackalloc ClearValue[2];
				clearValues[0].Color = new ClearColorValue() { Float32_0 = 0.0f, Float32_1 = 0.0f, Float32_2 = 0.0f, Float32_3 = 1.0f };
				clearValues[1].DepthStencil = new ClearDepthStencilValue() { Depth = 1.0f, Stencil = 0 };

				renderPassInfo.ClearValueCount = 2;
				renderPassInfo.PClearValues = clearValues;

				_vk.CmdBeginRenderPass(CommandBuffers[i], &renderPassInfo, SubpassContents.Inline);
				_vk.CmdBindPipeline(CommandBuffers[i], PipelineBindPoint.Graphics, GraphicsPipeline);

				Silk.NET.Vulkan.Buffer[] vertexBuffers = { VertexBuffer };

				//
				//
				//
				ulong* offsets = (ulong*)Marshal.AllocHGlobal(Marshal.SizeOf<ulong>());
				offsets[0] = 0;

				_vk.CmdBindVertexBuffers(CommandBuffers[i], 0, 1, vertexBuffers, offsets);
				_vk.CmdBindIndexBuffer(CommandBuffers[i], IndexBuffer, 0, IndexType.Uint32);

				fixed (DescriptorSet* descriptorSet = &DescriptorSets[i])
				{
					_vk.CmdBindDescriptorSets(CommandBuffers[i], PipelineBindPoint.Graphics, PipelineLayout, 0, 1, descriptorSet, 0, null);
				}

				fixed (PushConstantData* data = &Data)
				{
					_vk.CmdPushConstants(
					CommandBuffers[i],
					PipelineLayout,
					ShaderStageFlags.ShaderStageVertexBit,
					0,
					(uint)Marshal.SizeOf<PushConstantData>(),
					data);
				}
				

				_vk.CmdDrawIndexed(CommandBuffers[i], (uint)Indices.Count, 1, 0, 0, 0);

				_vk.CmdEndRenderPass(CommandBuffers[i]);

				if (_vk.EndCommandBuffer(CommandBuffers[i]) != Result.Success)
				{
					Trace.TraceError("failed to record command buffer!");
					Console.ReadKey();
					return;
				}

				Marshal.FreeHGlobal((IntPtr)offsets);
			}

		}

		unsafe private void CreateSyncObjects()
		{
			ImageAvailableSemaphores = new Silk.NET.Vulkan.Semaphore[MAX_FRAMES_IN_FLIGHT];
			RenderFinishedSemaphores = new Silk.NET.Vulkan.Semaphore[MAX_FRAMES_IN_FLIGHT];

			InFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
			ImagesInFlight = new Fence[SwapChainImages.Length];

			SemaphoreCreateInfo semaphoreInfo = new();
			semaphoreInfo.SType = StructureType.SemaphoreCreateInfo;

			FenceCreateInfo fenceInfo = new();
			fenceInfo.SType = StructureType.FenceCreateInfo;
			fenceInfo.Flags = FenceCreateFlags.FenceCreateSignaledBit;

			for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
			{
				fixed (Silk.NET.Vulkan.Semaphore* imageAvailableSemaphore = &ImageAvailableSemaphores[i], renderFinishedSemaphore = &RenderFinishedSemaphores[i])
				{
					if (_vk.CreateSemaphore(Device, &semaphoreInfo, null, imageAvailableSemaphore) != Result.Success || _vk.CreateSemaphore(Device, &semaphoreInfo, null, renderFinishedSemaphore) != Result.Success)
					{
						Trace.TraceError("failed to create semaphores!");
						Console.ReadKey();
						return;
					}
				}

				fixed (Fence* inFlightFences = &InFlightFences[i])
				{
					if (_vk.CreateFence(Device, &fenceInfo, null, inFlightFences) != Result.Success)
					{
						Trace.TraceError("failed to create fence!");
						Console.ReadKey();
						return;
					}
				}
			}
		}

		unsafe private void CreateImage(int width, int height, uint mipLevels, SampleCountFlags numSamples, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ref Image image, ref DeviceMemory imageMemory)
		{
			ImageCreateInfo imageInfo = new();

			imageInfo.SType = StructureType.ImageCreateInfo;
			imageInfo.ImageType = ImageType.ImageType2D;
			imageInfo.Extent.Width = (uint)width;
			imageInfo.Extent.Height = (uint)height;
			imageInfo.Extent.Depth = 1;
			imageInfo.MipLevels = mipLevels;
			imageInfo.ArrayLayers = 1;

			imageInfo.Format = format;

			imageInfo.Tiling = tiling;

			imageInfo.InitialLayout = ImageLayout.Undefined;

			imageInfo.Usage = usage;

			imageInfo.SharingMode = SharingMode.Exclusive;

			imageInfo.Samples = numSamples;
			imageInfo.Flags = 0; // Optional
			
			fixed (Image* textureImage = &image)
			{
				if (_vk.CreateImage(Device, &imageInfo, null, textureImage) != Result.Success)
				{
					Trace.TraceError("failed to create image!");
					Console.ReadKey();
					return;
				}

			}

			//MemoryRequirements memRequirements;
			//_vk.GetImageMemoryRequirements(Device, image, &memRequirements);
			/*
			if (Test)
			{
				MemoryAllocateInfo allocInfo = new();
				allocInfo.SType = StructureType.MemoryAllocateInfo;
				allocInfo.AllocationSize = memRequirements.Size + 17694720 + 5594112 + 100000;
				allocInfo.MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties);

				fixed (DeviceMemory* textureImageMemory = &imageMemory)
				{
					if (_vk.AllocateMemory(Device, &allocInfo, null, textureImageMemory) != Result.Success)
					{
						Trace.TraceError("failed to allocate image memory!");
						Console.ReadKey();
						return;
					}

				}
				_vk.BindImageMemory(Device, image, imageMemory, 0);
				Test = false;
			}
			else if (Test2)
			{
				_vk.BindImageMemory(Device, image, ColorImageMemory, 17694720);
				Test2 = false;
			}
			else 
			{
				_vk.BindImageMemory(Device, image, ColorImageMemory, 17694720+ 17694720);
			}*/

			/*
			MemoryAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.MemoryAllocateInfo;
			allocInfo.AllocationSize = memRequirements.Size;
			allocInfo.MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties);

			fixed (DeviceMemory* textureImageMemory = &imageMemory)
			{
				if (_vk.AllocateMemory(Device, &allocInfo, null, textureImageMemory) != Result.Success)
				{
					Trace.TraceError("failed to allocate image memory!");
					Console.ReadKey();
					return;
				}

			}
			_vk.BindImageMemory(Device, image, imageMemory, 0);
			*/
			//VulkanMemoryLocal.BindImage(ref _vk, ref Device, ref image, properties);
			VulkanMemoryLocal2.BindImageOrBuffer(ref _vk, ref Device, image, properties);
		}

		unsafe private void CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags, uint mipLevels, ref ImageView imageView)
		{
			ImageViewCreateInfo viewInfo = new();
			viewInfo.SType = StructureType.ImageViewCreateInfo;
			viewInfo.Image = image;
			viewInfo.ViewType = ImageViewType.ImageViewType2D;
			viewInfo.Format = format;
			viewInfo.SubresourceRange.AspectMask = aspectFlags;
			viewInfo.SubresourceRange.BaseMipLevel = 0;
			viewInfo.SubresourceRange.LevelCount = mipLevels;
			viewInfo.SubresourceRange.BaseArrayLayer = 0;
			viewInfo.SubresourceRange.LayerCount = 1;

			ImageView imageViewL;

			if (_vk.CreateImageView(Device, &viewInfo, null, &imageViewL) != Result.Success)
			{
				Trace.TraceError("failed to create texture image view!");
				Console.ReadKey();
				return;
			}

			imageView = imageViewL;
		}

		unsafe private VulkanMemoryItem2 CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Silk.NET.Vulkan.Buffer buffer, ref DeviceMemory bufferMemory)
		{
			BufferCreateInfo bufferInfo = new();
			bufferInfo.SType = StructureType.BufferCreateInfo;

			bufferInfo.Size = size;

			bufferInfo.Usage = usage;

			bufferInfo.SharingMode = SharingMode.Exclusive;

			fixed (Silk.NET.Vulkan.Buffer* vertexBuffer = &buffer)
			{
				if (_vk.CreateBuffer(Device, &bufferInfo, null, vertexBuffer) != Result.Success)
				{
					Trace.TraceError("failed to create vertex buffer!");
					Console.ReadKey();
				}
			}

			/*
			MemoryRequirements memRequirements;
			_vk.GetBufferMemoryRequirements(Device, buffer, &memRequirements);

			MemoryAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.MemoryAllocateInfo;
			allocInfo.AllocationSize = memRequirements.Size;
			allocInfo.MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties);

			fixed (DeviceMemory* bMemory = &bufferMemory)
			{
				if (_vk.AllocateMemory(Device, &allocInfo, null, bMemory) != Result.Success)
				{
					Trace.TraceError("failed to allocate vertex buffer memory!");
					Console.ReadKey();
					return;
				}
			}

			_vk.BindBufferMemory(Device, buffer, bufferMemory, 0);
			*/
			//(VulkanMemoryChunk, VulkanMemoryItem) ChunkAndItem = VulkanMemoryLocal.BindBuffer(ref _vk, ref Device, ref buffer, properties);
			VulkanMemoryItem2 item = VulkanMemoryLocal2.BindImageOrBuffer(ref _vk, ref Device, buffer, properties);
			return item;
		}

		unsafe private void CopyBuffer(Silk.NET.Vulkan.Buffer srcBuffer, Silk.NET.Vulkan.Buffer dstBuffer, ulong size)
		{
			CommandBuffer commandBuffer = BeginSingleTimeCommands();

			BufferCopy copyRegion = new();
			copyRegion.SrcOffset = 0; // Optional
			copyRegion.DstOffset = 0; // Optional
			copyRegion.Size = size;
			_vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);

			EndSingleTimeCommands(commandBuffer);
		}

		unsafe private CommandBuffer BeginSingleTimeCommands()
		{
			CommandBufferAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.CommandBufferAllocateInfo;
			allocInfo.Level = CommandBufferLevel.Primary;
			allocInfo.CommandPool = CommandPoolSecond;
			allocInfo.CommandBufferCount = 1;

			CommandBuffer commandBuffer;
			_vk.AllocateCommandBuffers(Device, &allocInfo, &commandBuffer);

			CommandBufferBeginInfo beginInfo = new();
			beginInfo.SType = StructureType.CommandBufferBeginInfo;
			beginInfo.Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit;

			_vk.BeginCommandBuffer(commandBuffer, &beginInfo);

			return commandBuffer;
		}

		unsafe private void EndSingleTimeCommands(CommandBuffer commandBuffer)
		{
			_vk.EndCommandBuffer(commandBuffer);

			SubmitInfo submitInfo = new();
			submitInfo.SType = StructureType.SubmitInfo;
			submitInfo.CommandBufferCount = 1;
			submitInfo.PCommandBuffers = &commandBuffer;

			_vk.QueueSubmit(GraphicsQueue, 1, &submitInfo, default);
			_vk.QueueWaitIdle(GraphicsQueue);

			_vk.FreeCommandBuffers(Device, CommandPoolSecond, 1, &commandBuffer);
		}

		unsafe private void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout, uint mipLevels)
		{
			CommandBuffer commandBuffer = BeginSingleTimeCommands();

			ImageMemoryBarrier barrier = new();
			barrier.SType = StructureType.ImageMemoryBarrier;
			barrier.OldLayout = oldLayout;
			barrier.NewLayout = newLayout;

			barrier.SrcQueueFamilyIndex = Vk.QueueFamilyIgnored;
			barrier.DstQueueFamilyIndex = Vk.QueueFamilyIgnored;

			barrier.Image = image;

			if (newLayout == ImageLayout.DepthStencilAttachmentOptimal)
			{
				barrier.SubresourceRange.AspectMask = ImageAspectFlags.ImageAspectDepthBit;

				if (HasStencilComponent(format))
				{
					barrier.SubresourceRange.AspectMask |= ImageAspectFlags.ImageAspectStencilBit;
				}
			}
			else
			{
				barrier.SubresourceRange.AspectMask = ImageAspectFlags.ImageAspectColorBit;
			}

			barrier.SubresourceRange.BaseMipLevel = 0;
			barrier.SubresourceRange.LevelCount = mipLevels;
			barrier.SubresourceRange.BaseArrayLayer = 0;
			barrier.SubresourceRange.LayerCount = 1;

			PipelineStageFlags sourceStage;
			PipelineStageFlags destinationStage;

			if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
			{
				barrier.SrcAccessMask = 0;
				barrier.DstAccessMask = AccessFlags.AccessTransferWriteBit;

				sourceStage = PipelineStageFlags.PipelineStageTopOfPipeBit;
				destinationStage = PipelineStageFlags.PipelineStageTransferBit;
			}
			else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
			{
				barrier.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
				barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;

				sourceStage = PipelineStageFlags.PipelineStageTransferBit;
				destinationStage = PipelineStageFlags.PipelineStageFragmentShaderBit;
			}
			else if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.StencilAttachmentOptimal)
			{
				barrier.SrcAccessMask = 0;
				barrier.DstAccessMask = AccessFlags.AccessDepthStencilAttachmentReadBit | AccessFlags.AccessDepthStencilAttachmentWriteBit;

				sourceStage = PipelineStageFlags.PipelineStageTopOfPipeBit;
				destinationStage = PipelineStageFlags.PipelineStageEarlyFragmentTestsBit;
			}
			else
			{
				Trace.TraceError("unsupported layout transition!");
				Console.ReadKey();
				return;
			}

			_vk.CmdPipelineBarrier(
				commandBuffer,
				sourceStage, destinationStage,
				0,
				0, null,
				0, null,
				1, &barrier
			);

			EndSingleTimeCommands(commandBuffer);
		}

		unsafe private void CopyBufferToImage(Silk.NET.Vulkan.Buffer buffer, Image image, int width, int height)
		{
			CommandBuffer commandBuffer = BeginSingleTimeCommands();

			BufferImageCopy region = new();
			region.BufferOffset = 0;
			region.BufferRowLength = 0;
			region.BufferImageHeight = 0;

			region.ImageSubresource.AspectMask = ImageAspectFlags.ImageAspectColorBit;
			region.ImageSubresource.MipLevel = 0;
			region.ImageSubresource.BaseArrayLayer = 0;
			region.ImageSubresource.LayerCount = 1;

			region.ImageOffset = new Offset3D
			{
				X = 0,
				Y = 0,
				Z = 0
			};
			region.ImageExtent = new Extent3D
			{
				Width = (uint)width,
				Height = (uint)height,
				Depth = 1
			};

			_vk.CmdCopyBufferToImage(
				commandBuffer,
				buffer,
				image,
				ImageLayout.TransferDstOptimal,
				1,
				&region);

			EndSingleTimeCommands(commandBuffer);
		}

		unsafe private void GenerateMipmaps(Image image, Format imageFormat, int texWidth, int texHeight, uint mipLevels)
		{
			FormatProperties formatProperties;
			_vk.GetPhysicalDeviceFormatProperties(PhysicalDevice, imageFormat, &formatProperties);

			if ((formatProperties.OptimalTilingFeatures & FormatFeatureFlags.FormatFeatureSampledImageFilterLinearBit) == 0)
			{
				throw new Exception("texture image format does not support linear blitting!");
			}

			CommandBuffer commandBuffer = BeginSingleTimeCommands();

			ImageMemoryBarrier barrier = new();
			barrier.SType = StructureType.ImageMemoryBarrier;
			barrier.Image = image;
			barrier.SrcQueueFamilyIndex = Vk.QueueFamilyIgnored;
			barrier.DstQueueFamilyIndex = Vk.QueueFamilyIgnored;
			barrier.SubresourceRange.AspectMask = ImageAspectFlags.ImageAspectColorBit;
			barrier.SubresourceRange.BaseArrayLayer = 0;
			barrier.SubresourceRange.LayerCount = 1;
			barrier.SubresourceRange.LevelCount = 1;

			int mipWidth = texWidth;
			int mipHeight = texHeight;

			for (uint i = 1; i < mipLevels; i++)
			{
				barrier.SubresourceRange.BaseMipLevel = i - 1;
				barrier.OldLayout = ImageLayout.TransferDstOptimal;
				barrier.NewLayout = ImageLayout.TransferSrcOptimal;
				barrier.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
				barrier.DstAccessMask = AccessFlags.AccessTransferReadBit;

				_vk.CmdPipelineBarrier(commandBuffer,
					 PipelineStageFlags.PipelineStageTransferBit, PipelineStageFlags.PipelineStageTransferBit, 0,
					0, null,
					0, null,
					1, &barrier);

				ImageBlit blit = new();
				blit.SrcOffsets[0] = new Offset3D { X = 0, Y = 0, Z = 0 };
				blit.SrcOffsets[1] = new Offset3D { X = mipWidth, Y = mipHeight, Z = 1 };
				blit.SrcSubresource.AspectMask = ImageAspectFlags.ImageAspectColorBit;
				blit.SrcSubresource.MipLevel = i - 1;
				blit.SrcSubresource.BaseArrayLayer = 0;
				blit.SrcSubresource.LayerCount = 1;
				blit.DstOffsets[0] = new Offset3D(0, 0, 0);
				blit.DstOffsets[1] = new Offset3D(mipWidth > 1 ? mipWidth / 2 : 1, mipHeight > 1 ? mipHeight / 2 : 1, 1);
				blit.DstSubresource.AspectMask = ImageAspectFlags.ImageAspectColorBit;
				blit.DstSubresource.MipLevel = i;
				blit.DstSubresource.BaseArrayLayer = 0;
				blit.DstSubresource.LayerCount = 1;

				_vk.CmdBlitImage(commandBuffer,
					image, ImageLayout.TransferSrcOptimal,
					image, ImageLayout.TransferDstOptimal,
					1, &blit,
					Filter.Linear);

				barrier.OldLayout = ImageLayout.TransferSrcOptimal;
				barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
				barrier.SrcAccessMask = AccessFlags.AccessTransferReadBit;
				barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;

				_vk.CmdPipelineBarrier(commandBuffer,
					 PipelineStageFlags.PipelineStageTransferBit, PipelineStageFlags.PipelineStageFragmentShaderBit, 0,
					0, null,
					0, null,
					1, &barrier);

				if (mipWidth > 1) mipWidth /= 2;
				if (mipHeight > 1) mipHeight /= 2;
			}

			barrier.SubresourceRange.BaseMipLevel = mipLevels - 1;
			barrier.OldLayout = ImageLayout.TransferDstOptimal;
			barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
			barrier.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
			barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;

			_vk.CmdPipelineBarrier(commandBuffer,
				 PipelineStageFlags.PipelineStageTransferBit, PipelineStageFlags.PipelineStageFragmentShaderBit, 0,
				0, null,
				0, null,
				1, &barrier);

			EndSingleTimeCommands(commandBuffer);
		}

		private void RecreateSwapChain()
		{
			if (Minimized)
				return;

			_vk.DeviceWaitIdle(Device);

			CleanupSwapChain();

			CreateSwapChain();
			CreateImageViews();
			CreateRenderPass();
			CreateGraphicsPipeline();
			CreateColorResources();
			CreateDepthResources();
			CreateFramebuffers();
			CreateUniformBuffers();
			CreateDescriptorPool();
			CreateDescriptorSets();
			CreateCommandBuffers();
		}
		
		unsafe private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
		{
			PhysicalDeviceMemoryProperties memProperties;
			_vk.GetPhysicalDeviceMemoryProperties(PhysicalDevice, &memProperties);

			//https://github.com/jcant0n/VulkanSharp_Tutorials/blob/86f289c3cf547de7e08c6a4e1200f01cf21ca2d8/VKVertexBuffers/VulkanRenderer.cs#L678

			for (int i = 0; i < memProperties.MemoryTypeCount; ++i)
			{
				uint v = (uint)(typeFilter & (1 << i));
				MemoryPropertyFlags v1 = memProperties.MemoryTypes[i].PropertyFlags & properties;
				if (v != 0 && v1 != 0)
				{
					return (uint)i;
				}
			}

			throw new Exception("failed to find suitable memory type!");
		}
		
		unsafe private ShaderModule CreateShaderModule(byte[] code)
		{
			ShaderModuleCreateInfo createInfo = new();
			createInfo.SType = StructureType.ShaderModuleCreateInfo;
			createInfo.CodeSize = (nuint)code.Length;

			//https://github.com/dotnet/Silk.NET/blob/22696597d134d809c8ed5f35f181cda980cd8520/src/Lab/Experiments/VulkanTriangle/HelloTriangleApplication.cs#L1052
			fixed (byte* codePtr = code)
			{
				createInfo.PCode = (uint*)codePtr;
			}

			ShaderModule shaderModule;

			//test in and out
			if (_vk.CreateShaderModule(Device, in createInfo, null, out shaderModule) != Result.Success)
			{
				Trace.TraceError("failed to create shader module!");
				Console.ReadKey();
			}

			return shaderModule;
		}

		unsafe private SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device)
		{
			SwapChainSupportDetails details = new();

			_vkSurface?.GetPhysicalDeviceSurfaceCapabilities(device, Surface, &details.Capabilities);

			uint formatCount = 0;
			_vkSurface?.GetPhysicalDeviceSurfaceFormats(device, Surface, &formatCount, null);

			if (formatCount != 0)
			{
				details.Formats = new SurfaceFormatKHR[formatCount];
				_vkSurface?.GetPhysicalDeviceSurfaceFormats(device, Surface, &formatCount, details.Formats);
			}

			uint presentModeCount = 0;
			_vkSurface?.GetPhysicalDeviceSurfacePresentModes(device, Surface, &presentModeCount, null);

			if (presentModeCount != 0)
			{
				details.PresentModes = new PresentModeKHR[presentModeCount];
				_vkSurface?.GetPhysicalDeviceSurfacePresentModes(device, Surface, &presentModeCount, details.PresentModes);
			}

			return details;
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
				SDL.SDL_Vulkan_GetDrawableSize(Window, out int width, out int height);

				Extent2D actualExtent = new();
				actualExtent.Width = (uint)width;
				actualExtent.Height = (uint)height;
				Trace.WriteLine("actualExtent1: " + actualExtent.Height + ":" + actualExtent.Width);

				//https://github.com/dotnet/Silk.NET/blob/22696597d134d809c8ed5f35f181cda980cd8520/src/Lab/Experiments/VulkanTriangle/HelloTriangleApplication.cs#L761
				actualExtent.Width = new[]
				{
				capabilities.MinImageExtent.Width,
				new[] {capabilities.MaxImageExtent.Width, actualExtent.Width}.Min()
			}.Max();

				actualExtent.Height = new[]
				{
				capabilities.MinImageExtent.Height,
				new[] {capabilities.MaxImageExtent.Height, actualExtent.Height}.Min()
			}.Max();
				Trace.WriteLine("actualExtent2: " + actualExtent.Height + ":" + actualExtent.Width);

				return actualExtent;
			}
		}

		unsafe private bool IsDeviceSuitable(PhysicalDevice device)
		{
			//https://vulkan-tutorial.com/en/Drawing_a_triangle/Setup/Physical_devices_and_queue_families

			PhysicalDeviceProperties deviceProperties;
			_vk.GetPhysicalDeviceProperties(device, &deviceProperties);

			Trace.WriteLine($"Device Name: {Marshal.PtrToStringAnsi((IntPtr)deviceProperties.DeviceName)}");
			Trace.WriteLine($"MaxMemoryAllocationCount: {deviceProperties.Limits.MaxMemoryAllocationCount}");
			Trace.WriteLine($"MaxSamplerAnisotropy: {deviceProperties.Limits.MaxSamplerAnisotropy}");
			MaxSamplerAnisotropy = deviceProperties.Limits.MaxSamplerAnisotropy;

			PhysicalDeviceFeatures deviceFeatures;
			_vk.GetPhysicalDeviceFeatures(device, &deviceFeatures);

			//PhysicalDeviceMemoryProperties physicalDeviceMemoryProperties;
			//_vk.GetPhysicalDeviceMemoryProperties(device, &physicalDeviceMemoryProperties);

			QueueFamilyIndices indices = FindQueueFamilies(device);

			bool extensionsSupported = CheckDeviceExtensionSupport(device);

			bool swapChainAdequate = false;
			if (extensionsSupported)
			{
				SwapChainSupportDetails swapChainSupport = QuerySwapChainSupport(device);
				swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
			}

			return indices.IsComplete() && extensionsSupported && swapChainAdequate && deviceFeatures.SamplerAnisotropy;
		}

		unsafe private bool CheckDeviceExtensionSupport(PhysicalDevice device)
		{
			uint extensionCount;
			_vk.EnumerateDeviceExtensionProperties(device, "", &extensionCount, null);

			ExtensionProperties[] availableExtensions = new ExtensionProperties[extensionCount];
			_vk.EnumerateDeviceExtensionProperties(device, "", &extensionCount, availableExtensions);

			List<string> requiredExtensions = DeviceExtensions.ToList();

			Trace.WriteLine($"Available Extensions: {availableExtensions.Length}");
			foreach (ExtensionProperties extension in availableExtensions)
			{
				//Trace.WriteLine(Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName));
				requiredExtensions.Remove(Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName));
			}

			if (requiredExtensions.Count == 0)
				return true;
			else
				return false;
		}

		unsafe private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
		{
			QueueFamilyIndices indices = new();

			uint queueFamilyCount = 0;
			_vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

			QueueFamilyProperties[] queueFamilies = new QueueFamilyProperties[queueFamilyCount];
			_vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies);

			uint i = 0;
			foreach (QueueFamilyProperties queueFamily in queueFamilies)
			{
				if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
					indices.GraphicsFamily = i;

				Bool32 presentSupport = false;
				_vkSurface?.GetPhysicalDeviceSurfaceSupport(device, i, Surface, &presentSupport);

				if (presentSupport)
					indices.PresentFamily = i;

				if (indices.IsComplete())
					break;

				i++;
			}

			return indices;
		}

		unsafe private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
		{
			createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
			createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt;
			createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt;
			createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
			createInfo.PUserData = null; // Optional
		}

		private unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
		{
			if (messageSeverity > DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt)
			{
				Trace.Indent();
				Trace.WriteLine($"{messageSeverity} {messageTypes}" + Marshal.PtrToStringAnsi((IntPtr)pCallbackData->PMessage));
				Trace.WriteLine("");
				Trace.Unindent();
			}
			return Vk.False;
		}

		unsafe private bool CheckValidationLayerSupport()
		{
			uint layerCount;
			_vk.EnumerateInstanceLayerProperties(&layerCount, null);
			LayerProperties* availableLayers = stackalloc LayerProperties[(int)layerCount];
			_vk.EnumerateInstanceLayerProperties(&layerCount, availableLayers);

			for (int i = 0; i < layerCount; i++)
			{
				Trace.WriteLine($"Layer: {Marshal.PtrToStringAnsi((IntPtr)availableLayers[i].LayerName)} | version: {availableLayers[i].SpecVersion}");
			}

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

		unsafe private void GetAllInstanceExtensions()
		{
			uint extensionCount;
			_vk.EnumerateInstanceExtensionProperties("", &extensionCount, null);
			ExtensionProperties* extensions = stackalloc ExtensionProperties[(int)extensionCount];
			_vk.EnumerateInstanceExtensionProperties("", &extensionCount, extensions);

			for (int i = 0; i < extensionCount; i++)
			{
				//Trace.WriteLine($"Extension: {GetString(extensions[i].ExtensionName)} | version: {extensions[i].SpecVersion}");
				Trace.WriteLine($"Extension: {Marshal.PtrToStringAnsi((IntPtr)extensions[i].ExtensionName)} | version: {extensions[i].SpecVersion}");
			}
		}

		unsafe private Format FindSupportedFormat(Format[] candidates, ImageTiling tiling, FormatFeatureFlags features)
		{
			foreach (Format format in candidates)
			{
				FormatProperties props;
				_vk.GetPhysicalDeviceFormatProperties(PhysicalDevice, format, &props);

				if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
				{
					return format;
				}
				else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
				{
					return format;
				}
			}

			throw new Exception("failed to find supported format!");
		}

		private Format FindDepthFormat()
		{
			return FindSupportedFormat(new Format[3] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint }, ImageTiling.Optimal, FormatFeatureFlags.FormatFeatureDepthStencilAttachmentBit);
		}

		private bool HasStencilComponent(Format format)
		{
			return format == Format.D32SfloatS8Uint || format == Format.D24UnormS8Uint;
		}

		unsafe private SampleCountFlags GetMaxUsableSampleCount()
		{
			PhysicalDeviceProperties physicalDeviceProperties;
			_vk.GetPhysicalDeviceProperties(PhysicalDevice, &physicalDeviceProperties);

			SampleCountFlags counts = physicalDeviceProperties.Limits.FramebufferColorSampleCounts & physicalDeviceProperties.Limits.FramebufferDepthSampleCounts;

			Trace.WriteLine("MaxUsableSampleCount(MSAA): " + counts.ToString());

			if ((counts & SampleCountFlags.SampleCount64Bit) != 0)
				return SampleCountFlags.SampleCount64Bit;
			if ((counts & SampleCountFlags.SampleCount32Bit) != 0)
				return SampleCountFlags.SampleCount32Bit;
			if ((counts & SampleCountFlags.SampleCount16Bit) != 0)
				return SampleCountFlags.SampleCount16Bit;
			if ((counts & SampleCountFlags.SampleCount8Bit) != 0)
				return SampleCountFlags.SampleCount8Bit;
			if ((counts & SampleCountFlags.SampleCount4Bit) != 0)
				return SampleCountFlags.SampleCount4Bit;
			if ((counts & SampleCountFlags.SampleCount2Bit) != 0)
				return SampleCountFlags.SampleCount2Bit;

			return SampleCountFlags.SampleCount1Bit;
		}

		unsafe private void MainLoop()
		{
			bool test = true;
			while (test)
			{
				if (SDL.SDL_PollEvent(out SDL.SDL_Event test_event) == 1)
				{
					if (test_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
					{
						Trace.WriteLine($"Window {test_event.window.windowID} closed");
						test = false;
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
					if (test_event.type == SDL.SDL_EventType.SDL_KEYDOWN) 
					{
						switch (test_event.key.keysym.sym)
						{
							case SDL.SDL_Keycode.SDLK_LEFT:
								Trace.WriteLine(SDL.SDL_Keycode.SDLK_LEFT);
								Data.Position = new Vector4(Data.Position.X+1f, Data.Position.Y, Data.Position.Z, Data.Position.W);
								break;
							default:
								break;
						}
					}
				}

				DrawFrame();
			}

			_vk.DeviceWaitIdle(Device);
		}

		unsafe private void DrawFrame()
		{
			fixed (Fence* inFlightFences = &InFlightFences[CurrentFrame])
			{
				_vk.WaitForFences(Device, 1, inFlightFences, true, uint.MaxValue);
			}

			uint imageIndex;
			Result result = _vkSwapchain.AcquireNextImage(Device, SwapChain, uint.MaxValue, ImageAvailableSemaphores[CurrentFrame], default, &imageIndex);

			if (result == Result.ErrorOutOfDateKhr)
			{
				RecreateSwapChain();
				return;
			}
			else if (result != Result.Success && result != Result.SuboptimalKhr)
			{
				Trace.TraceError("failed to acquire swap chain image!");
				Console.ReadKey();
				return;
			}

			// Check if a previous frame is using this image (i.e. there is its fence to wait on)
			if (ImagesInFlight[imageIndex].Handle != 0)
			{
				fixed (Fence* imagesInFlight = &ImagesInFlight[imageIndex])
				{
					_vk.WaitForFences(Device, 1, imagesInFlight, true, uint.MaxValue);
				}
			}
			// Mark the image as now being in use by this frame
			ImagesInFlight[imageIndex] = InFlightFences[CurrentFrame];

			UpdateUniformBuffer(imageIndex);

			SubmitInfo submitInfo = new();
			submitInfo.SType = StructureType.SubmitInfo;

			Silk.NET.Vulkan.Semaphore* waitSemaphores = stackalloc[] { ImageAvailableSemaphores[CurrentFrame] };
			PipelineStageFlags* waitStages = stackalloc[] { PipelineStageFlags.PipelineStageColorAttachmentOutputBit };
			submitInfo.WaitSemaphoreCount = 1;
			submitInfo.PWaitSemaphores = waitSemaphores;
			submitInfo.PWaitDstStageMask = waitStages;

			submitInfo.CommandBufferCount = 1;

			fixed (CommandBuffer* buffer = &CommandBuffers[imageIndex])
			{
				submitInfo.PCommandBuffers = buffer;
			}

			Silk.NET.Vulkan.Semaphore* signalSemaphores = stackalloc[] { RenderFinishedSemaphores[CurrentFrame] };
			submitInfo.SignalSemaphoreCount = 1;
			submitInfo.PSignalSemaphores = signalSemaphores;

			fixed (Fence* inFlightFences = &InFlightFences[CurrentFrame])
			{
				_vk.ResetFences(Device, 1, inFlightFences);
			}

			if (_vk.QueueSubmit(GraphicsQueue, 1, &submitInfo, InFlightFences[CurrentFrame]) != Result.Success)
			{
				Trace.TraceError("failed to submit draw command buffer!");
				Console.ReadKey();
				return;
			}

			PresentInfoKHR presentInfo = new();
			presentInfo.SType = StructureType.PresentInfoKhr;

			presentInfo.WaitSemaphoreCount = 1;
			presentInfo.PWaitSemaphores = signalSemaphores;

			SwapchainKHR* swapChains = stackalloc[] { SwapChain };
			presentInfo.SwapchainCount = 1;
			presentInfo.PSwapchains = swapChains;
			presentInfo.PImageIndices = &imageIndex;

			presentInfo.PResults = null; // Optional

			result = _vkSwapchain.QueuePresent(PresentQueue, &presentInfo);

			if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr)
			{
				RecreateSwapChain();
			}
			else if (result != Result.Success)
			{
				Trace.TraceError("failed to present swap chain image!");
				Console.ReadKey();
				return;
			}

			CurrentFrame = (CurrentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
		}

		unsafe private void UpdateUniformBuffer(uint currentImage)
		{
			//https://github.com/jcant0n/VulkanSharp_Tutorials/blob/86f289c3cf547de7e08c6a4e1200f01cf21ca2d8/VKUniformBuffers/VulkanRenderer.cs#L97
			double oneFullRotPer4SecInRad = (DateTime.Now.Ticks % (4 * TimeSpan.TicksPerSecond))
	* ((Math.PI / 2f) / TimeSpan.TicksPerSecond);

			UniformBufferObject ubo = new();
			//ubo.Model = Matrix4x4.CreateRotationZ((float)oneFullRotPer4SecInRad);
			ubo.Model = Matrix4x4.Identity;
			ubo.View = Matrix4x4.CreateLookAt(new Vector3(2.0f, 2.0f, 2.0f), Vector3.Zero, new Vector3(0.0f, 0.0f, 1.0f));
			ubo.Proj = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4.0f, SwapChainExtent.Width / (float)SwapChainExtent.Height, 0.1f, 10.0f);

			ubo.Proj.M22 *= -1;

			ulong bufferSize = (ulong)Marshal.SizeOf<UniformBufferObject>();
			void* data;

			//_vk.MapMemory(Device, UniformBuffersChunksAndItens[currentImage].Item1.DeviceMemory, UniformBuffersChunksAndItens[currentImage].Item2.StartOffset, bufferSize, 0, &data);
			_vk.MapMemory(Device, UniformBuffersChunks[currentImage].DeviceMemory, UniformBuffersItems[currentImage].StartOffset, bufferSize, 0, &data);
			Marshal.StructureToPtr(ubo, (IntPtr)data, false);

			//_vk.UnmapMemory(Device, UniformBuffersChunksAndItens[currentImage].Item1.DeviceMemory);
			_vk.UnmapMemory(Device, UniformBuffersChunks[currentImage].DeviceMemory);
		}

		unsafe private void Cleanup()
		{

			CleanupSwapChain();

			_vk.DestroySampler(Device, TextureSampler, null);
			_vk.DestroyImageView(Device, TextureImageView, null);

			_vk.DestroyImage(Device, TextureImage, null);
			_vk.FreeMemory(Device, TextureImageMemory, null);

			_vk.DestroyDescriptorSetLayout(Device, DescriptorSetLayout, null);

			_vk.DestroyBuffer(Device, IndexBuffer, null);
			_vk.FreeMemory(Device, IndexBufferMemory, null);

			_vk.DestroyBuffer(Device, VertexBuffer, null);
			_vk.FreeMemory(Device, VertexBufferMemory, null);

			//VulkanMemoryLocal.FreeAll(ref _vk, ref Device);
			VulkanMemoryLocal2.FreeAll(ref _vk, ref Device);

			Parallel.For(0, MAX_FRAMES_IN_FLIGHT, (i) =>
			{
				_vk.DestroySemaphore(Device, RenderFinishedSemaphores[i], null);
				_vk.DestroySemaphore(Device, ImageAvailableSemaphores[i], null);
				_vk.DestroyFence(Device, InFlightFences[i], null);
			});
			/*
			for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
			{
				_vk.DestroySemaphore(Device, RenderFinishedSemaphores[i], null);
				_vk.DestroySemaphore(Device, ImageAvailableSemaphores[i], null);
				_vk.DestroyFence(Device, InFlightFences[i], null);
			}*/

			_vk.DestroyCommandPool(Device, CommandPool, null);
			_vk.DestroyCommandPool(Device, CommandPoolSecond, null);

			_vk.DestroyDevice(Device, null);

			if (EnableValidationLayers)
				_debugUtils?.DestroyDebugUtilsMessenger(VulkanInstance, DebugMessenger, null);

			_vkSurface?.DestroySurface(VulkanInstance, Surface, null);

			_vk.DestroyInstance(VulkanInstance, null);

			Help.FreeMemory();

			SDL.SDL_DestroyWindow(Window);

			SDL.SDL_Quit();
		}

		unsafe private void CleanupSwapChain()
		{
			_vk.DestroyImageView(Device, ColorImageView, null);
			_vk.DestroyImage(Device, ColorImage, null);
			_vk.FreeMemory(Device, ColorImageMemory, null);

			_vk.DestroyImageView(Device, DepthImageView, null);
			_vk.DestroyImage(Device, DepthImage, null);
			_vk.FreeMemory(Device, DepthImageMemory, null);

			foreach (Framebuffer framebuffer in SwapChainFramebuffers)
			{
				_vk.DestroyFramebuffer(Device, framebuffer, null);
			}

			_vk.FreeCommandBuffers(Device, CommandPool, (uint)CommandBuffers.Length, CommandBuffers);

			_vk.DestroyPipeline(Device, GraphicsPipeline, null);

			_vk.DestroyPipelineLayout(Device, PipelineLayout, null);

			_vk.DestroyRenderPass(Device, RenderPass, null);

			foreach (ImageView imageView in SwapChainImageViews)
			{
				_vk.DestroyImageView(Device, imageView, null);
			}

			_vkSwapchain?.DestroySwapchain(Device, SwapChain, null);

			Parallel.For(0, SwapChainImages.Length, (i) =>
			{
				_vk.DestroyBuffer(Device, UniformBuffers[i], null);
				_vk.FreeMemory(Device, UniformBuffersMemory[i], null);
			});
			/*
			for (int i = 0; i < SwapChainImages.Length; i++)
			{
				_vk.DestroyBuffer(Device, UniformBuffers[i], null);
				_vk.FreeMemory(Device, UniformBuffersMemory[i], null);
			}*/

			_vk.DestroyDescriptorPool(Device, DescriptorPool, null);
		}
	}
}

public struct QueueFamilyIndices
{
	public uint? GraphicsFamily;
	public uint? PresentFamily;

	public bool IsComplete()
	{
		return GraphicsFamily.HasValue && PresentFamily.HasValue;
	}
}

public struct SwapChainSupportDetails
{
	public SurfaceCapabilitiesKHR Capabilities;
	public SurfaceFormatKHR[] Formats;
	public PresentModeKHR[] PresentModes;
}

public struct Vertex
{
	public Vector3 Pos;
	public Vector3 Color;
	public Vector2 TexCoord;

	unsafe public static VertexInputBindingDescription GetBindingDescription()
	{
		VertexInputBindingDescription bindingDescription = new();

		bindingDescription.Binding = 0;
		bindingDescription.Stride = (uint)Marshal.SizeOf<Vertex>();
		bindingDescription.InputRate = VertexInputRate.Vertex;

		return bindingDescription;
	}

	public static VertexInputAttributeDescription[] GetAttributeDescriptions()
	{
		VertexInputAttributeDescription[] attributeDescriptions = new VertexInputAttributeDescription[3];

		attributeDescriptions[0].Binding = 0;
		attributeDescriptions[0].Location = 0;
		attributeDescriptions[0].Format = Format.R32G32B32Sfloat;
		attributeDescriptions[0].Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Pos));

		attributeDescriptions[1].Binding = 0;
		attributeDescriptions[1].Location = 1;
		attributeDescriptions[1].Format = Format.R32G32B32Sfloat;
		attributeDescriptions[1].Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Color));

		attributeDescriptions[2].Binding = 0;
		attributeDescriptions[2].Location = 2;
		attributeDescriptions[2].Format = Format.R32G32Sfloat;
		attributeDescriptions[2].Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(TexCoord));

		return attributeDescriptions;
	}
};

//https://vulkan-tutorial.com/Uniform_buffers/Descriptor_pool_and_sets
//
[StructLayout(LayoutKind.Explicit)]
public struct UniformBufferObject
{
	[FieldOffset(0)]
	public Matrix4x4 Model;
	[FieldOffset(64)]
	public Matrix4x4 View;
	[FieldOffset(128)]
	public Matrix4x4 Proj;
};

public struct PushConstantData
{
	public Vector4 Color;
	public Vector4 Position;
};