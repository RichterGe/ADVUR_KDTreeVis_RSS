using System;
using Godot;

namespace KDTreeVis.KdTree;

/// <summary>
/// Own ray casting implementation: front-to-back k-d tree traversal plus a brute-force reference.
/// Uses the slab method for ray-AABB and Möller-Trumbore for ray-triangle tests.
/// </summary>
public static class KdTreeRayCaster
{
    private const float ParallelEpsilon = 1e-8f;
    private const float DeterminantEpsilon = 1e-8f;
    private const float HitDistanceEpsilon = 1e-5f;
    private const float IntervalEpsilon = 1e-5f;

    /// <summary>Finds the first triangle hit by the ray, visiting only cells the ray passes through.</summary>
    public static bool Cast(KdTreeNode root, Triangle[] triangles, Ray ray, out RayHit hit)
    {
        hit = default;
        if (!IntersectAabb(root.Bounds, ray, out float tMin, out float tMax))
            return false;

        var traversal = new TraversalState(triangles, ray);
        traversal.Traverse(root, MathF.Max(tMin, 0f), tMax);

        if (traversal.BestIndex < 0)
            return false;

        hit = new RayHit(traversal.BestIndex, traversal.BestT, ray.PointAt(traversal.BestT));
        return true;
    }

    /// <summary>Reference implementation that tests every triangle; used to validate the tree traversal.</summary>
    public static bool CastBruteForce(Triangle[] triangles, Ray ray, out RayHit hit)
    {
        float bestT = float.PositiveInfinity;
        int bestIndex = -1;

        for (int i = 0; i < triangles.Length; i++)
        {
            if (IntersectTriangle(triangles[i], ray, out float t) && t < bestT)
            {
                bestT = t;
                bestIndex = i;
            }
        }

        hit = bestIndex < 0 ? default : new RayHit(bestIndex, bestT, ray.PointAt(bestT));
        return bestIndex >= 0;
    }

    /// <summary>Stack-allocated traversal state so recursive calls stay concise and allocation-free.</summary>
    private struct TraversalState
    {
        private readonly Triangle[] _triangles;
        private readonly Ray _ray;

        public float BestT;
        public int BestIndex;

        public TraversalState(Triangle[] triangles, Ray ray)
        {
            _triangles = triangles;
            _ray = ray;
            BestT = float.PositiveInfinity;
            BestIndex = -1;
        }

        /// <summary>
        /// Traverses the subtree for the ray interval [tMin, tMax] inside the node's cell.
        /// Returns true when a hit inside this interval was found, so callers can stop early.
        /// </summary>
        public bool Traverse(KdTreeNode node, float tMin, float tMax)
        {
            if (node.IsLeaf)
            {
                TestLeaf(node);
                // A triangle may span several cells, so only a hit inside this cell's interval is final.
                return BestT <= tMax + IntervalEpsilon;
            }

            int axis = node.SplitAxis;
            float origin = _ray.Origin[axis];
            float direction = _ray.Direction[axis];
            float split = node.SplitPosition;

            bool leftIsNear = origin < split || (origin == split && direction <= 0f);
            KdTreeNode near = leftIsNear ? node.Left : node.Right;
            KdTreeNode far = leftIsNear ? node.Right : node.Left;

            // Ray runs parallel to the split plane and therefore never crosses it.
            if (MathF.Abs(direction) < ParallelEpsilon)
                return Traverse(near, tMin, tMax);

            float tSplit = (split - origin) / direction;

            // tSplit < 0 means the split plane lies behind the ray origin (ray moves away from it),
            // and tSplit > tMax means it only crosses after leaving this cell; either way only near is hit.
            if (tSplit > tMax || tSplit < 0f)
                return Traverse(near, tMin, tMax);

            // 0 <= tSplit < tMin means the ray already crossed the split plane before entering this cell.
            if (tSplit < tMin)
                return Traverse(far, tMin, tMax);

            if (Traverse(near, tMin, tSplit))
                return true;

            return Traverse(far, tSplit, tMax);
        }

        private void TestLeaf(KdTreeNode leaf)
        {
            ReadOnlySpan<int> indices = leaf.TriangleIndices;
            for (int i = 0; i < indices.Length; i++)
            {
                int triangleIndex = indices[i];
                if (IntersectTriangle(_triangles[triangleIndex], _ray, out float t) && t < BestT)
                {
                    BestT = t;
                    BestIndex = triangleIndex;
                }
            }
        }
    }

    /// <summary>Slab method. Returns the entry/exit parameters of the ray within the box.</summary>
    private static bool IntersectAabb(Aabb box, Ray ray, out float tMin, out float tMax)
    {
        tMin = float.NegativeInfinity;
        tMax = float.PositiveInfinity;

        Vector3 boxMin = box.Position;
        Vector3 boxMax = box.End;

        for (int axis = 0; axis < 3; axis++)
        {
            float origin = ray.Origin[axis];
            float direction = ray.Direction[axis];

            if (MathF.Abs(direction) < ParallelEpsilon)
            {
                if (origin < boxMin[axis] || origin > boxMax[axis])
                    return false;
                continue;
            }

            float t0 = (boxMin[axis] - origin) / direction;
            float t1 = (boxMax[axis] - origin) / direction;
            if (t0 > t1)
                (t0, t1) = (t1, t0);

            tMin = MathF.Max(tMin, t0);
            tMax = MathF.Min(tMax, t1);
            if (tMin > tMax)
                return false;
        }

        return tMax >= 0f;
    }

    /// <summary>Möller-Trumbore intersection test (two-sided, so back faces are hit as well).</summary>
    private static bool IntersectTriangle(in Triangle triangle, Ray ray, out float t)
    {
        t = 0f;

        Vector3 edge1 = triangle.V1 - triangle.V0;
        Vector3 edge2 = triangle.V2 - triangle.V0;
        Vector3 pVec = ray.Direction.Cross(edge2);
        float determinant = edge1.Dot(pVec);
        if (MathF.Abs(determinant) < DeterminantEpsilon)
            return false;

        // Solve for barycentric coordinates (u, v) and ray distance t.
        float inverseDeterminant = 1f / determinant;
        Vector3 tVec = ray.Origin - triangle.V0;
        float u = tVec.Dot(pVec) * inverseDeterminant;
        if (u < 0f || u > 1f)
            return false;

        Vector3 qVec = tVec.Cross(edge1);
        float v = ray.Direction.Dot(qVec) * inverseDeterminant;
        if (v < 0f || u + v > 1f)
            return false;

        t = edge2.Dot(qVec) * inverseDeterminant;
        return t > HitDistanceEpsilon;
    }
}
