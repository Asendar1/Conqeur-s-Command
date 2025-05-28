using System;
using System.Collections.Generic;
using UnityEngine;

public class ui_manager : MonoBehaviour
{

    [SerializeField] private Canvas in_match_canvas;
    [SerializeField] private GameObject command_panel;

    // ? Units panels
    [SerializeField] private GameObject worker_panel;

    // ? Buildings panels
    [SerializeField] private GameObject barracks_panel;
    [SerializeField] private GameObject supply_base_panel;

    private Dictionary<unit_ids, GameObject> unit_panels = new Dictionary<unit_ids, GameObject>();
    private Dictionary<building_ids, GameObject> building_panels = new Dictionary<building_ids, GameObject>();
    void Awake()
    {
        unit_panels[unit_ids.worker_unit] = worker_panel;

        building_panels[building_ids.Barracks] = barracks_panel;
        building_panels[building_ids.SupplyBase] = supply_base_panel;


        // ! Later when i have a main menu i should hide this one
        clear_unit_building_panels();
    }


    private void OnEnable()
    {
        ui_events.On_unit_selected += show_unit_panel;
        ui_events.On_building_selected += show_building_panel;
        ui_events.On_deselect += clear_unit_building_panels;
    }

    private void OnDisable()
    {
        ui_events.On_unit_selected -= show_unit_panel;
        ui_events.On_building_selected -= show_building_panel;
        ui_events.On_deselect -= clear_unit_building_panels;
    }

	private void show_unit_panel(unit_main main)
	{
        if (unit_panels.TryGetValue(main.unit_id, out GameObject panel))
        {
            clear_unit_building_panels();
            panel.SetActive(true);
        }
	}

    private void show_building_panel(building_main main)
    {
        if (building_panels.TryGetValue(main.building_id, out GameObject panel))
        {
            clear_unit_building_panels();
            panel.SetActive(true);
        }
	}

	private void clear_unit_building_panels()
    {
        foreach (var panel in unit_panels.Values)
        {
            panel.SetActive(false);
        }
        foreach (var panel in building_panels.Values)
        {
            panel.SetActive(false);
        }
    }
}
