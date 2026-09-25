using UnityEngine;

namespace TDAnnihilation
{
    public sealed class GreenwardVillagerController : MonoBehaviour
    {
        [SerializeField] private Vector3 workPosition;
        [SerializeField] private Vector3 shelterPosition;
        [SerializeField] private float returnSpeed = 0.85f;
        [SerializeField] private float fleeSpeed = 1.35f;

        private TDVerticalSliceBootstrap game;
        private Animator animator;
        private bool hasSpeed;
        private bool hasScared;
        private bool hasWorking;
        private bool waveState;

        public void Configure(Vector3 work, Vector3 shelter)
        {
            workPosition = work;
            shelterPosition = shelter;
            transform.position = work;
        }

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            hasSpeed = HasParameter("Speed", AnimatorControllerParameterType.Float);
            hasScared = HasParameter("Scared", AnimatorControllerParameterType.Bool);
            hasWorking = HasParameter("Working", AnimatorControllerParameterType.Bool);
        }

        private void Start()
        {
            game = FindAnyObjectByType<TDVerticalSliceBootstrap>();
            waveState = game != null && game.Phase == TDGamePhase.Wave;
        }

        private void Update()
        {
            if (game == null) game = FindAnyObjectByType<TDVerticalSliceBootstrap>();
            bool wave = game != null && game.Phase == TDGamePhase.Wave;
            if (wave != waveState)
            {
                waveState = wave;
                if (hasScared) animator.SetBool("Scared", wave);
            }
            Vector3 destination = wave ? shelterPosition : workPosition;
            Vector3 delta = destination - transform.position;
            delta.y = 0f;
            bool moving = delta.sqrMagnitude > 0.06f;
            if (moving)
            {
                float speed = wave ? fleeSpeed : returnSpeed;
                Vector3 next = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
                next.y = GreenwardWorldLayout.HeightAt(next.x, next.z) + 0.25f;
                transform.position = next;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 6f);
            }
            if (animator != null && hasSpeed) animator.SetFloat("Speed", moving ? (wave ? 1.2f : 0.6f) : 0f, 0.15f, Time.deltaTime);
            if (animator != null && hasWorking) animator.SetBool("Working", !wave && !moving);
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
