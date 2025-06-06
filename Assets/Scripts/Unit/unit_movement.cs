using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class unit_movement : MonoBehaviour
{
    private unit_main unit_main;

    private List<Cell> current_path;
    private int current_waypoint_i;
    private LayerMask unit_layer;

    [Header("Movement Settings")]
    [SerializeField] private float move_speed = 5f;
    [SerializeField] private float rotation_speed = 5f;
    [SerializeField] private float stopping_distance = 0.5f;
    [SerializeField] private float avoidance_distance = 1f;

    private bool is_moving = false;
    private Quaternion target_rotation;
    private Vector3 current_destination;
    private bool dest_reached = true;

    private Coroutine move_routine = null;
    private float path_recalc_timer = 0f;
    private float path_recalc_interval = 1f;

    void Start()
    {
        unit_main = GetComponent<unit_main>();
        if (unit_layer == 0)
        {
            unit_layer = LayerMask.GetMask("Unit");
        }
    }

    public void MoveTo(Vector3 destination)
    {
        current_destination = destination;
        dest_reached = false;

        List<Cell> path = path_finding_system.instance.find_path(transform.position, destination);

        if (path != null && path.Count > 0)
        {
            if (move_routine != null)
                StopCoroutine(move_routine);

            current_path = path;
            current_waypoint_i = 0;
            is_moving = true;

            move_routine = StartCoroutine(follow_path());
        }
    }

    private IEnumerator follow_path()
    {
        while (is_moving && current_path != null && current_waypoint_i < current_path.Count)
        {
            Vector3 waypoint_pos = current_path[current_waypoint_i].world_position;
            waypoint_pos.y = transform.position.y; // Keep the y position constant

            // It's quite important to understand this one line incase of the unit goes zoom zoom.
            // what it does is it calculates where the unit should be looking at (e.g. (2, 0, 3) - (6, 0, 7) = (-4, 0, -4))
            // meaning the unit will look left 4 unit and down 4 unit.
            // then we normalize it so it doesn't go 4 unit left and 4 unit down fast, it should be 1 (or the unit's speed)
            // making it (-1, 0, -1) which is the direction the unit should be looking at.
            Vector3 direction = (waypoint_pos - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, waypoint_pos);

            direction = apply_avoidance(direction);

            if (direction != Vector3.zero)
            {
                target_rotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, target_rotation, rotation_speed * Time.deltaTime);
            }
            if (distance > stopping_distance)
            {
                float angle_to_target = Quaternion.Angle(transform.rotation, target_rotation);
                float speed_multiplier = Mathf.Clamp01(1 - (angle_to_target / 90f));

                transform.position += direction * move_speed * speed_multiplier * Time.deltaTime;
            }
            else
            {
                current_waypoint_i++;
            }

            // path recalculation
            path_recalc_timer += Time.deltaTime;
            if (path_recalc_timer >= path_recalc_interval)
            {
                path_recalc_timer = 0f;
                if (Vector3.Distance(transform.position, current_destination) > stopping_distance * 2f)
                {
                    recalcuate_path();
                }
            }
            yield return null;
        }
        path_completed();
    }

    private void recalcuate_path()
    {
        List<Cell> new_path = path_finding_system.instance.find_path(transform.position, current_destination);

        if (new_path != null && new_path.Count > 0)
        {
            current_path = new_path;
            current_waypoint_i = 0;
        }
    }

    private Vector3 apply_avoidance(Vector3 direction)
    {
        Collider[] nearby_units = Physics.OverlapSphere(transform.position, avoidance_distance, unit_layer);
        if (nearby_units.Length <= 1)
        {
            return direction; // No other units nearby, no avoidance needed
        }
        Vector3 avoid_dir = Vector3.zero;
        int avoid_count = 0;

        foreach (Collider unit in nearby_units)
        {
            if (unit.gameObject == gameObject)
                continue; // Skip self
            Vector3 away_dir = transform.position - unit.transform.position;
            float distance = away_dir.magnitude;
            if (distance > 0)
            {
                float weight = 1f - (distance / avoidance_distance);
                avoid_dir += away_dir.normalized * weight;
                avoid_count++;
            }
        }

        if (avoid_count == 0)
            return direction;
        avoid_dir /= avoid_count; // Average the avoidance direction
        Vector3 final_dir = Vector3.Lerp(direction, avoid_dir.normalized, .5f).normalized;
        return final_dir;
    }

    private void path_completed()
    {
        is_moving = false;
        dest_reached = true;
        current_path = null;
        current_waypoint_i = 0;
        move_routine = null;
    }

    public void stop_unit()
    {
        if (move_routine != null)
            StopCoroutine(move_routine);
        is_moving = false;
        move_routine = null;
    }

    public int approach_unit_within_range(Vector3 target_position)
    {
        float distance = Vector3.Distance(transform.position, target_position);
        if (distance > unit_main.unit_range)
        {
            Vector3 dir = (target_position - transform.position).normalized;
            Vector3 range_position = target_position - dir * unit_main.unit_range;

            if (!is_moving || dest_reached || Vector3.Distance(transform.position, range_position) > stopping_distance)
            {
                MoveTo(range_position);
            }
            return 1; // Approaching
        }
        else
        {
            stop_unit();
            return 0; // Within range
        }
    }

    void OnDrawGizmos()
    {
        if (current_path != null && current_path.Count > 0 && is_moving)
        {
            Gizmos.color = Color.blue;

            // Draw line segments for the path
            for (int i = current_waypoint_i; i < current_path.Count - 1; i++)
            {
                Vector3 start = current_path[i].world_position;
                Vector3 end = current_path[i + 1].world_position;

                // Adjust height for better visibility
                start.y += 0.1f;
                end.y += 0.1f;

                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(start, 0.2f);
            }

            // Mark current waypoint
            if (current_waypoint_i < current_path.Count)
            {
                Gizmos.color = Color.yellow;
                Vector3 target = current_path[current_waypoint_i].world_position;
                target.y += 0.1f;
                Gizmos.DrawSphere(target, 0.3f);
            }
        }
    }
}
