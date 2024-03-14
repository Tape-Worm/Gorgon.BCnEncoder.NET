using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using BCnEncoder.Shared;
using Gorgon.Graphics;

namespace BCnEncoder.Encoder.Bc7;

[StructLayout(LayoutKind.Sequential)]
internal class ClusterIndices4X4
{
    private readonly int[] _indices = new int[16];

    public Span<int> AsSpan => _indices;

    public int this[int x, int y]
    {
        get => AsSpan[x + y * 4];
        set => AsSpan[x + y * 4] = value;
    }

    public int this[int index]
    {
        get => AsSpan[index];
        set => AsSpan[index] = value;
    }

    public int NumClusters
    {
        get
        {
            Span<int> t = AsSpan;
            Span<int> clusters = stackalloc int[16];
            int distinct = 0;
            for (int i = 0; i < 16; i++)
            {
                int cluster = t[i];
                bool found = false;
                for (int j = 0; j < distinct; j++)
                {
                    if (clusters[j] == cluster)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    clusters[distinct] = cluster;
                    ++distinct;
                }
            }
            return distinct;
        }
    }

    /// <summary>
    /// Reduces block down to adjacent cluster indices. For example,
    /// block that contains clusters 5, 16 and 77 will become a block that contains clusters 0, 1 and 2
    /// </summary>
    public ClusterIndices4X4 Reduce(out int numClusters)
    {
        var result = new ClusterIndices4X4();
        numClusters = NumClusters;
        Span<int> mapKey = stackalloc int[numClusters];
        Span<int> indices = AsSpan;
        Span<int> outIndices = result.AsSpan;
        int next = 0;
        for (int i = 0; i < 16; i++)
        {
            int cluster = indices[i];
            bool found = false;
            for (int j = 0; j < next; j++)
            {
                if (mapKey[j] == cluster)
                {
                    found = true;
                    outIndices[i] = j;
                    break;
                }
            }
            if (!found)
            {
                outIndices[i] = next;
                mapKey[next] = cluster;
                ++next;
            }
        }

        return result;
    }
}

internal static class Bc7EncodingHelpers
{
    private static readonly int[] _varPatternRAlpha = new int[] { 1, -1, 1, 0, 0, -1, 0, 0, 0, 0 };
    private static readonly int[] _varPatternRNoAlpha = new int[] { 1, -1, 1, 0, 0, -1, 0, 0 };

    private static readonly int[] _varPatternGAlpha = new int[] { 1, -1, 0, 1, 0, 0, -1, 0, 0, 0 };
    private static readonly int[] _varPatternGNoAlpha = new int[] { 1, -1, 0, 1, 0, 0, -1, 0 };

    private static readonly int[] _varPatternBAlpha = new int[] { 1, -1, 0, 0, 1, 0, 0, -1, 0, 0 };
    private static readonly int[] _varPatternBNoAlpha = new int[] { 1, -1, 0, 0, 1, 0, 0, -1 };

    private static readonly int[] _varPatternAAlpha = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, -1 };
    private static readonly int[] _varPatternANoAlpha = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };

    public static bool TypeHasPBits(Bc7BlockType type) => type switch
    {
        Bc7BlockType.Type0 or Bc7BlockType.Type1 or Bc7BlockType.Type3 or Bc7BlockType.Type6 or Bc7BlockType.Type7 => true,
        _ => false,
    };

    public static bool TypeHasSharedPBits(Bc7BlockType type) => type == Bc7BlockType.Type1;

    /// <summary>
    /// Includes PBit
    /// </summary>
    public static int GetColorComponentPrecisionWithPBit(Bc7BlockType type) => type switch
    {
        Bc7BlockType.Type0 or Bc7BlockType.Type2 or Bc7BlockType.Type4 => 5,
        Bc7BlockType.Type1 or Bc7BlockType.Type5 => 7,
        Bc7BlockType.Type3 or Bc7BlockType.Type6 => 8,
        Bc7BlockType.Type7 => 6,
        _ => 0,
    };

    /// <summary>
    /// Includes PBit
    /// </summary>
    public static int GetAlphaComponentPrecisionWithPBit(Bc7BlockType type) => type switch
    {
        Bc7BlockType.Type4 or Bc7BlockType.Type7 => 6,
        Bc7BlockType.Type5 or Bc7BlockType.Type6 => 8,
        _ => 0,
    };

    /// <summary>
    /// Does not include pBit
    /// </summary>
    public static int GetColorComponentPrecision(Bc7BlockType type) => type switch
    {
        Bc7BlockType.Type0 => 4,
        Bc7BlockType.Type1 => 6,
        Bc7BlockType.Type2 or Bc7BlockType.Type4 or Bc7BlockType.Type7 => 5,
        Bc7BlockType.Type3 or Bc7BlockType.Type5 or Bc7BlockType.Type6 => 7,
        _ => 0,
    };

    /// <summary>
    /// Does not include pBit
    /// </summary>
    public static int GetAlphaComponentPrecision(Bc7BlockType type) => type switch
    {
        Bc7BlockType.Type4 => 6,
        Bc7BlockType.Type5 => 8,
        Bc7BlockType.Type6 => 7,
        Bc7BlockType.Type7 => 5,
        _ => 0,
    };

    public static int GetColorIndexBitCount(Bc7BlockType type, int type4IdxMode = 0)
    {
        switch (type)
        {
            case Bc7BlockType.Type4 when type4IdxMode == 1:
            case Bc7BlockType.Type0:
            case Bc7BlockType.Type1:
                return 3;
            case Bc7BlockType.Type4 when type4IdxMode == 0:
            case Bc7BlockType.Type2:
            case Bc7BlockType.Type3:
            case Bc7BlockType.Type7:
            case Bc7BlockType.Type5:
                return 2;
            case Bc7BlockType.Type6:
                return 4;
        }

        return 0;
    }

    public static int GetAlphaIndexBitCount(Bc7BlockType type, int type4IdxMode = 0)
    {
        switch (type)
        {
            case Bc7BlockType.Type6:
                return 4;
            case Bc7BlockType.Type4 when type4IdxMode == 0:
                return 3;
            case Bc7BlockType.Type4 when type4IdxMode == 1:
            case Bc7BlockType.Type5:
            case Bc7BlockType.Type7:
                return 2;
        }

        return 0;
    }


    public static void ExpandEndpoints(Bc7BlockType type, ColorRgba32[] endpoints, byte[] pBits)
    {
        if (type is Bc7BlockType.Type0 or Bc7BlockType.Type1 or Bc7BlockType.Type3 or Bc7BlockType.Type6 or Bc7BlockType.Type7)
        {
            for (int i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] <<= 1;
            }

            if (type == Bc7BlockType.Type1)
            {
                endpoints[0] |= pBits[0];
                endpoints[1] |= pBits[0];
                endpoints[2] |= pBits[1];
                endpoints[3] |= pBits[1];
            }
            else
            {
                for (int i = 0; i < endpoints.Length; i++)
                {
                    endpoints[i] |= pBits[i];
                }
            }
        }

        int colorPrecision = GetColorComponentPrecisionWithPBit(type);
        int alphaPrecision = GetAlphaComponentPrecisionWithPBit(type);
        for (int i = 0; i < endpoints.Length; i++)
        {
            // ColorComponentPrecision & AlphaComponentPrecision includes pbit
            // left shift endpoint components so that their MSB lies in bit 7
            endpoints[i].r = (byte)(endpoints[i].r << (8 - colorPrecision));
            endpoints[i].g = (byte)(endpoints[i].g << (8 - colorPrecision));
            endpoints[i].b = (byte)(endpoints[i].b << (8 - colorPrecision));
            endpoints[i].a = (byte)(endpoints[i].a << (8 - alphaPrecision));

            // Replicate each component's MSB into the LSBs revealed by the left-shift operation above
            endpoints[i].r = (byte)(endpoints[i].r | (endpoints[i].r >> colorPrecision));
            endpoints[i].g = (byte)(endpoints[i].g | (endpoints[i].g >> colorPrecision));
            endpoints[i].b = (byte)(endpoints[i].b | (endpoints[i].b >> colorPrecision));
            endpoints[i].a = (byte)(endpoints[i].a | (endpoints[i].a >> alphaPrecision));
        }

        //If this mode does not explicitly define the alpha component
        //set alpha equal to 255
        if (type is Bc7BlockType.Type0 or Bc7BlockType.Type1 or Bc7BlockType.Type2 or Bc7BlockType.Type3)
        {
            for (int i = 0; i < endpoints.Length; i++)
            {
                endpoints[i].a = 255;
            }
        }
    }


    public static ColorRgba32 ExpandEndpoint(Bc7BlockType type, ColorRgba32 endpoint, byte pBit)
    {
        if (type is Bc7BlockType.Type0 or Bc7BlockType.Type1 or Bc7BlockType.Type3 or Bc7BlockType.Type6 or Bc7BlockType.Type7)
        {
            endpoint <<= 1;
            endpoint |= pBit;
        }

        int colorPrecision = GetColorComponentPrecisionWithPBit(type);
        int alphaPrecision = GetAlphaComponentPrecisionWithPBit(type);
        endpoint.r = (byte)(endpoint.r << (8 - colorPrecision));
        endpoint.g = (byte)(endpoint.g << (8 - colorPrecision));
        endpoint.b = (byte)(endpoint.b << (8 - colorPrecision));
        endpoint.a = (byte)(endpoint.a << (8 - alphaPrecision));

        // Replicate each component's MSB into the LSBs revealed by the left-shift operation above
        endpoint.r = (byte)(endpoint.r | (endpoint.r >> colorPrecision));
        endpoint.g = (byte)(endpoint.g | (endpoint.g >> colorPrecision));
        endpoint.b = (byte)(endpoint.b | (endpoint.b >> colorPrecision));
        endpoint.a = (byte)(endpoint.a | (endpoint.a >> alphaPrecision));

        //If this mode does not explicitly define the alpha component
        //set alpha equal to 255
        if (type is Bc7BlockType.Type0 or Bc7BlockType.Type1 or Bc7BlockType.Type2 or Bc7BlockType.Type3)
        {
            endpoint.a = 255;
        }

        return endpoint;
    }

    public static int SelectBest2SubsetPartition(ClusterIndices4X4 reducedIndicesBlock, int numDistinctClusters, out int bestError)
    {
        bool first = true;
        bestError = 999;
        int bestPartition = 0;


        int CalculatePartitionError(int partitionIndex)
        {
            int error = 0;
            ReadOnlySpan<int> partitionTable = Bc7Block.Subsets2PartitionTable[partitionIndex];
            Span<int> subset0 = stackalloc int[numDistinctClusters];
            Span<int> subset1 = stackalloc int[numDistinctClusters];
            int max0Idx = 0;
            int max1Idx = 0;

            //Calculate largest cluster index for each subset 
            for (int i = 0; i < 16; i++)
            {
                if (partitionTable[i] == 0)
                {
                    int r = reducedIndicesBlock[i];
                    subset0[r]++;
                    int count = subset0[r];
                    if (count > subset0[max0Idx])
                    {
                        max0Idx = r;
                    }
                }
                else
                {
                    int r = reducedIndicesBlock[i];
                    subset1[r]++;
                    int count = subset1[r];
                    if (count > subset1[max1Idx])
                    {
                        max1Idx = r;
                    }
                }
            }

            // Calculate error by counting as error everything that does not match the largest cluster
            for (int i = 0; i < 16; i++)
            {
                if (partitionTable[i] == 0)
                {
                    if (reducedIndicesBlock[i] != max0Idx)
                    {
                        error++;
                    }
                }
                else
                {
                    if (reducedIndicesBlock[i] != max1Idx)
                    {
                        error++;
                    }
                }
            }

            return error;
        }

        for (int i = 0; i < 64; i++) // Loop through all possible indices
        {
            int error = CalculatePartitionError(i);
            if (first)
            {
                bestError = error;
                bestPartition = i;
                first = false;
            }
            else if (error < bestError)
            {
                bestError = error;
                bestPartition = i;
            }
            // Break early if exact match
            if (bestError == 0)
            {
                break;
            }
        }

        return bestPartition;
    }

    public static int[] Rank2SubsetPartitions(ClusterIndices4X4 reducedIndicesBlock, int numDistinctClusters)
    {
        int[] output = Enumerable.Range(0, 64).ToArray();


        int CalculatePartitionError(int partitionIndex)
        {
            int error = 0;
            ReadOnlySpan<int> partitionTable = Bc7Block.Subsets2PartitionTable[partitionIndex];
            Span<int> subset0 = stackalloc int[numDistinctClusters];
            Span<int> subset1 = stackalloc int[numDistinctClusters];
            int max0Idx = 0;
            int max1Idx = 0;

            //Calculate largest cluster index for each subset 
            for (int i = 0; i < 16; i++)
            {
                if (partitionTable[i] == 0)
                {
                    int r = reducedIndicesBlock[i];
                    subset0[r]++;
                    int count = subset0[r];
                    if (count > subset0[max0Idx])
                    {
                        max0Idx = r;
                    }
                }
                else
                {
                    int r = reducedIndicesBlock[i];
                    subset1[r]++;
                    int count = subset1[r];
                    if (count > subset1[max1Idx])
                    {
                        max1Idx = r;
                    }
                }
            }

            // Calculate error by counting as error everything that does not match the largest cluster
            for (int i = 0; i < 16; i++)
            {
                if (partitionTable[i] == 0)
                {
                    if (reducedIndicesBlock[i] != max0Idx)
                    {
                        error++;
                    }
                }
                else
                {
                    if (reducedIndicesBlock[i] != max1Idx)
                    {
                        error++;
                    }
                }
            }

            return error;
        }

        output = output.OrderBy(CalculatePartitionError).ToArray();

        return output;
    }

    public static int SelectBest3SubsetPartition(ClusterIndices4X4 reducedIndicesBlock, int numDistinctClusters, out int bestError)
    {
        bool first = true;
        bestError = 999;
        int bestPartition = 0;



        int CalculatePartitionError(int partitionIndex)
        {
            int error = 0;
            ReadOnlySpan<int> partitionTable = Bc7Block.Subsets3PartitionTable[partitionIndex];

            Span<int> subset0 = stackalloc int[numDistinctClusters];
            Span<int> subset1 = stackalloc int[numDistinctClusters];
            Span<int> subset2 = stackalloc int[numDistinctClusters];
            int max0Idx = 0;
            int max1Idx = 0;
            int max2Idx = 0;

            //Calculate largest cluster index for each subset 
            for (int i = 0; i < 16; i++)
            {
                if (partitionTable[i] == 0)
                {
                    int r = reducedIndicesBlock[i];
                    subset0[r]++;
                    int count = subset0[r];
                    if (count > subset0[max0Idx])
                    {
                        max0Idx = r;
                    }
                }
                else if (partitionTable[i] == 1)
                {
                    int r = reducedIndicesBlock[i];
                    subset1[r]++;
                    int count = subset1[r];
                    if (count > subset1[max1Idx])
                    {
                        max1Idx = r;
                    }
                }
                else
                {
                    int r = reducedIndicesBlock[i];
                    subset2[r]++;
                    int count = subset2[r];
                    if (count > subset2[max2Idx])
                    {
                        max2Idx = r;
                    }
                }
            }

            // Calculate error by counting as error everything that does not match the largest cluster
            for (int i = 0; i < 16; i++)
            {
                if (partitionTable[i] == 0)
                {
                    if (reducedIndicesBlock[i] != max0Idx)
                    {
                        error++;
                    }
                }
                else if (partitionTable[i] == 1)
                {
                    if (reducedIndicesBlock[i] != max1Idx)
                    {
                        error++;
                    }
                }
                else
                {
                    if (reducedIndicesBlock[i] != max2Idx)
                    {
                        error++;
                    }
                }
            }

            return error;
        }

        for (int i = 0; i < 64; i++) // Loop through all possible indices
        {
            int error = CalculatePartitionError(i);
            if (first)
            {
                bestError = error;
                bestPartition = i;
                first = false;
            }
            else if (error < bestError)
            {
                bestError = error;
                bestPartition = i;
            }
            // Break early if exact match
            if (bestError == 0)
            {
                break;
            }
        }

        return bestPartition;
    }

    public static int[] Rank3SubsetPartitions(ClusterIndices4X4 reducedIndicesBlock, int numDistinctClusters)
    {
        int[] output = Enumerable.Range(0, 64).ToArray();

        int CalculatePartitionError(int partitionIndex)
        {
            int error = 0;
            ReadOnlySpan<int> partitionTable = Bc7Block.Subsets3PartitionTable[partitionIndex];

            Span<int> subset0 = stackalloc int[numDistinctClusters];
            Span<int> subset1 = stackalloc int[numDistinctClusters];
            Span<int> subset2 = stackalloc int[numDistinctClusters];
            int max0Idx = 0;
            int max1Idx = 0;
            int max2Idx = 0;

            //Calculate largest cluster index for each subset 
            for (int i = 0; i < 16; i++)
            {
                if (partitionTable[i] == 0)
                {
                    int r = reducedIndicesBlock[i];
                    subset0[r]++;
                    int count = subset0[r];
                    if (count > subset0[max0Idx])
                    {
                        max0Idx = r;
                    }
                }
                else if (partitionTable[i] == 1)
                {
                    int r = reducedIndicesBlock[i];
                    subset1[r]++;
                    int count = subset1[r];
                    if (count > subset1[max1Idx])
                    {
                        max1Idx = r;
                    }
                }
                else
                {
                    int r = reducedIndicesBlock[i];
                    subset2[r]++;
                    int count = subset2[r];
                    if (count > subset2[max2Idx])
                    {
                        max2Idx = r;
                    }
                }
            }

            // Calculate error by counting as error everything that does not match the largest cluster
            for (int i = 0; i < 16; i++)
            {
                if (partitionTable[i] == 0)
                {
                    if (reducedIndicesBlock[i] != max0Idx)
                    {
                        error++;
                    }
                }
                else if (partitionTable[i] == 1)
                {
                    if (reducedIndicesBlock[i] != max1Idx)
                    {
                        error++;
                    }
                }
                else
                {
                    if (reducedIndicesBlock[i] != max2Idx)
                    {
                        error++;
                    }
                }
            }

            return error;
        }

        output = output.OrderBy(CalculatePartitionError).ToArray();

        return output;
    }


    public static void GetInitialUnscaledEndpoints(RawBlock4X4Rgba32 block, out ColorRgba32 ep0,
        out ColorRgba32 ep1)
    {

        Span<GorgonColor> originalPixels = block.AsSpan;
        PcaVectors.CreateWithAlpha(originalPixels, out Vector4 mean, out Vector4 pa);
        PcaVectors.GetExtremePointsWithAlpha(block.AsSpan, mean, pa, out Vector4 min, out Vector4 max);

        ep0 = new ColorRgba32((byte)(min.X * 255), (byte)(min.Y * 255), (byte)(min.Z * 255), (byte)(min.W * 255));
        ep1 = new ColorRgba32((byte)(max.X * 255), (byte)(max.Y * 255), (byte)(max.Z * 255), (byte)(max.W * 255));
    }

    public static void GetInitialUnscaledEndpointsForSubset(RawBlock4X4Rgba32 block, out ColorRgba32 ep0,
        out ColorRgba32 ep1, ReadOnlySpan<int> partitionTable, int subsetIndex)
    {

        Span<GorgonColor> originalPixels = block.AsSpan;

        int count = 0;
        for (int i = 0; i < 16; i++)
        {
            if (partitionTable[i] == subsetIndex)
            {
                count++;
            }
        }

        Span<GorgonColor> subsetColors = stackalloc GorgonColor[count];
        int next = 0;
        for (int i = 0; i < 16; i++)
        {
            if (partitionTable[i] == subsetIndex)
            {
                subsetColors[next++] = originalPixels[i];
            }
        }

        PcaVectors.CreateWithAlpha(subsetColors, out Vector4 mean, out Vector4 pa);
        PcaVectors.GetExtremePointsWithAlpha(block.AsSpan, mean, pa, out Vector4 min, out Vector4 max);

        ep0 = new ColorRgba32((byte)(min.X * 255), (byte)(min.Y * 255), (byte)(min.Z * 255), (byte)(min.W * 255));
        ep1 = new ColorRgba32((byte)(max.X * 255), (byte)(max.Y * 255), (byte)(max.Z * 255), (byte)(max.W * 255));
    }

    public static ColorRgba32 ScaleDownEndpoint(ColorRgba32 endpoint, Bc7BlockType type, bool ignoreAlpha, out byte pBit)
    {
        int colorPrecision = GetColorComponentPrecisionWithPBit(type);
        int alphaPrecision = GetAlphaComponentPrecisionWithPBit(type);

        byte r = (byte)(endpoint.r >> (8 - colorPrecision));
        byte g = (byte)(endpoint.g >> (8 - colorPrecision));
        byte b = (byte)(endpoint.b >> (8 - colorPrecision));
        byte a = (byte)(endpoint.a >> (8 - alphaPrecision));

        if (TypeHasPBits(type))
        {
            int pBitVotingMask = (1 << (8 - colorPrecision + 1)) - 1;
            float pBitVotes = 0;
            pBitVotes += endpoint.r & pBitVotingMask;
            pBitVotes += endpoint.g & pBitVotingMask;
            pBitVotes += endpoint.b & pBitVotingMask;
            pBitVotes /= 3;

            if (pBitVotes >= (pBitVotingMask / 2f))
            {
                pBit = 1;
            }
            else
            {
                pBit = 0;
            }

            r >>= 1;
            g >>= 1;
            b >>= 1;
            a >>= 1;
        }
        else
        {
            pBit = 0;
        }

        return ignoreAlpha ? new ColorRgba32(r, g, b, 0) : new ColorRgba32(r, g, b, a);
    }

    public static ColorRgba32 InterpolateColor(ColorRgba32 endPointStart, ColorRgba32 endPointEnd,
        int colorIndex, int alphaIndex, int colorBitCount, int alphaBitCount)
    {
        static byte InterpolateByte(byte e0, byte e1, int index, int indexPrecision)
        {
            if (indexPrecision == 0)
            {
                return e0;
            }

            ReadOnlySpan<byte> aWeights2 = Bc7Block.ColorInterpolationWeights2;
            ReadOnlySpan<byte> aWeights3 = Bc7Block.ColorInterpolationWeights3;
            ReadOnlySpan<byte> aWeights4 = Bc7Block.ColorInterpolationWeights4;

            return indexPrecision switch
            {
                2 => (byte)(((64 - aWeights2[index]) * (e0) + aWeights2[index] * (e1) + 32) >> 6),
                3 => (byte)(((64 - aWeights3[index]) * (e0) + aWeights3[index] * (e1) + 32) >> 6),
                _ => (byte)(((64 - aWeights4[index]) * (e0) + aWeights4[index] * (e1) + 32) >> 6),
            };
        }

        var result = new ColorRgba32(
            InterpolateByte(endPointStart.r, endPointEnd.r, colorIndex, colorBitCount),
            InterpolateByte(endPointStart.g, endPointEnd.g, colorIndex, colorBitCount),
            InterpolateByte(endPointStart.b, endPointEnd.b, colorIndex, colorBitCount),
            InterpolateByte(endPointStart.a, endPointEnd.a, alphaIndex, alphaBitCount)
        );

        return result;
    }

    public static void ClampEndpoint(ref ColorRgba32 endpoint, byte colorMax, byte alphaMax)
    {
        if (endpoint.r > colorMax)
        {
            endpoint.r = colorMax;
        }

        if (endpoint.g > colorMax)
        {
            endpoint.g = colorMax;
        }

        if (endpoint.b > colorMax)
        {
            endpoint.b = colorMax;
        }

        if (endpoint.a > alphaMax)
        {
            endpoint.a = alphaMax;
        }
    }

    private static int FindClosestColorIndex(ColorYCbCrAlpha color, ReadOnlySpan<ColorYCbCrAlpha> colors, out float bestError)
    {
        bestError = color.CalcDistWeighted(colors[0], 4, 2);
        int bestIndex = 0;
        for (int i = 1; i < colors.Length; i++)
        {
            float error = color.CalcDistWeighted(colors[i], 4, 2);
            if (error < bestError)
            {
                bestIndex = i;
                bestError = error;
            }
        }
        return bestIndex;
    }

    private static int FindClosestColorIndex(ColorYCbCr color, ReadOnlySpan<ColorYCbCr> colors, out float bestError)
    {
        bestError = color.CalcDistWeighted(colors[0], 4);
        int bestIndex = 0;
        for (int i = 1; i < colors.Length; i++)
        {
            float error = color.CalcDistWeighted(colors[i], 4);
            if (error < bestError)
            {
                bestIndex = i;
                bestError = error;
            }
            if (bestError == 0)
            {
                break;
            }
        }
        return bestIndex;
    }

    private static int FindClosestAlphaIndex(byte alpha, ReadOnlySpan<byte> alphas, out float bestError)
    {
        bestError = (alpha - alphas[0]) * (alpha - alphas[0]);
        int bestIndex = 0;
        for (int i = 1; i < alphas.Length; i++)
        {
            float error = (alpha - alphas[i]) * (alpha - alphas[i]);
            if (error < bestError)
            {
                bestIndex = i;
                bestError = error;
            }

            if (bestError == 0)
            {
                break;
            }
        }
        return bestIndex;
    }


    private static float TrySubsetEndpoints(Bc7BlockType type, RawBlock4X4Rgba32 raw, ColorRgba32 ep0, ColorRgba32 ep1,
        ReadOnlySpan<int> partitionTable, int subsetIndex, int type4IdxMode)
    {
        int colorIndexPrecision = GetColorIndexBitCount(type, type4IdxMode);
        int alphaIndexPrecision = GetAlphaIndexBitCount(type, type4IdxMode);

        if (type is Bc7BlockType.Type4 or Bc7BlockType.Type5)
        { //separate indices for color and alpha
            Span<ColorYCbCr> colors = stackalloc ColorYCbCr[1 << colorIndexPrecision];
            Span<byte> alphas = stackalloc byte[1 << alphaIndexPrecision];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new ColorYCbCr(InterpolateColor(ep0, ep1, i,
                    0, colorIndexPrecision, 0));
            }

            for (int i = 0; i < alphas.Length; i++)
            {
                alphas[i] = InterpolateColor(ep0, ep1, 0,
                    i, 0, alphaIndexPrecision).a;
            }

            Span<GorgonColor> pixels = raw.AsSpan;
            float error = 0;

            for (int i = 0; i < 16; i++)
            {
                var pixelColor = new ColorYCbCr(pixels[i]);

                FindClosestColorIndex(pixelColor, colors, out float ce);
                FindClosestAlphaIndex((byte)(pixels[i].Alpha * 255.0f), alphas, out float ae);

                error += ce + ae;
            }

            return error / 16;
        }
        else
        {
            Span<ColorYCbCrAlpha> colors = stackalloc ColorYCbCrAlpha[1 << colorIndexPrecision];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new ColorYCbCrAlpha(InterpolateColor(ep0, ep1, i,
                    i, colorIndexPrecision, alphaIndexPrecision));
            }

            Span<GorgonColor> pixels = raw.AsSpan;
            float error = 0;
            float count = 0;

            for (int i = 0; i < 16; i++)
            {
                if (partitionTable[i] == subsetIndex)
                {
                    var pixelColor = new ColorYCbCrAlpha(pixels[i]);

                    FindClosestColorIndex(pixelColor, colors, out float e);
                    error += e * e;
                    count++;
                }
            }

            error /= count;
            return error;
        }

    }

    public static void FillSubsetIndices(Bc7BlockType type, RawBlock4X4Rgba32 raw, ColorRgba32 ep0, ColorRgba32 ep1, ReadOnlySpan<int> partitionTable, int subsetIndex,
        Span<byte> indicesToFill)
    {
        int colorIndexPrecision = GetColorIndexBitCount(type);
        int alphaIndexPrecision = GetAlphaIndexBitCount(type);

        if (type is Bc7BlockType.Type4 or Bc7BlockType.Type5)
        { //separate indices for color and alpha
            throw new ArgumentException(null, nameof(type));
        }
        else
        {
            Span<ColorYCbCrAlpha> colors = stackalloc ColorYCbCrAlpha[1 << colorIndexPrecision];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new ColorYCbCrAlpha(InterpolateColor(ep0, ep1, i,
                    i, colorIndexPrecision, alphaIndexPrecision));
            }

            Span<GorgonColor> pixels = raw.AsSpan;

            for (int i = 0; i < 16; i++)
            {
                if (partitionTable[i] == subsetIndex)
                {
                    var pixelColor = new ColorYCbCrAlpha(pixels[i]);

                    int index = FindClosestColorIndex(pixelColor, colors, out float e);
                    indicesToFill[i] = (byte)index;
                }
            }
        }
    }

    /// <summary>
    /// Used for Modes 4 and 5
    /// </summary>
    public static void FillAlphaColorIndices(Bc7BlockType type, RawBlock4X4Rgba32 raw, ColorRgba32 ep0, ColorRgba32 ep1,
        Span<byte> colorIndicesToFill, Span<byte> alphaIndicesToFill, int idxMode = 0)
    {
        int colorIndexPrecision = GetColorIndexBitCount(type, idxMode);
        int alphaIndexPrecision = GetAlphaIndexBitCount(type, idxMode);

        switch (type)
        {
            case Bc7BlockType.Type4:
            case Bc7BlockType.Type5:
                Span<ColorYCbCr> colors = stackalloc ColorYCbCr[1 << colorIndexPrecision];
                Span<byte> alphas = stackalloc byte[1 << alphaIndexPrecision];

                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = new ColorYCbCr(InterpolateColor(ep0, ep1, i,
                        0, colorIndexPrecision, 0));
                }

                for (int i = 0; i < alphas.Length; i++)
                {
                    alphas[i] = InterpolateColor(ep0, ep1, 0,
                        i, 0, alphaIndexPrecision).a;
                }

                Span<GorgonColor> pixels = raw.AsSpan;

                for (int i = 0; i < 16; i++)
                {
                    GorgonColor color = pixels[i];
                    var pixelColor = new ColorYCbCr(color);

                    int index = FindClosestColorIndex(pixelColor, colors, out float e);
                    colorIndicesToFill[i] = (byte)index;

                    index = FindClosestAlphaIndex((byte)(color.Alpha * 255.0f), alphas, out float _);
                    alphaIndicesToFill[i] = (byte)index;
                }
                break;
            default:
                throw new ArgumentException(null, nameof(type));
        }
    }

    public static void OptimizeSubsetEndpointsWithPBit(Bc7BlockType type, RawBlock4X4Rgba32 raw, ref ColorRgba32 ep0, ref ColorRgba32 ep1, ref byte pBit0, ref byte pBit1,
        int variation, ReadOnlySpan<int> partitionTable, int subsetIndex, bool variatePBits, bool variateAlpha, int type4IdxMode = 0)
    {

        byte colorMax = (byte)((1 << GetColorComponentPrecision(type)) - 1);
        byte alphaMax = (byte)((1 << GetAlphaComponentPrecision(type)) - 1);

        float bestError = TrySubsetEndpoints(type, raw,
            ExpandEndpoint(type, ep0, pBit0),
            ExpandEndpoint(type, ep1, pBit1), partitionTable, subsetIndex, type4IdxMode
        );

        ReadOnlySpan<int> varPatternR = variateAlpha
            ? _varPatternRAlpha
            : _varPatternRNoAlpha;
        ReadOnlySpan<int> varPatternG = variateAlpha
            ? _varPatternGAlpha
            : _varPatternGNoAlpha;
        ReadOnlySpan<int> varPatternB = variateAlpha
            ? _varPatternBAlpha
            : _varPatternBNoAlpha;
        ReadOnlySpan<int> varPatternA = variateAlpha
            ? _varPatternAAlpha
            : _varPatternANoAlpha;


        while (variation > 0)
        {
            bool foundBetter = false;

            for (int i = 0; i < varPatternR.Length; i++)
            {
                var testEndPoint0 = new ColorRgba32(
                    (byte)(ep0.r - variation * varPatternR[i]),
                    (byte)(ep0.g - variation * varPatternG[i]),
                    (byte)(ep0.b - variation * varPatternB[i]),
                    (byte)(ep0.a - variation * varPatternA[i])
                );

                var testEndPoint1 = new ColorRgba32(
                    (byte)(ep1.r + variation * varPatternR[i]),
                    (byte)(ep1.g + variation * varPatternG[i]),
                    (byte)(ep1.b + variation * varPatternB[i]),
                    (byte)(ep1.a + variation * varPatternA[i])
                );
                ClampEndpoint(ref testEndPoint0, colorMax, alphaMax);
                ClampEndpoint(ref testEndPoint1, colorMax, alphaMax);

                float error = TrySubsetEndpoints(type, raw,
                    ExpandEndpoint(type, testEndPoint0, pBit0),
                    ExpandEndpoint(type, testEndPoint1, pBit1), partitionTable, subsetIndex, type4IdxMode
                );
                if (error < bestError)
                {
                    bestError = error;
                    ep0 = testEndPoint0;
                    ep1 = testEndPoint1;
                    foundBetter = true;
                }
            }

            for (int i = 0; i < varPatternR.Length; i++)
            {
                var testEndPoint0 = new ColorRgba32(
                    (byte)(ep0.r + variation * varPatternR[i]),
                    (byte)(ep0.g + variation * varPatternG[i]),
                    (byte)(ep0.b + variation * varPatternB[i]),
                    (byte)(ep0.a + variation * varPatternA[i])
                    );
                ClampEndpoint(ref testEndPoint0, colorMax, alphaMax);

                float error = TrySubsetEndpoints(type, raw,
                    ExpandEndpoint(type, testEndPoint0, pBit0),
                    ExpandEndpoint(type, ep1, pBit1), partitionTable, subsetIndex, type4IdxMode
                );
                if (error < bestError)
                {
                    bestError = error;
                    ep0 = testEndPoint0;
                    foundBetter = true;
                }
            }

            for (int i = 0; i < varPatternR.Length; i++)
            {
                var testEndPoint1 = new ColorRgba32(
                    (byte)(ep1.r + variation * varPatternR[i]),
                    (byte)(ep1.g + variation * varPatternG[i]),
                    (byte)(ep1.b + variation * varPatternB[i]),
                    (byte)(ep1.a + variation * varPatternA[i])
                );
                ClampEndpoint(ref testEndPoint1, colorMax, alphaMax);

                float error = TrySubsetEndpoints(type, raw,
                    ExpandEndpoint(type, ep0, pBit0),
                    ExpandEndpoint(type, testEndPoint1, pBit1), partitionTable, subsetIndex, type4IdxMode
                );
                if (error < bestError)
                {
                    bestError = error;
                    ep1 = testEndPoint1;
                    foundBetter = true;
                }
            }

            if (variatePBits)
            {
                {
                    byte testPBit0 = pBit0 == 0 ? (byte)1 : (byte)0;
                    float error = TrySubsetEndpoints(type, raw,
                        ExpandEndpoint(type, ep0, testPBit0),
                        ExpandEndpoint(type, ep1, pBit1), partitionTable, subsetIndex, type4IdxMode
                    );
                    if (error < bestError)
                    {
                        bestError = error;
                        pBit0 = testPBit0;
                        foundBetter = true;
                    }
                }
                {
                    byte testPBit1 = pBit1 == 0 ? (byte)1 : (byte)0;
                    float error = TrySubsetEndpoints(type, raw,
                        ExpandEndpoint(type, ep0, pBit0),
                        ExpandEndpoint(type, ep1, testPBit1), partitionTable, subsetIndex, type4IdxMode
                    );
                    if (error < bestError)
                    {
                        bestError = error;
                        pBit1 = testPBit1;
                        foundBetter = true;
                    }
                }
            }
            if (!foundBetter)
            {
                variation--;
            }
        }
    }

    public static RawBlock4X4Rgba32 RotateBlockColors(RawBlock4X4Rgba32 block, int rotation)
    {
        if (rotation == 0)
        {
            return block;
        }

        var rotated = new RawBlock4X4Rgba32();
        Span<GorgonColor> pixels = block.AsSpan;
        Span<GorgonColor> output = rotated.AsSpan;
        for (int i = 0; i < 16; i++)
        {
            GorgonColor color = pixels[i];

            switch (rotation)
            {
                case 1:
                    output[i] = new GorgonColor(color.Alpha, color.Green, color.Blue, color.Red);
                    break;
                case 2:
                    output[i] = new GorgonColor(color.Red, color.Alpha, color.Blue, color.Green);
                    break;
                case 3:
                    output[i] = new GorgonColor(color.Red, color.Green, color.Alpha, color.Blue);
                    break;
            }
        }

        return rotated;
    }

}
