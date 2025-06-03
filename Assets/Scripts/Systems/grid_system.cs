using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class grid_system : MonoBehaviour
{
    public static grid_system instance;

    private Terrain terrain;

    [Header("Grid Settings")]
    [SerializeField] private bool draw_grid = true;
    [SerializeField] private float gird_size = 1f;

    private int grid_x, grid_z;
    private Cell[,] grid;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("No active terrain found. Please ensure a terrain is present in the scene.");
            return;
        }

        grid_x = Mathf.RoundToInt(terrain.terrainData.size.x);
        grid_z = Mathf.RoundToInt(terrain.terrainData.size.z);
        init_grid();
    }

    private void init_grid()
    {
        grid = new Cell[grid_x, grid_z];

        for (int x = 0; x < grid_x; x++)
        {
            for (int z = 0; z < grid_z; z++)
            {
                Vector3 world_pos = new Vector3(x, 0f, z);
                float height = terrain.SampleHeight(world_pos);
                world_pos.y = height;
                grid[x, z] = new Cell(world_pos, x, z, height);
                grid[x, z].is_walkable = !Physics.CheckSphere(world_pos, gird_size * 0.5f, LayerMask.GetMask("Clickable"));
            }
        }
    }

    void OnDrawGizmos()
    {

        for (int x = 0; x < grid_x; x++)
        {
            for (int z = 0; z < grid_z; z++)
            {
                Gizmos.color = grid[x, z].is_walkable ? Color.green : Color.red;
                Vector3 world_pos = grid[x, z].world_position;
                Gizmos.DrawWireCube(world_pos, new Vector3(gird_size, 0.1f, gird_size));
            }
        }
    }

}

public class Cell
{
    public Vector3 world_position;
    public int grid_x;
    public int grid_z;

    public bool is_walkable = true;

    public float height;

    public int g_cost;
    public int h_cost;
    public int f_cost
    {
        get { return g_cost + h_cost; }
    }
    public Cell parent;

    public Cell(Vector3 world_position, int grid_x, int grid_z, float height)
    {
        this.world_position = world_position;
        this.grid_x = grid_x;
        this.grid_z = grid_z;
        this.height = height;
    }
}
