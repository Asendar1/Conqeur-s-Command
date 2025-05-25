using UnityEngine;
using System;

public static class game_events
{
	public static event Action<Vector3, int> On_added_money;
	public static event Action<int, team_ids> On_money_deposited; // i may change this later, i don't like to give a team refrence

	public static void added_money(Vector3 pos, int amount)
	{
		On_added_money?.Invoke(pos, amount);
	}

	public static void money_deposited(int amount, team_ids team)
	{
		On_money_deposited?.Invoke(amount, team);
	}
}
