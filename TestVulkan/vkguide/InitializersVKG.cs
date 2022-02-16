using Silk.NET.Vulkan;
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

		unsafe public static ImageCreateInfo ImageCreateInfo(Format format, ImageUsageFlags usageFlags, Extent3D extent)
		{
			ImageCreateInfo info = new();
			info.SType = StructureType.ImageCreateInfo;
			info.PNext = null;

			info.ImageType = ImageType.ImageType2D;

			info.Format = format;
			info.Extent = extent;

			info.MipLevels = 1;
			info.ArrayLayers = 1;
			info.Samples = SampleCountFlags.SampleCount1Bit;
			info.Tiling = ImageTiling.Optimal;
			info.Usage = usageFlags;

			return info;
		}

		unsafe public static ImageViewCreateInfo ImageviewCreateInfo(Format format, Image image, ImageAspectFlags aspectFlags)
		{
			//build a image-view for the depth image to use for rendering
			ImageViewCreateInfo info = new();
			info.SType = StructureType.ImageViewCreateInfo;
			info.PNext = null;

			info.ViewType = ImageViewType.ImageViewType2D;
			info.Image = image;
			info.Format = format;
			info.SubresourceRange.BaseMipLevel = 0;
			info.SubresourceRange.LevelCount = 1;
			info.SubresourceRange.BaseArrayLayer = 0;
			info.SubresourceRange.LayerCount = 1;
			info.SubresourceRange.AspectMask = aspectFlags;

			return info;
		}

		unsafe public static PipelineDepthStencilStateCreateInfo DepthStencilCreateInfo(bool bDepthTest, bool bDepthWrite, CompareOp compareOp)
		{
			PipelineDepthStencilStateCreateInfo info = new();
			info.SType = StructureType.PipelineDepthStencilStateCreateInfo;
			info.PNext = null;

			info.DepthTestEnable = bDepthTest ? true : false;
			info.DepthWriteEnable = bDepthWrite ? true : false;
			info.DepthCompareOp = bDepthTest ? compareOp : CompareOp.Always;
			info.DepthBoundsTestEnable = false;
			info.MinDepthBounds = 0.0f; // Optional
			info.MaxDepthBounds = 1.0f; // Optional
			info.StencilTestEnable = false;

			return info;
		}

		unsafe public static DescriptorSetLayoutBinding DescriptorsetLayoutBinding(DescriptorType type, ShaderStageFlags stageFlags, uint binding)
		{
			DescriptorSetLayoutBinding setbind = new();

			setbind.Binding = binding;
			setbind.DescriptorCount = 1;
			setbind.DescriptorType = type;
			setbind.PImmutableSamplers = null;
			setbind.StageFlags = stageFlags;

			return setbind;
		}

		unsafe public static WriteDescriptorSet WriteDescriptorBuffer(DescriptorType type, DescriptorSet dstSet,ref DescriptorBufferInfo bufferInfo, uint binding)
		{
			WriteDescriptorSet write = new();
			write.SType = StructureType.WriteDescriptorSet;
			write.PNext = null;

			write.DstBinding = binding;
			write.DstSet = dstSet;
			write.DescriptorCount = 1;
			write.DescriptorType = type;

			fixed (DescriptorBufferInfo* bufferInfoPtr = &bufferInfo) 
			{
				write.PBufferInfo = bufferInfoPtr;
			}

			return write;
		}

		unsafe public static CommandBufferBeginInfo CommandBufferBeginInfo(CommandBufferUsageFlags flags = 0)
		{
			CommandBufferBeginInfo info = new();

			info.SType = StructureType.CommandBufferBeginInfo;
			info.PNext = null;

			info.PInheritanceInfo = null;
			info.Flags = flags;
			
			return info;
		}

		unsafe public static SubmitInfo SubmitInfo(ref CommandBuffer cmd)
		{
			SubmitInfo info = new();
			info.SType = StructureType.SubmitInfo;
			info.PNext = null;

			info.WaitSemaphoreCount = 0;
			info.PWaitSemaphores = null;
			info.PWaitDstStageMask = null;
			info.CommandBufferCount = 1;

			fixed (CommandBuffer* cmdPtr = &cmd)
			{
				info.PCommandBuffers = cmdPtr;
			}

			info.SignalSemaphoreCount = 0;
			info.PSignalSemaphores = null;

			return info;
		}

		unsafe public static SamplerCreateInfo SamplerCreateInfo(Filter filters, SamplerAddressMode samplerAddressMode = SamplerAddressMode.Repeat)
		{
			SamplerCreateInfo info = new();
			info.SType = StructureType.SamplerCreateInfo;
			info.PNext = null;

			info.MagFilter = filters;
			info.MinFilter = filters;
			info.AddressModeU = samplerAddressMode;
			info.AddressModeV = samplerAddressMode;
			info.AddressModeW = samplerAddressMode;

			return info;
		}

		unsafe public static WriteDescriptorSet WriteDescriptorImage(DescriptorType type, DescriptorSet dstSet,ref DescriptorImageInfo imageInfo, uint binding)
		{
			WriteDescriptorSet write = new();
			write.SType = StructureType.WriteDescriptorSet;
			write.PNext = null;

			write.DstBinding = binding;
			write.DstSet = dstSet;
			write.DescriptorCount = 1;
			write.DescriptorType = type;

			fixed (DescriptorImageInfo* imageInfoPtr = &imageInfo)
			{
				write.PImageInfo = imageInfoPtr;
			}

			return write;
		}
	}
}
