using System;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public enum building_ids
{
    None,
    HQ,
    Barracks
}
public class building_controller : MonoBehaviour
{
    private building_ids showcase_building_id;
    private main_controller main_controller;
    private LayerMask ground;
    private GameObject building_prefab = null;
    private GameObject building_preview = null;
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
		throw new NotImplementedException();
	}

	private bool check_placment_validity(Vector3 pos)
	{
		throw new NotImplementedException();
	}

	private GameObject get_preview_prefab(building_ids showcase_building_id)
	{
		throw new NotImplementedException();
	}

	public void spawn_building(building_ids building_id)
    {
        switch (building_id)
        {
            case building_ids.HQ:
                showcase_building_id = building_ids.HQ;
                building_prefab = Resources.Load<GameObject>(null);
                break;
            case building_ids.Barracks:
                showcase_building_id = building_ids.Barracks;
                building_prefab = Resources.Load<GameObject>(null);
                break;
        }
        // ! the cost calculation is done before,
        // ! the player can't excute this function if he didn't got the funds already
    }
    public void cancel_building()
    {
        showcase_building_id = building_ids.None;
    }
}
