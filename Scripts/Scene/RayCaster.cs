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

    [Export] public Color HighlightColor { get; set; } = new(1.0f, 0.2f, 0.1f);

    private static readonly StringName CastRay = "cast_ray";

    private KdTreeNode _root;
    private Triangle[] _triangles;

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
    }

    /// <summary>Sets the tree and triangles that rays are cast against.</summary>
    public void SetTree(KdTreeNode root, Triangle[] triangles)
    {
        _root = root;
        _triangles = triangles;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(CastRay))
            CastFromCamera();
    }

    private void CastFromCamera()
    {
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null || _root == null)
            return;

        var ray = new Ray(camera.GlobalPosition, -camera.GlobalTransform.Basis.Z);

        if (!KdTreeRayCaster.Cast(_root, _triangles, ray, out RayHit hit))
        {
            Mesh = null;
            GD.Print("Ray cast -> no hit.");
            return;
        }

        ShowHighlight(_triangles[hit.TriangleIndex], ray);
        GD.Print($"Ray cast -> triangle {hit.TriangleIndex}, t = {hit.T:F3}, position = {hit.Position}");
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
