using System;
using System.Buffers;
using BCnEncoder.Shared;

namespace BCnEncoder.Encoder.Bc7;

internal static class Bc7Mode1Encoder
{

    public static Bc7Block EncodeBlock(RawBlock4X4Rgba32 block, int startingVariation, int bestPartition)
    {
        var output = new Bc7Block();
        const Bc7BlockType type = Bc7BlockType.Type1;


        ColorRgba32[] colors = ArrayPool<ColorRgba32>.Shared.Rent(4);
        (byte r, byte g, byte b)[] endPoints = ArrayPool<(byte, byte, byte)>.Shared.Rent(4);
        byte[] pBits = new byte[2];
        ReadOnlySpan<int> partitionTable = Bc7Block.Subsets2PartitionTable[bestPartition];

        byte[] indicesArray = ArrayPool<byte>.Shared.Rent(16);
        var indices = new Span<byte>(indicesArray, 0, 16);

        int[] anchorIndices = new int[] {
            0,
            Bc7Block.Subsets2AnchorIndices[bestPartition]
        };

        try
        {
            for (int subset = 0; subset < 2; subset++)
            {
                Bc7EncodingHelpers.GetInitialUnscaledEndpointsForSubset(block, out ColorRgba32 ep0, out ColorRgba32 ep1,
                    partitionTable, subset);
                ColorRgba32 scaledEp0 =
                    Bc7EncodingHelpers.ScaleDownEndpoint(ep0, type, true, out byte _);
                ColorRgba32 scaledEp1 =
                    Bc7EncodingHelpers.ScaleDownEndpoint(ep1, type, true, out byte pBit);

                Bc7EncodingHelpers.OptimizeSubsetEndpointsWithPBit(type, block, ref scaledEp0,
                    ref scaledEp1, ref pBit, ref pBit, startingVariation, partitionTable, subset, true, false);

                ep0 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp0, pBit);
                ep1 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp1, pBit);
                Bc7EncodingHelpers.FillSubsetIndices(type, block,
                    ep0,
                    ep1,
                    partitionTable, subset, indices);

                if ((indices[anchorIndices[subset]] & 0b100) > 0) //If anchor index most significant bit is 1, switch endpoints
                {
                    (scaledEp1, scaledEp0) = (scaledEp0, scaledEp1);

                    //redo indices
                    ep0 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp0, pBit);
                    ep1 = Bc7EncodingHelpers.ExpandEndpoint(type, scaledEp1, pBit);
                    Bc7EncodingHelpers.FillSubsetIndices(type, block,
                        ep0,
                        ep1,
                        partitionTable, subset, indices);
                }

                colors[subset * 2] = scaledEp0;
                colors[subset * 2 + 1] = scaledEp1;
                pBits[subset] = pBit;
            }

            for (int i = 0; i < 4; ++i)
            {
                endPoints[i] = (colors[i].r, colors[i].g, colors[i].b);
            }

            output.PackType1(bestPartition, new Span<(byte r, byte g, byte b)>(endPoints, 0, 4), pBits, indices);

            return output;
        }
        finally
        {
            ArrayPool<(byte, byte, byte)>.Shared.Return(endPoints, true);
            ArrayPool<ColorRgba32>.Shared.Return(colors, true);
            ArrayPool<byte>.Shared.Return(indicesArray, true);
        }
    }
}
