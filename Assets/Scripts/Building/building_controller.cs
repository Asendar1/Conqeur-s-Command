using System;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;

public enum building_ids
{
    None,
    HQ,
    Barracks,
    SupplyBase,
    MoneyYard
}
public class building_controller : MonoBehaviour
{
    private building_ids showcase_building_id;
    private main_controller main_controller;
    private LayerMask ground;
    private GameObject building_prefab = null;
    private GameObject building_preview = null;

    public static Dictionary<building_ids, int> buildings_cost = new Dictionary<building_ids, int>
    {
        {building_ids.HQ, 200},
        {building_ids.Barracks, 150},
        {building_ids.SupplyBase, 125}
    };
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        showcase_building_id = building_ids.None;
        main_controller = GetComponent<main_controller>();
        if (main_controller == null)
        {
            Debug.LogError("main_controller component not found on this GameObject.");
            return;
        }
        ground = LayerMask.GetMask("Ground");
        if (ground == 0)
        {
            Debug.LogError("Ground layer not found. Make sure the layer is set up correctly.");
            return;
        }
    }
    void Update()
    {
        if (showcase_building_id != building_ids.None)
        {
            handle_building_placement();
        }
    }

    private void handle_building_placement()
    {
        if (building_preview == null)
        {
            GameObject preview_prefab = get_preview_prefab(showcase_building_id);
            if (preview_prefab != null)
            {
                building_preview = Instantiate(preview_prefab);
            }
        }
        if (building_preview != null)
        {
            // TODO i might add rivers later so check for that as well not only the ground layer
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, ground))
            {
                Vector3 pos = hit.point;
                pos.y = 0;
                building_preview.transform.position = pos;
                bool is_valid_position = check_placment_validity(pos);
                Renderer[] renderers = building_preview.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.material.color = is_valid_position ? Color.green : Color.red;
                }
                if (Input.GetMouseButtonDown(0) && is_valid_position)
                {
                    place_building(pos);
                    cancel_building();
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                cancel_building();
            }
        }
	}

    private void place_building(Vector3 pos)
    {
        int cost = buildings_cost[showcase_building_id];
        if (!main_controller.spend_money(cost))
        {
            Debug.Log("You are poor my guy");
            return;
        }
        GameObject new_building = Instantiate(building_prefab, pos, quaternion.identity);
        building_main this_building = new_building.GetComponent<building_main>();
        if (this_building != null)
        {
            this_building.team_id = main_controller.my_team_id;
        }
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

	private GameObject get_preview_prefab(building_ids showcase_building_id)
	{
		switch (showcase_building_id)
        {
            case building_ids.HQ:
                return Resources.Load<GameObject>("Prefabs/Buildings/HQ/HQ_preview");
            case building_ids.Barracks:
                return Resources.Load<GameObject>("Prefabs/Buildings/Barracks/Barracks_preview");
            case building_ids.SupplyBase:
                return Resources.Load<GameObject>("Prefabs/Buildings/Supply_base/supply_base_preview");
            default:
                Debug.LogError("Invalid building ID.");
                return null;
        }
	}

	public void spawn_building(building_ids building_id)
    {
        if (building_preview != null)
        {
            Destroy(building_preview);
            building_preview = null;
        }
        switch (building_id)
        {
            case building_ids.HQ:
                showcase_building_id = building_ids.HQ;
                building_prefab = Resources.Load<GameObject>("Prefabs/Buildings/HQ/HQ");
                break;
            case building_ids.Barracks:
                showcase_building_id = building_ids.Barracks;
                building_prefab = Resources.Load<GameObject>("Prefabs/Buildings/Barracks/Barracks");
                break;
            case building_ids.SupplyBase:
                showcase_building_id = building_ids.SupplyBase;
                building_prefab = Resources.Load<GameObject>("Prefabs/Buildings/Supply_base/supply_base");
                break;
        }
        // ! the cost calculation is done before,
        // ! the player can't excute this function if he didn't got the funds already
    }
    public void cancel_building()
    {
        showcase_building_id = building_ids.None;
        if (building_preview != null)
        {
            Destroy(building_preview);
            building_preview = null;
        }
    }
}
