using Evergine.Bindings.Vulkan;
using SDL2;
using System;
using System.Diagnostics;

namespace TestVulkan
{
	public class FirstAppT
	{
		private const int WIDTH = 960;
		private const int HEIGHT = 540;

		private WindowT Window;
		private DeviceT Device;
		private SwapChainT SwapChain;
		private PipelineT Pipeline;

		private VkPipelineLayout PipelineLayout;
		private VkCommandBuffer[] CommandBuffers;
		public FirstAppT()
		{
			Window = new(WIDTH, HEIGHT, "Hello Vulkan!");
			Device = new(ref Window);
			SwapChain = new(ref Device, Window.GetExtent());

			CreatePipelineLayout();
			CreatePipeline();
			CreateCommandBuffers();
		}

		public void Run() 
		{
			bool run = true;
			while (run)
			{
				if (SDL.SDL_PollEvent(out SDL.SDL_Event test_event) == 1)
				{
					if (test_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE)
					{
						Trace.WriteLine($"Window {test_event.window.windowID} closed");
						run = false;
					}
				}
			}

			Destroy();
			Pipeline.DestroyPipeline();
			Device.DestroyDebugMessenger();
			Window.DestroyWindow();
		}

		unsafe private void CreatePipelineLayout() 
		{
			VkPipelineLayoutCreateInfo pipelineLayoutInfo = new();
			pipelineLayoutInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO;
			pipelineLayoutInfo.setLayoutCount = 0;
			pipelineLayoutInfo.pSetLayouts = null;
			pipelineLayoutInfo.pushConstantRangeCount = 0;
			pipelineLayoutInfo.pPushConstantRanges = null;

			fixed (VkPipelineLayout* pipelineLayout = &PipelineLayout)
			{
				if (VulkanNative.vkCreatePipelineLayout(Device.Device, &pipelineLayoutInfo, null, pipelineLayout) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to create pipeline layout!");
				}
			}			
		}

		private void CreatePipeline() 
		{
			PipelineConfigInfo pipelineConfig = PipelineT.DefaultPipelineConfig(SwapChain.SwapChainExtent.width, SwapChain.SwapChainExtent.height);
			pipelineConfig.RenderPass = SwapChain.RenderPass;
			pipelineConfig.PipelineLayout = PipelineLayout;
			Pipeline = new(ref Device, Program.Directory + @"\TestVulkan\shaders\Svert.spv", Program.Directory + @"\TestVulkan\shaders\Sfrag.spv", pipelineConfig);

		}

		private void CreateCommandBuffers() { }

		private void DrawFrame() { }

		unsafe public void Destroy() 
		{
			VulkanNative.vkDestroyPipelineLayout(Device.Device, PipelineLayout, null);

		}
	}
}
