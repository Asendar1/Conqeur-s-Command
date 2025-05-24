using System.Runtime.InteropServices.WindowsRuntime;
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
        team_id = team_ids.Ayham_team;
    }
    public void add_money(int amount)
    {
        throw new System.NotImplementedException();
    }
}
