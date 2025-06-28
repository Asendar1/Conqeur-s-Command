using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AsendarPathFinding
{
	public class TriangleNavMesh : MonoBehaviour
	{
		[Header("NavMesh Settings")]
		public LayerMask walkableLayerMask = -1; // -1 means all layers
		public LayerMask obstacleLayerMask;
		public float maxSlopeAngle = 45;
		// triangle simplification settings is a field that controls how much the triangle can be low or high res
		public float triangleSimplificationTolerance = 0.5f;

		[Header("Debug Settings")]
		public bool showNavMesh = true;
		public bool showTrinagleIds = false;

		// RunTime Data
		private NativeArray<NavTriangle> triangles;
		private Dictionary<int, int> triangleIdToIndex;
		public bool isGenerated { get; private set; }

		public NativeArray<NavTriangle> Triangles => triangles;
		public int triangleCount => triangles.IsCreated ? triangles.Length : 0;

		private void Awake()
		{
			obstacleLayerMask = LayerMask.GetMask("Obstacle", "Building");
		}

		[ContextMenu("Generate NavMesh")]
		public void generateNavMesh()
		{
			Debug.Log("generating navmesh...");

			// first stio: calculate the walkable areas
			List<MeshData> walkableAreas = collectWalkableAreas(); // may be refered as geometry

			if (walkableAreas.Count == 0)
			{
				Debug.LogWarning("No walkable areas found. NavMesh generation aborted.");
				return;
			}
			//step 2: gnerate the traingles from the meshes
			List<NavTriangle> triangles = generateTrianglesFromMeshes(walkableAreas);

			// step 3: calaculate neighbors
			calculateNeighbors(triangles);

			// step 4 : assign types and costs
			assignTypesAndCosts(triangles);

			// step 5 : convert to stack memory for performance
			createNativeArrays(triangles);

			isGenerated = true;
			Debug.Log("navmesh generated successfully. Number of triangles: " + triangles.Count);
		}

		public struct MeshData
		{
			public Vector3[] vertices;
			public int[] triangles;
			public Transform transform;
			public Renderer renderer;
		}

		private List<MeshData> collectWalkableAreas()
		{
			List<MeshData> walkableAreas = new List<MeshData>();

			// find all objects with the walkable layer
			GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

			foreach (GameObject obj in allObjects)
			{
				if (((1 << obj.layer) & walkableLayerMask) != 0)
				{
					MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
					MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();

					if (meshFilter != null && meshFilter.sharedMesh != null &&
						meshRenderer != null)
					{
						Mesh mesh = meshFilter.sharedMesh;

						MeshData meshData = new MeshData
						{
							vertices = mesh.vertices,
							triangles = mesh.triangles,
							transform = obj.transform,
							renderer = meshRenderer
						};

						walkableAreas.Add(meshData);
					}
				}

				Terrain terrain = obj.GetComponent<Terrain>();
				if (terrain != null && terrain.terrainData != null)
				{

					// Convert terrain to mesh data
					MeshData terrainMeshData = ConvertTerrainToMeshData(terrain);
					walkableAreas.Add(terrainMeshData);
				}
			}

			Debug.Log("Found " + walkableAreas.Count + " walkable areas in the scene.");
			return walkableAreas;
		}

		[Header("Performance Safety")]
		[Range(16, 128)]
		public int maxTerrainResolution = 32; // START SMALL!
		public int maxTrianglesPerTerrain = 5000; // Safety limit
		public bool enableTerrainConversion = false; // Manual enable

		private MeshData ConvertTerrainToMeshData(Terrain terrain)
		{
			if (!enableTerrainConversion)
			{
				Debug.LogWarning("Terrain conversion disabled. Enable in inspector if needed.");
				return new MeshData(); // Return empty data
			}

			TerrainData terrainData = terrain.terrainData;

			// SAFETY: Use small resolution for testing
			int resolution = Mathf.Min(maxTerrainResolution, 32); // Cap at 32x32 for safety

			Vector3 terrainSize = terrainData.size;
			Debug.Log($"🗻 Converting terrain at {resolution}x{resolution} resolution...");

			// Safety check for triangle count
			int estimatedTriangles = (resolution - 1) * (resolution - 1) * 2;
			if (estimatedTriangles > maxTrianglesPerTerrain)
			{
				Debug.LogError($"❌ Terrain would generate {estimatedTriangles} triangles! Limit is {maxTrianglesPerTerrain}. Reduce resolution.");
				return new MeshData();
			}

			List<Vector3> vertices = new List<Vector3>();

			for (int y = 0; y < resolution; y++)
			{
				for (int x = 0; x < resolution; x++)
				{
					float normalizedX = (float)x / (resolution - 1);
					float normalizedY = (float)y / (resolution - 1);

					float height = terrainData.GetInterpolatedHeight(normalizedX, normalizedY);

					Vector3 vertex = new Vector3(
						normalizedX * terrainSize.x,
						height,
						normalizedY * terrainSize.z
					);

					vertices.Add(vertex);
				}
			}

			List<int> triangles = new List<int>();
			for (int y = 0; y < resolution - 1; y++)
			{
				for (int x = 0; x < resolution - 1; x++)
				{
					int i = y * resolution + x;

					triangles.Add(i);
					triangles.Add(i + resolution);
					triangles.Add(i + 1);

					triangles.Add(i + 1);
					triangles.Add(i + resolution);
					triangles.Add(i + resolution + 1);
				}
			}

			Debug.Log($"✅ Terrain converted safely: {vertices.Count} vertices, {triangles.Count / 3} triangles");

			return new MeshData
			{
				vertices = vertices.ToArray(),
				triangles = triangles.ToArray(),
				transform = terrain.transform,
				renderer = null
			};
		}


		private List<NavTriangle> generateTrianglesFromMeshes(List<MeshData> meshes)
		{
			List<NavTriangle> triangles = new List<NavTriangle>();
			int triangleIdCounter = 0;

			foreach (MeshData meshData in meshes)
			{
				Matrix4x4 localToWorld = meshData.transform.localToWorldMatrix;

				for (int i = 0; i < meshData.triangles.Length; i += 3) // FIX: increment by 3
				{
					Vector3 localV0 = meshData.vertices[meshData.triangles[i]];
					Vector3 localV1 = meshData.vertices[meshData.triangles[i + 1]];
					Vector3 localV2 = meshData.vertices[meshData.triangles[i + 2]];

					float3 worldv0 = localToWorld.MultiplyPoint3x4(localV0);
					float3 worldv1 = localToWorld.MultiplyPoint3x4(localV1);
					float3 worldv2 = localToWorld.MultiplyPoint3x4(localV2);

					float3 center = (worldv0 + worldv1 + worldv2) / 3f;
					float3 normal = math.normalize(math.cross(worldv1 - worldv0, worldv2 - worldv0));

					float slopeAngle = math.degrees(math.acos(math.dot(normal, math.up())));
					if (slopeAngle > maxSlopeAngle) continue;

					float area = .5f * math.length(math.cross(worldv1 - worldv0, worldv2 - worldv0));
					if (area < .1f) continue;

					NavTriangle triangle = new NavTriangle
					{
						v0 = worldv0,
						v1 = worldv1,
						v2 = worldv2,
						center = center,
						normal = normal,
						movementCost = 1f,
						terrainType = 0,
						triangleId = triangleIdCounter++
					};

					// FIXED: Initialize neighbors using helper method
					unsafe
					{
						triangle.SetNeighbor(0, -1);
						triangle.SetNeighbor(1, -1);
						triangle.SetNeighbor(2, -1);
					}

					if (!Physics.CheckSphere(center, 0.5f, obstacleLayerMask) &&
						(!Physics.CheckSphere(triangle.v0, 0.3f, obstacleLayerMask) ||
						!Physics.CheckSphere(triangle.v1, 0.3f, obstacleLayerMask) ||
						!Physics.CheckSphere(triangle.v2, 0.3f, obstacleLayerMask)))
					{
						triangles.Add(triangle);
					}
				}
			}

			Debug.Log("Generated " + triangles.Count + " triangles from the meshes.");
			return triangles;
		}

		private struct Edge
		{
			public float3 start, end;

			public Edge(float3 start, float3 end)
			{
				this.start = start;
				this.end = end;
			}

			public bool IsSharedWith(Edge other, float threshold)
			{
				// Check if edges share the same endpoints (within threshold)
				bool startMatch1 = math.distance(start, other.start) < threshold;
				bool endMatch1 = math.distance(end, other.end) < threshold;

				bool startMatch2 = math.distance(start, other.end) < threshold;
				bool endMatch2 = math.distance(end, other.start) < threshold;

				return (startMatch1 && endMatch1) || (startMatch2 && endMatch2);
			}
		}

		private void calculateNeighbors(List<NavTriangle> triangles)
		{
			Debug.Log("Calculating triangle neighbors...");
			float neighborDistanceThreshold = 0.1f;

			for (int i = 0; i < triangles.Count; i++)
			{
				NavTriangle triangle = triangles[i];

				Edge[] edges = new Edge[3]
				{
			new Edge(triangle.v0, triangle.v1),
			new Edge(triangle.v1, triangle.v2),
			new Edge(triangle.v2, triangle.v0)
				};

				for (int j = 0; j < triangles.Count; j++)
				{
					if (i == j) continue;

					NavTriangle otherTriangle = triangles[j];
					Edge[] otherEdges = new Edge[3]
					{
				new Edge(otherTriangle.v0, otherTriangle.v1),
				new Edge(otherTriangle.v1, otherTriangle.v2),
				new Edge(otherTriangle.v2, otherTriangle.v0)
					};

					for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
					{
						// FIXED: Use helper method to check neighbors
						if (triangle.GetNeighbor(edgeIndex) != -1) continue;

						for (int otherEdgeIndex = 0; otherEdgeIndex < 3; otherEdgeIndex++)
						{
							if (edges[edgeIndex].IsSharedWith(otherEdges[otherEdgeIndex], neighborDistanceThreshold))
							{
								// FIXED: Use helper method to set neighbor
								triangle.SetNeighbor(edgeIndex, otherTriangle.triangleId);
								break;
							}
						}
					}
				}

				triangles[i] = triangle;
			}

			Debug.Log("Triangle neighbors calculated");
		}

		private void assignTypesAndCosts(List<NavTriangle> triangles)
		{
			Debug.Log("Assigning types and costs to triangles...");

			// For simplicity, we can assign a default type and cost
			for (int i = 0; i < triangles.Count; i++)
			{
				NavTriangle triangle = triangles[i];

				float slopeAngle = math.degrees(math.acos(math.dot(triangle.normal, math.up())));

				if (slopeAngle < 10f) triangle.movementCost = 1f;

				if (slopeAngle < 25f) triangle.movementCost = 1.5f;
				else triangle.movementCost = 50f; // just a placeholder untill better unit system is there

				triangle.terrainType = 0; // Default terrain type, can be extended later
				triangles[i] = triangle; // Update the triangle in the list
			}

			Debug.Log("Types and costs assigned to triangles");
		}

		private void createNativeArrays(List<NavTriangle> triangleList)
		{
			// Dispose previous arrays if they exist
			if (triangles.IsCreated) triangles.Dispose();

			// Create new NativeArray for performance
			triangles = new NativeArray<NavTriangle>(triangleList.Count, Allocator.Persistent);
			triangleIdToIndex = new Dictionary<int, int>();

			for (int i = 0; i < triangleList.Count; i++)
			{
				triangles[i] = triangleList[i];
				triangleIdToIndex[triangleList[i].triangleId] = i;
			}
		}

		void OnDestroy()
		{
			if (triangles.IsCreated)
			{
				triangles.Dispose();
			}
		}

		public int findTriangleIndex(float3 worldPos)
		{
			if (!isGenerated) return -1;

			for (int i = 0; i < triangles.Length; i++)
			{
				if (triangles[i].isPointInside(worldPos))
				{
					return i; // Return the index of the triangle that contains the point
				}
			}
			return -1; // No triangle found
		}

		public NavTriangle? findTriangle(float3 worldPos)
		{
			int index = findTriangleIndex(worldPos);
			if (index != -1)
			{
				return triangles[index]; // Return the triangle if found
			}
			return null; // No triangle found
		}

		public List<int> getNeighborsIndices(int triangleIndex)
		{
			List<int> neighborIndices = new List<int>();

			if (triangleIndex < 0 || triangleIndex >= triangles.Length) return neighborIndices;

			NavTriangle triangle = triangles[triangleIndex];

			for (int i = 0; i < 3; i++)
			{
				int neighborId = triangle.GetNeighbor(i); // FIXED: Use helper method
				if (neighborId != -1 && triangleIdToIndex.TryGetValue(neighborId, out int neighborIndex))
				{
					neighborIndices.Add(neighborIndex);
				}
			}

			return neighborIndices;
		}

		public float3 GetRandomPointInTriangle(int triangleIndex)
		{
			if (triangleIndex < 0 || triangleIndex >= triangles.Length)
				return float3.zero;

			NavTriangle triangle = triangles[triangleIndex];

			// Generate random barycentric coordinates
			float r1 = UnityEngine.Random.value;
			float r2 = UnityEngine.Random.value;

			if (r1 + r2 > 1f)
			{
				r1 = 1f - r1;
				r2 = 1f - r2;
			}

			float r3 = 1f - r1 - r2;

			return r1 * triangle.v0 + r2 * triangle.v1 + r3 * triangle.v2;
		}

		void OnDrawGizmos()
		{
			if (!showNavMesh || !isGenerated) return;

			for (int i = 0; i < triangles.Length; i++)
			{
				NavTriangle triangle = triangles[i];

				// Color based on terrain type
				if (triangle.terrainType == 0)
					Gizmos.color = Color.green;

				Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);

				// Draw triangle
				Vector3 v0 = triangle.v0;
				Vector3 v1 = triangle.v1;
				Vector3 v2 = triangle.v2;

				Gizmos.DrawLine(v0, v1);
				Gizmos.DrawLine(v1, v2);
				Gizmos.DrawLine(v2, v0);

				// Draw triangle center
				Gizmos.color = Color.white;
				Gizmos.DrawWireSphere(triangle.center, 0.2f);

				// Show triangle ID
				if (showTrinagleIds)
				{
					UnityEditor.Handles.Label(triangle.center, triangle.triangleId.ToString());
				}
			}
		}
	}
};
