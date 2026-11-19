using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightHour
{
    /// <summary>
    /// ONE-CLICK SETUP: put this on an empty GameObject in an empty scene and press Play.
    /// It builds the board, path, camera, lighting and every game system at runtime.
    ///
    /// All tower / enemy / wave stats live in the lists below and are editable in the Inspector.
    /// Right-click the component header > Reset to restore the defaults.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Content")]
        public List<TowerDef> towers = new List<TowerDef>();
        public List<EnemyDef> enemies = new List<EnemyDef>();
        public List<WaveDef> waves = new List<WaveDef>();

        [Header("S.E.E.S. roster (STORY.md §5; used from party selection, #2, onward)")]
        [Tooltip("The 8 members who can join Makoto. Each places one tower: their own Persona.")]
        public List<MemberDef> members = new List<MemberDef>();
        [Tooltip("Makoto's Personas (the Wild Card). He can field each one once.")]
        public List<TowerDef> makotoPersonas = new List<TowerDef>();

        [Header("Map")]
        [Tooltip("Optional: an object whose children are path waypoints (in order). Overrides the list below.")]
        public Transform customPathRoot;
        public List<Vector3> pathControlPoints = new List<Vector3>();
        public Vector2 boardHalfSize = new Vector2(20f, 13f);
        public float pathWidth = 2f;

        [Header("Rules")]
        public int startingSP = 350;
        public int baseTowerLimit = 6;

        [Header("Look")]
        public bool buildEnvironment = true;
        public Color groundColor = new Color(0.05f, 0.11f, 0.08f);
        public Color gridColor = new Color(0.14f, 0.32f, 0.22f, 0.6f);
        public Color pathColor = new Color(0.42f, 0.03f, 0.07f);
        [Tooltip("The sickly green of the Dark Hour.")]
        public Color skyColor = new Color(0.02f, 0.07f, 0.05f);

        [Header("Optional art overrides")]
        public GameObject groundPrefab;
        public GameObject goalPrefab;
        public GameObject spawnPrefab;

        [Header("Audio (optional)")]
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.4f;
        public AudioClip weakSound;
        public AudioClip allOutSound;
        public AudioClip killSound;
        public AudioClip gameOverSound;
        public AudioClip victorySound;

        void Reset()
        {
            towers = DefaultContent.Towers();
            enemies = DefaultContent.Enemies();
            waves = DefaultContent.Waves();
            members = DefaultContent.Members();
            makotoPersonas = DefaultContent.MakotoPersonas();
            pathControlPoints = DefaultContent.PathPoints();
        }

        void FillMissing()
        {
            if (towers == null || towers.Count == 0) towers = DefaultContent.Towers();
            if (enemies == null || enemies.Count == 0) enemies = DefaultContent.Enemies();
            if (waves == null || waves.Count == 0) waves = DefaultContent.Waves();
            if (members == null || members.Count == 0) members = DefaultContent.Members();
            if (makotoPersonas == null || makotoPersonas.Count == 0) makotoPersonas = DefaultContent.MakotoPersonas();
            if (pathControlPoints == null || pathControlPoints.Count < 2) pathControlPoints = DefaultContent.PathPoints();
        }

        void Awake()
        {
            FillMissing();

            Camera cam = SetupCamera();
            SetupLighting();

            // Path
            var pathGo = new GameObject("Enemy Path");
            var path = pathGo.AddComponent<EnemyPath>();
            var pts = new List<Vector3>();
            if (customPathRoot != null && customPathRoot.childCount >= 2)
                foreach (Transform t in customPathRoot) pts.Add(new Vector3(t.position.x, 0f, t.position.z));
            else
                pts.AddRange(pathControlPoints);
            path.SetControlPoints(pts);

            if (buildEnvironment)
            {
                BuildGround();
                BuildPathVisuals(path);
                BuildSpawnAndGoal(path);
            }
            else
            {
                EnsureGroundCollider();
            }

            // Systems (spawner first so GameManager can find it)
            var systems = new GameObject("Game Systems");
            var spawner = systems.AddComponent<WaveSpawner>();
            var gm = systems.AddComponent<GameManager>();
            gm.startingSP = startingSP;
            gm.baseTowerLimit = baseTowerLimit;
            gm.weakSound = weakSound;
            gm.allOutSound = allOutSound;
            gm.killSound = killSound;
            gm.gameOverSound = gameOverSound;
            gm.victorySound = victorySound;
            gm.Configure(towers, enemies, waves, spawner);
            gm.ConfigureRoster(members, makotoPersonas);

            var placer = systems.AddComponent<TowerPlacer>();
            placer.mapHalfExtents = boardHalfSize - Vector2.one * 0.8f;
            placer.pathClearance = pathWidth * 0.5f + 0.7f;
            systems.AddComponent<HUD>();

            if (music != null)
            {
                var src = cam.gameObject.AddComponent<AudioSource>();
                src.clip = music;
                src.loop = true;
                src.volume = musicVolume;
                src.spatialBlend = 0f;
                src.Play();
            }
        }

        Camera SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0f, 27f, -23f);
            cam.transform.LookAt(new Vector3(0f, 0f, -1.5f));
            cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = skyColor;
            cam.farClipPlane = 300f;
            if (cam.GetComponent<CameraRig>() == null)
            {
                var rig = cam.gameObject.AddComponent<CameraRig>();
                rig.focusBounds = boardHalfSize - new Vector2(2f, 1f);
            }
            return cam;
        }

        void SetupLighting()
        {
            Light sun = null;
            foreach (var l in FindObjectsOfTypeCompat())
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null)
            {
                sun = new GameObject("Moonlight").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.color = new Color(0.72f, 1f, 0.8f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.27f, 0.25f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = skyColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 120f;
        }

        static Light[] FindObjectsOfTypeCompat()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<Light>();
#endif
        }

        // ------------------------------------------------------------ Environment

        void BuildGround()
        {
            var env = new GameObject("Environment").transform;

            GameObject ground;
            if (groundPrefab != null)
            {
                ground = Instantiate(groundPrefab, env);
                EnsureGroundCollider();
            }
            else
            {
                ground = VisualFactory.Prim(PrimitiveType.Cube, env, new Vector3(0f, -0.5f, 0f),
                                            new Vector3(boardHalfSize.x * 2f, 1f, boardHalfSize.y * 2f), groundColor, 0f,
                                            keepCollider: true, name: "Ground");
            }
            ground.AddComponent<GroundMarker>();

            // Faint grid so players can read placement spots.
            var grid = new GameObject("Grid").transform;
            grid.SetParent(env, false);
            for (float x = -boardHalfSize.x; x <= boardHalfSize.x + 0.01f; x += 2f)
            {
                var lr = VisualFactory.Line(grid, gridColor, 0.03f, "GridX");
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(x, 0.01f, -boardHalfSize.y));
                lr.SetPosition(1, new Vector3(x, 0.01f, boardHalfSize.y));
            }
            for (float z = -boardHalfSize.y; z <= boardHalfSize.y + 0.01f; z += 2f)
            {
                var lr = VisualFactory.Line(grid, gridColor, 0.03f, "GridZ");
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(-boardHalfSize.x, 0.01f, z));
                lr.SetPosition(1, new Vector3(boardHalfSize.x, 0.01f, z));
            }

            // Border pillars (decoration, outside the build area).
            var deco = new GameObject("Decor").transform;
            deco.SetParent(env, false);
            for (int i = 0; i < 14; i++)
            {
                float x = Mathf.Lerp(-boardHalfSize.x, boardHalfSize.x, i / 13f);
                float h = 1.5f + Mathf.PerlinNoise(i * 0.7f, 0.3f) * 4f;
                VisualFactory.Prim(PrimitiveType.Cube, deco, new Vector3(x, h * 0.5f, boardHalfSize.y + 1.5f),
                                   new Vector3(1.2f, h, 1.2f), new Color(0.06f, 0.08f, 0.07f));
            }
        }

        void EnsureGroundCollider()
        {
            foreach (var gmk in FindGroundMarkers()) if (gmk != null) return;
            var floor = new GameObject("Invisible Ground");
            var box = floor.AddComponent<BoxCollider>();
            box.size = new Vector3(boardHalfSize.x * 2f, 1f, boardHalfSize.y * 2f);
            box.center = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<GroundMarker>();
        }

        static GroundMarker[] FindGroundMarkers()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<GroundMarker>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<GroundMarker>();
#endif
        }

        void BuildPathVisuals(EnemyPath path)
        {
            var root = new GameObject("Path Visual").transform;
            var pts = path.Points;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector3 a = pts[i], b = pts[i + 1];
                Vector3 d = b - a;
                float len = d.magnitude;
                if (len < 1e-3f) continue;
                var seg = VisualFactory.Prim(PrimitiveType.Cube, root, (a + b) * 0.5f + Vector3.up * 0.02f,
                                             new Vector3(pathWidth, 0.04f, len + 0.05f), pathColor, 0.25f);
                seg.transform.rotation = Quaternion.LookRotation(d);
                seg.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            for (int i = 0; i < pts.Count; i++)
            {
                var joint = VisualFactory.Prim(PrimitiveType.Cylinder, root, pts[i] + Vector3.up * 0.02f,
                                               new Vector3(pathWidth, 0.02f, pathWidth), pathColor, 0.25f);
                joint.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        void BuildSpawnAndGoal(EnemyPath path)
        {
            Vector3 start = path.StartPoint;
            Vector3 end = path.EndPoint;

            // Spawn portal
            if (spawnPrefab != null) Instantiate(spawnPrefab, start, Quaternion.identity);
            else
            {
                var portal = new GameObject("Spawn Portal").transform;
                portal.position = start;
                VisualFactory.Prim(PrimitiveType.Cylinder, portal, new Vector3(0f, 0.05f, 0f), new Vector3(2.6f, 0.05f, 2.6f),
                                   new Color(0.45f, 0.1f, 0.85f), 1.8f);
                var cube = VisualFactory.Prim(PrimitiveType.Cube, portal, new Vector3(0f, 1.6f, 0f), Vector3.one * 0.8f,
                                              new Color(0.6f, 0.2f, 1f), 2f);
                var spin = cube.AddComponent<Spin>();
                spin.degreesPerSecond = new Vector3(40f, 90f, 25f);
                spin.bobHeight = 0.3f;
            }

            // Goal: the Clock Tower
            if (goalPrefab != null) Instantiate(goalPrefab, end, Quaternion.identity);
            else
            {
                var tower = new GameObject("Clock Tower").transform;
                tower.position = end + new Vector3(1.2f, 0f, 0f);
                VisualFactory.Prim(PrimitiveType.Cube, tower, new Vector3(0f, 3.5f, 0f), new Vector3(2.2f, 7f, 2.2f),
                                   new Color(0.1f, 0.1f, 0.12f), 0f, keepCollider: false, name: "Body");
                VisualFactory.Prim(PrimitiveType.Cube, tower, new Vector3(0f, 7.4f, 0f), new Vector3(2.6f, 0.8f, 2.6f),
                                   new Color(0.3f, 0.03f, 0.06f));
                var face = VisualFactory.Prim(PrimitiveType.Cylinder, tower, new Vector3(0f, 5.6f, -1.12f),
                                              new Vector3(1.6f, 0.05f, 1.6f), new Color(0.7f, 1f, 0.6f), 2.5f, false, "Clock Face");
                face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var hand = VisualFactory.Prim(PrimitiveType.Cube, tower, new Vector3(0f, 5.6f, -1.2f),
                                              new Vector3(0.1f, 0.7f, 0.05f), Color.black, 0f, false, "Hand");
                var handSpin = hand.AddComponent<Spin>();
                handSpin.degreesPerSecond = new Vector3(0f, 0f, -30f);
            }
        }
    }
}
