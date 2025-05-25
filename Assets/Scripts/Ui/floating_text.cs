using System.Collections;
using UnityEngine;
using TMPro;

public class floating_text : MonoBehaviour
{
    [SerializeField] private TextMeshPro textComponent;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private AnimationCurve scaleCurve;
    [SerializeField] private AnimationCurve alphaCurve;
    [SerializeField] private Color positiveColor = Color.green;
    [SerializeField] private Color negativeColor = Color.red;

    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshPro>();
    }

    public void Initialize(int amount, bool randomizePosition = true)
    {
        // Format text with plus/minus sign
        textComponent.text = amount > 0 ? "+" + amount : amount.ToString();

        // Set text color based on amount
        textComponent.color = amount >= 0 ? positiveColor : negativeColor;

        // Slightly randomize position if requested
        if (randomizePosition)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(0.2f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            );
            transform.position += randomOffset;
        }

        // Start animation
        StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {
        float elapsed = 0;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + Vector3.up * 2f; // Float upward
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;

            // Move upward
            transform.position = Vector3.Lerp(startPosition, targetPosition, normalizedTime);

            // Scale according to curve
            float scale = scaleCurve.Evaluate(normalizedTime);
            transform.localScale = startScale * scale;

            // Fade according to curve
            Color color = textComponent.color;
            color.a = alphaCurve.Evaluate(normalizedTime);
            textComponent.color = color;

            yield return null;
        }

        // Destroy when animation completes
        Destroy(gameObject);
    }
}
