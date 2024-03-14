using System;
using System.Diagnostics;
using System.Numerics;
using BCnEncoder.Shared;
using Gorgon.Graphics;

namespace BCnEncoder.Encoder;

internal class Bc3BlockEncoder : BcBlockEncoder<Bc3Block>
{
    public Bc3BlockEncoder()
        : base(4)
    {
    }

    protected override Bc3Block EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality) => quality switch
    {
        CompressionQuality.Fast => Bc3BlockEncoderFast.EncodeBlock(block),
        CompressionQuality.Balanced => Bc3BlockEncoderBalanced.EncodeBlock(block),
        CompressionQuality.BestQuality => Bc3BlockEncoderSlow.EncodeBlock(block),
        _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null),
    };

    private static Bc3Block TryColors(RawBlock4X4Rgba32 rawBlock, ColorRgb565 color0, ColorRgb565 color1, out float error, float rWeight = 0.3f, float gWeight = 0.6f, float bWeight = 0.1f)
    {
        var output = new Bc3Block();

        Span<GorgonColor> pixels = rawBlock.AsSpan;

        output.color0 = color0;
        output.color1 = color1;

        var c0 = color0.ToColorRgb24();
        var c1 = color1.ToColorRgb24();

        ReadOnlySpan<ColorRgb24> colors = [
            c0,
            c1,
            c0 * (2.0 / 3.0) + c1 * (1.0 / 3.0),
            c0 * (1.0 / 3.0) + c1 * (2.0 / 3.0)
        ];

        error = 0;
        for (int i = 0; i < 16; i++)
        {
            GorgonColor color = pixels[i];
            output[i] = ColorChooser.ChooseClosestColor4(colors, color, rWeight, gWeight, bWeight, out float e);
            error += e;
        }

        return output;
    }

    private static Bc3Block FindAlphaValues(Bc3Block colorBlock, RawBlock4X4Rgba32 rawBlock, int variations)
    {
        int bestError;
        Span<GorgonColor> pixels = rawBlock.AsSpan;

        //Find min and max alpha
        byte minAlpha = 255;
        byte maxAlpha = 0;
        bool hasExtremeValues = false;
        for (int i = 0; i < pixels.Length; i++)
        {
            int alpha = (int)(pixels[i].Alpha * 255.0f);
            if (alpha is < 255 and > 0)
            {
                if (alpha < minAlpha)
                {
                    minAlpha = (byte)alpha;
                }

                if (alpha > maxAlpha)
                {
                    maxAlpha = (byte)alpha;
                }
            }
            else
            {
                hasExtremeValues = true;
            }
        }


        int SelectAlphaIndices(ref Bc3Block block, Span<GorgonColor> aPixels)
        {
            int cumulativeError = 0;
            byte a0 = block.Alpha0;
            byte a1 = block.Alpha1;
            Span<byte> alphas = a0 > a1 ? [
                a0,
                a1,
                (byte)(6 / 7.0 * a0 + 1 / 7.0 * a1),
                (byte)(5 / 7.0 * a0 + 2 / 7.0 * a1),
                (byte)(4 / 7.0 * a0 + 3 / 7.0 * a1),
                (byte)(3 / 7.0 * a0 + 4 / 7.0 * a1),
                (byte)(2 / 7.0 * a0 + 5 / 7.0 * a1),
                (byte)(1 / 7.0 * a0 + 6 / 7.0 * a1),
            ] : [
                a0,
                a1,
                (byte)(4 / 5.0 * a0 + 1 / 5.0 * a1),
                (byte)(3 / 5.0 * a0 + 2 / 5.0 * a1),
                (byte)(2 / 5.0 * a0 + 3 / 5.0 * a1),
                (byte)(1 / 5.0 * a0 + 4 / 5.0 * a1),
                0,
                255
            ];

            for (int i = 0; i < aPixels.Length; i++)
            {
                byte bestIndex = 0;
                int alpha = (int)(aPixels[i].Alpha * 255.0f);
                bestError = Math.Abs(alpha - alphas[0]);
                for (byte j = 1; j < alphas.Length; j++)
                {
                    int error = Math.Abs(alpha - alphas[j]);
                    if (error < bestError)
                    {
                        bestIndex = j;
                        bestError = error;
                    }
                    if (bestError == 0)
                    {
                        break;
                    }
                }
                block.SetAlphaIndex(i, bestIndex);
                cumulativeError += bestError * bestError;
            }

            return cumulativeError;
        }

        //everything is either fully opaque or fully transparent
        if (hasExtremeValues && minAlpha == 255 && maxAlpha == 0)
        {
            colorBlock.Alpha0 = 0;
            colorBlock.Alpha1 = 255;
            int error = SelectAlphaIndices(ref colorBlock, pixels);
            Debug.Assert(0 == error);
            return colorBlock;
        }

        Bc3Block best = colorBlock;
        best.Alpha0 = maxAlpha;
        best.Alpha1 = minAlpha;
        bestError = SelectAlphaIndices(ref best, pixels);
        if (bestError == 0)
        {
            return best;
        }
        for (byte i = 1; i < variations; i++)
        {
            {
                byte a0 = ByteHelper.ClampToByte(maxAlpha - i * 2);
                byte a1 = ByteHelper.ClampToByte(minAlpha + i * 2);
                Bc3Block block = colorBlock;
                block.Alpha0 = hasExtremeValues ? a1 : a0;
                block.Alpha1 = hasExtremeValues ? a0 : a1;
                int error = SelectAlphaIndices(ref block, pixels);
                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                }
            }
            {
                byte a0 = ByteHelper.ClampToByte(maxAlpha + i * 2);
                byte a1 = ByteHelper.ClampToByte(minAlpha - i * 2);
                Bc3Block block = colorBlock;
                block.Alpha0 = hasExtremeValues ? a1 : a0;
                block.Alpha1 = hasExtremeValues ? a0 : a1;
                int error = SelectAlphaIndices(ref block, pixels);
                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                }
            }
            {
                byte a0 = ByteHelper.ClampToByte(maxAlpha);
                byte a1 = ByteHelper.ClampToByte(minAlpha - i * 2);
                Bc3Block block = colorBlock;
                block.Alpha0 = hasExtremeValues ? a1 : a0;
                block.Alpha1 = hasExtremeValues ? a0 : a1;
                int error = SelectAlphaIndices(ref block, pixels);
                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                }
            }
            {
                byte a0 = ByteHelper.ClampToByte(maxAlpha + i * 2);
                byte a1 = ByteHelper.ClampToByte(minAlpha);
                Bc3Block block = colorBlock;
                block.Alpha0 = hasExtremeValues ? a1 : a0;
                block.Alpha1 = hasExtremeValues ? a0 : a1;
                int error = SelectAlphaIndices(ref block, pixels);
                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                }
            }
            {
                byte a0 = ByteHelper.ClampToByte(maxAlpha);
                byte a1 = ByteHelper.ClampToByte(minAlpha + i * 2);
                Bc3Block block = colorBlock;
                block.Alpha0 = hasExtremeValues ? a1 : a0;
                block.Alpha1 = hasExtremeValues ? a0 : a1;
                int error = SelectAlphaIndices(ref block, pixels);
                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                }
            }
            {
                byte a0 = ByteHelper.ClampToByte(maxAlpha - i * 2);
                byte a1 = ByteHelper.ClampToByte(minAlpha);
                Bc3Block block = colorBlock;
                block.Alpha0 = hasExtremeValues ? a1 : a0;
                block.Alpha1 = hasExtremeValues ? a0 : a1;
                int error = SelectAlphaIndices(ref block, pixels);
                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                }
            }

            if (bestError < 10)
            {
                break;
            }
        }

        return best;
    }

    private static class Bc3BlockEncoderFast
    {

        internal static Bc3Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
        {
            Span<GorgonColor> pixels = rawBlock.AsSpan;

            PcaVectors.Create(pixels, out Vector3 mean, out Vector3 principalAxis);
            PcaVectors.GetMinMaxColor565(pixels, mean, principalAxis, out ColorRgb565 min, out ColorRgb565 max);

            ColorRgb565 c0 = max;
            ColorRgb565 c1 = min;

            if (c0.data <= c1.data)
            {
                (c1, c0) = (c0, c1);
            }

            Bc3Block output = TryColors(rawBlock, c0, c1, out float _);
            output = FindAlphaValues(output, rawBlock, 3);

            return output;
        }
    }

    private static class Bc3BlockEncoderBalanced
    {
        private const int MaxTries = 24 * 2;
        private const float ErrorThreshsold = 0.05f;

        internal static Bc3Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
        {
            Span<GorgonColor> pixels = rawBlock.AsSpan;

            PcaVectors.Create(pixels, out Vector3 mean, out Vector3 pa);
            PcaVectors.GetMinMaxColor565(pixels, mean, pa, out ColorRgb565 min, out ColorRgb565 max);

            ColorRgb565 c0 = max;
            ColorRgb565 c1 = min;

            Bc3Block best = TryColors(rawBlock, c0, c1, out float bestError);

            for (int i = 0; i < MaxTries; i++)
            {
                (ColorRgb565 newC0, ColorRgb565 newC1) = ColorVariationGenerator.Variate565(c0, c1, i);

                Bc3Block block = TryColors(rawBlock, newC0, newC1, out float error);

                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                    c0 = newC0;
                    c1 = newC1;
                }

                if (bestError < ErrorThreshsold)
                {
                    break;
                }
            }
            best = FindAlphaValues(best, rawBlock, 5);
            return best;
        }
    }

    private static class Bc3BlockEncoderSlow
    {
        private const int MaxTries = 9999;
        private const float ErrorThreshsold = 0.01f;


        internal static Bc3Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
        {
            Span<GorgonColor> pixels = rawBlock.AsSpan;

            PcaVectors.Create(pixels, out Vector3 mean, out Vector3 pa);
            PcaVectors.GetMinMaxColor565(pixels, mean, pa, out ColorRgb565 min, out ColorRgb565 max);

            ColorRgb565 c0 = max;
            ColorRgb565 c1 = min;

            if (c0.data < c1.data)
            {
                (c1, c0) = (c0, c1);
            }

            Bc3Block best = TryColors(rawBlock, c0, c1, out float bestError);

            int lastChanged = 0;

            for (int i = 0; i < MaxTries; i++)
            {
                (ColorRgb565 newC0, ColorRgb565 newC1) = ColorVariationGenerator.Variate565(c0, c1, i);

                if (newC0.data < newC1.data)
                {
                    (newC1, newC0) = (newC0, newC1);
                }

                Bc3Block block = TryColors(rawBlock, newC0, newC1, out float error);

                lastChanged++;

                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                    c0 = newC0;
                    c1 = newC1;
                    lastChanged = 0;
                }

                if (bestError < ErrorThreshsold || lastChanged > ColorVariationGenerator.VarPatternCount)
                {
                    break;
                }
            }

            best = FindAlphaValues(best, rawBlock, 8);
            return best;
        }
    }

}
