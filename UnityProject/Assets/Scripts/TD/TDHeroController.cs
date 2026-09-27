using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace TDAnnihilation
{
    public enum TDAttackType
    {
        Light,
        Heavy,
        Mega
    }

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
        [SerializeField] private TDHeroEquipment equipment = TDHeroEquipment.Unarmed;
        private const float HeavyCooldown = 3.2f;
        private const float MegaCooldown = 8.4f;
        private Animator animator;
        private CharacterController controller;
        private float verticalVelocity;
        private float lightAttackTimer;
        private float heavyAttackTimer;
        private float megaAttackTimer;
        private TDSkillModifiers skillModifiers = new TDSkillModifiers();
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
            if (Time.timeScale == 0f) return;
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
            lightAttackTimer = Mathf.Max(0f, lightAttackTimer - deltaTime);
            heavyAttackTimer = Mathf.Max(0f, heavyAttackTimer - deltaTime);
            megaAttackTimer = Mathf.Max(0f, megaAttackTimer - deltaTime);
        }

        public void Attack() => TryAttack(TDAttackType.Light);

        public TDHeroEquipment Equipment => equipment;

        public void Equip(TDHeroEquipment newEquipment)
        {
            if (!System.Enum.IsDefined(typeof(TDHeroEquipment), newEquipment))
                throw new System.ArgumentOutOfRangeException(nameof(newEquipment));
            equipment = newEquipment;
        }

        public float EffectiveAttackDamage => attackDamage * (1f + skillModifiers.HeroDamageBonus);
        public float EffectiveAttackRange => attackRange * (1f + skillModifiers.HeroRangeBonus);
        public float EffectiveMegaRadius => 7.5f * (1f + skillModifiers.MegaRadiusBonus);
        public float EffectiveHeavyDamageMultiplier => 2.2f + skillModifiers.HeavyDamageBonus;

        public void ApplySkillModifiers(TDSkillModifiers modifiers)
        {
            skillModifiers = modifiers ?? new TDSkillModifiers();
        }

        public float AttackCooldownRemaining(TDAttackType type)
        {
            switch (type)
            {
                case TDAttackType.Heavy: return heavyAttackTimer;
                case TDAttackType.Mega: return megaAttackTimer;
                default: return lightAttackTimer;
            }
        }

        public float AttackCooldownDuration(TDAttackType type)
        {
            switch (type)
            {
                case TDAttackType.Heavy: return EffectiveCooldown(HeavyCooldown);
                case TDAttackType.Mega: return EffectiveCooldown(MegaCooldown);
                default: return EffectiveCooldown(attackCooldown);
            }
        }

        private float EffectiveCooldown(float baseline) => Mathf.Max(0.01f, baseline * (1f - skillModifiers.HeroCooldownReduction));

        public bool TryAttack(TDAttackType type)
        {
            if (AttackCooldownRemaining(type) > 0f) return false;
            switch (type)
            {
                case TDAttackType.Heavy: heavyAttackTimer = EffectiveCooldown(HeavyCooldown); break;
                case TDAttackType.Mega: megaAttackTimer = EffectiveCooldown(MegaCooldown); break;
                default: lightAttackTimer = EffectiveCooldown(attackCooldown); break;
            }
            PlayAttackAnimation();
            TDEnemyController[] enemies = FindObjectsByType<TDEnemyController>();
            foreach (TDEnemyController enemy in enemies)
            {
                if (enemy == null) continue;
                Vector3 direction = enemy.transform.position - transform.position;
                direction.y = 0f;
                if (type == TDAttackType.Mega)
                {
                    if (direction.sqrMagnitude <= EffectiveMegaRadius * EffectiveMegaRadius) enemy.TakeDamage(CalculateDamage(2.5f));
                    continue;
                }
                float range = type == TDAttackType.Heavy ? EffectiveAttackRange * 1.3f : EffectiveAttackRange;
                float arc = type == TDAttackType.Heavy ? 150f : attackArc;
                if (direction.sqrMagnitude > range * range) continue;
                if (Vector3.Angle(transform.forward, direction) <= arc * 0.5f)
                    enemy.TakeDamage(CalculateDamage(type == TDAttackType.Heavy ? EffectiveHeavyDamageMultiplier : 1f));
            }
            return true;
        }

        private float CalculateDamage(float multiplier)
        {
            float damage = EffectiveAttackDamage * multiplier;
            return Random.value < skillModifiers.HeroCriticalChance ? damage * 2f : damage;
        }

        private void PlayAttackAnimation()
        {
            if (animator == null) return;
            string state = equipment == TDHeroEquipment.Melee ? "MeleeAttack" :
                equipment == TDHeroEquipment.Staff ? "StaffAttack" : "UnarmedAttack";
            int stateHash = Animator.StringToHash("Base Layer." + state);
            if (animator.HasState(0, stateHash))
                animator.CrossFadeInFixedTime(stateHash, 0.06f, 0, 0f);
            else if (hasAttackTrigger)
                animator.SetTrigger("Attack");
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
