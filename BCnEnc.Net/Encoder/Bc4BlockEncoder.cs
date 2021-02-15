using System;
using System.Buffers;
using System.Diagnostics;
using BCnEncoder.Shared;
using Gorgon.Graphics;

namespace BCnEncoder.Encoder
{
    internal class Bc4BlockEncoder : BcBlockEncoder<Bc4Block>
	{

		private readonly bool _luminanceAsRed;

		public Bc4BlockEncoder(bool luminanceAsRed)
			: base(4) => _luminanceAsRed = luminanceAsRed;

		protected override Bc4Block EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality)
		{
			var output = new Bc4Block();
			byte[] colorsArray = ArrayPool<byte>.Shared.Rent(16);

			var colors = new Span<byte>(colorsArray, 0, 16);
			Span<GorgonColor> pixels = block.AsSpan;

			try
			{
				for (int i = 0; i < 16; i++)
				{
					int r = (int)(pixels[i].Red * 255.0f);
					if (_luminanceAsRed)
					{
						colors[i] = (byte)(new ColorYCbCr(GorgonColor.FromRGBA(pixels[i])).y * 255);
					}
					else
					{
						colors[i] = (byte)r;
					}
				}
				switch (quality)
				{
					case CompressionQuality.Fast:
						return FindRedValues(output, colors, 3);
					case CompressionQuality.Balanced:
						return FindRedValues(output, colors, 4);
					case CompressionQuality.BestQuality:
						return FindRedValues(output, colors, 8);

					default:
						throw new ArgumentOutOfRangeException(nameof(quality), quality, null);
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(colorsArray, true);
			}
		}

		#region Encoding private stuff
		private static int SelectIndices(ref Bc4Block block, Span<byte> pixels, int bestError)
		{
			int cumulativeError = 0;
			byte c0 = block.Red0;
			byte c1 = block.Red1;
			Span<byte> colors = c0 > c1
				? stackalloc byte[] {
				c0,
				c1,
				(byte)(6 / 7.0 * c0 + 1 / 7.0 * c1),
				(byte)(5 / 7.0 * c0 + 2 / 7.0 * c1),
				(byte)(4 / 7.0 * c0 + 3 / 7.0 * c1),
				(byte)(3 / 7.0 * c0 + 4 / 7.0 * c1),
				(byte)(2 / 7.0 * c0 + 5 / 7.0 * c1),
				(byte)(1 / 7.0 * c0 + 6 / 7.0 * c1),
			}
				: stackalloc byte[] {
				c0,
				c1,
				(byte)(4 / 5.0 * c0 + 1 / 5.0 * c1),
				(byte)(3 / 5.0 * c0 + 2 / 5.0 * c1),
				(byte)(2 / 5.0 * c0 + 3 / 5.0 * c1),
				(byte)(1 / 5.0 * c0 + 4 / 5.0 * c1),
				0,
				255
			};
			for (int i = 0; i < pixels.Length; i++)
			{
				byte bestIndex = 0;
				bestError = Math.Abs(pixels[i] - colors[0]);
				for (byte j = 1; j < colors.Length; j++)
				{
					int error = Math.Abs(pixels[i] - colors[j]);
					if (error < bestError)
					{
						bestIndex = j;
						bestError = error;
					}

					if (bestError == 0)
					{
						break;
					}
				}

				block.SetRedIndex(i, bestIndex);
				cumulativeError += bestError * bestError;
			}

			return cumulativeError;
		}

		private static Bc4Block FindRedValues(Bc4Block colorBlock, Span<byte> pixels, int variations)
		{
			int bestError;
			//Find min and max alpha
			byte min = 255;
			byte max = 0;
			bool hasExtremeValues = false;
			for (int i = 0; i < pixels.Length; i++)
			{
				if (pixels[i] < 255 && pixels[i] > 0)
				{
					if (pixels[i] < min)
					{
						min = pixels[i];
					}

					if (pixels[i] > max)
					{
						max = pixels[i];
					}
				}
				else
				{
					hasExtremeValues = true;
				}
			}

			//everything is either fully black or fully red
			if (hasExtremeValues && min == 255 && max == 0)
			{
				colorBlock.Red0 = 0;
				colorBlock.Red1 = 255;
				int error = SelectIndices(ref colorBlock, pixels, 0);
				Debug.Assert(0 == error, $"There should not be an error value here: {error}");
				return colorBlock;
			}

			Bc4Block best = colorBlock;
			best.Red0 = max;
			best.Red1 = min;
			bestError = SelectIndices(ref best, pixels, 0);
			if (bestError == 0)
			{
				return best;
			}

			for (byte i = (byte)variations; i > 0; i--)
			{
				{
					byte c0 = ByteHelper.ClampToByte(max - i);
					byte c1 = ByteHelper.ClampToByte(min + i);
					Bc4Block block = colorBlock;
					block.Red0 = hasExtremeValues ? c1 : c0;
					block.Red1 = hasExtremeValues ? c0 : c1;
					int error = SelectIndices(ref block, pixels, bestError);
					if (error < bestError)
					{
						best = block;
						bestError = error;
						max = c0;
						min = c1;
					}
				}
				{
					byte c0 = ByteHelper.ClampToByte(max + i);
					byte c1 = ByteHelper.ClampToByte(min - i);
					Bc4Block block = colorBlock;
					block.Red0 = hasExtremeValues ? c1 : c0;
					block.Red1 = hasExtremeValues ? c0 : c1;
					int error = SelectIndices(ref block, pixels, bestError);
					if (error < bestError)
					{
						best = block;
						bestError = error;
						max = c0;
						min = c1;
					}
				}
				{
					byte c0 = ByteHelper.ClampToByte(max);
					byte c1 = ByteHelper.ClampToByte(min - i);
					Bc4Block block = colorBlock;
					block.Red0 = hasExtremeValues ? c1 : c0;
					block.Red1 = hasExtremeValues ? c0 : c1;
					int error = SelectIndices(ref block, pixels, bestError);
					if (error < bestError)
					{
						best = block;
						bestError = error;
						max = c0;
						min = c1;
					}
				}
				{
					byte c0 = ByteHelper.ClampToByte(max + i);
					byte c1 = ByteHelper.ClampToByte(min);
					Bc4Block block = colorBlock;
					block.Red0 = hasExtremeValues ? c1 : c0;
					block.Red1 = hasExtremeValues ? c0 : c1;
					int error = SelectIndices(ref block, pixels, bestError);
					if (error < bestError)
					{
						best = block;
						bestError = error;
						max = c0;
						min = c1;
					}
				}
				{
					byte c0 = ByteHelper.ClampToByte(max);
					byte c1 = ByteHelper.ClampToByte(min + i);
					Bc4Block block = colorBlock;
					block.Red0 = hasExtremeValues ? c1 : c0;
					block.Red1 = hasExtremeValues ? c0 : c1;
					int error = SelectIndices(ref block, pixels, bestError);
					if (error < bestError)
					{
						best = block;
						bestError = error;
						max = c0;
						min = c1;
					}
				}
				{
					byte c0 = ByteHelper.ClampToByte(max - i);
					byte c1 = ByteHelper.ClampToByte(min);
					Bc4Block block = colorBlock;
					block.Red0 = hasExtremeValues ? c1 : c0;
					block.Red1 = hasExtremeValues ? c0 : c1;
					int error = SelectIndices(ref block, pixels, bestError);
					if (error < bestError)
					{
						best = block;
						bestError = error;
						max = c0;
						min = c1;
					}
				}

				if (bestError < 5)
				{
					break;
				}
			}

			return best;
		}


		#endregion
	}
}