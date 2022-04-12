using Evergine.Bindings.Vulkan;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	public struct VertexT 
	{
		public Vector3 Position;
		public Vector3 Color;
		public Vector3 Normal;
		public Vector2 Uv;

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
			VkVertexInputAttributeDescription[] attributeDescriptions = new VkVertexInputAttributeDescription[4];

			attributeDescriptions[0].binding = 0;
			attributeDescriptions[0].location = 0;
			attributeDescriptions[0].format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT;
			attributeDescriptions[0].offset = (uint)Marshal.OffsetOf<VertexT>(nameof(Position));

			attributeDescriptions[1].binding = 0;
			attributeDescriptions[1].location = 1;
			attributeDescriptions[1].format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT;
			attributeDescriptions[1].offset = (uint)Marshal.OffsetOf<VertexT>(nameof(Color));

			attributeDescriptions[2].binding = 0;
			attributeDescriptions[2].location = 2;
			attributeDescriptions[2].format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT;
			attributeDescriptions[2].offset = (uint)Marshal.OffsetOf<VertexT>(nameof(Normal));

			attributeDescriptions[3].binding = 0;
			attributeDescriptions[3].location = 3;
			attributeDescriptions[3].format = VkFormat.VK_FORMAT_R32G32_SFLOAT;
			attributeDescriptions[3].offset = (uint)Marshal.OffsetOf<VertexT>(nameof(Uv));
			return attributeDescriptions;
		}
	}

	public struct BuilderT 
	{
		public VertexT[] Vertices = Array.Empty<VertexT>();
		public uint[] Indices = Array.Empty<uint>();

		public BuilderT() { }

		public void LoadModel(ref string filepath) 
		{
			List<VertexT> _vertices = new();
			List<uint> _indices = new();

			string[] lines = File.ReadAllLines(filepath);

			int offsetV = Array.FindIndex(lines, row => row.StartsWith("v ")) - 1;
			int offsetT = Array.FindIndex(lines, row => row.StartsWith("vt ")) - 1;
			int offsetN = Array.FindIndex(lines, row => row.StartsWith("vn ")) - 1;

			int offsetF = Array.FindIndex(lines, row => row.StartsWith("f "));
			int fCount = lines.Count(f => f.StartsWith("f "));

			Dictionary<VertexT, uint> vertexMapTrue = new();

			for (int i = 0; i < fCount; i++)
			{
				string[] line;
				line = lines[i + offsetF].Split(" ");

				while(!line[0].Contains("f"))
				{
					offsetF++;
					line = lines[i + offsetF].Split(" ");
				}

				foreach (string s in line)
				{
					if (s.StartsWith("f"))
						continue;

					string[] el = s.Split("/");
					int index = offsetV + int.Parse(el[0]);
					int indexT = offsetT + int.Parse(el[1]);
					int indexN = offsetN + int.Parse(el[2]);

					string[] vertexL = lines[index].Split(" ");
					string[] vertexTextL = lines[indexT].Split(" ");
					string[] vertexN = lines[indexN].Split(" ");

					VertexT vertex;

					vertex.Position = new Vector3()
					{
						X = float.Parse(vertexL[1], NumberStyles.Any, CultureInfo.InvariantCulture),
						Y = float.Parse(vertexL[2], NumberStyles.Any, CultureInfo.InvariantCulture),
						Z = float.Parse(vertexL[3], NumberStyles.Any, CultureInfo.InvariantCulture)
					};

					if (vertexL.Length > 4)
					{
						vertex.Color = new Vector3
						{
							X = float.Parse(vertexL[4], NumberStyles.Any, CultureInfo.InvariantCulture),
							Y = float.Parse(vertexL[5], NumberStyles.Any, CultureInfo.InvariantCulture),
							Z = float.Parse(vertexL[6], NumberStyles.Any, CultureInfo.InvariantCulture),
						};
					}
					else 
					{
						vertex.Color = new Vector3
						{
							X = 1.0f,
							Y = 1.0f,
							Z = 1.0f
						};
					}

					vertex.Uv = new Vector2
					{
						X = float.Parse(vertexTextL[1], NumberStyles.Any, CultureInfo.InvariantCulture),
						Y = 1.0f - float.Parse(vertexTextL[2], NumberStyles.Any, CultureInfo.InvariantCulture)
					};

					vertex.Normal = new Vector3
					{
						X = float.Parse(vertexN[1], NumberStyles.Any, CultureInfo.InvariantCulture),
						Y = float.Parse(vertexN[2], NumberStyles.Any, CultureInfo.InvariantCulture),
						Z = float.Parse(vertexN[3], NumberStyles.Any, CultureInfo.InvariantCulture),
					};
					
					if (vertexMapTrue.TryGetValue(vertex, out uint meshIndex))
					{
						_indices.Add(meshIndex);
					}
					else
					{
						_indices.Add((uint)_vertices.Count);
						vertexMapTrue[vertex] = (uint)_vertices.Count;
						_vertices.Add(vertex);
					}
					//_vertices.Add(vertex);
				}

				Vertices = _vertices.ToArray();
				Indices = _indices.ToArray();
			}
		}
	}

	public class ModelT
	{
		private DeviceT Device;

		private BufferT VertexBuffer;
		private uint VertexCount;

		private BufferT IndexBuffer;
		private uint IndexCount;

		private bool HasIndexBuffer = false;
		public ModelT(ref DeviceT device, ref BuilderT builder)
		{
			Device = device;
			CreateVertexBuffers(ref builder.Vertices);
			CreateIndexBuffers(ref builder.Indices);
		}

		unsafe public void Bind(VkCommandBuffer commandBuffer) 
		{
			VkBuffer[] buffers = { VertexBuffer.Buffer };

			ulong* offsets = (ulong*)Marshal.AllocHGlobal(Marshal.SizeOf<ulong>());
			offsets[0] = 0;

			fixed (VkBuffer* buffersL = &buffers[0])
			{
				VulkanNative.vkCmdBindVertexBuffers(commandBuffer, 0, 1, buffersL, offsets);
			}

			if (HasIndexBuffer) 
			{
				VulkanNative.vkCmdBindIndexBuffer(commandBuffer, IndexBuffer.Buffer, 0, VkIndexType.VK_INDEX_TYPE_UINT32);
			}
		}
		unsafe public void Draw(VkCommandBuffer commandBuffer) 
		{
			if (HasIndexBuffer) 
			{
				VulkanNative.vkCmdDrawIndexed(commandBuffer, IndexCount, 1,0,0,0);
			}else
				VulkanNative.vkCmdDraw(commandBuffer, VertexCount, 1, 0, 0);
		}

		static public ModelT CreateModelFromFile(ref DeviceT device, string filepath) 
		{
			BuilderT builder = new();

			builder.LoadModel(ref filepath);
			Trace.WriteLine("Vertex count: " + builder.Vertices.Length);
			return new ModelT(ref device,ref builder);
		}

		unsafe private void CreateVertexBuffers(ref VertexT[] vertices) 
		{
			VertexCount = (uint)vertices.Length;

			ulong bufferSize = (ulong)(Marshal.SizeOf<VertexT>() * VertexCount);

			uint vertexSize = (uint)Marshal.SizeOf<VertexT>();

			BufferT stagingBuffer = new(
				ref Device,
				vertexSize,
				VertexCount,
				VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT,
				VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT
				);

			stagingBuffer.Map();
			stagingBuffer.WriteToBufferV(ref vertices);

			VertexBuffer = new BufferT(
				ref Device,
				vertexSize,
				VertexCount,
				VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT,
				VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);

			Device.CopyBuffer(stagingBuffer.Buffer, VertexBuffer.Buffer, bufferSize);

			stagingBuffer.DestroyBuffer();
		}
		unsafe private void CreateIndexBuffers(ref uint[] indices)
		{
			IndexCount = (uint)indices.Length;
			if (IndexCount > 0)
				HasIndexBuffer = true;
			else
				return;

			ulong bufferSize = (ulong)(Marshal.SizeOf<uint>() * IndexCount);
			uint indexSize = (uint)Marshal.SizeOf<uint>();

			BufferT stagingBuffer = new(
				ref Device,
				indexSize,
				IndexCount,
				VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT,
				VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT
				);

			stagingBuffer.Map();
			stagingBuffer.WriteToBufferI(ref indices);

			IndexBuffer = new(
				ref Device,
				indexSize,
				IndexCount,
				VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT, 
				VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT
				);

			Device.CopyBuffer(stagingBuffer.Buffer, IndexBuffer.Buffer, bufferSize);

			stagingBuffer.DestroyBuffer();
		}
	}
}
