using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    /// <summary>
    /// The whole UI, drawn with IMGUI so it needs no Canvas, prefabs or TextMeshPro setup.
    /// Everything is laid out on a virtual 1080p-tall canvas and scaled to the real screen.
    /// Style: red / black / white slanted panels.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        public bool showHelp = true;

        const float RefHeight = 1080f;
        const float H = RefHeight;
        const float BannerLife = 1.8f;

        static readonly Color Red = new Color(0.86f, 0.06f, 0.12f);
        static readonly Color Panel = new Color(0.03f, 0.03f, 0.04f, 0.92f);
        static readonly Color White = new Color(0.97f, 0.97f, 0.97f);
        static readonly Color Gold = new Color(1f, 0.85f, 0.2f);
        static readonly Color Grey = new Color(0.62f, 0.62f, 0.66f);

        class FloatEntry
        {
            public Vector3 world;
            public string text;
            public Color color;
            public float age;
            public float life;
            public int size;
        }

        readonly List<Rect> blockers = new List<Rect>();
        readonly List<FloatEntry> floats = new List<FloatEntry>();
        int blockerFrame = -1;
        float scale = 1f;
        float W { get { return Screen.width / scale; } }

        string bannerText;
        Color bannerColor = Color.white;
        float bannerAge = 99f;

        Texture2D tex;
        GUIStyle sBody, sSmall, sHeader, sTitle, sTitleCenter, sCenter, sBanner, sFloat, sButton, sTiny;
        GUIStyle sCardTitle, sMini, sTagRight;
        bool stylesBuilt;

        void Awake()
        {
            Instance = this;
            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (tex != null) Destroy(tex);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bannerAge += dt;
            for (int i = floats.Count - 1; i >= 0; i--)
            {
                floats[i].age += dt;
                if (floats[i].age >= floats[i].life) floats.RemoveAt(i);
            }
            if (InputCompat.Down(GameKey.Help)) showHelp = !showHelp;
        }

        // ------------------------------------------------------------ Public API

        public void Float(Vector3 world, string text, Color color, int size = 22, float life = 0.9f)
        {
            if (floats.Count > 90) floats.RemoveAt(0);
            floats.Add(new FloatEntry { world = world, text = text, color = color, size = size, life = life });
        }

        public void Banner(string text, Color color)
        {
            bannerText = text;
            bannerColor = color;
            bannerAge = 0f;
        }

        /// <summary>True if the mouse is over a HUD panel (so clicks there don't place towers).</summary>
        public bool IsPointerOverHUD(Vector2 screenPos)
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.IsModalState) return true;
            Vector2 g = new Vector2(screenPos.x / scale, (Screen.height - screenPos.y) / scale);
            for (int i = 0; i < blockers.Count; i++)
                if (blockers[i].Contains(g)) return true;
            return false;
        }

        // ------------------------------------------------------------ Drawing

        void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (!stylesBuilt) BuildStyles();
            if (Time.frameCount != blockerFrame)
            {
                blockers.Clear();
                blockerFrame = Time.frameCount;
            }

            scale = Mathf.Max(0.1f, Screen.height / RefHeight);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            Camera cam = Camera.main;
            if (cam != null && gm.State != GameState.Briefing)
            {
                DrawEnemyBars(cam);
                DrawFloats(cam);
            }

            if (gm.State != GameState.Briefing) DrawStatus(gm);
            DrawBossBar();

            if (gm.CanPlaceNow)
            {
                DrawTowerCards(gm);
                DrawSidebar(gm);
                if (showHelp) DrawHelp(gm);
                DrawPlacementHint(gm);
            }
            if (gm.State == GameState.Planning) DrawStartButton(gm);

            DrawBanner();

            switch (gm.State)
            {
                case GameState.Briefing: DrawBriefing(gm); break;
                case GameState.PartySelect: DrawPartySelect(gm); break;
                case GameState.Dismiss: DrawDismiss(gm); break;
                case GameState.Scan: DrawScan(gm); break;
                case GameState.Victory: DrawEnd(gm, true); break;
                case GameState.GameOver: DrawEnd(gm, false); break;
            }

            if (gm.IsPaused)
            {
                Fill(new Rect(0, H * 0.5f - 50, W, 100), new Color(0, 0, 0, 0.7f));
                Text(new Rect(0, H * 0.5f - 50, W, 100), "PAUSED   -   Space: resume    R: retreat (restart the climb)", sBanner, White);
            }
        }

        void BuildStyles()
        {
            stylesBuilt = true;
            sBody = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, richText = false };
            StripInteractionStates(sBody); // every other style is copied from sBody
            SetTextColor(sBody, White);
            sSmall = new GUIStyle(sBody) { fontSize = 16 };
            sTiny = new GUIStyle(sBody) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            sHeader = new GUIStyle(sBody) { fontSize = 28, fontStyle = FontStyle.Bold, wordWrap = false };
            sTitle = new GUIStyle(sHeader) { fontSize = 84, fontStyle = FontStyle.BoldAndItalic };
            sTitleCenter = new GUIStyle(sTitle) { alignment = TextAnchor.MiddleCenter };
            sCenter = new GUIStyle(sBody) { alignment = TextAnchor.UpperCenter };
            sBanner = new GUIStyle(sHeader) { fontSize = 60, fontStyle = FontStyle.BoldAndItalic, alignment = TextAnchor.MiddleCenter };
            sFloat = new GUIStyle(sHeader) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            sButton = new GUIStyle(sHeader) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
            sCardTitle = new GUIStyle(sBody) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = false, clipping = TextClipping.Clip };
            sMini = new GUIStyle(sBody) { fontSize = 14, wordWrap = false, clipping = TextClipping.Clip };
            sTagRight = new GUIStyle(sBody) { fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = false, alignment = TextAnchor.UpperRight };
        }

        // ---- primitives

        void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, tex);
            GUI.color = old;
        }

        void Block(Rect r) { blockers.Add(r); }

        /// <summary>
        /// Plain text. The colour is set on EVERY interaction state, because the built-in label style
        /// has its own hover colour: without this, any text under the mouse (and its shadow) would
        /// "light up" as if it were clickable. Only buttons and cards draw a hover look.
        /// </summary>
        void Text(Rect r, string s, GUIStyle st, Color c, bool shadow = true)
        {
            if (shadow)
            {
                SetTextColor(st, new Color(0f, 0f, 0f, 0.85f * c.a));
                GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s, st);
            }
            SetTextColor(st, c);
            GUI.Label(r, s, st);
        }

        static void SetTextColor(GUIStyle st, Color c)
        {
            st.normal.textColor = c;
            st.hover.textColor = c;
            st.active.textColor = c;
            st.focused.textColor = c;
            st.onNormal.textColor = c;
            st.onHover.textColor = c;
            st.onActive.textColor = c;
            st.onFocused.textColor = c;
        }

        /// <summary>Labels never draw a hover/active background either.</summary>
        static void StripInteractionStates(GUIStyle st)
        {
            st.hover.background = null;
            st.active.background = null;
            st.focused.background = null;
            st.onHover.background = null;
            st.onActive.background = null;
            st.onFocused.background = null;
        }

        /// <summary>Runs draw() rotated around a pivot (virtual coords) - the slanted "persona" look.</summary>
        void Slanted(float angle, Vector2 pivot, System.Action draw)
        {
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pivot * scale);
            draw();
            GUI.matrix = m;
        }

        // Interaction looks, used by every clickable element:
        //   hover    = slightly lighter fill + thin faint outline (only when the element can be clicked)
        //   selected = bright red fill + thick white border (clearly not a hover)
        //   disabled = dimmed, no hover, never returns a click
        static readonly Color HoverFill = new Color(0.2f, 0.2f, 0.23f, 0.96f);
        static readonly Color SelectedFill = new Color(0.8f, 0.05f, 0.11f, 0.98f);
        static readonly Color HoverOutline = new Color(1f, 1f, 1f, 0.35f);
        static readonly Color DisabledVeil = new Color(0f, 0f, 0f, 0.5f);

        void Outline(Rect r, float t, Color c)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        bool Button(Rect r, string label, Color bg, Color fg, bool enabled = true)
        {
            Block(r);
            bool hover = enabled && r.Contains(Event.current.mousePosition);
            Fill(new Rect(r.x + 6, r.y + 6, r.width, r.height), new Color(0, 0, 0, 0.6f));
            Fill(r, enabled ? (hover ? Color.Lerp(bg, White, 0.2f) : bg) : new Color(0.3f, 0.3f, 0.32f));
            if (hover) Outline(r, 2f, HoverOutline);
            Text(r, label, sButton, enabled ? fg : Grey, false);
            return enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        static readonly Color TagGreen = new Color(0.55f, 1f, 0.65f);

        bool Card(Rect r, string title, string body, Color accent, bool selected, bool interactive = true, string tag = null)
        {
            Block(r);
            bool hover = interactive && r.Contains(Event.current.mousePosition);
            Fill(r, selected ? SelectedFill : hover ? HoverFill : Panel);
            Fill(new Rect(r.x, r.y, 10, r.height), accent);
            if (selected) Outline(r, 4f, White);
            else if (hover) Outline(r, 2f, HoverOutline);
            Text(new Rect(r.x + 24, r.y + 12, r.width - 34, 36), title, sHeader, White);
            Text(new Rect(r.x + 24, r.y + 52, r.width - 34, r.height - 60), body, sSmall,
                 selected ? White : new Color(0.88f, 0.88f, 0.9f));
            if (!interactive) Fill(r, DisabledVeil);
            if (tag != null) Text(new Rect(r.x + 24, r.yMax - 30, r.width - 34, 24), tag, sCardTitle, TagGreen);
            return interactive && GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        // ---- world-space overlays

        bool ToGui(Camera cam, Vector3 world, out Vector2 g)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            g = new Vector2(sp.x / scale, (Screen.height - sp.y) / scale);
            return sp.z > 0f;
        }

        void DrawEnemyBars(Camera cam)
        {
            foreach (var e in Enemy.All)
            {
                if (!e.IsAlive || e.Def.IsBoss) continue;
                if (!ToGui(cam, e.AimPoint + Vector3.up * (e.HeightOffset + 0.45f), out Vector2 g)) continue;
                const float w = 58f, h = 8f;
                Rect bg = new Rect(g.x - w * 0.5f, g.y, w, h);
                Fill(bg, new Color(0, 0, 0, 0.85f));
                float pct = Mathf.Clamp01(e.HP / e.Def.maxHP);
                Fill(new Rect(bg.x + 1, bg.y + 1, (w - 2) * pct, h - 2), e.IsKnockedDown ? Gold : Red);
                // One pip per weakness on the left (grey = no weakness), slow pip on the right.
                int pips = 0;
                if (e.Def.weaknesses != null)
                    foreach (var el in e.Def.weaknesses)
                    {
                        if (!ElementInfo.HasAffinity(el)) continue;
                        Fill(new Rect(bg.x - 11 - pips * 10, bg.y, 8, h), ElementInfo.GetColor(el));
                        pips++;
                    }
                if (pips == 0) Fill(new Rect(bg.x - 11, bg.y, 8, h), ElementInfo.GetColor(Element.None));
                if (e.IsSlowed) Fill(new Rect(bg.xMax + 3, bg.y, 8, h), ElementInfo.GetColor(Element.Ice));
            }
        }

        void DrawFloats(Camera cam)
        {
            foreach (var f in floats)
            {
                if (!ToGui(cam, f.world, out Vector2 g)) continue;
                float t = f.age / f.life;
                g.y -= t * 55f;
                int oldSize = sFloat.fontSize;
                sFloat.fontSize = f.size;
                Text(new Rect(g.x - 160, g.y - 22, 320, 44), f.text, sFloat, new Color(f.color.r, f.color.g, f.color.b, 1f - t * t));
                sFloat.fontSize = oldSize;
            }
        }

        void DrawBossBar()
        {
            Enemy boss = null;
            foreach (var e in Enemy.All)
                if (e.IsAlive && e.Def.IsBoss) { boss = e; break; }
            if (boss == null) return;

            float w = Mathf.Clamp(W - 1100f, 360f, 760f);
            Rect bar = new Rect((W - w) * 0.5f, 64, w, 24);
            Text(new Rect(bar.x, 22, w, 40), boss.Def.displayName.ToUpperInvariant(), sHeader, White);
            Fill(new Rect(bar.x - 3, bar.y - 3, bar.width + 6, bar.height + 6), Color.black);
            Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(boss.HP / boss.Def.maxHP), bar.height), Red);
            Text(new Rect(bar.x, bar.y - 2, w, 28), $"{Mathf.CeilToInt(boss.HP)} / {boss.Def.maxHP:0}", sTiny, White);
        }

        // ---- panels

        void DrawStatus(GameManager gm)
        {
            Block(new Rect(0, 0, 540, 185));
            Slanted(-3f, new Vector2(260f, 95f), () =>
            {
                Rect panel = new Rect(-30, 18, 560, 152);
                Fill(panel, Panel);
                Fill(new Rect(panel.x, panel.yMax - 8, panel.width, 8), Red);

                string floorTitle = gm.CurrentFloor != null ? gm.FloorTitle(gm.FloorIndex) : "Tartarus";
                Text(new Rect(36, 26, 500, 40), floorTitle.ToUpperInvariant(), sHeader, White);

                string phase;
                switch (gm.State)
                {
                    case GameState.Battle:
                        phase = gm.WaveGapTimer > 0f
                            ? $"BATTLE - next wave in {Mathf.CeilToInt(gm.WaveGapTimer)}s"
                            : $"BATTLE - wave {gm.WaveInFloor + 1} of {gm.WaveCount}";
                        break;
                    case GameState.Planning: phase = "DEPLOY - place towers, then face the Shadows"; break;
                    case GameState.Scan: phase = gm.ScanIsBreather ? "THE STAIRS - a short breather" : "ARRIVAL - Fuuka scans the floor"; break;
                    case GameState.PartySelect: phase = "PARTY - choose who climbs with Makoto"; break;
                    case GameState.Dismiss: phase = "THE SEAL TIGHTENS - dismiss towers"; break;
                    case GameState.Victory: phase = "ALL SEALS BROKEN"; break;
                    case GameState.GameOver: phase = "OVERRUN"; break;
                    default: phase = ""; break;
                }
                Text(new Rect(36, 64, 500, 28), phase, sSmall, Red);
                Text(new Rect(36, 92, 500, 30),
                     $"SP {gm.SP}     FIELD {gm.TowerCount}/{gm.FieldLimit}     SHADOWS {gm.ShadowsRemaining}", sBody, White);
                Text(new Rect(36, 122, 500, 26),
                     $"Floor {gm.FloorNumber} of {gm.Floors.Count}     Defeated {gm.Kills}", sSmall, Gold);
            });
        }

        void DrawSidebar(GameManager gm)
        {
            Rect r = new Rect(20, 200, 400, 300);
            Block(r);
            Fill(r, Panel);
            Fill(new Rect(r.x, r.y, r.width, 5), Red);
            Text(new Rect(r.x + 16, r.y + 12, r.width - 32, 32), "SOCIAL LINKS", sHeader, White);
            string links =
                $"Magician (Fire)   Rank {gm.Rank(SocialLink.Magician)}\n" +
                $"Empress (Ice)     Rank {gm.Rank(SocialLink.Empress)}\n" +
                $"Chariot (Elec)    Rank {gm.Rank(SocialLink.Chariot)}";
            Text(new Rect(r.x + 16, r.y + 50, r.width - 32, 90), links, sSmall, Gold);

            string toggles =
                $"[P] Aim: {(gm.pathAwareAim ? "PATH-AWARE (predicts corners)" : "LINEAR LEAD (velocity)")}\n" +
                $"[L] Aim lines: {(gm.showAimLines ? "ON" : "OFF")}\n" +
                $"[F] Speed x{gm.GameSpeed}    [Space] Pause\n" +
                "[WASD] Pan    [Scroll] Zoom    [H] Help";
            Text(new Rect(r.x + 16, r.y + 150, r.width - 32, 100), toggles, sSmall, White);
            Text(new Rect(r.x + 16, r.y + 248, r.width - 32, 50),
                 "Hit a WEAKNESS to knock a Shadow DOWN.\nAll Shadows down = ALL-OUT ATTACK!", sSmall, new Color(1f, 0.55f, 0.55f));
        }

        void DrawHelp(GameManager gm)
        {
            const float w = 480f;
            Rect r = new Rect(W - w - 20, 20, w, 350);
            Block(r);
            Fill(r, Panel);
            Fill(new Rect(r.x, r.y, r.width, 6), Red);
            Text(new Rect(r.x + 18, r.y + 16, w - 36, 36), "HOW TO PLACE TOWERS", sHeader, White);
            string body =
                "1.  Pick a Persona: 1, 2 or 3 for your party members, 4 to cycle Makoto's Personas, " +
                "or click a card at the bottom (hover a card for its stats).\n" +
                "2.  Move the mouse over the ground. GREEN ghost = valid spot, RED = blocked.\n" +
                "3.  LEFT-CLICK to place it. The ring shows its range.\n" +
                "4.  RIGHT-CLICK or Esc to stop placing.\n\n" +
                $"Placing is free, but the FIELD LIMIT is {gm.FieldLimit} towers, and it drops as you climb. " +
                "Each Persona can be on the field once. Hover a tower and press X to dismiss it.";
            Text(new Rect(r.x + 18, r.y + 60, w - 36, 240), body, sSmall, White);
            Text(new Rect(r.x + 18, r.yMax - 36, w - 36, 28), "Shadow eye colour = its weakness.     [H] hide", sSmall, Gold);
        }

        // Tower bar layout (virtual 1080p): Makoto's Personas at the bottom (one or two rows),
        // the party row above them, and the start button above both.
        const float BarBottom = 16f, MiniH = 80f, MiniGap = 8f, MiniMaxW = 180f, MiniPad = 22f;
        const float PartyCardW = 330f, PartyCardH = 104f, PartyGap = 14f, RowLabelH = 24f;

        struct BarLayout
        {
            public int members, makotoCount, perRow, rows;
            public float miniW, makotoTop, partyY, startY;
        }

        /// <summary>
        /// Makoto's cards are never narrower than their longest text (name, element, role), so nothing
        /// is cut off. If they don't fit in one row at that width (e.g. 4:3), they split into balanced rows.
        /// </summary>
        BarLayout LayoutBar(GameManager gm)
        {
            var L = new BarLayout { members = gm.MemberTowerCount };
            L.makotoCount = Mathf.Max(0, gm.Towers.Count - L.members);
            L.makotoTop = H - BarBottom;
            if (L.makotoCount > 0)
            {
                float minW = 0f;
                for (int i = L.members; i < gm.Towers.Count; i++)
                {
                    var d = gm.Towers[i];
                    minW = Mathf.Max(minW, sCardTitle.CalcSize(new GUIContent(d.displayName)).x);
                    minW = Mathf.Max(minW, sMini.CalcSize(new GUIContent(ElementInfo.Label(d.element))).x);
                    minW = Mathf.Max(minW, sMini.CalcSize(new GUIContent(DefInfo.RoleLabel(d.role))).x);
                }
                minW += MiniPad;
                float avail = W - 40f;
                int fit = Mathf.Clamp(Mathf.FloorToInt((avail + MiniGap) / (minW + MiniGap)), 1, L.makotoCount);
                L.rows = Mathf.CeilToInt(L.makotoCount / (float)fit);
                L.perRow = Mathf.CeilToInt(L.makotoCount / (float)L.rows);
                L.miniW = Mathf.Clamp((avail - (L.perRow - 1) * MiniGap) / L.perRow, minW, Mathf.Max(minW, MiniMaxW));
                L.makotoTop = H - BarBottom - L.rows * MiniH - (L.rows - 1) * MiniGap;
            }
            float above = L.makotoCount > 0 ? L.makotoTop - RowLabelH - 6f : L.makotoTop;
            L.partyY = L.members > 0 ? above - PartyCardH : above;
            L.startY = (L.members > 0 ? L.partyY - RowLabelH : L.partyY) - 10f - 60f;
            return L;
        }

        void DrawTowerCards(GameManager gm)
        {
            var placer = TowerPlacer.Instance;
            var L = LayoutBar(gm);
            // A card is disabled (dimmed, no hover, no click) when the field is full or its Persona is already placed.
            bool full = gm.FieldFull;
            TowerDef hovered = null;
            Rect hoveredRect = default;
            Vector2 mouse = Event.current.mousePosition;

            // Party row: the members' Personas, keys 1-3.
            float partyRowW = L.members * PartyCardW + (L.members - 1) * PartyGap;
            float px0 = (W - partyRowW) * 0.5f;
            if (L.members > 0)
            {
                Rect label = new Rect(px0, L.partyY - RowLabelH, partyRowW, RowLabelH);
                RowLabel(label, "PARTY   [1] [2] [3]", Gold);
                if (full)
                    Text(label, $"Field limit reached ({gm.TowerCount}/{gm.FieldLimit}): press X on a tower to dismiss it", sTagRight,
                         new Color(1f, 0.45f, 0.45f));
            }
            for (int i = 0; i < L.members; i++)
            {
                var d = gm.Towers[i];
                Rect r = new Rect(px0 + i * (PartyCardW + PartyGap), L.partyY, PartyCardW, PartyCardH);
                bool selected = placer != null && placer.Selected == i;
                bool placed = gm.IsOnField(d);
                string body = $"{gm.MemberName(d.user)}  ·  {ElementInfo.Label(d.element)}  ·  {DefInfo.RoleLabel(d.role)}";
                if (!placed)
                    body += $"\nRate {gm.EffectiveFireRate(d):0.0#}/s  ·  Proj {d.projectileSpeed:0} m/s{(d.ballistic ? " arc" : "")}  ·  H {d.height:0.0} m";
                if (Card(r, $"[{i + 1}] {d.displayName}", body, d.color, selected, TowerPlacer.CanPick(gm, i),
                         placed ? "ON FIELD" : null) && placer != null)
                    placer.Select(i);
                if (r.Contains(mouse)) { hovered = d; hoveredRect = r; }
            }

            // Makoto's rows: the Wild Card's Personas, key 4 cycles.
            if (L.makotoCount > 0)
            {
                float rowW = L.perRow * L.miniW + (L.perRow - 1) * MiniGap;
                float mx0 = (W - rowW) * 0.5f;
                RowLabel(new Rect(mx0, L.makotoTop - RowLabelH, rowW, RowLabelH), "MAKOTO  ·  WILD CARD   [4] cycle, or click", Gold);
                for (int k = 0; k < L.makotoCount; k++)
                {
                    int i = L.members + k;
                    int row = k / L.perRow, col = k % L.perRow;
                    int inRow = Mathf.Min(L.perRow, L.makotoCount - row * L.perRow);
                    float rx0 = (W - (inRow * L.miniW + (inRow - 1) * MiniGap)) * 0.5f; // centre a shorter last row
                    var d = gm.Towers[i];
                    Rect r = new Rect(rx0 + col * (L.miniW + MiniGap), L.makotoTop + row * (MiniH + MiniGap), L.miniW, MiniH);
                    bool selected = placer != null && placer.Selected == i;
                    bool placed = gm.IsOnField(d);
                    if (MiniCard(r, d.displayName, ElementInfo.Label(d.element), DefInfo.RoleLabel(d.role),
                                 d.color, ElementInfo.GetColor(d.element), selected, TowerPlacer.CanPick(gm, i),
                                 placed ? "ON FIELD" : null) && placer != null)
                        placer.Select(i);
                    if (r.Contains(mouse)) { hovered = d; hoveredRect = r; }
                }
            }

            // The stats tooltip is information, not a highlight, so it shows even when the cards are disabled.
            if (hovered != null) DrawTowerTooltip(gm, hovered, hoveredRect);
        }

        void RowLabel(Rect r, string text, Color c)
        {
            Block(r);
            Text(r, text, sMini, c);
        }

        bool MiniCard(Rect r, string title, string line1, string line2, Color accent, Color line1Color, bool selected, bool interactive,
                      string tag = null)
        {
            Block(r);
            bool hover = interactive && r.Contains(Event.current.mousePosition);
            Fill(r, selected ? SelectedFill : hover ? HoverFill : Panel);
            Fill(new Rect(r.x, r.y, r.width, 6), accent);
            if (selected) Outline(r, 3f, White);
            else if (hover) Outline(r, 2f, HoverOutline);
            Text(new Rect(r.x + 8, r.y + 10, r.width - 12, 24), title, sCardTitle, White);
            Text(new Rect(r.x + 8, r.y + 34, r.width - 12, 20), line1, sMini, selected ? White : line1Color);
            if (tag == null) Text(new Rect(r.x + 8, r.y + 54, r.width - 12, 20), line2, sMini, selected ? White : Grey);
            if (!interactive) Fill(r, DisabledVeil);
            if (tag != null) Text(new Rect(r.x + 8, r.y + 54, r.width - 12, 20), tag, sMini, TagGreen);
            return interactive && GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>Full stats for the hovered card, floating just above it.</summary>
        void DrawTowerTooltip(GameManager gm, TowerDef d, Rect anchor)
        {
            const float w = 460f, h = 156f;
            float x = Mathf.Clamp(anchor.center.x - w * 0.5f, 10f, W - w - 10f);
            Rect r = new Rect(x, anchor.y - h - 8f, w, h);
            Fill(r, new Color(0.02f, 0.02f, 0.03f, 0.97f));
            Fill(new Rect(r.x, r.y, 8, r.height), d.color);
            string who = d.user == PartyMember.Makoto ? "Makoto (Wild Card)" : gm.MemberName(d.user);
            Text(new Rect(r.x + 20, r.y + 8, w - 30, 30), d.displayName, sHeader, White);
            Text(new Rect(r.x + 20, r.y + 40, w - 30, 22),
                 $"{who}  ·  {DefInfo.ArcanaLabel(d.arcana)}  ·  {ElementInfo.Label(d.element)}", sMini, Gold);
            string extra = (d.splashRadius > 0.01f ? $"   Splash {d.splashRadius:0.#} m" : "") +
                           (d.slowDuration > 0f ? $"   Slow {Mathf.RoundToInt((1f - d.slowMultiplier) * 100f)}%" : "");
            string stats =
                $"Rate {gm.EffectiveFireRate(d):0.0#}/s   Proj {d.projectileSpeed:0} m/s{(d.ballistic ? " (arc)" : "")}   Height {d.height:0.0} m\n" +
                $"Range {d.range:0.#} m   Dmg {gm.EffectiveDamage(d):0}{extra}\n" +
                d.description;
            Text(new Rect(r.x + 20, r.y + 64, w - 30, h - 68), stats, sSmall, new Color(0.88f, 0.88f, 0.9f));
        }

        void DrawPlacementHint(GameManager gm)
        {
            var p = TowerPlacer.Instance;
            if (p == null) return;
            Vector2 m = Event.current.mousePosition;
            if (p.Selected >= 0 && p.GhostVisible)
            {
                string s = p.CurrentValid
                    ? $"Left-click: place {gm.Towers[p.Selected].displayName}   (right-click to cancel)"
                    : p.InvalidReason;
                Text(new Rect(m.x + 26, m.y + 14, 640, 30), s, sBody,
                     p.CurrentValid ? new Color(0.55f, 1f, 0.65f) : new Color(1f, 0.45f, 0.45f));
            }
            else if (p.HoveredTower != null)
            {
                var t = p.HoveredTower;
                Text(new Rect(m.x + 26, m.y + 14, 640, 30),
                     $"{t.Def.displayName}  -  X: dismiss (frees a slot; it can be placed again)", sBody, White);
            }
        }

        void DrawStartButton(GameManager gm)
        {
            Rect r = new Rect((W - 500f) * 0.5f, LayoutBar(gm).startY, 500f, 60f);
            if (Button(r, ">>  FACE THE SHADOWS   [Enter]", Red, White)) gm.StartBattle();
        }

        void DrawBanner()
        {
            if (string.IsNullOrEmpty(bannerText) || bannerAge > BannerLife) return;
            float t = bannerAge / BannerLife;
            float a = t < 0.75f ? 1f : 1f - (t - 0.75f) / 0.25f;
            float slide = Mathf.Clamp01(bannerAge / 0.18f);
            float cy = H * 0.36f;
            Slanted(-5f, new Vector2(W * 0.5f, cy), () =>
            {
                float bandW = (W + 300f) * slide;
                Fill(new Rect(-150f, cy - 56f, bandW, 112f), new Color(0f, 0f, 0f, 0.85f * a));
                Fill(new Rect(-150f, cy - 64f, bandW, 8f), new Color(bannerColor.r, bannerColor.g, bannerColor.b, a));
                Fill(new Rect(-150f, cy + 56f, bandW, 8f), new Color(bannerColor.r, bannerColor.g, bannerColor.b, a));
                Text(new Rect(0f, cy - 56f, W, 112f), bannerText, sBanner, new Color(1f, 1f, 1f, a * slide));
            });
        }

        // ---- full-screen states

        void Dim()
        {
            Rect all = new Rect(0, 0, W, H);
            Block(all);
            Fill(all, new Color(0f, 0f, 0f, 0.72f));
        }

        void DrawBriefing(GameManager gm)
        {
            Dim();
            Slanted(-4f, new Vector2(W * 0.5f, 250f), () =>
            {
                Fill(new Rect(-100f, 160f, W + 200f, 170f), Red);
                Fill(new Rect(-100f, 330f, W + 200f, 10f), White);
                Text(new Rect(0f, 168f, W, 110f), "MIDNIGHT HOUR", sTitleCenter, White);
                Text(new Rect(0f, 276f, W, 40f), "a Persona-inspired tower defense", sCenter, new Color(0.05f, 0.05f, 0.05f), false);
            });

            const float w = 980f;
            string story =
                "S.E.E.S. is climbing Tartarus, through the Tziah block. Partway up, the stairs seal shut behind " +
                "and ahead: a powerful Shadow higher up has locked the floors and is sending its Shadows to wear the team down.\n\n" +
                "Deploy Personas around the team's position to hold out. Every Shadow has a weakness (its glowing eyes): " +
                "hit it to knock the Shadow DOWN and earn that tower ONE MORE shot. Knock them all down at once for an " +
                "ALL-OUT ATTACK. Clear every wave on a floor to break its seal and climb on.\n\n" +
                "Break all six seals. If a single Shadow reaches the team, S.E.E.S. is overrun.";
            Text(new Rect((W - w) * 0.5f, 400f, w, 300f), story, sCenter, White);

            if (Button(new Rect((W - 380f) * 0.5f, 760f, 380f, 72f), "START   [Enter]", Red, White)) gm.BeginGame();
            Text(new Rect(0f, 860f, W, 30f), "1-3 party Persona  -  4 Makoto's Personas  -  Left-click place  -  Right-click cancel  -  Enter continue / face the Shadows  -  Space pause  -  F speed",
                 sCenter, Grey);
        }

        void DrawPartySelect(GameManager gm)
        {
            Dim();
            bool first = gm.IsFirstPartySelect;
            int need = gm.PartySizeRequired;
            string next = gm.FloorTitle(gm.FloorIndex);
            Slanted(-3f, new Vector2(W * 0.5f, 150f), () =>
            {
                Fill(new Rect(-100f, 90f, W + 200f, 120f), Red);
                Text(new Rect(0f, 98f, W, 70f), first ? "CHOOSE YOUR PARTY" : "CHANGE THE PARTY?", sBanner, White);
                Text(new Rect(0f, 166f, W, 34f),
                     first ? $"Makoto leads. Pick {need} members to climb with him."
                           : $"A short breather before {next}. Swap members, or keep the team.",
                     sCenter, White);
            });

            // Makoto: fixed leader.
            Rect lead = new Rect((W - 900f) * 0.5f, 236f, 900f, 70f);
            Block(lead);
            Fill(lead, Panel);
            Fill(new Rect(lead.x, lead.y, 10, lead.height), Red);
            Text(new Rect(lead.x + 24, lead.y + 8, lead.width - 34, 28), "MAKOTO   ·   Leader, always in the party", sCardTitle, White);
            Text(new Rect(lead.x + 24, lead.y + 38, lead.width - 34, 24),
                 $"Wild Card: he can field any of his {gm.MakotoPersonas.Count} Personas (bottom row of the tower bar).", sSmall, Gold);

            // The 8 members, 4 x 2.
            const int cols = 4;
            const float ch = 170f, gap = 18f, y0 = 330f;
            float cw = Mathf.Min(410f, (W - 80f - (cols - 1) * gap) / cols);
            float x0 = (W - (cols * cw + (cols - 1) * gap)) * 0.5f;
            int count = gm.PartyCount;
            for (int i = 0; i < gm.Members.Count; i++)
            {
                var m = gm.Members[i];
                var p = m.persona;
                Rect r = new Rect(x0 + (i % cols) * (cw + gap), y0 + (i / cols) * (ch + gap), cw, ch);
                bool inParty = gm.IsInParty(m.id);
                string key = i < 8 ? $"[{i + 1}] " : "";
                string body = $"{p.displayName}  ·  {DefInfo.ArcanaLabel(p.arcana)}  ·  {ElementInfo.Label(p.element)}\n{p.description}";
                // When the party is full, only members already in it can be clicked (to take them out).
                bool clickable = inParty || count < need;
                if (Card(r, key + m.displayName.ToUpperInvariant(), body, p.color, inParty, clickable)) gm.TogglePartyMember(i);
                if (inParty)
                {
                    Rect badge = new Rect(r.xMax - 124f, r.yMax - 34f, 112f, 26f);
                    Fill(badge, White);
                    Text(badge, "IN PARTY", sTiny, Red, false);
                }
            }

            float infoY = y0 + 2f * (ch + gap) + 4f;
            string status = count < need ? $"Party: {count} / {need}   -   pick {need - count} more."
                                         : $"Party: {count} / {need}   -   full. Click a member to take them out.";
            Text(new Rect(0f, infoY, W, 30f), status, sCenter, count == need ? Gold : White);

            // Field limit for the coming floor (it only ever drops).
            var leaving = first ? new List<string>() : gm.TowersLeavingField();
            int limit = gm.FieldLimit, prev = gm.PreviousFieldLimit, after = gm.TowerCount - leaving.Count;
            string limitText;
            if (first) limitText = $"Field limit: {limit} towers on the field at once. It drops as you climb.";
            else
            {
                limitText = limit < prev ? $"The seal tightens: the field limit drops from {prev} to {limit}." : $"The field limit stays at {limit}.";
                if (after > limit) limitText += $"  You have {after} on the field, so you'll dismiss {after - limit} next.";
            }
            Text(new Rect(0f, infoY + 32f, W, 30f), limitText, sCenter, !first && after > limit ? new Color(1f, 0.55f, 0.55f) : Gold);

            if (leaving.Count > 0)
                Text(new Rect(0f, infoY + 60f, W, 30f), "Leaving the field: " + string.Join(", ", leaving),
                     sCenter, new Color(1f, 0.55f, 0.55f));

            bool ok = gm.CanConfirmParty;
            Rect btn = new Rect((W - 440f) * 0.5f, infoY + 96f, 440f, 68f);
            if (Button(btn, first ? "ENTER TARTARUS   [Enter]" : "CONTINUE   [Enter]", Red, White, ok))
                gm.ConfirmParty();
            Text(new Rect(0f, btn.yMax + 18f, W, 30f), "Press 1-8 or click a member to add or remove them.   Enter to confirm.", sCenter, Grey);
        }

        /// <summary>Over the new floor's field limit: pick which towers leave before the floor starts (STORY.md §2).</summary>
        void DrawDismiss(GameManager gm)
        {
            Dim();
            int limit = gm.FieldLimit, count = gm.TowerCount, keep = gm.TowersAfterDismiss;
            Slanted(-3f, new Vector2(W * 0.5f, 150f), () =>
            {
                Fill(new Rect(-100f, 90f, W + 200f, 120f), Red);
                Text(new Rect(0f, 98f, W, 70f), "THE SEAL TIGHTENS", sBanner, White);
                Text(new Rect(0f, 166f, W, 34f),
                     $"Only {limit} Personas can stay on the field here. Choose {Mathf.Max(0, count - limit)} to dismiss.", sCenter, White);
            });

            var towers = gm.FieldTowers;
            const int cols = 4;
            const float ch = 110f, gap = 18f, y0 = 270f;
            float cw = Mathf.Min(380f, (W - 80f - (cols - 1) * gap) / cols);
            int n = towers.Count;
            int rows = Mathf.CeilToInt(n / (float)cols);
            for (int i = 0; i < n; i++)
            {
                var t = towers[i];
                if (t == null || t.Def == null) continue;
                var d = t.Def;
                int row = i / cols, col = i % cols;
                int inRow = Mathf.Min(cols, n - row * cols);
                float rx0 = (W - (inRow * cw + (inRow - 1) * gap)) * 0.5f;
                Rect r = new Rect(rx0 + col * (cw + gap), y0 + row * (ch + gap), cw, ch);
                bool marked = gm.IsMarkedForDismiss(t);
                string who = d.user == PartyMember.Makoto ? "Makoto" : gm.MemberName(d.user);
                string key = i < 8 ? $"[{i + 1}] " : "";
                string body = $"{who}  ·  {ElementInfo.Label(d.element)}\n{DefInfo.RoleLabel(d.role)}";
                if (Card(r, key + d.displayName, body, d.color, marked)) gm.ToggleDismiss(i);
                if (marked)
                {
                    Rect badge = new Rect(r.xMax - 118f, r.yMax - 34f, 106f, 26f);
                    Fill(badge, White);
                    Text(badge, "DISMISS", sTiny, Red, false);
                }
            }

            float infoY = y0 + rows * (ch + gap) + 6f;
            bool ok = gm.CanConfirmDismiss;
            string status = ok ? $"{keep} of {count} will stay (limit {limit}). Ready."
                               : $"{keep} of {count} would stay. Mark {keep - limit} more to get down to {limit}.";
            Text(new Rect(0f, infoY, W, 30f), status, sCenter, ok ? Gold : new Color(1f, 0.55f, 0.55f));

            Rect btn = new Rect((W - 440f) * 0.5f, infoY + 44f, 440f, 68f);
            if (Button(btn, "CONTINUE   [Enter]", Red, White, ok)) gm.ConfirmDismiss();
            Text(new Rect(0f, btn.yMax + 18f, W, 30f), "Press 1-8 or click a tower to mark or unmark it.   Enter to confirm.", sCenter, Grey);
        }

        /// <summary>
        /// Fuuka's scan of a floor, generated from its waves: every Shadow type, how many come, and their
        /// weaknesses and resistances. Shown on arrival at floor 1, and at every stairs breather for the floor ahead.
        /// </summary>
        void DrawScan(GameManager gm)
        {
            Dim();
            bool breather = gm.ScanIsBreather;
            int floor = gm.FloorIndex;
            string floorTitle = gm.FloorTitle(floor);
            Slanted(-3f, new Vector2(W * 0.5f, 150f), () =>
            {
                Fill(new Rect(-100f, 90f, W + 200f, 120f), Red);
                Text(new Rect(0f, 98f, W, 70f), breather ? "THE STAIRS" : floorTitle.ToUpperInvariant(), sBanner, White);
                Text(new Rect(0f, 166f, W, 34f),
                     breather ? $"The seal is broken. A short breather before {floorTitle}, while Fuuka scans ahead."
                              : "The stairs seal behind S.E.E.S. Fuuka scans the floor.",
                     sCenter, White);
            });

            var scan = gm.ScanFloor(floor);
            int waves = floor < gm.Floors.Count ? gm.Floors[floor].waves.Count : 0;
            const float rowH = 58f, headH = 56f;
            float w = Mathf.Min(1120f, W - 80f);
            Rect panel = new Rect((W - w) * 0.5f, 240f, w, headH + Mathf.Max(1, scan.Count) * rowH + 14f);
            Block(panel);
            Fill(panel, Panel);
            Fill(new Rect(panel.x, panel.y, panel.width, 5), Gold);
            Text(new Rect(panel.x + 20, panel.y + 14, w - 40, 30),
                 $"FUUKA'S SCAN   ·   {floorTitle}   ·   {waves} wave{(waves == 1 ? "" : "s")}   ·   field limit {gm.LimitForFloor(floor)}",
                 sCardTitle, Gold);

            float y = panel.y + headH;
            if (scan.Count == 0)
                Text(new Rect(panel.x + 20, y + 16, w - 40, 26), "No Shadows detected.", sSmall, Grey);
            float nameW = w * 0.26f, statW = w * 0.2f, weakW = w * 0.3f;
            foreach (var entry in scan)
            {
                var d = entry.def;
                float x = panel.x + 20;
                Fill(new Rect(panel.x + 12, y + 4, w - 24, rowH - 8), new Color(1f, 1f, 1f, 0.04f));
                Fill(new Rect(x, y + 12, 10, rowH - 24), d.IsBoss ? Red : ElementInfo.GetColor(d.PrimaryWeakness));
                string tags = (d.IsBoss ? "  BOSS" : d.rank == ShadowRank.FloorBoss ? "  FLOOR BOSS" : "") +
                              (d.movement == ShadowMovement.Fast ? "  FAST" : d.movement == ShadowMovement.Flying ? "  FLYING" : "") +
                              (d.blocksAilments ? "  NO AILMENTS" : "");
                Text(new Rect(x + 22, y + 8, nameW - 30, 24), d.displayName, sCardTitle, White);
                Text(new Rect(x + 22, y + 32, nameW - 30, 20), $"x{entry.count}{tags}", sMini, d.IsBoss ? new Color(1f, 0.45f, 0.45f) : Grey);
                Text(new Rect(x + nameW, y + 18, statW, 22), $"HP {d.maxHP:0}   ·   {d.speed:0.#} m/s", sSmall, new Color(0.88f, 0.88f, 0.9f));
                AffinityChips(new Rect(x + nameW + statW, y + 8, weakW - 10, 44), "WEAK", d.weaknesses, Gold);
                AffinityChips(new Rect(x + nameW + statW + weakW, y + 8, w - (nameW + statW + weakW) - 40, 44), "RESISTS", d.resists, Grey);
                y += rowH;
            }

            Rect btn = new Rect((W - 460f) * 0.5f, panel.yMax + 28f, 460f, 68f);
            if (Button(btn, breather ? "TO THE PARTY   [Enter]" : "DEPLOY   [Enter]", Red, White)) gm.ContinueFromScan();
            Text(new Rect(0f, btn.yMax + 16f, W, 30f),
                 breather ? "Next: swap party members if you want, then deploy on the new floor." : "Next: deploy your Personas around the team.",
                 sCenter, Grey);
        }

        /// <summary>"WEAK" / "RESISTS" label with one coloured element name per entry (or "none").</summary>
        void AffinityChips(Rect r, string label, List<Element> elements, Color labelColor)
        {
            Text(new Rect(r.x, r.y, r.width, 18), label, sMini, labelColor);
            float x = r.x;
            int shown = 0;
            if (elements != null)
                foreach (var e in elements)
                {
                    if (!ElementInfo.HasAffinity(e)) continue;
                    string t = ElementInfo.Label(e);
                    float tw = sMini.CalcSize(new GUIContent(t)).x;
                    Text(new Rect(x, r.y + 20, tw + 4, 20), t, sMini, ElementInfo.GetColor(e));
                    x += tw + 14f;
                    shown++;
                }
            if (shown == 0) Text(new Rect(x, r.y + 20, 80, 20), "none", sMini, Grey);
        }

        void DrawEnd(GameManager gm, bool won)
        {
            Dim();
            Slanted(-4f, new Vector2(W * 0.5f, 330f), () =>
            {
                Fill(new Rect(-100f, 250f, W + 200f, 160f), won ? Gold : Red);
                Text(new Rect(0f, 255f, W, 150f), won ? "THE LAST SEAL BREAKS" : "S.E.E.S. IS OVERRUN",
                     won ? sTitleCenter : sBanner, won ? new Color(0.05f, 0.05f, 0.05f) : White, !won);
            });
            string stats = won
                ? $"S.E.E.S. broke all {gm.Floors.Count} seals.\nShadows defeated: {gm.Kills}     All-Out Attacks: {gm.AllOutCount}"
                : $"A Shadow broke through to the team on {gm.FloorTitle(gm.FloorIndex)}.\nShadows defeated: {gm.Kills}     All-Out Attacks: {gm.AllOutCount}";
            Text(new Rect(0f, 470f, W, 80f), stats, sCenter, White);

            if (won)
            {
                if (Button(new Rect((W - 380f) * 0.5f, 600f, 380f, 72f), "PLAY AGAIN   [R]", Red, White)) gm.ResetGame();
                return;
            }

            // Lost: retry this floor with the same party, or retreat and start the climb over.
            const float bw = 400f, gap = 40f;
            float x0 = (W - (bw * 2f + gap)) * 0.5f;
            Rect retry = new Rect(x0, 600f, bw, 72f), retreat = new Rect(x0 + bw + gap, 600f, bw, 72f);
            if (Button(retry, "RETRY FLOOR   [Enter]", Red, White)) gm.RetryFloor();
            if (Button(retreat, "RETREAT   [R]", new Color(0.22f, 0.22f, 0.26f), White)) gm.ResetGame();
            Text(new Rect(retry.x, retry.yMax + 16f, bw, 50f),
                 $"{gm.FloorTitle(gm.FloorIndex)} again with the same party.\nThe field starts empty: redeploy.", sCenter, Grey);
            Text(new Rect(retreat.x, retreat.yMax + 16f, bw, 50f),
                 "Leave Tartarus and climb again from Floor 1.\nYou choose the party again.", sCenter, Grey);
        }
    }
}
