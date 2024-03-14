using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BCnEncoder.Shared;
using Gorgon.Math;
using Gorgon.Native;

namespace BCnEncoder.Encoder;

internal abstract class BcBlockEncoder<T>(int maxThreads)
    : IBcBlockEncoder
    where T : unmanaged
{
    private readonly int _maxThreads = maxThreads.Max(1).Min((Environment.ProcessorCount / 2).Max(1));

    public GorgonNativeBuffer<byte> Encode(RawBlock4X4Rgba32[] blocks, int blockCount, CompressionQuality quality, bool parallel = true)
    {
        var result = new GorgonNativeBuffer<byte>(blockCount * Unsafe.SizeOf<T>());
        GorgonPtr<T> outputBlocks = ((GorgonPtr<byte>)result).To<T>();

        void EncodeData(int index) => outputBlocks[index] = EncodeBlock(blocks[index], quality);

        if (parallel)
        {
            Parallel.For(0, blockCount, new ParallelOptions { MaxDegreeOfParallelism = _maxThreads }, EncodeData);
        }
        else
        {
            for (int i = 0; i < blockCount; i++)
            {
                EncodeData(i);
            }
        }

        return result;
    }

    protected abstract T EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality);
}
