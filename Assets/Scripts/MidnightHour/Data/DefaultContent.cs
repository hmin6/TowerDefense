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
        // Indices used by the waves below.
        public const int Mask = 0, Slime = 1, Knight = 2, Reaper = 3;

        /// <summary>Field limit per floor (STORY.md §2): the closer to the Jailer, the stricter.</summary>
        public static List<int> FieldLimits()
        {
            return new List<int> { 8, 7, 6, 6, 5, 4 };
        }

        public static List<EnemyDef> Enemies()
        {
            return new List<EnemyDef>
            {
                new EnemyDef
                {
                    displayName = "Cowardly Mask", maxHP = 32f, speed = 4.2f,
                    weaknesses = { Element.Elec }, resists = { Element.Fire }, spReward = 12,
                    shape = PrimitiveType.Sphere, scale = new Vector3(0.9f, 0.9f, 0.9f),
                    color = new Color(0.16f, 0.1f, 0.22f)
                },
                new EnemyDef
                {
                    displayName = "Gloom Slime", maxHP = 75f, speed = 2.6f,
                    weaknesses = { Element.Ice }, resists = { Element.Elec }, spReward = 20,
                    shape = PrimitiveType.Sphere, scale = new Vector3(1.3f, 0.8f, 1.3f),
                    color = new Color(0.28f, 0.04f, 0.12f)
                },
                new EnemyDef
                {
                    displayName = "Iron Knight", maxHP = 190f, speed = 1.7f,
                    weaknesses = { Element.Fire }, resists = { Element.Ice }, spReward = 35,
                    shape = PrimitiveType.Capsule, scale = new Vector3(0.9f, 0.9f, 0.9f),
                    color = new Color(0.12f, 0.12f, 0.16f)
                },
                new EnemyDef
                {
                    displayName = "The Reaper", maxHP = 3200f, speed = 1.1f,
                    weaknesses = { }, resists = { }, spReward = 0, rank = ShadowRank.Boss,
                    shape = PrimitiveType.Capsule, scale = new Vector3(1.3f, 1.5f, 1.3f),
                    color = new Color(0.02f, 0.02f, 0.02f)
                },
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

        public static List<WaveDef> Waves()
        {
            return new List<WaveDef>
            {
                new WaveDef { title = "Day 1 - First Night", clearBonus = 75,
                    groups = { G(Mask, 8, 1.1f) } },
                new WaveDef { title = "Day 2 - Slime Season", clearBonus = 100,
                    groups = { G(Mask, 6, 0.9f), G(Slime, 4, 1.6f, 2f) } },
                new WaveDef { title = "Day 3 - Iron Footsteps", clearBonus = 100,
                    groups = { G(Slime, 6, 1.2f), G(Knight, 2, 3f, 1f), G(Mask, 8, 0.5f, 2f) } },
                new WaveDef { title = "Day 4 - Mixed Company", clearBonus = 125,
                    groups = { G(Knight, 4, 2.2f), G(Mask, 10, 0.45f, 1.5f), G(Slime, 6, 1f, 1.5f) } },
                new WaveDef { title = "Day 5 - The Swarm", clearBonus = 150,
                    groups = { G(Mask, 12, 0.4f), G(Knight, 6, 1.6f, 2f), G(Slime, 10, 0.7f, 1f) } },
                new WaveDef { title = "FULL MOON", clearBonus = 0,
                    groups = { G(Mask, 10, 0.4f), G(Knight, 5, 1.5f, 1f), G(Reaper, 1, 1f, 3f), G(Slime, 8, 1.2f, 2f) } },
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
