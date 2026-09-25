using UnityEngine;

namespace TDAnnihilation
{
    public sealed class GreenwardAmbientMotion : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward;
        public float speed = 22f;
        public float sway = 0f;
        private Quaternion origin;
        private void Awake() => origin = transform.localRotation;
        private void Update()
        {
            if (sway > 0f) transform.localRotation = origin * Quaternion.AngleAxis(Mathf.Sin(Time.time * speed * Mathf.Deg2Rad) * sway, axis);
            else transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
        }
    }
}
