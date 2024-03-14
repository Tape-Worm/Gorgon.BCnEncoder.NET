﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BCnEncTests
{
    public static class ImageLoader {
        public static Image<Rgba32> testDiffuse1 { get; } = LoadTestImage("../../../testImages/test_diffuse_1_512.jpg");
        public static Image<Rgba32> testBlur1 { get; } = LoadTestImage("../../../testImages/test_blur_1_512.jpg");
        public static Image<Rgba32> testNormal1 { get; } = LoadTestImage("../../../testImages/test_normal_1_512.jpg");
        public static Image<Rgba32> testHeight1 { get; } = LoadTestImage("../../../testImages/test_height_1_512.jpg");
        public static Image<Rgba32> testGradient1 { get; } = LoadTestImage("../../../testImages/test_gradient_1_512.jpg");
        public static Image<Rgba32> testTransparentSprite1 { get; } = LoadTestImage("../../../testImages/test_transparent.png");
        public static Image<Rgba32> testAlphaGradient1 { get; } = LoadTestImage("../../../testImages/test_alphagradient_1_512.png");
        public static Image<Rgba32> testAlpha1 { get; } = LoadTestImage("../../../testImages/test_alpha_1_512.png");
        public static Image<Rgba32> testRedGreen1 { get; } = LoadTestImage("../../../testImages/test_red_green_1_64.png");
        public static Image<Rgba32> testRgbHard1 { get; } = LoadTestImage("../../../testImages/test_rgb_hard_1.png");
        public static Image<Rgba32> testLenna { get; } = LoadTestImage("../../../testImages/test_lenna_512.png");

        public static Image<Rgba32>[] testCubemap { get; } = new [] {
            LoadTestImage("../../../testImages/cubemap/right.png"),
            LoadTestImage("../../../testImages/cubemap/left.png"),
            LoadTestImage("../../../testImages/cubemap/top.png"),
            LoadTestImage("../../../testImages/cubemap/bottom.png"),
            LoadTestImage("../../../testImages/cubemap/back.png"),
            LoadTestImage("../../../testImages/cubemap/forward.png")
        };

        private static Image<Rgba32> LoadTestImage(string filename) => Image.Load<Rgba32>(filename);
    }
}
