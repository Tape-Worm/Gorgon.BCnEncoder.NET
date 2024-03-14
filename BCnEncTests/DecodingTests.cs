using System;
using System.Collections.Generic;
using System.IO;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using SixLabors.ImageSharp;
using Xunit;

namespace BCnEncTests
{
    public class DecodingTests
    {
        [Fact]
        public void Bc1Decode() {
            using FileStream fs = File.OpenRead(@"../../../testImages/test_decompress_bc1.ktx");
            var file = KtxFile.Load(fs);
            Assert.True(file.Header.VerifyHeader());
            Assert.Equal((uint)1, file.Header.NumberOfFaces);

            var decoder = new BcDecoder();
            using Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = decoder.Decode(file);

            Assert.Equal((uint)image.Width, file.Header.PixelWidth);
            Assert.Equal((uint)image.Height, file.Header.PixelHeight);

            using FileStream outFs = File.OpenWrite("decoding_test_bc1.png");
            image.SaveAsPng(outFs);
        }

        [Fact]
        public void Bc1AlphaDecode() {
            using FileStream fs = File.OpenRead(@"../../../testImages/test_decompress_bc1a.ktx");
            var file = KtxFile.Load(fs);
            Assert.True(file.Header.VerifyHeader());
            Assert.Equal((uint)1, file.Header.NumberOfFaces);

            var decoder = new BcDecoder();
            using Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = decoder.Decode(file);

            Assert.Equal((uint)image.Width, file.Header.PixelWidth);
            Assert.Equal((uint)image.Height, file.Header.PixelHeight);

            using FileStream outFs = File.OpenWrite("decoding_test_bc1a.png");
            image.SaveAsPng(outFs);
        }

        [Fact]
        public void Bc2Decode() {
            using FileStream fs = File.OpenRead(@"../../../testImages/test_decompress_bc2.ktx");
            var file = KtxFile.Load(fs);
            Assert.True(file.Header.VerifyHeader());
            Assert.Equal((uint)1, file.Header.NumberOfFaces);

            var decoder = new BcDecoder();
            using Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = decoder.Decode(file);

            Assert.Equal((uint)image.Width, file.Header.PixelWidth);
            Assert.Equal((uint)image.Height, file.Header.PixelHeight);

            using FileStream outFs = File.OpenWrite("decoding_test_bc2.png");
            image.SaveAsPng(outFs);
        }

        [Fact]
        public void Bc3Decode() {
            using FileStream fs = File.OpenRead(@"../../../testImages/test_decompress_bc3.ktx");
            var file = KtxFile.Load(fs);
            Assert.True(file.Header.VerifyHeader());
            Assert.Equal((uint)1, file.Header.NumberOfFaces);

            var decoder = new BcDecoder();
            using Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = decoder.Decode(file);

            Assert.Equal((uint)image.Width, file.Header.PixelWidth);
            Assert.Equal((uint)image.Height, file.Header.PixelHeight);

            using FileStream outFs = File.OpenWrite("decoding_test_bc3.png");
            image.SaveAsPng(outFs);
        }

        [Fact]
        public void Bc4Decode() {
            using FileStream fs = File.OpenRead(@"../../../testImages/test_decompress_bc4_unorm.ktx");
            var file = KtxFile.Load(fs);
            Assert.True(file.Header.VerifyHeader());
            Assert.Equal((uint)1, file.Header.NumberOfFaces);

            var decoder = new BcDecoder();
            using Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = decoder.Decode(file);

            Assert.Equal((uint)image.Width, file.Header.PixelWidth);
            Assert.Equal((uint)image.Height, file.Header.PixelHeight);

            using FileStream outFs = File.OpenWrite("decoding_test_bc4.png");
            image.SaveAsPng(outFs);
        }

        [Fact]
        public void Bc5Decode() {
            using FileStream fs = File.OpenRead(@"../../../testImages/test_decompress_bc5_unorm.ktx");
            var file = KtxFile.Load(fs);
            Assert.True(file.Header.VerifyHeader());
            Assert.Equal((uint)1, file.Header.NumberOfFaces);

            var decoder = new BcDecoder();
            using Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = decoder.Decode(file);

            Assert.Equal((uint)image.Width, file.Header.PixelWidth);
            Assert.Equal((uint)image.Height, file.Header.PixelHeight);

            using FileStream outFs = File.OpenWrite("decoding_test_bc5.png");
            image.SaveAsPng(outFs);
        }

        [Fact]
        public void Bc7DecodeRgb() {
            using FileStream fs = File.OpenRead(@"../../../testImages/test_decompress_bc7_rgb.ktx");
            var file = KtxFile.Load(fs);
            Assert.True(file.Header.VerifyHeader());
            Assert.Equal((uint)1, file.Header.NumberOfFaces);

            var decoder = new BcDecoder();
            using Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = decoder.Decode(file);

            Assert.Equal((uint)image.Width, file.Header.PixelWidth);
            Assert.Equal((uint)image.Height, file.Header.PixelHeight);

            using FileStream outFs = File.OpenWrite("decoding_test_bc7_rgb.png");
            image.SaveAsPng(outFs);
        }

        [Fact]
        public void Bc7DecodeUnorm() {
            using FileStream fs = File.OpenRead(@"../../../testImages/test_decompress_bc7_unorm.ktx");
            var file = KtxFile.Load(fs);
            Assert.True(file.Header.VerifyHeader());
            Assert.Equal((uint)1, file.Header.NumberOfFaces);

            var decoder = new BcDecoder();
            using Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = decoder.Decode(file);

            Assert.Equal((uint)image.Width, file.Header.PixelWidth);
            Assert.Equal((uint)image.Height, file.Header.PixelHeight);

            using FileStream outFs = File.OpenWrite("decoding_test_bc7_unorm.png");
            image.SaveAsPng(outFs);
        }

        [Fact]
        public void Bc7DecodeEveryBlockType() {
            using FileStream fs = File.OpenRead(@"../../../testImages/test_decompress_bc7_types.ktx");
            var file = KtxFile.Load(fs);
            Assert.True(file.Header.VerifyHeader());
            Assert.Equal((uint)1, file.Header.NumberOfFaces);

            var decoder = new BcDecoder();
            using Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = decoder.Decode(file);

            Assert.Equal((uint)image.Width, file.Header.PixelWidth);
            Assert.Equal((uint)image.Height, file.Header.PixelHeight);

            using FileStream outFs = File.OpenWrite("decoding_test_bc7_types.png");
            image.SaveAsPng(outFs);
        }
    }
}
