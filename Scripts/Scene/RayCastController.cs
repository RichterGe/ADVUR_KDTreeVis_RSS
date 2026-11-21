using System.Diagnostics;
using Godot;
using KDTreeVis.KdTree;

namespace KDTreeVis.Scene;

/// <summary>
/// Shoots a ray from the active camera along its view direction through the k-d tree,
/// draws the ray line, highlights the first hit triangle, and reports the result on the HUD.
/// </summary>
public partial class RayCastController : Node3D
{
    private const float HighlightOffset = 0.002f;
    private const float BruteForceDistanceEpsilon = 1e-4f;

    // The real ray starts at the camera center, where it would be seen end-on and therefore be
    // invisible; the drawn line starts slightly below the camera instead.
    private const float LineStartDrop = 0.15f;
    private const float MissLineLength = 100f;

    [Export] public HudOverlay Hud { get; set; }
    [Export] public Color HighlightColor { get; set; } = new(1.0f, 0.2f, 0.1f);
    [Export] public Color LineColor { get; set; } = new(1.0f, 0.9f, 0.1f);

    private static readonly StringName CastRay = "cast_ray";

    private KdTreeNode _root;
    private Triangle[] _triangles;
    private ImmediateMesh _highlightMesh;
    private ImmediateMesh _rayLineMesh;

    public override void _Ready()
    {
        if (!InputMap.HasAction(CastRay))
            GD.PushError($"{nameof(RayCastController)}: Input action '{CastRay}' is missing in the Input Map.");

        if (Hud == null)
            GD.PushError($"{nameof(RayCastController)}: Assign Hud in the Inspector!");

        _highlightMesh = CreateImmediateOverlay("HitHighlight", HighlightColor, doubleSided: true);
        _rayLineMesh = CreateImmediateOverlay("RayLine", LineColor, doubleSided: false);
    }

    /// <summary>Sets the tree and triangles that rays are cast against.</summary>
    public void SetTree(KdTreeNode root, Triangle[] triangles)
    {
        _root = root;
        _triangles = triangles;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
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

        bool hitFound = KdTreeRayCaster.Cast(_root, _triangles, ray, out RayHit hit);
        VerifyAgainstBruteForce(ray, hitFound, hit);

        if (!hitFound)
        {
            _highlightMesh.ClearSurfaces();
            DrawRayLine(lineStart, ray.PointAt(MissLineLength));
            GD.Print("Ray cast -> no hit.");
            Hud?.ShowRayResult("No hit");
            return;
        }

        DrawRayLine(lineStart, hit.Position);
        DrawHighlight(_triangles[hit.TriangleIndex], ray);
        GD.Print($"Ray cast -> triangle {hit.TriangleIndex}, t = {hit.T:F3}, position = {hit.Position}");
        Hud?.ShowRayResult($"Hit triangle {hit.TriangleIndex}");
    }

    /// <summary>Cross-checks both hits and misses against the brute-force reference (debug builds only).</summary>
    [Conditional("DEBUG")]
    private void VerifyAgainstBruteForce(Ray ray, bool treeHitFound, RayHit treeHit)
    {
        bool bruteHitFound = KdTreeRayCaster.CastBruteForce(_triangles, ray, out RayHit bruteHit);
        bool distanceMatches = !treeHitFound || Mathf.Abs(bruteHit.T - treeHit.T) <= BruteForceDistanceEpsilon;
        if (treeHitFound != bruteHitFound || !distanceMatches)
        {
            GD.PushWarning($"{nameof(RayCastController)}: k-d tree result (hit = {treeHitFound}, t = {treeHit.T}) " +
                           $"differs from brute force (hit = {bruteHitFound}, t = {bruteHit.T}).");
        }
    }

    private ImmediateMesh CreateImmediateOverlay(string name, Color color, bool doubleSided)
    {
        var mesh = new ImmediateMesh();
        var instance = new MeshInstance3D
        {
            Name = name,
            // World-space vertices are written directly, so ignore parent transforms.
            TopLevel = true,
            Mesh = mesh,
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = color,
                CullMode = doubleSided ? BaseMaterial3D.CullModeEnum.Disabled : BaseMaterial3D.CullModeEnum.Back,
            },
        };
        AddChild(instance);
        return mesh;
    }

    private void DrawRayLine(Vector3 start, Vector3 end)
    {
        _rayLineMesh.ClearSurfaces();
        _rayLineMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        _rayLineMesh.SurfaceAddVertex(start);
        _rayLineMesh.SurfaceAddVertex(end);
        _rayLineMesh.SurfaceEnd();
    }

    private void DrawHighlight(in Triangle triangle, Ray ray)
    {
        // Push the highlight slightly towards the viewer to avoid z-fighting with the original surface.
        Vector3 normal = triangle.Normal;
        Vector3 offset = (normal.Dot(ray.Direction) > 0f ? -normal : normal) * HighlightOffset;

        _highlightMesh.ClearSurfaces();
        _highlightMesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        _highlightMesh.SurfaceAddVertex(triangle.V0 + offset);
        _highlightMesh.SurfaceAddVertex(triangle.V1 + offset);
        _highlightMesh.SurfaceAddVertex(triangle.V2 + offset);
        _highlightMesh.SurfaceEnd();
    }
}
