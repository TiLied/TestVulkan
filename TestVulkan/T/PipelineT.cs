using Evergine.Bindings.Vulkan;
using System;

namespace TestVulkan
{
	public struct PipelineConfigInfo
	{
		public VkViewport Viewport;
		public VkRect2D Scissor;
		
		public VkPipelineInputAssemblyStateCreateInfo InputAssemblyInfo;
		public VkPipelineRasterizationStateCreateInfo RasterizationInfo;
		public VkPipelineMultisampleStateCreateInfo MultisampleInfo;
		public VkPipelineColorBlendAttachmentState ColorBlendAttachment;
		public VkPipelineColorBlendStateCreateInfo ColorBlendInfo;
		public VkPipelineDepthStencilStateCreateInfo DepthStencilInfo;
		public VkPipelineLayout PipelineLayout = VkPipelineLayout.Null;
		public VkRenderPass RenderPass = VkRenderPass.Null;
		public uint Subpass = 0;
	}

	public class PipelineT
	{
		private DeviceT Device;

		private VkPipeline GraphicsPipeline;
		private VkShaderModule VertShaderModule;
		private VkShaderModule FragShaderModule;

		public PipelineT(ref DeviceT device, string vertPath, string fragPath, PipelineConfigInfo configInfo)
		{
			Device = device;
			CreateGraphicsPipeline(vertPath, fragPath, configInfo);
		}

		private static byte[] ReadFile(string filename)
		{
			byte[] bytes = File.ReadAllBytes(filename);

			if (bytes == null)
				throw new Exception("file is null: " + filename);

			return bytes;
		}

		unsafe private void CreateGraphicsPipeline(string vertPath, string fragPath, PipelineConfigInfo configInfo) 
		{
			byte[] vertShaderCode = ReadFile(vertPath);
			byte[] fragShaderCode = ReadFile(fragPath);

			CreateShaderModule(vertShaderCode, ref VertShaderModule);
			CreateShaderModule(fragShaderCode, ref FragShaderModule);

			VkPipelineShaderStageCreateInfo* shaderStages = stackalloc VkPipelineShaderStageCreateInfo[2];

			shaderStages[0].sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
			shaderStages[0].stage = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT;
			shaderStages[0].module = VertShaderModule;
			shaderStages[0].pName = (byte*)"main".ReturnIntPtr();
			shaderStages[0].flags = 0;
			shaderStages[0].pNext = null;
			shaderStages[0].pSpecializationInfo = null;

			shaderStages[1].sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
			shaderStages[1].stage = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT;
			shaderStages[1].module = FragShaderModule;
			shaderStages[1].pName = (byte*)"main".ReturnIntPtr();
			shaderStages[1].flags = 0;
			shaderStages[1].pNext = null;
			shaderStages[1].pSpecializationInfo = null;

			VkPipelineVertexInputStateCreateInfo vertexInputInfo = new();
			vertexInputInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO;
			vertexInputInfo.vertexAttributeDescriptionCount = 0;
			vertexInputInfo.vertexBindingDescriptionCount = 0;
			vertexInputInfo.pVertexAttributeDescriptions = null;
			vertexInputInfo.pVertexBindingDescriptions = null;

			VkPipelineViewportStateCreateInfo viewportInfo = new();
			viewportInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO;
			viewportInfo.viewportCount = 1;
			viewportInfo.pViewports = &configInfo.Viewport;
			viewportInfo.scissorCount = 1;
			viewportInfo.pScissors = &configInfo.Scissor;

			VkGraphicsPipelineCreateInfo pipelineInfo = new();
			pipelineInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO;
			pipelineInfo.stageCount = 2;
			pipelineInfo.pStages = shaderStages;
			pipelineInfo.pVertexInputState = &vertexInputInfo;
			pipelineInfo.pInputAssemblyState = &configInfo.InputAssemblyInfo;
			pipelineInfo.pViewportState = &viewportInfo;
			pipelineInfo.pRasterizationState = &configInfo.RasterizationInfo;
			pipelineInfo.pMultisampleState = &configInfo.MultisampleInfo;
			pipelineInfo.pColorBlendState = &configInfo.ColorBlendInfo;
			pipelineInfo.pDepthStencilState = &configInfo.DepthStencilInfo;
			pipelineInfo.pDynamicState = null;

			pipelineInfo.layout = configInfo.PipelineLayout;
			pipelineInfo.renderPass = configInfo.RenderPass;
			pipelineInfo.subpass = configInfo.Subpass;


			pipelineInfo.basePipelineIndex = -1;
			pipelineInfo.basePipelineHandle = VkPipeline.Null;


			fixed (VkPipeline* graphicsPipeline = &GraphicsPipeline)
			{
				if (VulkanNative.vkCreateGraphicsPipelines(Device.Device, VkPipelineCache.Null, 1, &pipelineInfo, null, graphicsPipeline) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to create graphics pipeline!");
				}
			}
			
		}

		unsafe private void CreateShaderModule(byte[] code, ref VkShaderModule shaderModule) 
		{
			VkShaderModuleCreateInfo createInfo = new();
			createInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO;
			createInfo.codeSize = (nuint)code.Length;

			fixed (byte* codePtr = code)
			{
				createInfo.pCode = (uint*)codePtr;
			}

			fixed (VkShaderModule* shaderModuleL = &shaderModule)
			{
				if (VulkanNative.vkCreateShaderModule(Device.Device, &createInfo, null, shaderModuleL) != VkResult.VK_SUCCESS)
				{
					throw new Exception("failed to create shader module!");
				}
			}
			
		}

		unsafe static public PipelineConfigInfo DefaultPipelineConfig(uint width, uint height)
		{
			PipelineConfigInfo configInfo = new();

			configInfo.InputAssemblyInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO;
			configInfo.InputAssemblyInfo.topology = VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;
			configInfo.InputAssemblyInfo.primitiveRestartEnable = false;

			configInfo.Viewport.x = 0.0f;
			configInfo.Viewport.y = 0.0f;
			configInfo.Viewport.width = width;
			configInfo.Viewport.height = height;
			configInfo.Viewport.minDepth = 0.0f;
			configInfo.Viewport.maxDepth = 1.0f;

			configInfo.Scissor.offset = new VkOffset2D(0, 0);
			configInfo.Scissor.extent = new VkExtent2D(width, height);

			configInfo.RasterizationInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO;
			configInfo.RasterizationInfo.depthClampEnable = false;
			configInfo.RasterizationInfo.rasterizerDiscardEnable = false;
			configInfo.RasterizationInfo.polygonMode = VkPolygonMode.VK_POLYGON_MODE_FILL;
			configInfo.RasterizationInfo.lineWidth = 1.0f;
			configInfo.RasterizationInfo.cullMode = VkCullModeFlags.VK_CULL_MODE_NONE;
			configInfo.RasterizationInfo.frontFace = VkFrontFace.VK_FRONT_FACE_CLOCKWISE;
			configInfo.RasterizationInfo.depthBiasEnable = false;
			configInfo.RasterizationInfo.depthBiasConstantFactor = 0.0f;  // Optional
			configInfo.RasterizationInfo.depthBiasClamp = 0.0f;           // Optional
			configInfo.RasterizationInfo.depthBiasSlopeFactor = 0.0f;     // Optional


			configInfo.MultisampleInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO;
			configInfo.MultisampleInfo.sampleShadingEnable = false;
			configInfo.MultisampleInfo.rasterizationSamples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT;
			configInfo.MultisampleInfo.minSampleShading = 1.0f;           // Optional
			configInfo.MultisampleInfo.pSampleMask = null;             // Optional
			configInfo.MultisampleInfo.alphaToCoverageEnable = false;  // Optional
			configInfo.MultisampleInfo.alphaToOneEnable = false;       // Optional



			configInfo.ColorBlendAttachment.colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT;
			configInfo.ColorBlendAttachment.blendEnable = false;
			configInfo.ColorBlendAttachment.srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE;   // Optional
			configInfo.ColorBlendAttachment.dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO;  // Optional
			configInfo.ColorBlendAttachment.colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD;              // Optional
			configInfo.ColorBlendAttachment.srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE;   // Optional
			configInfo.ColorBlendAttachment.dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO;  // Optional
			configInfo.ColorBlendAttachment.alphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD;              // Optional

			configInfo.ColorBlendInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO;
			configInfo.ColorBlendInfo.logicOpEnable = false;
			configInfo.ColorBlendInfo.logicOp = VkLogicOp.VK_LOGIC_OP_COPY;  // Optional
			configInfo.ColorBlendInfo.attachmentCount = 1;
			configInfo.ColorBlendInfo.pAttachments = &configInfo.ColorBlendAttachment;
			configInfo.ColorBlendInfo.blendConstants_0 = 0.0f;  // Optional
			configInfo.ColorBlendInfo.blendConstants_1 = 0.0f;  // Optional
			configInfo.ColorBlendInfo.blendConstants_2 = 0.0f;  // Optional
			configInfo.ColorBlendInfo.blendConstants_3 = 0.0f;  // Optional


			configInfo.DepthStencilInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO;
			configInfo.DepthStencilInfo.depthTestEnable = true;
			configInfo.DepthStencilInfo.depthWriteEnable = true;
			configInfo.DepthStencilInfo.depthCompareOp = VkCompareOp.VK_COMPARE_OP_LESS;
			configInfo.DepthStencilInfo.depthBoundsTestEnable = false;
			configInfo.DepthStencilInfo.minDepthBounds = 0.0f;  // Optional
			configInfo.DepthStencilInfo.maxDepthBounds = 1.0f;  // Optional
			configInfo.DepthStencilInfo.stencilTestEnable = false;
			configInfo.DepthStencilInfo.front = new();  // Optional
			configInfo.DepthStencilInfo.back = new();   // Optional

			return configInfo;
		}

		unsafe public void DestroyPipeline()
		{
			VulkanNative.vkDestroyShaderModule(Device.Device, VertShaderModule, null);
			VulkanNative.vkDestroyShaderModule(Device.Device, FragShaderModule, null);
			VulkanNative.vkDestroyPipeline(Device.Device, GraphicsPipeline, null);

		}
	}
}
