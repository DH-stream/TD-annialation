using UnityEngine;
using UnityEngine.InputSystem;

namespace TDAnnihilation
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class TDHeroController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float sprintSpeed = 8.5f;
        [SerializeField] private float jumpHeight = 2.2f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float attackArc = 110f;
        [SerializeField] private float attackDamage = 24f;
        [SerializeField] private float attackCooldown = 0.55f;
        private Animator animator;
        private CharacterController controller;
        private float verticalVelocity;
        private float attackTimer;
        private bool grounded;
        private bool hasSpeedParameter;
        private bool hasGroundedParameter;
        private bool hasJumpTrigger;
        private bool hasAttackTrigger;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            controller = GetComponent<CharacterController>();
            controller.radius = 0.38f;
            controller.height = 1.65f;
            controller.center = new Vector3(0f, 0.82f, 0f);
            controller.stepOffset = 0.35f;
            hasSpeedParameter = HasParameter("Speed", AnimatorControllerParameterType.Float);
            hasGroundedParameter = HasParameter("Grounded", AnimatorControllerParameterType.Bool);
            hasJumpTrigger = HasParameter("Jump", AnimatorControllerParameterType.Trigger);
            hasAttackTrigger = HasParameter("Attack", AnimatorControllerParameterType.Trigger);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            Vector2 input = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            input = Vector2.ClampMagnitude(input, 1f);
            bool sprinting = (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) && input.sqrMagnitude > 0.01f;
            bool jumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            Vector3 movement = CameraRelativeMovement(input);
            float deltaTime = Time.deltaTime;
            float groundY = GreenwardWorldLayout.HeightAt(transform.position.x, transform.position.z) + 0.25f;
            grounded = transform.position.y <= groundY + 0.06f && verticalVelocity <= 0f;
            if (grounded)
            {
                verticalVelocity = -2f;
                if (jumpPressed)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    grounded = false;
                    if (hasJumpTrigger) animator.SetTrigger("Jump");
                }
            }
            else verticalVelocity += gravity * deltaTime;

            float speed = sprinting ? sprintSpeed : moveSpeed;
            controller.Move((movement * speed + Vector3.up * verticalVelocity) * deltaTime);
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, -45f, 45f);
            position.z = Mathf.Clamp(position.z, -29f, 29f);
            float targetGround = GreenwardWorldLayout.HeightAt(position.x, position.z) + 0.25f;
            if (position.y < targetGround && verticalVelocity <= 0f)
            {
                position.y = targetGround;
                verticalVelocity = -2f;
                grounded = true;
            }
            transform.position = position;
            if (movement.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), deltaTime * 12f);
            if (animator != null)
            {
                float animationSpeed = movement.sqrMagnitude < 0.001f ? 0f : sprinting ? 2f : 1f;
                if (hasSpeedParameter) animator.SetFloat("Speed", animationSpeed, 0.12f, deltaTime);
                if (hasGroundedParameter) animator.SetBool("Grounded", grounded);
            }
            attackTimer -= deltaTime;
            if (keyboard.fKey.wasPressedThisFrame) Attack();
        }

        public void Attack()
        {
            if (attackTimer > 0f) return;
            attackTimer = attackCooldown;
            if (animator != null && hasAttackTrigger) animator.SetTrigger("Attack");
            TDEnemyController[] enemies = FindObjectsByType<TDEnemyController>(FindObjectsSortMode.None);
            foreach (TDEnemyController enemy in enemies)
            {
                if (enemy == null) continue;
                Vector3 direction = enemy.transform.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > attackRange * attackRange) continue;
                if (Vector3.Angle(transform.forward, direction) <= attackArc * 0.5f) enemy.TakeDamage(attackDamage);
            }
        }

        private Vector3 CameraRelativeMovement(Vector2 input)
        {
            Camera camera = Camera.main;
            Vector3 forward = camera == null ? Vector3.forward : camera.transform.forward;
            Vector3 right = camera == null ? Vector3.right : camera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
        }

        private bool HasParameter(string name, AnimatorControllerParameterType type)
        {
            if (animator == null) return false;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.name == name && parameter.type == type) return true;
            return false;
        }
    }
}
