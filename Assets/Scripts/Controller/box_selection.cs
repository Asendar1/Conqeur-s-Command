using UnityEngine;
using System.Collections.Generic;

public class box_selection : MonoBehaviour
{
    private LayerMask unit_layer;
    [SerializeField] private RectTransform selection_box;
    private Vector2 start_pos;
    private bool is_dragging = false;
    [SerializeField] private main_controller main_controller;
    private Camera main_camera;

    void Start()
    {
        main_camera = Camera.main;
        if (main_camera == null)
        {
            Debug.LogError("Main camera not found.");
            return;
        }
        unit_layer = LayerMask.GetMask("Clickable");
        selection_box.gameObject.SetActive(false);
    }

    void Update()
    {
        // Start box selection
        if (Input.GetMouseButtonDown(0))
        {
            start_pos = Input.mousePosition;
            is_dragging = true;
            selection_box.gameObject.SetActive(true);
        }

        // Update box size during drag
        if (is_dragging)
        {
            UpdateSelectionBox();
        }

        // End box selection
        if (Input.GetMouseButtonUp(0) && is_dragging)
        {
            is_dragging = false;
            selection_box.gameObject.SetActive(false);
            SelectUnitsInBox();
        }
    }

    private void UpdateSelectionBox()
    {
        // Calculate corners of selection box
        float width = Input.mousePosition.x - start_pos.x;
        float height = Input.mousePosition.y - start_pos.y;

        // Update position (anchored at bottom-left)
        selection_box.anchoredPosition = new Vector2(
            (width > 0) ? start_pos.x : Input.mousePosition.x,
            (height > 0) ? start_pos.y : Input.mousePosition.y
        );

        // Update size (absolute values for width/height)
        selection_box.sizeDelta = new Vector2(
            Mathf.Abs(width),
            Mathf.Abs(height)
        );
    }

    private void SelectUnitsInBox()
    {
        // Only proceed if the box is large enough (to distinguish from clicks)
        if (selection_box.sizeDelta.magnitude < 10f)
            return;

        // Get the corners in screen space
        Vector2 min = new Vector2(
            Mathf.Min(start_pos.x, Input.mousePosition.x),
            Mathf.Min(start_pos.y, Input.mousePosition.y)
        );
        Vector2 max = new Vector2(
            Mathf.Max(start_pos.x, Input.mousePosition.x),
            Mathf.Max(start_pos.y, Input.mousePosition.y)
        );

        // Check if user is holding Alt for multi-select
        bool is_multi_selecting = Input.GetKey(KeyCode.LeftAlt);

        // If not multi-selecting, clear current selection
        if (!is_multi_selecting)
        {
            main_controller.de_select_all_units();
        }

        // Find all unit_main components in scene
        unit_main[] all_units = FindObjectsByType<unit_main>(FindObjectsSortMode.None);

        foreach (unit_main unit in all_units)
        {
            // Only consider player's own units
            if (unit.team_id != team_ids.Ayham_team)
                continue;

            // Convert unit's world position to screen space
            Vector3 screenPos = main_camera.WorldToScreenPoint(unit.transform.position);

            // Check if the unit's screen position is within our selection box
            if (screenPos.x >= min.x && screenPos.x <= max.x &&
                screenPos.y >= min.y && screenPos.y <= max.y)
            {
                // Add unit to selection
                if (!main_controller.selected_units.Contains(unit))
                {
                    main_controller.selected_units.Add(unit);
                    unit.set_select(true);
                }
            }
        }
        if (main_controller.selected_units.Count > 0)
        {
            ui_events.unit_selected(main_controller.selected_units[0]);
        }
    }
}
