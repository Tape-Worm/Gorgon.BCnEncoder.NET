using System;
using System.Buffers;
using BCnEncoder.Encoder.Bc7;
using BCnEncoder.Properties;
using BCnEncoder.Shared;
using Gorgon.Graphics;
using Gorgon.Math;
using Gorgon.Native;

namespace BCnEncoder.Encoder
{
    /// <summary>
    /// Handles all encoding of images into compressed or uncompressed formats. 
    /// </summary>
    public class BcEncoder
	{
		/// <summary>
		/// If true, when encoding to a format that only includes a red channel,
		/// use the pixel luminance instead of just the red channel. Default is false.
		/// </summary>
		public bool LuminanceAsRed
		{
			get;
			set;
		} = false;

		private static void ImageTo4X4(Span<RawBlock4X4Rgba32> output, GorgonPtr<int> image, BufferFormat sourceFormat, int imageWidth, int imageHeight, int blocksWidth, int blocksHeight)
		{
			if (sourceFormat is not BufferFormat.R8G8B8A8_UNorm and not BufferFormat.R8G8B8A8_UNorm_SRgb and not BufferFormat.B8G8R8A8_UNorm and not BufferFormat.B8G8R8A8_UNorm_SRgb and not BufferFormat.B8G8R8X8_UNorm and not BufferFormat.B8G8R8X8_UNorm_SRgb)
			{
				throw new NotSupportedException(string.Format(Resources.BCENC_ERR_SOURCE_FORMAT_NOT_SUPPORTED, sourceFormat));
			}

			for (int y = 0; y < imageHeight; y++)
			{
				for (int x = 0; x < imageWidth; x++)
				{
					int pixels = image[x + y * imageWidth];
					GorgonColor color = GorgonColor.Black;

					switch (sourceFormat)
					{
						case BufferFormat.R8G8B8A8_UNorm:
						case BufferFormat.R8G8B8A8_UNorm_SRgb:
							color = GorgonColor.FromABGR(pixels);
							break;
						case BufferFormat.B8G8R8A8_UNorm:
						case BufferFormat.B8G8R8A8_UNorm_SRgb:
						case BufferFormat.B8G8R8X8_UNorm:
						case BufferFormat.B8G8R8X8_UNorm_SRgb:
							color = new GorgonColor(pixels);
							break;
					}

					int blockIndexX = (int)Math.Floor(x / 4.0f);
					int blockIndexY = (int)Math.Floor(y / 4.0f);
					int blockInternalIndexX = x % 4;
					int blockInternalIndexY = y % 4;

					int index = blockIndexX + blockIndexY * blocksWidth;
					ref readonly RawBlock4X4Rgba32 block = ref output[index];

					block[blockInternalIndexX, blockInternalIndexY] = color;
				}
			}

			//Fill in block y with edge color
			if (imageHeight % 4 != 0)
			{
				int yPaddingStart = imageHeight % 4;
				for (int i = 0; i < blocksWidth; i++)
				{
					RawBlock4X4Rgba32 lastBlock = output[i + blocksWidth * (blocksHeight - 1)];
					for (int y = yPaddingStart; y < 4; y++)
					{
						for (int x = 0; x < 4; x++)
						{
							lastBlock[x, y] = lastBlock[x, y - 1];
						}
					}
					output[i + blocksWidth * (blocksHeight - 1)] = lastBlock;
				}
			}

			//Fill in block x with edge color
			if (imageWidth % 4 != 0)
			{
				int xPaddingStart = imageWidth % 4;
				for (int i = 0; i < blocksHeight; i++)
				{
					RawBlock4X4Rgba32 lastBlock = output[blocksWidth - 1 + i * blocksWidth];
					for (int x = xPaddingStart; x < 4; x++)
					{
						for (int y = 0; y < 4; y++)
						{
							lastBlock[x, y] = lastBlock[x - 1, y];
						}
					}
					output[blocksWidth - 1 + i * blocksWidth] = lastBlock;
				}
			}
		}

		private IBcBlockEncoder GetEncoder(BufferFormat format, bool includeAlpha)
		{
			switch (format)
			{
				case BufferFormat.BC1_UNorm when includeAlpha:
				case BufferFormat.BC1_UNorm_SRgb when includeAlpha:
					return new Bc1AlphaBlockEncoder();
				case BufferFormat.BC1_UNorm:
				case BufferFormat.BC1_UNorm_SRgb:
					return new Bc1BlockEncoder();
				case BufferFormat.BC2_UNorm:
				case BufferFormat.BC2_UNorm_SRgb:
					return new Bc2BlockEncoder();
				case BufferFormat.BC3_UNorm:
				case BufferFormat.BC3_UNorm_SRgb:
					return new Bc3BlockEncoder();
				case BufferFormat.BC4_UNorm:
				case BufferFormat.BC4_SNorm:
					return new Bc4BlockEncoder(LuminanceAsRed);
				case BufferFormat.BC5_UNorm:
				case BufferFormat.BC5_SNorm:
					return new Bc5BlockEncoder();
				case BufferFormat.BC7_UNorm:
				case BufferFormat.BC7_UNorm_SRgb:
					return new Bc7Encoder();
				default:
					return null;
			}
		}

		/// <summary>
		/// Encodes all mipmap levels into a list of byte buffers.
		/// </summary>
		public GorgonNativeBuffer<byte> EncodeToRawBytes(GorgonPtr<byte> inputImage, 
														 int width, 
														 int height, 
														 bool include1BitAlpha, 
														 BufferFormat sourceFormat,
														 BufferFormat compressionFormat, 
														 CompressionQuality quality = CompressionQuality.Balanced, 
														 bool useMultipleThreads = true)
		{
			int blocksWidth = (int)(width / 4.0f).FastCeiling();
			int blocksHeight = (int)(height / 4.0f).FastCeiling();
			int blockCount = blocksWidth * blocksHeight;
			IBcBlockEncoder compressedEncoder;

			compressedEncoder = GetEncoder(compressionFormat, include1BitAlpha);
			
			if (compressedEncoder == null)
			{
				throw new NotSupportedException(string.Format(Resources.BCENC_ERR_FORMAT_NOT_SUPPORTED, compressionFormat));
			}

			RawBlock4X4Rgba32[] blocks = ArrayPool<RawBlock4X4Rgba32>.Shared.Rent(blockCount);

			try
			{
				// Initialize our blocks.
				for (int i = 0; i < blockCount; ++i)
				{
					blocks[i] = new RawBlock4X4Rgba32();
				}

                ImageTo4X4(new Span<RawBlock4X4Rgba32>(blocks, 0, blockCount), inputImage.To<int>(), sourceFormat, width, height, blocksWidth, blocksHeight);

				return compressedEncoder.Encode(blocks, blockCount, quality, useMultipleThreads);
			}
			finally
			{
				ArrayPool<RawBlock4X4Rgba32>.Shared.Return(blocks, true);
			}
		}

	}		
}
