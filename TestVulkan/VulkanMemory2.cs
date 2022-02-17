using Silk.NET.Vulkan;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TestVulkan
{
	unsafe public class VulkanMemory2
	{
		//https://gpuopen.com/learn/vulkan-device-memory/
		//
		//

		//Best Practices layer suggest threshold
		private const ulong MinSizeOneAllocation = 262144;

		private ulong DefaultSizeAllocationsDLBDivide = 16;

		private ulong DefaultSizeAllocationsDivide = 4;

		private ulong DefaultSizeOneAllocationsDLB = MinSizeOneAllocation;

		private ulong DefaultSizeOneAllocations = MinSizeOneAllocation;

		private ulong AdjacentOffset = 1;

		private ConcurrentDictionary<uint, ConcurrentDictionary<int,VulkanMemoryChunk2>> MemoryIndices = new();

		private int IdChunk = 0;

		private readonly PhysicalDeviceMemoryProperties MemoryProperties;

		public VulkanMemory2(PhysicalDeviceProperties deviceProperties, PhysicalDeviceMemoryProperties physicalDeviceMemoryProperties)
		{
			Trace.WriteLine("Vulkan Memory 2 initialization");

			MemoryProperties = physicalDeviceMemoryProperties;

			DefaultSizeOneAllocationsDLB = physicalDeviceMemoryProperties.MemoryHeaps[0].Size / DefaultSizeAllocationsDLBDivide;

			DefaultSizeOneAllocations = DefaultSizeOneAllocationsDLB / DefaultSizeAllocationsDivide;

			if (physicalDeviceMemoryProperties.MemoryHeapCount >= 3) 
			{
				//TODO!! TEST!!!!!!!!!!!!!!
				//
				//
				//
				if (DefaultSizeOneAllocations > physicalDeviceMemoryProperties.MemoryHeaps[2].Size) 
					DefaultSizeOneAllocations = physicalDeviceMemoryProperties.MemoryHeaps[2].Size;

				ulong _allocCount = physicalDeviceMemoryProperties.MemoryHeaps[2].Size / DefaultSizeOneAllocations;

				if(_allocCount != 1 && _allocCount < 2)
					DefaultSizeOneAllocations = physicalDeviceMemoryProperties.MemoryHeaps[2].Size / 2;

			}

			if (DefaultSizeOneAllocations < MinSizeOneAllocation)
				DefaultSizeOneAllocations = MinSizeOneAllocation;

			if (DefaultSizeOneAllocationsDLB < MinSizeOneAllocation)
				DefaultSizeOneAllocationsDLB = MinSizeOneAllocation;

			//TODO! MaxSizeOneAllocation

			AdjacentOffset = (ulong)MathF.Max(deviceProperties.Limits.BufferImageGranularity, deviceProperties.Limits.MinUniformBufferOffsetAlignment);
		}

		unsafe public VulkanMemoryItem2 BindImageOrBuffer(ref Vk vk, ref Device device, dynamic imageOrBuffer, MemoryPropertyFlags properties) 
		{
			ulong _sizeOfAllocation = MinSizeOneAllocation;

			if (properties.HasFlag(MemoryPropertyFlags.MemoryPropertyDeviceLocalBit))
				_sizeOfAllocation = DefaultSizeOneAllocationsDLB;
			else
				_sizeOfAllocation = DefaultSizeOneAllocations;

			MemoryRequirements memRequirements;
			if (imageOrBuffer is Image)
				vk.GetImageMemoryRequirements(device, (Image)imageOrBuffer, &memRequirements);
			else 
				vk.GetBufferMemoryRequirements(device, (Silk.NET.Vulkan.Buffer)imageOrBuffer, &memRequirements);

			VulkanMemoryItem2 _item = MakeItem(memRequirements);

			uint _index = FindMemoryType(memRequirements.MemoryTypeBits, properties);

			if (!MemoryIndices.ContainsKey(_index))
				MemoryIndices.TryAdd(_index, new ConcurrentDictionary<int, VulkanMemoryChunk2>());

			ConcurrentDictionary<int, VulkanMemoryChunk2> _list = MemoryIndices[_index];

			foreach (KeyValuePair<int, VulkanMemoryChunk2> entry in _list)
			{
				if (entry.Value.FreeSpace > _item.SizeWithAdjacentOffset)
				{
					ulong _size = _item.SizeWithAdjacentOffset;
					if (entry.Value.Alignment < memRequirements.Alignment)
					{
						entry.Value.Alignment = memRequirements.Alignment;
					}

					ulong _divide;
					if (_item.SizeWithAdjacentOffset > entry.Value.Alignment)
					{
						_divide = _item.SizeWithAdjacentOffset / entry.Value.Alignment;
						_size = (ulong)(entry.Value.Alignment * MathF.Ceiling(_divide));
					}
					else
					{
						_size = entry.Value.Alignment;
					}

					if (entry.Value.FreeSpace < _size)
						continue;

					ulong _sizeAll = 0;

					foreach (VulkanMemoryItem2 _item2 in entry.Value.VulkanMemoryItems)
					{
						//_item2.StartOffset = _sizeAll;
						if (_item2.SizeWithAdjacentOffset > entry.Value.Alignment)
						{
							_divide = _item2.SizeWithAdjacentOffset / entry.Value.Alignment;
							_sizeAll += (ulong)(entry.Value.Alignment * MathF.Ceiling(_divide));
						}
						else
						{
							_sizeAll += entry.Value.Alignment;
						}
					}

					entry.Value.FreeSpace -= _size;

					_item.IdChunk = entry.Value.IdChunk;
					_item.StartOffset = _sizeAll;
					_item.EndOffset = _sizeAll + _size;
					entry.Value.VulkanMemoryItems.Add(_item);

					if (imageOrBuffer is Image)
						vk.BindImageMemory(device, (Image)imageOrBuffer, entry.Value.DeviceMemory, _sizeAll);
					else
						vk.BindBufferMemory(device, (Silk.NET.Vulkan.Buffer)imageOrBuffer, entry.Value.DeviceMemory, _sizeAll);
					
					return _item;
				}
			}

			VulkanMemoryChunk2 _chunck = MakeChank(ref vk, ref device, _list, _index, _sizeOfAllocation, memRequirements);

			_item.IdChunk = _chunck.IdChunk;
			_item.StartOffset = 0;
			_item.EndOffset = _item.SizeWithAdjacentOffset;

			_chunck.VulkanMemoryItems.Add(_item);

			if (imageOrBuffer is Image)
				vk.BindImageMemory(device, (Image)imageOrBuffer, _chunck.DeviceMemory, 0);
			else
				vk.BindBufferMemory(device, (Silk.NET.Vulkan.Buffer)imageOrBuffer, _chunck.DeviceMemory, 0);

			return _item;
		}

		private VulkanMemoryItem2 MakeItem(MemoryRequirements memRequirements)
		{
			VulkanMemoryItem2 _vmi = new();

			_vmi.IsFreed = false;
			_vmi.MemoryRequirements = memRequirements;
			_vmi.SizeWithAdjacentOffset = (memRequirements.Size + AdjacentOffset - 1) & ~(AdjacentOffset - 1);
			_vmi.SizeWithAdjacentOffset += AdjacentOffset;

			return _vmi;
		}

		unsafe private VulkanMemoryChunk2 MakeChank(ref Vk vk, ref Device device, ConcurrentDictionary<int, VulkanMemoryChunk2> _list, uint _index, ulong _sizeOfAllocation, MemoryRequirements memRequirements)
		{
			VulkanMemoryChunk2 _vmc = new();
			DeviceMemory _deviceMemory;

			_vmc.IdChunk = IdChunk;

			IdChunk += 1;

			_vmc.Alignment = memRequirements.Alignment;

			MemoryAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.MemoryAllocateInfo;

			if (_sizeOfAllocation >= memRequirements.Size)
			{
				allocInfo.AllocationSize = _sizeOfAllocation;

				_vmc.Size = _sizeOfAllocation;
				_vmc.FreeSpace = _sizeOfAllocation - memRequirements.Size;
			}
			else
			{
				allocInfo.AllocationSize = memRequirements.Size;

				_vmc.Size = memRequirements.Size;
				_vmc.FreeSpace = 0;
			}

			allocInfo.MemoryTypeIndex = _index;

			if (vk.AllocateMemory(device, &allocInfo, null, &_deviceMemory) != Result.Success)
			{
				Trace.TraceError("failed to allocate image memory!");
				Console.ReadKey();
				return _vmc;
			}

			_vmc.DeviceMemory = _deviceMemory;

			_list.TryAdd(_vmc.IdChunk, _vmc);

			return _vmc;
		}

		unsafe private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
		{
			for (int i = 0; i < MemoryProperties.MemoryTypeCount; ++i)
			{
				uint v = (uint)(typeFilter & (1 << i));
				MemoryPropertyFlags v1 = MemoryProperties.MemoryTypes[i].PropertyFlags & properties;
				if (v != 0 && v1 != 0)
				{
					return (uint)i;
				}
			}

			throw new Exception("failed to find suitable memory type!");
		}

		public VulkanMemoryChunk2 ReturnChunk(VulkanMemoryItem2 item) 
		{
			foreach (KeyValuePair<uint, ConcurrentDictionary<int, VulkanMemoryChunk2>> entry in MemoryIndices)
			{
				foreach (KeyValuePair<int, VulkanMemoryChunk2> entry2 in entry.Value)
				{
					if(entry2.Value.IdChunk == item.IdChunk)
						return entry2.Value;
				}
			}

			return null;
		}

		unsafe public void FreeOne(ref Vk vk, ref Device device, VulkanMemoryChunk2 chunk, VulkanMemoryItem2 item)
		{
			//TODO IF CHUNK IS NULL!
			item.IsFreed = true;

			foreach (VulkanMemoryItem2 mItem in chunk.VulkanMemoryItems)
			{
				if (mItem.IsFreed == false)
					return;
			}

			vk.FreeMemory(device, chunk.DeviceMemory, null);
			//
			//
			//delete chunk
			chunk.FreeSpace = 0;
			chunk.IsFreed = true;
			foreach (KeyValuePair<uint, ConcurrentDictionary<int, VulkanMemoryChunk2>> entry in MemoryIndices)
			{
				if (entry.Value.TryRemove(chunk.IdChunk, out _))
				{
					Console.WriteLine("Free chunk!!!");
					return;
				}
				else
					continue;
			}
		}
		
		unsafe public void FreeAll(ref Vk vk, ref Device device)
		{
			foreach (KeyValuePair<uint, ConcurrentDictionary<int,VulkanMemoryChunk2>> entry in MemoryIndices)
			{
				foreach (KeyValuePair<int, VulkanMemoryChunk2> entry2 in entry.Value)
				{
					if (entry2.Value.IsFreed)
						continue;

					vk.FreeMemory(device, entry2.Value.DeviceMemory, null);
				}
			}

			MemoryIndices.Clear();
		}
	}

	public class VulkanMemoryChunk2
	{
		public int IdChunk { get; set; } = 0;

		public DeviceMemory DeviceMemory { get; set; } = new();

		public ulong Alignment { get; set; } = new();

		public ulong Size { get; set; } = 0;

		public ulong FreeSpace { get; set; } = 0;

		public bool IsFreed { get; set; } = false;

		public ConcurrentBag<VulkanMemoryItem2> VulkanMemoryItems { get; set; } = new();
	}

	public class VulkanMemoryItem2
	{
		public int IdChunk { get; set; } = 0;

		public MemoryRequirements MemoryRequirements { get; set; } = new();

		public ulong SizeWithAdjacentOffset { get; set; } = 0;

		public ulong StartOffset { get; set; } = 0;

		public ulong EndOffset { get; set; } = 0;

		public bool IsFreed { get; set; } = false;
	}
}
