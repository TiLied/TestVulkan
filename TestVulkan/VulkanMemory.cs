using Silk.NET.Vulkan;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TestVulkan
{
	unsafe public class VulkanMemory
	{
		//Best Practices layer suggest threshold
		private const ulong MinSizeOneAllocation = 262144;

		//Default size for allocation
		private ulong DefaultSizeOneAllocation = MinSizeOneAllocation;

		//Read somewhere 4gb is a max
		private const ulong MaxSizeOneAllocation = 4294967296;

		//Depends on hardware
		private ulong MaxSizeAllocations = 2048;

		//Default adjacent offset(item + adjacent offset in memory)
		private ulong AdjacentOffset = 1;

		private ConcurrentDictionary<uint, ConcurrentBag<VulkanMemoryChunk>> MemoryIndices = new();

		private PhysicalDeviceMemoryProperties MemoryProperties;

		public VulkanMemory(PhysicalDeviceProperties deviceProperties, PhysicalDeviceMemoryProperties physicalDeviceMemoryProperties)
		{
			ulong _types = physicalDeviceMemoryProperties.MemoryTypeCount;

			if (_types % 2 != 0)
				_types++;

			MaxSizeAllocations = deviceProperties.Limits.MaxMemoryAllocationCount;

			if (MaxSizeAllocations > 10240)
				MaxSizeAllocations = 10240;

			AdjacentOffset = deviceProperties.Limits.BufferImageGranularity;

			ulong _size = 0;

			for (int i = 0; i < physicalDeviceMemoryProperties.MemoryHeapCount; i++)
			{
				_size += physicalDeviceMemoryProperties.MemoryHeaps[i].Size;
			}

			ulong _sizeOneAllocation = _size / (MaxSizeAllocations / _types);

			DefaultSizeOneAllocation = (ulong)Math.Ceiling((double)_sizeOneAllocation);

			if (DefaultSizeOneAllocation < MinSizeOneAllocation)
				DefaultSizeOneAllocation = MinSizeOneAllocation;

			if (DefaultSizeOneAllocation > MaxSizeOneAllocation)
				DefaultSizeOneAllocation = MaxSizeOneAllocation;

			MemoryProperties = physicalDeviceMemoryProperties;
		}

		unsafe public (VulkanMemoryChunk, VulkanMemoryItem) BindImage(ref Vk vk, ref Device device, ref Image image, MemoryPropertyFlags properties)
		{
			MemoryRequirements memRequirements;
			vk.GetImageMemoryRequirements(device, image, &memRequirements);

			uint _index = FindMemoryType(memRequirements.MemoryTypeBits, properties);

			double asd2;
			ulong _size;
			if (memRequirements.Size > AdjacentOffset)
			{
				asd2 = memRequirements.Size / AdjacentOffset;
				_size = (ulong)(AdjacentOffset * Math.Ceiling(asd2)) + AdjacentOffset;
			}
			else
			{
				_size = AdjacentOffset + AdjacentOffset;
			}

			if (!MemoryIndices.ContainsKey(_index)) 
				MemoryIndices.TryAdd(_index, new ConcurrentBag<VulkanMemoryChunk>());

			ConcurrentBag<VulkanMemoryChunk> _list = MemoryIndices[_index];

			foreach (VulkanMemoryChunk chunk in _list)
			{
				if (chunk.FreeSpace >= _size)
				{
					VulkanMemoryItem _item2 = MakeItem(chunk, _size, memRequirements);

					if (memRequirements.Alignment > chunk.Alignment)
						chunk.Alignment = memRequirements.Alignment;

					vk.BindImageMemory(device, image, chunk.DeviceMemory, chunk.SumOffset);

					chunk.FreeSpace -= _size;
					chunk.SumOffset += _size;

					return (chunk, _item2);
				}
			}

			VulkanMemoryChunk _chunck = MakeChank(ref vk, ref device, MemoryIndices[_index], _size, _index, memRequirements);

			VulkanMemoryItem _item = MakeItem(_chunck, _size, memRequirements);

			vk.BindImageMemory(device, image, _chunck.DeviceMemory, 0);

			return (_chunck, _item);
		}

		unsafe public (VulkanMemoryChunk, VulkanMemoryItem) BindBuffer(ref Vk vk, ref Device device, ref Silk.NET.Vulkan.Buffer buffer, MemoryPropertyFlags properties)
		{
			MemoryRequirements memRequirements;
			vk.GetBufferMemoryRequirements(device, buffer, &memRequirements);

			uint _index = FindMemoryType(memRequirements.MemoryTypeBits, properties);
			double asd2;
			ulong _size;
			if (memRequirements.Size > AdjacentOffset)
			{
				asd2 = memRequirements.Size / AdjacentOffset;
				_size = (ulong)(AdjacentOffset * Math.Ceiling(asd2)) + AdjacentOffset;
			}
			else
			{
				_size = AdjacentOffset + AdjacentOffset;
			}

			//ulong _size = (ulong)(AdjacentOffset * Math.Ceiling(asd2));

			if (!MemoryIndices.ContainsKey(_index))
				MemoryIndices.TryAdd(_index, new ConcurrentBag<VulkanMemoryChunk>());

			ConcurrentBag<VulkanMemoryChunk> _list = MemoryIndices[_index];

			foreach (VulkanMemoryChunk chunk in _list)
			{
				if (chunk.FreeSpace >= _size)
				{
					VulkanMemoryItem _item2 = MakeItem(chunk, _size, memRequirements);

					if(memRequirements.Alignment > chunk.Alignment)
						chunk.Alignment = memRequirements.Alignment;

					vk.BindBufferMemory(device, buffer, chunk.DeviceMemory, chunk.SumOffset);

					chunk.FreeSpace -= _size;
					chunk.SumOffset += _size;

					return (chunk, _item2);
				}
			}

			VulkanMemoryChunk _chunck = MakeChank(ref vk, ref device, MemoryIndices[_index], _size, _index, memRequirements);

			VulkanMemoryItem _item = MakeItem(_chunck, _size, memRequirements);

			vk.BindBufferMemory(device, buffer, _chunck.DeviceMemory, 0);

			return (_chunck, _item);
		}

		private VulkanMemoryItem MakeItem(VulkanMemoryChunk _vmc, ulong _size, 	MemoryRequirements memRequirements)
		{
			VulkanMemoryItem _vmi = new();

			_vmi.IsFreed = false;
			_vmi.MemoryRequirements = memRequirements;

			if (_vmc.VulkanMemoryItems.Any())
			{
				VulkanMemoryItem _last = _vmc.VulkanMemoryItems.First();
				_vmi.StartOffset = _last.EndOffset;
				_vmi.EndOffset = _last.EndOffset + _size;
			}
			else
			{
				_vmi.StartOffset = 0;
				_vmi.EndOffset = _size;
			}

			_vmc.VulkanMemoryItems.Add(_vmi);

			return _vmi;
		}

		unsafe private VulkanMemoryChunk MakeChank(ref Vk vk, ref Device device, ConcurrentBag<VulkanMemoryChunk> _list, ulong _size, uint _index, MemoryRequirements memRequirements)
		{
			VulkanMemoryChunk _vmc = new();

			DeviceMemory _deviceMemory;

			MemoryAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.MemoryAllocateInfo;

			if (DefaultSizeOneAllocation >= _size)
			{
				allocInfo.AllocationSize = DefaultSizeOneAllocation;

				_vmc.Size = DefaultSizeOneAllocation;
				_vmc.FreeSpace = DefaultSizeOneAllocation - _size;
			}
			else
			{
				allocInfo.AllocationSize = _size;

				_vmc.Size = _size;
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
			_vmc.SumOffset += _size;

			_vmc.Alignment = memRequirements.Alignment;

			_list.Add(_vmc);

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

		unsafe public void FreeOne(ref Vk vk, ref Device device, VulkanMemoryChunk chunk, VulkanMemoryItem item)
		{
			item.IsFreed = true;

			foreach (VulkanMemoryItem mItem in chunk.VulkanMemoryItems)
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
		}

		unsafe public void FreeAll(ref Vk vk, ref Device device)
		{
			foreach (KeyValuePair<uint, ConcurrentBag<VulkanMemoryChunk>> entry in MemoryIndices)
			{
				foreach (VulkanMemoryChunk vmc in entry.Value)
				{
					if (vmc.IsFreed)
						continue;

					vk.FreeMemory(device, vmc.DeviceMemory, null);
				}
			}

			MemoryIndices.Clear();
		}
	}
	unsafe public class VulkanMemoryChunk
	{
		public ulong Alignment { get; set; } = new();
		public bool IsFreed { get; set; } = false;

		public DeviceMemory DeviceMemory { get; set; }

		public ulong Size { get; set; }

		public ulong FreeSpace { get; set; }

		public ulong SumOffset { get; set; } = 0;

		public ConcurrentBag<VulkanMemoryItem> VulkanMemoryItems { get; set; } = new();
	}

	public class VulkanMemoryItem
	{
		public MemoryRequirements MemoryRequirements;
		public ulong StartOffset { get; set; } = 0;

		public ulong EndOffset { get; set; } = 0;

		public bool IsFreed { get; set; } = false;
	}
}
