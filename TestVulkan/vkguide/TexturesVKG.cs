using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using StbImageSharp;

namespace TestVulkan
{
	public class TexturesVKG
	{
		public TexturesVKG()
		{
		}

		unsafe public static bool LoadImageFromFile(ref EngineVKG engine, string file, ref AllocatedImageVKG outImage)
		{
			byte[] buffer = File.ReadAllBytes(file);
			ImageResult pixels = ImageResult.FromMemory(buffer, ColorComponents.RedGreenBlueAlpha);

			if (pixels == null)
				return false;

			ulong imageSize = (ulong)(pixels.Width * pixels.Height * 4);

			//the format R8G8B8A8 matches exactly with the pixels loaded from stb_image lib
			Format image_format = Format.R8G8B8A8Srgb;

			//allocate temporary buffer for holding texture data to upload
			AllocatedBufferVKG stagingBuffer = engine.CreateBuffer(imageSize, BufferUsageFlags.BufferUsageTransferSrcBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit);
			VulkanMemoryChunk2 chunk = engine.memory2.ReturnChunk(stagingBuffer._allocation);

			//copy data to buffer
			void* data;
			engine._vk.MapMemory(engine._device, chunk.DeviceMemory, stagingBuffer._allocation.StartOffset, imageSize, 0, &data);
			Marshal.Copy(pixels.Data, 0, (IntPtr)data, pixels.Data.Length);
			engine._vk.UnmapMemory(engine._device, chunk.DeviceMemory);

			Extent3D imageExtent;
			imageExtent.Width = (uint)pixels.Width;
			imageExtent.Height = (uint)pixels.Height;
			imageExtent.Depth = 1;

			ImageCreateInfo dimg_info = InitializersVKG.ImageCreateInfo(image_format, ImageUsageFlags.ImageUsageSampledBit | ImageUsageFlags.ImageUsageTransferDstBit, imageExtent);

			AllocatedImageVKG newImage;

			if (engine._vk.CreateImage(engine._device, in dimg_info, null, out newImage._image) != Result.Success)
			{
				throw new Exception("failed to create image!");
			}

			newImage._allocation = engine.memory2.BindImageOrBuffer(ref engine._vk, ref engine._device, newImage._image, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);

			EngineVKG engineTmp = engine;
			engine.ImmediateSubmit(new Action<CommandBuffer>((cmd) =>
			{ 
			    ImageSubresourceRange range = new();
				range.AspectMask = ImageAspectFlags.ImageAspectColorBit;
				range.BaseMipLevel = 0;
				range.LevelCount = 1;
				range.BaseArrayLayer = 0;
				range.LayerCount = 1;

				ImageMemoryBarrier imageBarrier_toTransfer = new();
				imageBarrier_toTransfer.SType = StructureType.ImageMemoryBarrier;

				imageBarrier_toTransfer.OldLayout = ImageLayout.Undefined;
				imageBarrier_toTransfer.NewLayout = ImageLayout.TransferDstOptimal;
				imageBarrier_toTransfer.Image = newImage._image;
				imageBarrier_toTransfer.SubresourceRange = range;

				imageBarrier_toTransfer.SrcAccessMask = 0;
				imageBarrier_toTransfer.DstAccessMask = AccessFlags.AccessTransferWriteBit;

				//barrier the image into the transfer-receive layout
				engineTmp._vk.CmdPipelineBarrier(cmd, PipelineStageFlags.PipelineStageTopOfPipeBit, PipelineStageFlags.PipelineStageTransferBit, 0, 0, null, 0, null, 1, in imageBarrier_toTransfer);

				BufferImageCopy copyRegion = new();
				copyRegion.BufferOffset = 0;
				copyRegion.BufferRowLength = 0;
				copyRegion.BufferImageHeight = 0;

				copyRegion.ImageSubresource.AspectMask = ImageAspectFlags.ImageAspectColorBit;
				copyRegion.ImageSubresource.MipLevel = 0;
				copyRegion.ImageSubresource.BaseArrayLayer = 0;
				copyRegion.ImageSubresource.LayerCount = 1;
				copyRegion.ImageExtent = imageExtent;

				//copy the buffer into the image
				engineTmp._vk.CmdCopyBufferToImage(cmd, stagingBuffer._buffer, newImage._image, ImageLayout.TransferDstOptimal, 1, in copyRegion);

				ImageMemoryBarrier imageBarrier_toReadable = imageBarrier_toTransfer;

				imageBarrier_toReadable.OldLayout = ImageLayout.TransferDstOptimal;
				imageBarrier_toReadable.NewLayout = ImageLayout.ReadOnlyOptimal;

				imageBarrier_toReadable.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
				imageBarrier_toReadable.DstAccessMask = AccessFlags.AccessShaderReadBit;

				//barrier the image into the shader readable layout
				engineTmp._vk.CmdPipelineBarrier(cmd, PipelineStageFlags.PipelineStageTransferBit, PipelineStageFlags.PipelineStageFragmentShaderBit, 0, 0, null, 0, null, 1, in imageBarrier_toReadable);
			}));

			engine.deletors.Enqueue(new Action(() =>
			{
				VulkanMemoryChunk2 chunk = engineTmp.memory2.ReturnChunk(newImage._allocation);
				engineTmp._vk.DestroyImage(engineTmp._device, newImage._image, null);
				engineTmp.memory2.FreeOne(ref engineTmp._vk, ref engineTmp._device, chunk, newImage._allocation);
			}));

			engine = engineTmp;

			VulkanMemoryChunk2 chunk2 = engine.memory2.ReturnChunk(stagingBuffer._allocation);
			engine._vk.DestroyBuffer(engine._device, stagingBuffer._buffer, null);
			engine.memory2.FreeOne(ref engine._vk, ref engine._device, chunk2, stagingBuffer._allocation);

			Trace.WriteLine("Texture loaded successfully!");

			outImage = newImage;
			return true;
		}
	}
}
