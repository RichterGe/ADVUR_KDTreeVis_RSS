using Godot;

namespace KDTreeVis.KdTree;

/// <summary>A ray in world space: <c>Origin + t * Direction</c> for t &gt;= 0.</summary>
public readonly struct Ray
{
    public readonly Vector3 Origin;
    public readonly Vector3 Direction;

    /// <summary>Creates a ray; the direction is normalized so that t is a world-space distance.</summary>
    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction.Normalized();
    }

    public Vector3 PointAt(float t) => Origin + Direction * t;
}
