using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    /// <summary>
    /// A predetermined, NON-straight path. Control points are smoothed with a Catmull-Rom spline,
    /// then baked into a dense polyline that enemies follow by "distance travelled".
    /// </summary>
    public class EnemyPath : MonoBehaviour
    {
        public static EnemyPath Instance { get; private set; }

        [Tooltip("World-space control points. If empty, this object's children are used in order.")]
        public List<Vector3> controlPoints = new List<Vector3>();

        [Tooltip("Curve samples per control segment. 0 = sharp corners.")]
        [Range(0, 16)] public int smoothSubdivisions = 8;

        readonly List<Vector3> points = new List<Vector3>();
        float[] cumulative = new float[0];

        public float TotalLength { get; private set; }
        public IReadOnlyList<Vector3> Points { get { return points; } }
        public Vector3 StartPoint { get { return points.Count > 0 ? points[0] : transform.position; } }
        public Vector3 EndPoint { get { return points.Count > 0 ? points[points.Count - 1] : transform.position; } }

        void Awake()
        {
            Instance = this;
            if (controlPoints.Count < 2 && transform.childCount >= 2)
            {
                controlPoints.Clear();
                foreach (Transform t in transform) controlPoints.Add(t.position);
            }
            Rebuild();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetControlPoints(IList<Vector3> pts)
        {
            controlPoints = new List<Vector3>(pts);
            Rebuild();
        }

        public void Rebuild()
        {
            points.Clear();
            int n = controlPoints.Count;
            if (n < 2)
            {
                TotalLength = 0f;
                cumulative = new float[0];
                return;
            }

            if (smoothSubdivisions <= 0)
            {
                points.AddRange(controlPoints);
            }
            else
            {
                for (int i = 0; i < n - 1; i++)
                {
                    Vector3 p0 = controlPoints[Mathf.Max(i - 1, 0)];
                    Vector3 p1 = controlPoints[i];
                    Vector3 p2 = controlPoints[i + 1];
                    Vector3 p3 = controlPoints[Mathf.Min(i + 2, n - 1)];
                    for (int s = 0; s < smoothSubdivisions; s++)
                        points.Add(CatmullRom(p0, p1, p2, p3, s / (float)smoothSubdivisions));
                }
                points.Add(controlPoints[n - 1]);
            }

            cumulative = new float[points.Count];
            TotalLength = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                TotalLength += Vector3.Distance(points[i - 1], points[i]);
                cumulative[i] = TotalLength;
            }
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>Position on the path after travelling <paramref name="distance"/> meters, plus the travel direction.</summary>
        public Vector3 Evaluate(float distance, out Vector3 direction)
        {
            direction = Vector3.forward;
            if (points.Count == 0) return transform.position;
            if (points.Count == 1) return points[0];

            distance = Mathf.Clamp(distance, 0f, TotalLength);
            int i = System.Array.BinarySearch(cumulative, distance);
            if (i < 0) i = ~i - 1;
            i = Mathf.Clamp(i, 0, points.Count - 2);

            float segLen = cumulative[i + 1] - cumulative[i];
            float t = segLen > 1e-5f ? (distance - cumulative[i]) / segLen : 0f;
            Vector3 d = points[i + 1] - points[i];
            if (d.sqrMagnitude > 1e-8f) direction = d.normalized;
            return Vector3.Lerp(points[i], points[i + 1], t);
        }

        /// <summary>Shortest ground-plane distance from a point to the path (used to block tower placement).</summary>
        public float DistanceToPathXZ(Vector3 p)
        {
            float best = float.MaxValue;
            Vector2 q = new Vector2(p.x, p.z);
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 a = new Vector2(points[i].x, points[i].z);
                Vector2 b = new Vector2(points[i + 1].x, points[i + 1].z);
                Vector2 ab = b - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / len2) : 0f;
                best = Mathf.Min(best, Vector2.Distance(q, a + ab * t));
            }
            return best;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (points.Count > 1)
            {
                for (int i = 0; i < points.Count - 1; i++) Gizmos.DrawLine(points[i], points[i + 1]);
            }
            else
            {
                for (int i = 0; i < controlPoints.Count - 1; i++) Gizmos.DrawLine(controlPoints[i], controlPoints[i + 1]);
            }
        }
    }
}
