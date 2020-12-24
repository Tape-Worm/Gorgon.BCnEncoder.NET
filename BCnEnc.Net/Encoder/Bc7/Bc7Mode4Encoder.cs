using System;
using System.Buffers;
using BCnEncoder.Shared;

namespace BCnEncoder.Encoder.Bc7
{
	internal static class Bc7Mode4Encoder
	{

		private static ReadOnlySpan<int> PartitionTable => new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		const int Subset = 0;

		public static Bc7Block EncodeBlock(RawBlock4X4Rgba32 block, int startingVariation)
		{
            Bc7BlockType type = Bc7BlockType.Type4;

			Span<Bc7Block> outputs = stackalloc Bc7Block[8];
			byte[] colorIndicesArray = ArrayPool<byte>.Shared.Rent(16);
			byte[] alphaIndicesArray = ArrayPool<byte>.Shared.Rent(16);
			var colorEndPoints = new (byte r, byte g, byte b)[2];
            byte[] alphaEndPoints = new byte[2];

			try
			{
				for (int idxMode = 0; idxMode < 2; idxMode++)
				{
					for (int rotation = 0; rotation < 4; rotation++)
					{
						RawBlock4X4Rgba32 rotatedBlock = Bc7EncodingHelpers.RotateBlockColors(block, rotation);
						var output = new Bc7Block();

						Bc7EncodingHelpers.GetInitialUnscaledEndpoints(rotatedBlock, out ColorRgba32 ep0, out ColorRgba32 ep1);

						ColorRgba32 scaledEp0 =
							Bc7EncodingHelpers.ScaleDownEndpoint(ep0, type, false, out byte _);
						ColorRgba32 scaledEp1 =
							Bc7EncodingHelpers.ScaleDownEndpoint(ep1, type, false, out byte _);

						byte pBit = 0; //fake pBit

						Bc7EncodingHelpers.OptimizeSubsetEndpointsWithPBit(type, rotatedBlock, ref scaledEp0,
							ref scaledEp1, ref pBit, ref pBit, startingVariation, PartitionTable, Subset,
							false, true, idxMode);

						ep0 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp0, 0);
						ep1 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp1, 0);

						Array.Clear(colorIndicesArray, 0, colorIndicesArray.Length);
						Array.Clear(alphaIndicesArray, 0, alphaIndicesArray.Length);
						var colorIndices = new Span<byte>(colorIndicesArray, 0, 16);
						var alphaIndices = new Span<byte>(alphaIndicesArray, 0, 16);
						Bc7EncodingHelpers.FillAlphaColorIndices(type, rotatedBlock,
							ep0,
							ep1,
							colorIndices, alphaIndices, idxMode);

						bool needsRedo = false;


						if ((colorIndices[0] & (idxMode == 0 ? 0b10 : 0b100)) > 0) //If anchor index most significant bit is 1, switch endpoints
						{
							ColorRgba32 c = scaledEp0;
							byte alpha0 = scaledEp0.a;
							byte alpha1 = scaledEp1.a;

							scaledEp0 = scaledEp1;
							scaledEp1 = c;
							scaledEp0.a = alpha0;
							scaledEp1.a = alpha1;

							needsRedo = true;
						}
						if ((alphaIndices[0] & (idxMode == 0 ? 0b100 : 0b10)) > 0) //If anchor index most significant bit is 1, switch endpoints
						{
							byte a = scaledEp0.a;

							scaledEp0.a = scaledEp1.a;
							scaledEp1.a = a;

							needsRedo = true;
						}

						if (needsRedo)
						{
							//redo indices
							ep0 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp0, 0);
							ep1 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp1, 0);
							Bc7EncodingHelpers.FillAlphaColorIndices(type, rotatedBlock,
								ep0,
								ep1,
								colorIndices, alphaIndices, idxMode);
						}

						colorEndPoints[0] = (scaledEp0.r, scaledEp0.g, scaledEp0.b);
						colorEndPoints[1] = (scaledEp1.r, scaledEp1.g, scaledEp1.b);
						alphaEndPoints[0] = scaledEp0.a;
						alphaEndPoints[1] = scaledEp1.a;

						if (idxMode == 0)
						{
							output.PackType4(rotation, (byte)idxMode, colorEndPoints, alphaEndPoints, colorIndices, alphaIndices);
						}
						else
						{
							output.PackType4(rotation, (byte)idxMode, colorEndPoints, alphaEndPoints, alphaIndices, colorIndices);
						}

						outputs[idxMode * 4 + rotation] = output;
					}
				}

				int bestIndex = 0;
				float bestError = 0;
				bool first = true;

				// Find best out of generated blocks
				for (int i = 0; i < outputs.Length; i++)
				{
					RawBlock4X4Rgba32 decoded = outputs[i].Decode();

					float error = block.CalculateYCbCrAlphaError(decoded);
					if (error < bestError || first)
					{
						first = false;
						bestError = error;
						bestIndex = i;
					}
				}

				return outputs[bestIndex];
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(alphaIndicesArray, true);
				ArrayPool<byte>.Shared.Return(colorIndicesArray, true);
			}

		}
	}
}
