using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    // Enums below are saved as ints in Main.unity: append new values at the end, never reorder.

    /// <summary>Who summons a Persona tower. Fuuka is the navigator, so she never places a tower.</summary>
    public enum PartyMember { None, Makoto, Yukari, Junpei, Akihiko, Mitsuru, Aigis, Ken, Koromaru, Shinjiro }

    /// <summary>The Major Arcana, in tarot order.</summary>
    public enum Arcana
    {
        None, Fool, Magician, Priestess, Empress, Emperor, Hierophant, Lovers, Chariot, Justice, Hermit,
        Fortune, Strength, HangedMan, Death, Temperance, Devil, Tower, Star, Moon, Sun, Judgement
    }

    /// <summary>Every tower's job (STORY.md §5). No two towers share a role. None = a starter-kit tower.</summary>
    public enum TowerRole
    {
        None,
        // S.E.E.S. members
        Pushback, Artillery, Brawler, Controller, Sniper, Executioner, Debuffer, TankBuster,
        // Makoto's Personas
        Generalist, Trapper, SupportAura, ChainLightning, SpBattery, LastLine, AntiAir, Pulse,
        ResistanceBreaker, Multishot, Interceptor
    }

    /// <summary>Where a Persona comes from. Own = a member's own Persona. Movie / Game = how Makoto has it by September.</summary>
    public enum PersonaSource { Own, Movie, Game }

    /// <summary>How a Shadow moves along the path. Behaviour arrives with the Tziah I roster (#5).</summary>
    public enum ShadowMovement { Ground, Fast, Flying }

    /// <summary>Normal Shadow, a floor finale (Arcane Turret, Sleeping Table), the final boss, or a treasure Shadow.</summary>
    public enum ShadowRank { Normal, FloorBoss, Boss, Treasure }

    /// <summary>Readable names for the UI.</summary>
    public static class DefInfo
    {
        public static string RoleLabel(TowerRole r)
        {
            switch (r)
            {
                case TowerRole.TankBuster: return "Tank buster";
                case TowerRole.SupportAura: return "Support aura";
                case TowerRole.ChainLightning: return "Chain lightning";
                case TowerRole.SpBattery: return "SP battery";
                case TowerRole.LastLine: return "Last line of defense";
                case TowerRole.AntiAir: return "Anti-air";
                case TowerRole.ResistanceBreaker: return "Resistance breaker";
                case TowerRole.None: return "";
                default: return r.ToString();
            }
        }

        public static string ArcanaLabel(Arcana a)
        {
            return a == Arcana.HangedMan ? "Hanged Man" : a == Arcana.None ? "" : a.ToString();
        }
    }

    /// <summary>All stats for one tower type: a Persona. Edit these in the Inspector on the GameBootstrap object.</summary>
    [System.Serializable]
    public class TowerDef
    {
        [Tooltip("The Persona's name, shown on its card.")]
        public string displayName = "Tower";
        [TextArea(1, 3)] public string description = "";
        public Element element = Element.Elec;

        [Header("Persona")]
        [Tooltip("Who summons this Persona. Each member has exactly one; Makoto can field each of his once.")]
        public PartyMember user = PartyMember.None;
        public Arcana arcana = Arcana.None;
        [Tooltip("The tower's job (STORY.md §5). Behaviours arrive in M2.")]
        public TowerRole role = TowerRole.None;
        public PersonaSource source = PersonaSource.Own;

        [Header("Stats")]
        [Tooltip("Shots per second.")]
        public float fireRate = 1f;

        [Tooltip("Straight shots: speed along the flight line (m/s).\nBallistic shots: HORIZONTAL speed (m/s); gravity handles the vertical.")]
        public float projectileSpeed = 20f;

        public float damage = 10f;
        [Tooltip("Targeting radius measured on the ground (XZ).")]
        public float range = 8f;

        [Tooltip("Height of the tower pillar in meters (the muzzle sits just above it).")]
        public float height = 2f;

        [Tooltip("Ballistic = projectile uses gravity and the tower solves an arc.")]
        public bool ballistic = false;

        [Tooltip("0 = single target. > 0 = explosion radius.")]
        public float splashRadius = 0f;

        [Range(0.1f, 1f)] public float slowMultiplier = 1f;
        public float slowDuration = 0f;

        public float projectileScale = 0.3f;
        public Color color = Color.white;

        [Header("Optional art")]
        [Tooltip("Optional model. If it has children named 'Head' and 'Muzzle' they are used for aiming.")]
        public GameObject modelPrefab;
        public AudioClip fireSound;
    }

    /// <summary>A S.E.E.S. member who can join the party. Each places exactly one tower: their own Persona.</summary>
    [System.Serializable]
    public class MemberDef
    {
        public PartyMember id = PartyMember.None;
        public string displayName = "Member";
        [Tooltip("This member's Persona: the one tower they can place. Its element, Arcana and role describe the member.")]
        public TowerDef persona = new TowerDef();
    }

    /// <summary>All stats for one enemy (Shadow) type.</summary>
    [System.Serializable]
    public class EnemyDef : ISerializationCallbackReceiver
    {
        public string displayName = "Shadow";
        public float maxHP = 50f;
        [Tooltip("Meters per second along the path.")]
        public float speed = 3f;

        [Header("Affinities")]
        [Tooltip("Damage types that deal extra damage and knock this Shadow down.")]
        public List<Element> weaknesses = new List<Element>();
        [Tooltip("Damage types that deal reduced damage.")]
        public List<Element> resists = new List<Element>();

        [Header("Type")]
        public ShadowMovement movement = ShadowMovement.Ground;
        [Tooltip("Boss = can't be knocked down and gets the big health bar.")]
        public ShadowRank rank = ShadowRank.Normal;
        public int spReward = 15;

        [Header("Placeholder look (ignored if modelPrefab is set)")]
        public PrimitiveType shape = PrimitiveType.Sphere;
        public Vector3 scale = Vector3.one;
        public Color color = new Color(0.1f, 0.1f, 0.12f);

        [Header("Optional art")]
        public GameObject modelPrefab;
        public AudioClip deathSound;

        // Starter-kit fields, kept only so old scene data migrates into the lists above.
        [SerializeField, HideInInspector] Element weakness = Element.None;
        [SerializeField, HideInInspector] Element resist = Element.None;
        [SerializeField, HideInInspector] bool isBoss = false;

        public bool IsBoss { get { return rank == ShadowRank.Boss; } }

        public bool IsWeakTo(Element e)
        {
            return ElementInfo.HasAffinity(e) && weaknesses != null && weaknesses.Contains(e);
        }

        public bool Resists(Element e)
        {
            return ElementInfo.HasAffinity(e) && resists != null && resists.Contains(e);
        }

        /// <summary>The first listed weakness (used for the eye colour), or None.</summary>
        public Element PrimaryWeakness
        {
            get
            {
                if (weaknesses != null)
                    foreach (var e in weaknesses) if (ElementInfo.HasAffinity(e)) return e;
                return Element.None;
            }
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (weaknesses == null) weaknesses = new List<Element>();
            if (resists == null) resists = new List<Element>();
            if (weakness != Element.None && !weaknesses.Contains(weakness)) weaknesses.Add(weakness);
            if (resist != Element.None && !resists.Contains(resist)) resists.Add(resist);
            if (isBoss) rank = ShadowRank.Boss;
            weakness = Element.None;
            resist = Element.None;
            isBoss = false;
        }
    }

    [System.Serializable]
    public class SpawnGroup
    {
        [Tooltip("Index into the Enemies list.")]
        public int enemyIndex = 0;
        public int count = 5;
        [Tooltip("Seconds between spawns inside this group.")]
        public float interval = 1f;
        [Tooltip("Seconds to wait before this group starts.")]
        public float delayBefore = 0f;
    }

    [System.Serializable]
    public class WaveDef
    {
        [Tooltip("Only used in warnings and logs.")]
        public string title = "Wave";
        [Tooltip("SP given when this wave is cleared.")]
        public int clearBonus = 100;
        public List<SpawnGroup> groups = new List<SpawnGroup>();
    }

    /// <summary>One sealed floor of Tartarus (STORY.md §2-3): its waves, and optionally its own path.</summary>
    [System.Serializable]
    public class FloorDef
    {
        [Tooltip("Shown on banners, the status panel and Fuuka's scan.")]
        public string title = "Floor";
        [Tooltip("The floor's waves, in order. The seal breaks when the last one is cleared.")]
        public List<WaveDef> waves = new List<WaveDef>();
        [Tooltip("Optional: this floor's own path (2+ points, smoothed into a winding route).\n" +
                 "Empty = the board's default path (GameBootstrap > Map).")]
        public List<Vector3> pathPoints = new List<Vector3>();
    }
}
