using System.Collections.Generic;
using Godot;
using KDTreeVis.KdTree;

namespace KDTreeVis.Scene;

/// <summary>
/// Draws the leaf cell boundaries of a k-d tree as a wireframe overlay and toggles it via input action.
/// </summary>
public partial class KdTreeVisualizer : MeshInstance3D
{
    private const int BoxCornerCount = 8;

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

        // Aabb.GetEndpoint encodes the corner in the bits of its index (one bit per axis),
        // so two corners share an edge exactly when their indices differ in a single bit.
        for (int corner = 0; corner < BoxCornerCount; corner++)
        {
            for (int bit = 1; bit < BoxCornerCount; bit <<= 1)
            {
                if ((corner & bit) != 0)
                    continue;

                vertices.Add(node.Bounds.GetEndpoint(corner));
                vertices.Add(node.Bounds.GetEndpoint(corner | bit));
            }
        }
    }
}
