using UnityEngine;

namespace MidnightHour
{
    /// <summary>Spins and bobs an object (spawn portal, decorations).</summary>
    public class Spin : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0f, 90f, 0f);
        public float bobHeight = 0f;
        public float bobSpeed = 2f;
        Vector3 basePos;

        void Start() { basePos = transform.localPosition; }

        void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
            if (bobHeight > 0f)
                transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        }
    }
}
