using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    /// <summary>
    /// Fires continuously at a set rate at the enemy furthest along the path,
    /// leading the shot to the PREDICTED intercept point (velocity-based by default).
    /// </summary>
    public class Tower : MonoBehaviour
    {
        public static readonly List<Tower> All = new List<Tower>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public TowerDef Def { get; private set; }
        public int TypeIndex { get; private set; }
        public Enemy Target { get; private set; }
        public Vector3 LastAimPoint { get; private set; }

        Transform head;
        Transform muzzle;
        LineRenderer aimLine;
        float cooldown;
        float retargetTimer;
        const float TurnSpeed = 720f;

        public float EffectiveDamage
        {
            get { var gm = GameManager.Instance; return gm != null ? gm.EffectiveDamage(Def) : Def.damage; }
        }

        public float EffectiveFireRate
        {
            get { var gm = GameManager.Instance; return gm != null ? gm.EffectiveFireRate(Def) : Def.fireRate; }
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Init(TowerDef def, int typeIndex, Transform headTransform, Transform muzzleTransform)
        {
            Def = def;
            TypeIndex = typeIndex;
            head = headTransform;
            muzzle = muzzleTransform;
            aimLine = VisualFactory.Line(transform, new Color(def.color.r, def.color.g, def.color.b, 0.8f), 0.05f, "Aim Line");
            aimLine.positionCount = 2;
            aimLine.enabled = false;
            cooldown = 0.25f;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool active = gm != null && (gm.State == GameState.Battle || gm.State == GameState.Planning);
            if (!active || Def == null)
            {
                if (aimLine != null) aimLine.enabled = false;
                return;
            }

            float dt = Time.deltaTime;
            cooldown = Mathf.Max(0f, cooldown - dt);
            retargetTimer -= dt;

            if (retargetTimer <= 0f || Target == null || !Target.IsAlive || !InRange(Target))
            {
                Target = FindTarget();
                retargetTimer = 0.15f;
            }

            if (Target == null)
            {
                aimLine.enabled = false;
                return;
            }

            Vector3 launch = ComputeShot(Target, out Vector3 aim);
            LastAimPoint = aim;
            if (launch.sqrMagnitude > 1e-4f)
                head.rotation = Quaternion.RotateTowards(head.rotation, Quaternion.LookRotation(launch), TurnSpeed * dt);

            aimLine.enabled = gm.showAimLines;
            if (aimLine.enabled)
            {
                aimLine.SetPosition(0, muzzle.position);
                aimLine.SetPosition(1, aim);
            }

            if (cooldown <= 0f)
            {
                Projectile.Spawn(this, muzzle.position, launch);
                gm.PlaySfx(Def.fireSound, 0.35f);
                cooldown = 1f / Mathf.Max(0.01f, EffectiveFireRate);
            }
        }

        /// <summary>Returns the launch velocity and the predicted point the shot is aimed at.</summary>
        public Vector3 ComputeShot(Enemy target, out Vector3 aimPoint)
        {
            Vector3 origin = muzzle.position;
            var gm = GameManager.Instance;
            bool pathAware = gm != null && gm.pathAwareAim;

            // Default (assignment spec): predict from CURRENT position + CURRENT velocity.
            Vector3 pos = target.AimPoint, vel = target.Velocity;
            System.Func<float, Vector3> predict;
            if (pathAware) predict = target.PredictPosition;
            else predict = t => pos + vel * t;

            if (Def.ballistic)
                return Aiming.BallisticLaunch(origin, Def.projectileSpeed, Physics.gravity, predict, out aimPoint, out _);

            if (pathAware)
                aimPoint = Aiming.InterceptIterative(origin, Def.projectileSpeed, predict);
            else if (!Aiming.TryIntercept(origin, pos, vel, Def.projectileSpeed, out aimPoint, out _))
                aimPoint = pos;

            return (aimPoint - origin).normalized * Def.projectileSpeed;
        }

        bool InRange(Enemy e)
        {
            Vector3 d = e.AimPoint - transform.position;
            d.y = 0f;
            return d.sqrMagnitude <= Def.range * Def.range;
        }

        /// <summary>"First" targeting: the living enemy in range that is furthest along the path.</summary>
        Enemy FindTarget()
        {
            Enemy best = null;
            float bestDistance = -1f;
            foreach (var e in Enemy.All)
            {
                if (!e.IsAlive || !InRange(e)) continue;
                if (e.Distance > bestDistance)
                {
                    best = e;
                    bestDistance = e.Distance;
                }
            }
            return best;
        }

        /// <summary>Persona "One More": knocking an enemy down gives this tower an instant extra shot.</summary>
        public void OneMore()
        {
            cooldown = 0f;
            if (HUD.Instance != null)
                HUD.Instance.Float(head.position + Vector3.up * 0.8f, "ONE MORE!", new Color(1f, 0.25f, 0.3f), 26, 0.9f);
        }

        void OnDrawGizmosSelected()
        {
            if (Def == null) return;
            Gizmos.color = Def.color;
            Gizmos.DrawWireSphere(transform.position, Def.range);
        }
    }
}
