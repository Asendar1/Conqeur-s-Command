using UnityEngine;

public class unit_attacks : MonoBehaviour
{
    private unit_main unit;
    private unit_main current_target;
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
        if (current_target == null || !unit.is_attacking || !unit.is_alive)
        {
            current_target = null;
            return;
        }

        // Check if target is still alive
        if (!current_target.is_alive)
        {
            unit.is_attacking = false;
            current_target = null;
            return;
        }

        if (unit_movement.approach_unit_within_range(current_target) == 0)
        {
            unit.is_attacking = true;
            attack_timer += Time.deltaTime;
            // from what i understood, this will check the time in seconds between frames "attack_timer += Time.deltaTime;"
            // when the time is greater than the unit's attack speed, it will deal damage to the target
            // and reset the attack timer.
            // (the formula works as follows: 1 / unit.unit_attack_speed = time between attacks)
            // so if the unit's attack speed is 1, it will attack every second
            // if the unit's attack speed is 2, it will attack every 0.5 seconds
            // if the unit's attack speed is 0.5, it will attack every 2 seconds and so on
            if (attack_timer >= 1f / unit.unit_attack_speed)
            {
                current_target.take_damage(unit.unit_damage);
                attack_timer = 0f; // Reset the attack timer
            }
        }
    }

    public void attack_target(unit_main target_unit)
    {
        current_target = target_unit;
    }
}
