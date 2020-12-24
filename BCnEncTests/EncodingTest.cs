using System.IO;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace BCnEncTests
{
	public class Bc1GradientTest
	{
		private readonly ITestOutputHelper _output;

        public Bc1GradientTest(ITestOutputHelper output) => this._output = output;

        [Fact]
		public void Bc1GradientBestQuality()
		{
            Image<Rgba32> image = ImageLoader.testGradient1;
			
			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1,
				CompressionQuality.BestQuality, 
				"encoding_bc1_gradient_bestQuality.ktx",
				_output);
		}

		[Fact]
		public void Bc1GradientBalanced()
		{
            Image<Rgba32> image = ImageLoader.testGradient1;

			
			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1,
				CompressionQuality.Balanced, 
				"encoding_bc1_gradient_balanced.ktx",
				_output);
		}

		[Fact]
		public void Bc1GradientFast()
		{
            Image<Rgba32> image = ImageLoader.testGradient1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1,
				CompressionQuality.Fast, 
				"encoding_bc1_gradient_fast.ktx",
				_output);
		}
	}

	public class Bc1DiffuseTest
	{

		private readonly ITestOutputHelper output;

        public Bc1DiffuseTest(ITestOutputHelper output) => this.output = output;


        [Fact]
		public void Bc1DiffuseBestQuality()
		{
            Image<Rgba32> image = ImageLoader.testDiffuse1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1,
				CompressionQuality.BestQuality, 
				"encoding_bc1_diffuse_bestQuality.ktx",
				output);
		}

		[Fact]
		public void Bc1DiffuseBalanced()
		{
            Image<Rgba32> image = ImageLoader.testDiffuse1;
			
			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1,
				CompressionQuality.Balanced, 
				"encoding_bc1_diffuse_balanced.ktx",
				output);
		}

		[Fact]
		public void Bc1DiffuseFast()
		{
            Image<Rgba32> image = ImageLoader.testDiffuse1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1,
				CompressionQuality.Fast, 
				"encoding_bc1_diffuse_fast.ktx",
				output);
		}


	}


	public class Bc1BlurryTest
	{
		private readonly ITestOutputHelper output;

        public Bc1BlurryTest(ITestOutputHelper output) => this.output = output;

        [Fact]
		public void Bc1BlurBestQuality()
		{
            Image<Rgba32> image = ImageLoader.testBlur1;
			
			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1,
				CompressionQuality.BestQuality, 
				"encoding_bc1_blur_bestQuality.ktx",
				output);
		}

		[Fact]
		public void Bc1BlurBalanced()
		{
            Image<Rgba32> image = ImageLoader.testBlur1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1,
				CompressionQuality.Balanced, 
				"encoding_bc1_blur_balanced.ktx",
				output);
		}

		[Fact]
		public void Bc1BlurFast()
		{
            Image<Rgba32> image = ImageLoader.testBlur1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1,
				CompressionQuality.Fast, 
				"encoding_bc1_blur_fast.ktx",
				output);
		}

	}

	public class Bc1ASpriteTest
	{

		private readonly ITestOutputHelper output;

        public Bc1ASpriteTest(ITestOutputHelper output) => this.output = output;


        [Fact]
		public void Bc1aSpriteBestQuality()
		{
            Image<Rgba32> image = ImageLoader.testTransparentSprite1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1WithAlpha,
				CompressionQuality.BestQuality, 
				"encoding_bc1a_sprite_bestQuality.ktx",
				output);
		}

		[Fact]
		public void Bc1aSpriteBalanced()
		{
            Image<Rgba32> image = ImageLoader.testTransparentSprite1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1WithAlpha,
				CompressionQuality.Balanced, 
				"encoding_bc1a_sprite_balanced.ktx",
				output);
		}

		[Fact]
		public void Bc1aSpriteFast()
		{
            Image<Rgba32> image = ImageLoader.testTransparentSprite1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC1WithAlpha,
				CompressionQuality.Fast, 
				"encoding_bc1a_sprite_fast.ktx",
				output);
		}

	}

	public class Bc2GradientTest
	{

		private readonly ITestOutputHelper output;

        public Bc2GradientTest(ITestOutputHelper output) => this.output = output;


        [Fact]
		public void Bc2GradientBestQuality()
		{
            Image<Rgba32> image = ImageLoader.testAlphaGradient1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC2,
				CompressionQuality.BestQuality, 
				"encoding_bc2_gradient_bestQuality.ktx",
				output);
		}

		[Fact]
		public void Bc2GradientBalanced()
		{
            Image<Rgba32> image = ImageLoader.testAlphaGradient1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC2,
				CompressionQuality.Balanced, 
				"encoding_bc2_gradient_balanced.ktx",
				output);
		}

		[Fact]
		public void Bc2GradientFast()
		{
            Image<Rgba32> image = ImageLoader.testAlphaGradient1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC2,
				CompressionQuality.Fast, 
				"encoding_bc2_gradient_fast.ktx",
				output);
		}

	}

	public class Bc3GradientTest
	{

		private readonly ITestOutputHelper output;

        public Bc3GradientTest(ITestOutputHelper output) => this.output = output;


        [Fact]
		public void Bc3GradientBestQuality()
		{
            Image<Rgba32> image = ImageLoader.testAlphaGradient1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC3,
				CompressionQuality.BestQuality, 
				"encoding_bc3_gradient_bestQuality.ktx",
				output);
		}

		[Fact]
		public void Bc3GradientBalanced()
		{
            Image<Rgba32> image = ImageLoader.testAlphaGradient1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC3,
				CompressionQuality.Balanced, 
				"encoding_bc3_gradient_balanced.ktx",
				output);
		}

		[Fact]
		public void Bc3GradientFast()
		{
            Image<Rgba32> image = ImageLoader.testAlphaGradient1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC3,
				CompressionQuality.Fast, 
				"encoding_bc3_gradient_fast.ktx",
				output);
		}

	}

	public class Bc4RedTest
	{

		private readonly ITestOutputHelper output;

        public Bc4RedTest(ITestOutputHelper output) => this.output = output;


        [Fact]
		public void Bc4RedBestQuality()
		{
            Image<Rgba32> image = ImageLoader.testHeight1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC4,
				CompressionQuality.BestQuality, 
				"encoding_bc4_red_bestQuality.ktx",
				output);
		}

		[Fact]
		public void Bc4RedBalanced()
		{
            Image<Rgba32> image = ImageLoader.testHeight1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC4,
				CompressionQuality.Balanced, 
				"encoding_bc4_red_balanced.ktx",
				output);
		}

		[Fact]
		public void Bc4RedFast()
		{
            Image<Rgba32> image = ImageLoader.testHeight1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC4,
				CompressionQuality.Fast, 
				"encoding_bc4_red_fast.ktx",
				output);
		}

	}

	public class Bc5RedGreenTest
	{

		private readonly ITestOutputHelper output;

        public Bc5RedGreenTest(ITestOutputHelper output) => this.output = output;


        [Fact]
		public void Bc5RedGreenBestQuality()
		{
            Image<Rgba32> image = ImageLoader.testRedGreen1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC5,
				CompressionQuality.BestQuality, 
				"encoding_bc5_red_green_bestQuality.ktx",
				output);
		}

		[Fact]
		public void Bc5RedGreenBalanced()
		{
            Image<Rgba32> image = ImageLoader.testRedGreen1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC5,
				CompressionQuality.Balanced, 
				"encoding_bc5_red_green_balanced.ktx",
				output);
		}

		[Fact]
		public void Bc5RedGreenFast()
		{
            Image<Rgba32> image = ImageLoader.testRedGreen1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC5,
				CompressionQuality.Fast, 
				"encoding_bc5_red_green_fast.ktx",
				output);
		}
	}

	public class Bc7RgbTest
	{

		private readonly ITestOutputHelper output;

        public Bc7RgbTest(ITestOutputHelper output) => this.output = output;


        [Fact]
		public void Bc7RgbBestQuality()
		{
            Image<Rgba32> image = ImageLoader.testRgbHard1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC7,
				CompressionQuality.BestQuality, 
				"encoding_bc7_rgb_bestQuality.ktx",
				output);
		}

		[Fact]
		public void Bc7RgbBalanced()
		{
            Image<Rgba32> image = ImageLoader.testRgbHard1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC7,
				CompressionQuality.Balanced, 
				"encoding_bc7_rgb_balanced.ktx",
				output);
		}

		[Fact]
		public void Bc7LennaBalanced()
		{
            Image<Rgba32> image = ImageLoader.testLenna;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC7,
				CompressionQuality.Balanced, 
				"encoding_bc7_lenna_balanced.ktx",
				output);
		}

		[Fact]
		public void Bc7RgbFast()
		{
            Image<Rgba32> image = ImageLoader.testRgbHard1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC7,
				CompressionQuality.Fast, 
				"encoding_bc7_rgb_fast.ktx",
				output);
		}
	}

	public class Bc7RgbaTest
	{

		private readonly ITestOutputHelper output;

        public Bc7RgbaTest(ITestOutputHelper output) => this.output = output;


        [Fact]
		public void Bc7RgbaBestQuality()
		{
            Image<Rgba32> image = ImageLoader.testAlpha1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC7,
				CompressionQuality.BestQuality, 
				"encoding_bc7_rgba_bestQuality.ktx",
				output);
		}

		[Fact]
		public void Bc7RgbaBalanced()
		{
            Image<Rgba32> image = ImageLoader.testAlpha1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC7,
				CompressionQuality.Balanced, 
				"encoding_bc7_rgba_balanced.ktx",
				output);
		}

		[Fact]
		public void Bc7RgbaFast()
		{
            Image<Rgba32> image = ImageLoader.testAlpha1;

			TestHelper.ExecuteEncodingTest(image,
				CompressionFormat.BC7,
				CompressionQuality.Fast, 
				"encoding_bc7_rgba_fast.ktx",
				output);
		}
	}

	public class CubemapTest
	{

		[Fact]
		public void WriteCubeMapFile()
		{
            Image<Rgba32>[] images = ImageLoader.testCubemap;

			string filename = "encoding_bc1_cubemap.ktx";

			var encoder = new BcEncoder();
			encoder.OutputOptions.quality = CompressionQuality.Fast;
			encoder.OutputOptions.generateMipMaps = true;
			encoder.OutputOptions.format = CompressionFormat.BC1;

			using FileStream fs = File.OpenWrite(filename);
			encoder.EncodeCubeMap(images[0],images[1],images[2],images[3],images[4],images[5], fs);
			fs.Close();
		}
	}
}
