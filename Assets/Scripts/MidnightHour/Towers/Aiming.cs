using UnityEngine;

namespace MidnightHour
{
    /// <summary>
    /// Target-leading math.
    ///
    /// 1) TryIntercept: classic closed-form lead for a straight (no-gravity) projectile.
    ///    Solve |targetPos + targetVel * t - shooter| = speed * t for the smallest t &gt; 0:
    ///      (v.v - s^2) t^2 + 2 (d.v) t + d.d = 0,  where d = targetPos - shooter.
    ///
    /// 2) BallisticLaunch: lead for a gravity-affected projectile with a fixed HORIZONTAL speed.
    ///    Fixed-point iterate the flight time T, then solve the launch velocity exactly:
    ///      p(T) = origin + v0 T + 1/2 g T^2  ->  v0 = (aim - origin) / T - 1/2 g T
    ///    plus a small correction for PhysX's semi-implicit Euler integrator so the
    ///    simulated shell lands where the math says it will.
    ///
    /// 3) InterceptIterative: same idea as (1) but for any predictor function
    ///    (used by the optional "path-aware" aim mode that predicts around corners).
    /// </summary>
    public static class Aiming
    {
        public static bool TryIntercept(Vector3 shooter, Vector3 targetPos, Vector3 targetVel, float speed,
                                        out Vector3 aimPoint, out float time)
        {
            aimPoint = targetPos;
            time = 0f;
            Vector3 d = targetPos - shooter;
            float a = Vector3.Dot(targetVel, targetVel) - speed * speed;
            float b = 2f * Vector3.Dot(d, targetVel);
            float c = Vector3.Dot(d, d);

            float t;
            if (Mathf.Abs(a) < 1e-5f)
            {
                // Target as fast as the projectile: equation becomes linear.
                if (Mathf.Abs(b) < 1e-5f) return false;
                t = -c / b;
            }
            else
            {
                float disc = b * b - 4f * a * c;
                if (disc < 0f) return false; // can't catch it
                float sq = Mathf.Sqrt(disc);
                float t1 = (-b - sq) / (2f * a);
                float t2 = (-b + sq) / (2f * a);
                t = Mathf.Min(t1, t2);
                if (t <= 0f) t = Mathf.Max(t1, t2);
            }

            if (t <= 0f) return false;
            time = t;
            aimPoint = targetPos + targetVel * t;
            return true;
        }

        public static Vector3 InterceptIterative(Vector3 origin, float speed, System.Func<float, Vector3> predict,
                                                 int iterations = 8)
        {
            Vector3 p = predict(0f);
            for (int i = 0; i < iterations; i++)
            {
                float t = Vector3.Distance(origin, p) / Mathf.Max(0.01f, speed);
                p = predict(t);
            }
            return p;
        }

        public static Vector3 BallisticLaunch(Vector3 origin, float horizontalSpeed, Vector3 gravity,
                                              System.Func<float, Vector3> predict,
                                              out Vector3 aimPoint, out float flightTime, int iterations = 8)
        {
            Vector3 p = predict(0f);
            float T = 0.15f;
            for (int i = 0; i < iterations; i++)
            {
                Vector3 d = p - origin;
                float horizontal = new Vector2(d.x, d.z).magnitude;
                T = Mathf.Max(0.15f, horizontal / Mathf.Max(0.1f, horizontalSpeed));
                p = predict(T);
            }

            aimPoint = p;
            flightTime = T;
            // Exact continuous solution, then compensate for discrete integration:
            // semi-implicit Euler adds +1/2 g T dt of displacement, so remove it from v0.
            float dt = Time.fixedDeltaTime;
            return (p - origin) / T - 0.5f * gravity * T - 0.5f * gravity * dt;
        }
    }
}
