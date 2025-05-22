using UnityEngine;
using UnityEngine.Rendering;

public class supply_base : building_main
{

    protected override void init_building()
    {
        max_hp = 5000;
        current_hp = 5000;
        cost = 125;
        building_id = building_ids.SupplyBase;
    }
}
