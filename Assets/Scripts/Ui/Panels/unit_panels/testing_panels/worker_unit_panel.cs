using System;
using UnityEngine;
using UnityEngine.UI;

public class worker_unit_panel : MonoBehaviour
{
    [SerializeField] private Button hq_ui_button;
    [SerializeField] private Button barracks_ui_button;
    [SerializeField] private Button supply_base_ui_button;

    public static Action<building_ids> on_building_button_click;


    private void Start()
    {
        if (hq_ui_button != null)
        {
            hq_ui_button.onClick.AddListener(hq_button);
        }
        if (barracks_ui_button != null)
        {
            barracks_ui_button.onClick.AddListener(barracks_button);
        }
        if (supply_base_ui_button != null)
        {
            supply_base_ui_button.onClick.AddListener(supply_base_button);
        }
    }

    private void OnDestroy()
    {
        if (hq_ui_button != null)
        {
            hq_ui_button.onClick.RemoveListener(hq_button);
        }
        if (barracks_ui_button != null)
        {
            barracks_ui_button.onClick.RemoveListener(barracks_button);
        }
        if (supply_base_ui_button != null)
        {
            supply_base_ui_button.onClick.RemoveListener(supply_base_button);
        }
    }

    public void on_building_button_click_handler(building_ids building_id)
    {
        on_building_button_click?.Invoke(building_id);
    }

    public void hq_button()
    {
        on_building_button_click?.Invoke(building_ids.HQ);
    }

    public void barracks_button()
    {
        on_building_button_click?.Invoke(building_ids.Barracks);
    }

    public void supply_base_button()
    {
        on_building_button_click?.Invoke(building_ids.SupplyBase);
    }

}
