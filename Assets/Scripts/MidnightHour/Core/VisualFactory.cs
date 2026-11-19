using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightHour
{
    /// <summary>
    /// Builds placeholder visuals out of Unity primitives so the game runs with zero imported assets.
    /// Materials are cloned from the render pipeline's default material, so this works in
    /// Built-in, URP and HDRP without picking a shader by name.
    /// </summary>
    public static class VisualFactory
    {
        static Material baseLit;
        static Shader unlitShader;
        static readonly Dictionary<long, Material> litCache = new Dictionary<long, Material>();
        static readonly Dictionary<int, Material> unlitCache = new Dictionary<int, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            baseLit = null;
            unlitShader = null;
            litCache.Clear();
            unlitCache.Clear();
        }

        static Material BaseLit
        {
            get
            {
                if (baseLit == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    baseLit = tmp.GetComponent<Renderer>().sharedMaterial;
                    Object.DestroyImmediate(tmp);
                }
                return baseLit;
            }
        }

        static int Key(Color c)
        {
            Color32 k = c;
            return (k.r << 24) | (k.g << 16) | (k.b << 8) | k.a;
        }

        /// <summary>Shared lit material of a color. emission &gt; 0 makes it glow (add Bloom for the full effect).</summary>
        public static Material Lit(Color c, float emission = 0f)
        {
            long key = ((long)Key(c) << 16) | (long)Mathf.Clamp(Mathf.RoundToInt(emission * 100f), 0, 65535);
            if (litCache.TryGetValue(key, out var m) && m != null) return m;

            m = new Material(BaseLit) { name = "MH_Lit_" + key };
            m.color = c;
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            litCache[key] = m;
            return m;
        }

        /// <summary>Unlit, alpha-capable material (used for lines, trails and flashes).</summary>
        public static Material Unlit(Color c, bool shared = true)
        {
            int key = Key(c);
            if (shared && unlitCache.TryGetValue(key, out var cached) && cached != null) return cached;

            if (unlitShader == null)
            {
                unlitShader = Shader.Find("Sprites/Default");
                if (unlitShader == null) unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            var m = unlitShader != null ? new Material(unlitShader) : new Material(BaseLit);
            m.color = c;
            if (shared) unlitCache[key] = m;
            return m;
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale,
                                      Color color, float emission = 0f, bool keepCollider = false, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (!string.IsNullOrEmpty(name)) go.name = name;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Lit(color, emission);
            return go;
        }

        public static void StripColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        }

        /// <summary>Recolors every renderer (used for the green/red placement ghost).</summary>
        public static void Tint(GameObject go, Color c, float emission)
        {
            var mat = Lit(c, emission);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is LineRenderer || r is TrailRenderer) continue;
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        /// <summary>LineRenderers can't share a GameObject with a MeshRenderer, so this makes a child.</summary>
        public static LineRenderer Line(Transform parent, Color c, float width, string name = "Line")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Unlit(Color.white);
            lr.startColor = lr.endColor = c;
            lr.startWidth = lr.endWidth = width;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.numCapVertices = 2;
            return lr;
        }

        public static void SetCircle(LineRenderer lr, Vector3 center, float radius, int segments = 64)
        {
            lr.loop = true;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0.06f, Mathf.Sin(a) * radius));
            }
        }

        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        public static Bounds RendererBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
