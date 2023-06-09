using System;
using BCnEncoder.Properties;
using BCnEncoder.Shared;
using Gorgon.Graphics;
using Gorgon.Math;
using Gorgon.Native;

namespace BCnEncoder.Decoder;

/// <summary>
/// Decodes compressed files into Rgba format.
/// </summary>
public class BcDecoder
{
    /// <summary>
    /// If true, when encoding to a format that only includes a red channel,
    /// use the pixel luminance instead of just the red channel. Default is false.
    /// </summary>
    public bool LuminanceAsRed
    {
        get;
        set;
    } = false;

    private IBcBlockDecoder GetDecoder(BufferFormat format, bool expectAlpha)
    {
        switch (format)
        {
            case BufferFormat.BC1_UNorm when expectAlpha:
            case BufferFormat.BC1_UNorm_SRgb when expectAlpha:
                return new Bc1ADecoder();
            case BufferFormat.BC1_UNorm:
            case BufferFormat.BC1_UNorm_SRgb:
                return new Bc1NoAlphaDecoder();
            case BufferFormat.BC2_UNorm:
            case BufferFormat.BC2_UNorm_SRgb:
                return new Bc2Decoder();
            case BufferFormat.BC3_UNorm:
            case BufferFormat.BC3_UNorm_SRgb:
                return new Bc3Decoder();
            case BufferFormat.BC4_UNorm:
            case BufferFormat.BC4_SNorm:
                return new Bc4Decoder(LuminanceAsRed);
            case BufferFormat.BC5_SNorm:
            case BufferFormat.BC5_UNorm:
                return new Bc5Decoder();
            case BufferFormat.BC7_UNorm:
            case BufferFormat.BC7_UNorm_SRgb:
                return new Bc7Decoder();
            default:
                return null;
        }
    }

    private static GorgonNativeBuffer<byte> ImageFromRawBlocks(RawBlock4X4Rgba32[,] blocks, int pixelWidth, int pixelHeight)
    {
        var result = new GorgonNativeBuffer<byte>(pixelWidth * 4 * pixelHeight);
        GorgonPtr<int> output = ((GorgonPtr<byte>)result).To<int>();

        for (int y = 0; y < pixelHeight; y++)
        {
            for (int x = 0; x < pixelWidth; x++)
            {
                int blockIndexX = (int)(x / 4.0f).FastFloor();
                int blockIndexY = (int)(y / 4.0f).FastFloor();
                int blockInternalIndexX = x % 4;
                int blockInternalIndexY = y % 4;

                output[x + y * pixelWidth] =
                    blocks[blockIndexX, blockIndexY]
                        [blockInternalIndexX, blockInternalIndexY].ToABGR();
            }
        }

        return result;
    }

    /// <summary>
    /// Read a Ktx or a Dds file from a stream and decode it.
    /// </summary>
    public GorgonNativeBuffer<byte> Decode(GorgonPtr<byte> encodedData, int imageWidth, int imageHeight, bool has1BitAlpha, BufferFormat compressedFormat)
    {
        IBcBlockDecoder decoder = GetDecoder(compressedFormat, has1BitAlpha) ?? throw new NotSupportedException(string.Format(Resources.BCENC_ERR_FORMAT_NOT_SUPPORTED, compressedFormat));
        int blocksWidth = (int)(imageWidth / 4.0f).FastCeiling();
        int blocksHeight = (int)(imageHeight / 4.0f).FastCeiling();

        RawBlock4X4Rgba32[,] blocks = decoder.Decode(encodedData, imageWidth, imageHeight);

        return ImageFromRawBlocks(blocks, blocksWidth * 4, blocksHeight * 4);
    }
}