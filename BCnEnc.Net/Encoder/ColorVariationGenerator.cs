﻿using System.Collections.Generic;
using BCnEncoder.Shared;

namespace BCnEncoder.Encoder;

internal static class ColorVariationGenerator
{

    private static readonly int[] _varPatternEp0R = new int[] { 1, 1, 0, 0, -1, 0, 0, -1, 1, -1, 1, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private static readonly int[] _varPatternEp0G = new int[] { 1, 0, 1, 0, 0, -1, 0, -1, 1, -1, 0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private static readonly int[] _varPatternEp0B = new int[] { 1, 0, 0, 1, 0, 0, -1, -1, 1, -1, 0, 0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0 };
    private static readonly int[] _varPatternEp1R = new int[] { -1, -1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, -1, 1, 0, 0, -1, 0, 0 };
    private static readonly int[] _varPatternEp1G = new int[] { -1, 0, -1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, -1, 0, 1, 0, 0, -1, 0 };
    private static readonly int[] _varPatternEp1B = new int[] { -1, 0, 0, -1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, -1, 0, 0, 1, 0, 0, -1 };
    public static int VarPatternCount => _varPatternEp0R.Length;

    public static (ColorRgb565, ColorRgb565) Variate565(ColorRgb565 c0, ColorRgb565 c1, int i)
    {
        int idx = i % _varPatternEp0R.Length;
        var newEp0 = new ColorRgb565();
        var newEp1 = new ColorRgb565();

        newEp0.RawR = ByteHelper.ClampToByte(c0.RawR + _varPatternEp0R[idx]);
        newEp0.RawG = ByteHelper.ClampToByte(c0.RawG + _varPatternEp0G[idx]);
        newEp0.RawB = ByteHelper.ClampToByte(c0.RawB + _varPatternEp0B[idx]);

        newEp1.RawR = ByteHelper.ClampToByte(c1.RawR + _varPatternEp1R[idx]);
        newEp1.RawG = ByteHelper.ClampToByte(c1.RawG + _varPatternEp1G[idx]);
        newEp1.RawB = ByteHelper.ClampToByte(c1.RawB + _varPatternEp1B[idx]);

        return (newEp0, newEp1);
    }


    public static List<ColorRgb565> GenerateVariationsSidewaysMax(int variations, ColorYCbCr min, ColorYCbCr max)
    {
        var colors = new List<ColorRgb565>
        {
            min.ToColorRgb565(),
            max.ToColorRgb565()
        };

        for (int i = 0; i < variations; i++)
        {
            max.y -= 0.05f;
            min.y += 0.05f;

            var ma = max.ToColorRgb565();
            var mi = min.ToColorRgb565();
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }

            if (!colors.Contains(mi))
            {
                colors.Add(mi);
            }

            //variate reds in max
            ma.RawR += 1;
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }
            ma.RawR -= 2;
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }

            //variate blues in max
            ma.RawR += 1;
            ma.RawB += 1;
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }
            ma.RawB -= 2;
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }

        }

        return colors;
    }

    public static List<ColorRgb565> GenerateVariationsSidewaysMinMax(int variations, ColorYCbCr min, ColorYCbCr max)
    {
        var colors = new List<ColorRgb565>
        {
            min.ToColorRgb565(),
            max.ToColorRgb565()
        };

        for (int i = 0; i < variations; i++)
        {
            max.y -= 0.05f;
            min.y += 0.05f;

            var ma = max.ToColorRgb565();
            var mi = min.ToColorRgb565();
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }

            if (!colors.Contains(mi))
            {
                colors.Add(mi);
            }

            //variate reds in max
            ma.RawR += 1;
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }
            ma.RawR -= 2;
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }

            //variate blues in max
            ma.RawR += 1;
            ma.RawB += 1;
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }
            ma.RawB -= 2;
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }

            //variate reds in min
            mi.RawR += 1;
            if (!colors.Contains(mi))
            {
                colors.Add(mi);
            }
            ma.RawR -= 2;
            if (!colors.Contains(ma))
            {
                colors.Add(ma);
            }

            //variate blues in min
            mi.RawR += 1;
            mi.RawB += 1;
            if (!colors.Contains(mi))
            {
                colors.Add(mi);
            }
            mi.RawB -= 2;
            if (!colors.Contains(mi))
            {
                colors.Add(mi);
            }

        }

        return colors;
    }
}
