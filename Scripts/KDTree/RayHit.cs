using Godot;

namespace KDTreeVis.KdTree;

/// <summary>Result of a ray cast: the first triangle hit and the ray parameter of the hit.</summary>
/// <param name="TriangleIndex">Index into the triangle array.</param>
/// <param name="T">Distance along the ray (the ray direction is normalized).</param>
/// <param name="Position">World-space hit position.</param>
public readonly record struct RayHit(int TriangleIndex, float T, Vector3 Position);
