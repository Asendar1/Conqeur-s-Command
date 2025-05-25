using UnityEngine;

public class floating_text_manager : MonoBehaviour
{
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Transform worldSpaceCanvas;

    private void OnEnable()
    {
        // Subscribe to money events
        game_events.On_added_money += ShowMoneyFloatingText;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        game_events.On_added_money -= ShowMoneyFloatingText;
    }

    private void ShowMoneyFloatingText(Vector3 position, int amount)
    {
        // Don't show text for zero amounts
        if (amount == 0) return;

        // Calculate screen position (slightly above the actual position)
        Vector3 spawnPosition = position + Vector3.up * 1.5f;

        // Instantiate the floating text
        GameObject textObj = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);

        // If we have a world space canvas, parent to it
        if (worldSpaceCanvas != null)
            textObj.transform.SetParent(worldSpaceCanvas);

        // Initialize with amount
        floating_text textComponent = textObj.GetComponent<floating_text>();
        if (textComponent != null)
        {
            textComponent.Initialize(amount);
        }
        else
        {
            Debug.LogError("Floating text prefab missing floating_text component!");
        }

        // Make the text face the camera
        textObj.transform.forward = Camera.main.transform.forward;
    }
}
