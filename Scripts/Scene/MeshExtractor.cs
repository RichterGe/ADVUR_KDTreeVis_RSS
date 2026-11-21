using System;
using System.Collections.Generic;
using Godot;
using KDTreeVis.KdTree;

namespace KDTreeVis.Scene;

public static class MeshExtractor
{
    private const float DegenerateAreaSqEpsilon = 1e-12f;
    private const float RootBoundsPadding = 0.001f;

    public readonly record struct ExtractionResult(
        Triangle[] Triangles,
        Aabb SceneBounds,
        int MeshCount
    );

    public static ExtractionResult ExtractFromNode(Node root)
    {
        var triangles = new List<Triangle>();
        int meshCount = 0;

        CollectRecursive(root, triangles, ref meshCount);

        if (triangles.Count == 0)
            return new ExtractionResult(Array.Empty<Triangle>(), new Aabb(), meshCount);

        // Compute tight enclosing AABB around all extracted triangles
        Vector3 min = triangles[0].Bounds.Position;
        Vector3 max = triangles[0].Bounds.End;

        for (int i = 1; i < triangles.Count; i++)
        {
            Vector3 triMin = triangles[i].Bounds.Position;
            Vector3 triMax = triangles[i].Bounds.End;

            min = new Vector3(
                MathF.Min(min.X, triMin.X),
                MathF.Min(min.Y, triMin.Y),
                MathF.Min(min.Z, triMin.Z)
            );
            max = new Vector3(
                MathF.Max(max.X, triMax.X),
                MathF.Max(max.Y, triMax.Y),
                MathF.Max(max.Z, triMax.Z)
            );
        }

        // Tiny padding prevents precision misses on triangles lying flush on the root faces
        Vector3 pad = Vector3.One * RootBoundsPadding;
        Aabb sceneBounds = new(min - pad, (max - min) + pad * 2.0f);

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

                // Skip degenerate (zero-area) triangles
                if ((v1 - v0).Cross(v2 - v0).LengthSquared() < DegenerateAreaSqEpsilon)
                    continue;

                output.Add(new Triangle(output.Count, v0, v1, v2));
            }
        }

        foreach (Node child in node.GetChildren())
        {
            CollectRecursive(child, output, ref meshCount);
        }
    }
}