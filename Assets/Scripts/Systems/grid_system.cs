using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class grid_system : MonoBehaviour
{
    public static grid_system instance;

    private Terrain terrain;

    [Header("Grid Settings")]
    [SerializeField] private int chunk_size = 10;
    public float cell_size = 2f;
    private int grid_x, grid_z;
    private int chunks_x, chunks_z;
    private Cell[,] grid;
    private Chunk[,] chunks;

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
        chunks_x = Mathf.CeilToInt((float)grid_x / chunk_size);
        chunks_z = Mathf.CeilToInt((float)grid_z / chunk_size);
        init_regions();
        init_grid();
    }

    private void init_grid()
    {
        grid = new Cell[grid_x, grid_z];

        for (int x = 0; x < grid_x; x++)
        {
            for (int z = 0; z < grid_z; z++)
            {
                // Create Cell
                Vector3 world_pos = new Vector3(terrain.transform.position.x + x * cell_size + cell_size * 0.5f,
                                                0f,
                                                terrain.transform.position.z + z * cell_size + cell_size * 0.5f);
                float height = terrain.SampleHeight(world_pos);
                world_pos.y = height;
                grid[x, z] = new Cell(world_pos, x, z, height);
                grid[x, z].is_walkable = !Physics.CheckSphere(world_pos, cell_size * 0.5f, LayerMask.GetMask("Clickable"));
                // Assign cell to chunk
                int chunk_x = x / chunk_size;
                int chunk_z = z / chunk_size;

                // edge case where chunk_x or chunk_z might exceed the bounds
                if (chunk_x >= chunks_x) chunk_x = chunks_x - 1;
                if (chunk_z >= chunks_z) chunk_z = chunks_z - 1;

                int local_x = x % chunk_size;
                int local_z = z % chunk_size;

                chunks[chunk_x, chunk_z].cells[local_x, local_z] = grid[x, z];

            }
        }
    }

    private void init_regions()
    {
        int chunk_count_x = Mathf.CeilToInt((float)grid_x / chunk_size);
        int chunk_count_z = Mathf.CeilToInt((float)grid_z / chunk_size);
        chunks = new Chunk[chunk_count_x, chunk_count_z];

        for (int x = 0; x < chunk_count_x; x++)
        {
            for (int z = 0; z < chunk_count_z; z++)
            {
                Vector3 world_pos = new Vector3(terrain.transform.position.x + (x * chunk_size * cell_size) + (chunk_size * cell_size * .5f),
                                                0f,
                                                terrain.transform.position.z + (z * chunk_size * cell_size) + (chunk_size * cell_size * .5f));
                chunks[x, z] = new Chunk(world_pos, x, z, chunk_size);
            }
        }
    }

    public Cell get_cell_from_world_position(Vector3 world_position)
    {
        int x = Mathf.FloorToInt((world_position.x - terrain.transform.position.x) / cell_size);
        int z = Mathf.FloorToInt((world_position.z - terrain.transform.position.z) / cell_size);

        if (x < 0 || x >= grid_x || z < 0 || z >= grid_z)
        {
            Debug.LogWarning("World position is out of grid bounds.");
            return null;
        }

        return grid[x, z];
    }

    public Chunk get_chunk_from_world_position(Vector3 world_pos)
    {
        int chunk_x = Mathf.FloorToInt((world_pos.x - terrain.transform.position.x) / (chunk_size * cell_size));
        int chunk_z = Mathf.FloorToInt((world_pos.z - terrain.transform.position.z) / (chunk_size * cell_size));

        // Ensure within bounds
        if (chunk_x >= 0 && chunk_x < chunks_x && chunk_z >= 0 && chunk_z < chunks_z)
        {
            return chunks[chunk_x, chunk_z];
        }

        return null;
    }

    public void update_chunk(Chunk chunk)
    {
        if (chunk == null) return;

        foreach (Cell cell in chunk.cells)
        {
            if (cell == null) continue;
            cell.height = terrain.SampleHeight(cell.world_position);
            cell.is_walkable = !Physics.CheckSphere(cell.world_position, cell_size * .4f, LayerMask.GetMask("Clickable"));
        }
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
        // Draw cells
        if (grid != null)
        {
            for (int x = 0; x < grid_x; x++)
            {
                for (int z = 0; z < grid_z; z++)
                {
                    if (grid[x, z] != null)
                    {
                        Gizmos.color = grid[x, z].is_walkable ? Color.green : Color.red;
                        Vector3 world_pos = grid[x, z].world_position;
                        Gizmos.DrawWireCube(world_pos, new Vector3(cell_size * 0.9f, 0.1f, cell_size * 0.9f));
                    }
                }
            }
        }

        // Draw chunk boundaries
        if (chunks != null)
        {
            Gizmos.color = Color.blue;
            for (int rx = 0; rx < chunks_x; rx++)
            {
                for (int rz = 0; rz < chunks_z; rz++)
                {
                    if (chunks[rx, rz] != null)
                    {
                        // Draw actual chunk size
                        float chunk_world_size = chunk_size * cell_size;
                        Gizmos.DrawWireCube(
                            chunks[rx, rz].world_pos,
                            new Vector3(chunk_world_size, 1f, chunk_world_size)
                        );
                    }
                }
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

[System.Serializable]
public class Chunk
{
    public Vector3 world_pos;
    public int chunk_x;
    public int chunk_z;
    public Cell[,] cells;
    public Bounds bounds;

    public Chunk(Vector3 world_pos, int chunk_x, int chunk_z, int size)
    {
        this.world_pos = world_pos;
        this.chunk_x = chunk_x;
        this.chunk_z = chunk_z;

        cells = new Cell[size, size];
        bounds = new Bounds(world_pos, new Vector3(size * grid_system.instance.cell_size, 100f, size * grid_system.instance.cell_size));
    }
}
