using System;
using System.IO;
using System.Runtime.InteropServices;
using BCnEncoder.Shared;
using Gorgon.Native;

namespace BCnEncoder.Decoder
{
    internal class Bc7Decoder : IBcBlockDecoder
    {
        public RawBlock4X4Rgba32[,] Decode(GorgonPtr<byte> data, int pixelWidth, int pixelHeight)
        {
            int blockWidth = (int)Math.Ceiling(pixelWidth / 4.0);
            int blockHeight = (int)Math.Ceiling(pixelHeight / 4.0);

            if (data.Length != (blockWidth * blockHeight * Marshal.SizeOf<Bc7Block>()))
            {
                throw new InvalidDataException();
            }

            GorgonPtr<Bc7Block> encodedBlocks = data.To<Bc7Block>();

            var output = new RawBlock4X4Rgba32[blockWidth, blockHeight];

            for (int x = 0; x < blockWidth; x++)
            {
                for (int y = 0; y < blockHeight; y++)
                {
                    Bc7Block rawBlock = encodedBlocks[x + y * blockWidth];
                    output[x, y] = rawBlock.Decode();
                }
            }

            return output;
        }
    }
}
