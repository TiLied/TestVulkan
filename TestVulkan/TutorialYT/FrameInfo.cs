using Evergine.Bindings.Vulkan;
using System;

namespace TestVulkan
{
	public struct FrameInfo 
	{
		public int FrameIndex;
		public float FrameTime;
		public VkCommandBuffer CommandBuffer;
		public CameraT Camera;
	}
	public class FrameInfoT
	{
		public FrameInfoT()
		{
		}
	}
}
