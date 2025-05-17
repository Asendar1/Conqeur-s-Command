using UnityEngine;
using UnityEngine.AI;

public class unit_movement : MonoBehaviour
{
    private NavMeshAgent agent;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent)
        {
            Debug.LogError("NavMeshAgent component not found on this GameObject.");
            return;
        }
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
    public int approach_unit_within_range(unit_main target_unit)
    {
        float distance = Vector3.Distance(transform.position, target_unit.transform.position);
        if (distance > target_unit.unit_range)
        {
            agent.SetDestination(target_unit.transform.position);
            return 1; // Approaching
        }
        else
        {
            agent.isStopped = true;
            return 0; // In range
        }
    }
}
