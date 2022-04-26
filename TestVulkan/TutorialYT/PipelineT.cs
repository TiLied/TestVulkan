using Evergine.Bindings.Vulkan;
using System;

namespace TestVulkan
{
	public struct PipelineConfigInfo
	{
		public VkVertexInputBindingDescription[] BindingDescriptions = Array.Empty<VkVertexInputBindingDescription>();
		public VkVertexInputAttributeDescription[] AttributeDescriptions = Array.Empty<VkVertexInputAttributeDescription>();

		public VkPipelineViewportStateCreateInfo ViewportInfo = new();
		public VkPipelineInputAssemblyStateCreateInfo InputAssemblyInfo = new();
		public VkPipelineRasterizationStateCreateInfo RasterizationInfo = new();
		public VkPipelineMultisampleStateCreateInfo MultisampleInfo = new();
		public VkPipelineColorBlendAttachmentState ColorBlendAttachment = new();
		public VkPipelineColorBlendStateCreateInfo ColorBlendInfo = new();
		public VkPipelineDepthStencilStateCreateInfo DepthStencilInfo = new();

		public VkDynamicState[] DynamicStateEnables = Array.Empty<VkDynamicState>();
		public VkPipelineDynamicStateCreateInfo DynamicStateInfo = new();

		public VkPipelineLayout PipelineLayout = VkPipelineLayout.Null;
		public VkRenderPass RenderPass = VkRenderPass.Null;
		public uint Subpass = 0;

		public PipelineConfigInfo() { }
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

			VkVertexInputBindingDescription[] bindingDescriptions = configInfo.BindingDescriptions;
			VkVertexInputAttributeDescription[] attributeDescriptions = configInfo.AttributeDescriptions;

			VkPipelineVertexInputStateCreateInfo vertexInputInfo = new();
			vertexInputInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO;
			if (attributeDescriptions.Length == 0)
			{
				vertexInputInfo.vertexAttributeDescriptionCount = 0;
				vertexInputInfo.vertexBindingDescriptionCount = 0;
				vertexInputInfo.pVertexAttributeDescriptions = null;
				vertexInputInfo.pVertexBindingDescriptions = null;
			}
			else
			{
				vertexInputInfo.vertexAttributeDescriptionCount = (uint)attributeDescriptions.Length;
				vertexInputInfo.vertexBindingDescriptionCount = (uint)bindingDescriptions.Length;
				fixed (VkVertexInputAttributeDescription* pAD = &attributeDescriptions[0])
				{
					vertexInputInfo.pVertexAttributeDescriptions = pAD;
				}
				fixed (VkVertexInputBindingDescription* pBD = &bindingDescriptions[0])
				{
					vertexInputInfo.pVertexBindingDescriptions = pBD;
				}
			}

 			VkGraphicsPipelineCreateInfo pipelineInfo = new();
			pipelineInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO;
			pipelineInfo.stageCount = 2;
			pipelineInfo.pStages = shaderStages;
			pipelineInfo.pVertexInputState = &vertexInputInfo;
			pipelineInfo.pInputAssemblyState = &configInfo.InputAssemblyInfo;
			pipelineInfo.pViewportState = &configInfo.ViewportInfo;
			pipelineInfo.pRasterizationState = &configInfo.RasterizationInfo;
			pipelineInfo.pMultisampleState = &configInfo.MultisampleInfo;
			pipelineInfo.pColorBlendState = &configInfo.ColorBlendInfo;
			pipelineInfo.pDepthStencilState = &configInfo.DepthStencilInfo;
			pipelineInfo.pDynamicState = &configInfo.DynamicStateInfo;

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

		unsafe static public void DefaultPipelineConfig(ref PipelineConfigInfo configInfo)
		{
			configInfo.InputAssemblyInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO;
			configInfo.InputAssemblyInfo.topology = VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;
			configInfo.InputAssemblyInfo.primitiveRestartEnable = false;

			/*
			configInfo.Viewport.x = 0.0f;
			configInfo.Viewport.y = 0.0f;
			configInfo.Viewport.width = width;
			configInfo.Viewport.height = height;
			configInfo.Viewport.minDepth = 0.0f;
			configInfo.Viewport.maxDepth = 1.0f;

			configInfo.Scissor.offset = new VkOffset2D(0, 0);
			configInfo.Scissor.extent = new VkExtent2D(width, height);
			*/

			configInfo.ViewportInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO;
			configInfo.ViewportInfo.viewportCount = 1;
			configInfo.ViewportInfo.pViewports = null;
			configInfo.ViewportInfo.scissorCount = 1;
			configInfo.ViewportInfo.pScissors = null;

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

			fixed (VkPipelineColorBlendAttachmentState* colorBlendAttachment = &configInfo.ColorBlendAttachment) 
			{
				configInfo.ColorBlendInfo.pAttachments = colorBlendAttachment;
			}

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

			configInfo.DynamicStateEnables = new VkDynamicState[2] { VkDynamicState.VK_DYNAMIC_STATE_VIEWPORT, VkDynamicState.VK_DYNAMIC_STATE_SCISSOR };

			configInfo.DynamicStateInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO;
			fixed (VkDynamicState* dynamicStateEnable = &configInfo.DynamicStateEnables[0])
			{
				configInfo.DynamicStateInfo.pDynamicStates = dynamicStateEnable;
			}
			configInfo.DynamicStateInfo.dynamicStateCount = (uint)configInfo.DynamicStateEnables.Length;
			configInfo.DynamicStateInfo.flags = 0;

			configInfo.BindingDescriptions = VertexT.GetBindingDescriptions();
			configInfo.AttributeDescriptions = VertexT.GetAttributeDescriptions();
		}

		static public void EnableAlphaBlending(ref PipelineConfigInfo configInfo) 
		{

			configInfo.ColorBlendAttachment.colorWriteMask = VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT | VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT;
			configInfo.ColorBlendAttachment.blendEnable = true;
			configInfo.ColorBlendAttachment.srcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_SRC_ALPHA;
			configInfo.ColorBlendAttachment.dstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA;
			configInfo.ColorBlendAttachment.colorBlendOp = VkBlendOp.VK_BLEND_OP_ADD;
			configInfo.ColorBlendAttachment.srcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE;
			configInfo.ColorBlendAttachment.dstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO;
			configInfo.ColorBlendAttachment.alphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD;

		}

		public void Bind(VkCommandBuffer commandBuffer) 
		{
			VulkanNative.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, GraphicsPipeline);
		}

		unsafe public void DestroyPipeline()
		{
			VulkanNative.vkDestroyShaderModule(Device.Device, VertShaderModule, null);
			VulkanNative.vkDestroyShaderModule(Device.Device, FragShaderModule, null);
			VulkanNative.vkDestroyPipeline(Device.Device, GraphicsPipeline, null);

		}
	}
}
