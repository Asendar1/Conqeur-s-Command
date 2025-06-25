using UnityEngine;

namespace AsendarPathFinding
{
	public class flowFieldManager : MonoBehaviour
	{
		[Header("flow field settings")]
		public int gridWidth = 100;
		public int gridHeight = 100;
		public float cellSize = 2f;
		public Vector3 worldOrigin = new Vector3(-100, 0, -100);

		[Header("Debug")]
		public bool showFlowField = false;
		public bool showDistance = false;

		private flowField currentFlowField;
		private bool flowFieldValid = false;

		public static flowFieldManager instance { get; private set; }

		private Vector3 lastTarget = Vector3.zero;
		private float targetThreshold = 5f;

		void Awake()
		{
			instance = this;
		}

		public void generateFlowField(Vector3 target)
		{

			if (Vector3.Distance(target, lastTarget) < targetThreshold && flowFieldValid)
			{
				return; // Use existing flow field
			}

			currentFlowField = FlowFieldGenerator.generateFlowField(target, worldOrigin, gridWidth, gridHeight, cellSize);
			flowFieldValid = true;
			lastTarget = target;
		}

		public void moveUnitsToTarget(flowFieldAgent[] units, Vector3 target)
		{
			generateFlowField(target);

			foreach (flowFieldAgent unit in units)
			{
				if (unit != null)
				{
					unit.setTarget(target, currentFlowField);
				}
			}
		}

		void OnDrawGizmos()
		{
			if (!flowFieldValid) return;

			if (showFlowField)
			{
				DrawFlowField();
			}

			if (showDistance)
			{
				DrawDistances();
			}
		}

		private void DrawFlowField()
		{
			Gizmos.color = Color.blue;

			for (int x = 0; x < currentFlowField.width; x++)
			{
				for (int y = 0; y < currentFlowField.height; y++)
				{
					if (!currentFlowField.walkable[x, y]) continue;

					Vector3 world_pos = currentFlowField.gridToWorld(x, y);
					Vector2 flow_dir = currentFlowField.flowDirections[x, y];

					if (flow_dir.magnitude > 0.1f)
					{
						Vector3 flow_3d = new Vector3(flow_dir.x, 0, flow_dir.y);
						Gizmos.DrawRay(world_pos, flow_3d * cellSize * 0.5f);
					}
				}
			}
		}

		private void DrawDistances()
		{
			for (int x = 0; x < currentFlowField.width; x++)
			{
				for (int y = 0; y < currentFlowField.height; y++)
				{
					if (!currentFlowField.walkable[x, y])
					{
						Gizmos.color = Color.red;
					}
					else
					{
						float distance = currentFlowField.distance[x, y];
						if (distance == float.MaxValue)
						{
							Gizmos.color = Color.black;
						}
						else
						{
							float normalized = Mathf.Clamp01(distance / 50f);
							Gizmos.color = Color.Lerp(Color.green, Color.yellow, normalized);
						}
					}

					Vector3 world_pos = currentFlowField.gridToWorld(x, y);
					Gizmos.DrawCube(world_pos, Vector3.one * cellSize * 0.1f);
				}
			}
		}

	}
}
