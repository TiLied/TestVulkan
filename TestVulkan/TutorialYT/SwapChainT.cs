using Evergine.Bindings.Vulkan;
using System;
using System.Diagnostics;

namespace TestVulkan
{
	public class SwapChainT
	{
		public static readonly uint MAX_FRAMES_IN_FLIGHT = 2;

		private DeviceT Device;
		private VkExtent2D WindowExtent;

		public VkSwapchainKHR SwapChain;
		private SwapChainT OldSwapChain;

		public VkImage[] SwapChainImages;

		private VkFormat SwapChainImageFormat;
		private VkFormat SwapChainDepthFormat;

		public bool CompareSwapChainFormats(SwapChainT swapChain) => 
			swapChain.SwapChainDepthFormat == SwapChainDepthFormat && 
			swapChain.SwapChainImageFormat == SwapChainImageFormat;

		public VkExtent2D SwapChainExtent;
		private VkImageView[] SwapChainImageViews;
		public VkFramebuffer[] SwapChainFramebuffers;

		public VkRenderPass RenderPass;

		private VkImage[] DepthImages;
		private VkDeviceMemory[] DepthImageMemorys;
		private VkImageView[] DepthImageViews;

		private VkSemaphore[] ImageAvailableSemaphores;
		private VkSemaphore[] RenderFinishedSemaphores;
		private VkFence[] InFlightFences;
		private VkFence[] ImagesInFlight;

		private int CurrentFrame = 0;

		public SwapChainT(ref DeviceT deviceRef, VkExtent2D windowExtent)
		{
			Device = deviceRef;
			WindowExtent = windowExtent;

			Init();
		}

		public SwapChainT(ref DeviceT deviceRef, VkExtent2D windowExtent, ref SwapChainT previos)
		{
			Device = deviceRef;
			WindowExtent = windowExtent;
			OldSwapChain = previos;

			Init();

			OldSwapChain.DestroySwapChain();
			OldSwapChain = null;
		}

		private void Init() 
		{
			CreateSwapChain();
			CreateImageViews();
			CreateRenderPass();
			CreateDepthResources();
			CreateFramebuffers();
			CreateSyncObjects();
		}

		public float ExtentAspectRatio()
		{
			return (float)SwapChainExtent.width / (float)SwapChainExtent.height;
		}

		unsafe public VkResult AcquireNextImage(ref uint imageIndex)
		{
			fixed (VkFence* InFlightFence = &InFlightFences[CurrentFrame])
			{
				VulkanNative.vkWaitForFences(
								Device.Device,
								1,
								InFlightFence,
								true,
								uint.MaxValue);
			}

			VkResult result;

			fixed (uint* imageIndexL = &imageIndex)
			{
				result = VulkanNative.vkAcquireNextImageKHR(
								Device.Device,
								SwapChain,
								uint.MaxValue,
								ImageAvailableSemaphores[CurrentFrame],  // must be a not signaled semaphore
								VkFence.Null,
								imageIndexL);
			}
			
			return result;
		}

		unsafe public VkResult SubmitCommandBuffers(ref VkCommandBuffer buffers, ref uint imageIndex) 
		{
			if (ImagesInFlight[imageIndex].Handle != 0)
			{
				fixed (VkFence* imagesInFlight = &ImagesInFlight[imageIndex])
				{
					VulkanNative.vkWaitForFences(Device.Device, 1, imagesInFlight, true, uint.MaxValue);
				}
			}
			ImagesInFlight[imageIndex] = InFlightFences[CurrentFrame];

			VkSubmitInfo submitInfo = new();
			submitInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO;

			VkSemaphore* waitSemaphores = stackalloc VkSemaphore[1];
			waitSemaphores[0] = ImageAvailableSemaphores[CurrentFrame];

			VkPipelineStageFlags* waitStages = stackalloc VkPipelineStageFlags[1];
			waitStages[0] = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;

			submitInfo.waitSemaphoreCount = 1;
			submitInfo.pWaitSemaphores = waitSemaphores;
			submitInfo.pWaitDstStageMask = waitStages;

			submitInfo.commandBufferCount = 1;
			fixed (VkCommandBuffer* buffer = &buffers)
			{
				submitInfo.pCommandBuffers = buffer;
			}

			VkSemaphore* signalSemaphores = stackalloc VkSemaphore[1];
			signalSemaphores[0] = RenderFinishedSemaphores[CurrentFrame];

			submitInfo.signalSemaphoreCount = 1;
			submitInfo.pSignalSemaphores = signalSemaphores;

			fixed (VkFence* inFlightFence = &InFlightFences[CurrentFrame])
			{
				VulkanNative.vkResetFences(Device.Device, 1, inFlightFence);
				if (VulkanNative.vkQueueSubmit(Device.GraphicsQueue, 1, &submitInfo, InFlightFences[CurrentFrame]) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to submit draw command buffer!");
				}
			}

			VkPresentInfoKHR presentInfo = new();
			presentInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PRESENT_INFO_KHR;

			presentInfo.waitSemaphoreCount = 1;
			presentInfo.pWaitSemaphores = signalSemaphores;

			VkSwapchainKHR* swapChains = stackalloc VkSwapchainKHR[1];
			swapChains[0] = SwapChain;

			presentInfo.swapchainCount = 1;
			presentInfo.pSwapchains = swapChains;

			fixed (uint* imageIndexL = &imageIndex)
			{
				presentInfo.pImageIndices = imageIndexL;
			}

			VkResult result = VulkanNative.vkQueuePresentKHR(Device.PresentQueue, &presentInfo);

			CurrentFrame = (CurrentFrame + 1) % (int)MAX_FRAMES_IN_FLIGHT;

			return result;
		}

		unsafe private void CreateSwapChain() 
		{
			SwapChainSupportDetailsT swapChainSupport = Device.QuerySwapChainSupport(Device.PhysicalDevice);

			VkSurfaceFormatKHR surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
			VkPresentModeKHR presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
			VkExtent2D extent = ChooseSwapExtent(swapChainSupport.Capabilities);

			uint imageCount = swapChainSupport.Capabilities.minImageCount + 1;
			if (swapChainSupport.Capabilities.maxImageCount > 0 &&
				imageCount > swapChainSupport.Capabilities.maxImageCount)
			{
				imageCount = swapChainSupport.Capabilities.maxImageCount;
			}

			VkSwapchainCreateInfoKHR createInfo = new();
			createInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR;
			createInfo.surface = Device.Surface;

			createInfo.minImageCount = imageCount;
			createInfo.imageFormat = surfaceFormat.format;
			createInfo.imageColorSpace = surfaceFormat.colorSpace;
			createInfo.imageExtent = extent;
			createInfo.imageArrayLayers = 1;
			createInfo.imageUsage = VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;

			QueueFamilyIndicesT indices = Device.FindQueueFamilies(Device.PhysicalDevice);

			uint* queueFamilyIndices = stackalloc uint[2];
			queueFamilyIndices[0] = indices.GraphicsFamily;
			queueFamilyIndices[1] = indices.PresentFamily;

			if (indices.GraphicsFamily != indices.PresentFamily)
			{
				createInfo.imageSharingMode = VkSharingMode.VK_SHARING_MODE_CONCURRENT;
				createInfo.queueFamilyIndexCount = 2;
				createInfo.pQueueFamilyIndices = queueFamilyIndices;
			}
			else
			{
				createInfo.imageSharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE;
				createInfo.queueFamilyIndexCount = 0;      // Optional
				createInfo.pQueueFamilyIndices = null;  // Optional
			}

			createInfo.preTransform = swapChainSupport.Capabilities.currentTransform;
			createInfo.compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;

			createInfo.presentMode = presentMode;
			createInfo.clipped = true;

			if(OldSwapChain == null)
				createInfo.oldSwapchain = VkSwapchainKHR.Null;
			else
				createInfo.oldSwapchain = OldSwapChain.SwapChain;

			fixed (VkSwapchainKHR* swapChain = &SwapChain)
			{
				if (VulkanNative.vkCreateSwapchainKHR(Device.Device, &createInfo, null, swapChain) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to create swap chain!");
				}
			}

			// we only specified a minimum number of images in the swap chain, so the implementation is
			// allowed to create a swap chain with more. That's why we'll first query the final number of
			// images with vkGetSwapchainImagesKHR, then resize the container and finally call it again to
			// retrieve the handles.
			VulkanNative.vkGetSwapchainImagesKHR(Device.Device, SwapChain, &imageCount, null);
			SwapChainImages = new VkImage[imageCount];
			fixed (VkImage* swapChainImages = &SwapChainImages[0])
			{
				VulkanNative.vkGetSwapchainImagesKHR(Device.Device, SwapChain, &imageCount, swapChainImages);
			}

			SwapChainImageFormat = surfaceFormat.format;
			SwapChainExtent = extent;
		}

		private VkSurfaceFormatKHR ChooseSwapSurfaceFormat(VkSurfaceFormatKHR[] availableFormats)
		{
			VkSurfaceFormatKHR returnFormat = availableFormats[0];
			foreach (VkSurfaceFormatKHR availableFormat in availableFormats)
			{
				Trace.WriteLine($"Available Swap Surface Format: {availableFormat.format}");
				if (availableFormat.format == VkFormat.VK_FORMAT_B8G8R8A8_SRGB && availableFormat.colorSpace == VkColorSpaceKHR.VK_COLORSPACE_SRGB_NONLINEAR_KHR)
					returnFormat = availableFormat;
			}

			Trace.WriteLine($"Return Format: {returnFormat.format}");

			return returnFormat;
		}

		private VkPresentModeKHR ChooseSwapPresentMode(VkPresentModeKHR[] availablePresentModes)
		{
			VkPresentModeKHR returnPresentMode = VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR;
			
			foreach (VkPresentModeKHR availablePresentMode in availablePresentModes)
			{
				Trace.WriteLine($"Available Swap Present Mode: {availablePresentMode}");
				if (availablePresentMode == VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR)
					returnPresentMode = availablePresentMode;
			}
			Trace.WriteLine($"Return Present Mode: {returnPresentMode}");
			
			return returnPresentMode;
		}

		private VkExtent2D ChooseSwapExtent(VkSurfaceCapabilitiesKHR capabilities)
		{
			if (capabilities.currentExtent.width != uint.MaxValue)
			{
				Trace.WriteLine("CurrentExtent: " + capabilities.currentExtent.height + ":" + capabilities.currentExtent.width);
				return capabilities.currentExtent;
			}
			else
			{
				VkExtent2D actualExtent = WindowExtent;
				actualExtent.width = Math.Max(capabilities.minImageExtent.width, Math.Min(capabilities.maxImageExtent.width, actualExtent.width));
				actualExtent.height = Math.Max(capabilities.minImageExtent.height, Math.Min(capabilities.maxImageExtent.height, actualExtent.height));

				Trace.WriteLine("CctualExtent: " + actualExtent.height + ":" + actualExtent.width);

				return actualExtent;
			}
		}

		unsafe private void CreateImageViews() 
		{
			SwapChainImageViews = new VkImageView[SwapChainImages.Length];

			for (int i = 0; i < SwapChainImages.Length; i++)
			{
				VkImageViewCreateInfo viewInfo = new();
				viewInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
				viewInfo.image = SwapChainImages[i];
				viewInfo.viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D;
				viewInfo.format = SwapChainImageFormat;
				viewInfo.subresourceRange.aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT;
				viewInfo.subresourceRange.baseMipLevel = 0;
				viewInfo.subresourceRange.levelCount = 1;
				viewInfo.subresourceRange.baseArrayLayer = 0;
				viewInfo.subresourceRange.layerCount = 1;

				fixed (VkImageView* swapChainImageView = &SwapChainImageViews[i])
				{
					if (VulkanNative.vkCreateImageView(Device.Device, &viewInfo, null, swapChainImageView) != VkResult.VK_SUCCESS)
					{
						throw new Exception("failed to create texture image view!");
					}
				}
			}
		}

		unsafe private void CreateRenderPass() 
		{
			VkAttachmentDescription depthAttachment = new();
			depthAttachment.format = FindDepthFormat();
			depthAttachment.samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT;
			depthAttachment.loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR;
			depthAttachment.storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE;
			depthAttachment.stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE;
			depthAttachment.stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE;
			depthAttachment.initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED;
			depthAttachment.finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;

			VkAttachmentReference depthAttachmentRef = new();
			depthAttachmentRef.attachment = 1;
			depthAttachmentRef.layout = VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;

			VkAttachmentDescription colorAttachment = new();
			colorAttachment.format = SwapChainImageFormat;
			colorAttachment.samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT;
			colorAttachment.loadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR;
			colorAttachment.storeOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE;
			colorAttachment.stencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE;
			colorAttachment.stencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE;
			colorAttachment.initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED;
			colorAttachment.finalLayout = VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR;

			VkAttachmentReference colorAttachmentRef = new();
			colorAttachmentRef.attachment = 0;
			colorAttachmentRef.layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

			VkSubpassDescription subpass = new();
			subpass.pipelineBindPoint = VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS;
			subpass.colorAttachmentCount = 1;
			subpass.pColorAttachments = &colorAttachmentRef;
			subpass.pDepthStencilAttachment = &depthAttachmentRef;

			VkSubpassDependency dependency = new();

			dependency.dstSubpass = 0;
			dependency.dstAccessMask = VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT | VkAccessFlags.VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
			dependency.dstStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT | VkPipelineStageFlags.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT;
			dependency.srcSubpass = VulkanNative.VK_SUBPASS_EXTERNAL;
			dependency.srcAccessMask = 0;
			dependency.srcStageMask = VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT | VkPipelineStageFlags.VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT;

			VkAttachmentDescription* attachments = stackalloc VkAttachmentDescription[2];
			attachments[0] = colorAttachment;
			attachments[1] = depthAttachment;

			VkRenderPassCreateInfo renderPassInfo = new();
			renderPassInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO;
			renderPassInfo.attachmentCount = 2;
			renderPassInfo.pAttachments = attachments;
			renderPassInfo.subpassCount = 1;
			renderPassInfo.pSubpasses = &subpass;
			renderPassInfo.dependencyCount = 1;
			renderPassInfo.pDependencies = &dependency;

			fixed (VkRenderPass* renderPass = &RenderPass)
			{
				if (VulkanNative.vkCreateRenderPass(Device.Device, &renderPassInfo, null, renderPass) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to create render pass!");
				}
			}
			
		}

		private VkFormat FindDepthFormat()
		{
			VkFormat[] _f = new VkFormat[3];
			_f[0] = VkFormat.VK_FORMAT_D32_SFLOAT;
			_f[1] = VkFormat.VK_FORMAT_D32_SFLOAT_S8_UINT;
			_f[2] = VkFormat.VK_FORMAT_D24_UNORM_S8_UINT;

			return Device.FindSupportedFormat(_f, VkImageTiling.VK_IMAGE_TILING_OPTIMAL, VkFormatFeatureFlags.VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT);
		}

		unsafe private void CreateDepthResources() 
		{
			VkFormat depthFormat = FindDepthFormat();
			SwapChainDepthFormat = depthFormat;
			VkExtent2D swapChainExtent = SwapChainExtent;

			DepthImages = new VkImage[SwapChainImages.Length];
			DepthImageMemorys = new VkDeviceMemory[SwapChainImages.Length];
			DepthImageViews = new VkImageView[SwapChainImages.Length];

			for (int i = 0; i < DepthImages.Length; i++)
			{
				VkImageCreateInfo imageInfo = new();
				imageInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO;
				imageInfo.imageType = VkImageType.VK_IMAGE_TYPE_2D;
				imageInfo.extent.width = swapChainExtent.width;
				imageInfo.extent.height = swapChainExtent.height;
				imageInfo.extent.depth = 1;
				imageInfo.mipLevels = 1;
				imageInfo.arrayLayers = 1;
				imageInfo.format = depthFormat;
				imageInfo.tiling = VkImageTiling.VK_IMAGE_TILING_OPTIMAL;
				imageInfo.initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED;
				imageInfo.usage = VkImageUsageFlags.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;
				imageInfo.samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT;
				imageInfo.sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE;
				imageInfo.flags = 0;

				Device.CreateImageWithInfo(
					imageInfo,
					VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT,
					ref DepthImages[i],
					ref DepthImageMemorys[i]);

				VkImageViewCreateInfo viewInfo = new();
				viewInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
				viewInfo.image = DepthImages[i];
				viewInfo.viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D;
				viewInfo.format = depthFormat;
				viewInfo.subresourceRange.aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT;
				viewInfo.subresourceRange.baseMipLevel = 0;
				viewInfo.subresourceRange.levelCount = 1;
				viewInfo.subresourceRange.baseArrayLayer = 0;
				viewInfo.subresourceRange.layerCount = 1;

				fixed (VkImageView* depthImageView = &DepthImageViews[i])
				{
					if (VulkanNative.vkCreateImageView(Device.Device, &viewInfo, null, depthImageView) != VkResult.VK_SUCCESS)
					{
						throw new Exception("failed to create texture image view!");
					}
				}
			}
		}

		unsafe private void CreateFramebuffers() 
		{
			SwapChainFramebuffers = new VkFramebuffer[SwapChainImages.Length];
			for (int i = 0; i < SwapChainImages.Length; i++)
			{
				VkImageView* attachments = stackalloc VkImageView[2];
				attachments[0] = SwapChainImageViews[i];
				attachments[1] = DepthImageViews[i];

				VkExtent2D swapChainExtent = SwapChainExtent;
				VkFramebufferCreateInfo framebufferInfo = new();
				framebufferInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO;
				framebufferInfo.renderPass = RenderPass;
				framebufferInfo.attachmentCount = 2;
				framebufferInfo.pAttachments = attachments;
				framebufferInfo.width = swapChainExtent.width;
				framebufferInfo.height = swapChainExtent.height;
				framebufferInfo.layers = 1;

				fixed (VkFramebuffer* swapChainFramebuffer = &SwapChainFramebuffers[i])
				{
					if (VulkanNative.vkCreateFramebuffer(
							Device.Device,
							&framebufferInfo,
							null,
							swapChainFramebuffer) != VkResult.VK_SUCCESS)
					{
						throw new Exception("failed to create framebuffer!");
					}
				}

			}
		}

		unsafe private void CreateSyncObjects() 
		{
			ImageAvailableSemaphores = new VkSemaphore[MAX_FRAMES_IN_FLIGHT];
			RenderFinishedSemaphores = new VkSemaphore[MAX_FRAMES_IN_FLIGHT];
			InFlightFences = new VkFence[MAX_FRAMES_IN_FLIGHT];
			ImagesInFlight = new VkFence[SwapChainImages.Length];

			VkSemaphoreCreateInfo semaphoreInfo = new();
			semaphoreInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO;

			VkFenceCreateInfo fenceInfo = new();
			fenceInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO;
			fenceInfo.flags = VkFenceCreateFlags.VK_FENCE_CREATE_SIGNALED_BIT;

			for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
			{
				fixed (VkSemaphore* imageAvailableSemaphore = &ImageAvailableSemaphores[i], renderFinishedSemaphore = &RenderFinishedSemaphores[i])
				{
					if (VulkanNative.vkCreateSemaphore(Device.Device, &semaphoreInfo, null, imageAvailableSemaphore) != VkResult.VK_SUCCESS || VulkanNative.vkCreateSemaphore(Device.Device, &semaphoreInfo, null, renderFinishedSemaphore) != VkResult.VK_SUCCESS)
					{
						Trace.TraceError("failed to create semaphores!");
						throw new Exception("failed to create semaphores!");
					}
				}

				fixed (VkFence* inFlightFences = &InFlightFences[i])
				{
					if (VulkanNative.vkCreateFence(Device.Device, &fenceInfo, null, inFlightFences) != VkResult.VK_SUCCESS)
					{
						Trace.TraceError("failed to create fence!");
						throw new Exception("failed to create fence!");

					}
				}
			}
		}

		unsafe public void DestroySwapChain() 
		{
			foreach (VkImageView imageView in SwapChainImageViews)
			{
				VulkanNative.vkDestroyImageView(Device.Device, imageView, null);
			}

			VulkanNative.vkDestroySwapchainKHR(Device.Device, SwapChain, null);

			for (int i = 0; i < DepthImages.Length; i++)
			{
				VulkanNative.vkDestroyImageView(Device.Device, DepthImageViews[i], null);
				VulkanNative.vkDestroyImage(Device.Device, DepthImages[i], null);
				VulkanNative.vkFreeMemory(Device.Device, DepthImageMemorys[i], null);
			}

			foreach (VkFramebuffer framebuffer in SwapChainFramebuffers)
			{
				VulkanNative.vkDestroyFramebuffer(Device.Device, framebuffer, null);
			}

			VulkanNative.vkDestroyRenderPass(Device.Device, RenderPass, null);

			// cleanup synchronization objects
			for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
			{
				VulkanNative.vkDestroySemaphore(Device.Device, RenderFinishedSemaphores[i], null);
				VulkanNative.vkDestroySemaphore(Device.Device, ImageAvailableSemaphores[i], null);
				VulkanNative.vkDestroyFence(Device.Device, InFlightFences[i], null);
			}
		}

	}
}
