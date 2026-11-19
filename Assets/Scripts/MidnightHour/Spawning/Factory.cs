using UnityEngine;

namespace MidnightHour
{
    /// <summary>
    /// Builds towers, enemies and effects. If a def has a modelPrefab it is used,
    /// otherwise a stylised placeholder is assembled from primitives.
    /// </summary>
    public static class Factory
    {
        static readonly Color Dark = new Color(0.07f, 0.07f, 0.09f);

        // ---------------------------------------------------------------- Towers

        public static Tower BuildTower(TowerDef def, int typeIndex, Vector3 groundPos)
        {
            var root = new GameObject(def.displayName);
            root.transform.position = groundPos;
            BuildTowerVisual(def, root.transform, false, out Transform head, out Transform muzzle);
            var tower = root.AddComponent<Tower>();
            tower.Init(def, typeIndex, head, muzzle);
            Flash.Spawn(groundPos + Vector3.up * 0.5f, def.color, 1.5f, 0.4f);
            return tower;
        }

        public static GameObject BuildTowerGhost(TowerDef def)
        {
            var root = new GameObject("Placement Ghost");
            BuildTowerVisual(def, root.transform, true, out _, out _);
            VisualFactory.StripColliders(root);
            return root;
        }

        static void BuildTowerVisual(TowerDef def, Transform root, bool ghost, out Transform head, out Transform muzzle)
        {
            float h = Mathf.Max(0.5f, def.height);

            if (def.modelPrefab != null)
            {
                var model = Object.Instantiate(def.modelPrefab, root);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                head = VisualFactory.FindDeep(model.transform, "Head");
                muzzle = VisualFactory.FindDeep(model.transform, "Muzzle");
                if (head == null)
                {
                    head = new GameObject("Head").transform;
                    head.SetParent(root, false);
                    head.localPosition = Vector3.up * h;
                }
                if (muzzle == null)
                {
                    muzzle = new GameObject("Muzzle").transform;
                    muzzle.SetParent(head, false);
                    muzzle.localPosition = new Vector3(0f, 0f, 0.6f);
                }
                if (!ghost && root.GetComponentInChildren<Collider>() == null)
                {
                    var cap = root.gameObject.AddComponent<CapsuleCollider>();
                    cap.height = h;
                    cap.radius = 0.6f;
                    cap.center = Vector3.up * h * 0.5f;
                }
                return;
            }

            // Plinth, pillar (height = def.height), glowing ring, orb head with a barrel.
            VisualFactory.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.1f, 0f), new Vector3(1.4f, 0.1f, 1.4f), Dark);
            VisualFactory.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.7f, h * 0.5f, 0.7f),
                               Color.Lerp(Dark, def.color, 0.25f), 0f, keepCollider: true, name: "Pillar");
            VisualFactory.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, h * 0.8f, 0f), new Vector3(0.85f, 0.04f, 0.85f), def.color, 1.5f);

            head = new GameObject("Head").transform;
            head.SetParent(root, false);
            head.localPosition = new Vector3(0f, h + 0.35f, 0f);
            VisualFactory.Prim(PrimitiveType.Sphere, head, Vector3.zero, Vector3.one * 0.8f, def.color, 0.6f);
            VisualFactory.Prim(PrimitiveType.Cube, head, new Vector3(0f, 0f, 0.5f), new Vector3(0.22f, 0.22f, 0.8f), Dark);
            if (def.ballistic)
                VisualFactory.Prim(PrimitiveType.Cylinder, head, new Vector3(0f, 0f, 0.85f), new Vector3(0.4f, 0.06f, 0.4f), def.color, 1f)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(head, false);
            muzzle.localPosition = new Vector3(0f, 0f, 0.95f);
        }

        // ---------------------------------------------------------------- Enemies

        public static Enemy SpawnEnemy(EnemyDef def, int typeIndex)
        {
            var path = EnemyPath.Instance;
            if (path == null || def == null) return null;

            GameObject root;
            if (def.modelPrefab != null)
            {
                root = new GameObject(def.displayName);
                var model = Object.Instantiate(def.modelPrefab, root.transform);
                model.transform.localPosition = Vector3.zero;
                if (root.GetComponentInChildren<Collider>() == null)
                {
                    Bounds b = VisualFactory.RendererBounds(root);
                    var cap = root.AddComponent<CapsuleCollider>();
                    cap.center = b.center;
                    cap.height = Mathf.Max(0.5f, b.size.y);
                    cap.radius = Mathf.Max(0.25f, Mathf.Max(b.extents.x, b.extents.z));
                }
            }
            else
            {
                root = GameObject.CreatePrimitive(def.shape);
                root.name = def.displayName;
                root.transform.localScale = def.scale;
                root.GetComponent<Renderer>().sharedMaterial = VisualFactory.Lit(def.color, 0.1f);
                AddMask(root, def);
            }

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.mass = def.IsBoss ? 50f : 1f;

            var enemy = root.AddComponent<Enemy>();
            enemy.Init(def, typeIndex, path);
            Flash.Spawn(enemy.AimPoint, new Color(0.6f, 0.2f, 1f), 1.2f, 0.35f);
            return enemy;
        }

        /// <summary>White Shadow mask on the front. Its EYES glow in the colour of its first weakness.</summary>
        static void AddMask(GameObject root, EnemyDef def)
        {
            var mf = root.GetComponent<MeshFilter>();
            Vector3 ext = mf != null ? mf.sharedMesh.bounds.extents : Vector3.one * 0.5f;
            float faceY = def.shape == PrimitiveType.Capsule ? ext.y * 0.45f : ext.y * 0.2f;

            var mask = VisualFactory.Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, faceY, ext.z * 0.9f),
                                          new Vector3(ext.x * 1.3f, 0.32f, 0.12f), new Color(0.92f, 0.92f, 0.9f), 0f, false, "Mask");
            Color eye = def.IsBoss ? new Color(1f, 0.05f, 0.05f) : ElementInfo.GetColor(def.PrimaryWeakness);
            VisualFactory.Prim(PrimitiveType.Cube, mask.transform, new Vector3(-0.22f, 0.05f, 0.6f), new Vector3(0.18f, 0.4f, 0.4f), eye, 3f);
            VisualFactory.Prim(PrimitiveType.Cube, mask.transform, new Vector3(0.22f, 0.05f, 0.6f), new Vector3(0.18f, 0.4f, 0.4f), eye, 3f);

            if (def.IsBoss)
            {
                // Two long barrels, because every reaper needs a pair of guns.
                VisualFactory.Prim(PrimitiveType.Cube, root.transform, new Vector3(-0.75f, 0f, 0.4f), new Vector3(0.12f, 0.12f, 0.9f), new Color(0.5f, 0.5f, 0.55f));
                VisualFactory.Prim(PrimitiveType.Cube, root.transform, new Vector3(0.75f, 0f, 0.4f), new Vector3(0.12f, 0.12f, 0.9f), new Color(0.5f, 0.5f, 0.55f));
            }
            else if (def.shape == PrimitiveType.Capsule)
            {
                // Knight helmet crest.
                VisualFactory.Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, ext.y * 0.95f, 0f), new Vector3(0.12f, 0.35f, 0.9f), new Color(0.5f, 0.05f, 0.08f));
            }
        }

        // ---------------------------------------------------------------- Effects

        /// <summary>Physics shards that burst out when a Shadow dies.</summary>
        public static void Shatter(Vector3 pos, Color color, int count, float size)
        {
            for (int i = 0; i < count; i++)
            {
                var go = VisualFactory.Prim(PrimitiveType.Cube, null, pos + Random.insideUnitSphere * 0.3f,
                                            Vector3.one * size * Random.Range(0.6f, 1f), Color.Lerp(color, Color.white, 0.15f),
                                            0.4f, keepCollider: true, name: "Debris");
                go.AddComponent<Debris>();
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                rb.AddExplosionForce(7f, pos - Vector3.up * 0.4f, 2f, 0.6f, ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);
                Object.Destroy(go, Random.Range(1.8f, 2.8f));
            }
        }
    }
}
