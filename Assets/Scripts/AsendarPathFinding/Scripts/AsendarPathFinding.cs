using Unity.Mathematics;
using Unity.VisualScripting;

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

	[System.Serializable]
	public unsafe struct NavTriangle
	{
		public float3 v0, v1, v2;
		public float3 center;
		public float3 normal;
		public fixed int neighbors[3];
		public float movementCost;
		public byte terrainType; // might add ships and such
		public int triangleId;

		public void SetNeighbor(int index, int neighborId)
        {
            if (index >= 0 && index < 3)
                neighbors[index] = neighborId;
        }

        public int GetNeighbor(int index)
        {
            if (index >= 0 && index < 3)
                return neighbors[index];
            return -1;
        }

		public bool canUnitPass(MovementUnitTypes unitType)
		{
			return true; // this function is for the ships if implemented later
		}

		public bool isPointInside(float3 point)
		{
			float3 v0v1 = v1 - v0;
			float3 v0v2 = v2 - v0;
			float3 v0p = point - v0;

			float d00 = math.dot(v0v1, v0v1);
			float d01 = math.dot(v0v1, v0v2);
			float d11 = math.dot(v0v2, v0v2);
			float d20 = math.dot(v0p, v0v1);
			float d21 = math.dot(v0p, v0v2);

			float denom = d00 * d11 - d01 * d01;
			if (denom == 0) return false; // Degenerate triangle

			float invDenom = 1.0f / denom;
			float u = (d11 * d20 - d01 * d21) * invDenom;
			float v = (d00 * d21 - d01 * d20) * invDenom;

			return (u >= 0) && (v >= 0) && (u + v <= 1);
		}
	};



}
