using Gorgon.BCnEncoder.Shared;
using Gorgon.Native;

namespace Gorgon.BCnEncoder.Encoder;

internal interface IBcBlockEncoder
{
    GorgonNativeBuffer<byte> Encode(RawBlock4X4Rgba32[] blocks, int blockCount, CompressionQuality quality, bool parallel = true);
}
