using Silk.NET.Vulkan;
using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TestVulkan
{
	/*
	public class MeshVKG
	{
		public MeshVKG()
		{
		}
	}*/
	public struct VertexInputDescriptionVKG
	{
		public VertexInputBindingDescription[] bindings;
		public VertexInputAttributeDescription[] attributes;

		public uint flags = 0;
	};

	public struct VertexVKG
	{
		public Vector3 position;
		public Vector3 normal;
		public Vector3 color;
		public Vector2 uv;

		public static VertexInputDescriptionVKG GetVertexSescription() 
		{
			VertexInputDescriptionVKG description = new();

			//we will have just 1 vertex buffer binding, with a per-vertex rate
			VertexInputBindingDescription mainBinding = new();
			mainBinding.Binding = 0;
			mainBinding.Stride = (uint)Marshal.SizeOf<VertexVKG>();
			mainBinding.InputRate = VertexInputRate.Vertex;

			description.bindings = new VertexInputBindingDescription[1] { mainBinding };

			//Position will be stored at Location 0
			VertexInputAttributeDescription positionAttribute = new();
			positionAttribute.Binding = 0;
			positionAttribute.Location = 0;
			positionAttribute.Format = Format.R32G32B32Sfloat;
			positionAttribute.Offset = (uint)Marshal.OffsetOf<VertexVKG>(nameof(position));

			//Normal will be stored at Location 1
			VertexInputAttributeDescription normalAttribute = new();
			normalAttribute.Binding = 0;
			normalAttribute.Location = 1;
			normalAttribute.Format = Format.R32G32B32Sfloat;
			normalAttribute.Offset = (uint)Marshal.OffsetOf<VertexVKG>(nameof(normal));

			//Color will be stored at Location 2
			VertexInputAttributeDescription colorAttribute = new();
			colorAttribute.Binding = 0;
			colorAttribute.Location = 2;
			colorAttribute.Format = Format.R32G32B32Sfloat;
			colorAttribute.Offset = (uint)Marshal.OffsetOf<VertexVKG>(nameof(color));

			//UV will be stored at Location 3
			VertexInputAttributeDescription uvAttribute = new();
			uvAttribute.Binding = 0;
			uvAttribute.Location = 3;
			uvAttribute.Format = Format.R32G32Sfloat;
			uvAttribute.Offset = (uint)Marshal.OffsetOf<VertexVKG>(nameof(uv));

			description.attributes = new VertexInputAttributeDescription[] 
			{ 
				positionAttribute, 
				normalAttribute, 
				colorAttribute,
				uvAttribute
			};

			return description;
		}
	};

	public struct MeshVKG
	{
		public VertexVKG[] _vertices;

		public AllocatedBufferVKG _vertexBuffer;
		public bool LoadFromObj(string filename) 
		{
			List<VertexVKG> _verticesl = new();
			List<uint> _indices = new();

			string[] lines = File.ReadAllLines(filename);

			int offsetV = Array.FindIndex(lines, row => row.StartsWith("v ")) - 1;
			int offsetT = Array.FindIndex(lines, row => row.StartsWith("vt ")) - 1;
			int offsetN = Array.FindIndex(lines, row => row.StartsWith("vn ")) - 1;

			int offsetF = Array.FindIndex(lines, row => row.StartsWith("f "));
			int fCount = lines.Count(f => f.StartsWith("f "));

			Dictionary<VertexVKG, uint> vertexMapTrue = new();

			for (int i = 0; i < fCount; i++)
			{
				string[] line;
				line = lines[i + offsetF].Split(" ");

				while (!line[0].Contains("f"))
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

					while (!vertexTextL[0].Contains("vt"))
					{
						indexT++;
						vertexTextL = lines[indexT].Split(" ");
					}

					VertexVKG vertex;

					vertex.position = new Vector3()
					{
						X = float.Parse(vertexL[1], NumberStyles.Any, CultureInfo.InvariantCulture),
						Y = float.Parse(vertexL[2], NumberStyles.Any, CultureInfo.InvariantCulture),
						Z = float.Parse(vertexL[3], NumberStyles.Any, CultureInfo.InvariantCulture)
					};

					/*
					if (vertexL.Length > 4)
					{
						vertex.color = new Vector3
						{
							X = float.Parse(vertexL[4], NumberStyles.Any, CultureInfo.InvariantCulture),
							Y = float.Parse(vertexL[5], NumberStyles.Any, CultureInfo.InvariantCulture),
							Z = float.Parse(vertexL[6], NumberStyles.Any, CultureInfo.InvariantCulture),
						};
					}
					else
					{
						vertex.color = new Vector3
						{
							X = 1.0f,
							Y = 1.0f,
							Z = 1.0f
						};
					}*/

					vertex.normal = new Vector3
					{
						X = float.Parse(vertexN[1], NumberStyles.Any, CultureInfo.InvariantCulture),
						Y = float.Parse(vertexN[2], NumberStyles.Any, CultureInfo.InvariantCulture),
						Z = float.Parse(vertexN[3], NumberStyles.Any, CultureInfo.InvariantCulture),
					};

					vertex.color = vertex.normal;

					vertex.uv = new Vector2
					{
						X = float.Parse(vertexTextL[1], NumberStyles.Any, CultureInfo.InvariantCulture),
						Y = 1.0f - float.Parse(vertexTextL[2], NumberStyles.Any, CultureInfo.InvariantCulture)
					};

					/*
					if (vertexMapTrue.TryGetValue(vertex, out uint meshIndex))
					{
						_indices.Add(meshIndex);
					}
					else
					{
						_indices.Add((uint)_vertices.Count);
						vertexMapTrue[vertex] = (uint)_vertices.Count;
						_vertices.Add(vertex);
					}*/
					_verticesl.Add(vertex);
				}

				_vertices = _verticesl.ToArray();
				//Indices = _indices.ToArray();
			}

			return false;
		}
	};
}
