using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

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
            Debug.Log("TDHeroController initializing on: " + gameObject.name);
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator != null) Debug.Log("Found animator in child: " + animator.gameObject.name);
            }

            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<CharacterController>();
                Debug.Log("Added CharacterController to hero");
            }

            controller.radius = 0.38f;
            controller.height = 1.65f;
            controller.center = new Vector3(0f, 0.82f, 0f);
            controller.stepOffset = 0.35f;
            InitializeAnimatorSetup();
            hasSpeedParameter = HasParameter("Speed", AnimatorControllerParameterType.Float);
            hasGroundedParameter = HasParameter("Grounded", AnimatorControllerParameterType.Bool);
            hasJumpTrigger = HasParameter("Jump", AnimatorControllerParameterType.Trigger);
            hasAttackTrigger = HasParameter("Attack", AnimatorControllerParameterType.Trigger);
            Debug.Log("TDHeroController initialized. Animator: " + (animator != null ? "present" : "MISSING"));
        }

        private void InitializeAnimatorSetup()
        {
            if (animator == null)
            {
                return;
            }
            if (animator.runtimeAnimatorController != null) return;


#if UNITY_EDITOR
            string controllerPath = "Assets/Animations/PlayerAnimatorController.controller";
            AnimatorController controllerAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            // If the asset is missing, create it with basic parameters
            if (controllerAsset == null)
            {
                controllerAsset = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                Debug.Log("Created new PlayerAnimatorController at: " + controllerPath);
            }

            EnsureControllerParameters(controllerAsset);

            if (animator.runtimeAnimatorController != controllerAsset)
            {
                animator.runtimeAnimatorController = controllerAsset;
                Debug.Log("Assigned PlayerAnimatorController to animator");
            }
#else
            // At runtime, try to load from Resources
            if (animator.runtimeAnimatorController == null)
            {
                RuntimeAnimatorController rtController = Resources.Load<RuntimeAnimatorController>("Animations/PlayerAnimatorController");
                if (rtController != null)
                {
                    animator.runtimeAnimatorController = rtController;
                    Debug.Log("Loaded PlayerAnimatorController from Resources at runtime");
                }
                else if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning("No animator controller found! Hero movement may not animate.");
                }
            }
#endif
        }

        private void EnsureControllerParameters(AnimatorController controllerAsset)
        {
            if (controllerAsset == null)
            {
                return;
            }

            if (System.Array.Exists(controllerAsset.parameters, p => p.name == "Speed" && p.type == AnimatorControllerParameterType.Float) == false)
            {
                controllerAsset.AddParameter("Speed", AnimatorControllerParameterType.Float);
            }

            if (System.Array.Exists(controllerAsset.parameters, p => p.name == "Grounded" && p.type == AnimatorControllerParameterType.Bool) == false)
            {
                controllerAsset.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            }

            if (System.Array.Exists(controllerAsset.parameters, p => p.name == "Jump" && p.type == AnimatorControllerParameterType.Trigger) == false)
            {
                controllerAsset.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            }

            if (System.Array.Exists(controllerAsset.parameters, p => p.name == "Attack" && p.type == AnimatorControllerParameterType.Trigger) == false)
            {
                controllerAsset.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            }
        }

        private void EnsureControllerStates(AnimatorController controllerAsset)
        {
#if UNITY_EDITOR
            if (controllerAsset == null) return;
            var layers = controllerAsset.layers;
            if (layers == null || layers.Length == 0 || layers[0] == null) return;
            AnimatorStateMachine root = layers[0].stateMachine;
            if (root == null) return;

            var statesArr = root.states;
            // Check if states already exist
            bool hasAllStates = statesArr != null && 
                System.Array.Exists(statesArr, s => s.state != null && s.state.name == "Idle") &&
                System.Array.Exists(statesArr, s => s.state != null && s.state.name == "Walk") &&
                System.Array.Exists(statesArr, s => s.state != null && s.state.name == "Run");
            if (hasAllStates) return; // States already set up

            // Note: Animation clips from FBX files need to be extracted as separate .anim assets.
            // For now, we'll keep this method for manual setup only.
            // Automatic clip loading is disabled to avoid legacy animation warnings.
#endif
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
            TDEnemyController[] enemies = FindObjectsByType<TDEnemyController>();
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
                if (parameter.name == name && parameter.type == type)
                {
                    return true;
                }
            return false;
        }

        // Runtime check - just verify parameters exist, don't try to add them
        private void ValidateAnimatorParameters()
        {
            if (animator == null)
            {
                Debug.LogWarning("TDHeroController: Animator is null, cannot validate parameters");
                return;
            }

            if (animator.parameters.Length == 0)
            {
                Debug.LogWarning("TDHeroController: Animator has no parameters. This is expected if controller is empty.");
            }

            // Just log what we find
            Debug.Log("TDHeroController: Animator has " + animator.parameters.Length + " parameters");
            foreach (var param in animator.parameters)
            {
                Debug.Log("  - " + param.name + " (" + param.type + ")");
            }
        }
    }
}
