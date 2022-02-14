using System;
using Silk.NET.Vulkan;

namespace TestVulkan
{
	public struct AllocatedBufferVKG
	{
		public Silk.NET.Vulkan.Buffer _buffer;
		public VulkanMemoryItem2 _allocation;
	};

	public struct AllocatedImageVKG
	{
		public Image _image;
		public VulkanMemoryItem2 _allocation;
	};

}
