﻿using System;
using System.Buffers;
using System.Collections.Generic;
using Gorgon.Graphics;

namespace Gorgon.BCnEncoder.Shared;

/// <summary>
/// Simple Linear Iterative Clustering
/// </summary>
internal static class LinearClustering
{

    private struct LabXy
    {
        public float l;
        public float a;
        public float b;
        public float x;
        public float y;


        public static LabXy operator +(LabXy left, LabXy right) => new()
        {
            l = left.l + right.l,
            a = left.a + right.a,
            b = left.b + right.b,
            x = left.x + right.x,
            y = left.y + right.y,
        };

        public static LabXy operator /(LabXy left, int right) => new()
        {
            l = left.l / right,
            a = left.a / right,
            b = left.b / right,
            x = left.x / right,
            y = left.y / right,
        };
    }

    private struct ClusterCenter(LinearClustering.LabXy labxy)
    {
        public float l = labxy.l;
        public float a = labxy.a;
        public float b = labxy.b;
        public float x = labxy.x;
        public float y = labxy.y;
        public int count = 0;

        public float Distance(LabXy other, float m, float s)
        {
            float dLab = (float)System.Math.Sqrt(
                System.Math.Pow(l - other.l, 2) +
                System.Math.Pow(a - other.a, 2) +
                System.Math.Pow(b - other.b, 2));

            float dXy = (float)System.Math.Sqrt(
                System.Math.Pow(x - other.x, 2) +
                System.Math.Pow(y - other.y, 2));
            return dLab + (m / s) * dXy;
        }

        public float Distance(ClusterCenter other, float m, float s)
        {
            float dLab = (float)System.Math.Sqrt(
                (l - other.l) * (l - other.l) +
                (a - other.a) * (a - other.a) +
                (b - other.b) * (b - other.b));

            float dXy = (float)System.Math.Sqrt(
                (x - other.x) * (x - other.x) +
                (y - other.y) * (y - other.y));
            return dLab + m / s * dXy;
        }

        public static ClusterCenter operator +(ClusterCenter left, LabXy right) => new()
        {
            l = left.l + right.l,
            a = left.a + right.a,
            b = left.b + right.b,
            x = left.x + right.x,
            y = left.y + right.y,
            count = left.count + 1
        };

        public static ClusterCenter operator /(ClusterCenter left, int right) => new()
        {
            l = left.l / right,
            a = left.a / right,
            b = left.b / right,
            x = left.x / right,
            y = left.y / right,
            count = left.count
        };
    }

    /// <summary>
    /// The greater the value of M,
    /// the more spatial proximity is emphasized and the more compact the cluster,
    /// M should be in range of 1 to 20.
    /// </summary>
    public static int[] ClusterPixels(ReadOnlySpan<GorgonColor> pixels, int width, int height,
        int clusters, float m = 10, int maxIterations = 10, bool enforceConnectivity = true)
    {
        if (clusters < 2)
        {
            throw new ArgumentException("Number of clusters should be more than 1");
        }

        //Grid interval S
        float S = (float)System.Math.Sqrt(pixels.Length / (float)clusters);
        int[] clusterIndices = new int[pixels.Length];

        LabXy[] labXysArray = ArrayPool<LabXy>.Shared.Rent(pixels.Length);
        var labXys = new Span<LabXy>(labXysArray, 0, pixels.Length);

        ClusterCenter[] clusterCenterArray = ArrayPool<ClusterCenter>.Shared.Rent(clusters);
        ClusterCenter[] previousCenters = ArrayPool<ClusterCenter>.Shared.Rent(clusters);

        var clusterCenters = new Span<ClusterCenter>(clusterCenterArray, 0, clusters);

        float Error = 999;
        const float threshold = 0.1f;
        int iter = 0;

        try
        {
            ConvertToLabXy(pixels, labXys, width, height);
            InitialClusterCenters(width, height, clusterCenters, S, labXys);

            while (Error > threshold)
            {
                if (maxIterations > 0 && iter >= maxIterations)
                {
                    break;
                }
                iter++;

                clusterCenters.CopyTo(previousCenters);

                for (int i = 0; i < clusterIndices.Length; ++i)
                {
                    clusterIndices[i] = -1;
                }

                // Find closest cluster for pixels
                for (int j = 0; j < clusters; j++)
                {
                    int xL = System.Math.Max(0, (int)(clusterCenters[j].x - S));
                    int xH = System.Math.Min(width, (int)(clusterCenters[j].x + S));
                    int yL = System.Math.Max(0, (int)(clusterCenters[j].y - S));
                    int yH = System.Math.Min(height, (int)(clusterCenters[j].y + S));

                    for (int x = xL; x < xH; x++)
                    {
                        for (int y = yL; y < yH; y++)
                        {
                            int i = x + y * width;

                            if (clusterIndices[i] == -1)
                            {
                                clusterIndices[i] = j;
                            }
                            else
                            {
                                float prevDistance = clusterCenters[clusterIndices[i]].Distance(labXys[i], m, S);
                                float distance = clusterCenters[j].Distance(labXys[i], m, S);
                                if (distance < prevDistance)
                                {
                                    clusterIndices[i] = j;
                                }
                            }
                        }
                    }
                }

                Error = RecalculateCenters(clusters, m, labXys, clusterIndices, previousCenters, S, ref clusterCenters);
            }

            if (enforceConnectivity)
            {
                clusterIndices = EnforceConnectivity(clusterIndices, width, height, clusters);
            }
        }
        finally
        {
            ArrayPool<ClusterCenter>.Shared.Return(clusterCenterArray, true);
            ArrayPool<ClusterCenter>.Shared.Return(previousCenters, true);
            ArrayPool<LabXy>.Shared.Return(labXysArray, true);
        }

        return clusterIndices;
    }

    private static float RecalculateCenters(int clusters, float m, Span<LabXy> labXys, int[] clusterIndices,
        Span<ClusterCenter> previousCenters, float s, ref Span<ClusterCenter> clusterCenters)
    {
        clusterCenters.Clear();
        for (int i = 0; i < labXys.Length; i++)
        {
            int clusterIndex = clusterIndices[i];
            // Sometimes a pixel is out of the range of any cluster,
            // in that case, find the nearest cluster and add it to it
            if (clusterIndex == -1)
            {
                int bestCluster = 0;
                float bestDistance = previousCenters[0].Distance(labXys[i], m, s);
                for (int j = 1; j < clusters; j++)
                {
                    float dist = previousCenters[j].Distance(labXys[i], m, s);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestCluster = j;
                    }
                }
                clusterCenters[bestCluster] += labXys[i];
                clusterIndices[i] = bestCluster;
            }
            else
            {
                clusterCenters[clusterIndex] += labXys[i];
            }
        }

        float error = 0;
        for (int i = 0; i < clusters; i++)
        {
            if (clusterCenters[i].count > 0)
            {
                clusterCenters[i] /= clusterCenters[i].count;
                error += clusterCenters[i].Distance(previousCenters[i], m, s);
            }
        }

        error /= clusters;
        return error;
    }

    private static void InitialClusterCenters(int width, int height, Span<ClusterCenter> clusterCenters, float s, Span<LabXy> labXys)
    {
        switch (clusterCenters.Length)
        {
            case 2:
                {
                    int x0 = (int)System.Math.Floor(width * 0.333f);
                    int y0 = (int)System.Math.Floor(height * 0.333f);

                    int x1 = (int)System.Math.Floor(width * 0.666f);
                    int y1 = (int)System.Math.Floor(height * 0.666f);

                    int i0 = x0 + y0 * width;
                    clusterCenters[0] = new ClusterCenter(labXys[i0]);

                    int i1 = x1 + y1 * width;
                    clusterCenters[1] = new ClusterCenter(labXys[i1]);
                }
                break;
            case 3:
                {
                    int x0 = (int)System.Math.Floor(width * 0.333f);
                    int y0 = (int)System.Math.Floor(height * 0.333f);
                    int i0 = x0 + y0 * width;
                    clusterCenters[0] = new ClusterCenter(labXys[i0]);

                    int x1 = (int)System.Math.Floor(width * 0.666f);
                    int y1 = (int)System.Math.Floor(height * 0.333f);
                    int i1 = x1 + y1 * width;
                    clusterCenters[1] = new ClusterCenter(labXys[i1]);

                    int x2 = (int)System.Math.Floor(width * 0.5f);
                    int y2 = (int)System.Math.Floor(height * 0.666f);
                    int i2 = x2 + y2 * width;
                    clusterCenters[2] = new ClusterCenter(labXys[i2]);
                }
                break;
            default:
                {
                    int cIdx = 0;
                    //Choose initial centers
                    for (float x = s / 2; x < width; x += s)
                    {
                        for (float y = s / 2; y < height; y += s)
                        {
                            if (cIdx >= clusterCenters.Length)
                            {
                                break;
                            }

                            int i = (int)x + (int)y * width;
                            clusterCenters[cIdx] = new ClusterCenter(labXys[i]);
                            cIdx++;
                        }
                    }
                }
                break;
        }
    }

    private static void ConvertToLabXy(ReadOnlySpan<GorgonColor> pixels, Span<LabXy> labXys, int width, int height)
    {
        //Convert pixels to LabXy
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int i = x + y * width;
                var lab = new ColorLab(pixels[i]);
                labXys[i] = new LabXy()
                {
                    l = lab.l,
                    a = lab.a,
                    b = lab.b,
                    x = x,
                    y = y
                };
            }
        }
    }

    private static int[] EnforceConnectivity(int[] oldLabels, int width, int height, int clusters)
    {
        ReadOnlySpan<int> neighborX = [-1, 0, 1, 0];
        ReadOnlySpan<int> neighborY = [0, -1, 0, 1];

        int sSquared = (width * height) / clusters;

        var clusterX = new List<int>(sSquared);
        var clusterY = new List<int>(sSquared);

        int adjacentLabel = 0;
        bool[] usedLabelsArray = ArrayPool<bool>.Shared.Rent(clusters);
        int[] newLabels = new int[oldLabels.Length];
        var usedLabels = new Span<bool>(usedLabelsArray, 0, clusters);

        try
        {
            for (int i = 0; i < newLabels.Length; ++i)
            {
                newLabels[i] = -1;
            }

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int xyIndex = x + y * width;
                    if (newLabels[xyIndex] < 0)
                    {
                        int label = oldLabels[xyIndex];
                        newLabels[xyIndex] = label;

                        //New cluster
                        clusterX.Add(x);
                        clusterY.Add(y);

                        //Search neighbors for already completed clusters
                        for (int i = 0; i < neighborX.Length; ++i)
                        {
                            int nX = x + neighborX[i];
                            int nY = y + neighborY[i];
                            int nI = nX + nY * width;
                            if (nX < width && nX >= 0 && nY < height && nY >= 0)
                            {
                                if (newLabels[nI] >= 0)
                                {
                                    adjacentLabel = newLabels[nI];
                                    break;
                                }
                            }
                        }

                        //Count pixels in this cluster
                        for (int c = 0; c < clusterX.Count; ++c)
                        {
                            for (int i = 0; i < neighborX.Length; ++i)
                            {
                                int nX = clusterX[c] + neighborX[i];
                                int nY = clusterY[c] + neighborY[i];
                                int nI = nX + nY * width;
                                if (nX < width && nX >= 0 && nY < height && nY >= 0)
                                {
                                    if (newLabels[nI] == -1 && label == oldLabels[nI])
                                    {
                                        clusterX.Add(nX);
                                        clusterY.Add(nY);
                                        newLabels[nI] = label;
                                    }
                                }
                            }
                        }

                        // If this is unusually small cluster or this label is already used,
                        // merge with adjacent cluster
                        if (clusterX.Count < (sSquared / 4) || usedLabels[label])
                        {
                            for (int i = 0; i < clusterX.Count; ++i)
                            {
                                newLabels[(clusterY[i] * width + clusterX[i])] = adjacentLabel;
                            }
                        }
                        else
                        {
                            usedLabels[label] = true;
                        }

                        clusterX.Clear();
                        clusterY.Clear();
                    }
                }
            }
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(usedLabelsArray, true);
        }

        return newLabels;
    }
}
