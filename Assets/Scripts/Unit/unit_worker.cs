using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Mono.Cecil;
using System.Runtime.CompilerServices;


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


    private static Dictionary<building_ids, GameObject> building_prefabs = new Dictionary<building_ids, GameObject>();
    private static Dictionary<building_ids, GameObject> construction_prefabs = new Dictionary<building_ids, GameObject>();


    [SerializeField] private float build_speed = 1f;

    private enum build_states { Idle, Moving_to_building, Building };
    [SerializeField] private build_states current_state = build_states.Idle;

    public static Dictionary<building_ids, int> buildings_cost = new Dictionary<building_ids, int>
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
        cancle_building_placement();
    }

    void Update()
    {
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
                    StartCoroutine(build_construction(temp_site.transform.position));
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
                    base.set_move_order(pos);
                    current_state = build_states.Moving_to_building;
                    temp_site = Instantiate(temp_site_prefab, pos, Quaternion.identity);
                    cancle_building_placement();
                }
                else
                {
                    Debug.Log("You can't abuse this can you | you are poor now");
                }
            }
        }
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            cancle_building_placement();
        }
    }


    private void load_all_prefabs()
    {
        if (building_prefabs.Count == 0)
        {
            building_prefabs.Add(building_ids.HQ, Resources.Load<GameObject>("Prefabs/Buildings/HQ/HQ"));
            building_prefabs.Add(building_ids.Barracks, Resources.Load<GameObject>("Prefabs/Buildings/Barracks/Barracks"));
            building_prefabs.Add(building_ids.SupplyBase, Resources.Load<GameObject>("Prefabs/Buildings/SupplyBase/SupplyBase"));

            // do later the construction prefabs
            construction_prefabs.Add(building_ids.HQ, Resources.Load<GameObject>("Prefabs/Buildings/HQ/HQ_preview"));
            construction_prefabs.Add(building_ids.Barracks, Resources.Load<GameObject>("Prefabs/Buildings/Barracks/Barracks_preview"));
            construction_prefabs.Add(building_ids.SupplyBase, Resources.Load<GameObject>("Prefabs/Buildings/supply_base/supply_base_preview"));
        }
    }
    private void handle_building(building_ids ids)
    {
        if (!is_selected) return;
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
        Debug.Log("i am here");
        GameObject construction_site = Instantiate(building_prefabs[showcase_building_id], new Vector3(pos.x, pos.y - 3f, pos.z), Quaternion.identity);
        while (current_state == build_states.Building)
        {
            if (construction_site == null)
            {
                current_state = build_states.Idle;
                yield break;
            }
            construction_site.transform.up += Vector3.up * build_speed * Time.deltaTime;
            if (construction_site.transform.position.y >= pos.y)
            {
                current_state = build_states.Idle;
                GameObject new_building = Instantiate(building_prefabs[showcase_building_id], pos, Quaternion.identity);
                building_main this_building = new_building.GetComponent<building_main>();
                if (this_building != null)
                {
                    this_building.team_id = main_controller.my_team_id;
                }
                Destroy(construction_site);
                Destroy(temp_site);
                temp_site = null;
                cancle_building_placement();
                yield break;
            }
        }
    }

    private void start_building(building_ids ids)
    {
        cancle_building_placement();

        showcase_building_id = ids;
        GameObject prefab = construction_prefabs[ids];
        building_preview = Instantiate(prefab);
    }

    private void cancle_building_placement()
    {
        if (building_preview != null)
        {
            Destroy(building_preview);
            building_preview = null;
        }
        showcase_building_id = building_ids.None;
    }

    public override void set_move_order(Vector3 dest)
    {
        current_state = build_states.Idle;
        base.set_move_order(dest);
    }
}
