using System;
using Gorgon.Graphics;

namespace BCnEncoder.Shared
{
    internal struct ColorRgb565 : IEquatable<ColorRgb565>
	{
        public bool Equals(ColorRgb565 other) => data == other.data;

        public override bool Equals(object obj) => obj is ColorRgb565 other && Equals(other);

        public override int GetHashCode() => data.GetHashCode();

        private const ushort RedMask = 0b11111_000000_00000;
		private const int RedShift = 11;
		private const ushort GreenMask = 0b00000_111111_00000;
		private const int GreenShift = 5;
		private const ushort BlueMask = 0b00000_000000_11111;

		public ushort data;

		public byte R {
			get {
				int r5 = ((data & RedMask) >> RedShift);
				return (byte)((r5 << 3) | (r5 >> 2));
			}
			set {
				int r5 = value >> 3;
				data = (ushort)(data & ~RedMask);
				data = (ushort)(data | (r5 << RedShift));
			}
		}

		public byte G {
			get {
				int g6 = ((data & GreenMask) >> GreenShift);
				return (byte)((g6 << 2) | (g6 >> 4));
			}
			set {
				int g6 = value >> 2;
				data = (ushort)(data & ~GreenMask);
				data = (ushort)(data | (g6 << GreenShift));
			}
		}

		public byte B {
			get {
				int b5 = (data & BlueMask);
				return (byte)((b5 << 3) | (b5 >> 2));
			}
			set {
				int b5 = value >> 3;
				data = (ushort)(data & ~BlueMask);
				data = (ushort)(data | b5);
			}
		}

		public int RawR
        {
            get => ((data & RedMask) >> RedShift);
            set
            {
                if (value > 31)
                {
                    value = 31;
                }

                if (value < 0)
                {
                    value = 0;
                }

                data = (ushort)(data & ~RedMask);
                data = (ushort)(data | ((value) << RedShift));
            }
        }

        public int RawG
        {
            get => ((data & GreenMask) >> GreenShift);
            set
            {
                if (value > 63)
                {
                    value = 63;
                }

                if (value < 0)
                {
                    value = 0;
                }

                data = (ushort)(data & ~GreenMask);
                data = (ushort)(data | ((value) << GreenShift));
            }
        }

        public int RawB
        {
            get => (data & BlueMask);
            set
            {
                if (value > 31)
                {
                    value = 31;
                }

                if (value < 0)
                {
                    value = 0;
                }

                data = (ushort)(data & ~BlueMask);
                data = (ushort)(data | value);
            }
        }

        public ColorRgb565(byte r, byte g, byte b)
		{
			data = 0;
			R = r;
			G = g;
			B = b;
		}

        public ColorRgb24 ToColorRgb24() => new ColorRgb24(R, G, B);

        public override string ToString() => $"r : {R} g : {G} b : {B}";
    }

	internal struct ColorRgba32 : IEquatable<ColorRgba32>
	{
		public byte r, g, b, a;
		public ColorRgba32(byte r, byte g, byte b, byte a)
		{
			this.r = r;
			this.g = g;
			this.b = b;
			this.a = a;
		}

        public bool Equals(ColorRgba32 other) => r == other.r && g == other.g && b == other.b && a == other.a;

        public override bool Equals(object obj) => obj is ColorRgba32 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(r, g, b, a);

        /// <summary>
        /// Component-wise left shift
        /// </summary>
        public static ColorRgba32 operator <<(ColorRgba32 left, int right) => new ColorRgba32(
                ByteHelper.ClampToByte((left.r << right)),
                ByteHelper.ClampToByte((left.g << right)),
                ByteHelper.ClampToByte((left.b << right)),
                ByteHelper.ClampToByte((left.a << right))
            );

        /// <summary>
        /// Component-wise bitwise OR operation
        /// </summary>
        public static ColorRgba32 operator |(ColorRgba32 left, int right) => new ColorRgba32(
                ByteHelper.ClampToByte((left.r | right)),
                ByteHelper.ClampToByte((left.g | right)),
                ByteHelper.ClampToByte((left.b | right)),
                ByteHelper.ClampToByte((left.a | right))
            );

        public override string ToString() => $"r : {r} g : {g} b : {b} a : {a}";

		public GorgonColor ToGorgonColor() => new GorgonColor(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
    }

	internal struct ColorRgb24 : IEquatable<ColorRgb24>
	{
		public byte r, g, b;
		public ColorRgb24(byte r, byte g, byte b)
		{
			this.r = r;
			this.g = g;
			this.b = b;
		}

		public ColorRgb24(GorgonColor color) {
			r = (byte)(color.Red * 255.0f);
			g = (byte)(color.Green * 255.0f);
			b = (byte)(color.Blue * 255.0f);
		}

        public bool Equals(ColorRgb24 other) => r == other.r && g == other.g && b == other.b;

        public override bool Equals(object obj) => obj is ColorRgb24 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(r, g, b);

        public static ColorRgb24 operator +(ColorRgb24 left, ColorRgb24 right) => new ColorRgb24(
                ByteHelper.ClampToByte(left.r + right.r),
                ByteHelper.ClampToByte(left.g + right.g),
                ByteHelper.ClampToByte(left.b + right.b));

        public static ColorRgb24 operator *(ColorRgb24 left, double right) => new ColorRgb24(
                ByteHelper.ClampToByte((int)(left.r * right)),
                ByteHelper.ClampToByte((int)(left.g * right)),
                ByteHelper.ClampToByte((int)(left.b * right))
            );

        public override string ToString() => $"r : {r} g : {g} b : {b}";

		public GorgonColor ToGorgonColor() => new GorgonColor(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
	}

	internal struct ColorYCbCr
	{
		public float y;
		public float cb;
		public float cr;

		public ColorYCbCr(float y, float cb, float cr)
		{
			this.y = y;
			this.cb = cb;
			this.cr = cr;
		}

		public ColorYCbCr(ColorRgba32 rgba)
		{
			float fr = (float)rgba.r / 255;
			float fg = (float)rgba.g / 255;
			float fb = (float)rgba.b / 255;

			y = (0.2989f * fr + 0.5866f * fg + 0.1145f * fb);
			cb = (-0.1687f * fr - 0.3313f * fg + 0.5000f * fb);
			cr = (0.5000f * fr - 0.4184f * fg - 0.0816f * fb);
		}

		public ColorYCbCr(GorgonColor rgb)
		{
			float fr = rgb.Red;
			float fg = rgb.Green;
			float fb = rgb.Blue;

			y = (0.2989f * fr + 0.5866f * fg + 0.1145f * fb);
			cb = (-0.1687f * fr - 0.3313f * fg + 0.5000f * fb);
			cr = (0.5000f * fr - 0.4184f * fg - 0.0816f * fb);
		}

		public ColorRgb565 ToColorRgb565() {
			float r = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 0.0000 * cb + 1.4022 * cr)));
			float g = Math.Max(0.0f, Math.Min(1.0f, (float)(y - 0.3456 * cb - 0.7145 * cr)));
			float b = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 1.7710 * cb + 0.0000 * cr)));

			return new ColorRgb565((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
		}

		public override string ToString() {
			float r = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 0.0000 * cb + 1.4022 * cr)));
			float g = Math.Max(0.0f, Math.Min(1.0f, (float)(y - 0.3456 * cb - 0.7145 * cr)));
			float b = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 1.7710 * cb + 0.0000 * cr)));

			return $"r : {r * 255} g : {g * 255} b : {b * 255}";
		}

		public float CalcDistWeighted(ColorYCbCr other, float yWeight = 4) {
			float dy = (y - other.y) * (y - other.y) * yWeight;
			float dcb = (cb - other.cb) * (cb - other.cb);
			float dcr = (cr - other.cr) * (cr - other.cr);

			return (float)Math.Sqrt(dy + dcb + dcr);
		}
	}

	internal struct ColorYCbCrAlpha
	{
		public float y;
		public float cb;
		public float cr;
		public float alpha;

		public ColorYCbCrAlpha(float y, float cb, float cr, float alpha)
		{
			this.y = y;
			this.cb = cb;
			this.cr = cr;
			this.alpha = alpha;
		}

		public ColorYCbCrAlpha(ColorRgba32 rgba)
		{
			float fr = (float)rgba.r / 255;
			float fg = (float)rgba.g / 255;
			float fb = (float)rgba.b / 255;

			y = (0.2989f * fr + 0.5866f * fg + 0.1145f * fb);
			cb = (-0.1687f * fr - 0.3313f * fg + 0.5000f * fb);
			cr = (0.5000f * fr - 0.4184f * fg - 0.0816f * fb);
			alpha = rgba.a / 255f;
		}

		public ColorYCbCrAlpha(GorgonColor rgb)
		{
			float fr = rgb.Red;
			float fg = rgb.Green;
			float fb = rgb.Blue;

			y = (0.2989f * fr + 0.5866f * fg + 0.1145f * fb);
			cb = (-0.1687f * fr - 0.3313f * fg + 0.5000f * fb);
			cr = (0.5000f * fr - 0.4184f * fg - 0.0816f * fb);
			alpha = rgb.Alpha;
		}

		public override string ToString() {
			float r = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 0.0000 * cb + 1.4022 * cr)));
			float g = Math.Max(0.0f, Math.Min(1.0f, (float)(y - 0.3456 * cb - 0.7145 * cr)));
			float b = Math.Max(0.0f, Math.Min(1.0f, (float)(y + 1.7710 * cb + 0.0000 * cr)));

			return $"r : {r * 255} g : {g * 255} b : {b * 255}";
		}

		public float CalcDistWeighted(ColorYCbCrAlpha other, float yWeight = 4, float aWeight = 1) {
			float dy = (y - other.y) * (y - other.y) * yWeight;
			float dcb = (cb - other.cb) * (cb - other.cb);
			float dcr = (cr - other.cr) * (cr - other.cr);
			float da = (alpha - other.alpha) * (alpha - other.alpha) * aWeight;

			return (float)Math.Sqrt(dy + dcb + dcr + da);
		}
	}

	internal struct ColorXyz {
		public float x;
		public float y;
		public float z;

		public ColorXyz(float x, float y, float z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}

        public ColorXyz(ColorRgb24 color) => this = ColorToXyz(color);

        public static ColorXyz ColorToXyz(ColorRgb24 color) {
			float r = PivotRgb(color.r / 255.0f);
			float g = PivotRgb(color.g / 255.0f);
			float b = PivotRgb(color.b / 255.0f);

			// Observer. = 2°, Illuminant = D65
			return new ColorXyz(r * 0.4124f + g * 0.3576f + b * 0.1805f, r * 0.2126f + g * 0.7152f + b * 0.0722f, r * 0.0193f + g * 0.1192f + b * 0.9505f);
		}

        private static float PivotRgb(float n) => (n > 0.04045f ? (float)Math.Pow((n + 0.055f) / 1.055f, 2.4f) : n / 12.92f) * 100;
    }


	internal struct ColorLab {
		public float l;
		public float a;
		public float b;

		public ColorLab(float l, float a, float b) {
			this.l = l;
			this.a = a;
			this.b = b;
		}

        public ColorLab(GorgonColor color) => this = ColorToLab(new ColorRgb24(color));

        public static ColorLab ColorToLab(ColorRgb24 color) {
			var xyz = new ColorXyz(color);
			return XyzToLab(xyz);
		}


		public static ColorLab XyzToLab(ColorXyz xyz) {
			float REF_X = 95.047f; // Observer= 2°, Illuminant= D65
			float REF_Y = 100.000f;
			float REF_Z = 108.883f;

			float x = PivotXyz(xyz.x / REF_X);
			float y = PivotXyz(xyz.y / REF_Y);
			float z = PivotXyz(xyz.z / REF_Z);

			return new ColorLab(116 * y - 16, 500 * (x - y), 200 * (y - z));
		}

		private static float PivotXyz(float n) {
			float i = (float)Math.Pow(n, (1.0 / 3.0));
			return n > 0.008856f ? i : 7.787f * n + 16 / 116f;
		}
	}
}