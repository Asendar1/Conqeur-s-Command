using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public enum team_ids
{
    None,
    Ayham_team,
    Imran_team,
};

public enum unit_ids
{
    supply_unit,
    infantry_unit,
    worker_unit
};

public class unit_main : MonoBehaviour
{
    private LayerMask ground;
    unit_movement unit_movement;
    unit_attacks unit_attacks;
    health_bar_controll health_bar_controller;

    public unit_ids unit_id = unit_ids.infantry_unit;
    public team_ids team_id = team_ids.Ayham_team;
    public int unit_base_hp = 1000;
    public int unit_hp = 1000;
    public float unit_attack_speed = 1f;
    public int unit_damage = 100;
    public int unit_armor = 0;
    public float unit_range = 50f;
    private bool is_selected = false;
    public bool is_alive = true;
    public bool is_attacking = false;
    private bool is_under_attack = false;
    private float under_attack_timer = 0f;
    private const float under_attack_display_cd = 3f;
    public bool can_attack = true;


    void Start()
    {
        init_unit();
        ground = LayerMask.GetMask("Ground");
        unit_movement = GetComponent<unit_movement>();
        if (unit_movement == null)
        {
            Debug.LogError("unit_movement component not found on this GameObject.");
            return;
        }
        if (can_attack)
        {
            unit_attacks = GetComponent<unit_attacks>();
            if (unit_attacks == null)
            {
                Debug.LogError("unit_attacks component not found on this GameObject.");
                return;
            }
        }
        health_bar_controller = GetComponentInChildren<health_bar_controll>();
        if (health_bar_controller == null)
        {
            Debug.LogError("health_bar_controll component not found in children of this GameObject.");
            return;
        }
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
                    health_bar_controller.SetVisible(false);
            }
        }
    }
    public void take_damage(int dmg)
    {
        unit_hp -= dmg;
        is_under_attack = true;
        under_attack_timer = under_attack_display_cd;
        health_bar_controller.SetVisible(true);
        health_bar_controller.update_health_bar();
        if (unit_hp <= 0)
        {
            // Handle unit death
            is_alive = false;
            Debug.Log("Unit " + gameObject.name + " has died.");
            Destroy(gameObject);
        }
    }
    public void set_select(bool status)
    {
        is_selected = status;
        health_bar_controller.SetVisible(status);
    }
    public virtual void set_move_order(Vector3 dest)
    {
        unit_movement.MoveTo(dest);
    }
    public void set_attack_order(unit_main target_unit)
    {
        unit_attacks.attack_target(target_unit);
    }
    public void set_attack_order(building_main target_building)
    {
        unit_attacks.attack_target(target_building);
    }
    public void stop_unit()
    {
        unit_movement.stop_unit();
    }
    public virtual void unit_right_click(Vector3 pos, unit_main target_unit, building_main target_building, float radius)
    {
        if (can_attack)
        {
            if (target_unit != null && target_unit.team_id != team_id)
            {
                is_attacking = true;
                set_attack_order(target_unit);
                return;
            }
            if (target_building != null && target_building.team_id != team_id)
            {
                is_attacking = true;
                set_attack_order(target_building);
                return;
            }
        }
        is_attacking = false;
        // If no attack target, just move to the position
        if (radius > 0)
        {
            Vector2 random_pos = Random.insideUnitCircle * radius;
            Vector3 new_pos = new Vector3(pos.x + random_pos.x, pos.y, pos.z + random_pos.y);
            set_move_order(new_pos);
        }
        else
        {
            set_move_order(pos);
        }
    }
    protected virtual void init_unit()
    {
        // This method is to be overridden by child classes for specific unit initialization
    }
}
