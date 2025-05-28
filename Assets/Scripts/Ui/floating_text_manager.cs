using System;
using Unity.VisualScripting;
using UnityEngine;

public class floating_text_manager : MonoBehaviour
{
    [SerializeField] private GameObject floating_text_prefab;

    private void OnEnable()
    {
        // Subscribe to money events
        game_events.On_added_money += show_floating_text;
    }

	private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        game_events.On_added_money -= show_floating_text;
    }

    private void show_floating_text(Vector3 pos, int amount)
    {
        Vector3 spawn_pos = pos + Vector3.up * 2f;
        GameObject floating_text = Instantiate(floating_text_prefab, spawn_pos, Quaternion.identity);
        floating_text text = floating_text.GetComponent<floating_text>();
        if (text != null)
        {
            text.init(amount);
        }
        floating_text.transform.forward = Camera.main.transform.forward; // Face the camera

	}

}
