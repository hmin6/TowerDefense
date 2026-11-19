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
                case GameState.FreeTime: DrawFreeTime(gm); break;
                case GameState.Victory: DrawEnd(gm, true); break;
                case GameState.GameOver: DrawEnd(gm, false); break;
            }

            if (gm.IsPaused)
            {
                Fill(new Rect(0, H * 0.5f - 50, W, 100), new Color(0, 0, 0, 0.7f));
                Text(new Rect(0, H * 0.5f - 50, W, 100), "PAUSED   -   Space: resume    R: restart", sBanner, White);
            }
        }

        void BuildStyles()
        {
            stylesBuilt = true;
            sBody = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, richText = false };
            sBody.normal.textColor = White;
            sSmall = new GUIStyle(sBody) { fontSize = 16 };
            sTiny = new GUIStyle(sBody) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            sHeader = new GUIStyle(sBody) { fontSize = 28, fontStyle = FontStyle.Bold, wordWrap = false };
            sTitle = new GUIStyle(sHeader) { fontSize = 84, fontStyle = FontStyle.BoldAndItalic };
            sTitleCenter = new GUIStyle(sTitle) { alignment = TextAnchor.MiddleCenter };
            sCenter = new GUIStyle(sBody) { alignment = TextAnchor.UpperCenter };
            sBanner = new GUIStyle(sHeader) { fontSize = 60, fontStyle = FontStyle.BoldAndItalic, alignment = TextAnchor.MiddleCenter };
            sFloat = new GUIStyle(sHeader) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            sButton = new GUIStyle(sHeader) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
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

        void Text(Rect r, string s, GUIStyle st, Color c, bool shadow = true)
        {
            Color old = st.normal.textColor;
            if (shadow)
            {
                st.normal.textColor = new Color(0f, 0f, 0f, 0.85f * c.a);
                GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s, st);
            }
            st.normal.textColor = c;
            GUI.Label(r, s, st);
            st.normal.textColor = old;
        }

        /// <summary>Runs draw() rotated around a pivot (virtual coords) - the slanted "persona" look.</summary>
        void Slanted(float angle, Vector2 pivot, System.Action draw)
        {
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pivot * scale);
            draw();
            GUI.matrix = m;
        }

        bool Button(Rect r, string label, Color bg, Color fg)
        {
            Block(r);
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(new Rect(r.x + 6, r.y + 6, r.width, r.height), new Color(0, 0, 0, 0.6f));
            Fill(r, hover ? Color.Lerp(bg, White, 0.2f) : bg);
            Text(r, label, sButton, fg, false);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        bool Card(Rect r, string title, string body, Color accent, bool highlighted)
        {
            Block(r);
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(r, highlighted ? new Color(0.55f, 0.03f, 0.08f, 0.96f) : hover ? new Color(0.14f, 0.14f, 0.16f, 0.95f) : Panel);
            Fill(new Rect(r.x, r.y, 10, r.height), accent);
            Text(new Rect(r.x + 24, r.y + 12, r.width - 34, 36), title, sHeader, White);
            Text(new Rect(r.x + 24, r.y + 52, r.width - 34, r.height - 60), body, sSmall, new Color(0.88f, 0.88f, 0.9f));
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
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
                if (!e.IsAlive || e.Def.isBoss) continue;
                if (!ToGui(cam, e.AimPoint + Vector3.up * (e.HeightOffset + 0.45f), out Vector2 g)) continue;
                const float w = 58f, h = 8f;
                Rect bg = new Rect(g.x - w * 0.5f, g.y, w, h);
                Fill(bg, new Color(0, 0, 0, 0.85f));
                float pct = Mathf.Clamp01(e.HP / e.Def.maxHP);
                Fill(new Rect(bg.x + 1, bg.y + 1, (w - 2) * pct, h - 2), e.IsKnockedDown ? Gold : Red);
                // Weakness pip on the left, slow pip on the right.
                Fill(new Rect(bg.x - 11, bg.y, 8, h), ElementInfo.GetColor(e.Def.weakness));
                if (e.IsSlowed) Fill(new Rect(bg.xMax + 3, bg.y, 8, h), ElementInfo.GetColor(Element.Bufu));
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
                if (e.IsAlive && e.Def.isBoss) { boss = e; break; }
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

                string day = gm.WaveIndex < gm.Waves.Count ? gm.Waves[gm.WaveIndex].title : "Dawn";
                Text(new Rect(36, 26, 500, 40), day.ToUpperInvariant(), sHeader, White);

                string phase;
                switch (gm.State)
                {
                    case GameState.Night: phase = "THE DARK HOUR"; break;
                    case GameState.Planning: phase = "PREPARATION - place towers, then begin the night"; break;
                    case GameState.FreeTime: phase = "FREE TIME"; break;
                    case GameState.Victory: phase = "VICTORY"; break;
                    case GameState.GameOver: phase = "GAME OVER"; break;
                    default: phase = ""; break;
                }
                Text(new Rect(36, 64, 500, 28), phase, sSmall, Red);
                Text(new Rect(36, 92, 500, 30),
                     $"SP {gm.SP}     TOWERS {gm.TowerCount}/{gm.TowerLimit}     SHADOWS {gm.ShadowsRemaining}", sBody, White);
                Text(new Rect(36, 122, 500, 26),
                     $"Night {Mathf.Min(gm.WaveIndex + 1, gm.Waves.Count)} of {gm.Waves.Count}     Defeated {gm.Kills}", sSmall, Gold);
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
                $"Magician (Agi)    Rank {gm.Rank(SocialLink.Magician)}\n" +
                $"Empress (Bufu)    Rank {gm.Rank(SocialLink.Empress)}\n" +
                $"Chariot (Zio)     Rank {gm.Rank(SocialLink.Chariot)}\n" +
                $"Fool (tower slots) Rank {gm.Rank(SocialLink.Fool)}";
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
                "1.  Press 1, 2 or 3 (or click a card at the bottom) to pick a tower.\n" +
                "2.  Move the mouse over the ground. GREEN ghost = valid spot, RED = blocked.\n" +
                "3.  LEFT-CLICK to place it. The ring shows its range.\n" +
                "4.  RIGHT-CLICK or Esc to stop placing.\n\n" +
                $"Towers cost SP and you may only have {gm.TowerLimit} at a time. " +
                "Hover a tower and press X to dismiss it (50% refund).";
            Text(new Rect(r.x + 18, r.y + 60, w - 36, 240), body, sSmall, White);
            Text(new Rect(r.x + 18, r.yMax - 36, w - 36, 28), "Shadow eye colour = its weakness.     [H] hide", sSmall, Gold);
        }

        void DrawTowerCards(GameManager gm)
        {
            int n = gm.Towers.Count;
            const float cw = 340f, ch = 190f, gap = 16f;
            float total = n * cw + (n - 1) * gap;
            float x0 = (W - total) * 0.5f;
            float y = H - ch - 20f;
            var placer = TowerPlacer.Instance;

            for (int i = 0; i < n; i++)
            {
                var d = gm.Towers[i];
                Rect r = new Rect(x0 + i * (cw + gap), y, cw, ch);
                bool selected = placer != null && placer.Selected == i;
                bool afford = gm.SP >= d.cost && gm.TowerCount < gm.TowerLimit;
                string title = $"[{i + 1}] {d.displayName}";
                string body =
                    $"{ElementInfo.Label(d.element)}   |   {d.cost} SP\n" +
                    $"Rate {gm.EffectiveFireRate(d):0.0}/s   Proj {d.projectileSpeed:0} m/s{(d.ballistic ? " (arc)" : "")}\n" +
                    $"Height {d.height:0.0} m   Range {d.range:0.#} m   Dmg {gm.EffectiveDamage(d):0}\n" +
                    d.description;
                if (Card(r, title, body, d.color, selected) && placer != null) placer.Select(i);
                if (!afford) Fill(r, new Color(0, 0, 0, 0.45f));
            }
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
                     $"{t.Def.displayName}  -  X: dismiss (+{gm.RefundFor(t)} SP)", sBody, White);
            }
        }

        void DrawStartButton(GameManager gm)
        {
            Rect r = new Rect((W - 500f) * 0.5f, H - 190f - 20f - 84f, 500f, 64f);
            if (Button(r, ">>  BEGIN THE DARK HOUR   [Enter]", Red, White)) gm.StartNight();
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
                "When the clock strikes midnight, Seiran Academy twists into a labyrinth and the Shadows " +
                "march toward the Clock Tower. If even ONE of them reaches it, the Midnight Hour never ends.\n\n" +
                "Summon towers beside the path to stop them. Every Shadow has a weakness (its glowing eyes): " +
                "hit it to knock the Shadow DOWN and earn your tower ONE MORE shot. Knock them all down at once " +
                "for an ALL-OUT ATTACK. Between nights, spend your free time ranking up Social Links.\n\n" +
                "Survive six nights, including the Full Moon, to see the dawn.";
            Text(new Rect((W - w) * 0.5f, 400f, w, 300f), story, sCenter, White);

            if (Button(new Rect((W - 380f) * 0.5f, 760f, 380f, 72f), "START   [Enter]", Red, White)) gm.BeginGame();
            Text(new Rect(0f, 860f, W, 30f), "1/2/3 pick tower  -  Left-click place  -  Right-click cancel  -  Enter start night  -  Space pause  -  F speed",
                 sCenter, Grey);
        }

        void DrawFreeTime(GameManager gm)
        {
            Dim();
            Slanted(-3f, new Vector2(W * 0.5f, 170f), () =>
            {
                Fill(new Rect(-100f, 110f, W + 200f, 120f), Red);
                Text(new Rect(0f, 118f, W, 70f), "FREE TIME", sBanner, White);
                Text(new Rect(0f, 186f, W, 34f), "After school... who will you spend the day with?", sCenter, White);
            });

            var gold = new Color(1f, 0.85f, 0.2f);
            string[] titles =
            {
                $"[1] MAGICIAN   Rank {gm.Rank(SocialLink.Magician)} > {gm.Rank(SocialLink.Magician) + 1}",
                $"[2] EMPRESS    Rank {gm.Rank(SocialLink.Empress)} > {gm.Rank(SocialLink.Empress) + 1}",
                $"[3] CHARIOT    Rank {gm.Rank(SocialLink.Chariot)} > {gm.Rank(SocialLink.Chariot) + 1}",
                $"[4] FOOL       Rank {gm.Rank(SocialLink.Fool)} > {gm.Rank(SocialLink.Fool) + 1}",
            };
            int dmgPct = Mathf.RoundToInt(gm.rankDamageBonus * 100f);
            int ratePct = Mathf.RoundToInt(gm.rankFireRateBonus * 100f);
            string[] bodies =
            {
                $"Agi towers: +{dmgPct}% damage, +{ratePct}% fire rate.\nThe kid who never shuts up about fire.",
                $"Bufu towers: +{dmgPct}% damage, +{ratePct}% fire rate.\nThe student council president. Ice cold.",
                $"Zio towers: +{dmgPct}% damage, +{ratePct}% fire rate.\nYour rival from the track team.",
                "+1 tower slot.\nA quiet night alone, thinking it all over.",
            };
            Color[] accents = { ElementInfo.GetColor(Element.Agi), ElementInfo.GetColor(Element.Bufu), ElementInfo.GetColor(Element.Zio), White };

            const float cw = 470f, ch = 150f, gap = 24f;
            float x0 = (W - (cw * 2f + gap)) * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                Rect r = new Rect(x0 + (i % 2) * (cw + gap), 300f + (i / 2) * (ch + gap), cw, ch);
                if (Card(r, titles[i], bodies[i], accents[i], false)) gm.ChooseLink((SocialLink)i);
            }
            Text(new Rect(0f, 300f + 2f * (ch + gap) + 20f, W, 30f), "Press 1-4 or click a card.", sCenter, gold);
        }

        void DrawEnd(GameManager gm, bool won)
        {
            Dim();
            Slanted(-4f, new Vector2(W * 0.5f, 330f), () =>
            {
                Fill(new Rect(-100f, 250f, W + 200f, 160f), won ? Gold : Red);
                Text(new Rect(0f, 255f, W, 150f), won ? "DAWN BREAKS" : "THE MIDNIGHT HOUR NEVER ENDS",
                     won ? sTitleCenter : sBanner, won ? new Color(0.05f, 0.05f, 0.05f) : White, !won);
            });
            string stats = won
                ? $"You survived all {gm.Waves.Count} nights.\nShadows defeated: {gm.Kills}     All-Out Attacks: {gm.AllOutCount}"
                : $"A Shadow reached the Clock Tower on {(gm.WaveIndex < gm.Waves.Count ? gm.Waves[gm.WaveIndex].title : "the last night")}.\nShadows defeated: {gm.Kills}     All-Out Attacks: {gm.AllOutCount}";
            Text(new Rect(0f, 470f, W, 80f), stats, sCenter, White);
            if (Button(new Rect((W - 380f) * 0.5f, 600f, 380f, 72f), won ? "PLAY AGAIN   [R]" : "TRY AGAIN   [R]", Red, White))
                gm.ResetGame();
        }
    }
}
