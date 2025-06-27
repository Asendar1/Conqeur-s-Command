using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Data.SqlTypes;
using UnityEngine.UI;
using Unity.Collections;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using UnityEngine.AI;

public class main_controller : MonoBehaviour
{
    private LayerMask ground;
    private LayerMask unit;
    private LayerMask building;
    private LayerMask UI;

    private building_main building_main;
    public List<unit_main> all_units = new List<unit_main>();
    public List<unit_main> selected_units = new List<unit_main>();
    public team_ids my_team_id;
    private building_controller building_controller;
    [SerializeField] private TextMeshProUGUI money_text;


    //test
    [Header("test variables")]
    [SerializeField] private Vector3 cell1;
    [SerializeField] private Vector3 cell2;

    [Space(10)]
    [SerializeField] private int money = 1500;
    [SerializeField] private int power = 0;

    private unit_main target_unit;
    private building_main target_building;

    void Start()
    {
        my_team_id = team_ids.Ayham_team;
        ground = LayerMask.GetMask("Ground");
        unit = LayerMask.GetMask("Unit");
        building = LayerMask.GetMask("Building");
        UI = LayerMask.GetMask("UI");
        building_controller = GetComponent<building_controller>();
    }

    void OnEnable()
    {
        game_events.On_money_deposited += add_money;
    }

    void OnDisable()
    {
        game_events.On_money_deposited -= add_money;
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
                    Vector3 random_spawn = new Vector3(spawn_position.x + Random.Range(-5f, 5f), spawn_position.y + 1, spawn_position.z + Random.Range(-5f, 5f));
                    GameObject newUnitObject = Instantiate(unitPrefab, random_spawn, Quaternion.identity);
                }
            }
            else
            {
                Debug.LogError("Unit prefab not found! Make sure it exists in Resources/Prefabs folder.");
            }
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            building_controller.spawn_building(building_ids.HQ);
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            building_controller.spawn_building(building_ids.Barracks);
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            building_controller.spawn_building(building_ids.SupplyBase);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            foreach (unit_main unit in selected_units)
            {
                unit.stop_unit();
            }
        }
        // to test money addition
        if (Input.GetKeyDown(KeyCode.L))
        {
            Vector3 spawn_text = Camera.main.ScreenToWorldPoint(new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 10f));
            spawn_text.y = 0;
            game_events.added_money(spawn_text, -100);
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ground))
            {
                cell1 = hit.point;
                Debug.Log($"Z pressed: Selected cell1 at world position {cell1}");
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ground))
            {
                cell2 = hit.point;
                Debug.Log($"X pressed: Selected cell2 at world position {cell2}");
            }
        }
    }

    private void handle_right_click()
    {
        if (selected_units.Count == 0) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ground | unit | building))
        {
            target_unit = null;
            target_building = null;
            if (hit.collider.CompareTag("Unit"))
            {
                target_unit = hit.collider.GetComponent<unit_main>();
            }
            else if (hit.collider.CompareTag("Building"))
            {
                target_building = hit.collider.GetComponent<building_main>();
            }
            Vector3 targetPos = hit.point;
            targetPos.y = 0;
            if (selected_units.Count == 1)
            {
                selected_units[0].unit_right_click(targetPos, target_unit, target_building, 0);
                return;
            }

            generate_waypoints(targetPos, target_unit, target_building);
        }
    }

    private void generate_waypoints(Vector3 targetPos, unit_main target_unit, building_main target_building)
    {
        foreach (unit_main unit in selected_units)
        {
            unit.set_move_order(targetPos);
        }
    }

    private void handle_left_click()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            // If the pointer is over a UI element, do nothing
            return;
        }
        bool is_multi_selecting = Input.GetKey(KeyCode.LeftAlt);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, unit | ground))
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
                    ui_events.unit_selected(target_unit);
                    select_unit(target_unit);
                }
            }
            // TODO i should add a way to check building or unit health without the need to select it.
            // TODO maybe by hovering over it
            if (hit.collider.CompareTag("Building"))
            {
                de_select_all_units();
                building_main = hit.collider.GetComponent<building_main>();
                if (building_main != null && building_main.team_id == my_team_id)
                {
                    ui_events.building_selected(building_main);
                    building_main.set_selected(true);
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
        ui_events.deselect();
        foreach (unit_main unit in selected_units)
        {
            unit.set_select(false);
        }
        if (building_main != null)
        {
            building_main.set_selected(false);
        }
        selected_units.Clear();
    }

    public void add_unit(unit_main unit)
    {
        if (unit == null) return;
        if (!all_units.Contains(unit))
        {
            all_units.Add(unit);
        }
    }

    public void remove_unit(unit_main unit)
    {
        if (unit == null) return;
        if (selected_units.Contains(unit))
        {
            selected_units.Remove(unit);
            unit.set_select(false);
        }
    }

    public int get_money()
    {
        return money;
    }
    public bool spend_money(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            update_ui();
            return true;
        }
        return false;
    }
    public void add_money(int amount, team_ids team_id)
    {
        if (team_id != my_team_id) return;
        money += amount;
        update_ui();
    }
    public void set_money(int amount)
    {
        money = amount;
        update_ui();
    }
    private void update_ui()
    {
        if (money_text != null)
        {
            money_text.text = "Money: " + money.ToString();
        }
    }
}
