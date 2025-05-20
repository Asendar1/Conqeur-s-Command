using UnityEngine;

public class HQ : building_main
{
    protected override void init_building()
    {
        max_hp = 10000;
        current_hp = 10000;
        cost = 200;
        building_id = building_ids.HQ;
    }
}
