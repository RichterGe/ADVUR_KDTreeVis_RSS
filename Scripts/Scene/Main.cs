using Godot;
using KDTreeVis.KdTree;

namespace KDTreeVis.Scene;

/// <summary>Scene entry point: extracts geometry, builds the k-d tree, and wires up visualization and ray casting.</summary>
public partial class Main : Node3D
{
    [Export] public Node3D SceneGeometryRoot { get; set; }
    [Export] public KdTreeVisualizer Visualizer { get; set; }
    [Export] public RayCastController RayCastController { get; set; }

    [Export] public int MaxDepth { get; set; } = 16;
    [Export] public int MinTrianglesPerLeaf { get; set; } = 4;

    public override void _Ready()
    {
        if (SceneGeometryRoot == null || Visualizer == null || RayCastController == null)
        {
            GD.PushError($"{nameof(Main)}: Assign SceneGeometryRoot, Visualizer, and RayCastController in the Inspector!");
            return;
        }

        MeshExtractor.ExtractionResult result = MeshExtractor.ExtractFromNode(SceneGeometryRoot);

        GD.Print($"Extracted {result.Triangles.Length} triangles from {result.MeshCount} meshes.");
        GD.Print($"Scene AABB -> Min: {result.SceneBounds.Position}, Max: {result.SceneBounds.End}, Size: {result.SceneBounds.Size}");

        if (result.Triangles.Length == 0)
        {
            GD.PushError($"{nameof(Main)}: No triangles found, k-d tree not built.");
            return;
        }

        KdTreeNode treeRoot = KdTreeBuilder.Build(result.Triangles, result.SceneBounds, MaxDepth, MinTrianglesPerLeaf);
        LogStats(KdTreeStats.Compute(treeRoot));

        Visualizer.ShowTree(treeRoot);
        RayCastController.SetTree(treeRoot, result.Triangles);
    }

    private static void LogStats(KdTreeStats stats)
    {
        GD.Print($"k-d tree -> Nodes: {stats.NodeCount}, Leaves: {stats.LeafCount}, Max depth: {stats.MaxDepth}, " +
                 $"Triangles/leaf avg: {stats.AverageTrianglesPerLeaf:F2}, max: {stats.MaxTrianglesPerLeaf}");
    }
}
