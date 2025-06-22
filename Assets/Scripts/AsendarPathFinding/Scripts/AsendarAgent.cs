using System;
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

		[Header("Avoidance Settings")]
		public float avoidanceDistance = 3f;
		public LayerMask avoidanceLayerMask = -1; // assign at runtime
		// agent state
		public bool hasDestination = false;
		private Vector3 _dest;
		private Vector3 _velocity;

		void Start()
		{
			avoidanceLayerMask = LayerMask.GetMask("Obstacle", "Building");
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

			direction = basicAvoidance(direction);

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

		private Vector3 basicAvoidance(Vector3 direction)
		{
			if (!Physics.Raycast(transform.position, direction, avoidanceDistance, avoidanceLayerMask))
			{
				return direction; // no obstacles in the way
			}

			Vector3 lookRight = Quaternion.Euler(0, 45, 0) * direction;
			if (!Physics.Raycast(transform.position, lookRight, avoidanceDistance, avoidanceLayerMask))
			{
				return lookRight; // can move right
			}

			Vector3 sharpRight = Quaternion.Euler(0, 90, 0) * direction;
			if (!Physics.Raycast(transform.position, sharpRight, avoidanceDistance, avoidanceLayerMask))
			{
				return sharpRight;
			}

			Vector3 lookLeft = Quaternion.Euler(0, -45, 0) * direction;
			if (!Physics.Raycast(transform.position, lookLeft, avoidanceDistance, avoidanceLayerMask))
			{
				return lookLeft;
			}

			Vector3 sharpLeft = Quaternion.Euler(0, -90, 0) * direction;
			if (!Physics.Raycast(transform.position, sharpLeft, avoidanceDistance, avoidanceLayerMask))
			{
				return sharpLeft;
			}
			return Vector3.zero; // no valid direction found
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
