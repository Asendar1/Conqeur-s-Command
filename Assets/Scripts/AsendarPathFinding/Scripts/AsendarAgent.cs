using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AsendarPathFinding
{
	public class AsendarAgent : MonoBehaviour
	{
		#region Agent settings
		[Header("Movement Type")]
		public MovementUnitTypes movementUnitTypes = MovementUnitTypes.None;

		[Header("Hybrid Pathfinding")]
		private NavMeshPath navMeshPath;
		private List<Vector3> pathWaypoints = new List<Vector3>();
		private int currentWaypointIndex = 0;

		[Header("Movement Speed (Auto-configured)")]
		public float movementSpeed = 5f;
		public float turnSpeed = 180f;

		[Header("Avoidance Settings")]
		public float avoidanceDistance = 3f;
		public LayerMask avoidanceLayerMask = -1;

		[Header("Unit Avoidance Settings")]
		public float unitAvoidanceDistance = 2.5f;
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
		private int _frameSkip = 3;
		#endregion

		void Start()
		{
			// Initialize NavMeshPath
			navMeshPath = new NavMeshPath();

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
					turnSpeed = 300f;
					break;
			}
		}

		private IEnumerator movementCoroutine()
		{
			while (hasDestination)
			{
				// i don't think this works like it should do
				// updateStuckState();
				CheckWaypointProgress();

				Vector3 direction = (_dest - transform.position).normalized;
				float distance = Vector3.Distance(transform.position, _dest);

				checkIfOtherReachedDest();

				if (distance < 1f)
				{
					// Reached destination
					Stop();
					yield break;
				}

				direction = betterAvoidance(direction);

				if (direction == Vector3.zero)
				{
					Stop();
					yield break;
				}

				applyMovementByType(direction, _frameSkip);

				for (int i = 0; i < _frameSkip; i++)
				{
					yield return null;
				}
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

		private void applyMovementByType(Vector3 direction, int _frameskip)
		{
			if (movementUnitTypes == MovementUnitTypes.FootUnit)
			{
				transform.rotation = Quaternion.LookRotation(direction);
				transform.position += direction * movementSpeed * Time.deltaTime * _frameskip;
				transform.position = new Vector3(transform.position.x, 0f, transform.position.z); // Keep the unit on the ground
			}
			if (movementUnitTypes == MovementUnitTypes.HeavyVehicleUnit) // vehicles that need to turn inself to move
			{
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

				float alignment = Vector3.Dot(transform.forward, direction);
				if (alignment > .999f)
				{
					transform.position += direction * movementSpeed * Time.deltaTime;
				}
			}
		}

		public void SetDestination(Vector3 dest)
		{
			if (NavMesh.CalculatePath(transform.position, dest, NavMesh.AllAreas, navMeshPath))
			{
				// Extract waypoints
				pathWaypoints.Clear();
				pathWaypoints.AddRange(navMeshPath.corners);

				if (pathWaypoints.Count > 1)
				{
					currentWaypointIndex = 1; // Skip current position
					_dest = pathWaypoints[currentWaypointIndex];
					_originalDest = dest;
					hasDestination = true;

					Debug.Log($"Unity found path with {pathWaypoints.Count} waypoints");

					// Use YOUR movement system
					if (_movementCoroutine != null)
						StopCoroutine(_movementCoroutine);
					_movementCoroutine = StartCoroutine(movementCoroutine());
				}
			}
			else // fallback to old way for now
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
		}

		private void CheckWaypointProgress()
		{
			if (pathWaypoints.Count == 0) return;

			float distanceToWaypoint = Vector3.Distance(transform.position, _dest);

			if (distanceToWaypoint < 1.5f)
			{
				currentWaypointIndex++;

				if (currentWaypointIndex < pathWaypoints.Count)
				{
					_dest = pathWaypoints[currentWaypointIndex];
					Debug.Log($"Next waypoint: {currentWaypointIndex}/{pathWaypoints.Count}");
				}
				else
				{
					// Reached destination
					hasDestination = false;
					pathWaypoints.Clear();
					currentWaypointIndex = 0;
				}
			}
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
			// Draw current path waypoints
			if (pathWaypoints.Count > 1)
			{
				// Draw path lines
				Gizmos.color = Color.cyan;
				for (int i = 0; i < pathWaypoints.Count - 1; i++)
				{
					Gizmos.DrawLine(pathWaypoints[i], pathWaypoints[i + 1]);
				}

				// Draw waypoint spheres
				for (int i = 0; i < pathWaypoints.Count; i++)
				{
					if (i == 0)
					{
						// Start point - green
						Gizmos.color = Color.green;
						Gizmos.DrawWireSphere(pathWaypoints[i], 0.3f);
					}
					else if (i == pathWaypoints.Count - 1)
					{
						// End point - red
						Gizmos.color = Color.red;
						Gizmos.DrawWireSphere(pathWaypoints[i], 0.4f);
					}
					else if (i == currentWaypointIndex)
					{
						// Current target waypoint - yellow (larger)
						Gizmos.color = Color.yellow;
						Gizmos.DrawWireSphere(pathWaypoints[i], 0.6f);
					}
					else
					{
						// Other waypoints - white (small)
						Gizmos.color = Color.white;
						Gizmos.DrawWireSphere(pathWaypoints[i], 0.2f);
					}
				}

				// Draw current destination with special highlight
				if (currentWaypointIndex < pathWaypoints.Count)
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawLine(transform.position, pathWaypoints[currentWaypointIndex]);
				}
			}

			// Draw simple line to destination if no path waypoints
			else if (hasDestination)
			{
				Gizmos.color = Color.magenta;
				Gizmos.DrawLine(transform.position, _dest);
				Gizmos.DrawWireSphere(_dest, 0.5f);
			}
		}
	}
}
