using UnityEditor.Rendering;
using UnityEngine;

public class building_main : MonoBehaviour
{
    public team_ids team_id;
    public int max_hp;
    public int current_hp;
    public int cost;
    // public int build_time;
    public building_ids building_id;
    public bool is_selected = false;
    public bool is_alive = true;
    private health_bar_controll health_bar_controller;
    private bool is_under_attack = false;
    private float under_attack_timer = 3f;


    void Start()
    {
        health_bar_controller = GetComponentInChildren<health_bar_controll>();
        if (health_bar_controller == null)
        {
            Debug.LogError("health_bar_controll component not found in children of this GameObject.");
            return;
        }
        init_building();
        health_bar_controller.update_health_bar();
    }

    void Update()
    {
        if (is_under_attack)
        {
            under_attack_timer -= Time.deltaTime;
            if (under_attack_timer <= 0f)
            {
                is_under_attack = false;
                if (!is_selected)
                {
                    health_bar_controller.SetVisible(false);
                }
            }
        }
    }

    public virtual void take_damage(int damage)
    {
        is_under_attack = true;
        current_hp -= damage;
        under_attack_timer = 3f;
        health_bar_controller.SetVisible(true);
        health_bar_controller.update_health_bar();
        if (current_hp <= 0)
        {
            Destroy(gameObject);
        }
    }
    public virtual void set_selected(bool status)
    {
        is_selected = status;
        if (health_bar_controller != null)
        {
            health_bar_controller.SetVisible(status);
        }
    }
    protected virtual void init_building()
    {

    }
}
