using UnityEngine;
using System;

public static class game_events
{
	public static event Action<Vector3, int> On_added_money;

	public static void added_money(Vector3 pos, int amount)
	{
		On_added_money?.Invoke(pos, amount);
	}
}
