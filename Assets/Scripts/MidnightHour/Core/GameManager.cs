using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    public enum GameState { Briefing, Planning, Night, FreeTime, Victory, GameOver }

    /// <summary>Persona-style "Social Links" you rank up between nights.</summary>
    public enum SocialLink { Magician, Empress, Chariot, Fool }

    /// <summary>
    /// Game rules and flow:
    /// Briefing -> Planning (place towers) -> Night (wave) -> FreeTime (rank a Social Link) -> Planning ...
    /// The game ends the moment any Shadow reaches the Clock Tower.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Rules")]
        public int startingSP = 350;
        public int baseTowerLimit = 6;
        [Range(0f, 1f)] public float refundFraction = 0.5f;
        public float weaknessMultiplier = 1.5f;
        public float resistMultiplier = 0.5f;
        public float knockdownDuration = 1.6f;

        [Header("Social Links (per rank)")]
        public float rankDamageBonus = 0.25f;
        public float rankFireRateBonus = 0.10f;

        [Header("All-Out Attack")]
        [Tooltip("Fraction of each Shadow's MAX HP dealt when every Shadow on the field is knocked down.")]
        public float allOutDamageFraction = 0.5f;
        public float allOutCooldown = 6f;

        [Header("Showcase toggles (also P / L in game)")]
        [Tooltip("OFF = lead from current velocity (assignment spec). ON = predict along the path, around corners.")]
        public bool pathAwareAim = false;
        public bool showAimLines = false;

        [Header("Audio (optional)")]
        public AudioClip weakSound;
        public AudioClip allOutSound;
        public AudioClip killSound;
        public AudioClip gameOverSound;
        public AudioClip victorySound;

        public List<TowerDef> Towers { get; private set; } = new List<TowerDef>();
        public List<EnemyDef> Enemies { get; private set; } = new List<EnemyDef>();
        public List<WaveDef> Waves { get; private set; } = new List<WaveDef>();
        public List<MemberDef> Members { get; private set; } = new List<MemberDef>();
        public List<TowerDef> MakotoPersonas { get; private set; } = new List<TowerDef>();

        public GameState State { get; private set; } = GameState.Briefing;
        public int StateChangedFrame { get; private set; } = -1;
        public int SP { get; private set; }
        public int WaveIndex { get; private set; }
        public int Kills { get; private set; }
        public int AllOutCount { get; private set; }
        public bool IsPaused { get; private set; }
        public int GameSpeed { get; private set; } = 1;

        public int TowerCount { get { return Tower.All.Count; } }
        public int TowerLimit { get { return baseTowerLimit + Rank(SocialLink.Fool); } }
        public bool CanPlaceNow { get { return State == GameState.Planning || State == GameState.Night; } }
        public bool IsModalState
        {
            get { return State == GameState.Briefing || State == GameState.FreeTime || State == GameState.Victory || State == GameState.GameOver; }
        }
        public int ShadowsRemaining
        {
            get { return (spawner != null ? spawner.RemainingToSpawn : 0) + Enemy.AliveCount; }
        }

        readonly int[] ranks = new int[4];
        WaveSpawner spawner;
        AudioSource sfx;
        float allOutTimer;

        void Awake()
        {
            Instance = this;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        public void Configure(List<TowerDef> towers, List<EnemyDef> enemies, List<WaveDef> waves, WaveSpawner waveSpawner)
        {
            Towers = towers;
            Enemies = enemies;
            Waves = waves;
            spawner = waveSpawner;
            SP = startingSP;
            Time.timeScale = 1f;
        }

        /// <summary>The S.E.E.S. roster. Not placeable yet: party selection (#2) turns it into the tower bar.</summary>
        public void ConfigureRoster(List<MemberDef> members, List<TowerDef> makotoPersonas)
        {
            Members = members ?? new List<MemberDef>();
            MakotoPersonas = makotoPersonas ?? new List<TowerDef>();
        }

        // ------------------------------------------------------------ Loop

        void Update()
        {
            HandleKeys();

            if (State == GameState.Night)
            {
                CheckAllOutAttack();
                if (spawner != null && spawner.Done && Enemy.AliveCount == 0) WaveCleared();
            }
        }

        void HandleKeys()
        {
            if (InputCompat.Down(GameKey.StartNight))
            {
                if (State == GameState.Briefing) BeginGame();
                else if (State == GameState.Planning) StartNight();
            }

            if (State == GameState.FreeTime)
            {
                if (InputCompat.Down(GameKey.Tower1)) ChooseLink(SocialLink.Magician);
                else if (InputCompat.Down(GameKey.Tower2)) ChooseLink(SocialLink.Empress);
                else if (InputCompat.Down(GameKey.Tower3)) ChooseLink(SocialLink.Chariot);
                else if (InputCompat.Down(GameKey.Choice4)) ChooseLink(SocialLink.Fool);
            }

            if (InputCompat.Down(GameKey.Pause) && CanPlaceNow) TogglePause();
            if (InputCompat.Down(GameKey.Speed)) CycleSpeed();
            if (InputCompat.Down(GameKey.AimMode))
            {
                pathAwareAim = !pathAwareAim;
                Banner(pathAwareAim ? "PATH-AWARE AIM" : "LINEAR LEAD AIM", Color.white);
            }
            if (InputCompat.Down(GameKey.AimLines)) showAimLines = !showAimLines;
            if (InputCompat.Down(GameKey.Restart) && (State == GameState.Victory || State == GameState.GameOver || IsPaused))
                ResetGame();
        }

        void SetState(GameState s)
        {
            State = s;
            StateChangedFrame = Time.frameCount;
            ApplyTimeScale();
        }

        void ApplyTimeScale()
        {
            Time.timeScale = (State == GameState.GameOver || IsPaused) ? 0f : GameSpeed;
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            ApplyTimeScale();
        }

        public void CycleSpeed()
        {
            GameSpeed = GameSpeed >= 3 ? 1 : GameSpeed + 1;
            ApplyTimeScale();
        }

        // ------------------------------------------------------------ Flow

        public void BeginGame()
        {
            ResetGame();
        }

        public void StartNight()
        {
            if (State != GameState.Planning || WaveIndex >= Waves.Count || spawner == null) return;
            SetState(GameState.Night);
            spawner.Begin(Waves[WaveIndex], Enemies);
            Banner("THE DARK HOUR", new Color(0.86f, 0.06f, 0.12f));
        }

        void WaveCleared()
        {
            var wave = Waves[WaveIndex];
            SP += wave.clearBonus;
            WaveIndex++;
            if (WaveIndex >= Waves.Count)
            {
                SetState(GameState.Victory);
                Banner("DAWN BREAKS", new Color(1f, 0.85f, 0.2f));
                PlaySfx(victorySound);
            }
            else
            {
                SetState(GameState.FreeTime);
                Banner(wave.clearBonus > 0 ? $"NIGHT CLEARED  +{wave.clearBonus} SP" : "NIGHT CLEARED", new Color(1f, 0.85f, 0.2f));
            }
        }

        public void ChooseLink(SocialLink link)
        {
            if (State != GameState.FreeTime) return;
            ranks[(int)link]++;
            SetState(GameState.Planning);
            Banner($"{link.ToString().ToUpperInvariant()}  RANK {ranks[(int)link]}", new Color(1f, 0.85f, 0.2f));
        }

        public void EnemyReachedGoal(Enemy e)
        {
            if (State != GameState.Night) return;
            if (spawner != null) spawner.Stop();
            SetState(GameState.GameOver);
            Banner("THE CLOCK TOWER HAS FALLEN", new Color(0.86f, 0.06f, 0.12f));
            PlaySfx(gameOverSound);
            if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.8f);
        }

        public void OnEnemyKilled(Enemy e)
        {
            Kills++;
            SP += e.Def.spReward;
            PlaySfx(killSound, 0.5f);
        }

        /// <summary>Wipes the board and starts again from Day 1 (no scene reload needed).</summary>
        public void ResetGame()
        {
            if (spawner != null) spawner.Stop();
            foreach (var e in new List<Enemy>(Enemy.All)) if (e != null) Destroy(e.gameObject);
            foreach (var t in new List<Tower>(Tower.All)) if (t != null) Destroy(t.gameObject);
            foreach (var p in new List<Projectile>(Projectile.All)) if (p != null) Destroy(p.gameObject);
            if (TowerPlacer.Instance != null) TowerPlacer.Instance.Cancel();

            for (int i = 0; i < ranks.Length; i++) ranks[i] = 0;
            SP = startingSP;
            WaveIndex = 0;
            Kills = 0;
            AllOutCount = 0;
            allOutTimer = 0f;
            IsPaused = false;
            GameSpeed = 1;
            SetState(GameState.Planning);
            if (Waves.Count > 0) Banner(Waves[0].title.ToUpperInvariant(), Color.white);
        }

        // ------------------------------------------------------------ Towers

        public bool TryBuildTower(int typeIndex, Vector3 position)
        {
            if (!CanPlaceNow || typeIndex < 0 || typeIndex >= Towers.Count) return false;
            var def = Towers[typeIndex];
            if (SP < def.cost || TowerCount >= TowerLimit) return false;
            SP -= def.cost;
            Factory.BuildTower(def, typeIndex, position);
            return true;
        }

        public int RefundFor(Tower t)
        {
            return Mathf.RoundToInt(t.Def.cost * refundFraction);
        }

        public void SellTower(Tower t)
        {
            if (t == null) return;
            SP += RefundFor(t);
            Flash.Spawn(t.transform.position + Vector3.up, Color.white, 1.2f, 0.3f);
            Destroy(t.gameObject);
        }

        // ------------------------------------------------------------ Social Links

        public int Rank(SocialLink link) { return ranks[(int)link]; }

        public static SocialLink? LinkFor(Element e)
        {
            switch (e)
            {
                case Element.Fire: return SocialLink.Magician;
                case Element.Ice: return SocialLink.Empress;
                case Element.Elec: return SocialLink.Chariot;
                default: return null;
            }
        }

        public int RankFor(Element e)
        {
            var link = LinkFor(e);
            return link.HasValue ? Rank(link.Value) : 0;
        }

        public float EffectiveDamage(TowerDef d) { return d.damage * (1f + rankDamageBonus * RankFor(d.element)); }
        public float EffectiveFireRate(TowerDef d) { return d.fireRate * (1f + rankFireRateBonus * RankFor(d.element)); }

        // ------------------------------------------------------------ All-Out Attack

        void CheckAllOutAttack()
        {
            allOutTimer -= Time.deltaTime;
            if (allOutTimer > 0f) return;

            int alive = 0, down = 0;
            foreach (var e in Enemy.All)
            {
                if (!e.IsAlive) continue;
                alive++;
                if (e.IsKnockedDown) down++;
            }
            if (alive < 2 || down != alive) return;

            allOutTimer = allOutCooldown;
            AllOutCount++;
            Banner("ALL-OUT ATTACK!", new Color(0.86f, 0.06f, 0.12f));
            PlaySfx(allOutSound);
            if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.5f);

            foreach (var e in new List<Enemy>(Enemy.All))
            {
                if (e == null || !e.IsAlive) continue;
                Flash.Spawn(e.AimPoint, Color.white, 1.4f, 0.4f);
                e.TakeDamage(e.Def.maxHP * allOutDamageFraction, Element.Almighty, Vector3.up, null);
            }
        }

        // ------------------------------------------------------------ Helpers

        public void Banner(string text, Color color)
        {
            if (HUD.Instance != null) HUD.Instance.Banner(text, color);
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip != null && sfx != null) sfx.PlayOneShot(clip, volume);
        }
    }
}
