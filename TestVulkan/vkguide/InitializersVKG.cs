﻿using Silk.NET.Vulkan;
using System;

namespace TestVulkan
{
	public class InitializersVKG
	{
		unsafe public static CommandPoolCreateInfo CommandPoolCreateInfo(uint queueFamilyIndex, CommandPoolCreateFlags flags = 0) 
		{
			CommandPoolCreateInfo info = new();
			info.SType = StructureType.CommandPoolCreateInfo;
			info.PNext = null;

			info.QueueFamilyIndex = queueFamilyIndex;
			info.Flags = flags;

			return info;
		}

		unsafe public static CommandBufferAllocateInfo CommandBufferAllocateInfo(CommandPool pool, uint count = 1, CommandBufferLevel level = CommandBufferLevel.Primary) 
		{
			CommandBufferAllocateInfo info = new();
			info.SType = StructureType.CommandBufferAllocateInfo;
			info.PNext = null;

			info.CommandPool = pool;
			info.CommandBufferCount = count;
			info.Level = level;

			return info;
		}

		unsafe public static PipelineShaderStageCreateInfo PipelineShaderStageCreateInfo(ShaderStageFlags stage, ShaderModule shaderModule)
		{
			PipelineShaderStageCreateInfo info = new();
			info.SType = StructureType.PipelineShaderStageCreateInfo;
			info.PNext = null;

			//shader stage
			info.Stage = stage;
			//module containing the code for this shader stage
			info.Module = shaderModule;
			//the entry point of the shader
			info.PName = (byte*)"main".ReturnIntPtr();

			return info;
		}

		unsafe public static PipelineVertexInputStateCreateInfo VertexInputStateCreateInfo()
		{
			PipelineVertexInputStateCreateInfo info = new();
			info.SType = StructureType.PipelineVertexInputStateCreateInfo;
			info.PNext = null;

			//no vertex bindings or attributes
			info.VertexBindingDescriptionCount = 0;
			info.VertexAttributeDescriptionCount = 0;

			return info;
		}

		unsafe public static PipelineInputAssemblyStateCreateInfo InputAssemblyCreateInfo(PrimitiveTopology topology)
		{
			PipelineInputAssemblyStateCreateInfo info = new();
			info.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
			info.PNext = null;

			info.Topology = topology;
			//we are not going to use primitive restart on the entire tutorial so leave it on false
			info.PrimitiveRestartEnable = false;

			return info;
		}

		unsafe public static PipelineRasterizationStateCreateInfo RasterizationStateCreateInfo(PolygonMode polygonMode)
		{
			PipelineRasterizationStateCreateInfo info = new();
			info.SType = StructureType.PipelineRasterizationStateCreateInfo;
			info.PNext = null;

			info.DepthClampEnable = false;
			//discards all primitives before the rasterization stage if enabled which we don't want
			info.RasterizerDiscardEnable = false;

			info.PolygonMode = polygonMode;
			info.LineWidth = 1.0f;
			//no backface cull
			info.CullMode = CullModeFlags.CullModeNone;
			info.FrontFace = FrontFace.Clockwise;
			//no depth bias
			info.DepthBiasEnable = false;
			info.DepthBiasConstantFactor = 0.0f;
			info.DepthBiasClamp = 0.0f;
			info.DepthBiasSlopeFactor = 0.0f;

			return info;
		}

		unsafe public static PipelineMultisampleStateCreateInfo MultisamplingStateCreateInfo()
		{
			PipelineMultisampleStateCreateInfo info = new();
			info.SType = StructureType.PipelineMultisampleStateCreateInfo;
			info.PNext = null;

			info.SampleShadingEnable = false;
			//multisampling defaulted to no multisampling (1 sample per pixel)
			info.RasterizationSamples = SampleCountFlags.SampleCount1Bit;
			info.MinSampleShading = 1.0f;
			info.PSampleMask = null;
			info.AlphaToCoverageEnable = false;
			info.AlphaToOneEnable = false;

			return info;
		}

		public static PipelineColorBlendAttachmentState ColorBlendAttachmentState()
		{
			PipelineColorBlendAttachmentState colorBlendAttachment = new();
			colorBlendAttachment.ColorWriteMask = ColorComponentFlags.ColorComponentRBit | ColorComponentFlags.ColorComponentGBit | ColorComponentFlags.ColorComponentBBit | ColorComponentFlags.ColorComponentABit;
			colorBlendAttachment.BlendEnable = false;

			return colorBlendAttachment;
		}

		unsafe public static PipelineLayoutCreateInfo PipelineLayoutCreateInfo()
		{
			PipelineLayoutCreateInfo info = new();
			info.SType = StructureType.PipelineLayoutCreateInfo;
			info.PNext = null;

			//empty defaults
			info.Flags = 0;
			info.SetLayoutCount = 0;
			info.PSetLayouts = null;
			info.PushConstantRangeCount = 0;
			info.PPushConstantRanges = null;

			return info;
		}

		unsafe public static FenceCreateInfo FenceCreateInfo(FenceCreateFlags flags = 0)
		{
			FenceCreateInfo fenceCreateInfo = new();

			fenceCreateInfo.SType = StructureType.FenceCreateInfo;
			fenceCreateInfo.PNext = null;
			fenceCreateInfo.Flags = flags;

			return fenceCreateInfo;
		}

		unsafe public static SemaphoreCreateInfo SemaphoreCreateInfo(SemaphoreCreateFlags flags = 0)
		{
			SemaphoreCreateInfo semCreateInfo = new();

			semCreateInfo.SType = StructureType.SemaphoreCreateInfo;
			semCreateInfo.PNext = null;
			semCreateInfo.Flags = (uint)flags;

			return semCreateInfo;
		}

	}
}
