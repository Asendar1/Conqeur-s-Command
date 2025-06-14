using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

public class movement_system : MonoBehaviour
{
    public static movement_system instance;

    [Header("Performance Settings")]
    [SerializeField] private int units_per_batch = 25; // Reduced for better responsiveness
    [SerializeField] private float batch_processing_interval = 0.02f; // Faster batching (50 FPS)
    [SerializeField] private int recalc_units_per_frame = 10; // Reduced recalc load

    [Header("Movement Settings")]
    [SerializeField] private LayerMask obstacle_layer = -1;
    [SerializeField] private float ray_distance = 10f;
    [SerializeField] private float obstacle_avoidance_distance = 3f;

    private List<movement_data> move_data = new List<movement_data>();
    private List<Transform> unit_transforms = new List<Transform>();
    private Queue<int> proc_queue = new Queue<int>();
    private List<int> units_needing_recalc = new List<int>();

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
        public float last_move_time; // Track when unit was last moved
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
            StartCoroutine(apply_all_movement()); // Separate smooth movement loop
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Separate coroutine for smooth movement application
    private IEnumerator apply_all_movement()
    {
        while (true)
        {
            // Apply movement to ALL moving units every frame for smooth movement
            for (int i = 0; i < move_data.Count; i++)
            {
                if (i < unit_transforms.Count && move_data[i].is_moving && unit_transforms[i] != null)
                {
                    apply_smooth_movement(i);
                }
            }
            yield return null; // Every frame
        }
    }

    private IEnumerator proc_movement_batches()
    {
        while (true)
        {
            // Process recalculations in small batches
            if (units_needing_recalc.Count > 0)
            {
                proc_recalc_batch();
            }

            // Process logic updates in batches (not movement application)
            proc_logic_batch();

            yield return new WaitForSeconds(batch_processing_interval);
        }
    }

    private void apply_smooth_movement(int i)
    {
        movement_data data = move_data[i];
        Transform transform = unit_transforms[i];

        if (transform == null)
        {
            remove_unit(i);
            return;
        }

        // Use frame-rate independent time
        float delta_time = Time.deltaTime;
        Vector3 direction = data.cached_dir;

        if (direction == Vector3.zero)
        {
            // No direction calculated yet, calculate basic direction
            direction = (data.dest - transform.position).normalized;
        }

        // Apply movement based on type with proper deltaTime
        switch (data.type)
        {
            case movement_type.Infantry:
                apply_infantry_movement_smooth(ref data, transform, direction, delta_time);
                break;
            case movement_type.Vehicle:
                apply_vehicle_movement_smooth(ref data, transform, direction, delta_time);
                break;
            case movement_type.Aircraft:
                apply_aircraft_movement_smooth(ref data, transform, direction, delta_time);
                break;
        }

        // Check if reached destination
        if (Vector3.Distance(transform.position, data.dest) < 1f)
        {
            data.is_moving = false;
            data.current_velocity = Vector3.zero;
        }

        data.last_move_time = Time.time;
        move_data[i] = data;
    }

    private void apply_infantry_movement_smooth(ref movement_data data, Transform transform, Vector3 direction, float delta_time)
    {
        if (direction != Vector3.zero)
        {
            // Smooth rotation
            Quaternion target_rotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target_rotation,
                data.turn_speed * delta_time);
        }

        // Smooth acceleration
        Vector3 target_velocity = direction * data.max_speed;
        data.current_velocity = Vector3.MoveTowards(data.current_velocity, target_velocity,
            10f * delta_time); // Acceleration rate

        // Apply position change
        transform.position += data.current_velocity * delta_time;
    }

    private void apply_vehicle_movement_smooth(ref movement_data data, Transform transform, Vector3 direction, float delta_time)
    {
        if (direction != Vector3.zero)
        {
            // Limited turning for vehicles
            float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
            float max_turn = data.turn_speed * delta_time;
            float actual_turn = Mathf.Clamp(angle, -max_turn, max_turn);

            transform.Rotate(0, actual_turn, 0);
        }

        // Vehicle moves forward based on alignment
        float alignment = Vector3.Dot(transform.forward, direction);
        float target_speed = Mathf.Clamp01(alignment) * data.max_speed;

        // Smooth acceleration
        float current_speed = data.current_velocity.magnitude;
        float new_speed = Mathf.MoveTowards(current_speed, target_speed, 5f * delta_time);

        data.current_velocity = transform.forward * new_speed;
        transform.position += data.current_velocity * delta_time;
    }

    private void apply_aircraft_movement_smooth(ref movement_data data, Transform transform, Vector3 direction, float delta_time)
    {
        if (direction != Vector3.zero)
        {
            // Banking turns
            float turn_amount = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
            float max_turn = data.turn_speed * delta_time;
            float actual_turn = Mathf.Clamp(turn_amount, -max_turn, max_turn);

            transform.Rotate(0, actual_turn, 0);

            // Banking effect
            float bank = -actual_turn * 30f / max_turn;
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x,
                transform.eulerAngles.y, bank);
        }

        // Smooth acceleration
        Vector3 target_velocity = transform.forward * data.max_speed;
        data.current_velocity = Vector3.MoveTowards(data.current_velocity, target_velocity,
            8f * delta_time);

        transform.position += data.current_velocity * delta_time;
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

    private void proc_logic_batch()
    {
        int processed = 0;

        while (proc_queue.Count > 0 && processed < units_per_batch)
        {
            int unit_i = proc_queue.Dequeue();

            if (unit_i < move_data.Count && move_data[unit_i].is_moving)
            {
                proc_single_unit_logic(unit_i);

                // Re-queue if still moving
                if (move_data[unit_i].is_moving)
                {
                    proc_queue.Enqueue(unit_i);
                }
            }
            processed++;
        }
    }

    private void proc_single_unit_logic(int i)
    {
        if (i >= move_data.Count || i >= unit_transforms.Count) return;

        movement_data data = move_data[i];
        Transform transform = unit_transforms[i];

        if (transform == null)
        {
            remove_unit(i);
            return;
        }

        // Check if we need to recalculate path
        if (data.need_recalc || Time.time - data.last_calc_time > 0.3f) // More frequent recalc
        {
            if (!units_needing_recalc.Contains(i))
            {
                units_needing_recalc.Add(i);
                data.need_recalc = true;
                move_data[i] = data;
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
        float[] angles = { -30f, 30f, -60f, 60f, -90f, 90f };

        foreach (float angle in angles)
        {
            Vector3 test_dir = Quaternion.Euler(0, angle, 0) * original_dir;

            if (!Physics.Raycast(pos, test_dir, obstacle_avoidance_distance, obstacle_layer))
            {
                return test_dir;
            }
        }
        return original_dir;
    }

    private void remove_unit(int i)
    {
        if (i < move_data.Count)
        {
            move_data.RemoveAt(i);
            unit_transforms.RemoveAt(i);
            units_needing_recalc.Remove(i);
            UpdateQueueIndices(i);
        }
    }

    private void UpdateQueueIndices(int removed_index)
    {
        Queue<int> updated_proc_queue = new Queue<int>();
        while (proc_queue.Count > 0)
        {
            int index = proc_queue.Dequeue();
            if (index > removed_index)
                updated_proc_queue.Enqueue(index - 1);
            else if (index < removed_index)
                updated_proc_queue.Enqueue(index);
        }
        proc_queue = updated_proc_queue;

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

    // Public API remains the same
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
            need_recalc = false,
            last_move_time = Time.time
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
            data.current_velocity = Vector3.zero;
            move_data[unit_id] = data;
        }
    }

    public bool is_moving(int unit_id)
    {
        if (unit_id < move_data.Count)
        {
            return move_data[unit_id].is_moving;
        }
        return false;
    }
}
