using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    /// <summary>
    /// A real Rigidbody projectile (gravity on for ballistic shots). Each physics step it sphere-casts
    /// along the distance it just travelled, so even fast shots never tunnel through an enemy.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public static readonly List<Projectile> All = new List<Projectile>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        static readonly RaycastHit[] hitBuffer = new RaycastHit[16];
        static readonly Collider[] overlapBuffer = new Collider[64];

        Tower source;
        TowerDef def;
        float damage;
        Rigidbody rb;
        TrailRenderer trail;
        Vector3 lastPos;
        float radius;
        float life;
        bool done;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static Projectile Spawn(Tower source, Vector3 origin, Vector3 velocity)
        {
            var def = source.Def;
            var go = VisualFactory.Prim(PrimitiveType.Sphere, null, origin, Vector3.one * def.projectileScale,
                                        def.color, 2.5f, keepCollider: false, name: def.displayName + " Shot");
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(go.transform, false);
            var tr = trailGo.AddComponent<TrailRenderer>();
            tr.time = def.ballistic ? 0.4f : 0.18f;
            tr.startWidth = def.projectileScale * 0.9f;
            tr.endWidth = 0f;
            tr.sharedMaterial = VisualFactory.Unlit(Color.white);
            tr.startColor = def.color;
            tr.endColor = new Color(def.color.r, def.color.g, def.color.b, 0f);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = def.ballistic;
            rb.mass = 0.2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.SetVelocity(velocity);

            var p = go.AddComponent<Projectile>();
            p.source = source;
            p.def = def;
            p.damage = source.EffectiveDamage;
            p.rb = rb;
            p.trail = tr;
            p.lastPos = origin;
            p.radius = Mathf.Max(0.05f, def.projectileScale * 0.5f);
            return p;
        }

        void FixedUpdate()
        {
            if (done) return;
            life += Time.fixedDeltaTime;

            Vector3 pos = rb.position;
            Vector3 delta = pos - lastPos;
            float dist = delta.magnitude;
            if (dist > 1e-4f)
            {
                int n = Physics.SphereCastNonAlloc(lastPos, radius, delta / dist, hitBuffer, dist, ~0, QueryTriggerInteraction.Ignore);
                float bestDist = float.MaxValue;
                Vector3 bestPoint = pos;
                Enemy bestEnemy = null;
                bool found = false;

                for (int i = 0; i < n; i++)
                {
                    var h = hitBuffer[i];
                    var col = h.collider;
                    if (col == null) continue;
                    if (col.GetComponentInParent<Tower>() != null) continue;
                    if (col.GetComponent<Debris>() != null) continue;

                    var enemy = col.GetComponentInParent<Enemy>();
                    bool ground = enemy == null && col.GetComponentInParent<GroundMarker>() != null;
                    if (enemy != null && !enemy.IsAlive) continue;
                    if (enemy == null && !ground) continue;

                    if (h.distance < bestDist)
                    {
                        bestDist = h.distance;
                        // A distance of 0 means we started inside the collider; Unity reports point = zero then.
                        bestPoint = h.distance > 0f ? h.point : lastPos;
                        bestEnemy = enemy;
                        found = true;
                    }
                }

                if (found)
                {
                    Impact(bestPoint, bestEnemy, delta / dist);
                    return;
                }
            }

            lastPos = pos;
            if (life > 8f || pos.y < -5f) Destroy(gameObject);
        }

        void Impact(Vector3 point, Enemy enemy, Vector3 travelDir)
        {
            done = true;
            if (def.splashRadius > 0.01f)
            {
                Explode(point);
                Flash.Spawn(point, def.color, def.splashRadius, 0.35f);
                if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.12f);
            }
            else
            {
                if (enemy != null) ApplyHit(enemy, travelDir);
                Flash.Spawn(point, def.color, 0.5f, 0.2f);
            }

            // Let the trail fade out on its own instead of popping.
            if (trail != null)
            {
                trail.transform.SetParent(null, true);
                trail.autodestruct = true;
                trail.emitting = false;
            }
            Destroy(gameObject);
        }

        void ApplyHit(Enemy enemy, Vector3 dir)
        {
            HitResult result = enemy.TakeDamage(damage, def.element, dir, source);
            if (def.slowDuration > 0f && enemy.IsAlive) enemy.ApplySlow(def.slowMultiplier, def.slowDuration);
            if (result == HitResult.Knockdown && source != null) source.OneMore();
        }

        void Explode(Vector3 point)
        {
            int n = Physics.OverlapSphereNonAlloc(point, def.splashRadius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            var hitEnemies = new HashSet<Enemy>();
            for (int i = 0; i < n; i++)
            {
                var col = overlapBuffer[i];
                var e = col.GetComponentInParent<Enemy>();
                if (e != null && e.IsAlive && hitEnemies.Add(e))
                {
                    Vector3 dir = e.AimPoint - point;
                    ApplyHit(e, dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.up);
                }
            }

            // Shove any loose physics bodies (knocked-down Shadows, debris) for extra juice.
            n = Physics.OverlapSphereNonAlloc(point, def.splashRadius * 1.5f, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var body = overlapBuffer[i].attachedRigidbody;
                if (body != null && !body.isKinematic)
                    body.AddExplosionForce(6f, point, def.splashRadius * 1.5f, 0.8f, ForceMode.VelocityChange);
            }
        }
    }
}
