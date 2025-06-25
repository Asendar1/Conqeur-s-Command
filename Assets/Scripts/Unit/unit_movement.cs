using UnityEngine;
using AsendarPathFinding;

public class unit_movement : MonoBehaviour
{
    private flowFieldAgent agent;

    void Start()
    {
        agent = GetComponent<flowFieldAgent>();
        if (agent == null)
        {
            Debug.LogError("flowFieldAgent component not found on the unit.");
        }
    }

    public void MoveTo(Vector3 destination)
    {
        // if (agent != null)
        // {
        //     agent.setTarget(destination);
        // }
        return;
    }

    public void StopMovement()
    {
        // if (agent != null)
        // {
        //     agent.Stop();
        // }
        return;
    }
}
