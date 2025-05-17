using UnityEngine;
using UnityEngine.AI;

public enum team_ids
{
    Ayham_team,
    Imran_team
};

public class unit_main : MonoBehaviour
{
    private LayerMask ground;
    unit_movement unit_movement;
    unit_attacks unit_attacks;
    public team_ids team_id = team_ids.Ayham_team;
    public short unit_hp = 1000;
    public float unit_attack_speed = 1f;
    public short unit_damage = 100;
    public short unit_armor = 0;
    public float unit_range = 50f;
    private bool is_selected = false;
    public bool is_alive = true;
    public bool is_attacking = false;


    void Start()
    {
        ground = LayerMask.GetMask("Ground");
        unit_movement = GetComponent<unit_movement>();
        if (unit_movement == null)
        {
            Debug.LogError("unit_movement component not found on this GameObject.");
            return;
        }
        unit_attacks = GetComponent<unit_attacks>();
        if (unit_attacks == null)
        {
            Debug.LogError("unit_attacks component not found on this GameObject.");
            return;
        }
    }
    public void take_damage(short dmg)
    {
        unit_hp -= dmg;
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
        // if (is_selected)
        // {
        //     // Highlight the unit
        //     GetComponent<Renderer>().material.color = Color.green;
        // }
        // else
        // {
        //     // Remove highlight
        //     GetComponent<Renderer>().material.color = Color.white;
        // }
    }
    public void set_move_order(Vector3 dest)
    {
        unit_movement.MoveTo(dest);
    }
    public void set_attack_order(unit_main target_unit)
    {
        unit_attacks.attack_target(target_unit);
    }
}
