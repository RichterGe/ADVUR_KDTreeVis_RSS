using System;
using System.Collections.Generic;
using Godot;
using KDTreeVis.KdTree;

namespace KDTreeVis.Scene;

/// <summary>Extracts world-space triangles and an enclosing bounding box from a scene subtree.</summary>
public static class MeshExtractor
{
    private const float DegenerateAreaSqEpsilon = 1e-12f;
    private const float RootBoundsPadding = 0.001f;

    /// <summary>Result of extracting visible meshes from a scene node.</summary>
    public readonly record struct ExtractionResult(
        Triangle[] Triangles,
        Aabb SceneBounds,
        int MeshCount
    );

    /// <summary>Recursively collects all non-degenerate triangles under <paramref name="root"/>.</summary>
    public static ExtractionResult ExtractFromNode(Node root)
    {
        var triangles = new List<Triangle>();
        int meshCount = 0;

        CollectRecursive(root, triangles, ref meshCount);

        if (triangles.Count == 0)
            return new ExtractionResult(Array.Empty<Triangle>(), new Aabb(), meshCount);

        Aabb sceneBounds = triangles[0].Bounds;
        for (int i = 1; i < triangles.Count; i++)
            sceneBounds = sceneBounds.Merge(triangles[i].Bounds);

        // Tiny padding prevents precision misses on triangles lying flush on the root faces.
        sceneBounds = sceneBounds.Grow(RootBoundsPadding);

        return new ExtractionResult(triangles.ToArray(), sceneBounds, meshCount);
    }

    private static void CollectRecursive(Node node, List<Triangle> output, ref int meshCount)
    {
        if (node is MeshInstance3D meshInstance
            && meshInstance.IsVisibleInTree()
            && meshInstance.Mesh != null)
        {
            meshCount++;
            Transform3D worldTransform = meshInstance.GlobalTransform;
            Vector3[] localFaces = meshInstance.Mesh.GetFaces();

            for (int i = 0; i + 2 < localFaces.Length; i += 3)
            {
                Vector3 v0 = worldTransform * localFaces[i];
                Vector3 v1 = worldTransform * localFaces[i + 1];
                Vector3 v2 = worldTransform * localFaces[i + 2];

                // Skip degenerate (zero-area) triangles.
                if ((v1 - v0).Cross(v2 - v0).LengthSquared() < DegenerateAreaSqEpsilon)
                    continue;

                output.Add(new Triangle(v0, v1, v2));
            }
        }

        foreach (Node child in node.GetChildren())
            CollectRecursive(child, output, ref meshCount);
    }
}
