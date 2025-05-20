using UnityEngine;

public class unit_attacks : MonoBehaviour
{
    private unit_main unit;
    private unit_main current_target;
    private building_main current_target_building;
    private unit_movement unit_movement;
    private float attack_timer = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unit = GetComponent<unit_main>();
        if (unit == null)
        {
            Debug.LogError("unit_main component not found on this GameObject.");
            return;
        }
        unit_movement = GetComponent<unit_movement>();
        if (unit_movement == null)
        {
            Debug.LogError("unit_movement component not found on this GameObject.");
            return;
        }
    }

    void Update()
    {
        // Only process when we have a target and unit is attacking
        if (!unit.is_attacking || !unit.is_alive)
        {
            current_target = null;
            current_target_building = null;
            return;
        }
        if (current_target_building != null)
        {
            if (!current_target_building.is_alive)
            {
                unit.is_attacking = false;
                current_target_building = null;
                return;
            }
            if (unit_movement.approach_unit_within_range(current_target_building.transform.position) == 0)
            {
                unit.is_attacking = true;
                attack_timer += Time.deltaTime;
                if (attack_timer >= 1f / unit.unit_attack_speed)
                {
                    current_target_building.take_damage(unit.unit_damage);
                    attack_timer = 0f; // Reset the attack timer
                }
            }
        }
        if (current_target != null)
        {
            if (!current_target.is_alive)
            {
                unit.is_attacking = false;
                current_target = null;
                return;
            }
            if (unit_movement.approach_unit_within_range(current_target.transform.position) == 0)
            {
                unit.is_attacking = true;
                attack_timer += Time.deltaTime;
                if (attack_timer >= 1f / unit.unit_attack_speed)
                {
                    current_target.take_damage(unit.unit_damage);
                    attack_timer = 0f; // Reset the attack timer
                }
            }
        }

    }

    public void attack_target(unit_main target_unit)
    {
        current_target = target_unit;
    }
    public void attack_target(building_main target_building)
    {
        current_target_building = target_building;
    }
}
