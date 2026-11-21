using Godot;

namespace KDTreeVis.KdTree;

/// <summary>An immutable world-space triangle with precomputed centroid and bounding box.</summary>
public readonly struct Triangle
{
	public readonly Vector3 V0;
	public readonly Vector3 V1;
	public readonly Vector3 V2;
	public readonly Vector3 Centroid;
	public readonly Aabb Bounds;

	/// <summary>Creates a triangle from three world-space vertices.</summary>
	public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
	{
		V0 = v0;
		V1 = v1;
		V2 = v2;
		Centroid = (v0 + v1 + v2) / 3.0f;
		Bounds = new Aabb(v0, Vector3.Zero).Expand(v1).Expand(v2);
	}

	/// <summary>
	/// Outward-facing unit normal (not checked for degenerate triangles).
	/// Godot uses clockwise front-face winding, so (V2 - V0) x (V1 - V0) points outward.
	/// </summary>
	public Vector3 Normal => (V2 - V0).Cross(V1 - V0).Normalized();
}
