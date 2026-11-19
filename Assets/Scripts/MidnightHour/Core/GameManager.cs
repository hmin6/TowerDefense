using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    public enum GameState { Briefing, Planning, Battle, Scan, Victory, GameOver, PartySelect, Dismiss }

    /// <summary>Persona-style "Social Links". Nothing ranks them up any more; #14 replaces them with bonds.</summary>
    public enum SocialLink { Magician, Empress, Chariot, Fool }

    /// <summary>
    /// Game rules and flow (STORY.md §2). S.E.E.S. climbs 6 sealed floors of Tartarus:
    ///
    ///   Briefing -> PartySelect (Makoto + 3) -> Scan (arrive: the stairs seal, Fuuka scans floor 1)
    ///     -> Planning (deploy) -> Battle (the floor's waves) -> SEAL BROKEN
    ///     -> Scan (the stairs breather: Fuuka previews the next floor) -> PartySelect (swap members)
    ///     -> Dismiss (only if over the new, lower field limit) -> Planning (next floor) -> ...
    ///   Clearing floor 6 is a victory.
    ///
    /// The game ends the moment any Shadow reaches the team's position (S.E.E.S. is overrun).
    /// Then: Retry floor (same party, this floor again, empty field) or Retreat (restart from floor 1).
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

        [Header("Floors")]
        [Tooltip("Seconds between two waves on the same floor (towers can still be placed).")]
        public float wavePause = 4f;

        [Header("Field limit (STORY.md §2)")]
        [Tooltip("Most towers allowed on the field at once, per floor (STORY.md: 8, 7, 6, 6, 5, 4). " +
                 "Floor N uses entry N; later floors reuse the last entry.")]
        public List<int> fieldLimits = new List<int> { 8, 7, 6, 6, 5, 4 };

        [Header("Party (STORY.md §2)")]
        [Tooltip("How many members join Makoto. STORY.md: 3. Keys 1-3 pick their Personas, so 3 is the maximum.")]
        [Range(1, 3)] public int partySize = 3;

        [Header("Social Links (per rank; removed in #14)")]
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
        public List<FloorDef> Floors { get; private set; } = new List<FloorDef>();
        public List<MemberDef> Members { get; private set; } = new List<MemberDef>();
        public List<TowerDef> MakotoPersonas { get; private set; } = new List<TowerDef>();

        public GameState State { get; private set; } = GameState.Briefing;
        public int StateChangedFrame { get; private set; } = -1;
        public int SP { get; private set; }
        /// <summary>0-based floor the team is on (or, during the breather, the floor it's climbing to).</summary>
        public int FloorIndex { get; private set; }
        /// <summary>0-based wave within the current floor.</summary>
        public int WaveInFloor { get; private set; }
        /// <summary>Seconds until the next wave on this floor starts (0 = a wave is running).</summary>
        public float WaveGapTimer { get; private set; }
        /// <summary>True when the Scan screen is the stairs breather (preview of the next floor), false on arrival at floor 1.</summary>
        public bool ScanIsBreather { get; private set; }
        public int Kills { get; private set; }
        public int AllOutCount { get; private set; }
        public bool IsPaused { get; private set; }
        public int GameSpeed { get; private set; } = 1;

        public int TowerCount { get { return Tower.All.Count; } }
        /// <summary>The current floor's field limit. Placing is free; this is the only limit, and nothing raises it.</summary>
        public int FieldLimit { get { return LimitForFloor(FloorIndex); } }
        public bool FieldFull { get { return TowerCount >= FieldLimit; } }
        public bool CanPlaceNow { get { return State == GameState.Planning || State == GameState.Battle; } }
        public bool IsModalState
        {
            get
            {
                return State == GameState.Briefing || State == GameState.PartySelect || State == GameState.Dismiss ||
                       State == GameState.Scan || State == GameState.Victory || State == GameState.GameOver;
            }
        }
        public int ShadowsRemaining
        {
            get
            {
                int queued = 0;
                var floor = CurrentFloor;
                if (State == GameState.Battle && floor != null)
                    for (int w = WaveInFloor + (WaveGapTimer > 0f ? 0 : 1); w < floor.waves.Count; w++)
                        queued += CountShadows(floor.waves[w]);
                return queued + (spawner != null ? spawner.RemainingToSpawn : 0) + Enemy.AliveCount;
            }
        }

        public FloorDef CurrentFloor { get { return FloorIndex >= 0 && FloorIndex < Floors.Count ? Floors[FloorIndex] : null; } }
        public int FloorNumber { get { return Mathf.Min(FloorIndex + 1, Floors.Count); } }
        public int WaveCount { get { var f = CurrentFloor; return f != null ? f.waves.Count : 0; } }

        public string FloorTitle(int floor)
        {
            if (floor < 0 || floor >= Floors.Count) return "";
            var t = Floors[floor].title;
            return string.IsNullOrEmpty(t) ? $"Floor {floor + 1}" : t;
        }

        readonly int[] ranks = new int[4];
        // The members chosen to go with Makoto. Cleared on restart: starting over means choosing again.
        readonly List<PartyMember> party = new List<PartyMember>();
        // Towers marked on the dismiss screen; they leave the field when it's confirmed.
        readonly HashSet<Tower> dismissMarks = new HashSet<Tower>();
        WaveSpawner spawner;
        System.Action<List<Vector3>> applyLayout;
        AudioSource sfx;
        float allOutTimer;
        // Snapshot taken when a floor's deploy phase starts, so Retry floor can rewind to it.
        int floorStartSP, floorStartKills, floorStartAllOuts;

        static readonly Color SealGold = new Color(1f, 0.85f, 0.2f);
        static readonly Color DangerRed = new Color(0.86f, 0.06f, 0.12f);

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

        /// <param name="layout">Builds a floor's path and its visuals (null or under 2 points = the board's default path).</param>
        public void Configure(List<EnemyDef> enemies, List<FloorDef> floors, WaveSpawner waveSpawner,
                              System.Action<List<Vector3>> layout)
        {
            Enemies = enemies;
            Floors = floors ?? new List<FloorDef>();
            spawner = waveSpawner;
            applyLayout = layout;
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

            if (State == GameState.Battle)
            {
                CheckAllOutAttack();
                if (WaveGapTimer > 0f)
                {
                    WaveGapTimer -= Time.deltaTime;
                    if (WaveGapTimer <= 0f) BeginWave();
                }
                else if (spawner != null && spawner.Done && Enemy.AliveCount == 0)
                {
                    WaveCleared();
                }
            }
        }

        void HandleKeys()
        {
            if (InputCompat.Down(GameKey.StartNight))
            {
                if (State == GameState.Briefing) BeginGame();
                else if (State == GameState.Planning) StartBattle();
                else if (State == GameState.Scan) ContinueFromScan();
                else if (State == GameState.PartySelect) ConfirmParty();
                else if (State == GameState.Dismiss) ConfirmDismiss();
                else if (State == GameState.GameOver) RetryFloor();
            }

            // Keys 1-8 toggle members. Skip the frame the screen opened, so the key that opened it can't also toggle.
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

        // ------------------------------------------------------------ Flow: floors and waves

        public void BeginGame()
        {
            ResetGame();
        }

        /// <summary>Planning -> Battle: the floor's waves start, one after another.</summary>
        public void StartBattle()
        {
            if (State != GameState.Planning || CurrentFloor == null || spawner == null) return;
            SetState(GameState.Battle);
            WaveInFloor = 0;
            WaveGapTimer = 0f;
            Banner("THE SHADOWS ARE COMING", DangerRed);
            BeginWave();
        }

        void BeginWave()
        {
            WaveGapTimer = 0f;
            var floor = CurrentFloor;
            if (floor == null || WaveInFloor >= floor.waves.Count)
            {
                FloorCleared(); // a floor with no (more) waves breaks its seal right away
                return;
            }
            spawner.Begin(floor.waves[WaveInFloor], Enemies);
            if (floor.waves.Count > 1 && WaveInFloor > 0)
                Banner($"WAVE {WaveInFloor + 1} / {floor.waves.Count}", DangerRed);
        }

        void WaveCleared()
        {
            var floor = CurrentFloor;
            var wave = floor.waves[WaveInFloor];
            SP += wave.clearBonus;
            WaveInFloor++;
            if (WaveInFloor < floor.waves.Count)
            {
                // More waves on this floor: short pause, then the next one.
                WaveGapTimer = Mathf.Max(0.01f, wavePause);
                Banner($"WAVE CLEARED  -  {floor.waves.Count - WaveInFloor} MORE", SealGold);
                return;
            }
            FloorCleared();
        }

        /// <summary>Every wave on the floor is cleared: its seal breaks and the stairs up open.</summary>
        void FloorCleared()
        {
            if (spawner != null) spawner.Stop();
            FloorIndex++;
            WaveInFloor = 0;
            WaveGapTimer = 0f;
            if (FloorIndex >= Floors.Count)
            {
                SetState(GameState.Victory);
                Banner("THE LAST SEAL BREAKS", SealGold);
                PlaySfx(victorySound);
                return;
            }
            ApplyFloorLayout();
            ScanIsBreather = true;
            SetState(GameState.Scan); // the breather on the stairs, with Fuuka's preview of the next floor
            Banner("SEAL BROKEN", SealGold);
        }

        /// <summary>Scan screen -> next step: Planning on arrival at floor 1, the party screen at a breather.</summary>
        public void ContinueFromScan()
        {
            if (State != GameState.Scan) return;
            if (ScanIsBreather) SetState(GameState.PartySelect);
            else EnterFloor();
        }

        /// <summary>Starts preparing the floor, unless the field is over its (lower) limit: then the dismiss screen comes first.</summary>
        void EnterFloor()
        {
            if (TowerCount > FieldLimit)
            {
                dismissMarks.Clear();
                SetState(GameState.Dismiss);
                Banner("THE SEAL TIGHTENS", DangerRed);
                return;
            }
            SetState(GameState.Planning);
            floorStartSP = SP;
            floorStartKills = Kills;
            floorStartAllOuts = AllOutCount;
            Banner($"{FloorTitle(FloorIndex).ToUpperInvariant()}  -  THE STAIRS SEAL BEHIND YOU", Color.white);
        }

        void ApplyFloorLayout()
        {
            var floor = CurrentFloor;
            if (applyLayout != null) applyLayout(floor != null ? floor.pathPoints : null);
        }

        public void EnemyReachedGoal(Enemy e)
        {
            if (State != GameState.Battle) return;
            if (spawner != null) spawner.Stop();
            SetState(GameState.GameOver);
            Banner("S.E.E.S. IS OVERRUN", DangerRed);
            PlaySfx(gameOverSound);
            if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.8f);
        }

        public void OnEnemyKilled(Enemy e)
        {
            Kills++;
            SP += e.Def.spReward;
            PlaySfx(killSound, 0.5f);
        }

        /// <summary>Removes every Shadow, tower and projectile right away (deactivated now, destroyed at frame end).</summary>
        void ClearBoard()
        {
            if (spawner != null) spawner.Stop();
            foreach (var e in new List<Enemy>(Enemy.All)) if (e != null) { e.gameObject.SetActive(false); Destroy(e.gameObject); }
            foreach (var t in new List<Tower>(Tower.All)) if (t != null) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
            foreach (var p in new List<Projectile>(Projectile.All)) if (p != null) { p.gameObject.SetActive(false); Destroy(p.gameObject); }
            if (TowerPlacer.Instance != null) TowerPlacer.Instance.Cancel();
            dismissMarks.Clear();
        }

        /// <summary>
        /// Lost: "Retry floor". Same party, same floor, rewound to the start of its deploy phase (SP and
        /// stats as they were then). The field starts empty, so you redeploy.
        /// </summary>
        public void RetryFloor()
        {
            if (State != GameState.GameOver || CurrentFloor == null) return;
            ClearBoard();
            SP = floorStartSP;
            Kills = floorStartKills;
            AllOutCount = floorStartAllOuts;
            allOutTimer = 0f;
            WaveInFloor = 0;
            WaveGapTimer = 0f;
            IsPaused = false;
            GameSpeed = 1;
            ScanIsBreather = false;
            SetState(GameState.Scan); // arrival again: the stairs seal, Fuuka scans the floor
            Banner($"RETRY  -  {FloorTitle(FloorIndex).ToUpperInvariant()}", Color.white);
        }

        /// <summary>"Retreat": wipes the board and starts the climb again from floor 1 at the party screen (no scene reload).</summary>
        public void ResetGame()
        {
            ClearBoard();

            for (int i = 0; i < ranks.Length; i++) ranks[i] = 0;
            SP = startingSP;
            FloorIndex = 0;
            WaveInFloor = 0;
            WaveGapTimer = 0f;
            Kills = 0;
            AllOutCount = 0;
            allOutTimer = 0f;
            IsPaused = false;
            GameSpeed = 1;
            ApplyFloorLayout();
            // Like entering Tartarus: pick who goes in. Starting over means choosing the party again.
            party.Clear();
            dismissMarks.Clear();
            Towers = new List<TowerDef>();
            MemberTowerCount = 0;
            SetState(GameState.PartySelect);
        }

        // ------------------------------------------------------------ Fuuka's scan (from data)

        public struct ScanEntry
        {
            public EnemyDef def;
            public int count;
        }

        /// <summary>Every Shadow type on a floor, in order of first appearance, with how many come.</summary>
        public List<ScanEntry> ScanFloor(int floor)
        {
            var list = new List<ScanEntry>();
            if (floor < 0 || floor >= Floors.Count) return list;
            foreach (var wave in Floors[floor].waves)
                foreach (var g in wave.groups)
                {
                    if (g.enemyIndex < 0 || g.enemyIndex >= Enemies.Count || g.count <= 0) continue;
                    var def = Enemies[g.enemyIndex];
                    int at = list.FindIndex(x => x.def == def);
                    if (at < 0) list.Add(new ScanEntry { def = def, count = g.count });
                    else list[at] = new ScanEntry { def = def, count = list[at].count + g.count };
                }
            return list;
        }

        static int CountShadows(WaveDef wave)
        {
            int n = 0;
            foreach (var g in wave.groups) n += Mathf.Max(0, g.count);
            return n;
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
        /// <summary>True before the climb starts; false at a stairs breather.</summary>
        public bool IsFirstPartySelect { get { return FloorIndex == 0; } }

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

            if (IsFirstPartySelect)
            {
                // Before the climb: arrive at floor 1. The stairs seal, and Fuuka scans the floor.
                ScanIsBreather = false;
                SetState(GameState.Scan);
                return;
            }
            EnterFloor();
        }

        // ------------------------------------------------------------ Field limit (STORY.md §2)

        public int LimitForFloor(int floor)
        {
            if (fieldLimits == null || fieldLimits.Count == 0) return 6;
            return Mathf.Max(1, fieldLimits[Mathf.Clamp(floor, 0, fieldLimits.Count - 1)]);
        }

        /// <summary>The limit on the floor before this one (equal to FieldLimit on the first floor).</summary>
        public int PreviousFieldLimit { get { return LimitForFloor(FloorIndex - 1); } }

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

        // ------------------------------------------------------------ Social Links (no longer ranked; #14 removes them)

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
            Banner("ALL-OUT ATTACK!", DangerRed);
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
