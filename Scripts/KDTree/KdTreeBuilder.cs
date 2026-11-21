using System;
using System.Collections.Generic;
using Godot;

namespace KDTreeVis.KdTree;

/// <summary>Builds a 3D k-d tree with median splits along the longest axis of each node's bounds.</summary>
public static class KdTreeBuilder
{
    /// <summary>
    /// Builds the tree. Triangles crossing a split plane are referenced by both children.
    /// </summary>
    public static KdTreeNode Build(Triangle[] triangles, Aabb sceneBounds, int maxDepth, int minTrianglesPerLeaf)
    {
        var allIndices = new int[triangles.Length];
        for (int i = 0; i < allIndices.Length; i++)
            allIndices[i] = i;

        return BuildNode(allIndices, sceneBounds, 0);

        KdTreeNode BuildNode(int[] indices, Aabb bounds, int depth)
        {
            if (depth >= maxDepth || indices.Length <= minTrianglesPerLeaf)
                return KdTreeNode.CreateLeaf(bounds, indices);

            int axis = (int)bounds.GetLongestAxisIndex();
            float splitPosition = MedianCentroid(triangles, indices, axis);

            // Triangles spanning an earlier split plane can pull the median outside this cell.
            if (splitPosition <= bounds.Position[axis] || splitPosition >= bounds.End[axis])
                return KdTreeNode.CreateLeaf(bounds, indices);

            var leftIndices = new List<int>();
            var rightIndices = new List<int>();
            foreach (int index in indices)
            {
                Aabb triangleBounds = triangles[index].Bounds;
                if (triangleBounds.Position[axis] <= splitPosition)
                    leftIndices.Add(index);
                if (triangleBounds.End[axis] >= splitPosition)
                    rightIndices.Add(index);
            }

            // No progress: one child would contain everything, so splitting only adds depth.
            if (leftIndices.Count == indices.Length || rightIndices.Count == indices.Length)
                return KdTreeNode.CreateLeaf(bounds, indices);

            SplitBounds(bounds, axis, splitPosition, out Aabb leftBounds, out Aabb rightBounds);

            KdTreeNode left = BuildNode(leftIndices.ToArray(), leftBounds, depth + 1);
            KdTreeNode right = BuildNode(rightIndices.ToArray(), rightBounds, depth + 1);
            return KdTreeNode.CreateInner(bounds, axis, splitPosition, left, right);
        }
    }

    /// <remarks>
    /// Sorting is O(n log n) instead of Quickselect's O(n), but the tree is built only once
    /// and a sort is simpler and less error-prone.
    /// </remarks>
    private static float MedianCentroid(Triangle[] triangles, int[] indices, int axis)
    {
        var keys = new float[indices.Length];
        for (int i = 0; i < indices.Length; i++)
            keys[i] = triangles[indices[i]].Centroid[axis];

        Array.Sort(keys);
        return keys[keys.Length / 2];
    }

    private static void SplitBounds(Aabb bounds, int axis, float splitPosition, out Aabb left, out Aabb right)
    {
        Vector3 leftSize = bounds.Size;
        leftSize[axis] = splitPosition - bounds.Position[axis];
        left = new Aabb(bounds.Position, leftSize);

        Vector3 rightPosition = bounds.Position;
        rightPosition[axis] = splitPosition;
        Vector3 rightSize = bounds.Size;
        rightSize[axis] = bounds.End[axis] - splitPosition;
        right = new Aabb(rightPosition, rightSize);
    }
}
