using System;
using System.Buffers;
using BCnEncoder.Shared;

namespace BCnEncoder.Encoder.Bc7;

internal static class Bc7Mode6Encoder
{
    public static Bc7Block EncodeBlock(RawBlock4X4Rgba32 block, int startingVariation)
    {
        int[] partitionTableArray = ArrayPool<int>.Shared.Rent(16);
        byte[] indicesArray = ArrayPool<byte>.Shared.Rent(16);

        var partitionTable = new ReadOnlySpan<int>(partitionTableArray, 0, 16);
        var indices = new Span<byte>(indicesArray, 0, 16);

        (byte r, byte g, byte b, byte a)[] endPoints = new (byte, byte, byte, byte)[2];
        byte[] pBits = new byte[2];

        try
        {
            bool hasAlpha = block.HasTransparentPixels();

            var output = new Bc7Block();
            Bc7EncodingHelpers.GetInitialUnscaledEndpoints(block, out ColorRgba32 ep0, out ColorRgba32 ep1);

            ColorRgba32 scaledEp0 =
                Bc7EncodingHelpers.ScaleDownEndpoint(ep0, Bc7BlockType.Type6, false, out byte pBit0);
            ColorRgba32 scaledEp1 =
                Bc7EncodingHelpers.ScaleDownEndpoint(ep1, Bc7BlockType.Type6, false, out byte pBit1);

            const int subset = 0;

            //Force 255 alpha if fully opaque
            if (!hasAlpha)
            {
                pBit0 = 1;
                pBit1 = 1;
            }

            Bc7EncodingHelpers.OptimizeSubsetEndpointsWithPBit(Bc7BlockType.Type6, block, ref scaledEp0,
                ref scaledEp1, ref pBit0, ref pBit1, startingVariation, partitionTable, subset, hasAlpha, hasAlpha);

            ep0 = Bc7EncodingHelpers.ExpandEndpoint(Bc7BlockType.Type6, scaledEp0, pBit0);
            ep1 = Bc7EncodingHelpers.ExpandEndpoint(Bc7BlockType.Type6, scaledEp1, pBit1);

            Bc7EncodingHelpers.FillSubsetIndices(Bc7BlockType.Type6, block,
                ep0,
                ep1,
                partitionTable, subset, indices);

            if ((indices[0] & 0b1000) > 0) //If anchor index most significant bit is 1, switch endpoints
            {
                ColorRgba32 c = scaledEp0;
                byte p = pBit0;

                scaledEp0 = scaledEp1;
                pBit0 = pBit1;
                scaledEp1 = c;
                pBit1 = p;

                //redo indices
                ep0 = Bc7EncodingHelpers.ExpandEndpoint(Bc7BlockType.Type6, scaledEp0, pBit0);
                ep1 = Bc7EncodingHelpers.ExpandEndpoint(Bc7BlockType.Type6, scaledEp1, pBit1);
                Bc7EncodingHelpers.FillSubsetIndices(Bc7BlockType.Type6, block,
                    ep0,
                    ep1,
                    partitionTable, subset, indices);
            }

            endPoints[0] = (scaledEp0.r, scaledEp0.g, scaledEp0.b, scaledEp0.a);
            endPoints[1] = (scaledEp1.r, scaledEp1.g, scaledEp1.b, scaledEp1.a);
            pBits[0] = pBit0;
            pBits[1] = pBit1;

            output.PackType6(endPoints, pBits, indices);

            return output;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(indicesArray, true);
            ArrayPool<int>.Shared.Return(partitionTableArray, true);
        }
    }
}
