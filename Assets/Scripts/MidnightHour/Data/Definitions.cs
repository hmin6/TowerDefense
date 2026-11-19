using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    /// <summary>All stats for one tower type. Edit these in the Inspector on the GameBootstrap object.</summary>
    [System.Serializable]
    public class TowerDef
    {
        public string displayName = "Tower";
        [TextArea(1, 3)] public string description = "";
        public Element element = Element.Zio;
        public int cost = 100;

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

    /// <summary>All stats for one enemy (Shadow) type.</summary>
    [System.Serializable]
    public class EnemyDef
    {
        public string displayName = "Shadow";
        public float maxHP = 50f;
        [Tooltip("Meters per second along the path.")]
        public float speed = 3f;
        public Element weakness = Element.None;
        public Element resist = Element.None;
        public int spReward = 15;
        [Tooltip("Bosses can't be knocked down and get a big health bar.")]
        public bool isBoss = false;

        [Header("Placeholder look (ignored if modelPrefab is set)")]
        public PrimitiveType shape = PrimitiveType.Sphere;
        public Vector3 scale = Vector3.one;
        public Color color = new Color(0.1f, 0.1f, 0.12f);

        [Header("Optional art")]
        public GameObject modelPrefab;
        public AudioClip deathSound;
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
        public string title = "Night";
        public int clearBonus = 100;
        public List<SpawnGroup> groups = new List<SpawnGroup>();
    }
}
