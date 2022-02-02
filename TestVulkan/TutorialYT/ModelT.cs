using Evergine.Bindings.Vulkan;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	public struct VertexT 
	{
		public Vector2 Position;
		public Vector3 Color;

		static public VkVertexInputBindingDescription[] GetBindingDescriptions() 
		{
			VkVertexInputBindingDescription[] bindingDescriptions = new VkVertexInputBindingDescription[1];

			bindingDescriptions[0].binding = 0;
			bindingDescriptions[0].stride = (uint)Marshal.SizeOf<VertexT>();
			bindingDescriptions[0].inputRate = VkVertexInputRate.VK_VERTEX_INPUT_RATE_VERTEX;

			return bindingDescriptions;
		}

		static public VkVertexInputAttributeDescription[] GetAttributeDescriptions()
		{
			VkVertexInputAttributeDescription[] attributeDescriptions = new VkVertexInputAttributeDescription[2];

			attributeDescriptions[0].binding = 0;
			attributeDescriptions[0].location = 0;
			attributeDescriptions[0].format = VkFormat.VK_FORMAT_R32G32_SFLOAT;
			attributeDescriptions[0].offset = (uint)Marshal.OffsetOf<VertexT>(nameof(Position));

			attributeDescriptions[1].binding = 0;
			attributeDescriptions[1].location = 1;
			attributeDescriptions[1].format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT;
			attributeDescriptions[1].offset = (uint)Marshal.OffsetOf<VertexT>(nameof(Color));
			return attributeDescriptions;
		}
	}

	public class ModelT
	{
		private DeviceT Device;

		private VkBuffer VertexBuffer;
		private VkDeviceMemory VertexBufferMemory;
		private uint VertexCount;
		public ModelT(ref DeviceT device, ref VertexT[] vertices)
		{
			Device = device;
			CreateVertexBuffers(ref vertices);
		}

		unsafe public void Bind(VkCommandBuffer commandBuffer) 
		{
			VkBuffer[] buffers = { VertexBuffer };

			ulong* offsets = (ulong*)Marshal.AllocHGlobal(Marshal.SizeOf<ulong>());
			offsets[0] = 0;

			fixed (VkBuffer* buffersL = &buffers[0])
			{
				VulkanNative.vkCmdBindVertexBuffers(commandBuffer, 0, 1, buffersL, offsets);
			}
		}
		unsafe public void Draw(VkCommandBuffer commandBuffer) 
		{
			VulkanNative.vkCmdDraw(commandBuffer, VertexCount, 1, 0, 0);
		}

		unsafe private void CreateVertexBuffers(ref VertexT[] vertices) 
		{
			VertexCount = (uint)vertices.Length;

			ulong bufferSize = (ulong)(Marshal.SizeOf<VertexT>() * VertexCount);

			Device.CreateBuffer(bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT, VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT, ref VertexBuffer, ref VertexBufferMemory);

			void* data;
			VulkanNative.vkMapMemory(Device.Device, VertexBufferMemory, 0, bufferSize, 0, &data);
			vertices.AsSpan().CopyTo(new Span<VertexT>(data, vertices.Length));
			VulkanNative.vkUnmapMemory(Device.Device, VertexBufferMemory);
		}

		unsafe public void DestroyModel() 
		{
			VulkanNative.vkDestroyBuffer(Device.Device, VertexBuffer, null);
			VulkanNative.vkFreeMemory(Device.Device, VertexBufferMemory, null);
		}
	}
}
