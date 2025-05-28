using System.Collections;
using UnityEngine;
using TMPro;

public class floating_text : MonoBehaviour
{
	[SerializeField] private TextMeshPro text;

	[SerializeField] private float duration = 2f;
	[SerializeField] private float float_speed = 1f;

	[SerializeField] private Color positive_color = Color.green;
	[SerializeField] private Color negative_color = Color.red;

	public void init(int amount)
	{
		text.text = amount > 0 ? "+ " + amount : amount.ToString();
		text.color = amount > 0 ? positive_color : negative_color;

		StartCoroutine(floating_text_routine());
	}

	private IEnumerator floating_text_routine()
	{
		float elapsed_time = 0f;
		Vector3 start_pos = transform.position;
		while (elapsed_time < duration)
		{
			elapsed_time += Time.deltaTime;
			transform.position = start_pos + Vector3.up * (elapsed_time * float_speed);
			yield return null;
		}
		Destroy(gameObject);
	}
}
