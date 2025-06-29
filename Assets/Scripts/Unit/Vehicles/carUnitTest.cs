using UnityEngine;

public class carUnitTest : unit_main
{
	protected override void init_unit()
	{
		unit_id = unit_ids.vehicle_unit;
		team_id = team_ids.Ayham_team;
		unit_base_hp = 1500;
		unit_hp = unit_base_hp;
		unit_attack_speed = .8f;
		unit_damage = 160;
		unit_range = 55f;
	}

}
