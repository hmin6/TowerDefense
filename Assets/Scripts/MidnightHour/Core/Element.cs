using UnityEngine;

namespace MidnightHour
{
    /// <summary>
    /// Damage types, as in Persona 3 (STORY.md §3). Physical: Slash, Strike, Pierce.
    /// Magic: Fire (Agi), Ice (Bufu), Elec (Zio), Wind (Garu), Light (Hama), Dark (Mudo).
    /// Almighty ignores weaknesses and resistances.
    ///
    /// The numbers are saved as ints in Main.unity, so NEVER renumber a value; only add new ones.
    /// (1-3 were Agi / Bufu / Zio in the starter kit, so old data reads as Fire / Ice / Elec.)
    /// </summary>
    public enum Element
    {
        None = 0,
        Slash = 5,
        Strike = 6,
        Pierce = 7,
        Fire = 1,
        Ice = 2,
        Elec = 3,
        Wind = 8,
        Light = 9,
        Dark = 10,
        Almighty = 4,
    }

    /// <summary>What happened when something hit an enemy.</summary>
    public enum HitResult { None, Normal, Resisted, Weak, Knockdown, Killed }

    public static class ElementInfo
    {
        /// <summary>Every damage type, in the game's usual order.</summary>
        public static readonly Element[] All =
        {
            Element.Slash, Element.Strike, Element.Pierce,
            Element.Fire, Element.Ice, Element.Elec, Element.Wind, Element.Light, Element.Dark,
            Element.Almighty,
        };

        public static bool IsPhysical(Element e)
        {
            return e == Element.Slash || e == Element.Strike || e == Element.Pierce;
        }

        /// <summary>True when weaknesses and resistances apply to this damage type (not None, not Almighty).</summary>
        public static bool HasAffinity(Element e)
        {
            return e != Element.None && e != Element.Almighty;
        }

        public static Color GetColor(Element e)
        {
            switch (e)
            {
                case Element.Slash: return new Color(0.9f, 0.9f, 0.95f);
                case Element.Strike: return new Color(0.8f, 0.56f, 0.36f);
                case Element.Pierce: return new Color(0.6f, 0.72f, 0.85f);
                case Element.Fire: return new Color(1f, 0.42f, 0.12f);
                case Element.Ice: return new Color(0.45f, 0.85f, 1f);
                case Element.Elec: return new Color(1f, 0.88f, 0.2f);
                case Element.Wind: return new Color(0.35f, 0.95f, 0.45f);
                case Element.Light: return new Color(1f, 0.96f, 0.62f);
                case Element.Dark: return new Color(0.62f, 0.18f, 0.8f);
                case Element.Almighty: return new Color(1f, 1f, 1f);
                default: return new Color(0.55f, 0.55f, 0.55f);
            }
        }

        /// <summary>The Persona 3 spell family for magic types ("" for physical, None and Almighty).</summary>
        public static string SpellFamily(Element e)
        {
            switch (e)
            {
                case Element.Fire: return "Agi";
                case Element.Ice: return "Bufu";
                case Element.Elec: return "Zio";
                case Element.Wind: return "Garu";
                case Element.Light: return "Hama";
                case Element.Dark: return "Mudo";
                default: return "";
            }
        }

        /// <summary>Short HUD label, for example "FIRE (Agi)" or "PIERCE".</summary>
        public static string Label(Element e)
        {
            if (e == Element.None) return "none";
            string family = SpellFamily(e);
            string name = e.ToString().ToUpperInvariant();
            return family.Length > 0 ? $"{name} ({family})" : name;
        }
    }
}
