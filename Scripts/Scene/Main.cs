using Godot;
using KDTreeVis.KdTree;

namespace KDTreeVis.Scene;

public partial class Main : Node3D
{
    [Export]
    public Node3D SceneGeometryRoot { get; set; }

    [Export] public int MaxDepth { get; set; } = 16;
    [Export] public int MinTrianglesPerLeaf { get; set; } = 4;

    private Triangle[] _triangles;
    private Aabb _sceneBounds;
    private KdTreeNode _treeRoot;

    public override void _Ready()
    {
        if (SceneGeometryRoot == null)
        {
            GD.PushError("Main: Assign SceneGeometryRoot in the Inspector!");
            return;
        }

        var result = MeshExtractor.ExtractFromNode(SceneGeometryRoot);
        _triangles = result.Triangles;
        _sceneBounds = result.SceneBounds;

        GD.Print($"Extracted {_triangles.Length} triangles from {result.MeshCount} meshes.");
        GD.Print($"Scene AABB -> Min: {_sceneBounds.Position}, Max: {_sceneBounds.End}, Size: {_sceneBounds.Size}");

        if (_triangles.Length == 0)
        {
            GD.PushError("Main: No triangles found, k-d tree not built.");
            return;
        }

        _treeRoot = KdTreeBuilder.Build(_triangles, _sceneBounds, MaxDepth, MinTrianglesPerLeaf);
        LogStats(KdTreeStats.Compute(_treeRoot));

        var visualizer = new KdTreeVisualizer();
        AddChild(visualizer);
        visualizer.ShowTree(_treeRoot);
    }

    private static void LogStats(KdTreeStats stats)
    {
        GD.Print($"k-d tree -> Nodes: {stats.NodeCount}, Leaves: {stats.LeafCount}, Max depth: {stats.MaxDepth}, " +
                 $"Triangles/leaf avg: {stats.AverageTrianglesPerLeaf:F2}, max: {stats.MaxTrianglesPerLeaf}");
    }
}