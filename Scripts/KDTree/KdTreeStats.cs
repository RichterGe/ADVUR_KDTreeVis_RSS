using System;

namespace KDTreeVis.KdTree;

/// <summary>Summary statistics of a built k-d tree.</summary>
public readonly record struct KdTreeStats(
    int NodeCount,
    int LeafCount,
    int MaxDepth,
    float AverageTrianglesPerLeaf,
    int MaxTrianglesPerLeaf
)
{
    /// <summary>Computes the statistics by walking the whole tree once.</summary>
    public static KdTreeStats Compute(KdTreeNode root)
    {
        int nodeCount = 0, leafCount = 0, maxDepth = 0, maxTriangles = 0;
        long totalTriangles = 0;

        Visit(root, 0);

        float average = leafCount == 0 ? 0f : (float)totalTriangles / leafCount;
        return new KdTreeStats(nodeCount, leafCount, maxDepth, average, maxTriangles);

        void Visit(KdTreeNode node, int depth)
        {
            nodeCount++;
            maxDepth = Math.Max(maxDepth, depth);

            if (!node.IsLeaf)
            {
                Visit(node.Left, depth + 1);
                Visit(node.Right, depth + 1);
                return;
            }

            leafCount++;
            totalTriangles += node.TriangleIndices.Length;
            maxTriangles = Math.Max(maxTriangles, node.TriangleIndices.Length);
        }
    }
}
