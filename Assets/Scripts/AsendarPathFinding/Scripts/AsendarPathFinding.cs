using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

namespace AsendarPathFinding
{
	public enum MovementUnitTypes
	{
		None,
		FootUnit,
		CarUnit,
		LightVehicleUnit,
		LightArmoredVehicleUnit,
		HeavyVehicleUnit
	};

	public struct flowField
	{
		public Vector2[,] flowDirections;
		public float[,] distance;
		public bool[,] walkable;
		public int width;
		public int height;
		public float cellSize;
		public Vector3 worldOrigin;

		public flowField(int w, int h, float size, Vector3 origin)
		{
			width = w;
			height = h;
			cellSize = size;
			worldOrigin = origin;
			flowDirections = new Vector2[w, h];
			distance = new float[w, h];
			walkable = new bool[w, h];

			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					walkable[x, y] = true; // Initialize walkable grid (all walkable by default)
				}
			}
		}

		public Vector2Int worldToGrid(Vector3 worldPos)
		{
			Vector3 relativePos = worldPos - worldOrigin;
			return new Vector2Int(
				Mathf.FloorToInt(relativePos.x / cellSize),
				Mathf.FloorToInt(relativePos.z / cellSize)
			);
		}

		public Vector3 gridToWorld(int x, int y)
		{
			return worldOrigin + new Vector3(
				x * cellSize + cellSize * 0.5f,
				0,
				y * cellSize + cellSize * 0.5f
			);
		}
		public bool isValidCell(int x, int y)
		{
			return x >= 0 && x < width && y >= 0 && y < height;
		}
	}

	public static class FlowFieldGenerator
	{
		private static LayerMask obstacleLayerMask = LayerMask.GetMask("Obstacle", "Building"); // will this be set every request? "optimize"


		public static flowField generateFlowField(Vector3 target, Vector3 worldPos, int width, int height, float cellSize)
		{
			flowField field = new flowField(width, height, cellSize, worldPos);

			markObstacles(ref field); // step 1: mark the obstacles
			calculateDistances(ref field, target); // step 2: calculate distances to the target
			GenerateFlowDirections(ref field); // step 3: generate flow field directions

			return field;
		}

		private static void markObstacles(ref flowField field)
		{
			for (int x = 0; x < field.width; x++)
			{
				for (int y = 0; y < field.height; y++)
				{
					Vector3 worldPos = field.gridToWorld(x, y);

					if (Physics.CheckSphere(worldPos, field.cellSize * .4f, obstacleLayerMask))
					{
						field.walkable[x, y] = false;
					}
				}
			}
		}

		private static void calculateDistances(ref flowField field, Vector3 target)
		{
			Vector2Int targetCell = field.worldToGrid(target);

			for (int x = 0; x < field.width; x++)
			{
				for (int y = 0; y < field.height; y++)
				{
					field.distance[x, y] = float.MaxValue;
				}
			}

			// Dijstra's algro
			Queue<Vector2Int> openSet = new Queue<Vector2Int>();

			if (field.isValidCell(targetCell.x, targetCell.y))
			{
				field.distance[targetCell.x, targetCell.y] = 0f;
				openSet.Enqueue(targetCell);
			}

			while (openSet.Count > 0)
			{
				Vector2Int current = openSet.Dequeue();

				for (int dx = -1; dx <= 1; dx++)
				{
					for (int dy = -1; dy <= 1; dy++)
					{
						if (dx == 0 && dy == 0) continue; // skip the current cell

						int nx = current.x + dx;
						int ny = current.y + dy;

						if (!field.isValidCell(nx, ny) || !field.walkable[nx, ny])
							continue; // skip invalid or non-walkable cells

						float distanceCost = (dx == 0 || dy == 0) ? 1f : 1.4f; // 1 for straight, 1.4 for diagonal
						float newDistance = field.distance[current.x, current.y] + distanceCost;

						if (newDistance < field.distance[nx, ny])
						{
							field.distance[nx, ny] = newDistance;
							openSet.Enqueue(new Vector2Int(nx, ny));
						}
					}
				}
			}
		}

		private static void GenerateFlowDirections(ref flowField field)
		{
			for (int x = 0; x < field.width; x++)
			{
				for (int y = 0; y < field.height; y++)
				{
					if (!field.walkable[x, y]) continue;

					Vector2 bestDir = Vector2.zero;
					float bestDistance = field.distance[x, y];

					for (int dx = -1; dx <= 1; dx++)
					{
						for (int dy = -1; dy <= 1; dy++)
						{
							if (dx == 0 && dy == 0) continue;

							int nx = x + dx;
							int ny = y + dy;

							if (!field.isValidCell(nx, ny) || !field.walkable[nx, ny]) continue;

							if (field.distance[nx, ny] < bestDistance)
							{
								bestDistance = field.distance[nx, ny];
								bestDir = new Vector2(dx, dy).normalized;
							}
						}
					}

					field.flowDirections[x, y] = bestDir;
				}
			}
		}
	}
}
