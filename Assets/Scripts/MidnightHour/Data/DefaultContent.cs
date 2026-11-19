using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    /// <summary>
    /// The default game content. GameBootstrap copies this into its Inspector lists
    /// (right-click the component > Reset to restore these values).
    /// </summary>
    public static class DefaultContent
    {
        // Indices into Enemies(), used by the floors below. Order = STORY.md §3, floor by floor.
        public const int Maya = 0, Sigil = 1, Drive = 2, Wheel = 3, Knight = 4, Turret = 5, Tiara = 6, Idol = 7,
                         Twins = 8, Mother = 9, Relic = 10, Magus = 11, Musha = 12, Table = 13, Reaper = 14;

        /// <summary>Field limit per floor (STORY.md §2): the closer to the Jailer, the stricter.</summary>
        public static List<int> FieldLimits()
        {
            return new List<int> { 8, 7, 6, 6, 5, 4 };
        }

        static EnemyDef S(string name, float hp, float speed, Element[] weak, Element[] resist,
                          PrimitiveType shape, Vector3 scale, Color color, int sp,
                          ShadowMovement movement = ShadowMovement.Ground, ShadowRank rank = ShadowRank.Normal)
        {
            var e = new EnemyDef
            {
                displayName = name, maxHP = hp, speed = speed, spReward = sp,
                movement = movement, rank = rank, shape = shape, scale = scale, color = color,
            };
            e.weaknesses.AddRange(weak);
            e.resists.AddRange(resist);
            return e;
        }

        static readonly Element[] None = new Element[0];
        static Element[] E(params Element[] e) { return e; }

        /// <summary>
        /// The Tziah I Shadows (STORY.md §3). Weaknesses and resistances are from the original Persona 3 table.
        /// HP and speed are PLACEHOLDERS to tune in playtesting (#17). STORY.md only fixes these relations:
        /// Drives and Wheels are fast, Tiaras and Idols fly, Champion Knight is tanky and resists physical,
        /// the Arcane Turret has the highest HP of the regular Shadows, and Hakurou Musha is the strongest
        /// Shadow in Tziah I. The Reaper stays as the floor-6 boss until the Jailer (#12).
        /// </summary>
        public static List<EnemyDef> Enemies()
        {
            const ShadowMovement Fast = ShadowMovement.Fast, Fly = ShadowMovement.Flying;
            const ShadowRank FloorBoss = ShadowRank.FloorBoss;
            var table = S("Sleeping Table", 1500f, 0.9f, E(Element.Ice, Element.Light, Element.Dark), None,
                          PrimitiveType.Cube, new Vector3(2.2f, 1.1f, 1.6f), new Color(0.3f, 0.22f, 0.08f), 60, rank: FloorBoss);
            table.blocksAilments = true;
            return new List<EnemyDef>
            {
                // Floor 1: basic Shadows
                S("Ill-fated Maya", 40f, 3.2f, E(Element.Elec, Element.Wind), None,
                  PrimitiveType.Sphere, new Vector3(0.9f, 0.9f, 0.9f), new Color(0.16f, 0.1f, 0.22f), 10),
                S("Blue Sigil", 55f, 2.8f, E(Element.Dark), None,
                  PrimitiveType.Cylinder, new Vector3(0.9f, 0.7f, 0.9f), new Color(0.08f, 0.12f, 0.3f), 12),
                // Floor 2: fast Shadows (vehicles)
                S("Killer Drive", 45f, 5.6f, E(Element.Light), None,
                  PrimitiveType.Cube, new Vector3(1.1f, 0.6f, 1.6f), new Color(0.25f, 0.06f, 0.06f), 12, Fast),
                S("Brave Wheel", 70f, 4.8f, E(Element.Ice), None,
                  PrimitiveType.Cylinder, new Vector3(1.3f, 0.45f, 1.3f), new Color(0.22f, 0.16f, 0.05f), 14, Fast),
                // Floor 3: tanky Shadows, ending with three Arcane Turrets
                S("Champion Knight", 230f, 1.8f, E(Element.Wind), E(Element.Slash, Element.Strike, Element.Pierce),
                  PrimitiveType.Capsule, new Vector3(0.95f, 0.95f, 0.95f), new Color(0.14f, 0.14f, 0.18f), 25),
                S("Arcane Turret", 600f, 1.0f, E(Element.Elec), None,
                  PrimitiveType.Cylinder, new Vector3(1.4f, 1.1f, 1.4f), new Color(0.2f, 0.18f, 0.1f), 40, rank: FloorBoss),
                // Floor 4: flying Shadows
                S("Writhing Tiara", 60f, 3.0f, E(Element.Ice), None,
                  PrimitiveType.Sphere, new Vector3(0.8f, 0.55f, 0.8f), new Color(0.28f, 0.2f, 0.06f), 12, Fly),
                S("Tranquil Idol", 85f, 2.6f, E(Element.Dark), None,
                  PrimitiveType.Capsule, new Vector3(0.6f, 0.6f, 0.6f), new Color(0.12f, 0.2f, 0.18f), 14, Fly),
                // Floor 5: everything at once, ending with the Sleeping Table
                S("Death Twins", 130f, 2.8f, E(Element.Strike), None,
                  PrimitiveType.Capsule, new Vector3(0.8f, 0.8f, 0.8f), new Color(0.2f, 0.04f, 0.1f), 18),
                S("Elegant Mother", 160f, 2.2f, E(Element.Elec, Element.Dark), None,
                  PrimitiveType.Sphere, new Vector3(1.35f, 1.1f, 1.35f), new Color(0.25f, 0.08f, 0.2f), 20),
                S("Constancy Relic", 140f, 2.4f, E(Element.Fire), None,
                  PrimitiveType.Cube, new Vector3(1f, 1f, 1f), new Color(0.18f, 0.16f, 0.12f), 18),
                S("Magical Magus", 120f, 3.0f, E(Element.Fire), None,
                  PrimitiveType.Capsule, new Vector3(0.75f, 0.85f, 0.75f), new Color(0.1f, 0.06f, 0.22f), 18),
                S("Hakurou Musha", 420f, 2.6f, E(Element.Light, Element.Dark), None,
                  PrimitiveType.Capsule, new Vector3(1.1f, 1.2f, 1.1f), new Color(0.3f, 0.3f, 0.32f), 35),
                table,
                // Floor 6: placeholder boss until the Jailer (#12)
                S("The Reaper", 3200f, 1.1f, None, None,
                  PrimitiveType.Capsule, new Vector3(1.3f, 1.5f, 1.3f), new Color(0.02f, 0.02f, 0.02f), 0, rank: ShadowRank.Boss),
            };
        }

        // ------------------------------------------------------------ S.E.E.S. roster (STORY.md §5)
        // Identity (Persona, Arcana, element, role) comes from STORY.md. The stats are PLACEHOLDERS that
        // already differ in fire rate, projectile speed and height; the real numbers come with the
        // role issues (#6-#9) and the balance pass (#17).

        static TowerDef P(string persona, PartyMember user, Arcana arcana, Element element, TowerRole role,
                          PersonaSource source, string description,
                          float fireRate, float projectileSpeed, float damage, float range, float height,
                          float projectileScale, Color color)
        {
            return new TowerDef
            {
                displayName = persona, description = description, element = element,
                user = user, arcana = arcana, role = role, source = source,
                fireRate = fireRate, projectileSpeed = projectileSpeed, damage = damage, range = range, height = height,
                projectileScale = projectileScale, color = color,
            };
        }

        static MemberDef M(PartyMember id, string name, TowerDef persona)
        {
            return new MemberDef { id = id, displayName = name, persona = persona };
        }

        /// <summary>The 8 S.E.E.S. members who can join Makoto. Each places one tower: their own Persona.</summary>
        public static List<MemberDef> Members()
        {
            const PersonaSource Own = PersonaSource.Own;
            var hermes = P("Hermes", PartyMember.Junpei, Arcana.Magician, Element.Fire, TowerRole.Artillery, Own,
                           "Artillery. Slow, arcing fireballs that explode on an area.",
                           0.5f, 9f, 36f, 11f, 4.0f, 0.55f, new Color(1f, 0.42f, 0.1f));
            hermes.ballistic = true;
            hermes.splashRadius = 2.2f;

            var penthesilea = P("Penthesilea", PartyMember.Mitsuru, Arcana.Empress, Element.Ice, TowerRole.Controller, Own,
                                "Controller. Slows the Shadows she hits.",
                                1.1f, 16f, 12f, 8.5f, 2.4f, 0.35f, new Color(0.4f, 0.85f, 1f));
            penthesilea.slowMultiplier = 0.5f;
            penthesilea.slowDuration = 2f;

            return new List<MemberDef>
            {
                M(PartyMember.Yukari, "Yukari", P("Io", PartyMember.Yukari, Arcana.Lovers, Element.Wind, TowerRole.Pushback, Own,
                    "Pushback. Wind arrows knock Shadows backward along the path.",
                    1.6f, 24f, 9f, 8f, 2.0f, 0.25f, new Color(0.4f, 1f, 0.55f))),
                M(PartyMember.Junpei, "Junpei", hermes),
                M(PartyMember.Akihiko, "Akihiko", P("Polydeuces", PartyMember.Akihiko, Arcana.Emperor, Element.Elec, TowerRole.Brawler, Own,
                    "Brawler. Rapid, short-range hits that stack damage on one Shadow.",
                    4.0f, 32f, 5f, 5.5f, 1.0f, 0.2f, new Color(1f, 0.88f, 0.2f))),
                M(PartyMember.Mitsuru, "Mitsuru", penthesilea),
                M(PartyMember.Aigis, "Aigis", P("Palladion", PartyMember.Aigis, Arcana.Chariot, Element.Pierce, TowerRole.Sniper, Own,
                    "Sniper. Longest range; bullets pierce through a line of Shadows.",
                    0.8f, 45f, 22f, 16f, 3.4f, 0.2f, new Color(0.6f, 0.75f, 0.9f))),
                M(PartyMember.Ken, "Ken", P("Nemesis", PartyMember.Ken, Arcana.Justice, Element.Light, TowerRole.Executioner, Own,
                    "Executioner. Chance to instantly kill weakened, non-boss Shadows.",
                    1.0f, 20f, 14f, 9f, 2.8f, 0.3f, new Color(1f, 0.96f, 0.6f))),
                // STORY.md lists Cerberus as "Mudo / Agi": Dark is his element, and Fire is flavour only (decided 2026-11-18).
                M(PartyMember.Koromaru, "Koromaru", P("Cerberus", PartyMember.Koromaru, Arcana.Strength, Element.Dark, TowerRole.Debuffer, Own,
                    "Debuffer. Curses Shadows, so they take more damage from everyone.",
                    1.4f, 18f, 8f, 8f, 1.6f, 0.3f, new Color(0.62f, 0.18f, 0.8f))),
                M(PartyMember.Shinjiro, "Shinjiro", P("Castor", PartyMember.Shinjiro, Arcana.Hierophant, Element.Strike, TowerRole.TankBuster, Own,
                    "Tank buster. Very slow but massive physical hits; bonus damage to high-HP Shadows.",
                    0.35f, 14f, 60f, 7f, 3.0f, 0.5f, new Color(0.8f, 0.56f, 0.36f))),
            };
        }

        /// <summary>
        /// Makoto's 11 Personas (the Wild Card). He can field each one once.
        /// Elements are from STORY.md §5 (decided 2026-11-18). Sarasvati is neutral (Element.None).
        /// </summary>
        public static List<TowerDef> MakotoPersonas()
        {
            const PartyMember Mk = PartyMember.Makoto;
            const PersonaSource Movie = PersonaSource.Movie, Game = PersonaSource.Game;
            return new List<TowerDef>
            {
                P("Orpheus", Mk, Arcana.Fool, Element.Fire, TowerRole.Generalist, Movie,
                  "Generalist. Balanced at everything; a solid tower to place first.",
                  1.2f, 21f, 12f, 8.5f, 2.2f, 0.3f, new Color(0.85f, 0.3f, 0.35f)),
                P("Jack Frost", Mk, Arcana.Magician, Element.Ice, TowerRole.Trapper, Movie,
                  "Trapper. Plants frost mines on the path that burst when Shadows step on them.",
                  0.6f, 12f, 18f, 7.5f, 1.3f, 0.4f, new Color(0.7f, 0.92f, 1f)),
                P("Sarasvati", Mk, Arcana.Priestess, Element.None, TowerRole.SupportAura, Movie,
                  "Support aura. Makes nearby towers fire faster.",
                  0.9f, 17f, 6f, 7f, 2.6f, 0.25f, new Color(0.55f, 0.95f, 0.85f)),
                P("Oberon", Mk, Arcana.Emperor, Element.Elec, TowerRole.ChainLightning, Movie,
                  "Chain lightning. Each hit jumps to several more Shadows.",
                  1.3f, 26f, 9f, 8f, 2.1f, 0.25f, new Color(1f, 0.8f, 0.35f)),
                P("Thoth", Mk, Arcana.Hierophant, Element.Fire, TowerRole.SpBattery, Movie,
                  "SP battery. Generates team SP over time.",
                  0.7f, 13f, 8f, 7.5f, 3.6f, 0.3f, new Color(0.45f, 0.6f, 1f)),
                P("Shiisaa", Mk, Arcana.Hierophant, Element.Strike, TowerRole.LastLine, Movie,
                  "Last line of defense. Short range, but very heavy damage to any Shadow close to the team.",
                  2.2f, 22f, 20f, 4.5f, 1.1f, 0.35f, new Color(0.95f, 0.55f, 0.25f)),
                P("Thor", Mk, Arcana.Chariot, Element.Elec, TowerRole.AntiAir, Movie,
                  "Anti-air. Heavy damage to flying Shadows.",
                  0.75f, 34f, 24f, 10f, 3.8f, 0.4f, new Color(0.75f, 0.8f, 1f)),
                P("Melchizedek", Mk, Arcana.Justice, Element.Light, TowerRole.Pulse, Movie,
                  "Pulse. Damages every Shadow around it every few seconds, with no projectile.",
                  0.4f, 11f, 16f, 6f, 3.2f, 0.45f, new Color(1f, 0.9f, 0.75f)),
                P("Lakshmi", Mk, Arcana.Empress, Element.Ice, TowerRole.ResistanceBreaker, Game,
                  "Resistance breaker. Nearby Shadows lose their resistances.",
                  0.85f, 15f, 9f, 7f, 2.9f, 0.3f, new Color(1f, 0.6f, 0.8f)),
                P("Pixie", Mk, Arcana.Lovers, Element.Elec, TowerRole.Multishot, Game,
                  "Multishot. Fires small shots at up to 3 Shadows at once.",
                  2.6f, 28f, 4f, 7.5f, 1.4f, 0.18f, new Color(1f, 0.75f, 0.95f)),
                P("Valkyrie", Mk, Arcana.Strength, Element.Slash, TowerRole.Interceptor, Game,
                  "Interceptor. Targets the fastest Shadow in range first and hits fast Shadows harder.",
                  1.8f, 38f, 11f, 9f, 1.8f, 0.25f, new Color(0.9f, 0.3f, 0.3f)),
            };
        }

        static SpawnGroup G(int enemy, int count, float interval, float delay = 0f)
        {
            return new SpawnGroup { enemyIndex = enemy, count = count, interval = interval, delayBefore = delay };
        }

        static WaveDef W(int clearBonus, params SpawnGroup[] groups)
        {
            var w = new WaveDef { title = "Wave", clearBonus = clearBonus };
            w.groups.AddRange(groups);
            return w;
        }

        static FloorDef F(int number, params WaveDef[] waves)
        {
            var f = new FloorDef { title = $"Floor {number}" };
            f.waves.AddRange(waves);
            for (int i = 0; i < f.waves.Count; i++) f.waves[i].title = $"Floor {number} - wave {i + 1}";
            return f;
        }

        /// <summary>
        /// Six sealed floors with the Tziah I lineup (STORY.md §3). Each floor introduces one new thing.
        /// Wave sizes and spacing are PLACEHOLDERS to tune in playtesting (#17). Every floor still uses the
        /// board's default path until the Tartarus layouts (#25).
        /// </summary>
        public static List<FloorDef> Floors()
        {
            return new List<FloorDef>
            {
                // 1: basic Shadows (teaches placement and weaknesses)
                F(1, W(25, G(Maya, 6, 1.1f)),
                     W(50, G(Sigil, 4, 1.5f), G(Maya, 6, 0.9f, 1.5f))),
                // 2: fast Shadows (teaches skills)
                F(2, W(25, G(Drive, 5, 1.2f)),
                     W(50, G(Wheel, 4, 1.4f), G(Drive, 6, 0.8f, 1.5f))),
                // 3: tanky Shadows, ending with three Arcane Turrets (the 122F boss in the original game)
                F(3, W(25, G(Knight, 3, 2.5f)),
                     W(50, G(Knight, 4, 2f)),
                     W(75, G(Turret, 3, 3f))),
                // 4: flying Shadows
                F(4, W(25, G(Tiara, 6, 1f)),
                     W(50, G(Idol, 4, 1.4f), G(Tiara, 5, 0.8f, 1.5f))),
                // 5: everything at once, plus Hakurou Musha, ending with the Sleeping Table (the 135F sub-boss)
                F(5, W(25, G(Twins, 4, 1.2f), G(Relic, 3, 1.6f, 1f)),
                     W(50, G(Mother, 3, 1.8f), G(Magus, 4, 1.1f, 1f)),
                     W(75, G(Musha, 2, 3f), G(Twins, 3, 1f, 1f), G(Magus, 3, 1f, 1f)),
                     W(100, G(Table, 1, 1f))),
                // 6: boss floor. The Reaper + mixed summons are placeholders for the Jailer and its summons (#12)
                F(6, W(50, G(Maya, 6, 0.6f), G(Drive, 4, 0.8f, 1f), G(Tiara, 4, 0.9f, 1f)),
                     W(0, G(Reaper, 1, 1f), G(Knight, 3, 2f, 2f), G(Idol, 3, 1.2f, 1f))),
            };
        }

        /// <summary>A winding route across a 40 x 26 m board. The path is smoothed with Catmull-Rom curves.</summary>
        public static List<Vector3> PathPoints()
        {
            return new List<Vector3>
            {
                new Vector3(-18f, 0f, 9f),
                new Vector3(-9f, 0f, 9f),
                new Vector3(-9f, 0f, -7f),
                new Vector3(0f, 0f, -7f),
                new Vector3(0f, 0f, 7f),
                new Vector3(9f, 0f, 7f),
                new Vector3(9f, 0f, -9f),
                new Vector3(18f, 0f, -9f),
            };
        }
    }
}
