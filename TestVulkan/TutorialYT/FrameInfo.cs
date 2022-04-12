using Evergine.Bindings.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	[StructLayout(LayoutKind.Explicit)]
	public struct PointLight 
	{
		[FieldOffset(0)]
		public Vector4 Position;
		[FieldOffset(16)]
		public Vector4 Color;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct GlobalUboBase
	{
		[FieldOffset(0)]
		public Matrix4x4 Projection = Matrix4x4.Identity;
		[FieldOffset(64)]
		public Matrix4x4 View = Matrix4x4.Identity;
		[FieldOffset(128)]
		public Matrix4x4 InverseView = Matrix4x4.Identity;
		[FieldOffset(192)]
		public Vector4 AmbientColor = new(1.0f, 1.0f, 1.0f, 0.02f);
		[FieldOffset(208)]
		public int NumLights = 0;

		public GlobalUboBase() { }
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct GlobalUbo
	{
		[FieldOffset(0)]
		public Matrix4x4 Projection = Matrix4x4.Identity;
		[FieldOffset(64)]
		public Matrix4x4 View = Matrix4x4.Identity;
		[FieldOffset(128)]
		public Matrix4x4 InverseView = Matrix4x4.Identity;
		[FieldOffset(192)]
		public Vector4 AmbientColor = new(1.0f, 1.0f, 1.0f, 0.02f);
		[FieldOffset(208)]
		public PointLight[] PointLights = new PointLight[FirstAppT.MAX_LIGHTS];
		[FieldOffset(208 + (32 * 10) + 8)]
		public int NumLights = 0;

		public GlobalUbo() { }
	}

	public struct FrameInfo 
	{
		public int FrameIndex;
		public float FrameTime;
		public VkCommandBuffer CommandBuffer;
		public CameraT Camera;
		public VkDescriptorSet GlobalDescriptorSet;
		public GameObjectT GameObjects;
	}
}
