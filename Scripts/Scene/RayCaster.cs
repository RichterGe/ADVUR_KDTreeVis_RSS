using Godot;
using KDTreeVis.KdTree;

namespace KDTreeVis.Scene;

/// <summary>
/// Shoots a ray from the active camera along its view direction through the k-d tree
/// and highlights the first triangle that was hit.
/// </summary>
public partial class RayCaster : MeshInstance3D
{
    private const float HighlightOffset = 0.002f;
    private const float BruteForceDistanceEpsilon = 1e-4f;

    // The real ray starts exactly at the camera center, where it would be seen end-on and
    // therefore be invisible; the drawn line starts slightly below the camera instead.
    private const float LineStartDrop = 0.15f;
    private const float MissLineLength = 100f;

    [Export] public Color HighlightColor { get; set; } = new(1.0f, 0.2f, 0.1f);
    [Export] public Color LineColor { get; set; } = new(1.0f, 0.9f, 0.1f);

    private static readonly StringName CastRay = "cast_ray";

    private KdTreeNode _root;
    private Triangle[] _triangles;
    private MeshInstance3D _rayLine;
    private RayResultOverlay _resultOverlay;

    public override void _Ready()
    {
        if (!InputMap.HasAction(CastRay))
            GD.PushError($"{nameof(RayCaster)}: Input action '{CastRay}' is missing in the Input Map.");

        // The highlight uses world coordinates, so it must ignore the parent's transform.
        TopLevel = true;
        MaterialOverride = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = HighlightColor,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };

        _rayLine = new MeshInstance3D
        {
            TopLevel = true,
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = LineColor,
            },
        };
        AddChild(_rayLine);

        _resultOverlay = new RayResultOverlay();
        AddChild(_resultOverlay);
    }

    /// <summary>Sets the tree and triangles that rays are cast against.</summary>
    public void SetTree(KdTreeNode root, Triangle[] triangles)
    {
        _root = root;
        _triangles = triangles;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Left click also recaptures the mouse (FlyCamera); that click must not shoot a ray.
        // This node receives input before the camera, so the mouse is still visible for that click.
        if (@event.IsActionPressed(CastRay) && Input.MouseMode == Input.MouseModeEnum.Captured)
            CastFromCamera();
    }

    private void CastFromCamera()
    {
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null || _root == null)
            return;

        var ray = new Ray(camera.GlobalPosition, -camera.GlobalTransform.Basis.Z);

        Vector3 lineStart = ray.Origin - camera.GlobalTransform.Basis.Y * LineStartDrop;

        if (!KdTreeRayCaster.Cast(_root, _triangles, ray, out RayHit hit))
        {
            Mesh = null;
            ShowRayLine(lineStart, ray.PointAt(MissLineLength));
            GD.Print("Ray cast -> no hit.");
            _resultOverlay.ShowMessage("No hit");
            return;
        }

        ShowRayLine(lineStart, hit.Position);
        ShowHighlight(_triangles[hit.TriangleIndex], ray);
        GD.Print($"Ray cast -> triangle {hit.TriangleIndex}, t = {hit.T:F3}, position = {hit.Position}");
        _resultOverlay.ShowMessage($"Hit triangle {hit.TriangleIndex}");
        ReportBruteForceMismatch(ray, hit);
    }

    /// <summary>Cross-checks the tree result against the brute-force reference (debug builds only).</summary>
    [System.Diagnostics.Conditional("DEBUG")]
    private void ReportBruteForceMismatch(Ray ray, RayHit hit)
    {
        bool found = KdTreeRayCaster.CastBruteForce(_triangles, ray, out RayHit reference);
        if (!found || Mathf.Abs(reference.T - hit.T) > BruteForceDistanceEpsilon)
            GD.PushWarning($"{nameof(RayCaster)}: k-d tree hit (t = {hit.T}) differs from brute force " +
                           $"(found = {found}, t = {reference.T}).");
    }

    private void ShowRayLine(Vector3 start, Vector3 end)
    {
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = new[] { start, end };

        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
        _rayLine.Mesh = arrayMesh;
    }

    private void ShowHighlight(Triangle triangle, Ray ray)
    {
        // Push the highlight towards the viewer to avoid z-fighting with the original surface.
        Vector3 normal = triangle.Normal;
        Vector3 offset = (normal.Dot(ray.Direction) > 0f ? -normal : normal) * HighlightOffset;

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = new[] { triangle.V0 + offset, triangle.V1 + offset, triangle.V2 + offset };

        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        Mesh = arrayMesh;
    }
}
