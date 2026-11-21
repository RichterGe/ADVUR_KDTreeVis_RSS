using System.Collections.Generic;
using Godot;
using KDTreeVis.KdTree;

namespace KDTreeVis.Scene;

/// <summary>
/// Draws the leaf cell boundaries of a k-d tree as a single wireframe mesh and toggles it via input action.
/// </summary>
public partial class KdTreeVisualizer : MeshInstance3D
{
    [Export] public Color LineColor { get; set; } = new(0.2f, 0.9f, 1.0f);

    private static readonly StringName ToggleWireframe = "toggle_kd_wireframe";

    public override void _Ready()
    {
        if (!InputMap.HasAction(ToggleWireframe))
            GD.PushError($"{nameof(KdTreeVisualizer)}: Input action '{ToggleWireframe}' is missing in the Input Map.");

        // The mesh uses world coordinates, so it must ignore the parent's transform.
        TopLevel = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(ToggleWireframe))
            Visible = !Visible;
    }

    /// <summary>Builds one line mesh containing the cell boxes of all leaves.</summary>
    public void ShowTree(KdTreeNode root)
    {
        var vertices = new List<Vector3>();
        CollectLeafEdges(root, vertices);

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();

        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
        arrayMesh.SurfaceSetMaterial(0, new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = LineColor,
        });

        Mesh = arrayMesh;
    }

    private static void CollectLeafEdges(KdTreeNode node, List<Vector3> vertices)
    {
        if (!node.IsLeaf)
        {
            CollectLeafEdges(node.Left, vertices);
            CollectLeafEdges(node.Right, vertices);
            return;
        }

        AddBoxEdges(node.Bounds, vertices);
    }

    private static void AddBoxEdges(Aabb bounds, List<Vector3> vertices)
    {
        Vector3 min = bounds.Position;
        Vector3 max = bounds.End;

        Vector3 c000 = new(min.X, min.Y, min.Z);
        Vector3 c100 = new(max.X, min.Y, min.Z);
        Vector3 c010 = new(min.X, max.Y, min.Z);
        Vector3 c110 = new(max.X, max.Y, min.Z);
        Vector3 c001 = new(min.X, min.Y, max.Z);
        Vector3 c101 = new(max.X, min.Y, max.Z);
        Vector3 c011 = new(min.X, max.Y, max.Z);
        Vector3 c111 = new(max.X, max.Y, max.Z);

        // Bottom face
        AddEdge(c000, c100);
        AddEdge(c100, c101);
        AddEdge(c101, c001);
        AddEdge(c001, c000);

        // Top face
        AddEdge(c010, c110);
        AddEdge(c110, c111);
        AddEdge(c111, c011);
        AddEdge(c011, c010);

        // Vertical pillars
        AddEdge(c000, c010);
        AddEdge(c100, c110);
        AddEdge(c101, c111);
        AddEdge(c001, c011);

        void AddEdge(Vector3 a, Vector3 b)
        {
            vertices.Add(a);
            vertices.Add(b);
        }
    }
}
