using NUnit.Framework;
using UnityEngine;
using TMPro;
using System;

public class temp_site : building_main
{
    public bool is_left = false;
    public bool is_completed = false;
    public float build_progress = 0f;
    public float building_time_multiplier = 1f; // Multiplier for the building time
    public building_ids building_id_when_completed;
    private bool did_init_speed = false;

    private TextMeshPro progress_text;
    protected override void init_building()
    {
        building_id = building_ids.None;
        max_hp = 1000;
        current_hp = 1000;
        progress_text = GetComponentInChildren<TextMeshPro>();
        switch (building_id_when_completed)
        {
            // building time for HQ will be the same as the default time
            case building_ids.SupplyBase:
                building_time_multiplier = 2f;
                break;
            case building_ids.Barracks:
                building_time_multiplier = 3.5f;
                break;
        }
    }

    void Update()
    {
        progress_text.text = Mathf.RoundToInt(build_progress) + "%";
        if (build_progress >= 100f)
        {
            is_completed = true;
            Destroy(this.gameObject);
        }

        // if (!did_init_speed)
        // {
        //     init_building_speed();
        // }
    }

    private void init_building_speed()
    {
        switch (building_id)
        {
            // building time for HQ will be the same as the default time
            case building_ids.SupplyBase:
                building_time_multiplier = 2f;
                break;
            case building_ids.Barracks:
                building_time_multiplier = 3.5f;
                break;
        }
        did_init_speed = true;
	}

	public void add_progress(float progress)
    {
        build_progress += progress * building_time_multiplier;
    }
}
