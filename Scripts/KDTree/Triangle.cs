using System;
using Godot;

namespace KDTreeVis.KdTree;

public readonly struct Triangle
{
	public readonly int Id;
	public readonly Vector3 V0;
	public readonly Vector3 V1;
	public readonly Vector3 V2;
	public readonly Vector3 Centroid;
	public readonly Aabb Bounds;

	public Triangle(int id, Vector3 v0, Vector3 v1, Vector3 v2)
	{
		Id = id;
		V0 = v0;
		V1 = v1;
		V2 = v2;

		Centroid = (v0 + v1 + v2) / 3.0f;

		Vector3 min = new(
			MathF.Min(v0.X, MathF.Min(v1.X, v2.X)),
			MathF.Min(v0.Y, MathF.Min(v1.Y, v2.Y)),
			MathF.Min(v0.Z, MathF.Min(v1.Z, v2.Z))
		);

		Vector3 max = new(
			MathF.Max(v0.X, MathF.Max(v1.X, v2.X)),
			MathF.Max(v0.Y, MathF.Max(v1.Y, v2.Y)),
			MathF.Max(v0.Z, MathF.Max(v1.Z, v2.Z))
		);

		// In Godot, Aabb is defined by Position (min corner) and Size (extents).
		Bounds = new Aabb(min, max - min);
	}

	/// Geometric face normal (not unit-length-checked for degenerate tris).
	/// Note: Godot uses clockwise front-face winding, so (V2 - V0) x (V1 - V0) points outward.
	public Vector3 Normal => (V2 - V0).Cross(V1 - V0).Normalized();
}
