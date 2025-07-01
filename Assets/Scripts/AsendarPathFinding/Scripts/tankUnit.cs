using AsendarPathFinding;
using UnityEngine;
using System.Collections.Generic;

namespace AsendarPathFinding
{

	public class tankUnit : AsendarAgent
	{
		// the only sole purpose of this right now is to not make move for other footUnits

		protected override Vector3 betterAvoidance(Vector3 direction)
		{
			Vector3 avoidanceForce = Vector3.zero;

			Collider[] colliders = Physics.OverlapSphere(transform.position, 1.5f, unitAvoidanceLayerMask);
			foreach (Collider collider in colliders)
			{
				if (collider.gameObject == this.gameObject) continue;

				AsendarAgent agent = collider.GetComponent<AsendarAgent>();
				if (agent != null && agent.movementUnitTypes != MovementUnitTypes.HeavyVehicleUnit)
				{
					// TODO complete the funcion and make another one that wait for the footUnits to moves. iam tired now
				}
			}
		}
	}
}
