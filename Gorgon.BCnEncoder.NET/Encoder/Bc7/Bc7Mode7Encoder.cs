using System;
using System.Buffers;
using Gorgon.BCnEncoder.Shared;

namespace Gorgon.BCnEncoder.Encoder.Bc7;

internal static class Bc7Mode7Encoder
{

    public static Bc7Block EncodeBlock(RawBlock4X4Rgba32 block, int startingVariation, int bestPartition)
    {
        ColorRgba32[] colors = ArrayPool<ColorRgba32>.Shared.Rent(4);
        (byte r, byte g, byte b, byte a)[] endPoints = ArrayPool<(byte, byte, byte, byte)>.Shared.Rent(4);
        byte[] pBitsArray = ArrayPool<byte>.Shared.Rent(4);
        byte[] indicesArray = ArrayPool<byte>.Shared.Rent(16);

        var pBits = new Span<byte>(pBitsArray, 0, 4);
        var indices = new Span<byte>(indicesArray, 0, 16);

        var output = new Bc7Block();
        const Bc7BlockType type = Bc7BlockType.Type7;

        ReadOnlySpan<int> partitionTable = Bc7Block.Subsets2PartitionTable[bestPartition];

        int[] anchorIndices = [
                0,
                Bc7Block.Subsets2AnchorIndices[bestPartition]
            ];

        try
        {
            for (int subset = 0; subset < 2; subset++)
            {

                Bc7EncodingHelpers.GetInitialUnscaledEndpointsForSubset(block, out ColorRgba32 ep0, out ColorRgba32 ep1,
                    partitionTable, subset);
                ColorRgba32 scaledEp0 =
                    Bc7EncodingHelpers.ScaleDownEndpoint(ep0, type, false, out byte pBit0);
                ColorRgba32 scaledEp1 =
                    Bc7EncodingHelpers.ScaleDownEndpoint(ep1, type, false, out byte pBit1);

                Bc7EncodingHelpers.OptimizeSubsetEndpointsWithPBit(type, block, ref scaledEp0,
                    ref scaledEp1, ref pBit0, ref pBit1, startingVariation, partitionTable, subset, true, true);

                ep0 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp0, pBit0);
                ep1 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp1, pBit1);
                Bc7EncodingHelpers.FillSubsetIndices(type, block,
                    ep0,
                    ep1,
                    partitionTable, subset, indices);

                if ((indices[anchorIndices[subset]] & 0b10) > 0) //If anchor index most significant bit is 1, switch endpoints
                {
                    ColorRgba32 c = scaledEp0;
                    byte p = pBit0;

                    scaledEp0 = scaledEp1;
                    pBit0 = pBit1;
                    scaledEp1 = c;
                    pBit1 = p;

                    //redo indices
                    ep0 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp0, pBit0);
                    ep1 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp1, pBit1);
                    Bc7EncodingHelpers.FillSubsetIndices(type, block,
                        ep0,
                        ep1,
                        partitionTable, subset, indices);
                }

                colors[subset * 2] = scaledEp0;
                colors[subset * 2 + 1] = scaledEp1;
                pBits[subset * 2] = pBit0;
                pBits[subset * 2 + 1] = pBit1;
            }

            for (int i = 0; i < 4; ++i)
            {
                endPoints[i] = (colors[i].r, colors[i].g, colors[i].b, colors[i].a);
            }

            output.PackType7(bestPartition, new Span<(byte, byte, byte, byte)>(endPoints, 0, 4), pBits, indices);

            return output;
        }
        finally
        {
            ArrayPool<ColorRgba32>.Shared.Return(colors);
            ArrayPool<(byte, byte, byte, byte)>.Shared.Return(endPoints);
            ArrayPool<byte>.Shared.Return(pBitsArray);
            ArrayPool<byte>.Shared.Return(indicesArray);
        }
    }
}
