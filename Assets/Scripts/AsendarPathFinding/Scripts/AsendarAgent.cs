using System;
using System.Collections;
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

		// stuck detection
		private float _stuckThreshold = 0.1f;
		private float _stuckTimer = 0f;
		private Vector3 _lastPosition;

		// Coroutine Fields
		private Coroutine _movementCoroutine;
		private float _movementUpdateInterval = 0.1f;
		private WaitForSeconds _waitForSeconds;

		void Start()
		{
			// init _waitForSeconds to avoid GC allocations
			_waitForSeconds = new WaitForSeconds(_movementUpdateInterval);
			_movementCoroutine = null;

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
		// ! in state of moving the system to a coroutine. Will delete after testing
		// void Update()
		// {
		// 	if (!hasDestination) return;
		// 	_stuckTimer += Time.deltaTime;
		// 	if (_stuckTimer > _stuckThreshold && Vector3.Distance(_lastPosition, transform.position) < 0.01f)
		// 		amIstuck();

		// 	Vector3 direction = (_dest - transform.position).normalized;
		// 	float distance = Vector3.Distance(transform.position, _dest);

		// 	// ? for now check if the nearby unit has reached its destination
		// 	Collider[] nearbyUnits = Physics.OverlapSphere(_dest, 2f, unitAvoidanceLayerMask);
		// 	if (nearbyUnits.Length > 0)
		// 	{
		// 		foreach (Collider unit in nearbyUnits)
		// 		{
		// 			if (unit.gameObject == this.gameObject) continue;
		// 			AsendarAgent agent = unit.GetComponent<AsendarAgent>();
		// 			if (agent != null && agent.IsMoving() && agent._dest == _dest)
		// 			{
		// 				if (Vector3.Distance(unit.transform.position, agent._dest) < 2f)
		// 				{
		// 					hasDestination = false;
		// 					return;
		// 				}
		// 			}
		// 		}
		// 	}

		// 	if (distance < 1f)
		// 	{
		// 		hasDestination = false;
		// 		return;
		// 	}

		// 	direction = betterAvoidance(direction);

		// 	if (movementUnitTypes == MovementUnitTypes.FootUnit)
		// 	{
		// 		transform.rotation = Quaternion.LookRotation(direction);
		// 		transform.position += direction * movementSpeed * Time.deltaTime;
		// 		transform.position = new Vector3(transform.position.x, 0f, transform.position.z); // Keep the unit on the ground
		// 	}
		// 	else
		// 	{
		// 		// For vehicles, we need to handle rotation and movement separately
		// 		Quaternion targetRotation = Quaternion.LookRotation(direction);
		// 		transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

		// 		float alignment = Vector3.Dot(transform.forward, direction);
		// 		if (alignment > .99f)
		// 		{
		// 			transform.position += direction * movementSpeed * Time.deltaTime;
		// 		}
		// 	}
		// }

		private IEnumerator movementCoroutine()
		{
			while (hasDestination)
			{
				updateStuckState();
				Vector3 direction = (_dest - transform.position).normalized;
				float distance = Vector3.Distance(transform.position, _dest);
				// for now since i still don't have a free waypoint system, i will use the info from other units
				checkIfOtherReachedDest();
				if (distance < 1f)
				{
					hasDestination = false;
					yield break; // Stop the coroutine when destination is reached
				}
				direction = betterAvoidance(direction);
				if (direction == Vector3.zero)
				{
					// I plan into making a smarter unitMovement system later
					// so for now there is no system to handle such cases. Just stop the unit
					Stop();
				}
				applyMovementByType(direction);
				yield return null;
			}
		}

		#region Stuck Detection (In Beta - Need testing)
		private void updateStuckState()
		{
			if (_stuckTimer > _stuckThreshold && Vector3.Distance(_lastPosition, transform.position) < 0.01f)
			{
				amIstuck();
			}
			else
			{
				_stuckTimer = 0f;
				_lastPosition = transform.position;
			}
		}

		private void amIstuck()
		{
			Vector3 escapeDirection = Vector3.zero;
			int blockedUnits = 0;

			Collider[] colliders = Physics.OverlapSphere(transform.position, unitAvoidanceDistance, unitAvoidanceLayerMask);
			foreach (Collider collider in colliders)
			{
				if (collider.gameObject == this.gameObject) continue;

				// Push away from ALL nearby units, not toward their destinations
				Vector3 pushAway = transform.position - collider.transform.position;
				escapeDirection += pushAway.normalized;
				blockedUnits++;
			}

			if (blockedUnits > 0)
			{
				escapeDirection = escapeDirection.normalized;
				Vector3 escapePosition = transform.position + escapeDirection * 3f;

				// Make sure escape position is valid
				if (!Physics.CheckSphere(escapePosition, 0.8f, unitAndObstacleLayerMask))
				{
					SetDestination(escapePosition);
				}
			}

			_stuckTimer = 0f;
			_lastPosition = transform.position;
		}
		#endregion

		private void checkIfOtherReachedDest()
		{
			Collider[] nearbyUnits = Physics.OverlapSphere(_dest, 2f, unitAvoidanceLayerMask);
			if (nearbyUnits.Length > 0)
			{
				foreach (Collider unit in nearbyUnits)
				{
					if (unit.gameObject == this.gameObject) continue;
					AsendarAgent agent = unit.GetComponent<AsendarAgent>();
					if (agent != null && agent.IsMoving() && agent._dest == _dest)
					{
						if (Vector3.Distance(unit.transform.position, agent._dest) < 2f)
						{
							hasDestination = false;
							return;
						}
					}
				}
			}
		}

		#region Avoidance Logic (In Beta - Units tend to suck in walls wide enough)
		private Vector3 betterAvoidance(Vector3 direction)
		{
			Vector3 avoidanceForce = Vector3.zero;

			Collider[] nearbyUnits = Physics.OverlapSphere(transform.position, unitAvoidanceDistance, unitAvoidanceLayerMask);
			foreach (Collider unit in nearbyUnits)
			{
				if (unit.gameObject == this.gameObject) continue;

				Vector3 repulsion = transform.position - unit.transform.position;
				float distance = repulsion.magnitude;

				if (distance < unitAvoidanceDistance && distance > 0)
				{
					float strength = Mathf.Clamp01((unitAvoidanceDistance - distance) / unitAvoidanceDistance);
					avoidanceForce += repulsion.normalized * strength;
				}
			}

			Vector3 finalDirection = direction + avoidanceForce;
			finalDirection = basicAvoidance(finalDirection);

			return finalDirection.normalized;
		}

		private Vector3 basicAvoidance(Vector3 direction)
		{
			if (!Physics.Raycast(transform.position, direction, avoidanceDistance, avoidanceLayerMask))
				return direction; // Clear path

			// Try alternative directions
			Vector3[] alternatives = {
				Quaternion.Euler(0, 45, 0) * direction,    // Right 45°
                Quaternion.Euler(0, -45, 0) * direction,   // Left 45°
                Quaternion.Euler(0, 90, 0) * direction,    // Right 90°
                Quaternion.Euler(0, -90, 0) * direction    // Left 90°
            };

			foreach (Vector3 alt in alternatives)
			{
				if (!Physics.Raycast(transform.position, alt, avoidanceDistance, avoidanceLayerMask))
				{
					return alt;
				}
			}
			return Vector3.zero; // No valid direction
		}
		#endregion

		private void applyMovementByType(Vector3 direction)
		{
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

		public void SetDestination(Vector3 dest)
		{
			_dest = dest;
			_originalDest = dest;
			hasDestination = true;

			if (_movementCoroutine != null)
			{
				StopCoroutine(_movementCoroutine);
			}
			_movementCoroutine = StartCoroutine(movementCoroutine());
		}
		public void Stop()
		{
			if (_movementCoroutine != null)
			{
				StopCoroutine(_movementCoroutine);
				_movementCoroutine = null;
			}
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
