using System.Collections.Generic;
using UnityEngine;

public class path_finding_system : MonoBehaviour
{
    public static path_finding_system instance;
    private List<GameObject> debug_planes = new List<GameObject>();


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
    }

    public List<Cell> find_path(Vector3 start, Vector3 end)
    {
        clean_debug_planes();
        Cell start_cell = grid_system.instance.get_cell_from_world_position(start);
        Cell end_cell = grid_system.instance.get_cell_from_world_position(end);

        if (start_cell == null || end_cell == null)
        {
            return null;
        }

        reset_path_data(start_cell);

        List<Cell> open_set = new List<Cell>();
        HashSet<Cell> closed_set = new HashSet<Cell>();

        open_set.Add(start_cell);
        while (open_set.Count > 0)
        {
            Cell current_cell = open_set[0];
            for (int i = 1; i < open_set.Count; i++)
            {
                if (open_set[i].f_cost < current_cell.f_cost ||
                    (open_set[i].f_cost == current_cell.f_cost && open_set[i].h_cost < current_cell.h_cost))
                {
                    current_cell = open_set[i];
                }
            }

            open_set.Remove(current_cell);
            closed_set.Add(current_cell);

            if (current_cell == end_cell)
            {
                return retrace_path(current_cell);
            }

            foreach (Cell neighbor in grid_system.instance.get_neighbors(current_cell))
            {
                if (!neighbor.is_walkable || closed_set.Contains(neighbor))
                {
                    // if (!neighbor.is_walkable)
                    // {
                    //     GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    //     debug_planes.Add(plane);
                    //     plane.transform.position = neighbor.world_position + Vector3.up * 0.1f;
                    //     plane.transform.localScale = new Vector3(.2f, .2f, .2f);
                    //     plane.GetComponent<Renderer>().material.color = Color.red;
                    // }
                    continue;
                }
                int move_cost = current_cell.g_cost + get_distance(current_cell, neighbor);
                if (move_cost < neighbor.g_cost || !open_set.Contains(neighbor))
                {
                    neighbor.g_cost = move_cost;
                    neighbor.h_cost = get_distance(neighbor, end_cell);
                    neighbor.parent = current_cell;
                    if (!open_set.Contains(neighbor))
                    {
                        open_set.Add(neighbor);
                    }
                }
            }
        }
        return null;
    } // ? (1, 3) (4, 5) distX = 3 distZ = 2 38 | 32

    private void clean_debug_planes()
    {
        foreach (GameObject plane in debug_planes)
        {
            if (plane != null)
            {
                Destroy(plane);
            }
        }
        debug_planes.Clear();
	}

	private int get_distance(Cell cell_a, Cell cell_b)
    {
        int dist_x = Mathf.Abs(cell_a.grid_x - cell_b.grid_x);
        int dist_z = Mathf.Abs(cell_a.grid_z - cell_b.grid_z);
        if (dist_x > dist_z)
            return 14 * dist_z + 10 * (dist_x - dist_z);
        return 14 * dist_x + 10 * (dist_z - dist_x);
    }

    public List<Cell> retrace_path(Cell cell)
    {
        List<Cell> path = new List<Cell>();

        while (cell != null)
        {
            path.Add(cell);
            cell = cell.parent;
        }
        path.Reverse();
        // foreach (Cell c in path)
        // {
        //     GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        //     debug_planes.Add(plane);
        //     plane.transform.position = c.world_position + Vector3.up * 0.5f;
        //     plane.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        //     plane.GetComponent<Renderer>().material.color = Color.green;
        // }
        return path;
    }

    private void reset_path_data(Cell start_cell)
    {
        // Reset only the cells we need for this path
        // This is more efficient than resetting the entire grid
        HashSet<Cell> visited = new HashSet<Cell>();
        Queue<Cell> cells_to_reset = new Queue<Cell>();

        cells_to_reset.Enqueue(start_cell);
        visited.Add(start_cell);

        while (cells_to_reset.Count > 0)
        {
            Cell current = cells_to_reset.Dequeue();
            current.g_cost = 0;
            current.h_cost = 0;

            // If this cell has a parent from a previous path calculation
            if (current.parent != null)
            {
                current.parent = null;

                // Add neighbors to reset queue if they haven't been visited
                foreach (Cell neighbor in grid_system.instance.get_neighbors(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        cells_to_reset.Enqueue(neighbor);
                    }
                }
            }
        }
    }
}


// nice idea here
//  ? if (heightDiff > 0.5f) // Adjust threshold as needed
//  ?              moveCost += Mathf.RoundToInt(heightDiff * 2);
