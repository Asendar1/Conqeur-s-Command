using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class grid_system : MonoBehaviour
{
    public static grid_system instance;

    private Terrain terrain;

    [Header("Grid Settings")]
    [SerializeField] private bool draw_grid = true;
    [SerializeField] private float cell_size = 2f;

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

        grid_x = Mathf.RoundToInt(terrain.terrainData.size.x / cell_size);
        grid_z = Mathf.RoundToInt(terrain.terrainData.size.z / cell_size);
        init_grid();
    }

    private void init_grid()
    {
        grid = new Cell[grid_x, grid_z];

        for (int x = 0; x < grid_x; x++)
        {
            for (int z = 0; z < grid_z; z++)
            {
                Vector3 world_pos = new Vector3(x * cell_size, 0f, z * cell_size);
                float height = terrain.SampleHeight(world_pos);
                world_pos.y = height;
                grid[x, z] = new Cell(world_pos, x, z, height);
                grid[x, z].is_walkable = !Physics.CheckSphere(world_pos, cell_size * 0.5f, LayerMask.GetMask("Clickable"));
            }
        }
    }

    public Cell get_cell_from_world_position(Vector3 world_position)
    {
        int x = Mathf.FloorToInt(world_position.x / cell_size);
        int z = Mathf.FloorToInt(world_position.z / cell_size);

        if (x < 0 || x >= grid_x || z < 0 || z >= grid_z)
        {
            Debug.LogWarning("World position is out of grid bounds.");
            return null;
        }

        return grid[x, z];
    }

    public List<Cell> get_neighbors(Cell cell)
    {
        List<Cell> neighbors = new List<Cell>();

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue;
                int check_x = cell.grid_x + x;
                int check_z = cell.grid_z + z;

                if (check_x >= 0 && check_x < grid_x && check_z >= 0 && check_z < grid_z)
                {
                    neighbors.Add(grid[check_x, check_z]);
                }
            }
        }

        return neighbors;
    }

    public void test_click(Vector3 pos)
    {
        Debug.Log(pos);
        Cell click_cell = get_cell_from_world_position(pos);
        Debug.Log(click_cell.world_position);

        click_cell.is_walkable = false;
    }

    void OnDrawGizmos()
    {

        for (int x = 0; x < grid_x; x++)
        {
            for (int z = 0; z < grid_z; z++)
            {
                Gizmos.color = grid[x, z].is_walkable ? Color.green : Color.red;
                Vector3 world_pos = grid[x, z].world_position;
                Gizmos.DrawWireCube(world_pos, new Vector3(cell_size, 0.1f, cell_size));
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
