using Evergine.Bindings.Vulkan;
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace TestVulkan
{
	public class BufferT
	{
		private DeviceT Device;
		private ulong InstanceSize;
		private uint InstanceCount;
		private VkBufferUsageFlags UsageFlags;
		private VkMemoryPropertyFlags MemoryPropertyFlags;
		private ulong MinOffsetAlignment;

		private ulong AlignmentSize;
		public VkBuffer Buffer = VkBuffer.Null;
		private VkDeviceMemory Memory = VkDeviceMemory.Null;
		private ulong BufferSize;

		unsafe private void* Mapped = null;
		public BufferT(ref DeviceT device,
	ulong instanceSize,
	uint instanceCount,
	VkBufferUsageFlags usageFlags,
	VkMemoryPropertyFlags memoryPropertyFlags,
	ulong minOffsetAlignment = 1)
		{
			Device = device;
			InstanceSize = instanceSize;
			InstanceCount = instanceCount;
			UsageFlags = usageFlags;
			MemoryPropertyFlags = memoryPropertyFlags;
			MinOffsetAlignment = minOffsetAlignment;

			AlignmentSize = GetAlignment(instanceSize, minOffsetAlignment);
			BufferSize = AlignmentSize * instanceCount;
			device.CreateBuffer(BufferSize, usageFlags, memoryPropertyFlags,ref Buffer,ref Memory);
		}

		/**
 * Returns the minimum instance size required to be compatible with devices minOffsetAlignment
 *
 * @param instanceSize The size of an instance
 * @param minOffsetAlignment The minimum required alignment, in bytes, for the offset member (eg
 * minUniformBufferOffsetAlignment)
 *
 * @return VkResult of the buffer mapping call
 */
		public ulong GetAlignment(ulong instanceSize, ulong minOffsetAlignment)
		{
			if (minOffsetAlignment > 0)
			{
				return (instanceSize + minOffsetAlignment - 1) & ~(minOffsetAlignment - 1);
			}

			return instanceSize;
		}

		/**
 * Map a memory range of this buffer. If successful, mapped points to the specified buffer range.
 *
 * @param size (Optional) Size of the memory range to map. Pass VK_WHOLE_SIZE to map the complete
 * buffer range.
 * @param offset (Optional) Byte offset from beginning
 *
 * @return VkResult of the buffer mapping call
 */
		unsafe public VkResult Map(ulong size = VulkanNative.VK_WHOLE_SIZE, ulong offset = 0)
		{
			VkResult result;

			fixed (void* ptr = &Mapped) 
			{
				result = VulkanNative.vkMapMemory(Device.Device, Memory, offset, size, 0, (void**)ptr);
			}

			return result;
		}

		/**
 * Unmap a mapped memory range
 *
 * @note Does not return a result as vkUnmapMemory can't fail
 */
		unsafe public void Unmap()
		{
			if (Mapped != null)
			{
				VulkanNative.vkUnmapMemory(Device.Device, Memory);
				Mapped = null;
			}
		}

		/**
 * Copies the specified data to the mapped buffer. Default value writes whole buffer range
 *
 * @param data Pointer to the data to copy
 * @param size (Optional) Size of the data to copy. Pass VK_WHOLE_SIZE to flush the complete buffer
 * range.
 * @param offset (Optional) Byte offset from beginning of mapped region
 *
 */
		unsafe public void WriteToBuffer(void* data, ulong size = VulkanNative.VK_WHOLE_SIZE, ulong offset = 0)
		{
			if (size == VulkanNative.VK_WHOLE_SIZE)
			{
				//data.AsSpan().CopyTo(new Span<VertexT>(Mapped, data.Length));

				//memcpy(mapped, data, bufferSize);
			}
			else
			{
				char* memOffset = (char*)Mapped;
				memOffset += offset;
				//data.AsSpan().CopyTo(new Span<VertexT>(memOffset, data.Length));
				//memcpy(memOffset, data, size);
			}
		}

		unsafe public void WriteToBufferV(ref VertexT[] data, ulong size = VulkanNative.VK_WHOLE_SIZE, ulong offset = 0)
		{
			if (size == VulkanNative.VK_WHOLE_SIZE)
			{
				data.AsSpan().CopyTo(new Span<VertexT>(Mapped, data.Length));

				//memcpy(mapped, data, bufferSize);
			}
			else
			{
				char* memOffset = (char*)Mapped;
				memOffset += offset;
				data.AsSpan().CopyTo(new Span<VertexT>(memOffset, data.Length));
				//memcpy(memOffset, data, size);
			}
		}
		unsafe public void WriteToBufferI(ref uint[] data, ulong size = VulkanNative.VK_WHOLE_SIZE, ulong offset = 0)
		{
			if (size == VulkanNative.VK_WHOLE_SIZE)
			{
				data.AsSpan().CopyTo(new Span<uint>(Mapped, data.Length));

				//memcpy(mapped, data, bufferSize);
			}
			else
			{
				char* memOffset = (char*)Mapped;
				memOffset += offset;
				data.AsSpan().CopyTo(new Span<uint>(memOffset, data.Length));
				//memcpy(memOffset, data, size);
			}
		}

		unsafe public void WriteToBufferU(ref GlobalUbo data, ulong size = VulkanNative.VK_WHOLE_SIZE, ulong offset = 0)
		{
			if (size == VulkanNative.VK_WHOLE_SIZE)
			{
				Marshal.StructureToPtr(data, (IntPtr)Mapped, false);

				//memcpy(mapped, data, bufferSize);
			}
			else
			{
				//Marshal.StructureToPtr(data, (IntPtr)Mapped, false);
				IntPtr memOffset = (IntPtr)Mapped;
				var memOffset1 = memOffset.ToInt64();
				memOffset1 += (long)offset;
				Marshal.StructureToPtr(data, (IntPtr)memOffset1, false);
				//memcpy(memOffset, data, size);
			}
		}
		/**
 * Flush a memory range of the buffer to make it visible to the device
 *
 * @note Only required for non-coherent memory
 *
 * @param size (Optional) Size of the memory range to flush. Pass VK_WHOLE_SIZE to flush the
 * complete buffer range.
 * @param offset (Optional) Byte offset from beginning
 *
 * @return VkResult of the flush call
 */
		unsafe public VkResult Flush(ulong size = VulkanNative.VK_WHOLE_SIZE, ulong offset = 0)
		{
			VkMappedMemoryRange mappedRange = new();
			mappedRange.sType = VkStructureType.VK_STRUCTURE_TYPE_MAPPED_MEMORY_RANGE;
			mappedRange.memory = Memory;
			mappedRange.offset = offset;
			mappedRange.size = size;
			return VulkanNative.vkFlushMappedMemoryRanges(Device.Device, 1, &mappedRange);
		}

		/**
 * Invalidate a memory range of the buffer to make it visible to the host
 *
 * @note Only required for non-coherent memory
 *
 * @param size (Optional) Size of the memory range to invalidate. Pass VK_WHOLE_SIZE to invalidate
 * the complete buffer range.
 * @param offset (Optional) Byte offset from beginning
 *
 * @return VkResult of the invalidate call
 */
		unsafe public VkResult Invalidate(ulong size, ulong offset)
		{
			VkMappedMemoryRange mappedRange = new();
			mappedRange.sType = VkStructureType.VK_STRUCTURE_TYPE_MAPPED_MEMORY_RANGE;
			mappedRange.memory = Memory;
			mappedRange.offset = offset;
			mappedRange.size = size;
			return VulkanNative.vkInvalidateMappedMemoryRanges(Device.Device, 1, &mappedRange);
		}

		/**
 * Create a buffer info descriptor
 *
 * @param size (Optional) Size of the memory range of the descriptor
 * @param offset (Optional) Byte offset from beginning
 *
 * @return VkDescriptorBufferInfo of specified offset and range
 */
		unsafe public VkDescriptorBufferInfo DescriptorInfo(ulong size, ulong offset)
		{
			return new VkDescriptorBufferInfo()
			{
				buffer = Buffer,
				offset = offset,
				range = size,
			};
		}

		/**
 * Copies "instanceSize" bytes of data to the mapped buffer at an offset of index * alignmentSize
 *
 * @param data Pointer to the data to copy
 * @param index Used in offset calculation
 *
 */
		unsafe public void WriteToIndex(void* data, int index)
		{
			WriteToBuffer(data, InstanceSize, (ulong)index * AlignmentSize);
		}
		unsafe public void WriteToIndexU(ref GlobalUbo data, int index)
		{
			WriteToBufferU(ref data, InstanceSize, (ulong)index * AlignmentSize);
			/*
			if (InstanceSize == VulkanNative.VK_WHOLE_SIZE)
			{
				Marshal.StructureToPtr(data, (IntPtr)Mapped, false);

				//memcpy(mapped, data, bufferSize);
			}
			else
			{
				//Marshal.StructureToPtr(data, (IntPtr)Mapped, false);
				IntPtr memOffset = (IntPtr)Mapped;
				var memOffset1 = memOffset.ToInt64();
				memOffset1 += (long)index * (long)AlignmentSize;
				Marshal.StructureToPtr(data, (IntPtr)memOffset1, false);
				//memcpy(memOffset, data, size);
			}*/
		}
		/**
 *  Flush the memory range at index * alignmentSize of the buffer to make it visible to the device
 *
 * @param index Used in offset calculation
 *
 */
		unsafe public VkResult FlushIndex(int index) 
		{ 
			return Flush(AlignmentSize, (ulong)index * AlignmentSize);
		}

		/**
 * Create a buffer info descriptor
 *
 * @param index Specifies the region given by index * alignmentSize
 *
 * @return VkDescriptorBufferInfo for instance at index
 */
		unsafe public VkDescriptorBufferInfo DescriptorInfoForIndex(int index)
		{
			return DescriptorInfo(AlignmentSize, (ulong)index * AlignmentSize);
		}

		/**
 * Invalidate a memory range of the buffer to make it visible to the host
 *
 * @note Only required for non-coherent memory
 *
 * @param index Specifies the region to invalidate: index * alignmentSize
 *
 * @return VkResult of the invalidate call
 */
		unsafe public VkResult InvalidateIndex(int index)
		{
			return Invalidate(AlignmentSize, (ulong)index * AlignmentSize);
		}

		unsafe public void DestroyBuffer()
		{
			Unmap();
			VulkanNative.vkDestroyBuffer(Device.Device, Buffer, null);
			VulkanNative.vkFreeMemory(Device.Device, Memory, null);
		}
	}
}
