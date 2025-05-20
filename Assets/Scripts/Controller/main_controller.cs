using UnityEngine;
using System.Collections.Generic;

public class main_controller : MonoBehaviour
{
    private LayerMask ground;
    private LayerMask unit_layer;
    public List<unit_main> all_units = new List<unit_main>();
    public List<unit_main> selected_units = new List<unit_main>();
    public team_ids my_team_id = team_ids.Ayham_team;
    void Start()
    {
        ground = LayerMask.GetMask("Ground");
        unit_layer = LayerMask.GetMask("Clickable");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))  // Right click
        {
            handle_right_click();
        }
        if (Input.GetMouseButtonDown(0)) // Left click to deselect
        {
            handle_left_click();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Example of spawning a game object when R key is pressed
            GameObject unitPrefab = Resources.Load<GameObject>("Prefabs/Units/unit_ayhem"); // Adjust path to your prefab
            if (unitPrefab != null)
            {
                Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit_spawn, Mathf.Infinity, ground);
                Vector3 spawn_position = hit_spawn.point;
                spawn_position.y = 0;
                for (int i = 0; i < 100; i++)
                {
                    GameObject newUnitObject = Instantiate(unitPrefab, spawn_position, Quaternion.identity);
                    unit_main newUnit = newUnitObject.GetComponent<unit_main>();
                }
            }
            else
            {
                Debug.LogError("Unit prefab not found! Make sure it exists in Resources/Prefabs folder.");
            }
        }
    }

    private void handle_right_click()
    {
        if (selected_units.Count == 0) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground | unit_layer))
        {
            if (hit.collider.gameObject.CompareTag("Unit"))
            {
                unit_main target_unit = hit.collider.GetComponent<unit_main>();
                if (target_unit != null && target_unit.team_id != my_team_id)
                {
                    // Attack the target unit
                    foreach (unit_main unit in selected_units)
                    {
                        unit.is_attacking = true;
                        unit.set_attack_order(target_unit);
                    }
                    Debug.Log("Attacking target unit: " + target_unit.name);
                    return;
                }
            }
            Vector3 center_point = hit.point;
            float radius = Mathf.Sqrt(selected_units.Count) * 1.2f;
            foreach (unit_main unit in selected_units)
            {
                unit.is_attacking = false;
                Vector2 random_point = Random.insideUnitCircle * radius;
                Vector3 dest = center_point + new Vector3(random_point.x, 0, random_point.y);
                unit.set_move_order(dest);
            }
        }
    }
    private void handle_left_click()
    {
        bool is_multi_selecting = Input.GetKey(KeyCode.LeftAlt);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, unit_layer | ground))
        {
            if (!is_multi_selecting)
            {
                de_select_all_units();
            }
            if (hit.collider.CompareTag("Unit"))
            {
                unit_main target_unit = hit.collider.GetComponent<unit_main>();
                if (target_unit != null && target_unit.team_id == my_team_id)
                {
                    select_unit(target_unit);
                }
            }
        }
    }
    public void select_unit(unit_main unit)
    {
        if (selected_units.Contains(unit))
        {
            return;
        }
        selected_units.Add(unit);
        unit.set_select(true);
    }
    public void de_select_all_units()
    {
        foreach (unit_main unit in selected_units)
        {
            unit.set_select(false);
        }
        selected_units.Clear();
    }
}
