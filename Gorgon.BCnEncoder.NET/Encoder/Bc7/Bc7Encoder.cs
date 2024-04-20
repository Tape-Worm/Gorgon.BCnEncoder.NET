using System;
using System.Collections.Generic;
using Gorgon.BCnEncoder.Shared;

namespace Gorgon.BCnEncoder.Encoder.Bc7;

internal class Bc7Encoder : BcBlockEncoder<Bc7Block>
{
    private static ClusterIndices4X4 CreateClusterIndexBlock(RawBlock4X4Rgba32 raw, out int outputNumClusters,
        int numClusters = 3)
    {

        var indexBlock = new ClusterIndices4X4();

        int[] indices = LinearClustering.ClusterPixels(raw.AsSpan, 4, 4,
            numClusters, 1, 10, false);

        Span<int> output = indexBlock.AsSpan;
        for (int i = 0; i < output.Length; i++)
        {
            output[i] = indices[i];
        }

        int nClusters = indexBlock.NumClusters;
        if (nClusters < numClusters)
        {
            indexBlock = indexBlock.Reduce(out nClusters);
        }

        outputNumClusters = nClusters;
        return indexBlock;
    }

    protected override Bc7Block EncodeBlock(RawBlock4X4Rgba32 rawBlock, CompressionQuality quality) => quality switch
    {
        CompressionQuality.Fast => Bc7EncoderFast.EncodeBlock(rawBlock),
        CompressionQuality.Balanced => Bc7EncoderBalanced.EncodeBlock(rawBlock),
        CompressionQuality.BestQuality => Bc7EncoderBestQuality.EncodeBlock(rawBlock),
        _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null),
    };

    private static class Bc7EncoderFast
    {
        private const float ErrorThreshsold = 0.005f;
        private const int MaxTries = 5;

        private static IEnumerable<Bc7Block> TryMethods(RawBlock4X4Rgba32 rawBlock, int[] best2SubsetPartitions, int[] best3SubsetPartitions, bool alpha)
        {
            if (alpha)
            {
                yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 5);
                yield return Bc7Mode5Encoder.EncodeBlock(rawBlock, 3);
            }
            else
            {
                yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 6);
                for (int i = 0; i < 64; i++)
                {
                    if (best3SubsetPartitions[i] < 16)
                    {
                        yield return Bc7Mode0Encoder.EncodeBlock(rawBlock, 3, best3SubsetPartitions[i]);
                    }

                    yield return Bc7Mode1Encoder.EncodeBlock(rawBlock, 4, best2SubsetPartitions[i]);

                }
            }
        }

        public static Bc7Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
        {
            bool hasAlpha = rawBlock.HasTransparentPixels();

            ClusterIndices4X4 indexBlock2 = CreateClusterIndexBlock(rawBlock, out int clusters2, 2);
            ClusterIndices4X4 indexBlock3 = CreateClusterIndexBlock(rawBlock, out int clusters3, 3);

            if (clusters2 < 2)
            {
                clusters2 = clusters3;
                indexBlock2 = indexBlock3;
            }

            int[] best2SubsetPartitions = Bc7EncodingHelpers.Rank2SubsetPartitions(indexBlock2, clusters2);
            int[] best3SubsetPartitions = Bc7EncodingHelpers.Rank3SubsetPartitions(indexBlock3, clusters3);

            float bestError = 99999;
            var best = new Bc7Block();
            int tries = 0;
            foreach (Bc7Block block in TryMethods(rawBlock, best2SubsetPartitions, best3SubsetPartitions, hasAlpha))
            {
                RawBlock4X4Rgba32 decoded = block.Decode();
                float error = rawBlock.CalculateYCbCrAlphaError(decoded);
                tries++;

                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                }

                if (error < ErrorThreshsold || tries > MaxTries)
                {
                    break;
                }

            }

            return best;
        }
    }

    private static class Bc7EncoderBalanced
    {
        private const float ErrorThreshsold = 0.005f;
        private const int MaxTries = 25;

        private static IEnumerable<Bc7Block> TryMethods(RawBlock4X4Rgba32 rawBlock, int[] best2SubsetPartitions, int[] best3SubsetPartitions, bool alpha)
        {
            if (alpha)
            {
                yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 6);
                yield return Bc7Mode5Encoder.EncodeBlock(rawBlock, 4);
                yield return Bc7Mode4Encoder.EncodeBlock(rawBlock, 4);
                for (int i = 0; i < 64; i++)
                {
                    yield return Bc7Mode7Encoder.EncodeBlock(rawBlock, 3, best2SubsetPartitions[i]);
                }
            }
            else
            {
                yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 6);
                yield return Bc7Mode5Encoder.EncodeBlock(rawBlock, 4);
                yield return Bc7Mode4Encoder.EncodeBlock(rawBlock, 4);
                for (int i = 0; i < 64; i++)
                {
                    yield return best3SubsetPartitions[i] < 16
                        ? Bc7Mode0Encoder.EncodeBlock(rawBlock, 3, best3SubsetPartitions[i])
                        : Bc7Mode2Encoder.EncodeBlock(rawBlock, 5, best3SubsetPartitions[i]);

                    yield return Bc7Mode1Encoder.EncodeBlock(rawBlock, 4, best2SubsetPartitions[i]);
                }
            }
        }

        public static Bc7Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
        {
            bool hasAlpha = rawBlock.HasTransparentPixels();

            ClusterIndices4X4 indexBlock2 = CreateClusterIndexBlock(rawBlock, out int clusters2, 2);
            ClusterIndices4X4 indexBlock3 = CreateClusterIndexBlock(rawBlock, out int clusters3, 3);

            if (clusters2 < 2)
            {
                clusters2 = clusters3;
                indexBlock2 = indexBlock3;
            }

            int[] best2SubsetPartitions = Bc7EncodingHelpers.Rank2SubsetPartitions(indexBlock2, clusters2);
            int[] best3SubsetPartitions = Bc7EncodingHelpers.Rank3SubsetPartitions(indexBlock3, clusters3);

            float bestError = 99999;
            var best = new Bc7Block();
            int tries = 0;
            foreach (Bc7Block block in TryMethods(rawBlock, best2SubsetPartitions, best3SubsetPartitions, hasAlpha))
            {
                RawBlock4X4Rgba32 decoded = block.Decode();
                float error = rawBlock.CalculateYCbCrAlphaError(decoded);
                tries++;

                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                }

                if (error < ErrorThreshsold || tries > MaxTries)
                {
                    break;
                }

            }

            return best;
        }
    }

    private static class Bc7EncoderBestQuality
    {

        private const float ErrorThreshsold = 0.001f;
        private const int MaxTries = 40;

        private static IEnumerable<Bc7Block> TryMethods(RawBlock4X4Rgba32 rawBlock, int[] best2SubsetPartitions, int[] best3SubsetPartitions, bool alpha)
        {
            if (alpha)
            {
                yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 8);
                yield return Bc7Mode5Encoder.EncodeBlock(rawBlock, 5);
                yield return Bc7Mode4Encoder.EncodeBlock(rawBlock, 5);
                for (int i = 0; i < 64; i++)
                {
                    yield return Bc7Mode7Encoder.EncodeBlock(rawBlock, 4, best2SubsetPartitions[i]);

                }
            }
            else
            {
                yield return Bc7Mode6Encoder.EncodeBlock(rawBlock, 8);
                yield return Bc7Mode5Encoder.EncodeBlock(rawBlock, 5);
                yield return Bc7Mode4Encoder.EncodeBlock(rawBlock, 5);
                for (int i = 0; i < 64; i++)
                {
                    if (best3SubsetPartitions[i] < 16)
                    {
                        yield return Bc7Mode0Encoder.EncodeBlock(rawBlock, 4, best3SubsetPartitions[i]);
                    }
                    yield return Bc7Mode2Encoder.EncodeBlock(rawBlock, 5, best3SubsetPartitions[i]);

                    yield return Bc7Mode1Encoder.EncodeBlock(rawBlock, 4, best2SubsetPartitions[i]);
                    yield return Bc7Mode3Encoder.EncodeBlock(rawBlock, 5, best2SubsetPartitions[i]);

                }
            }
        }

        public static Bc7Block EncodeBlock(RawBlock4X4Rgba32 rawBlock)
        {
            bool hasAlpha = rawBlock.HasTransparentPixels();

            ClusterIndices4X4 indexBlock2 = CreateClusterIndexBlock(rawBlock, out int clusters2, 2);
            ClusterIndices4X4 indexBlock3 = CreateClusterIndexBlock(rawBlock, out int clusters3, 3);

            if (clusters2 < 2)
            {
                clusters2 = clusters3;
                indexBlock2 = indexBlock3;
            }

            int[] best2SubsetPartitions = Bc7EncodingHelpers.Rank2SubsetPartitions(indexBlock2, clusters2);
            int[] best3SubsetPartitions = Bc7EncodingHelpers.Rank3SubsetPartitions(indexBlock3, clusters3);

            float bestError = 99999;
            var best = new Bc7Block();
            int tries = 0;
            foreach (Bc7Block block in TryMethods(rawBlock, best2SubsetPartitions, best3SubsetPartitions, hasAlpha))
            {
                RawBlock4X4Rgba32 decoded = block.Decode();
                float error = rawBlock.CalculateYCbCrAlphaError(decoded);
                tries++;

                if (error < bestError)
                {
                    best = block;
                    bestError = error;
                }

                if (error < ErrorThreshsold || tries > MaxTries)
                {
                    break;
                }

            }

            return best;
        }
    }

    public Bc7Encoder()
        : base(Environment.ProcessorCount / 2)
    {
    }
}
