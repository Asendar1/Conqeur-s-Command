using System;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.VisualScripting;
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
		public LayerMask avoidanceLayerMask = -1;

		[Header("Unit Avoidance Settings")]
		public float unitAvoidanceDistance = 1.5f;
		public LayerMask unitAvoidanceLayerMask = -1;

		private LayerMask unitAndObstacleLayerMask = -1;
		// agent state
		public bool hasDestination = false;
		private Vector3 _dest;
		private Vector3 _velocity;

		private Vector3 _originalDest;
		private float _lastCollionCheck = 0f;
		private float _checkInterval = 0.005f;

		void Start()
		{
			avoidanceLayerMask = LayerMask.GetMask("Obstacle", "Building");
			unitAvoidanceLayerMask = LayerMask.GetMask("Unit");
			unitAndObstacleLayerMask = LayerMask.GetMask("Unit", "Obstacle", "Building");
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

			// Is there a unit at _dest? if yes then get a new _dest near it
			if (Time.time - _lastCollionCheck > _checkInterval)
			{
				_lastCollionCheck = Time.time;
				Collider[] hitColliders = Physics.OverlapSphere(_dest, 1f, unitAvoidanceLayerMask);
				if (hitColliders.Length > 0)
				{
					if (hitColliders[0].gameObject != this.gameObject)
					{
						_dest = findNearestFreePosition(_originalDest, -2f, 2f);
					}
					// ! Very important note. This is a temporary solution due to this being rendered every frame,
					// ! cuz the check is done every frame the unit for certain will know when to get another _dest.
					// ! idk if later in optimization if the check will be as fast or not.
				}
			}
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
				transform.position = new Vector3(transform.position.x, 0f, transform.position.z); // Keep the unit on the ground
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

		private Vector3 findNearestFreePosition(Vector3 dest, float v1, float v2)
		{
			float random = UnityEngine.Random.Range(0, 360);
			for (float radius = v1; radius <= v2; radius += 0.5f)
			{
				for (int angle = 0; angle < 360; angle += 30)
				{
					Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
					Vector3 newPosition = dest + offset;

					if (!Physics.CheckSphere(newPosition, unitAvoidanceDistance, unitAndObstacleLayerMask))
					{
						return newPosition; // Found a free position
					}
				}
			}
			return dest; // No free position found, return original destination
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
			_originalDest = dest;
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

		void OnDrawGizmos()
		{
			if (hasDestination)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawLine(transform.position, _dest);
				Gizmos.DrawWireSphere(_dest, 0.5f);
			}

			// Draw avoidance sphere
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, avoidanceDistance);

			// Draw unit avoidance sphere
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(transform.position, unitAvoidanceDistance);
		}
	}
}
