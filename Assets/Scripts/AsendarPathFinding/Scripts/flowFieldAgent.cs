using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace AsendarPathFinding
{
	public class flowFieldAgent : MonoBehaviour
	{
		[Header("Unit Configuration")]
		public MovementUnitTypes movementUnitType = MovementUnitTypes.FootUnit;

		[Header("Movement Settings (Auto-Configured)")]
		public float maxSpeed = 5f;
		public float acceleration = 8f;
		public float turnSpeed = 5f;

		[Header("Local Avoidance")]
		public float avoidanceRadius = 1.5f;
		public float avoidanceStrength = 2f;
		public float neightborRadius = 3f; // radius to check for nearby agents
		public float alignmentStrength = 1f; // strength of alignment with nearby agents
		public float cohesionStrength = 1f; // strength of cohesion with nearby agents (cohesion means like sticking together)

		[Header("Others")]
		public LayerMask unitLayerMask;

		private flowField currentFlowField;
		private Vector3 currentVelocity;
		private Vector3 targetPos;
		private bool hasTarget = false;

		void Start()
		{
			unitLayerMask = LayerMask.GetMask("Unit");
			switch (movementUnitType)
			{
				case MovementUnitTypes.FootUnit:
					maxSpeed = 3f;
					turnSpeed = 999f;
					avoidanceRadius = 1f;
					break;
				case MovementUnitTypes.CarUnit:
					maxSpeed = 12f;
					turnSpeed = 120f;
					avoidanceRadius = 2f;
					break;
				case MovementUnitTypes.HeavyVehicleUnit:
					maxSpeed = 8f;
					turnSpeed = 60f;
					avoidanceRadius = 2.5f;
					break;
			}
		}


		public float update_frequency = 0.02f; // 50 FPS instead of 130 FPS
		private float last_update_time = 0f;

		void Update()
		{
			if (Time.time - last_update_time < update_frequency) return;
			last_update_time = Time.time;

			if (!hasTarget)
			{
				currentVelocity = Vector3.zero;
				return;
			}

			float distanceToTarget = Vector3.Distance(transform.position, targetPos);
			if (distanceToTarget < 2f)
			{
				hasTarget = false;
				currentVelocity = Vector3.zero;
				return;
			}

			Vector3 flowDirection = getFlowDirection();

			Vector3 separationForce = calculateSeparationForce();
			Vector3 alignmentForce = calculateAlignmentForce();
			Vector3 choesionForce = calculateCohesionForce();

			Vector3 desiredDir = (flowDirection * 1f +
				separationForce * (avoidanceStrength * 5f) +
				alignmentForce * alignmentStrength +
				choesionForce * cohesionStrength).normalized;

			applyMovementByType(desiredDir);
		}

		private Vector3 getFlowDirection()
		{
			Vector2Int gridPos = currentFlowField.worldToGrid(transform.position);

			if (currentFlowField.isValidCell(gridPos.x, gridPos.y))
			{
				Vector2 flow2d = currentFlowField.flowDirections[gridPos.x, gridPos.y];
				return new Vector3(flow2d.x, 0, flow2d.y);
			}
			// fallback: direct direction to target
			return (targetPos - transform.position).normalized;
		}

		private Vector3 calculateSeparationForce()
		{
			Vector3 separation = Vector3.zero;
			int neighborCount = 0;

			Collider[] nearbyUnits = Physics.OverlapSphere(transform.position, avoidanceRadius, unitLayerMask);

			foreach (Collider unit in nearbyUnits)
			{
				if (unit.gameObject == gameObject) continue;

				Vector3 directionAway = transform.position - unit.transform.position;
				float distance = directionAway.magnitude;

				if (distance > .1f && distance < avoidanceRadius)
				{
					// MUCH stronger force when closer
					float strength = (avoidanceRadius - distance) / avoidanceRadius;
					strength = strength * strength; // Square it for exponential falloff

					separation += directionAway.normalized * strength;
					neighborCount++;
				}
			}

			return neighborCount > 0 ? separation / neighborCount : Vector3.zero;
		}

		private Vector3 calculateAlignmentForce()
		{
			Vector3 averageVelocity = Vector3.zero;
			int neighborCount = 0;

			Collider[] nearbyUnits = Physics.OverlapSphere(transform.position, neightborRadius, unitLayerMask);

			foreach (Collider unit in nearbyUnits)
			{
				if (unit.gameObject == gameObject) continue;

				flowFieldAgent otherAgent = unit.GetComponent<flowFieldAgent>();
				if (otherAgent != null)
				{
					averageVelocity += otherAgent.currentVelocity;
					neighborCount++;
				}
			}

			if (neighborCount > 0)
			{
				averageVelocity /= neighborCount;
				return (averageVelocity - currentVelocity).normalized;
			}

			return Vector3.zero;
		}

		private Vector3 calculateCohesionForce()
		{
			Vector3 centerOfMass = Vector3.zero;
			int neightborCount = 0;

			Collider[] nearbyUnits = Physics.OverlapSphere(transform.position, neightborRadius, unitLayerMask);

			foreach (Collider unit in nearbyUnits)
			{
				if (unit.gameObject == gameObject) continue;

				centerOfMass += unit.transform.position;
				neightborCount++;
			}

			if (neightborCount > 0)
			{
				centerOfMass /= neightborCount;
				return (centerOfMass - transform.position).normalized;
			}

			return Vector3.zero;
		}

		private void applyMovementByType(Vector3 desiredDir)
		{
			Vector3 target_velocity = desiredDir * maxSpeed;
			currentVelocity = Vector3.Lerp(currentVelocity, target_velocity, acceleration * Time.deltaTime);

			switch (movementUnitType)
			{
				case MovementUnitTypes.FootUnit:
					// Infantry: Instant turning
					if (currentVelocity.magnitude > 0.1f)
					{
						transform.rotation = Quaternion.LookRotation(currentVelocity);
					}
					transform.position += currentVelocity * Time.deltaTime;
					break;

				case MovementUnitTypes.CarUnit:
				case MovementUnitTypes.LightVehicleUnit:
					// Vehicles: Smooth turning
					if (currentVelocity.magnitude > 0.1f)
					{
						Quaternion target_rotation = Quaternion.LookRotation(currentVelocity);
						transform.rotation = Quaternion.RotateTowards(transform.rotation, target_rotation, turnSpeed * Time.deltaTime);
					}
					transform.position += currentVelocity * Time.deltaTime;
					break;

				case MovementUnitTypes.HeavyVehicleUnit:
					// Tanks: Stop and turn, then move
					if (currentVelocity.magnitude > 0.1f)
					{
						Vector3 desired_forward = currentVelocity.normalized;
						float alignment = Vector3.Dot(transform.forward, desired_forward);

						if (alignment > 0.8f)
						{
							transform.position += currentVelocity * Time.deltaTime;
						}
						else
						{
							Quaternion target_rotation = Quaternion.LookRotation(desired_forward);
							transform.rotation = Quaternion.RotateTowards(transform.rotation, target_rotation, turnSpeed * Time.deltaTime);
						}
					}
					break;
			}
		}

		// public api
		public void setTarget(Vector3 targerPos, flowField field)
		{
			this.targetPos = targerPos;
			currentFlowField = field;
			hasTarget = true;
		}

		public void stop()
		{
			hasTarget = false;
			currentVelocity = Vector3.zero;
		}

		public bool isMoving()
		{
			return hasTarget && currentVelocity.magnitude > 0.1f;
		}
	}
}
