using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

public class movement_system : MonoBehaviour
{
    public static movement_system instance;

    [Header("Performance Settings")]
    [SerializeField] private int units_per_batch = 50;
    [SerializeField] private float batch_processing_interval = 0.05f; // 50 ms
    [SerializeField] private int recalc_units_per_frame = 20; // Limit recalculations per frame

    [Header("Movement Settings")]
    [SerializeField] private LayerMask obstacle_layer = -1;
    [SerializeField] private float ray_distance = 10f;
    [SerializeField] private float obstacle_avoidance_distance = 3f;

    // Data Structures for better performance
    private List<movement_data> move_data = new List<movement_data>();
    private List<Transform> unit_transforms = new List<Transform>();
    private Queue<int> proc_queue = new Queue<int>();
    private List<int> units_needing_recalc = new List<int>();

    // Event systems for future use
    public static event Action<Bounds> on_terrain_changed;
    public static event Action<Vector3> on_obstacle_added;
    public static event Action<Vector3> on_obstacle_removed;

    [System.Serializable]
    public struct movement_data
    {
        public Vector3 dest;
        public Vector3 current_velocity;
        public Vector3 cached_dir;
        public float max_speed;
        public float turn_speed;
        public movement_type type;
        public bool is_moving;
        public float last_calc_time;
        public bool need_recalc;
    }

    public enum movement_type
    {
        Infantry,
        Vehicle,
        Aircraft
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            StartCoroutine(proc_movement_batches());
            // Subscribe to events when ready
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events when ready
    }

    private IEnumerator proc_movement_batches()
    {
        while (true)
        {
            // Process recalculations first (limited per frame)
            if (units_needing_recalc.Count > 0)
            {
                proc_recalc_batch();
            }

            // Process regular movement batch
            proc_move_batch();

            yield return new WaitForSeconds(batch_processing_interval);
        }
    }

    private void proc_recalc_batch()
    {
        int processed = 0;

        while (units_needing_recalc.Count > 0 && processed < recalc_units_per_frame)
        {
            int unit_i = units_needing_recalc[0];
            units_needing_recalc.RemoveAt(0);

            if (unit_i < move_data.Count && unit_i < unit_transforms.Count)
            {
                Vector3 new_dir = calc_unit_dir(unit_i);

                movement_data data = move_data[unit_i];
                data.cached_dir = new_dir;
                data.last_calc_time = Time.time;
                data.need_recalc = false;
                move_data[unit_i] = data;
            }

            processed++;
        }
    }

    private void proc_move_batch()
    {
        int processed = 0;

        while (proc_queue.Count > 0 && processed < units_per_batch)
        {
            int unit_i = proc_queue.Dequeue();

            if (unit_i < move_data.Count && move_data[unit_i].is_moving)
            {
                proc_single_unit(unit_i);

                // Re-queue if still moving
                if (move_data[unit_i].is_moving)
                {
                    proc_queue.Enqueue(unit_i);
                }
            }
            processed++;
        }
    }

    private void proc_single_unit(int i)
    {
        if (i >= move_data.Count || i >= unit_transforms.Count) return;

        movement_data data = move_data[i];
        Transform transform = unit_transforms[i];

        if (transform == null)
        {
            remove_unit(i);
            return;
        }

        // Use cached direction or mark for recalculation
        Vector3 dir = data.cached_dir;
        if (data.need_recalc || Time.time - data.last_calc_time > 0.5f)
        {
            // Don't recalculate immediately - queue it instead for better performance
            if (!units_needing_recalc.Contains(i))
            {
                units_needing_recalc.Add(i);
                data.need_recalc = true;
                move_data[i] = data;
            }
        }

        // Apply movement with current cached direction
        apply_movement(i, dir);

        // Check if reached destination
        if (Vector3.Distance(transform.position, data.dest) < 1f)
        {
            data.is_moving = false;
            move_data[i] = data;
        }
    }

    private void remove_unit(int i)
    {
        if (i < move_data.Count)
        {
            move_data.RemoveAt(i);
            unit_transforms.RemoveAt(i);
            units_needing_recalc.Remove(i);

            // Update indices in queues since we removed an item
            UpdateQueueIndices(i);
        }
    }

    private void UpdateQueueIndices(int removed_index)
    {
        // Update proc_queue indices
        Queue<int> updated_proc_queue = new Queue<int>();
        while (proc_queue.Count > 0)
        {
            int index = proc_queue.Dequeue();
            if (index > removed_index)
                updated_proc_queue.Enqueue(index - 1);
            else if (index < removed_index)
                updated_proc_queue.Enqueue(index);
            // Skip the removed index
        }
        proc_queue = updated_proc_queue;

        // Update recalc list indices
        for (int i = 0; i < units_needing_recalc.Count; i++)
        {
            if (units_needing_recalc[i] > removed_index)
                units_needing_recalc[i]--;
            else if (units_needing_recalc[i] == removed_index)
            {
                units_needing_recalc.RemoveAt(i);
                i--;
            }
        }
    }

    private Vector3 calc_unit_dir(int i)
    {
        movement_data data = move_data[i];
        Transform transform = unit_transforms[i];

        if (transform == null) return Vector3.zero;

        Vector3 to_target = (data.dest - transform.position).normalized;

        // Ray-casting for obstacle avoidance
        if (Physics.Raycast(transform.position, to_target, out RaycastHit hit, ray_distance, obstacle_layer))
        {
            if (hit.distance < obstacle_avoidance_distance)
            {
                return find_another_dir(transform.position, to_target);
            }
        }
        return to_target;
    }

    private Vector3 find_another_dir(Vector3 pos, Vector3 original_dir)
    {
        float[] angles = { -30f, 30f, -60f, 60f, -90f, 90f, -120f, 120f };

        foreach (float angle in angles)
        {
            Vector3 test_dir = Quaternion.Euler(0, angle, 0) * original_dir;

            if (!Physics.Raycast(pos, test_dir, obstacle_avoidance_distance, obstacle_layer))
            {
                return test_dir;
            }
        }
        return original_dir; // Fallback to original direction if no alternative found
    }

    private void apply_movement(int i, Vector3 dir)
    {
        movement_data data = move_data[i];
        Transform transform = unit_transforms[i];

        switch (data.type)
        {
            case movement_type.Infantry:
                apply_infantry_movement(ref data, transform, dir);
                break;
            case movement_type.Vehicle:
                apply_vehicle_movement(ref data, transform, dir);
                break;
            case movement_type.Aircraft:
                apply_aircraft_movement(ref data, transform, dir);
                break;
        }
        move_data[i] = data;
    }

    private void apply_infantry_movement(ref movement_data data, Transform transform, Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion target_rotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target_rotation,
                data.turn_speed * Time.fixedDeltaTime);
        }

        data.current_velocity = Vector3.MoveTowards(data.current_velocity,
            direction * data.max_speed, 10f * Time.fixedDeltaTime);

        transform.position += data.current_velocity * Time.fixedDeltaTime;
    }

    private void apply_vehicle_movement(ref movement_data data, Transform transform, Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
            float max_turn = data.turn_speed * Time.fixedDeltaTime;
            float actual_turn = Mathf.Clamp(angle, -max_turn, max_turn);

            transform.Rotate(0, actual_turn, 0);
        }

        float alignment = Vector3.Dot(transform.forward, direction);
        float target_speed = Mathf.Clamp01(alignment) * data.max_speed;

        data.current_velocity = Vector3.MoveTowards(data.current_velocity,
            transform.forward * target_speed, 5f * Time.fixedDeltaTime);

        transform.position += data.current_velocity * Time.fixedDeltaTime;
    }

    private void apply_aircraft_movement(ref movement_data data, Transform transform, Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            float turn_amount = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
            float max_turn = data.turn_speed * Time.fixedDeltaTime;
            float actual_turn = Mathf.Clamp(turn_amount, -max_turn, max_turn);

            transform.Rotate(0, actual_turn, 0);

            float bank = -actual_turn * 30f / max_turn;
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x,
                transform.eulerAngles.y, bank);
        }

        data.current_velocity = Vector3.MoveTowards(data.current_velocity,
            transform.forward * data.max_speed, 8f * Time.fixedDeltaTime);

        transform.position += data.current_velocity * Time.fixedDeltaTime;
    }

    // Public API
    public int register_unit(Transform unit_transform, movement_type type, float speed = 5f, float turn_speed = 180f)
    {
        movement_data data = new movement_data
        {
            dest = unit_transform.position,
            current_velocity = Vector3.zero,
            cached_dir = Vector3.zero,
            max_speed = speed,
            turn_speed = turn_speed,
            type = type,
            is_moving = false,
            last_calc_time = Time.time,
            need_recalc = false
        };

        move_data.Add(data);
        unit_transforms.Add(unit_transform);

        return move_data.Count - 1;
    }

    public void move_unit(int unit_id, Vector3 dest)
    {
        if (unit_id < move_data.Count)
        {
            movement_data data = move_data[unit_id];
            data.dest = dest;
            data.is_moving = true;
            data.need_recalc = true;
            move_data[unit_id] = data;

            if (!proc_queue.Contains(unit_id))
            {
                proc_queue.Enqueue(unit_id);
            }
        }
    }

    public void stop_unit(int unit_id)
    {
        if (unit_id < move_data.Count)
        {
            movement_data data = move_data[unit_id];
            data.is_moving = false;
            move_data[unit_id] = data;
        }
    }

    // Helper method to mark units for recalculation (for future event system)
    public void mark_units_for_recalc_in_area(Vector3 center, float radius)
    {
        for (int i = 0; i < unit_transforms.Count; i++)
        {
            if (unit_transforms[i] != null &&
                Vector3.Distance(unit_transforms[i].position, center) <= radius)
            {
                if (!units_needing_recalc.Contains(i))
                {
                    units_needing_recalc.Add(i);
                    movement_data data = move_data[i];
                    data.need_recalc = true;
                    move_data[i] = data;
                }
            }
        }
    }
}
