using UnityEngine;

namespace MidnightHour
{
    /// <summary>Damage elements, named after the classic Persona spell families.</summary>
    public enum Element { None, Agi, Bufu, Zio, Almighty }

    /// <summary>What happened when something hit an enemy.</summary>
    public enum HitResult { None, Normal, Resisted, Weak, Knockdown, Killed }

    public static class ElementInfo
    {
        public static Color GetColor(Element e)
        {
            switch (e)
            {
                case Element.Agi: return new Color(1f, 0.42f, 0.12f);
                case Element.Bufu: return new Color(0.45f, 0.85f, 1f);
                case Element.Zio: return new Color(1f, 0.88f, 0.2f);
                case Element.Almighty: return new Color(1f, 1f, 1f);
                default: return new Color(0.55f, 0.55f, 0.55f);
            }
        }

        public static string Label(Element e)
        {
            switch (e)
            {
                case Element.Agi: return "AGI (fire)";
                case Element.Bufu: return "BUFU (ice)";
                case Element.Zio: return "ZIO (elec)";
                case Element.Almighty: return "ALMIGHTY";
                default: return "none";
            }
        }
    }
}
