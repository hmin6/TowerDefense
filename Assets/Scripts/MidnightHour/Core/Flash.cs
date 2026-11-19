using UnityEngine;

namespace MidnightHour
{
    /// <summary>Expanding, fading sphere used for hits and explosions.</summary>
    public class Flash : MonoBehaviour
    {
        public float targetRadius = 1f;
        public float life = 0.3f;
        public Color color = Color.white;

        float age;
        Material mat;

        public static void Spawn(Vector3 pos, Color color, float radius, float life = 0.3f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Flash";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.2f;
            var f = go.AddComponent<Flash>();
            f.targetRadius = radius;
            f.life = life;
            f.color = color;
        }

        void Start()
        {
            var r = GetComponent<Renderer>();
            mat = VisualFactory.Unlit(color, shared: false);
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, targetRadius * 2f, Mathf.Sqrt(t));
            if (mat != null) mat.color = new Color(color.r, color.g, color.b, (1f - t) * 0.7f);
            if (t >= 1f) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }
    }
}
