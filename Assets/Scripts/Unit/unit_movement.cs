using UnityEngine;
using AsendarPathFinding;

public class unit_movement : MonoBehaviour
{
    private AsendarAgent agent;

    void Start()
    {
        agent = GetComponent<AsendarAgent>();
        if (agent == null)
        {
            Debug.LogError("AsendarAgent component not found on the unit.");
        }
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent != null)
        {
            agent.SetDestination(destination);
        }
    }

    public void StopMovement()
    {
        if (agent != null)
        {
            agent.Stop();
        }
    }
}
