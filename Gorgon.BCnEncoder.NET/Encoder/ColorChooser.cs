using System;
using Gorgon.BCnEncoder.Shared;
using Gorgon.Graphics;

namespace Gorgon.BCnEncoder.Encoder;

internal static class ColorChooser
{
    public static int ChooseClosestColor4(ReadOnlySpan<ColorRgb24> colors, GorgonColor color, float rWeight, float gWeight, float bWeight, out float error)
    {
        (int R, int G, int B, int _) = color.GetIntegerComponents();

        ReadOnlySpan<float> d = [
            System.Math.Abs(colors[0].r - R) * rWeight
            + System.Math.Abs(colors[0].g - G) * gWeight
            + System.Math.Abs(colors[0].b - B) * bWeight,
            System.Math.Abs(colors[1].r - R) * rWeight
            + System.Math.Abs(colors[1].g - G) * gWeight
            + System.Math.Abs(colors[1].b - B) * bWeight,
            System.Math.Abs(colors[2].r - R) * rWeight
            + System.Math.Abs(colors[2].g - G) * gWeight
            + System.Math.Abs(colors[2].b - B) * bWeight,
            System.Math.Abs(colors[3].r - R) * rWeight
            + System.Math.Abs(colors[3].g - G) * gWeight
            + System.Math.Abs(colors[3].b - B) * bWeight,
        ];

        int b0 = d[0] > d[3] ? 1 : 0;
        int b1 = d[1] > d[2] ? 1 : 0;
        int b2 = d[0] > d[2] ? 1 : 0;
        int b3 = d[1] > d[3] ? 1 : 0;
        int b4 = d[2] > d[3] ? 1 : 0;

        int x0 = b1 & b2;
        int x1 = b0 & b3;
        int x2 = b0 & b4;

        int idx = (x2 | ((x0 | x1) << 1));
        error = d[idx];
        return idx;
    }


    public static int ChooseClosestColor4AlphaCutoff(ReadOnlySpan<ColorRgb24> colors, GorgonColor color, float rWeight, float gWeight, float bWeight, int alphaCutoff, bool hasAlpha, out float error)
    {
        (int R, int G, int B, int A) = color.GetIntegerComponents();

        if (hasAlpha && A < alphaCutoff)
        {
            error = 0;
            return 3;
        }

        ReadOnlySpan<float> d = [
            System.Math.Abs(colors[0].r - R) * rWeight
            + System.Math.Abs(colors[0].g - G) * gWeight
            + System.Math.Abs(colors[0].b - B) * bWeight,
            System.Math.Abs(colors[1].r - R) * rWeight
            + System.Math.Abs(colors[1].g - G) * gWeight
            + System.Math.Abs(colors[1].b - B) * bWeight,
            System.Math.Abs(colors[2].r - R) * rWeight
            + System.Math.Abs(colors[2].g - G) * gWeight
            + System.Math.Abs(colors[2].b - B) * bWeight,

            hasAlpha ? 999 :
            System.Math.Abs(colors[3].r - R) * rWeight
            + System.Math.Abs(colors[3].g - G) * gWeight
            + System.Math.Abs(colors[3].b - B) * bWeight,
        ];

        int b0 = d[0] > d[2] ? 1 : 0;
        int b1 = d[1] > d[3] ? 1 : 0;
        int b2 = d[0] > d[3] ? 1 : 0;
        int b3 = d[1] > d[2] ? 1 : 0;
        int nb3 = d[1] > d[2] ? 0 : 1;
        int b4 = d[0] > d[1] ? 1 : 0;
        int b5 = d[2] > d[3] ? 1 : 0;

        int idx = (nb3 & b4) | (b2 & b5) | (((b0 & b3) | (b1 & b2)) << 1);

        error = d[idx];
        return idx;
    }

    public static int ChooseClosestColor(Span<ColorRgb24> colors, GorgonColor color)
    {
        (int R, int G, int B, int _) = color.GetIntegerComponents();

        int closest = 0;
        int closestError =
            System.Math.Abs(colors[0].r - R)
            + System.Math.Abs(colors[0].g - G)
            + System.Math.Abs(colors[0].b - B);

        for (int i = 1; i < colors.Length; i++)
        {
            int error =
                System.Math.Abs(colors[i].r - R)
                + System.Math.Abs(colors[i].g - G)
                + System.Math.Abs(colors[i].b - B);
            if (error < closestError)
            {
                closest = i;
                closestError = error;
            }
        }
        return closest;
    }

    public static int ChooseClosestColor(Span<ColorRgba32> colors, GorgonColor color)
    {
        (int R, int G, int B, int A) = color.GetIntegerComponents();

        int closest = 0;
        int closestError =
            System.Math.Abs(colors[0].r - R)
            + System.Math.Abs(colors[0].g - G)
            + System.Math.Abs(colors[0].b - B)
            + System.Math.Abs(colors[0].a - A);

        for (int i = 1; i < colors.Length; i++)
        {
            int error =
                System.Math.Abs(colors[i].r - R)
                + System.Math.Abs(colors[i].g - G)
                + System.Math.Abs(colors[i].b - B)
                + System.Math.Abs(colors[i].a - A);
            if (error < closestError)
            {
                closest = i;
                closestError = error;
            }
        }
        return closest;
    }

    public static int ChooseClosestColorAlphaCutOff(Span<ColorRgba32> colors, GorgonColor color, byte alphaCutOff = 255 / 2)
    {
        (int R, int G, int B, int A) = color.GetIntegerComponents();

        if (A <= alphaCutOff)
        {
            return 3;
        }

        int closest = 0;
        int closestError =
            System.Math.Abs(colors[0].r - R)
            + System.Math.Abs(colors[0].g - G)
            + System.Math.Abs(colors[0].b - B);

        for (int i = 1; i < colors.Length; i++)
        {
            if (i == 3)
            {
                continue; // Skip transparent
            }

            int error =
                System.Math.Abs(colors[i].r - R)
                + System.Math.Abs(colors[i].g - G)
                + System.Math.Abs(colors[i].b - B);
            if (error < closestError)
            {
                closest = i;
                closestError = error;
            }
        }
        return closest;
    }

    public static int ChooseClosestColor(Span<ColorYCbCr> colors, ColorYCbCr color, float luminanceMultiplier = 4)
    {
        int closest = 0;
        float closestError = 0;
        bool first = true;

        for (int i = 0; i < colors.Length; i++)
        {
            float error = System.Math.Abs(colors[i].y - color.y) * luminanceMultiplier
                          + System.Math.Abs(colors[i].cb - color.cb)
                          + System.Math.Abs(colors[i].cr - color.cr);
            if (first)
            {
                closestError = error;
                first = false;
            }
            else if (error < closestError)
            {
                closest = i;
                closestError = error;
            }
        }
        return closest;
    }

    public static int ChooseClosestColor(Span<ColorYCbCr> colors, int color, float luminanceMultiplier = 4)
        => ChooseClosestColor(colors, new ColorYCbCr(GorgonColor.FromRGBA(color)), luminanceMultiplier);
}
