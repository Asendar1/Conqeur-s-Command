using NUnit.Framework;
using UnityEngine;
using TMPro;

public class temp_site : building_main
{
    public bool is_left = false;
    public bool is_completed = false;
    public float build_progress = 0f;

    private TextMeshPro progress_text;
    protected override void init_building()
    {
        max_hp = 1000;
        current_hp = 1000;
        progress_text = GetComponentInChildren<TextMeshPro>();
    }

    void Update()
    {
        progress_text.text = Mathf.RoundToInt(build_progress) + "%";
        if (build_progress >= 100f)
        {
            is_completed = true;
            Destroy(this.gameObject);
        }
    }
}
