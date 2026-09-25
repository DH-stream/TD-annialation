using UnityEngine;
using UnityEngine.AI;

public class VillagerRunToHouse : MonoBehaviour
{
    [Header("Destination Settings")]
    public Transform houseTarget;
    public float moveSpeed = 4.5f;
    public float stopDistance = 1.2f;

    [Header("Components")]
    public Animator animator;
    public NavMeshAgent navAgent;

    [Header("State")]
    public bool isRunning = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (houseTarget == null)
        {
            GameObject house = GameObject.Find("HouseTarget");
            if (house == null) house = GameObject.Find("House");
            if (house == null) house = GameObject.Find("Huset");
            if (house != null) houseTarget = house.transform;
        }
    }

    private void OnEnable()
    {
        WaveManager.OnWaveStart += OnWaveStart;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveStart -= OnWaveStart;
    }

    private void Update()
    {
        if (!isRunning) return;

        if (houseTarget == null) return;

        float distance = Vector3.Distance(transform.position, houseTarget.position);

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(houseTarget.position);
            navAgent.speed = moveSpeed;

            if (animator != null)
            {
                animator.SetFloat("Speed", navAgent.velocity.magnitude);
                animator.SetBool("IsRunning", true);
            }

            if (distance <= stopDistance || (navAgent.remainingDistance <= stopDistance && !navAgent.pathPending))
            {
                OnReachedHouse();
            }
        }
        else
        {
            // Direkt förflyttning om NavMesh inte är bakad
            Vector3 direction = (houseTarget.position - transform.position);
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }

            transform.position = Vector3.MoveTowards(transform.position, houseTarget.position, moveSpeed * Time.deltaTime);

            if (animator != null)
            {
                animator.SetFloat("Speed", moveSpeed);
                animator.SetBool("IsRunning", true);
            }

            if (distance <= stopDistance)
            {
                OnReachedHouse();
            }
        }
    }

    [ContextMenu("Trigger Wave Start (Run to House)")]
    public void OnWaveStart()
    {
        isRunning = true;
        if (animator != null)
        {
            animator.SetBool("IsRunning", true);
            foreach (var param in animator.parameters)
            {
                if (param.name == "Run" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger("Run");
                }
            }
        }
    }

    private void OnReachedHouse()
    {
        isRunning = false;
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsRunning", false);
        }

        gameObject.SetActive(false);
        Debug.Log($"{gameObject.name} sprang in i huset och gömde sig!");
    }
}
