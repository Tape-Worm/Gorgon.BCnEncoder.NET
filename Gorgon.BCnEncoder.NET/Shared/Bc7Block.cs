using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Gorgon.Graphics;

namespace Gorgon.BCnEncoder.Shared;

internal enum Bc7BlockType : uint
{
    Type0,
    Type1,
    Type2,
    Type3,
    Type4,
    Type5,
    Type6,
    Type7,
    Type8Reserved
}

internal struct Bc7Block
{
    public ulong lowBits;
    public ulong highBits;

    public static ReadOnlySpan<byte> ColorInterpolationWeights2 => [0, 21, 43, 64];
    public static ReadOnlySpan<byte> ColorInterpolationWeights3 => [0, 9, 18, 27, 37, 46, 55, 64];
    public static ReadOnlySpan<byte> ColorInterpolationWeights4 => [0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64];


    public static readonly int[][] Subsets2PartitionTable = [
        [0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1],
        [0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1],
        [0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1],
        [0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 1, 1, 1],
        [0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 1],
        [0, 0, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1],
        [0, 0, 0, 1, 0, 0, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1],
        [0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 1, 1, 1],
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1],
        [0, 0, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1],
        [0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1],
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1],
        [0, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1],
        [0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1],
        [0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1],
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1],
        [0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1],
        [0, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0],
        [0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0],
        [0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0],
        [0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0],
        [0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0, 1, 1, 1, 0],
        [0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0],
        [0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0, 1],
        [0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0],
        [0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0],
        [0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0],
        [0, 0, 1, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 1, 0, 0],
        [0, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0],
        [0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0],
        [0, 1, 1, 1, 0, 0, 0, 1, 1, 0, 0, 0, 1, 1, 1, 0],
        [0, 0, 1, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0],
        [0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1],
        [0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1],
        [0, 1, 0, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 0, 1, 0],
        [0, 0, 1, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 0, 0],
        [0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 0, 0],
        [0, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0],
        [0, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0, 1],
        [0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 1],
        [0, 1, 1, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 1, 0],
        [0, 0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0],
        [0, 0, 1, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 1, 0, 0],
        [0, 0, 1, 1, 1, 0, 1, 1, 1, 1, 0, 1, 1, 1, 0, 0],
        [0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 1, 1, 0],
        [0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 1, 1],
        [0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1],
        [0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0],
        [0, 1, 0, 0, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0],
        [0, 0, 1, 0, 0, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0],
        [0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 0, 0, 1, 0],
        [0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 0, 0, 1, 0, 0],
        [0, 1, 1, 0, 1, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 1],
        [0, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 0, 1, 0, 0, 1],
        [0, 1, 1, 0, 0, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 0],
        [0, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0, 1, 1, 0],
        [0, 1, 1, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 0, 0, 1],
        [0, 1, 1, 0, 0, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 1],
        [0, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1],
        [0, 0, 0, 1, 1, 0, 0, 0, 1, 1, 1, 0, 0, 1, 1, 1],
        [0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1],
        [0, 0, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0],
        [0, 0, 1, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0],
        [0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1]
    ];

    public static readonly int[][] Subsets3PartitionTable = [
        [0, 0, 1, 1, 0, 0, 1, 1, 0, 2, 2, 1, 2, 2, 2, 2],
        [0, 0, 0, 1, 0, 0, 1, 1, 2, 2, 1, 1, 2, 2, 2, 1],
        [0, 0, 0, 0, 2, 0, 0, 1, 2, 2, 1, 1, 2, 2, 1, 1],
        [0, 2, 2, 2, 0, 0, 2, 2, 0, 0, 1, 1, 0, 1, 1, 1],
        [0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 2, 2],
        [0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 2, 2, 0, 0, 2, 2],
        [0, 0, 2, 2, 0, 0, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1],
        [0, 0, 1, 1, 0, 0, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1],
        [0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2],
        [0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2],
        [0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2],
        [0, 0, 1, 2, 0, 0, 1, 2, 0, 0, 1, 2, 0, 0, 1, 2],
        [0, 1, 1, 2, 0, 1, 1, 2, 0, 1, 1, 2, 0, 1, 1, 2],
        [0, 1, 2, 2, 0, 1, 2, 2, 0, 1, 2, 2, 0, 1, 2, 2],
        [0, 0, 1, 1, 0, 1, 1, 2, 1, 1, 2, 2, 1, 2, 2, 2],
        [0, 0, 1, 1, 2, 0, 0, 1, 2, 2, 0, 0, 2, 2, 2, 0],
        [0, 0, 0, 1, 0, 0, 1, 1, 0, 1, 1, 2, 1, 1, 2, 2],
        [0, 1, 1, 1, 0, 0, 1, 1, 2, 0, 0, 1, 2, 2, 0, 0],
        [0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 2, 2],
        [0, 0, 2, 2, 0, 0, 2, 2, 0, 0, 2, 2, 1, 1, 1, 1],
        [0, 1, 1, 1, 0, 1, 1, 1, 0, 2, 2, 2, 0, 2, 2, 2],
        [0, 0, 0, 1, 0, 0, 0, 1, 2, 2, 2, 1, 2, 2, 2, 1],
        [0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 2, 2, 0, 1, 2, 2],
        [0, 0, 0, 0, 1, 1, 0, 0, 2, 2, 1, 0, 2, 2, 1, 0],
        [0, 1, 2, 2, 0, 1, 2, 2, 0, 0, 1, 1, 0, 0, 0, 0],
        [0, 0, 1, 2, 0, 0, 1, 2, 1, 1, 2, 2, 2, 2, 2, 2],
        [0, 1, 1, 0, 1, 2, 2, 1, 1, 2, 2, 1, 0, 1, 1, 0],
        [0, 0, 0, 0, 0, 1, 1, 0, 1, 2, 2, 1, 1, 2, 2, 1],
        [0, 0, 2, 2, 1, 1, 0, 2, 1, 1, 0, 2, 0, 0, 2, 2],
        [0, 1, 1, 0, 0, 1, 1, 0, 2, 0, 0, 2, 2, 2, 2, 2],
        [0, 0, 1, 1, 0, 1, 2, 2, 0, 1, 2, 2, 0, 0, 1, 1],
        [0, 0, 0, 0, 2, 0, 0, 0, 2, 2, 1, 1, 2, 2, 2, 1],
        [0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2, 2, 1, 2, 2, 2],
        [0, 2, 2, 2, 0, 0, 2, 2, 0, 0, 1, 2, 0, 0, 1, 1],
        [0, 0, 1, 1, 0, 0, 1, 2, 0, 0, 2, 2, 0, 2, 2, 2],
        [0, 1, 2, 0, 0, 1, 2, 0, 0, 1, 2, 0, 0, 1, 2, 0],
        [0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 0, 0, 0, 0],
        [0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0],
        [0, 1, 2, 0, 2, 0, 1, 2, 1, 2, 0, 1, 0, 1, 2, 0],
        [0, 0, 1, 1, 2, 2, 0, 0, 1, 1, 2, 2, 0, 0, 1, 1],
        [0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 0, 0, 0, 0, 1, 1],
        [0, 1, 0, 1, 0, 1, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2],
        [0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 2, 1, 2, 1, 2, 1],
        [0, 0, 2, 2, 1, 1, 2, 2, 0, 0, 2, 2, 1, 1, 2, 2],
        [0, 0, 2, 2, 0, 0, 1, 1, 0, 0, 2, 2, 0, 0, 1, 1],
        [0, 2, 2, 0, 1, 2, 2, 1, 0, 2, 2, 0, 1, 2, 2, 1],
        [0, 1, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 0, 1, 0, 1],
        [0, 0, 0, 0, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1],
        [0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 2, 2, 2, 2],
        [0, 2, 2, 2, 0, 1, 1, 1, 0, 2, 2, 2, 0, 1, 1, 1],
        [0, 0, 0, 2, 1, 1, 1, 2, 0, 0, 0, 2, 1, 1, 1, 2],
        [0, 0, 0, 0, 2, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 2],
        [0, 2, 2, 2, 0, 1, 1, 1, 0, 1, 1, 1, 0, 2, 2, 2],
        [0, 0, 0, 2, 1, 1, 1, 2, 1, 1, 1, 2, 0, 0, 0, 2],
        [0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 2, 2, 2, 2],
        [0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2, 2, 1, 1, 2],
        [0, 1, 1, 0, 0, 1, 1, 0, 2, 2, 2, 2, 2, 2, 2, 2],
        [0, 0, 2, 2, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 2, 2],
        [0, 0, 2, 2, 1, 1, 2, 2, 1, 1, 2, 2, 0, 0, 2, 2],
        [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 1, 2],
        [0, 0, 0, 2, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 1],
        [0, 2, 2, 2, 1, 2, 2, 2, 0, 2, 2, 2, 1, 2, 2, 2],
        [0, 1, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2],
        [0, 1, 1, 1, 2, 0, 1, 1, 2, 2, 0, 1, 2, 2, 2, 0],
    ];

    public static readonly int[] Subsets2AnchorIndices = [
        15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15,
        15, 2, 8, 2, 2, 8, 8, 15,
        2, 8, 2, 2, 8, 8, 2, 2,
        15, 15, 6, 8, 2, 8, 15, 15,
        2, 8, 2, 2, 2, 15, 15, 6,
        6, 2, 6, 8, 15, 15, 2, 2,
        15, 15, 15, 15, 15, 2, 2, 15
    ];

    public static readonly int[] Subsets3AnchorIndices2 = [
        3, 3, 15, 15, 8, 3, 15, 15,
        8, 8, 6, 6, 6, 5, 3, 3,
        3, 3, 8, 15, 3, 3, 6, 10,
        5, 8, 8, 6, 8, 5, 15, 15,
        8, 15, 3, 5, 6, 10, 8, 15,
        15, 3, 15, 5, 15, 15, 15, 15,
        3, 15, 5, 5, 5, 8, 5, 10,
        5, 10, 8, 13, 15, 12, 3, 3
    ];

    public static readonly int[] Subsets3AnchorIndices3 = [
        15, 8, 8, 3, 15, 15, 3, 8,
        15, 15, 15, 15, 15, 15, 15, 8,
        15, 8, 15, 3, 15, 8, 15, 8,
        3, 15, 6, 10, 15, 15, 10, 8,
        15, 3, 15, 10, 10, 8, 9, 10,
        6, 15, 8, 15, 3, 6, 6, 8,
        15, 3, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 3, 15, 15, 8
    ];

    private Bc7BlockType Type
    {
        get
        {
            for (int i = 0; i < 8; i++)
            {
                ulong mask = (ulong)(1 << i);
                if ((lowBits & mask) == mask)
                {
                    return (Bc7BlockType)i;
                }
            }

            return Bc7BlockType.Type8Reserved;
        }
    }

    private int NumSubsets => Type switch
    {
        Bc7BlockType.Type0 or Bc7BlockType.Type2 => 3,
        Bc7BlockType.Type1 or Bc7BlockType.Type3 or Bc7BlockType.Type7 => 2,
        _ => 1,
    };

    private bool HasSubsets => Type switch
    {
        Bc7BlockType.Type0 or Bc7BlockType.Type1 or Bc7BlockType.Type2 or Bc7BlockType.Type3 or Bc7BlockType.Type7 => true,
        _ => false,
    };

    private int PartitionSetId => Type switch
    {
        Bc7BlockType.Type0 => ByteHelper.Extract4(lowBits, 1),
        Bc7BlockType.Type1 => ByteHelper.Extract6(lowBits, 2),
        Bc7BlockType.Type2 => ByteHelper.Extract6(lowBits, 3),
        Bc7BlockType.Type3 => ByteHelper.Extract6(lowBits, 4),
        Bc7BlockType.Type7 => ByteHelper.Extract6(lowBits, 8),
        _ => -1,
    };

    private byte RotationBits => Type switch
    {
        Bc7BlockType.Type4 => ByteHelper.Extract2(lowBits, 5),
        Bc7BlockType.Type5 => ByteHelper.Extract2(lowBits, 6),
        _ => 0,
    };

    /// <summary>
    /// Bitcount of color component including Pbit
    /// </summary>
    private int ColorComponentPrecision => Type switch
    {
        Bc7BlockType.Type0 or Bc7BlockType.Type2 or Bc7BlockType.Type4 => 5,
        Bc7BlockType.Type1 or Bc7BlockType.Type5 => 7,
        Bc7BlockType.Type3 or Bc7BlockType.Type6 => 8,
        Bc7BlockType.Type7 => 6,
        _ => 0,
    };

    /// <summary>
    /// Bitcount of alpha component including Pbit
    /// </summary>
    private int AlphaComponentPrecision => Type switch
    {
        Bc7BlockType.Type4 or Bc7BlockType.Type7 => 6,
        Bc7BlockType.Type5 or Bc7BlockType.Type6 => 8,
        _ => 0,
    };

    private bool HasRotationBits => Type switch
    {
        Bc7BlockType.Type4 or Bc7BlockType.Type5 => true,
        _ => false,
    };

    private bool HasPBits => Type switch
    {
        Bc7BlockType.Type0 or Bc7BlockType.Type1 or Bc7BlockType.Type3 or Bc7BlockType.Type6 or Bc7BlockType.Type7 => true,
        _ => false,
    };

    private bool HasAlpha => Type switch
    {
        Bc7BlockType.Type4 or Bc7BlockType.Type5 or Bc7BlockType.Type6 or Bc7BlockType.Type7 => true,
        _ => false,
    };

    /// <summary>
    /// Type 4 has 2-bit and 3-bit indices. If index mode is 0, color components will use 2-bit indices and alpha will use 3-bit indices.
    /// In index mode 1, color will use 3-bit indices and alpha will use 2-bit indices.
    /// </summary>
    private int Type4IndexMode => Type == Bc7BlockType.Type4 ? ByteHelper.Extract1(lowBits, 7) : 0;

    private int ColorIndexBitCount
    {
        get
        {
            switch (Type)
            {
                case Bc7BlockType.Type4 when Type4IndexMode == 1:
                case Bc7BlockType.Type0:
                case Bc7BlockType.Type1:
                    return 3;
                case Bc7BlockType.Type4 when Type4IndexMode == 0:
                case Bc7BlockType.Type2:
                case Bc7BlockType.Type3:
                case Bc7BlockType.Type5:
                case Bc7BlockType.Type7:
                    return 2;
                case Bc7BlockType.Type6:
                    return 4;

            }
            return 0;
        }
    }

    private int AlphaIndexBitCount
    {
        get
        {
            switch (Type)
            {
                case Bc7BlockType.Type4 when Type4IndexMode == 0:
                    return 3;
                case Bc7BlockType.Type4 when Type4IndexMode == 1:
                case Bc7BlockType.Type5:
                case Bc7BlockType.Type7:
                    return 2;
                case Bc7BlockType.Type6:
                    return 4;
            }
            return 0;
        }
    }

    private int GetRawEndPointCount() => Type switch
    {
        Bc7BlockType.Type2 or Bc7BlockType.Type0 => 6,
        Bc7BlockType.Type7 or Bc7BlockType.Type3 or Bc7BlockType.Type1 => 4,
        Bc7BlockType.Type6 or Bc7BlockType.Type5 or Bc7BlockType.Type4 => 2,
        _ => -1,
    };

    private void ExtractRawEndpoints(Span<ColorRgba32> endpoints)
    {
        switch (Type)
        {
            case Bc7BlockType.Type0:
                endpoints[0].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 5, 4);
                endpoints[1].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 9, 4);
                endpoints[2].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 13, 4);
                endpoints[3].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 17, 4);
                endpoints[4].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 21, 4);
                endpoints[5].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 25, 4);

                endpoints[0].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 29, 4);
                endpoints[1].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 33, 4);
                endpoints[2].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 37, 4);
                endpoints[3].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 41, 4);
                endpoints[4].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 45, 4);
                endpoints[5].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 49, 4);

                endpoints[0].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 53, 4);
                endpoints[1].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 57, 4);
                endpoints[2].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 61, 4);
                endpoints[3].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 65, 4);
                endpoints[4].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 69, 4);
                endpoints[5].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 73, 4);
                break;
            case Bc7BlockType.Type1:
                endpoints[0].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 8, 6);
                endpoints[1].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 14, 6);
                endpoints[2].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 20, 6);
                endpoints[3].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 26, 6);

                endpoints[0].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 32, 6);
                endpoints[1].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 38, 6);
                endpoints[2].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 44, 6);
                endpoints[3].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 50, 6);

                endpoints[0].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 56, 6);
                endpoints[1].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 62, 6);
                endpoints[2].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 68, 6);
                endpoints[3].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 74, 6);
                break;
            case Bc7BlockType.Type2:
                endpoints[0].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 9, 5);
                endpoints[1].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 14, 5);
                endpoints[2].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 19, 5);
                endpoints[3].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 24, 5);
                endpoints[4].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 29, 5);
                endpoints[5].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 34, 5);

                endpoints[0].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 39, 5);
                endpoints[1].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 44, 5);
                endpoints[2].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 49, 5);
                endpoints[3].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 54, 5);
                endpoints[4].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 59, 5);
                endpoints[5].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 64, 5);

                endpoints[0].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 69, 5);
                endpoints[1].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 74, 5);
                endpoints[2].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 79, 5);
                endpoints[3].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 84, 5);
                endpoints[4].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 89, 5);
                endpoints[5].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 94, 5);
                break;
            case Bc7BlockType.Type3:
                endpoints[0].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 10, 7);
                endpoints[1].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 17, 7);
                endpoints[2].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 24, 7);
                endpoints[3].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 31, 7);

                endpoints[0].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 38, 7);
                endpoints[1].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 45, 7);
                endpoints[2].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 52, 7);
                endpoints[3].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 59, 7);

                endpoints[0].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 66, 7);
                endpoints[1].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 73, 7);
                endpoints[2].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 80, 7);
                endpoints[3].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 87, 7);
                break;
            case Bc7BlockType.Type4:
                endpoints[0].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 8, 5);
                endpoints[1].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 13, 5);

                endpoints[0].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 18, 5);
                endpoints[1].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 23, 5);

                endpoints[0].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 28, 5);
                endpoints[1].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 33, 5);

                endpoints[0].a = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 38, 6);
                endpoints[1].a = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 44, 6);
                break;
            case Bc7BlockType.Type5:
                endpoints[0].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 8, 7);
                endpoints[1].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 15, 7);

                endpoints[0].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 22, 7);
                endpoints[1].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 29, 7);

                endpoints[0].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 36, 7);
                endpoints[1].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 43, 7);

                endpoints[0].a = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 50, 8);
                endpoints[1].a = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 58, 8);
                break;
            case Bc7BlockType.Type6:
                endpoints[0].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 7, 7);
                endpoints[1].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 14, 7);

                endpoints[0].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 21, 7);
                endpoints[1].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 28, 7);

                endpoints[0].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 35, 7);
                endpoints[1].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 42, 7);

                endpoints[0].a = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 49, 7);
                endpoints[1].a = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 56, 7);
                break;
            case Bc7BlockType.Type7:
                endpoints[0].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 14, 5);
                endpoints[1].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 19, 5);
                endpoints[2].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 24, 5);
                endpoints[3].r = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 29, 5);

                endpoints[0].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 34, 5);
                endpoints[1].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 39, 5);
                endpoints[2].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 44, 5);
                endpoints[3].g = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 49, 5);

                endpoints[0].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 54, 5);
                endpoints[1].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 59, 5);
                endpoints[2].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 64, 5);
                endpoints[3].b = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 69, 5);

                endpoints[0].a = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 74, 5);
                endpoints[1].a = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 79, 5);
                endpoints[2].a = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 84, 5);
                endpoints[3].a = (byte)ByteHelper.ExtractFrom128(lowBits, highBits, 89, 5);
                break;
            default:
                throw new InvalidDataException();
        }
    }

    private void ExtractPBitArray(Span<byte> pBitArray)
    {
        switch (Type)
        {
            case Bc7BlockType.Type0:
                pBitArray[0] = ByteHelper.Extract1(highBits, 77 - 64);
                pBitArray[1] = ByteHelper.Extract1(highBits, 78 - 64);
                pBitArray[2] = ByteHelper.Extract1(highBits, 79 - 64);
                pBitArray[3] = ByteHelper.Extract1(highBits, 80 - 64);
                pBitArray[4] = ByteHelper.Extract1(highBits, 81 - 64);
                pBitArray[5] = ByteHelper.Extract1(highBits, 82 - 64);
                break;
            case Bc7BlockType.Type1:
                pBitArray[0] = ByteHelper.Extract1(highBits, 80 - 64);
                pBitArray[1] = ByteHelper.Extract1(highBits, 81 - 64);
                break;
            case Bc7BlockType.Type3:
                pBitArray[0] = ByteHelper.Extract1(highBits, 94 - 64);
                pBitArray[1] = ByteHelper.Extract1(highBits, 95 - 64);
                pBitArray[2] = ByteHelper.Extract1(highBits, 96 - 64);
                pBitArray[3] = ByteHelper.Extract1(highBits, 97 - 64);
                break;
            case Bc7BlockType.Type6:
                pBitArray[0] = ByteHelper.Extract1(lowBits, 63);
                pBitArray[1] = ByteHelper.Extract1(highBits, 0);
                break;
            case Bc7BlockType.Type7:
                pBitArray[0] = ByteHelper.Extract1(highBits, 94 - 64);
                pBitArray[1] = ByteHelper.Extract1(highBits, 95 - 64);
                pBitArray[2] = ByteHelper.Extract1(highBits, 96 - 64);
                pBitArray[3] = ByteHelper.Extract1(highBits, 97 - 64);
                break;
        }
    }

    private int GetPBitCount() => Type switch
    {
        Bc7BlockType.Type0 => 6,
        Bc7BlockType.Type6 or Bc7BlockType.Type1 => 2,
        Bc7BlockType.Type7 or Bc7BlockType.Type3 => 4,
        _ => 0,
    };

    private void FinalizeEndpoints(Span<ColorRgba32> endpoints)
    {
        if (HasPBits)
        {
            for (int i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] <<= 1;
            }

            int arraySize = GetPBitCount();
            byte[] pBitArray = ArrayPool<byte>.Shared.Rent(arraySize);
            var pBits = new Span<byte>(pBitArray, 0, arraySize);

            try
            {
                ExtractPBitArray(pBits);

                if (Type == Bc7BlockType.Type1)
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
            finally
            {
                ArrayPool<byte>.Shared.Return(pBitArray, true);
            }

        }

        int colorPrecision = ColorComponentPrecision;
        int alphaPrecision = AlphaComponentPrecision;
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
        if (!HasAlpha)
        {
            for (int i = 0; i < endpoints.Length; i++)
            {
                endpoints[i].a = 255;
            }
        }
    }

    private static int GetPartitionIndex(int numSubsets, int partitionSetId, int i) => numSubsets switch
    {
        1 => 0,
        2 => Subsets2PartitionTable[partitionSetId][i],
        3 => Subsets3PartitionTable[partitionSetId][i],
        _ => throw new ArgumentOutOfRangeException(nameof(numSubsets), numSubsets, "Number of subsets can only be 1, 2 or 3"),
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndexOffset(int numSubsets, int partitionIndex, int bitCount, int index)
    {
        if (index == 0)
        {
            return 0;
        }

        switch (numSubsets)
        {
            case 1:
                return bitCount * index - 1;
            case 2:
                int anchorIndex = Subsets2AnchorIndices[partitionIndex];
                return index <= anchorIndex ? bitCount * index - 1 : bitCount * index - 2;
            case 3:
                int anchor2Index = Subsets3AnchorIndices2[partitionIndex];
                int anchor3Index = Subsets3AnchorIndices3[partitionIndex];

                return index <= anchor2Index && index <= anchor3Index
                    ? bitCount * index - 1
                    : index > anchor2Index && index > anchor3Index ? bitCount * index - 3 : bitCount * index - 2;
        }

        throw new ArgumentOutOfRangeException(nameof(numSubsets), numSubsets, "Number of subsets can only be 1, 2 or 3");
    }

    /// <summary>
    /// Decrements bitCount by one if index is one of the anchor indices.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndexBitCount(int numSubsets, int partitionIndex, int bitCount, int index)
    {
        if (index == 0)
        {
            return bitCount - 1;
        }

        switch (numSubsets)
        {
            case 2 when index == Subsets2AnchorIndices[partitionIndex]:
            case 3 when (index == Subsets3AnchorIndices2[partitionIndex]) || (index == Subsets3AnchorIndices3[partitionIndex]):
                return bitCount - 1;
            default:
                return bitCount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndexBegin(Bc7BlockType type, int bitCount, bool isAlpha) => type switch
    {
        Bc7BlockType.Type0 => 83,
        Bc7BlockType.Type1 => 82,
        Bc7BlockType.Type2 => 99,
        Bc7BlockType.Type3 => 98,
        Bc7BlockType.Type4 => bitCount == 2 ? 50 : 81,
        Bc7BlockType.Type5 => isAlpha ? 97 : 66,
        Bc7BlockType.Type6 => 65,
        Bc7BlockType.Type7 => 98,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    private int GetAlphaIndex(Bc7BlockType type, int numSubsets, int partitionIndex, int bitCount, int index)
    {
        if (bitCount == 0)
        {
            return 0; // No Alpha
        }

        int indexOffset = GetIndexOffset(numSubsets, partitionIndex, bitCount, index);
        int indexBitCount = GetIndexBitCount(numSubsets, partitionIndex, bitCount, index);
        int indexBegin = GetIndexBegin(type, bitCount, true);
        return (int)ByteHelper.ExtractFrom128(lowBits, highBits, indexBegin + indexOffset, indexBitCount);
    }

    private int GetColorIndex(Bc7BlockType type, int numSubsets, int partitionIndex, int bitCount, int index)
    {
        int indexOffset = GetIndexOffset(numSubsets, partitionIndex, bitCount, index);
        int indexBitCount = GetIndexBitCount(numSubsets, partitionIndex, bitCount, index);
        int indexBegin = GetIndexBegin(type, bitCount, false);
        return (int)ByteHelper.ExtractFrom128(lowBits, highBits, indexBegin + indexOffset, indexBitCount);
    }

    private static ColorRgba32 InterpolateColor(ColorRgba32 endPointStart, ColorRgba32 endPointEnd,
        int colorIndex, int alphaIndex, int colorBitCount, int alphaBitCount)
    {
        static byte InterpolateByte(byte e0, byte e1, int index, int indexPrecision)
        {
            if (indexPrecision == 0)
            {
                return e0;
            }

            ReadOnlySpan<byte> aWeights2 = ColorInterpolationWeights2;
            ReadOnlySpan<byte> aWeights3 = ColorInterpolationWeights3;
            ReadOnlySpan<byte> aWeights4 = ColorInterpolationWeights4;

            return indexPrecision == 2
                ? (byte)(((64 - aWeights2[index]) * (e0) + aWeights2[index] * (e1) + 32) >> 6)
                : indexPrecision == 3
                    ? (byte)(((64 - aWeights3[index]) * (e0) + aWeights3[index] * (e1) + 32) >> 6)
                    : (byte)(((64 - aWeights4[index]) * (e0) + aWeights4[index] * (e1) + 32) >> 6);
        }

        var result = new ColorRgba32(
            InterpolateByte(endPointStart.r, endPointEnd.r, colorIndex, colorBitCount),
            InterpolateByte(endPointStart.g, endPointEnd.g, colorIndex, colorBitCount),
            InterpolateByte(endPointStart.b, endPointEnd.b, colorIndex, colorBitCount),
            InterpolateByte(endPointStart.a, endPointEnd.a, alphaIndex, alphaBitCount)
            );

        return result;
    }
    /// <summary>
    /// 00 – no swapping
    /// 01 – swap A and R
    /// 10 – swap A and G
    /// 11 - swap A and B
    /// </summary>		
    private static ColorRgba32 SwapChannels(ColorRgba32 source, int rotation) => rotation switch
    {
        0b00 => source,
        0b01 => new ColorRgba32(source.a, source.g, source.b, source.r),
        0b10 => new ColorRgba32(source.r, source.a, source.b, source.g),
        0b11 => new ColorRgba32(source.r, source.g, source.a, source.b),
        _ => source,
    };


    public RawBlock4X4Rgba32 Decode()
    {
        var output = new RawBlock4X4Rgba32();
        Bc7BlockType type = Type;

        ////decode partition data from explicit partition bits
        //subset_index = 0;
        int numSubsets = 1;
        int partitionIndex = 0;

        if (HasSubsets)
        {
            numSubsets = NumSubsets;
            partitionIndex = PartitionSetId;
            //subset_index = get_partition_index(num_subsets, partition_set_id, x, y);
        }

        Span<GorgonColor> pixels = output.AsSpan;

        bool hasRotationBits = HasRotationBits;
        int rotation = RotationBits;
        int endPointCount = GetRawEndPointCount();

        if (endPointCount == -1)
        {
            throw new InvalidDataException();
        }

        ColorRgba32[] endPointArray = ArrayPool<ColorRgba32>.Shared.Rent(endPointCount);
        var endPoints = new Span<ColorRgba32>(endPointArray, 0, endPointCount);

        try
        {
            ExtractRawEndpoints(endPoints);
            FinalizeEndpoints(endPoints);

            for (int i = 0; i < pixels.Length; i++)
            {
                int subsetIndex = GetPartitionIndex(numSubsets, partitionIndex, i);

                ColorRgba32 endPointStart = endPoints[2 * subsetIndex];
                ColorRgba32 endPointEnd = endPoints[2 * subsetIndex + 1];

                int alphaBitCount = AlphaIndexBitCount;
                int colorBitCount = ColorIndexBitCount;
                int alphaIndex = GetAlphaIndex(type, numSubsets, partitionIndex, alphaBitCount, i);
                int colorIndex = GetColorIndex(type, numSubsets, partitionIndex, colorBitCount, i);

                ColorRgba32 outputColor = InterpolateColor(endPointStart, endPointEnd, colorIndex, alphaIndex,
                    colorBitCount, alphaBitCount);

                if (hasRotationBits)
                {
                    //Decode the 2 color rotation bits as follows:
                    // 00 – Block format is Scalar(A) Vector(RGB) - no swapping
                    // 01 – Block format is Scalar(R) Vector(AGB) - swap A and R
                    // 10 – Block format is Scalar(G) Vector(RAB) - swap A and G
                    // 11 - Block format is Scalar(B) Vector(RGA) - swap A and B
                    outputColor = SwapChannels(outputColor, rotation);
                }

                pixels[i] = outputColor.ToGorgonColor();
            }

            return output;
        }
        finally
        {
            ArrayPool<ColorRgba32>.Shared.Return(endPointArray);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetComponent((byte r, byte g, byte b) value, int componentID) => componentID switch
    {
        1 => value.g,
        2 => value.b,
        _ => value.r,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetComponent((byte r, byte g, byte b, byte a) value, int componentID) => componentID switch
    {
        1 => value.g,
        2 => value.b,
        3 => value.a,
        _ => value.r,
    };

    public void PackType0(int partitionIndex4Bit, Span<(byte r, byte g, byte b)> subsetEndpoints, Span<byte> pBits, Span<byte> indices)
    {
        Debug.Assert(partitionIndex4Bit < 16, "Mode 0 should have 4bit partition index");
        Debug.Assert(subsetEndpoints.Length == 6, "Mode 0 should have 6 endpoints");
        Debug.Assert(pBits.Length == 6, "Mode 0 should have 6 pBits");
        Debug.Assert(indices.Length == 16, "Provide 16 indices");

        lowBits = 1; // Set Mode 0
        highBits = 0;

        lowBits = ByteHelper.Store4(lowBits, 1, (byte)partitionIndex4Bit);

        int nextIdx = 5;
        //Store endpoints

        // Loop control seems different than original.  
        // Outer loop is the component count (3 in the case of this pack type), inner is number of subsets.
        // So, it would appear that we iterate through the subsets:
        // Subset 0: Get r, Subset 1: Get r, etc...
        // THEN on iteration of component:
        // Subset 0: Get g, Subset 1: Get g, etc....

        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < subsetEndpoints.Length; j++)
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx, 4, GetComponent(subsetEndpoints[j], i));
                nextIdx += 4;
            }
        }

        //Store pBits
        for (int i = 0; i < pBits.Length; i++)
        {
            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx, 1, pBits[i]);
            nextIdx++;
        }
        Debug.Assert(nextIdx == 83);

        int colorBitCount = ColorIndexBitCount;
        int indexBegin = GetIndexBegin(Bc7BlockType.Type0, colorBitCount, false);
        for (int i = 0; i < 16; i++)
        {
            int indexOffset = GetIndexOffset(NumSubsets,
                partitionIndex4Bit, colorBitCount, i);
            int indexBitCount = GetIndexBitCount(NumSubsets,
                partitionIndex4Bit, colorBitCount, i);

            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                indexBegin + indexOffset, indexBitCount, indices[i]);
        }
    }

    public void PackType1(int partitionIndex6Bit, Span<(byte r, byte g, byte b)> subsetEndpoints, Span<byte> pBits, Span<byte> indices)
    {
        Debug.Assert(partitionIndex6Bit < 64, "Mode 1 should have 6bit partition index");
        Debug.Assert(subsetEndpoints.Length == 4, "Mode 1 should have 4 endpoints");
        Debug.Assert(pBits.Length == 2, "Mode 1 should have 2 pBits");
        Debug.Assert(indices.Length == 16, "Provide 16 indices");

        lowBits = 2; // Set Mode 1
        highBits = 0;

        lowBits = ByteHelper.Store6(lowBits, 2, (byte)partitionIndex6Bit);

        int nextIdx = 8;
        //Store endpoints
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < subsetEndpoints.Length; j++)
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx, 6, GetComponent(subsetEndpoints[j], i));
                nextIdx += 6;
            }
        }
        //Store pBits
        for (int i = 0; i < pBits.Length; i++)
        {
            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx, 1, pBits[i]);
            nextIdx++;
        }
        Debug.Assert(nextIdx == 82);

        int colorBitCount = ColorIndexBitCount;
        int indexBegin = GetIndexBegin(Bc7BlockType.Type1, colorBitCount, false);
        for (int i = 0; i < 16; i++)
        {
            int indexOffset = GetIndexOffset(NumSubsets,
                partitionIndex6Bit, colorBitCount, i);
            int indexBitCount = GetIndexBitCount(NumSubsets,
                partitionIndex6Bit, colorBitCount, i);

            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                indexBegin + indexOffset, indexBitCount, indices[i]);
        }
    }

    public void PackType2(int partitionIndex6Bit, Span<(byte r, byte g, byte b)> subsetEndpoints, Span<byte> indices)
    {
        Debug.Assert(partitionIndex6Bit < 64, "Mode 2 should have 6bit partition index");
        Debug.Assert(subsetEndpoints.Length == 6, "Mode 2 should have 6 endpoints");
        Debug.Assert(indices.Length == 16, "Provide 16 indices");

        lowBits = 0b100; // Set Mode 2
        highBits = 0;

        lowBits = ByteHelper.Store6(lowBits, 3, (byte)partitionIndex6Bit);

        int nextIdx = 9;

        for (int i = 0; i < 3; ++i)
        {
            //Store endpoints
            for (int j = 0; j < subsetEndpoints.Length; j++)
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx, 5, GetComponent(subsetEndpoints[j], i));
                nextIdx += 5;
            }
        }

        Debug.Assert(nextIdx == 99);

        int colorBitCount = ColorIndexBitCount;
        int indexBegin = GetIndexBegin(Bc7BlockType.Type2, colorBitCount, false);
        for (int i = 0; i < 16; i++)
        {
            int indexOffset = GetIndexOffset(NumSubsets,
                partitionIndex6Bit, colorBitCount, i);
            int indexBitCount = GetIndexBitCount(NumSubsets,
                partitionIndex6Bit, colorBitCount, i);

            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                indexBegin + indexOffset, indexBitCount, indices[i]);
        }
    }

    public void PackType3(int partitionIndex6Bit, Span<(byte r, byte g, byte b)> subsetEndpoints, Span<byte> pBits, Span<byte> indices)
    {
        Debug.Assert(partitionIndex6Bit < 64, "Mode 3 should have 6bit partition index");
        Debug.Assert(subsetEndpoints.Length == 4, "Mode 3 should have 4 endpoints");
        Debug.Assert(pBits.Length == 4, "Mode 3 should have 4 pBits");
        Debug.Assert(indices.Length == 16, "Provide 16 indices");

        lowBits = 0b1000; // Set Mode 3
        highBits = 0;

        lowBits = ByteHelper.Store6(lowBits, 4, (byte)partitionIndex6Bit);

        int nextIdx = 10;
        //Store endpoints
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < subsetEndpoints.Length; j++)
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx, 7, GetComponent(subsetEndpoints[j], i));
                nextIdx += 7;
            }
        }

        //Store pBits
        for (int i = 0; i < pBits.Length; i++)
        {
            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx,
                1, pBits[i]);
            nextIdx++;
        }
        Debug.Assert(nextIdx == 98);

        int colorBitCount = ColorIndexBitCount;
        int indexBegin = GetIndexBegin(Bc7BlockType.Type3, colorBitCount, false);
        for (int i = 0; i < 16; i++)
        {
            int indexOffset = GetIndexOffset(NumSubsets,
                partitionIndex6Bit, colorBitCount, i);
            int indexBitCount = GetIndexBitCount(NumSubsets,
                partitionIndex6Bit, colorBitCount, i);

            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                indexBegin + indexOffset, indexBitCount, indices[i]);
        }
    }

    public void PackType4(int rotation, byte idxMode, Span<(byte r, byte g, byte b)> colorEndPoints, Span<byte> alphaEndPoints, Span<byte> indices2Bit, Span<byte> indices3Bit)
    {
        Debug.Assert(rotation < 4, "Rotation can only be 0-3");
        Debug.Assert(idxMode < 2, "IndexMode can only be 0 or 1");
        Debug.Assert(colorEndPoints.Length == 2, "Mode 4 should have 2 endpoints");
        Debug.Assert(alphaEndPoints.Length == 2, "Mode 4 should have 2 endpoints");

        Debug.Assert(indices2Bit.Length == 16, "Provide 16 indices");
        Debug.Assert(indices3Bit.Length == 16, "Provide 16 indices");

        lowBits = 0b10000; // Set Mode 4
        highBits = 0;

        lowBits = ByteHelper.Store2(lowBits, 5, (byte)rotation);
        lowBits = ByteHelper.Store1(lowBits, 7, idxMode);

        int nextIdx = 8;
        //Store color endpoints
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < colorEndPoints.Length; j++)
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx, 5, GetComponent(colorEndPoints[j], i));
                nextIdx += 5;
            }
        }

        //Store alpha endpoints
        for (int i = 0; i < alphaEndPoints.Length; i++)
        {
            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx,
                6, alphaEndPoints[i]);
            nextIdx += 6;
        }
        Debug.Assert(nextIdx == 50);

        int colorBitCount = ColorIndexBitCount;
        int colorIndexBegin = GetIndexBegin(Bc7BlockType.Type4, colorBitCount, false);
        for (int i = 0; i < 16; i++)
        {
            int indexOffset = GetIndexOffset(NumSubsets,
                0, colorBitCount, i);
            int indexBitCount = GetIndexBitCount(NumSubsets,
                0, colorBitCount, i);

            if (idxMode == 0)
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                    colorIndexBegin + indexOffset, indexBitCount, indices2Bit[i]);
            }
            else
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                    colorIndexBegin + indexOffset, indexBitCount, indices3Bit[i]);
            }
        }

        int alphaBitCount = AlphaIndexBitCount;
        int alphaIndexBegin = GetIndexBegin(Bc7BlockType.Type4, alphaBitCount, true);
        for (int i = 0; i < 16; i++)
        {
            int indexOffset = GetIndexOffset(NumSubsets,
                0, alphaBitCount, i);
            int indexBitCount = GetIndexBitCount(NumSubsets,
                0, alphaBitCount, i);

            if (idxMode == 0)
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                    alphaIndexBegin + indexOffset, indexBitCount, indices3Bit[i]);
            }
            else
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                    alphaIndexBegin + indexOffset, indexBitCount, indices2Bit[i]);
            }
        }
    }

    public void PackType5(int rotation, Span<(byte r, byte g, byte b)> colorEndPoints, Span<byte> alphaEndPoints, Span<byte> colorIndices, Span<byte> alphaIndices)
    {
        Debug.Assert(rotation < 4, "Rotation can only be 0-3");
        Debug.Assert(colorEndPoints.Length == 2, "Mode 5 should have 2 endpoints");
        Debug.Assert(alphaEndPoints.Length == 2, "Mode 5 should have 2 endpoints");

        Debug.Assert(colorIndices.Length == 16, "Provide 16 indices");
        Debug.Assert(alphaIndices.Length == 16, "Provide 16 indices");

        lowBits = 0b100000; // Set Mode 5
        highBits = 0;

        lowBits = ByteHelper.Store2(lowBits, 6, (byte)rotation);

        int nextIdx = 8;
        //Store color endpoints
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < colorEndPoints.Length; j++)
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx, 7, GetComponent(colorEndPoints[j], i));
                nextIdx += 7;
            }
        }
        //Store alpha endpoints
        for (int i = 0; i < alphaEndPoints.Length; i++)
        {
            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx,
                8, alphaEndPoints[i]);
            nextIdx += 8;
        }
        Debug.Assert(nextIdx == 66);

        int colorBitCount = ColorIndexBitCount;
        int colorIndexBegin = GetIndexBegin(Bc7BlockType.Type5, colorBitCount, false);
        for (int i = 0; i < 16; i++)
        {
            int indexOffset = GetIndexOffset(NumSubsets,
                0, colorBitCount, i);
            int indexBitCount = GetIndexBitCount(NumSubsets,
                0, colorBitCount, i);

            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                colorIndexBegin + indexOffset, indexBitCount, colorIndices[i]);

        }

        int alphaBitCount = AlphaIndexBitCount;
        int alphaIndexBegin = GetIndexBegin(Bc7BlockType.Type5, alphaBitCount, true);
        for (int i = 0; i < 16; i++)
        {
            int indexOffset = GetIndexOffset(NumSubsets,
                0, alphaBitCount, i);
            int indexBitCount = GetIndexBitCount(NumSubsets,
                0, alphaBitCount, i);


            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                alphaIndexBegin + indexOffset, indexBitCount, alphaIndices[i]);

        }
    }

    public void PackType6(Span<(byte r, byte g, byte b, byte a)> colorAlphaEndPoints, Span<byte> pBits, Span<byte> indices)
    {
        Debug.Assert(colorAlphaEndPoints.Length == 2,
            "Mode 6 should have 2 endpoints");
        Debug.Assert(pBits.Length == 2, "Mode 6 should have 2 pBits");
        Debug.Assert(indices.Length == 16, "Provide 16 indices");

        lowBits = 0b1000000; // Set Mode 6
        highBits = 0;

        int nextIdx = 7;

        for (int i = 0; i < 4; ++i)
        {
            //Store color and alpha endpoints
            for (int j = 0; j < colorAlphaEndPoints.Length; j++)
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx, 7, GetComponent(colorAlphaEndPoints[j], i));
                nextIdx += 7;
            }
        }
        //Store pBits
        for (int i = 0; i < pBits.Length; i++)
        {
            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx,
                1, pBits[i]);
            nextIdx++;
        }
        Debug.Assert(nextIdx == 65);

        int colorBitCount = ColorIndexBitCount;
        int colorIndexBegin = GetIndexBegin(Bc7BlockType.Type6, colorBitCount, false);
        for (int i = 0; i < 16; i++)
        {
            int indexOffset = GetIndexOffset(NumSubsets,
                0, colorBitCount, i);
            int indexBitCount = GetIndexBitCount(NumSubsets,
                0, colorBitCount, i);

            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                colorIndexBegin + indexOffset, indexBitCount, indices[i]);

        }
    }

    public void PackType7(int partitionIndex6Bit, Span<(byte r, byte g, byte b, byte a)> subsetEndpoints, Span<byte> pBits, Span<byte> indices)
    {
        Debug.Assert(partitionIndex6Bit < 64, "Mode 7 should have 6bit partition index");
        Debug.Assert(subsetEndpoints.Length == 4, "Mode 7 should have 4 endpoints");
        Debug.Assert(pBits.Length == 4, "Mode 7 should have 4 pBits");

        lowBits = 0b10000000; // Set Mode 7
        highBits = 0;

        lowBits = ByteHelper.Store6(lowBits, 8, (byte)partitionIndex6Bit);

        int nextIdx = 14;
        //Store endpoints
        for (int i = 0; i < 4; ++i)
        {
            for (int j = 0; j < subsetEndpoints.Length; j++)
            {
                (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx, 5, GetComponent(subsetEndpoints[j], i));
                nextIdx += 5;
            }
        }

        //Store pBits
        for (int i = 0; i < pBits.Length; i++)
        {
            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits, nextIdx,
                1, pBits[i]);
            nextIdx++;
        }
        Debug.Assert(nextIdx == 98);

        int colorBitCount = ColorIndexBitCount;
        int indexBegin = GetIndexBegin(Bc7BlockType.Type7, colorBitCount, false);
        for (int i = 0; i < 16; i++)
        {
            int indexOffset = GetIndexOffset(NumSubsets,
                partitionIndex6Bit, colorBitCount, i);
            int indexBitCount = GetIndexBitCount(NumSubsets,
                partitionIndex6Bit, colorBitCount, i);

            (lowBits, highBits) = ByteHelper.StoreTo128(lowBits, highBits,
                indexBegin + indexOffset, indexBitCount, indices[i]);
        }
    }
}
