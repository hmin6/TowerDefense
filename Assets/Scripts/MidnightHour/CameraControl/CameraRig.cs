using UnityEngine;

namespace MidnightHour
{
    /// <summary>WASD / arrow keys pan, scroll wheel zooms, plus screen shake. Works while paused.</summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        public float panSpeed = 18f;
        public float zoomStep = 3f;
        public float minDistance = 14f;
        public float maxDistance = 55f;
        public Vector2 focusBounds = new Vector2(18f, 12f);

        Vector3 focus;
        Vector3 backDir;
        float distance;
        float shake;

        void Awake() { Instance = this; }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            backDir = -transform.forward;
            var ground = new Plane(Vector3.up, Vector3.zero);
            var ray = new Ray(transform.position, transform.forward);
            if (ground.Raycast(ray, out float d))
            {
                focus = ray.GetPoint(d);
                distance = d;
            }
            else
            {
                focus = Vector3.zero;
                distance = 32f;
            }
        }

        public void Shake(float amount)
        {
            shake = Mathf.Max(shake, amount);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 move = Vector3.zero;
            if (InputCompat.Held(GameKey.PanUp)) move += fwd;
            if (InputCompat.Held(GameKey.PanDown)) move -= fwd;
            if (InputCompat.Held(GameKey.PanRight)) move += right;
            if (InputCompat.Held(GameKey.PanLeft)) move -= right;

            focus += move * panSpeed * dt * (distance / 32f);
            focus.x = Mathf.Clamp(focus.x, -focusBounds.x, focusBounds.x);
            focus.z = Mathf.Clamp(focus.z, -focusBounds.y, focusBounds.y);

            bool overHud = HUD.Instance != null && HUD.Instance.IsPointerOverHUD(InputCompat.MousePosition);
            if (!overHud) distance = Mathf.Clamp(distance - InputCompat.Scroll * zoomStep, minDistance, maxDistance);

            Vector3 pos = focus + backDir * distance;
            if (shake > 0f)
            {
                pos += Random.insideUnitSphere * shake;
                shake = Mathf.MoveTowards(shake, 0f, dt * 2.5f);
            }
            transform.position = pos;
        }
    }
}
