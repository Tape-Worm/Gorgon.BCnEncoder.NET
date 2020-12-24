using System;
using BCnEncoder.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace BCnEncTests
{
	public class BlockTests
	{
		[Fact]
		public void CreateBlocksExact()
		{
			using var testImage = new Image<Rgba32>(16, 16);

            RawBlock4X4Rgba32[] blocks = ImageToBlocks.ImageTo4X4(testImage.Frames[0], out int blocksWidth, out int blocksHeight);

			Assert.Equal(16, blocks.Length);
			Assert.Equal(4, blocksWidth);
			Assert.Equal(4, blocksHeight);
		}

		[Fact]
		public void CreateBlocksPadding()
		{
			using var testImage = new Image<Rgba32>(11, 15);

            RawBlock4X4Rgba32[] blocks = ImageToBlocks.ImageTo4X4(testImage.Frames[0], out int blocksWidth, out int blocksHeight);

			Assert.Equal(12, blocks.Length);
			Assert.Equal(3, blocksWidth);
			Assert.Equal(4, blocksHeight);
		}

		[Fact]
		public void PaddingColor()
		{
			using var testImage = new Image<Rgba32>(13, 13);

			if (!testImage.TryGetSinglePixelSpan(out Span<Rgba32> pixels)) {
				throw new Exception("Cannot get pixel span.");
			}
			for (int i = 0; i < pixels.Length; i++) {
				pixels[i] = new Rgba32(0, 125, 125);
			}

            RawBlock4X4Rgba32[] blocks = ImageToBlocks.ImageTo4X4(testImage.Frames[0], out int blocksWidth, out int blocksHeight);

			Assert.Equal(16, blocks.Length);
			Assert.Equal(4, blocksWidth);
			Assert.Equal(4, blocksHeight);

			for (int x = 0; x < blocksWidth; x++) {
				for (int y = 0; y < blocksHeight; y++) {
					foreach (Rgba32 color in blocks[x + y * blocksWidth].AsSpan) {
						Assert.Equal(new Rgba32(0, 125, 125), color);
					}
				}
			}
		}

		[Fact]
		public void BlocksToImage()
		{
			var r = new Random(0);
			using var testImage = new Image<Rgba32>(16, 16);

			if (!testImage.TryGetSinglePixelSpan(out Span<Rgba32> pixels)) {
				throw new Exception("Cannot get pixel span.");
			}
			for (int i = 0; i < pixels.Length; i++) {
				pixels[i] = new Rgba32(
					(byte)r.Next(255),
					(byte)r.Next(255),
					(byte)r.Next(255),
					(byte)r.Next(255));
			}

            RawBlock4X4Rgba32[] blocks = ImageToBlocks.ImageTo4X4(testImage.Frames[0], out int blocksWidth, out int blocksHeight);

			Assert.Equal(16, blocks.Length);
			Assert.Equal(4, blocksWidth);
			Assert.Equal(4, blocksHeight);

			using Image<Rgba32> output = ImageToBlocks.ImageFromRawBlocks(blocks, blocksWidth, blocksHeight);
			
			if (!output.TryGetSinglePixelSpan(out Span<Rgba32> pixels2)) {
				throw new Exception("Cannot get pixel span.");
			}

			Assert.Equal(pixels.Length, pixels2.Length);
			for (int i = 0; i < pixels.Length; i++) {
				Assert.Equal(pixels[i], pixels2[i]);
			}
		}

		[Fact]
		public void BlockError()
		{
			using var testImage = new Image<Rgba32>(16, 16);

            RawBlock4X4Rgba32[] blocks = ImageToBlocks.ImageTo4X4(testImage.Frames[0], out int blocksWidth, out int blocksHeight);

            RawBlock4X4Rgba32 block1 = blocks[2 + 2 * blocksWidth];
            RawBlock4X4Rgba32 block2 = blocks[2 + 2 * blocksWidth];

			Assert.Equal(0, block1.CalculateError(block2));

			for (int i = 0; i < block2.AsSpan.Length; i++) {
				block2.AsSpan[i].R = (byte) (block2.AsSpan[i].R + 2);
			}
			Assert.Equal(2, block1.CalculateError(block2));

			for (int i = 0; i < block2.AsSpan.Length; i++) {
				block2.AsSpan[i].G = (byte) (block2.AsSpan[i].R + 20);
			}
			Assert.Equal(22, block1.CalculateError(block2));
		}
	}
}
