using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Mono.Cecil;
using System.Runtime.CompilerServices;
using UnityEngine.AI;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.UI;


public enum faction_ids
{
    None,
    test_faction
};

public class unit_worker : unit_main
{
    private building_ids showcase_building_id = building_ids.None;
    private bool is_building = false;

    private GameObject building_prefab = null;
    private GameObject building_preview = null;
    private GameObject temp_site_prefab = null;
    private GameObject temp_site = null;
    private temp_site temp_script = null;

    private Coroutine building_coroutine = null;

    private Dictionary<building_ids, GameObject> building_prefabs = new Dictionary<building_ids, GameObject>();
    private Dictionary<building_ids, GameObject> construction_prefabs = new Dictionary<building_ids, GameObject>();


    [SerializeField] private float build_speed = 1f;

    private enum build_states { Idle, Moving_to_building, Moving_to_left_building, Building };
    [SerializeField] private build_states current_state = build_states.Idle;

    public Dictionary<building_ids, int> buildings_cost = new Dictionary<building_ids, int>
    {
        {building_ids.HQ, 200},
        {building_ids.Barracks, 150},
        {building_ids.SupplyBase, 125}
    };

    protected override void init_unit()
    {
        unit_id = unit_ids.worker_unit;
        unit_base_hp = 600;
        unit_hp = unit_base_hp;
        unit_attack_speed = 0;
        unit_damage = 0;
        can_attack = false;
        build_speed = 1f;
        // if bugs occurs. try setting unit range to 0

        temp_site_prefab = Resources.Load<GameObject>("Prefabs/temp_site");
        load_all_prefabs();
    }

    private void OnEnable()
    {
        worker_unit_panel.on_building_button_click += handle_building;
    }

    private void OnDisable()
    {
        worker_unit_panel.on_building_button_click -= handle_building;
        cancel_building_placement();
    }

    private void OnDestroy()
    {
        if (temp_site != null && temp_script != null)
        {
            temp_script.is_left = true;
        }
    }
    void Update()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            // If the pointer is over a UI element, do nothing
            return;
        }
        if (building_preview != null)
        {
            update_building_placement();
        }
        switch (current_state)
        {
            case build_states.Moving_to_building:
                if (temp_site != null && (transform.position - temp_site.transform.position).sqrMagnitude < 2f * 2f)
                {
                    current_state = build_states.Building;
                    building_coroutine = StartCoroutine(build_construction(temp_site.transform.position));
                }
                break;
        }
    }

    private void update_building_placement()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ground))
        {
            Vector3 pos = hit.point;
            pos.y = 0;
            building_preview.transform.position = pos;
            bool is_valid_position = check_placment_validity(pos);
            // there could be multiple parts of the building. if so do this
            Renderer[] renderers = building_preview.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material.color = is_valid_position ? Color.green : Color.red;
            }
            // building_preview.GetComponent<Renderer>().material.color = is_valid_position ? Color.green : Color.red;
                if (Input.GetMouseButtonDown(0) && is_valid_position)
                {
                    if (main_controller.spend_money(buildings_cost[showcase_building_id]))
                    {
                        main_controller.remove_unit(this);
                        base.set_move_order(pos);
                        current_state = build_states.Moving_to_building;
                        temp_site = Instantiate(temp_site_prefab, pos, Quaternion.identity);
                        temp_script = temp_site.GetComponent<temp_site>();
                        temp_script.team_id = main_controller.my_team_id;
                        temp_script.building_id = building_ids.None;
                        temp_script.building_id_when_completed = showcase_building_id;
                        if (building_preview != null)
                        {
                            Destroy(building_preview);
                            building_preview = null;
                        }
                    }
                    else
                    {
                        cancel_building_placement();
                        Debug.Log("You can't abuse this can you | you are poor now");
                    }
                }
        }
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            cancel_building_placement();
        }
    }


    private void load_all_prefabs()
    {
        if (building_prefabs.Count == 0)
        {
            building_prefabs.Add(building_ids.HQ, Resources.Load<GameObject>("Prefabs/Buildings/HQ/HQ"));
            building_prefabs.Add(building_ids.Barracks, Resources.Load<GameObject>("Prefabs/Buildings/Barracks/Barracks"));
            building_prefabs.Add(building_ids.SupplyBase, Resources.Load<GameObject>("Prefabs/Buildings/supply_base/supply_base"));

            // do later the construction prefabs
            construction_prefabs.Add(building_ids.HQ, Resources.Load<GameObject>("Prefabs/Buildings/HQ/HQ_preview"));
            construction_prefabs.Add(building_ids.Barracks, Resources.Load<GameObject>("Prefabs/Buildings/Barracks/Barracks_preview"));
            construction_prefabs.Add(building_ids.SupplyBase, Resources.Load<GameObject>("Prefabs/Buildings/supply_base/supply_base_preview"));
        }
    }
    private void handle_building(building_ids ids)
    {
        if (!is_selected) return;
        if (this != main_controller.selected_units[0])
        {
            return;
        }

        start_building(ids);
    }

    private bool check_placment_validity(Vector3 pos)
    {
        Collider preview_collider = building_preview.GetComponent<Collider>();
        Bounds bounds = preview_collider.bounds;
        Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.extents * 0.9f, Quaternion.identity);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject == building_preview || collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                continue;
            }
            return false;
        }
        return true;
	}

    private IEnumerator build_construction(Vector3 pos)
    {
        // Vector3 start_pos = Vector3(pos.x, pos.y - 3f, pos.z);
        while (temp_script.is_completed == false)
        {
            if (temp_site == null)
            {
                current_state = build_states.Idle;
                yield break; // Exit if the temp site is destroyed
            }
            temp_script.add_progress(Time.deltaTime * build_speed);
            yield return null;
        }
        // when the building is completed
        current_state = build_states.Idle;
        Destroy(temp_site);
        temp_site = null;
        temp_script = null;
        GameObject new_building = Instantiate(building_prefabs[showcase_building_id], pos, Quaternion.identity);
        building_main this_building = new_building.GetComponent<building_main>();
        if (this_building != null)
        {
            this_building.team_id = main_controller.my_team_id;
            this_building.building_id = showcase_building_id;
        }
        cancel_building_placement();
    }

    private void start_building(building_ids ids)
    {
        cancel_building_placement();

        showcase_building_id = ids;
        GameObject prefab = construction_prefabs[ids];
        building_preview = Instantiate(prefab);
    }

    private void cancel_building_placement()
    {
        if (building_preview != null)
        {
            Destroy(building_preview);
            building_preview = null;
        }
        showcase_building_id = building_ids.None;
    }

	public override void unit_right_click(Vector3 pos, unit_main target_unit, building_main target_building, float radius)
	{
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            // If the pointer is over a UI element, do nothing
            return;
        }
        if (temp_site != null && temp_script != null)
        {
            temp_script.is_left = true;
        }
        current_state = build_states.Idle;
        if (building_coroutine != null)
        {
            StopCoroutine(building_coroutine);
            building_coroutine = null;
        }
        temp_script = target_building as temp_site;
        if (temp_script != null && temp_script.team_id == main_controller.my_team_id && temp_script.is_left)
        {
            // if its a left building, start building it
            showcase_building_id = temp_script.building_id_when_completed;
            temp_site = temp_script.gameObject;
            temp_script.is_left = false;
            set_move_order(temp_site.transform.position);
            current_state = build_states.Moving_to_building;
            return;
        }
        temp_script = null;
        temp_site = null;
		base.unit_right_click(pos, target_unit, target_building, radius);
	}
}
