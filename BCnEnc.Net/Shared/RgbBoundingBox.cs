﻿using System;
using Gorgon.Graphics;

namespace BCnEncoder.Shared;

/// <summary>
/// Calculate the bounding box of rgb values as described in
/// "Real-Time DXT Compression by J.M.P. van Waveren, 2006, Id Software, Inc." and
/// "Real-Time YCoCg-DXT Compression J.M.P. van Waveren,  Ignacio Castaño id Software, Inc. NVIDIA Corp."
/// </summary>
internal static class RgbBoundingBox
{

    public static void Create565(ReadOnlySpan<GorgonColor> colors, out ColorRgb565 min, out ColorRgb565 max)
    {
        const int colorInsetShift = 4;
        const int c565_5_mask = 0xF8;
        const int c565_6_mask = 0xFC;

        int minR = 255,
            minG = 255,
            minB = 255;
        int maxR = 0,
            maxG = 0,
            maxB = 0;

        for (int i = 0; i < colors.Length; i++)
        {
            (int R, int G, int B, int _) = colors[i].GetIntegerComponents();

            if (R < minR)
            {
                minR = R;
            }

            if (G < minG)
            {
                minG = G;
            }

            if (B < minB)
            {
                minB = B;
            }

            if (R > maxR)
            {
                maxR = R;
            }

            if (G > maxG)
            {
                maxG = G;
            }

            if (B > maxB)
            {
                maxB = B;
            }
        }

        int insetR = (maxR - minR) >> colorInsetShift;
        int insetG = (maxG - minG) >> colorInsetShift;
        int insetB = (maxB - minB) >> colorInsetShift;

        // Inset by 1/16th
        minR = ((minR << colorInsetShift) + insetR) >> colorInsetShift;
        minG = ((minG << colorInsetShift) + insetG) >> colorInsetShift;
        minB = ((minB << colorInsetShift) + insetB) >> colorInsetShift;

        maxR = ((maxR << colorInsetShift) - insetR) >> colorInsetShift;
        maxG = ((maxG << colorInsetShift) - insetG) >> colorInsetShift;
        maxB = ((maxB << colorInsetShift) - insetB) >> colorInsetShift;

        minR = (minR >= 0) ? minR : 0;
        minG = (minG >= 0) ? minG : 0;
        minB = (minB >= 0) ? minB : 0;

        maxR = (maxR <= 255) ? maxR : 255;
        maxG = (maxG <= 255) ? maxG : 255;
        maxB = (maxB <= 255) ? maxB : 255;

        // Optimal rounding
        minR = (minR & c565_5_mask) | (minR >> 5);
        minG = (minG & c565_6_mask) | (minG >> 6);
        minB = (minB & c565_5_mask) | (minB >> 5);

        maxR = (maxR & c565_5_mask) | (maxR >> 5);
        maxG = (maxG & c565_6_mask) | (maxG >> 6);
        maxB = (maxB & c565_5_mask) | (maxB >> 5);

        min = new ColorRgb565((byte)minR, (byte)minG, (byte)minB);
        max = new ColorRgb565((byte)maxR, (byte)maxG, (byte)maxB);
    }

    public static void Create565AlphaCutoff(ReadOnlySpan<GorgonColor> colors, out ColorRgb565 min, out ColorRgb565 max, int alphaCutoff = 128)
    {
        const int colorInsetShift = 4;
        const int c565_5_mask = 0xF8;
        const int c565_6_mask = 0xFC;

        int minR = 255,
            minG = 255,
            minB = 255;
        int maxR = 0,
            maxG = 0,
            maxB = 0;

        for (int i = 0; i < colors.Length; i++)
        {
            (int R, int G, int B, int A) = colors[i].GetIntegerComponents();

            if (A < alphaCutoff)
            {
                continue;
            }

            if (R < minR)
            {
                minR = R;
            }

            if (G < minG)
            {
                minG = G;
            }

            if (B < minB)
            {
                minB = B;
            }

            if (R > maxR)
            {
                maxR = R;
            }

            if (G > maxG)
            {
                maxG = G;
            }

            if (B > maxB)
            {
                maxB = B;
            }
        }

        int insetR = (maxR - minR) >> colorInsetShift;
        int insetG = (maxG - minG) >> colorInsetShift;
        int insetB = (maxB - minB) >> colorInsetShift;

        minR = ((minR << colorInsetShift) + insetR) >> colorInsetShift;
        minG = ((minG << colorInsetShift) + insetG) >> colorInsetShift;
        minB = ((minB << colorInsetShift) + insetB) >> colorInsetShift;

        maxR = ((maxR << colorInsetShift) - insetR) >> colorInsetShift;
        maxG = ((maxG << colorInsetShift) - insetG) >> colorInsetShift;
        maxB = ((maxB << colorInsetShift) - insetB) >> colorInsetShift;

        minR = (minR >= 0) ? minR : 0;
        minG = (minG >= 0) ? minG : 0;
        minB = (minB >= 0) ? minB : 0;

        maxR = (maxR <= 255) ? maxR : 255;
        maxG = (maxG <= 255) ? maxG : 255;
        maxB = (maxB <= 255) ? maxB : 255;

        minR = (minR & c565_5_mask) | (minR >> 5);
        minG = (minG & c565_6_mask) | (minG >> 6);
        minB = (minB & c565_5_mask) | (minB >> 5);

        maxR = (maxR & c565_5_mask) | (maxR >> 5);
        maxG = (maxG & c565_6_mask) | (maxG >> 6);
        maxB = (maxB & c565_5_mask) | (maxB >> 5);

        min = new ColorRgb565((byte)minR, (byte)minG, (byte)minB);
        max = new ColorRgb565((byte)maxR, (byte)maxG, (byte)maxB);
    }

    public static void Create565a(ReadOnlySpan<GorgonColor> colors, out ColorRgb565 min, out ColorRgb565 max, out byte minAlpha, out byte maxAlpha)
    {
        const int colorInsetShift = 4;
        const int alphaInsetShift = 5;
        const int c565_5_mask = 0xF8;
        const int c565_6_mask = 0xFC;

        int minR = 255,
            minG = 255,
            minB = 255,
            minA = 255;
        int maxR = 0,
            maxG = 0,
            maxB = 0,
            maxA = 0;

        for (int i = 0; i < colors.Length; i++)
        {
            (int R, int G, int B, int A) = colors[i].GetIntegerComponents();

            if (R < minR)
            {
                minR = R;
            }

            if (G < minG)
            {
                minG = G;
            }

            if (B < minB)
            {
                minB = B;
            }

            if (A < minA)
            {
                minA = A;
            }

            if (R > maxR)
            {
                maxR = R;
            }

            if (G > maxG)
            {
                maxG = G;
            }

            if (B > maxB)
            {
                maxB = B;
            }

            if (A > maxA)
            {
                maxA = A;
            }
        }


        int insetR = (maxR - minR) >> colorInsetShift;
        int insetG = (maxG - minG) >> colorInsetShift;
        int insetB = (maxB - minB) >> colorInsetShift;
        int insetA = (maxA - minA) >> alphaInsetShift;

        minR = ((minR << colorInsetShift) + insetR) >> colorInsetShift;
        minG = ((minG << colorInsetShift) + insetG) >> colorInsetShift;
        minB = ((minB << colorInsetShift) + insetB) >> colorInsetShift;
        minA = ((minA << alphaInsetShift) + insetA) >> alphaInsetShift;

        maxR = ((maxR << colorInsetShift) - insetR) >> colorInsetShift;
        maxG = ((maxG << colorInsetShift) - insetG) >> colorInsetShift;
        maxB = ((maxB << colorInsetShift) - insetB) >> colorInsetShift;
        maxA = ((maxA << alphaInsetShift) - insetA) >> alphaInsetShift;

        minR = (minR >= 0) ? minR : 0;
        minG = (minG >= 0) ? minG : 0;
        minB = (minB >= 0) ? minB : 0;
        minA = (minA >= 0) ? minA : 0;

        maxR = (maxR <= 255) ? maxR : 255;
        maxG = (maxG <= 255) ? maxG : 255;
        maxB = (maxB <= 255) ? maxB : 255;
        maxA = (maxA <= 255) ? maxA : 255;

        minR = (minR & c565_5_mask) | (minR >> 5);
        minG = (minG & c565_6_mask) | (minG >> 6);
        minB = (minB & c565_5_mask) | (minB >> 5);

        maxR = (maxR & c565_5_mask) | (maxR >> 5);
        maxG = (maxG & c565_6_mask) | (maxG >> 6);
        maxB = (maxB & c565_5_mask) | (maxB >> 5);

        min = new ColorRgb565((byte)minR, (byte)minG, (byte)minB);
        max = new ColorRgb565((byte)maxR, (byte)maxG, (byte)maxB);
        minAlpha = (byte)minA;
        maxAlpha = (byte)maxA;
    }
}
