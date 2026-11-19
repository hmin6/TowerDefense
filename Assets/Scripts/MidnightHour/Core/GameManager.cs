using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    public enum GameState { Briefing, Planning, Night, FreeTime, Victory, GameOver, PartySelect, Dismiss }

    /// <summary>Persona-style "Social Links" you rank up between nights.</summary>
    public enum SocialLink { Magician, Empress, Chariot, Fool }

    /// <summary>
    /// Game rules and flow:
    /// Briefing -> PartySelect (Makoto + 3) -> Planning (place towers) -> Night (wave)
    ///   -> FreeTime (rank a Social Link) -> PartySelect (swap members)
    ///   -> Dismiss (only if over the new, lower field limit) -> Planning ...
    /// The game ends the moment any Shadow reaches the Clock Tower.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Rules")]
        public int startingSP = 350;
        public float weaknessMultiplier = 1.5f;
        public float resistMultiplier = 0.5f;
        public float knockdownDuration = 1.6f;

        [Header("Field limit (STORY.md §2)")]
        [Tooltip("Most towers allowed on the field at once, per floor (STORY.md: 8, 7, 6, 6, 5, 4). " +
                 "Until floors exist (#4), night N uses entry N. Later floors reuse the last entry.")]
        public List<int> fieldLimits = new List<int> { 8, 7, 6, 6, 5, 4 };

        [Header("Party (STORY.md §2)")]
        [Tooltip("How many members join Makoto. STORY.md: 3. Keys 1-3 pick their Personas, so 3 is the maximum.")]
        [Range(1, 3)] public int partySize = 3;

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

        /// <summary>What the player can place right now: the party members' Personas first, then Makoto's.</summary>
        public List<TowerDef> Towers { get; private set; } = new List<TowerDef>();
        /// <summary>How many entries at the start of <see cref="Towers"/> belong to party members (keys 1-3).</summary>
        public int MemberTowerCount { get; private set; }
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
        /// <summary>The current floor's field limit. Placing is free; this is the only limit, and nothing raises it.</summary>
        public int FieldLimit { get { return LimitForFloor(WaveIndex); } }
        public bool FieldFull { get { return TowerCount >= FieldLimit; } }
        public bool CanPlaceNow { get { return State == GameState.Planning || State == GameState.Night; } }
        public bool IsModalState
        {
            get
            {
                return State == GameState.Briefing || State == GameState.PartySelect || State == GameState.Dismiss ||
                       State == GameState.FreeTime || State == GameState.Victory || State == GameState.GameOver;
            }
        }
        public int ShadowsRemaining
        {
            get { return (spawner != null ? spawner.RemainingToSpawn : 0) + Enemy.AliveCount; }
        }

        readonly int[] ranks = new int[4];
        // The members chosen to go with Makoto. Cleared on restart: starting over means choosing again.
        readonly List<PartyMember> party = new List<PartyMember>();
        // Towers marked on the dismiss screen; they leave the field when it's confirmed.
        readonly HashSet<Tower> dismissMarks = new HashSet<Tower>();
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

        public void Configure(List<EnemyDef> enemies, List<WaveDef> waves, WaveSpawner waveSpawner)
        {
            Enemies = enemies;
            Waves = waves;
            spawner = waveSpawner;
            SP = startingSP;
            Time.timeScale = 1f;
        }

        /// <summary>The S.E.E.S. roster. Party selection turns it into the placeable <see cref="Towers"/>.</summary>
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
                else if (State == GameState.PartySelect) ConfirmParty();
                else if (State == GameState.Dismiss) ConfirmDismiss();
            }

            // Keys 1-8 toggle members. Skip the frame the screen opened, so the key that closed
            // Free Time (1-4) doesn't also toggle a member.
            if (State == GameState.PartySelect && Time.frameCount != StateChangedFrame)
            {
                for (int i = 0; i < PartyKeys.Length; i++)
                    if (InputCompat.Down(PartyKeys[i])) TogglePartyMember(i);
            }

            // Keys 1-8 mark towers on the dismiss screen (the field never holds more than 8).
            if (State == GameState.Dismiss && Time.frameCount != StateChangedFrame)
            {
                for (int i = 0; i < PartyKeys.Length; i++)
                    if (InputCompat.Down(PartyKeys[i])) ToggleDismiss(i);
            }

            if (State == GameState.FreeTime)
            {
                if (InputCompat.Down(GameKey.Tower1)) ChooseLink(SocialLink.Magician);
                else if (InputCompat.Down(GameKey.Tower2)) ChooseLink(SocialLink.Empress);
                else if (InputCompat.Down(GameKey.Tower3)) ChooseLink(SocialLink.Chariot);
                // No Fool link: it gave +1 tower slot, and nothing may raise the field limit (#3).
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
            // The break between nights continues on the party screen, where members can be swapped.
            SetState(GameState.PartySelect);
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

        /// <summary>Wipes the board and starts again from Day 1 at the party screen (no scene reload needed).</summary>
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
            // Like entering Tartarus: pick who goes in. Starting over means choosing the party again.
            party.Clear();
            dismissMarks.Clear();
            Towers = new List<TowerDef>();
            MemberTowerCount = 0;
            SetState(GameState.PartySelect);
        }

        // ------------------------------------------------------------ Party (STORY.md §2)

        static readonly GameKey[] PartyKeys =
        {
            GameKey.Tower1, GameKey.Tower2, GameKey.Tower3, GameKey.Choice4,
            GameKey.Choice5, GameKey.Choice6, GameKey.Choice7, GameKey.Choice8,
        };

        /// <summary>Members needed besides Makoto (fewer only if the roster is smaller than the party size).</summary>
        public int PartySizeRequired { get { return Mathf.Min(partySize, Members.Count); } }
        public int PartyCount { get { return party.Count; } }
        public bool CanConfirmParty { get { return State == GameState.PartySelect && party.Count == PartySizeRequired; } }
        /// <summary>True before the climb starts; false at a break between nights.</summary>
        public bool IsFirstPartySelect { get { return WaveIndex == 0; } }

        public bool IsInParty(PartyMember id) { return party.Contains(id); }

        /// <summary>Adds or removes a member (index into Members). Returns false if the party is already full.</summary>
        public bool TogglePartyMember(int memberIndex)
        {
            if (State != GameState.PartySelect || memberIndex < 0 || memberIndex >= Members.Count) return false;
            var id = Members[memberIndex].id;
            if (party.Remove(id)) return true;
            if (party.Count >= PartySizeRequired) return false;
            party.Add(id);
            return true;
        }

        /// <summary>Makoto's Personas always stay; a member's tower stays only while they're in the party.</summary>
        bool CanStayOnField(TowerDef def)
        {
            return def.user == PartyMember.Makoto || party.Contains(def.user);
        }

        /// <summary>Placed towers that confirming the current party would remove, as "Io (Yukari)".</summary>
        public List<string> TowersLeavingField()
        {
            var names = new List<string>();
            foreach (var t in Tower.All)
                if (t != null && t.Def != null && !CanStayOnField(t.Def))
                    names.Add($"{t.Def.displayName} ({MemberName(t.Def.user)})");
            return names;
        }

        public string MemberName(PartyMember id)
        {
            if (id == PartyMember.Makoto) return "Makoto";
            foreach (var m in Members) if (m.id == id) return m.displayName;
            return id.ToString();
        }

        /// <summary>Locks in the party: rebuilds the tower bar and withdraws swapped-out members' towers.</summary>
        public void ConfirmParty()
        {
            if (!CanConfirmParty) return;

            var placeable = new List<TowerDef>();
            foreach (var m in Members)                       // roster order, so keys 1-3 are predictable
                if (party.Contains(m.id) && m.persona != null) placeable.Add(m.persona);
            MemberTowerCount = placeable.Count;
            placeable.AddRange(MakotoPersonas);
            Towers = placeable;

            foreach (var t in new List<Tower>(Tower.All))
                if (t != null && t.Def != null && !CanStayOnField(t.Def)) RemoveTower(t);
            if (TowerPlacer.Instance != null) TowerPlacer.Instance.Cancel();

            EnterFloor();
        }

        /// <summary>Starts preparing the floor, unless the field is over its (lower) limit: then the dismiss screen comes first.</summary>
        void EnterFloor()
        {
            if (TowerCount > FieldLimit)
            {
                dismissMarks.Clear();
                SetState(GameState.Dismiss);
                Banner("THE SEAL TIGHTENS", new Color(0.86f, 0.06f, 0.12f));
                return;
            }
            SetState(GameState.Planning);
            if (WaveIndex < Waves.Count) Banner(Waves[WaveIndex].title.ToUpperInvariant(), Color.white);
        }

        // ------------------------------------------------------------ Field limit (STORY.md §2)

        public int LimitForFloor(int floor)
        {
            if (fieldLimits == null || fieldLimits.Count == 0) return 6;
            return Mathf.Max(1, fieldLimits[Mathf.Clamp(floor, 0, fieldLimits.Count - 1)]);
        }

        /// <summary>The limit on the floor before this one (equal to FieldLimit on the first floor).</summary>
        public int PreviousFieldLimit { get { return LimitForFloor(WaveIndex - 1); } }

        public bool IsOnField(TowerDef def)
        {
            foreach (var t in Tower.All) if (t != null && t.Def == def) return true;
            return false;
        }

        /// <summary>Why this Persona can't be placed right now (rules only, not the spot), or null if it can.</summary>
        public string PlacementBlockReason(TowerDef def)
        {
            if (def == null) return "Nothing selected";
            if (IsOnField(def))
                return def.user == PartyMember.Makoto
                    ? $"{def.displayName} is already on the field (each Persona only once)"
                    : $"{MemberName(def.user)}'s {def.displayName} is already on the field (one tower per member)";
            if (FieldFull) return $"Field limit reached ({TowerCount}/{FieldLimit}). Press X on a tower to dismiss it.";
            return null;
        }

        /// <summary>Takes a tower off the field right away (deactivated now, destroyed at the end of the frame).</summary>
        void RemoveTower(Tower t)
        {
            if (t == null) return;
            Flash.Spawn(t.transform.position + Vector3.up, Color.white, 1.2f, 0.3f);
            dismissMarks.Remove(t);
            t.gameObject.SetActive(false); // leaves Tower.All immediately, so counts are right this frame
            Destroy(t.gameObject);
        }

        // ---- dismiss screen

        public IReadOnlyList<Tower> FieldTowers { get { return Tower.All; } }
        public bool IsMarkedForDismiss(Tower t) { return dismissMarks.Contains(t); }
        public int MarkedCount { get { return dismissMarks.Count; } }
        public int TowersAfterDismiss { get { return TowerCount - dismissMarks.Count; } }
        public bool CanConfirmDismiss { get { return State == GameState.Dismiss && TowersAfterDismiss <= FieldLimit; } }

        /// <summary>Marks or unmarks a placed tower (index into FieldTowers) on the dismiss screen.</summary>
        public void ToggleDismiss(int index)
        {
            if (State != GameState.Dismiss || index < 0 || index >= Tower.All.Count) return;
            var t = Tower.All[index];
            if (!dismissMarks.Remove(t)) dismissMarks.Add(t);
        }

        public void ConfirmDismiss()
        {
            if (!CanConfirmDismiss) return;
            foreach (var t in new List<Tower>(dismissMarks)) RemoveTower(t);
            dismissMarks.Clear();
            EnterFloor();
        }

        // ------------------------------------------------------------ Towers

        public bool TryBuildTower(int typeIndex, Vector3 position)
        {
            if (!CanPlaceNow || typeIndex < 0 || typeIndex >= Towers.Count) return false;
            var def = Towers[typeIndex];
            if (PlacementBlockReason(def) != null) return false; // placing is free; only the rules can block it
            Factory.BuildTower(def, typeIndex, position);
            return true;
        }

        /// <summary>X on a tower: it leaves the field, freeing its slot so that Persona can be placed again.</summary>
        public void DismissTower(Tower t)
        {
            if (t == null || !CanPlaceNow) return;
            RemoveTower(t);
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
