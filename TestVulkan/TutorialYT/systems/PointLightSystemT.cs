﻿using Evergine.Bindings.Vulkan;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	public struct PointLightConstant 
	{
		public Vector4 Position;
		public Vector4 Color;
		public float Radius; 
	}

	public class PointLightSystemT
	{
		private DeviceT Device;

		private PipelineT Pipeline;
		private VkPipelineLayout PipelineLayout;

		public PointLightSystemT(ref DeviceT device, VkRenderPass renderPass, VkDescriptorSetLayout globalSetLayout)
		{
			Device = device;
			CreatePipelineLayout(globalSetLayout);
			CreatePipeline(renderPass);
		}

		public void Update(ref FrameInfo frameInfo, ref GlobalUbo ubo) 
		{
			int lightIndex = 0;
			foreach (KeyValuePair<int,GameObjectT> keyValue in GameObjectT.Map)
			{
				GameObjectT obj = keyValue.Value;
				if(obj.PointLight == null)
					continue;

				//copy
				ubo.PointLights[lightIndex].Position = new Vector4(obj.Transform.Translation,1.0f);
				ubo.PointLights[lightIndex].Color = new Vector4(obj.Color, obj.PointLight.Value.lightIntensity);
				lightIndex++;
			}

			ubo.NumLights = lightIndex;
		}

		unsafe public void Render(ref FrameInfo frameInfo)
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
				if (obj.PointLight == null)
					continue;

				PointLightConstant push = new();
				push.Position = new Vector4(obj.Transform.Translation, 1.0f);
				push.Color = new Vector4(obj.Color, obj.PointLight.Value.lightIntensity);
				push.Radius = obj.Transform.Scale.X;


				VulkanNative.vkCmdPushConstants(frameInfo.CommandBuffer, 
					PipelineLayout, 
					VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT, 
					0, 
					(uint)Marshal.SizeOf<PointLightConstant>(), 
					&push);

				VulkanNative.vkCmdDraw(frameInfo.CommandBuffer, 6, 1, 0, 0);
			}
		}
		unsafe private void CreatePipelineLayout(VkDescriptorSetLayout globalSetLayout)
		{
			VkPushConstantRange pushConstantRange = new();
			pushConstantRange.stageFlags = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT;
			pushConstantRange.offset = 0;
			pushConstantRange.size = (uint)Marshal.SizeOf<PointLightConstant>();

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
			pipelineConfig.AttributeDescriptions = Array.Empty<VkVertexInputAttributeDescription>();
			pipelineConfig.BindingDescriptions = Array.Empty<VkVertexInputBindingDescription>();

			pipelineConfig.RenderPass = renderPass;
			pipelineConfig.PipelineLayout = PipelineLayout;

			Pipeline = new(ref Device, 
				Program.Directory + @"\TestVulkan\shaders\PointLightVert.spv", 
				Program.Directory + @"\TestVulkan\shaders\PointLightFrag.spv", 
				pipelineConfig);
		}

		unsafe public void DestroyPLS()
		{
			VulkanNative.vkDestroyPipelineLayout(Device.Device, PipelineLayout, null);
			Pipeline.DestroyPipeline();
		}
	}
}