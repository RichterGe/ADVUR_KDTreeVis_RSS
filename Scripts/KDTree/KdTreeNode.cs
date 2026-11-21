using System;
using Godot;

namespace KDTreeVis.KdTree;

/// <summary>
/// A node of the k-d tree. Inner nodes store only split axis, split position, bounds and children;
/// leaves store the indices of the triangles that overlap their cell.
/// </summary>
public sealed class KdTreeNode
{
    private readonly int[] _triangleIndices;

    /// <summary>World-space bounds of the cell covered by this node.</summary>
    public Aabb Bounds { get; }

    /// <summary>Split axis (0 = X, 1 = Y, 2 = Z). Only meaningful for inner nodes.</summary>
    public int SplitAxis { get; }

    /// <summary>World-space position of the split plane. Only meaningful for inner nodes.</summary>
    public float SplitPosition { get; }

    /// <summary>Child on the lower side of the split plane; null for leaves.</summary>
    public KdTreeNode Left { get; }

    /// <summary>Child on the upper side of the split plane; null for leaves.</summary>
    public KdTreeNode Right { get; }

    /// <summary>Indices into the triangle array; empty for inner nodes.</summary>
    public ReadOnlySpan<int> TriangleIndices => _triangleIndices;

    public bool IsLeaf => Left == null;

    private KdTreeNode(Aabb bounds, int splitAxis, float splitPosition,
        KdTreeNode left, KdTreeNode right, int[] triangleIndices)
    {
        Bounds = bounds;
        SplitAxis = splitAxis;
        SplitPosition = splitPosition;
        Left = left;
        Right = right;
        _triangleIndices = triangleIndices;
    }

    /// <summary>Creates a leaf node holding the given triangle indices.</summary>
    public static KdTreeNode CreateLeaf(Aabb bounds, int[] triangleIndices) =>
        new(bounds, 0, 0f, null, null, triangleIndices);

    /// <summary>Creates an inner node that splits its cell at <paramref name="splitPosition"/>.</summary>
    public static KdTreeNode CreateInner(Aabb bounds, int splitAxis, float splitPosition,
        KdTreeNode left, KdTreeNode right) =>
        new(bounds, splitAxis, splitPosition, left, right, Array.Empty<int>());
}
