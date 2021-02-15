using System;
using SharpDX;
using BCnEncoder.Shared;
using Gorgon.Graphics;

namespace BCnEncoder.Encoder
{
    internal class Bc2BlockEncoder : BcBlockEncoder<Bc2Block>
	{
		public Bc2BlockEncoder()
			: base(4)
		{
		}

		protected override Bc2Block EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality)
		{
			switch (quality)
			{
				case CompressionQuality.Fast:
					return Bc2BlockEncoderFast.EncodeBlock(block);
				case CompressionQuality.Balanced:
					return Bc2BlockEncoderBalanced.EncodeBlock(block);
				case CompressionQuality.BestQuality:
					return Bc2BlockEncoderSlow.EncodeBlock(block);

				default:
					throw new ArgumentOutOfRangeException(nameof(quality), quality, null);
			}
		}

        #region Encoding private stuff

        private static Bc2Block TryColors(RawBlock4X4Rgba32 rawBlock, ColorRgb565 color0, ColorRgb565 color1, out float error, float rWeight = 0.3f, float gWeight = 0.6f, float bWeight = 0.1f)
		{
			var output = new Bc2Block();

            Span<GorgonColor> pixels = rawBlock.AsSpan;

			output.color0 = color0;
			output.color1 = color1;

			var c0 = color0.ToColorRgb24();
			var c1 = color1.ToColorRgb24();

			ReadOnlySpan<ColorRgb24> colors = stackalloc ColorRgb24[] {
					c0,
					c1,
					c0 * (2.0 / 3.0) + c1 * (1.0 / 3.0),
					c0 * (1.0 / 3.0) + c1 * (2.0 / 3.0)
				};

			error = 0;
			for (int i = 0; i < 16; i++)
			{
				GorgonColor color = pixels[i];
				output.SetAlpha(i, (byte)(color.Alpha * 255.0f));
				output[i] = ColorChooser.ChooseClosestColor4(colors, color, rWeight, gWeight, bWeight, out float e);
				error += e;
			}

			return output;
		}


		#endregion

		#region Encoders

		private static class Bc2BlockEncoderFast
		{

			internal static Bc2Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
			{
                Span<GorgonColor> pixels = rawBlock.AsSpan;

				PcaVectors.Create(pixels, out Vector3 mean, out Vector3 principalAxis);
				PcaVectors.GetMinMaxColor565(pixels, mean, principalAxis, out ColorRgb565 min, out ColorRgb565 max);
				
				ColorRgb565 c0 = max;
				ColorRgb565 c1 = min;

                Bc2Block output = TryColors(rawBlock, c0, c1, out float _);

				return output;
			}
		}

		private static class Bc2BlockEncoderBalanced {
			private const int MaxTries = 24 * 2;
			private const float ErrorThreshsold = 0.05f;

			internal static Bc2Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
			{
                Span<GorgonColor> pixels = rawBlock.AsSpan;

				PcaVectors.Create(pixels, out Vector3 mean, out Vector3 pa);
				PcaVectors.GetMinMaxColor565(pixels, mean, pa, out ColorRgb565 min, out ColorRgb565 max);

                ColorRgb565 c0 = max;
                ColorRgb565 c1 = min;

				Bc2Block best = TryColors(rawBlock, c0, c1, out float bestError);
				
				for (int i = 0; i < MaxTries; i++) {
					(ColorRgb565 newC0, ColorRgb565 newC1) = ColorVariationGenerator.Variate565(c0, c1, i);

                    Bc2Block block = TryColors(rawBlock, newC0, newC1, out float error);
					
					if (error < bestError)
					{
						best = block;
						bestError = error;
						c0 = newC0;
						c1 = newC1;
					}

					if (bestError < ErrorThreshsold) {
						break;
					}
				}

				return best;
			}
		}

		private static class Bc2BlockEncoderSlow
		{
			private const int MaxTries = 9999;
			private const float ErrorThreshsold = 0.01f;


			internal static Bc2Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
			{
                Span<GorgonColor> pixels = rawBlock.AsSpan;

				PcaVectors.Create(pixels, out Vector3 mean, out Vector3 pa);
				PcaVectors.GetMinMaxColor565(pixels, mean, pa, out ColorRgb565 min, out ColorRgb565 max);

                ColorRgb565 c0 = max;
                ColorRgb565 c1 = min;

				if (c0.data < c1.data)
				{
                    ColorRgb565 c = c0;
					c0 = c1;
					c1 = c;
				}

				Bc2Block best = TryColors(rawBlock, c0, c1, out float bestError);

				int lastChanged = 0;

				for (int i = 0; i < MaxTries; i++) {
					(ColorRgb565 newC0, ColorRgb565 newC1) = ColorVariationGenerator.Variate565(c0, c1, i);
					
					if (newC0.data < newC1.data)
					{
                        ColorRgb565 c = newC0;
						newC0 = newC1;
						newC1 = c;
					}

                    Bc2Block block = TryColors(rawBlock, newC0, newC1, out float error);

					lastChanged++;

					if (error < bestError)
					{
						best = block;
						bestError = error;
						c0 = newC0;
						c1 = newC1;
						lastChanged = 0;
					}

					if (bestError < ErrorThreshsold || lastChanged > ColorVariationGenerator.VarPatternCount) {
						break;
					}
				}

				return best;
			}
		}
		
		#endregion
	}
}
