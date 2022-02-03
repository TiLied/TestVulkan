using Evergine.Bindings.Vulkan;
using System;

namespace TestVulkan
{
	public class RendererT
	{
		private WindowT Window;
		private DeviceT Device;

		private SwapChainT SwapChain;
		private VkCommandBuffer[] CommandBuffers;

		private uint CurrentImageIndex;
		private int CurrentFrameIndex = 0;
		private bool IsFrameStarted = false;

		public bool IsFrameInProgress => IsFrameStarted;
		public VkCommandBuffer GetCurrentCommandBuffer => CommandBuffers[CurrentFrameIndex];
		public VkRenderPass GetSwapchainRenderPass => SwapChain.RenderPass;

		public int GetFrameIndex => CurrentFrameIndex;
		public RendererT(ref WindowT window, ref DeviceT device)
		{
			Window = window;
			Device = device;

			RecreateSwapChain();
			CreateCommandBuffers();
		}

		private void RecreateSwapChain()
		{
			//if (Minimized)
			//	return;

			VkExtent2D extent = Window.GetExtent();
			VulkanNative.vkDeviceWaitIdle(Device.Device);

			if (SwapChain == null)
				SwapChain = new(ref Device, extent);
			else
			{
				SwapChainT oldSwapChain = SwapChain;
				SwapChain = new(ref Device, extent, ref oldSwapChain);

				if (!oldSwapChain.CompareSwapChainFormats(SwapChain))
					throw new Exception("Swap image and depth format has changed!");

				/*
				if (SwapChain.SwapChainImages.Length != CommandBuffers.Length)
				{
					FreeCommandBuffers();
					CreateCommandBuffers();
				}*/
			}

			//
			//
			//
		}

		unsafe private void CreateCommandBuffers()
		{
			CommandBuffers = new VkCommandBuffer[SwapChainT.MAX_FRAMES_IN_FLIGHT];

			VkCommandBufferAllocateInfo allocateInfo = new();
			allocateInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO;
			allocateInfo.level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY;
			allocateInfo.commandPool = Device.CommandPool;
			allocateInfo.commandBufferCount = (uint)CommandBuffers.Length;

			fixed (VkCommandBuffer* commandBuffers = &CommandBuffers[0])
			{
				if (VulkanNative.vkAllocateCommandBuffers(Device.Device, &allocateInfo, commandBuffers) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to allocate command buffers!");
				}
			}
		}

		unsafe private void FreeCommandBuffers()
		{
			fixed (VkCommandBuffer* commandBuffers = &CommandBuffers[0])
			{
				VulkanNative.vkFreeCommandBuffers(Device.Device, Device.CommandPool, (uint)CommandBuffers.Length, commandBuffers);
			}

			CommandBuffers = null;
		}

		unsafe public VkCommandBuffer? BeginFrame()
		{
			VkResult result = SwapChain.AcquireNextImage(ref CurrentImageIndex);

			if (result == VkResult.VK_ERROR_OUT_OF_DATE_KHR)
			{
				RecreateSwapChain();
				return null;
			}

			if (result != VkResult.VK_SUCCESS && result != VkResult.VK_SUBOPTIMAL_KHR)
				throw new Exception("Fail To Acquire Next Image");

			IsFrameStarted = true;

			VkCommandBuffer commandBuffer = GetCurrentCommandBuffer;

			VkCommandBufferBeginInfo beginInfo = new();

			beginInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;

			if (VulkanNative.vkBeginCommandBuffer(commandBuffer, &beginInfo) != VkResult.VK_SUCCESS)
			{
				throw new Exception("failed to begin recording command buffer!");
			}

			return commandBuffer;
		}

		unsafe public void EndFrame() 
		{
			VkCommandBuffer commandBuffer = GetCurrentCommandBuffer;

			if (VulkanNative.vkEndCommandBuffer(commandBuffer) != VkResult.VK_SUCCESS)
			{
				throw new Exception("failed to record command buffer!");
			}

			VkResult result = SwapChain.SubmitCommandBuffers(ref commandBuffer, ref CurrentImageIndex);

			if (result == VkResult.VK_ERROR_OUT_OF_DATE_KHR || result == VkResult.VK_SUBOPTIMAL_KHR)
			{
				RecreateSwapChain();

				//Delete IsFrameStarted! and CurrentFrameIndex!!
				CurrentFrameIndex = (CurrentFrameIndex + 1) % SwapChainT.MAX_FRAMES_IN_FLIGHT;
				IsFrameStarted = false;
				return;
			}

			if (result != VkResult.VK_SUCCESS)
				throw new Exception("Fail to present");

			IsFrameStarted = false;
			CurrentFrameIndex = (CurrentFrameIndex + 1) % SwapChainT.MAX_FRAMES_IN_FLIGHT;
		}

		unsafe public void BeginSwapChainRenderPass(VkCommandBuffer commandBuffer) 
		{
			VkRenderPassBeginInfo renderPassInfo = new();
			renderPassInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO;
			renderPassInfo.renderPass = SwapChain.RenderPass;
			renderPassInfo.framebuffer = SwapChain.SwapChainFramebuffers[CurrentImageIndex];

			renderPassInfo.renderArea.offset = new VkOffset2D(0, 0);
			renderPassInfo.renderArea.extent = SwapChain.SwapChainExtent;

			VkClearValue* clearValues = stackalloc VkClearValue[2];
			clearValues[0].color = new VkClearColorValue(0.01f, 0.01f, 0.01f, 1.0f);
			clearValues[1].depthStencil = new VkClearDepthStencilValue() { depth = 1.0f, stencil = 0 };

			renderPassInfo.clearValueCount = 2;
			renderPassInfo.pClearValues = clearValues;

			VulkanNative.vkCmdBeginRenderPass(commandBuffer, &renderPassInfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);

			VkViewport viewport = new();
			viewport.x = 0.0f;
			viewport.y = 0.0f;
			viewport.width = SwapChain.SwapChainExtent.width;
			viewport.height = SwapChain.SwapChainExtent.height;
			viewport.minDepth = 0.0f;
			viewport.maxDepth = 1.0f;

			VkRect2D scissor = new()
			{
				extent = SwapChain.SwapChainExtent,
				offset = new VkOffset2D(0, 0)
			};

			VulkanNative.vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
			VulkanNative.vkCmdSetScissor(commandBuffer, 0, 1, &scissor);

		}
		public void EndSwapChainRenderPass(VkCommandBuffer commandBuffer)
		{
			VulkanNative.vkCmdEndRenderPass(commandBuffer);
		}

		unsafe public void DestroyRenderer()
		{
			SwapChain.DestroySwapChain();
			FreeCommandBuffers();
		}
	}
}
