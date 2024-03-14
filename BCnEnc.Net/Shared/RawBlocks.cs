using System;
using Gorgon.Graphics;

namespace BCnEncoder.Shared;


internal class RawBlock4X4Rgba32
{
    private readonly GorgonColor[] _p = new GorgonColor[16];

    public Span<GorgonColor> AsSpan => _p;

    public GorgonColor this[int x, int y]
    {
        get => _p[x + y * 4];
        set => _p[x + y * 4] = value;
    }

    public float CalculateYCbCrAlphaError(RawBlock4X4Rgba32 other, float yMultiplier = 2, float alphaMultiplier = 1)
    {
        float yError = 0;
        float cbError = 0;
        float crError = 0;
        float alphaError = 0;

        for (int i = 0; i < _p.Length; i++)
        {
            var col1 = new ColorYCbCrAlpha(_p[i]);
            var col2 = new ColorYCbCrAlpha(other._p[i]);

            float ye = (col1.y - col2.y) * yMultiplier;
            float cbe = col1.cb - col2.cb;
            float cre = col1.cr - col2.cr;
            float ae = (col1.alpha - col2.alpha) * alphaMultiplier;

            yError += ye * ye;
            cbError += cbe * cbe;
            crError += cre * cre;
            alphaError += ae * ae;
        }

        float error = yError + cbError + crError + alphaError;
        return error;
    }

    public bool HasTransparentPixels()
    {
        for (int i = 0; i < _p.Length; i++)
        {
            if (_p[i].Alpha < 1.0f)
            {
                return true;
            }
        }
        return false;
    }
}
