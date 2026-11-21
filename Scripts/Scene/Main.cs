using Godot;
using KDTreeVis.KdTree;

namespace KDTreeVis.Scene;

public partial class Main : Node3D
{
    [Export]
    public Node3D SceneGeometryRoot { get; set; }

    private Triangle[] _triangles;
    private Aabb _sceneBounds;

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
    }
}