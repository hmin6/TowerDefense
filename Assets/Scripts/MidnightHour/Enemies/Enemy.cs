using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    /// <summary>
    /// A Shadow walking the path. While walking it is a KINEMATIC rigidbody driven along the path.
    /// When hit on its weakness it becomes a DYNAMIC rigidbody, gets launched by real physics
    /// (knockdown), then recovers and walks back onto the path.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public static int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < All.Count; i++) if (All[i].IsAlive) n++;
                return n;
            }
        }

        enum State { Walking, KnockedDown, Recovering, Dead }

        public EnemyDef Def { get; private set; }
        public int TypeIndex { get; private set; }
        public float HP { get; private set; }
        /// <summary>Meters travelled along the path (used for "first in line" targeting).</summary>
        public float Distance { get; private set; }
        /// <summary>Current world velocity. Towers read this to lead their shots.</summary>
        public Vector3 Velocity { get; private set; }
        /// <summary>Distance from the ground to this enemy's center.</summary>
        public float HeightOffset { get; private set; }

        public bool IsAlive { get { return state != State.Dead; } }
        public bool IsKnockedDown { get { return state == State.KnockedDown; } }
        public bool IsSlowed { get { return slowTimer > 0f; } }
        public float CurrentSpeed { get { return Def.speed * (slowTimer > 0f ? slowMultiplier : 1f); } }
        public Vector3 AimPoint { get { return rb != null ? rb.position : transform.position; } }

        State state = State.Walking;
        Rigidbody rb;
        EnemyPath path;
        float slowTimer;
        float slowMultiplier = 1f;
        float stunTimer;
        const float RecoverSpeed = 6f;

        void Awake() { rb = GetComponent<Rigidbody>(); }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Init(EnemyDef def, int typeIndex, EnemyPath enemyPath)
        {
            Def = def;
            TypeIndex = typeIndex;
            path = enemyPath;
            HP = def.maxHP;
            if (rb == null) rb = GetComponent<Rigidbody>();

            // Measure how far the model reaches below its pivot so it walks ON the ground.
            transform.position = Vector3.zero;
            Bounds b = VisualFactory.RendererBounds(gameObject);
            HeightOffset = Mathf.Max(0.1f, -b.min.y);

            Vector3 p = path.Evaluate(0f, out Vector3 dir) + Vector3.up * HeightOffset;
            transform.SetPositionAndRotation(p, Flat(dir));
            rb.position = p;
            rb.rotation = transform.rotation;
        }

        static Quaternion Flat(Vector3 dir)
        {
            dir.y = 0f;
            return dir.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(dir) : Quaternion.identity;
        }

        /// <summary>Where this enemy will be in t seconds if it keeps following the path (path-aware aim mode).</summary>
        public Vector3 PredictPosition(float t)
        {
            if (state == State.Walking && path != null)
            {
                float d = Mathf.Min(Distance + CurrentSpeed * t, path.TotalLength);
                return path.Evaluate(d, out _) + Vector3.up * HeightOffset;
            }
            return AimPoint + Velocity * t;
        }

        void FixedUpdate()
        {
            if (Def == null || path == null) return;
            float dt = Time.fixedDeltaTime;
            if (slowTimer > 0f) slowTimer -= dt;

            switch (state)
            {
                case State.Walking:
                {
                    float spd = CurrentSpeed;
                    Distance += spd * dt;
                    if (Distance >= path.TotalLength)
                    {
                        ReachedGoal();
                        return;
                    }
                    Vector3 p = path.Evaluate(Distance, out Vector3 dir) + Vector3.up * HeightOffset;
                    rb.MovePosition(p);
                    rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Flat(dir), 720f * dt));
                    Velocity = dir * spd;
                    break;
                }
                case State.KnockedDown:
                {
                    Velocity = rb.GetVelocity();
                    stunTimer -= dt;
                    if (stunTimer <= 0f || rb.position.y < -5f)
                    {
                        rb.isKinematic = true;
                        state = State.Recovering;
                    }
                    break;
                }
                case State.Recovering:
                {
                    Vector3 target = path.Evaluate(Distance, out Vector3 dir) + Vector3.up * HeightOffset;
                    Vector3 next = Vector3.MoveTowards(rb.position, target, RecoverSpeed * dt);
                    Velocity = (next - rb.position) / dt;
                    rb.MovePosition(next);
                    Quaternion want = Flat(dir);
                    rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, want, 540f * dt));
                    if ((next - target).sqrMagnitude < 0.0025f && Quaternion.Angle(rb.rotation, want) < 8f)
                        state = State.Walking;
                    break;
                }
            }
        }

        public void ApplySlow(float multiplier, float duration)
        {
            if (!IsAlive || duration <= 0f) return;
            slowMultiplier = Mathf.Min(multiplier, slowTimer > 0f ? slowMultiplier : 1f);
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        /// <summary>Deals damage with Persona-style weakness / resist rules. Returns what happened.</summary>
        public HitResult TakeDamage(float amount, Element element, Vector3 hitDirection, Tower source)
        {
            if (!IsAlive) return HitResult.None;
            var gm = GameManager.Instance;
            float weakMult = gm != null ? gm.weaknessMultiplier : 1.5f;
            float resistMult = gm != null ? gm.resistMultiplier : 0.5f;

            // Almighty (and None) ignore affinities. If a type is somehow listed as both, the weakness wins.
            bool weak = Def.IsWeakTo(element);
            bool resist = !weak && Def.Resists(element);

            float dmg = amount * (weak ? weakMult : resist ? resistMult : 1f);
            HP -= dmg;

            var hud = HUD.Instance;
            Vector3 above = AimPoint + Vector3.up * (HeightOffset + 0.6f);
            if (hud != null)
            {
                hud.Float(AimPoint, Mathf.RoundToInt(dmg).ToString(), Color.white, 18, 0.6f);
                if (weak) hud.Float(above, "WEAK!", new Color(1f, 0.85f, 0.2f), 30, 0.9f);
                else if (resist) hud.Float(above, "RESIST", new Color(0.65f, 0.65f, 0.7f), 20, 0.7f);
            }

            if (HP <= 0f)
            {
                Die();
                return HitResult.Killed;
            }

            if (weak && state == State.Walking && !Def.IsBoss)
            {
                KnockDown(hitDirection);
                if (gm != null) gm.PlaySfx(gm.weakSound, 0.7f);
                return HitResult.Knockdown;
            }
            return weak ? HitResult.Weak : resist ? HitResult.Resisted : HitResult.Normal;
        }

        void KnockDown(Vector3 hitDirection)
        {
            var gm = GameManager.Instance;
            state = State.KnockedDown;
            stunTimer = gm != null ? gm.knockdownDuration : 1.5f;

            Vector3 push = hitDirection;
            push.y = 0f;
            if (push.sqrMagnitude < 1e-4f) push = -transform.forward;
            push.Normalize();

            rb.isKinematic = false;
            rb.AddForce(push * 2.5f + Vector3.up * 4.5f, ForceMode.VelocityChange);
            rb.AddTorque(Random.onUnitSphere * 6f, ForceMode.VelocityChange);

            if (HUD.Instance != null)
                HUD.Instance.Float(AimPoint + Vector3.up * (HeightOffset + 1.2f), "DOWN", new Color(1f, 0.3f, 0.3f), 24, 0.9f);
        }

        void Die()
        {
            state = State.Dead;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnEnemyKilled(this);
                gm.PlaySfx(Def.deathSound, 0.6f);
            }
            Factory.Shatter(AimPoint, Def.color, Def.IsBoss ? 24 : 7, Def.IsBoss ? 0.5f : 0.25f);
            Destroy(gameObject);
        }

        void ReachedGoal()
        {
            state = State.Dead;
            if (GameManager.Instance != null) GameManager.Instance.EnemyReachedGoal(this);
            Destroy(gameObject);
        }
    }
}
