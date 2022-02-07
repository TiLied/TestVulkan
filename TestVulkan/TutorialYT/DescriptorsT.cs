using Evergine.Bindings.Vulkan;
using System;
namespace TestVulkan
{
	public class DescriptorSetLayoutT
	{
        public class Builder
        {
            private DeviceT Device;
            private Dictionary<uint, VkDescriptorSetLayoutBinding> bindings = new();

            public Builder(ref DeviceT device)
            {
                Device = device;
            }

            public Builder AddBinding(uint binding,
                VkDescriptorType descriptorType,
                VkShaderStageFlags stageFlags,
                uint count = 1)
            {

                VkDescriptorSetLayoutBinding layoutBinding = new();
                layoutBinding.binding = binding;
                layoutBinding.descriptorType = descriptorType;
                layoutBinding.descriptorCount = count;
                layoutBinding.stageFlags = stageFlags;
                bindings[binding] = layoutBinding;
                return this;
            }

            public DescriptorSetLayoutT Build()
            {
                return new DescriptorSetLayoutT(ref Device, bindings);
            }
        }

        private DeviceT Device;
        public Dictionary<uint, VkDescriptorSetLayoutBinding> Bindings;
        private VkDescriptorSetLayout DescriptorSetLayoutF;
        public VkDescriptorSetLayout GetDescriptorSetLayout => DescriptorSetLayoutF;
        unsafe public DescriptorSetLayoutT(ref DeviceT device, Dictionary<uint, VkDescriptorSetLayoutBinding> bindings)
		{
			Device = device;
			Bindings = bindings;

            List<VkDescriptorSetLayoutBinding> setLayoutBindings = new();
			foreach (KeyValuePair<uint, VkDescriptorSetLayoutBinding> item in Bindings)
			{
                setLayoutBindings.Add(item.Value);
            }

            VkDescriptorSetLayoutCreateInfo descriptorSetLayoutInfo = new();
            descriptorSetLayoutInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO;
            descriptorSetLayoutInfo.bindingCount = (uint)setLayoutBindings.Count;
            fixed (VkDescriptorSetLayoutBinding* setLayoutBindingsPtr = &setLayoutBindings.ToArray()[0])
            {
                descriptorSetLayoutInfo.pBindings = setLayoutBindingsPtr;
            }

            fixed (VkDescriptorSetLayout* descriptorSetLayout = &DescriptorSetLayoutF)
            {
                if (VulkanNative.vkCreateDescriptorSetLayout(
                    Device.Device,
                    &descriptorSetLayoutInfo,
                    null,
                    descriptorSetLayout) != VkResult.VK_SUCCESS)
                {
                    throw new Exception("failed to create descriptor set layout!");
                }
            }
        }

        unsafe public void DestroyDescriptorSetLayout() 
        {
            VulkanNative.vkDestroyDescriptorSetLayout(Device.Device, DescriptorSetLayoutF, null);
        }

    }

    public class DescriptorPoolT
    {
        public class Builder
        {
            private DeviceT Device;
            private List<VkDescriptorPoolSize> PoolSizes = new();
            private uint MaxSets = 1000;
            private VkDescriptorPoolCreateFlags PoolFlags = 0;

            public Builder(ref DeviceT device)
            {
                Device = device;
            }

            public Builder AddPoolSize(VkDescriptorType descriptorType, uint count) 
            {
                PoolSizes.Add( new VkDescriptorPoolSize() { descriptorCount= count, type= descriptorType  });
                return this;
            }
            public Builder SetPoolFlags(VkDescriptorPoolCreateFlags flags) 
            {
                PoolFlags = flags;
                return this;
            }
            public Builder SetMaxSets(uint count) 
            {
                MaxSets = count;
                return this;
            }
            public DescriptorPoolT Build() 
            {
                return new DescriptorPoolT(ref Device, MaxSets, PoolFlags, ref PoolSizes);
            }

        };

        public DeviceT Device;
        private  VkDescriptorPool DescriptorPool;
        unsafe public DescriptorPoolT(ref DeviceT device,
            uint maxSets,
            VkDescriptorPoolCreateFlags poolFlags,
            ref List<VkDescriptorPoolSize> poolSizes)
        {
            Device = device;

            VkDescriptorPoolCreateInfo descriptorPoolInfo = new();
            descriptorPoolInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO;
            descriptorPoolInfo.poolSizeCount = (uint)poolSizes.Count;
            fixed (VkDescriptorPoolSize* poolSizesPtr = &poolSizes.ToArray()[0])
            {
                descriptorPoolInfo.pPoolSizes = poolSizesPtr;
            }
            descriptorPoolInfo.maxSets = maxSets;
            descriptorPoolInfo.flags = poolFlags;

            fixed (VkDescriptorPool* descriptorPool = &DescriptorPool)
            {
                if (VulkanNative.vkCreateDescriptorPool(Device.Device, &descriptorPoolInfo, null, descriptorPool) !=
                VkResult.VK_SUCCESS)
                {
                    throw new Exception("failed to create descriptor pool!");
                }
            }
           
        }

        unsafe public bool AllocateDescriptor(VkDescriptorSetLayout descriptorSetLayout, ref VkDescriptorSet descriptor) 
        {
            VkDescriptorSetAllocateInfo allocInfo = new();
            allocInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO;
            allocInfo.descriptorPool = DescriptorPool;
            allocInfo.pSetLayouts = &descriptorSetLayout;
            allocInfo.descriptorSetCount = 1;

            // Might want to create a "DescriptorPoolManager" class that handles this case, and builds
            // a new pool whenever an old pool fills up. But this is beyond our current scope
            fixed (VkDescriptorSet*  descriptorP = &descriptor)
            {
                if (VulkanNative.vkAllocateDescriptorSets(Device.Device, &allocInfo, descriptorP) != VkResult.VK_SUCCESS)
                {
                    return false;
                }
            }
           
            return true;
        }

        unsafe public void FreeDescriptors(ref VkDescriptorSet[] descriptors) 
        {
            fixed (VkDescriptorSet* descriptorP = &descriptors[0])
            {
                VulkanNative.vkFreeDescriptorSets(
                               Device.Device,
                               DescriptorPool,
                               (uint)descriptors.Length,
                               descriptorP);
            }
        }

        public void ResetPool() 
        {
            VulkanNative.vkResetDescriptorPool(Device.Device, DescriptorPool, 0);
        }

        unsafe public void DestroyDescriptorPool()
        {
            VulkanNative.vkDestroyDescriptorPool(Device.Device, DescriptorPool, null);
        }
    }

    public  class DescriptorWriterT
    {
        private DescriptorSetLayoutT SetLayout;
        private DescriptorPoolT Pool;
        private List<VkWriteDescriptorSet> Writes = new();
        public DescriptorWriterT(ref DescriptorSetLayoutT setLayout, ref DescriptorPoolT pool) 
        {
            SetLayout = setLayout;
            Pool = pool;


        }

        unsafe public DescriptorWriterT WriteBuffer(uint binding, ref VkDescriptorBufferInfo bufferInfo) 
        {
            VkDescriptorSetLayoutBinding bindingDescription = SetLayout.Bindings[binding];

            VkWriteDescriptorSet write = new();
            write.sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
            write.descriptorType = bindingDescription.descriptorType;
            write.dstBinding = binding;
            fixed (VkDescriptorBufferInfo* ptr = &bufferInfo)
            {
                write.pBufferInfo = ptr;
            }
            write.descriptorCount = 1;

            Writes.Add(write);

            return this;
        }

        unsafe public DescriptorWriterT WriteImage(uint binding, ref VkDescriptorImageInfo imageInfo) 
        {
            VkDescriptorSetLayoutBinding bindingDescription = SetLayout.Bindings[binding];

            VkWriteDescriptorSet write = new();
            write.sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
            write.descriptorType = bindingDescription.descriptorType;
            write.dstBinding = binding;
            fixed (VkDescriptorImageInfo* ptr = &imageInfo)
            {
                write.pImageInfo = ptr;
            }
            write.descriptorCount = 1;

            Writes.Add(write);
            return this;
        }

        public bool Build(ref VkDescriptorSet set)
        {
            bool success = Pool.AllocateDescriptor(SetLayout.GetDescriptorSetLayout, ref set);
            if (!success)
            {
                return false;
            }
            Overwrite(ref set);
            return true;
        }

        unsafe public void Overwrite(ref VkDescriptorSet set) 
        {
            VkWriteDescriptorSet[] arr = Writes.ToArray();

            for (int i = 0; i < arr.Length; i++)
			{
                arr[i].dstSet = set;
            }
            Writes = arr.ToList();
            fixed (VkWriteDescriptorSet* writes = &Writes.ToArray()[0])
            {
                VulkanNative.vkUpdateDescriptorSets(Pool.Device.Device, (uint)Writes.Count, writes, 0, null);
            }
        }

    };
}
