using System;
using System.Buffers;
using System.Diagnostics;
using BCnEncoder.Shared;
using Gorgon.Graphics;

namespace BCnEncoder.Encoder
{
    internal class Bc5BlockEncoder : BcBlockEncoder<Bc5Block>
	{
		public Bc5BlockEncoder()
			: base(4)
		{
		}

		private Bc5Block RedIndexSetter(Bc5Block inBlock, int i, byte idx)
		{
			inBlock.SetRedIndex(i, idx);
			return inBlock;
		}

		private Bc5Block RedCol0Setter(Bc5Block inBlock, byte col)
		{
			inBlock.Red0 = col;
			return inBlock;
		}

		private Bc5Block RedCol1Setter(Bc5Block inBlock, byte col)
		{
			inBlock.Red0 = col;
			return inBlock;
		}

		private byte RedCol0Getter(Bc5Block inBlock) => inBlock.Red0;

		private byte RedCol1Getter(Bc5Block inBlock) => inBlock.Red1;

		private Bc5Block GreenIndexSetter(Bc5Block inBlock, int i, byte idx)
		{
			inBlock.SetGreenIndex(i, idx);
			return inBlock;
		}

		private Bc5Block GreenCol0Setter(Bc5Block inBlock, byte col)
		{
			inBlock.Green0 = col;
			return inBlock;
		}

		private Bc5Block GreenCol1Setter(Bc5Block inBlock, byte col)
		{
			inBlock.Green0 = col;
			return inBlock;
		}

		private byte GreenCol0Getter(Bc5Block inBlock) => inBlock.Green0;

		private byte GreenCol1Getter(Bc5Block inBlock) => inBlock.Green1;

		protected override Bc5Block EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality)
		{
			var output = new Bc5Block();
			byte[] redsArray = ArrayPool<byte>.Shared.Rent(16);
			byte[] greensArray = ArrayPool<byte>.Shared.Rent(16);

			var reds = new Span<byte>(redsArray, 0, 16);
			var greens = new Span<byte>(greensArray, 0, 16);

			Span<GorgonColor> pixels = block.AsSpan;
			for (int i = 0; i < 16; i++)
			{
				(int R, int G, int _, int _) = pixels[i].GetIntegerComponents();
				reds[i] = (byte)R;
				greens[i] = (byte)G;
			}

			int variations = 0;
			int ErrorThreshsold = 0;
			switch (quality)
			{
				case CompressionQuality.Fast:
					variations = 3;
					ErrorThreshsold = 5;
					break;
				case CompressionQuality.Balanced:
					variations = 5;
					ErrorThreshsold = 1;
					break;
				case CompressionQuality.BestQuality:
					variations = 8;
					ErrorThreshsold = 0;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(quality), quality, null);
			}

			output = FindValues(output, reds, variations, ErrorThreshsold, 
				RedIndexSetter,
				RedCol0Setter,
				RedCol1Setter,
				RedCol0Getter,
				RedCol1Getter
			);
			output = FindValues(output, greens, variations, ErrorThreshsold,
				GreenIndexSetter,
				GreenCol0Setter,
				GreenCol1Setter,
				GreenCol0Getter,
				GreenCol1Getter);
			return output;
		}

		public BufferFormat GetDxgiFormat() => BufferFormat.BC5_UNorm;

		#region Encoding private stuff

		private static int SelectIndices(ref Bc5Block block, Span<byte> pixels, int bestError,
			Func<Bc5Block, int, byte, Bc5Block> indexSetter,
			Func<Bc5Block, byte> col0Getter,
			Func<Bc5Block, byte> col1Getter)
		{
			int cumulativeError = 0;
			//var c0 = block.Red0;
			//var c1 = block.Red1;
			byte c0 = col0Getter(block);
			byte c1 = col1Getter(block);
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

				block = indexSetter(block, i, bestIndex);				
				cumulativeError += bestError * bestError;
			}

			return cumulativeError;
		}

		private static Bc5Block FindValues(Bc5Block colorBlock, Span<byte> pixels, int variations, int errorThreshsold,
			Func<Bc5Block, int, byte, Bc5Block> indexSetter,
			Func<Bc5Block, byte, Bc5Block> col0Setter,
			Func<Bc5Block, byte, Bc5Block> col1Setter,
			Func<Bc5Block, byte> col0Getter,
			Func<Bc5Block, byte> col1Getter)
		{

			//Find min and max alpha
			int bestError = 0;
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
				//colorBlock.Red0 = 0;
				//colorBlock.Red1 = 255;
				colorBlock = col0Setter(colorBlock, 0);
				colorBlock = col1Setter(colorBlock, 255);
				int error = SelectIndices(ref colorBlock, pixels, 0, indexSetter, col0Getter, col1Getter);
				Debug.Assert(0 == error);
				return colorBlock;
			}

			Bc5Block best = colorBlock;
			//best.Red0 = max;
			//best.Red1 = min;
			best = col0Setter(best, max);
			best = col1Setter(best, min);
			bestError = SelectIndices(ref colorBlock, pixels, 0, indexSetter, col0Getter, col1Getter);
			if (bestError == 0)
			{
				return best;
			}

			for (byte i = (byte)variations; i > 0; i--)
			{
				{
					byte c0 = ByteHelper.ClampToByte(max - i);
					byte c1 = ByteHelper.ClampToByte(min + i);
					Bc5Block block = colorBlock;
					//block.Red0 = hasExtremeValues ? c1 : c0;
					//block.Red1 = hasExtremeValues ? c0 : c1;
					block = col0Setter(block, hasExtremeValues ? c1 : c0);
					block = col1Setter(block, hasExtremeValues ? c0 : c1);
					int error = SelectIndices(ref colorBlock, pixels, bestError, indexSetter, col0Getter, col1Getter);
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
					Bc5Block block = colorBlock;
					//block.Red0 = hasExtremeValues ? c1 : c0;
					//block.Red1 = hasExtremeValues ? c0 : c1;
					block = col0Setter(block, hasExtremeValues ? c1 : c0);
					block = col1Setter(block, hasExtremeValues ? c0 : c1);
					int error = SelectIndices(ref colorBlock, pixels, bestError, indexSetter, col0Getter, col1Getter);
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
					Bc5Block block = colorBlock;
					//block.Red0 = hasExtremeValues ? c1 : c0;
					//block.Red1 = hasExtremeValues ? c0 : c1;
					block = col0Setter(block, hasExtremeValues ? c1 : c0);
					block = col1Setter(block, hasExtremeValues ? c0 : c1);
					int error = SelectIndices(ref colorBlock, pixels, bestError, indexSetter, col0Getter, col1Getter);
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
					Bc5Block block = colorBlock;
					//block.Red0 = hasExtremeValues ? c1 : c0;
					//block.Red1 = hasExtremeValues ? c0 : c1;
					block = col0Setter(block, hasExtremeValues ? c1 : c0);
					block = col1Setter(block, hasExtremeValues ? c0 : c1);
					int error = SelectIndices(ref colorBlock, pixels, bestError, indexSetter, col0Getter, col1Getter);
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
					Bc5Block block = colorBlock;
					//block.Red0 = hasExtremeValues ? c1 : c0;
					//block.Red1 = hasExtremeValues ? c0 : c1;
					block = col0Setter(block, hasExtremeValues ? c1 : c0);
					block = col1Setter(block, hasExtremeValues ? c0 : c1);
					int error = SelectIndices(ref colorBlock, pixels, bestError, indexSetter, col0Getter, col1Getter);
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
					Bc5Block block = colorBlock;
					//block.Red0 = hasExtremeValues ? c1 : c0;
					//block.Red1 = hasExtremeValues ? c0 : c1;
					block = col0Setter(block, hasExtremeValues ? c1 : c0);
					block = col1Setter(block, hasExtremeValues ? c0 : c1);
					int error = SelectIndices(ref colorBlock, pixels, bestError, indexSetter, col0Getter, col1Getter);
					if (error < bestError)
					{
						best = block;
						bestError = error;
						max = c0;
						min = c1;
					}
				}

				if (bestError <= errorThreshsold)
				{
					break;
				}
			}

			return best;
		}


		#endregion
	}
}