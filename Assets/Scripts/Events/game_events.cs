using UnityEngine;
using System;

public static class game_events
{
	// ? Unit Events
	public static event Action<unit_main> on_unit_selected;
	public static event Action<unit_main> on_unit_deselected;
	public static event Action<unit_main, short> on_unit_damaged;
	public static event Action<unit_main> on_unit_destroyed;


}
