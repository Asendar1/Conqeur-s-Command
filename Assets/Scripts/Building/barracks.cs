using UnityEngine;

public class barracks : building_main
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void init_building()
    {
        max_hp = 6500;
        current_hp = 6500;
        cost = 150;
        building_id = building_ids.Barracks;
    }

}
