using UnityEngine;
using System.Collections.Generic;

public class main_controller : MonoBehaviour
{
    private LayerMask ground;
    private LayerMask unit_layer;
    public List<unit_main> all_units = new List<unit_main>();
    private List<unit_main> selected_units = new List<unit_main>();
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
    }

    private void handle_right_click()
    {
        Debug.Log("selected_units count: " + selected_units.Count);
        if (selected_units.Count == 0) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground | unit_layer))
        {
            if (hit.collider.gameObject.CompareTag("Unit"))
            {
                unit_main target_unit = hit.collider.GetComponent<unit_main>();
                Debug.Log("hit team id: " + target_unit.team_id);
                if (target_unit != null && target_unit.team_id != team_ids.Ayham_team)
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
            foreach (unit_main unit in selected_units)
            {
                unit.is_attacking = false;
                unit.set_move_order(hit.point);
            }
        }
    }
    private void handle_left_click()
    {
        bool is_multi_selecting = Input.GetKey(KeyCode.LeftAlt);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (!is_multi_selecting)
            {
                de_select_all_units();
            }
            if (hit.collider.CompareTag("Unit"))
            {
                unit_main target_unit = hit.collider.GetComponent<unit_main>();
                if (target_unit != null && target_unit.team_id == team_ids.Ayham_team)
                {
                    select_unit(target_unit);
                }
            }
        }
    }
    private void select_unit(unit_main unit)
    {
        if (selected_units.Contains(unit))
        {
            Debug.Log("Unit already selected: " + unit.name);
            return;
        }
        selected_units.Add(unit);
        unit.set_select(true);
        Debug.Log("Selected unit: " + unit.name);
    }
    private void de_select_all_units()
    {
        foreach (unit_main unit in selected_units)
        {
            unit.set_select(false);
            Debug.Log("Deselected unit: " + unit.name);
        }
        selected_units.Clear();
    }
}
