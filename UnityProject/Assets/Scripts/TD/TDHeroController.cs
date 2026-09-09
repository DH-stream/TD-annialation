using UnityEngine;
using UnityEngine.InputSystem;

namespace TDAnnihilation
{
    public sealed class TDHeroController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5.5f;
        private Animator animator;

        private void Awake() => animator = GetComponentInChildren<Animator>();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            Vector2 input = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) input = Vector2.zero;
            Vector3 movement = new Vector3(input.x, 0f, input.y).normalized;
            if (movement.sqrMagnitude > 0f)
            {
                Vector3 next = transform.position + movement * (moveSpeed * Time.deltaTime);
                next.x = Mathf.Clamp(next.x, -45f, 45f);
                next.z = Mathf.Clamp(next.z, -29f, 29f);
                next.y = GreenwardWorldLayout.HeightAt(next.x, next.z) + 0.25f;
                transform.position = next;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), Time.deltaTime * 12f);
            }
            if (animator != null) animator.speed = movement.sqrMagnitude > 0f ? 1f : 0.7f;
        }
    }
}
