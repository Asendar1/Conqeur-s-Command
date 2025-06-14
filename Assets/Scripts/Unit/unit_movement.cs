using UnityEngine;
using UnityEngine.AI;

public class unit_movement : MonoBehaviour
{
    [Header("Unit Settings")]
    [SerializeField] private UnitType unit_type = UnitType.Infantry;

    private NavMeshAgent agent;
    private bool is_moving = false;

    public enum UnitType
    {
        Infantry,
        Humvee,
        Tank,
        Aircraft
    }

    void Start()
    {
        SetupPureNavMesh();
    }

    private void SetupPureNavMesh()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        agent.updatePosition = true;
        agent.updateRotation = true;

        switch (unit_type)
        {
            case UnitType.Infantry:
                agent.speed = 3.5f;
                agent.acceleration = 12f; // Faster response
                agent.angularSpeed = 360f;
                agent.stoppingDistance = 0.3f; // Tighter stopping
                agent.radius = 0.4f; // Smaller personal space
                agent.avoidancePriority = Random.Range(40, 60); // Similar priorities
                break;

            case UnitType.Humvee:
                agent.speed = 12f;
                agent.acceleration = 10f;
                agent.angularSpeed = 200f; // Faster turning
                agent.stoppingDistance = 1f;
                agent.radius = 1f;
                agent.avoidancePriority = Random.Range(30, 50);
                break;

            case UnitType.Tank:
                agent.speed = 6f;
                agent.acceleration = 6f; // Faster than before
                agent.angularSpeed = 90f; // Still slow but not crawling
                agent.stoppingDistance = 1.5f;
                agent.radius = 1.5f;
                agent.avoidancePriority = Random.Range(20, 40);
                break;
        }

        agent.autoBraking = true;

        // Key for Generals feel: Less obstacle avoidance complexity
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
    }

    void Update()
    {
        // Simple check - let NavMesh do the work
        if (agent != null && is_moving)
        {
            if (!agent.hasPath || agent.remainingDistance < 0.1f)
            {
                is_moving = false;
            }
        }
    }

    // Public API
    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.enabled)
        {
            if (agent.SetDestination(destination))
            {
                is_moving = true;
            }
        }
    }

    public void stop_unit()
    {
        if (agent != null)
        {
            agent.ResetPath();
            is_moving = false;
        }
    }

    public bool IsMoving()
    {
        return is_moving && agent != null && agent.hasPath;
    }

    public int approach_unit_within_range(Vector3 target_position)
    {
        unit_main unit_main_component = GetComponent<unit_main>();
        if (unit_main_component == null)
        {
            Debug.LogWarning($"Unit {gameObject.name} missing unit_main component!");
            return 0;
        }

        float distance = Vector3.Distance(transform.position, target_position);

        if (distance > unit_main_component.unit_range)
        {
            Vector3 direction = (target_position - transform.position).normalized;
            Vector3 range_position = target_position - direction * unit_main_component.unit_range;

            MoveTo(range_position);
            return 1; // Approaching target
        }
        else
        {
            stop_unit();
            return 0; // Within range, ready to engage
        }
    }
}
