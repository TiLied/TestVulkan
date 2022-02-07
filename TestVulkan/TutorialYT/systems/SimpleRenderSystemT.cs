using Evergine.Bindings.Vulkan;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	[StructLayout(LayoutKind.Explicit)]
	public struct SimplePushConstantData
	{
		[FieldOffset(0)]
		public Matrix4x4 ModelMatrix = Matrix4x4.Identity;
		[FieldOffset(64)]
		public Matrix4x4 NormalMatrix = Matrix4x4.Identity;

	}
	public class SimpleRenderSystemT
	{
		private DeviceT Device;

		private PipelineT Pipeline;
		private VkPipelineLayout PipelineLayout;

		public SimpleRenderSystemT(ref DeviceT device, VkRenderPass renderPass, VkDescriptorSetLayout globalSetLayout)
		{
			Device = device;
			CreatePipelineLayout(globalSetLayout);
			CreatePipeline(renderPass);
		}

		unsafe public void RenderGameObjects(ref FrameInfo frameInfo)
		{
			Pipeline.Bind(frameInfo.CommandBuffer);

			fixed (VkDescriptorSet* globalDescriptorSet = &frameInfo.GlobalDescriptorSet)
			{
				VulkanNative.vkCmdBindDescriptorSets(frameInfo.CommandBuffer,
					VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
					PipelineLayout,
					0,
					1,
					globalDescriptorSet,
					0,
					null);
			}

			foreach (KeyValuePair<int, GameObjectT> keyValue in GameObjectT.Map)
			{
				GameObjectT obj = keyValue.Value;
				if (obj.Model == null)
					continue;

				//obj.Transform.Angle = obj.Transform.Angle + 0.01f % (MathF.PI * 2);

				SimplePushConstantData push = new();

				push.ModelMatrix = obj.Transform.Mat4();
				push.NormalMatrix = obj.Transform.NormalMatrix();

				VulkanNative.vkCmdPushConstants(frameInfo.CommandBuffer, PipelineLayout, VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT, 0, (uint)Marshal.SizeOf<SimplePushConstantData>(), &push);

				obj.Model.Bind(frameInfo.CommandBuffer);
				obj.Model.Draw(frameInfo.CommandBuffer);
			}
		}
		unsafe private void CreatePipelineLayout(VkDescriptorSetLayout globalSetLayout)
		{
			VkPushConstantRange pushConstantRange = new();
			pushConstantRange.stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT;
			pushConstantRange.offset = 0;
			pushConstantRange.size = (uint)Marshal.SizeOf<SimplePushConstantData>();

			VkDescriptorSetLayout[] descriptorSetLayouts = new VkDescriptorSetLayout[1];
			descriptorSetLayouts[0] = globalSetLayout;

			VkPipelineLayoutCreateInfo pipelineLayoutInfo = new();
			pipelineLayoutInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO;
			pipelineLayoutInfo.setLayoutCount = (uint)descriptorSetLayouts.Length;

			fixed (VkDescriptorSetLayout* pLayouts = &descriptorSetLayouts[0])
			{
				pipelineLayoutInfo.pSetLayouts = pLayouts;
			}

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