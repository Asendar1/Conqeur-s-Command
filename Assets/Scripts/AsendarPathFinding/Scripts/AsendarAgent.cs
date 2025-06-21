using UnityEngine;

namespace AsendarPathFinding
{
    public class AsendarAgent : MonoBehaviour
	{
		[Header("Movement Type")]
		public MovementUnitTypes movementUnitTypes = MovementUnitTypes.None;
		[Header("Movement Speed (Auto-configured)")]
		public float movementSpeed = 5f;
		public float turnSpeed = 180f;

		// agent state
		public bool hasDestination = false;
		private Vector3 _dest;
		private Vector3 _velocity;

		void Start()
		{
			// Set movement speed based on unit type
			switch (movementUnitTypes)
			{
				case MovementUnitTypes.FootUnit:
					movementSpeed = 3f;
					turnSpeed = 999f;
					break;
				case MovementUnitTypes.CarUnit:
					movementSpeed = 12f;
					turnSpeed = 120f;
					break;
				case MovementUnitTypes.HeavyVehicleUnit:
					movementSpeed = 8f;
					turnSpeed = 60f;
					break;
			}
		}
		// i'll use update for now but later it will optimized
		void Update()
		{
			if (!hasDestination)
			{
				return;
			}

			Vector3 direction = (_dest - transform.position).normalized;
			float distance = Vector3.Distance(transform.position, _dest);
			if (distance < 0.2f)
			{
				hasDestination = false;
				return;
			}

			if (movementUnitTypes == MovementUnitTypes.FootUnit)
			{
				transform.rotation = Quaternion.LookRotation(direction);
				transform.position += direction * movementSpeed * Time.deltaTime;
			}
			else
			{
				// For vehicles, we need to handle rotation and movement separately
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

				float alignment = Vector3.Dot(transform.forward, direction);
				if (alignment > .99f)
				{
					transform.position += direction * movementSpeed * Time.deltaTime;
				}
			}
		}

		public void SetDestination(Vector3 dest)
		{
			_dest = dest;
			hasDestination = true;
		}
		public void Stop()
		{
			hasDestination = false;
		}
		public bool IsMoving()
		{
			return hasDestination;
		}
	}
}
