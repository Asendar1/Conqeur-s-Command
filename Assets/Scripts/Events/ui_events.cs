using UnityEngine;
using System;

public class ui_events : MonoBehaviour
{
    public static event Action<unit_main> On_unit_selected;
    public static event Action<building_main> On_building_selected;

    public static event Action On_deselect;

    public static void unit_selected(unit_main unit)
    {
        On_unit_selected?.Invoke(unit);
    }
    public static void building_selected(building_main building)
    {
        On_building_selected?.Invoke(building);
    }
    public static void deselect()
    {
        On_deselect?.Invoke();
    }
}
