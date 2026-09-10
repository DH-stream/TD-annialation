using UnityEngine;
using UnityEngine.InputSystem;

namespace TDAnnihilation
{
    public sealed class TDStrategicCamera : MonoBehaviour
    {
        private Transform target;
        [SerializeField] private float distance = 12f;
        [SerializeField] private float height = 5.2f;
        [SerializeField] private float followSharpness = 8f;
        [SerializeField] private float orbitSharpness = 12f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = 12f;
        [SerializeField] private float maxPitch = 55f;
        private float yaw = 35f;
        private float pitch = 24f;
        private float targetDistance;
        private Vector3 followVelocity;
        private void Awake()
        {
            Camera cameraComponent = GetComponent<Camera>();
            cameraComponent.allowHDR = true;
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(0.38f, 0.55f, 0.68f);
            cameraComponent.fieldOfView = 62f;
            cameraComponent.nearClipPlane = 0.3f;
            var cameraData = GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (cameraData != null) cameraData.renderPostProcessing = false;
        }
        public void SetTarget(Transform value)
        {
            target = value;
            targetDistance = distance;
        }
        public static Vector3 CalculatePan(Vector2 input, float speed, float deltaTime)
        {
            return new Vector3(input.x, 0f, input.y) * (speed * deltaTime);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, minPitch, maxPitch);
            }
            if (mouse != null) targetDistance = Mathf.Clamp(targetDistance - mouse.scroll.ReadValue().y * 0.01f, 8f, 16f);
            if (targetDistance <= 0f) targetDistance = distance;
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + Vector3.up * 1.1f;
            Vector3 desiredPosition = focus + orbit * new Vector3(0f, height * 0.15f, -targetDistance);
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, -52f, 52f);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, -38f, 38f);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, 1f / followSharpness);
            Quaternion desiredRotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * orbitSharpness);
        }
    }
}
