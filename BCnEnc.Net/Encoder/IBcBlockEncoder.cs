using System;
using BCnEncoder.Shared;
using Gorgon.Graphics;
using Gorgon.Native;

namespace BCnEncoder.Encoder
{
    internal interface IBcBlockEncoder
	{
		GorgonNativeBuffer<byte> Encode(RawBlock4X4Rgba32[] blocks, int blockCount, CompressionQuality quality, bool parallel = true);
	}


}
