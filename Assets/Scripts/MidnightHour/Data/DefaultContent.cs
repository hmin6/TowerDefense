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

        public static List<TowerDef> Towers()
        {
            return new List<TowerDef>
            {
                new TowerDef
                {
                    displayName = "Zio Spark",
                    description = "Low tower. Rapid lightning darts. Shreds Masks.",
                    element = Element.Zio, cost = 100,
                    fireRate = 3.0f, projectileSpeed = 30f, damage = 8f, range = 7f, height = 1.2f,
                    projectileScale = 0.25f, color = new Color(1f, 0.85f, 0.15f)
                },
                new TowerDef
                {
                    displayName = "Bufu Frost",
                    description = "Mid tower. Ice shards slow Shadows by 50%.",
                    element = Element.Bufu, cost = 125,
                    fireRate = 1.1f, projectileSpeed = 16f, damage = 14f, range = 8.5f, height = 2.4f,
                    slowMultiplier = 0.5f, slowDuration = 2f,
                    projectileScale = 0.35f, color = new Color(0.4f, 0.85f, 1f)
                },
                new TowerDef
                {
                    displayName = "Agi Inferno",
                    description = "Tall tower. Lobs fireballs on a gravity arc. Splash.",
                    element = Element.Agi, cost = 175,
                    fireRate = 0.55f, projectileSpeed = 9f, damage = 38f, range = 11f, height = 4.0f,
                    ballistic = true, splashRadius = 2.2f,
                    projectileScale = 0.55f, color = new Color(1f, 0.42f, 0.1f)
                },
            };
        }

        public static List<EnemyDef> Enemies()
        {
            return new List<EnemyDef>
            {
                new EnemyDef
                {
                    displayName = "Cowardly Mask", maxHP = 32f, speed = 4.2f,
                    weakness = Element.Zio, resist = Element.Agi, spReward = 12,
                    shape = PrimitiveType.Sphere, scale = new Vector3(0.9f, 0.9f, 0.9f),
                    color = new Color(0.16f, 0.1f, 0.22f)
                },
                new EnemyDef
                {
                    displayName = "Gloom Slime", maxHP = 75f, speed = 2.6f,
                    weakness = Element.Bufu, resist = Element.Zio, spReward = 20,
                    shape = PrimitiveType.Sphere, scale = new Vector3(1.3f, 0.8f, 1.3f),
                    color = new Color(0.28f, 0.04f, 0.12f)
                },
                new EnemyDef
                {
                    displayName = "Iron Knight", maxHP = 190f, speed = 1.7f,
                    weakness = Element.Agi, resist = Element.Bufu, spReward = 35,
                    shape = PrimitiveType.Capsule, scale = new Vector3(0.9f, 0.9f, 0.9f),
                    color = new Color(0.12f, 0.12f, 0.16f)
                },
                new EnemyDef
                {
                    displayName = "The Reaper", maxHP = 3200f, speed = 1.1f,
                    weakness = Element.None, resist = Element.None, spReward = 0, isBoss = true,
                    shape = PrimitiveType.Capsule, scale = new Vector3(1.3f, 1.5f, 1.3f),
                    color = new Color(0.02f, 0.02f, 0.02f)
                },
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
