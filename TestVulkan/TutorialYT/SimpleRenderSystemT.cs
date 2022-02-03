using Evergine.Bindings.Vulkan;
using System;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	public class SimpleRenderSystemT
	{
		private DeviceT Device;

		private PipelineT Pipeline;
		private VkPipelineLayout PipelineLayout;

		public SimpleRenderSystemT(ref DeviceT device, VkRenderPass renderPass)
		{
			Device = device;
			CreatePipelineLayout();
			CreatePipeline(renderPass);
		}

		unsafe public void RenderGameObjects(VkCommandBuffer commandBuffer, ref List<GameObjectT> gameObjects)
		{
			Pipeline.Bind(commandBuffer);

			foreach (GameObjectT obj in gameObjects)
			{
				//obj.Transform2D.Rotation = (float)((obj.Transform2D.Rotation + 0.01f) % (Math.PI * 2));

				SimplePushConstantData push = new();
				push.Offset = obj.Transform2D.Translation;
				push.Color = obj.Color;
				push.Transform = obj.Transform2D.Mat4();

				VulkanNative.vkCmdPushConstants(commandBuffer, PipelineLayout, VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT, 0, (uint)Marshal.SizeOf<SimplePushConstantData>(), &push);

				obj.Model.Bind(commandBuffer);
				obj.Model.Draw(commandBuffer);
			}
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

		private void CreatePipeline(VkRenderPass renderPass)
		{
			PipelineConfigInfo pipelineConfig = new();

			PipelineT.DefaultPipelineConfig(ref pipelineConfig);

			pipelineConfig.RenderPass = renderPass;
			pipelineConfig.PipelineLayout = PipelineLayout;

			Pipeline = new(ref Device, Program.Directory + @"\TestVulkan\shaders\Svert.spv", Program.Directory + @"\TestVulkan\shaders\Sfrag.spv", pipelineConfig);
		}
		unsafe public void DestroySRS()
		{
			VulkanNative.vkDestroyPipelineLayout(Device.Device, PipelineLayout, null);
			Pipeline.DestroyPipeline();
		}
	}
}