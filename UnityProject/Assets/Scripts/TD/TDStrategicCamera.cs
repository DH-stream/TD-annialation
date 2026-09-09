using UnityEngine;
using UnityEngine.InputSystem;

namespace TDAnnihilation
{
    public sealed class TDStrategicCamera : MonoBehaviour
    {
        private Transform target;
        private Vector3 offset = new Vector3(-7f, 14.5f, -19f);
        private Vector3 panOffset;
        [SerializeField] private float panSpeed = 18f;
        private void Awake()
        {
            Camera cameraComponent = GetComponent<Camera>();
            cameraComponent.allowHDR = true;
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(0.38f, 0.55f, 0.68f);
            cameraComponent.fieldOfView = 46f;
            cameraComponent.nearClipPlane = 0.3f;
            var cameraData = GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (cameraData != null) cameraData.renderPostProcessing = false;
        }
        public void SetTarget(Transform value) => target = value;
        public static Vector3 CalculatePan(Vector2 input, float speed, float deltaTime)
        {
            return new Vector3(input.x, 0f, input.y) * (speed * deltaTime);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Keyboard keyboard = Keyboard.current;
            Vector2 input = keyboard == null ? Vector2.zero : new Vector2(
                (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f));
            if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
                panOffset += CalculatePan(input.normalized, panSpeed, Time.deltaTime);
            else if (input.sqrMagnitude < 0.01f)
                panOffset = Vector3.Lerp(panOffset, Vector3.zero, Time.deltaTime * 1.2f);
            panOffset.x = Mathf.Clamp(panOffset.x, -22f, 22f);
            panOffset.z = Mathf.Clamp(panOffset.z, -15f, 15f);
            Vector3 focus = target.position + new Vector3(11f, 0.8f, 1f) + panOffset;
            focus.x = Mathf.Clamp(focus.x, -18f, 31f);
            focus.z = Mathf.Clamp(focus.z, -10f, 11f);
            transform.position = Vector3.Lerp(transform.position, focus + offset, Time.deltaTime * 2.2f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(focus - transform.position), Time.deltaTime * 3f);
        }
    }
}
