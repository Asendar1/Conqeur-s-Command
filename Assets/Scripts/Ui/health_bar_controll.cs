using UnityEngine;

public class health_bar_controll : MonoBehaviour
{
    private Renderer healthBarRenderer;
    private MaterialPropertyBlock propertyBlock;
    private unit_main unitReference;
    private Transform cameraTransform;
    private float lastHealthPercentage = 1.0f; // Track last health percentage

    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;

    void Start()
    {
        // Initialize references
        healthBarRenderer = GetComponentInChildren<Renderer>();
        unitReference = GetComponentInParent<unit_main>();
        cameraTransform = Camera.main.transform;

        // Initialize MaterialPropertyBlock to avoid creating new materials
        propertyBlock = new MaterialPropertyBlock();

        // Initially hide the health bar
        healthBarRenderer.enabled = false;
    }

    void LateUpdate()
    {
        // Only do the camera facing if the health bar is visible
        if (healthBarRenderer.enabled)
        {
            // Face the camera (billboard) - this still needs to happen every frame
            transform.LookAt(
                transform.position + cameraTransform.rotation * Vector3.forward,
                cameraTransform.rotation * Vector3.up
            );
        }
    }

    // Call this when selection state changes
    public void SetVisible(bool visible)
    {
        healthBarRenderer.enabled = visible;

        // If becoming visible, update the health bar immediately
        if (visible)
        {
            update_health_bar();
        }
    }

    // New method to update health bar visuals - call this only when health changes
    public void update_health_bar()
    {
        if (!healthBarRenderer.enabled || unitReference == null)
            return;

        // Calculate health percentage
        float healthPercent = Mathf.Clamp01((float)unitReference.unit_hp / unitReference.unit_base_hp);

        // Only update if health percentage actually changed
        if (Mathf.Approximately(healthPercent, lastHealthPercentage))
            return;

        lastHealthPercentage = healthPercent;

        // Update the material property block
        propertyBlock.SetFloat("_FillAmount", healthPercent);

        // Set color based on health
        if (healthPercent > 0.6f)
            propertyBlock.SetColor("_FillColor", healthyColor);
        else if (healthPercent > 0.3f)
            propertyBlock.SetColor("_FillColor", damagedColor);
        else
            propertyBlock.SetColor("_FillColor", criticalColor);

        // Apply the property block
        healthBarRenderer.SetPropertyBlock(propertyBlock);
    }
}
