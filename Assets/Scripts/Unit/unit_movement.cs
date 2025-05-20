using UnityEngine;
using UnityEngine.AI;

public class unit_movement : MonoBehaviour
{
    private NavMeshAgent agent;
    private unit_main unit_main;
    void Start()
    {
        unit_main = GetComponent<unit_main>();
        agent = GetComponent<NavMeshAgent>();
        if (!agent)
        {
            Debug.LogError("NavMeshAgent component not found on this GameObject.");
            return;
        }
        agent.radius *= 0.8f;
        agent.avoidancePriority = Random.Range(1, 100);
    }
    public void MoveTo(Vector3 destination)
    {
        if (agent.isStopped)
        {
            agent.isStopped = false;
        }
        if (!agent)
        {
            Debug.LogError("NavMeshAgent component not found on this GameObject.");
            return;
        }
        agent.SetDestination(destination);
    }
    public void stop_unit()
    {
        agent.isStopped = true;
    }
    public int approach_unit_within_range(Vector3 target_position)
    {
        float distance = Vector3.Distance(transform.position, target_position);
        if (distance > unit_main.unit_range)
        {
            agent.isStopped = false;
            agent.SetDestination(target_position);
            return 1; // Approaching
        }
        else
        {
            agent.isStopped = true;
            return 0; // In range
        }
    }
}
