using Evergine.Bindings.Vulkan;
using SDL2;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	[StructLayout(LayoutKind.Explicit)]
	public struct SimplePushConstantData 
	{
		[FieldOffset(0)]
		public Vector2 Offset;
		[FieldOffset(16)]
		public Vector3 Color;

	}

	public class FirstAppT
	{
		private const int WIDTH = 960;
		private const int HEIGHT = 540;

		private bool Minimized = false;

		private WindowT Window;
		private DeviceT Device;
		private SwapChainT SwapChain;
		private PipelineT Pipeline;
		private ModelT Model;


		private VkPipelineLayout PipelineLayout;
		private VkCommandBuffer[] CommandBuffers;
		public FirstAppT()
		{
			Window = new(WIDTH, HEIGHT, "Hello Vulkan!");
			Device = new(ref Window);
			//SwapChain = new(ref Device, Window.GetExtent());

			LoadModels();
			CreatePipelineLayout();
			//CreatePipeline();
			RecreateSwapChain();
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
				}

				DrawFrame();
			}

			VulkanNative.vkDeviceWaitIdle(Device.Device);
			Destroy();
		}

		unsafe private void CreatePipelineLayout() 
		{
			VkPushConstantRange pushConstantRange = new();
			pushConstantRange.stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT;
			pushConstantRange.offset = 0;
			pushConstantRange.size = (uint)Marshal.SizeOf<SimplePushConstantData>();


			VkPipelineLayoutCreateInfo pipelineLayoutInfo = new();
			pipelineLayoutInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO;
			pipelineLayoutInfo.setLayoutCount = 0;
			pipelineLayoutInfo.pSetLayouts = null;
			pipelineLayoutInfo.pushConstantRangeCount = 1;
			pipelineLayoutInfo.pPushConstantRanges = &pushConstantRange;

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
			PipelineConfigInfo pipelineConfig = new();

			PipelineT.DefaultPipelineConfig(ref pipelineConfig);

			pipelineConfig.RenderPass = SwapChain.RenderPass;
			pipelineConfig.PipelineLayout = PipelineLayout;

			Pipeline = new(ref Device, Program.Directory + @"\TestVulkan\shaders\Svert.spv", Program.Directory + @"\TestVulkan\shaders\Sfrag.spv", pipelineConfig);
		}

		private void RecreateSwapChain() 
		{
			if (Minimized)
				return;

			VkExtent2D extent = Window.GetExtent();
			VulkanNative.vkDeviceWaitIdle(Device.Device);

			if (SwapChain == null)
				SwapChain = new(ref Device, extent);
			else 
			{
				SwapChain = new(ref Device, extent, ref SwapChain);
				if (SwapChain.SwapChainImages.Length != CommandBuffers.Length) 
				{
					FreeCommandBuffers();
					CreateCommandBuffers();
				}

			}

			CreatePipeline();
		}

		unsafe private void CreateCommandBuffers() 
		{
			CommandBuffers = new VkCommandBuffer[SwapChain.SwapChainImages.Length];

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
			/*
			for (int i = 0; i < CommandBuffers.Length; i++)
			{
				VkCommandBufferBeginInfo beginInfo = new();

				beginInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;

				if (VulkanNative.vkBeginCommandBuffer(CommandBuffers[i], &beginInfo) != VkResult.VK_SUCCESS) 
				{
					throw new Exception("failed to begin recording command buffer!");
				}

				VkRenderPassBeginInfo renderPassInfo = new();
				renderPassInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO;
				renderPassInfo.renderPass = SwapChain.RenderPass;
				renderPassInfo.framebuffer = SwapChain.SwapChainFramebuffers[i];

				renderPassInfo.renderArea.offset = new VkOffset2D(0, 0);
				renderPassInfo.renderArea.extent = SwapChain.SwapChainExtent;

				VkClearValue* clearValues = stackalloc VkClearValue[2];
				clearValues[0].color = new VkClearColorValue(0.1f, 0.1f, 0.1f, 1.0f);
				clearValues[1].depthStencil = new VkClearDepthStencilValue() { depth=1.0f, stencil=0 };

				renderPassInfo.clearValueCount = 2;
				renderPassInfo.pClearValues = clearValues;

				VulkanNative.vkCmdBeginRenderPass(CommandBuffers[i], &renderPassInfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);

				Pipeline.Bind(CommandBuffers[i]);

				Model.Bind(CommandBuffers[i]);
				Model.Draw(CommandBuffers[i]);

				VulkanNative.vkCmdEndRenderPass(CommandBuffers[i]);

				if (VulkanNative.vkEndCommandBuffer(CommandBuffers[i]) != VkResult.VK_SUCCESS) 
				{
					throw new Exception("failed to record command buffer!");
				}

			}*/
		}

		unsafe private void FreeCommandBuffers() 
		{
			fixed (VkCommandBuffer* commandBuffers = &CommandBuffers[0])
			{
				VulkanNative.vkFreeCommandBuffers(Device.Device, Device.CommandPool, (uint)CommandBuffers.Length, commandBuffers);
			}

			CommandBuffers = null;
		}

		static int Frame = 0;

		unsafe private void RecordCommandBuffer(int imageIndex)
		{
			Frame = (Frame + 1) % 1000;

			VkCommandBufferBeginInfo beginInfo = new();

			beginInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO;

			if (VulkanNative.vkBeginCommandBuffer(CommandBuffers[imageIndex], &beginInfo) != VkResult.VK_SUCCESS)
			{
				throw new Exception("failed to begin recording command buffer!");
			}

			VkRenderPassBeginInfo renderPassInfo = new();
			renderPassInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO;
			renderPassInfo.renderPass = SwapChain.RenderPass;
			renderPassInfo.framebuffer = SwapChain.SwapChainFramebuffers[imageIndex];

			renderPassInfo.renderArea.offset = new VkOffset2D(0, 0);
			renderPassInfo.renderArea.extent = SwapChain.SwapChainExtent;

			VkClearValue* clearValues = stackalloc VkClearValue[2];
			clearValues[0].color = new VkClearColorValue(0.01f, 0.01f, 0.01f, 1.0f);
			clearValues[1].depthStencil = new VkClearDepthStencilValue() { depth = 1.0f, stencil = 0 };

			renderPassInfo.clearValueCount = 2;
			renderPassInfo.pClearValues = clearValues;

			VulkanNative.vkCmdBeginRenderPass(CommandBuffers[imageIndex], &renderPassInfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);

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

			VulkanNative.vkCmdSetViewport(CommandBuffers[imageIndex], 0, 1, &viewport);
			VulkanNative.vkCmdSetScissor(CommandBuffers[imageIndex], 0, 1, &scissor);

			Pipeline.Bind(CommandBuffers[imageIndex]);

			Model.Bind(CommandBuffers[imageIndex]);

			for (int i = 0; i < 4; i++)
			{
				SimplePushConstantData push = new();

				push.Offset = new Vector2(-0.5f + Frame * 0.002f, -0.4f + i * 0.25f);
				push.Color = new Vector3(0.0f, 0.0f, 0.2f + 0.2f * i);

				VulkanNative.vkCmdPushConstants(CommandBuffers[imageIndex], PipelineLayout, VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT, 0, (uint)Marshal.SizeOf<SimplePushConstantData>(), &push);
				
				Model.Draw(CommandBuffers[imageIndex]);
			}

			VulkanNative.vkCmdEndRenderPass(CommandBuffers[imageIndex]);

			if (VulkanNative.vkEndCommandBuffer(CommandBuffers[imageIndex]) != VkResult.VK_SUCCESS)
			{
				throw new Exception("failed to record command buffer!");
			}

		}
		private void Sierpinski(ref List<VertexT> vertices, int depth, Vector2 left, Vector2 right, Vector2 top)
		{
			if (depth <= 0)
			{
				vertices.Add(new VertexT() { Position = top });
				vertices.Add(new VertexT() { Position = right });
				vertices.Add(new VertexT() { Position = left });
			}
			else
			{
				Vector2 leftTop = 0.5f * (left + top);
				Vector2 rightTop = 0.5f * (right + top);
				Vector2 leftRight = 0.5f * (left + right);
				Sierpinski(ref vertices, depth - 1, left, leftRight, leftTop);
				Sierpinski(ref vertices, depth - 1, leftRight, right, rightTop);
				Sierpinski(ref vertices, depth - 1, leftTop, rightTop, top);
			}
		}

		private void LoadModels() 
		{
			List<VertexT> vertices = new();

			vertices.Add(new VertexT() 
			{ 
				Position = new Vector2(0.0f,-0.5f),
				Color = new Vector3(1.0f,0.0f,0.0f)
			});
			vertices.Add(new VertexT() 
			{ 
				Position = new Vector2(0.5f, 0.5f),
				Color = new Vector3(0.0f, 1.0f, 0.0f)
			});
			vertices.Add(new VertexT() 
			{ 
				Position = new Vector2(-0.5f, 0.5f),
				Color = new Vector3(0.0f, 0.0f, 1.0f)
			});

			//Sierpinski(ref vertices, 5, new Vector2(-0.5f, 0.5f), new Vector2( 0.5f, 0.5f), new Vector2(0.0f, -0.5f));

			VertexT[] arrVertices = vertices.ToArray();

			Model = new ModelT(ref Device, ref arrVertices);
		}

		private void DrawFrame() 
		{
			uint imageIndex = default;

			VkResult result = SwapChain.AcquireNextImage(ref imageIndex);

			if (result == VkResult.VK_ERROR_OUT_OF_DATE_KHR) 
			{
				RecreateSwapChain();
				return;
			}

			if (result != VkResult.VK_SUCCESS && result != VkResult.VK_SUBOPTIMAL_KHR)
				throw new Exception("Fail To Acquire Next Image");

			RecordCommandBuffer((int)imageIndex);
			result = SwapChain.SubmitCommandBuffers(ref CommandBuffers[imageIndex], ref imageIndex);

			if (result == VkResult.VK_ERROR_OUT_OF_DATE_KHR || result == VkResult.VK_SUBOPTIMAL_KHR) 
			{
				RecreateSwapChain();
				return;
			}

			if (result != VkResult.VK_SUCCESS)
				throw new Exception("Fail to present");
		}

		unsafe public void Destroy() 
		{
			SwapChain.DestroySwapChain();

			VulkanNative.vkDestroyPipelineLayout(Device.Device, PipelineLayout, null);

			Pipeline.DestroyPipeline();
			Device.DestroyDebugMessenger();
			Window.DestroyWindow();
		}
	}
}
