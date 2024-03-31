using System.IO;
using System.Runtime.CompilerServices;
using Gorgon.BCnEncoder.Shared;
using Gorgon.Native;

namespace Gorgon.BCnEncoder.Decoder;

internal interface IBcBlockDecoder
{
    RawBlock4X4Rgba32[,] Decode(GorgonPtr<byte> data, int pixelWidth, int pixelHeight);
}

internal class Bc1NoAlphaDecoder : IBcBlockDecoder
{
    public RawBlock4X4Rgba32[,] Decode(GorgonPtr<byte> data, int pixelWidth, int pixelHeight)
    {
        int blockWidth = (int)System.Math.Ceiling(pixelWidth / 4.0f);
        int blockHeight = (int)System.Math.Ceiling(pixelHeight / 4.0f);

        if (data.Length != (blockWidth * blockHeight * Unsafe.SizeOf<Bc1Block>()))
        {
            throw new InvalidDataException();
        }

        GorgonPtr<Bc1Block> encodedBlocks = GorgonPtr<byte>.To<Bc1Block>(data);

        var output = new RawBlock4X4Rgba32[blockWidth, blockHeight];

        for (int x = 0; x < blockWidth; x++)
        {
            for (int y = 0; y < blockHeight; y++)
            {
                output[x, y] = encodedBlocks[x + y * blockWidth].Decode(false);
            }
        }

        return output;
    }
}

internal class Bc1ADecoder : IBcBlockDecoder
{
    public RawBlock4X4Rgba32[,] Decode(GorgonPtr<byte> data, int pixelWidth, int pixelHeight)
    {
        int blockWidth = (int)System.Math.Ceiling(pixelWidth / 4.0f);
        int blockHeight = (int)System.Math.Ceiling(pixelHeight / 4.0f);

        if (data.Length != (blockWidth * blockHeight * Unsafe.SizeOf<Bc1Block>()))
        {
            throw new InvalidDataException();
        }

        GorgonPtr<Bc1Block> encodedBlocks = GorgonPtr<byte>.To<Bc1Block>(data);

        var output = new RawBlock4X4Rgba32[blockWidth, blockHeight];

        for (int x = 0; x < blockWidth; x++)
        {
            for (int y = 0; y < blockHeight; y++)
            {
                output[x, y] = encodedBlocks[x + y * blockWidth].Decode(true);
            }
        }

        return output;
    }
}

internal class Bc2Decoder : IBcBlockDecoder
{
    public RawBlock4X4Rgba32[,] Decode(GorgonPtr<byte> data, int pixelWidth, int pixelHeight)
    {
        int blockWidth = (int)System.Math.Ceiling(pixelWidth / 4.0f);
        int blockHeight = (int)System.Math.Ceiling(pixelHeight / 4.0f);

        if (data.Length != (blockWidth * blockHeight * Unsafe.SizeOf<Bc2Block>()))
        {
            throw new InvalidDataException();
        }

        GorgonPtr<Bc2Block> encodedBlocks = GorgonPtr<byte>.To<Bc2Block>(data);

        var output = new RawBlock4X4Rgba32[blockWidth, blockHeight];

        for (int x = 0; x < blockWidth; x++)
        {
            for (int y = 0; y < blockHeight; y++)
            {
                output[x, y] = encodedBlocks[x + y * blockWidth].Decode();
            }
        }

        return output;
    }
}

internal class Bc3Decoder : IBcBlockDecoder
{
    public RawBlock4X4Rgba32[,] Decode(GorgonPtr<byte> data, int pixelWidth, int pixelHeight)
    {
        int blockWidth = (int)System.Math.Ceiling(pixelWidth / 4.0f);
        int blockHeight = (int)System.Math.Ceiling(pixelHeight / 4.0f);

        if (data.Length != (blockWidth * blockHeight * Unsafe.SizeOf<Bc3Block>()))
        {
            throw new InvalidDataException();
        }

        GorgonPtr<Bc3Block> encodedBlocks = GorgonPtr<byte>.To<Bc3Block>(data);

        var output = new RawBlock4X4Rgba32[blockWidth, blockHeight];

        for (int x = 0; x < blockWidth; x++)
        {
            for (int y = 0; y < blockHeight; y++)
            {
                output[x, y] = encodedBlocks[x + y * blockWidth].Decode();
            }
        }

        return output;
    }
}

internal class Bc4Decoder(bool redAsLuminance) : IBcBlockDecoder
{
    private readonly bool _redAsLuminance = redAsLuminance;

    public RawBlock4X4Rgba32[,] Decode(GorgonPtr<byte> data, int pixelWidth, int pixelHeight)
    {
        int blockWidth = (int)System.Math.Ceiling(pixelWidth / 4.0f);
        int blockHeight = (int)System.Math.Ceiling(pixelHeight / 4.0f);

        if (data.Length != (blockWidth * blockHeight * Unsafe.SizeOf<Bc4Block>()))
        {
            throw new InvalidDataException();
        }

        GorgonPtr<Bc4Block> encodedBlocks = GorgonPtr<byte>.To<Bc4Block>(data);

        var output = new RawBlock4X4Rgba32[blockWidth, blockHeight];

        for (int x = 0; x < blockWidth; x++)
        {
            for (int y = 0; y < blockHeight; y++)
            {
                output[x, y] = encodedBlocks[x + y * blockWidth].Decode(_redAsLuminance);
            }
        }

        return output;
    }
}

internal class Bc5Decoder : IBcBlockDecoder
{
    public RawBlock4X4Rgba32[,] Decode(GorgonPtr<byte> data, int pixelWidth, int pixelHeight)
    {
        int blockWidth = (int)System.Math.Ceiling(pixelWidth / 4.0f);
        int blockHeight = (int)System.Math.Ceiling(pixelHeight / 4.0f);

        if (data.Length != (blockWidth * blockHeight * Unsafe.SizeOf<Bc5Block>()))
        {
            throw new InvalidDataException();
        }

        GorgonPtr<Bc5Block> encodedBlocks = GorgonPtr<byte>.To<Bc5Block>(data);

        var output = new RawBlock4X4Rgba32[blockWidth, blockHeight];

        for (int x = 0; x < blockWidth; x++)
        {
            for (int y = 0; y < blockHeight; y++)
            {
                output[x, y] = encodedBlocks[x + y * blockWidth].Decode();
            }
        }

        return output;
    }
}
