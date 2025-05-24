using GLTFast;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class unit_supply : unit_main
{
	[SerializeField] private int gather_amount = 10;
	[SerializeField] private int carrying_amount = 0;
	[SerializeField] private int max_capacity = 10;
	[SerializeField] private int gathering_speed = 1;
	[SerializeField] private float deposit_speed = 0.5f;
	[SerializeField] private funds_building assigned_yard;
	[SerializeField] private supply_base supply_base;


	private enum gather_states { Idle, Moving_to_yard, Gathering, Returning_to_base };
	private gather_states current_state = gather_states.Idle;

	private WaitForSeconds gathering_wait;
	private WaitForSeconds deposit_wait;

	private Coroutine current_gathering;
	private Coroutine current_deposit;

	void Awake()
	{
		gathering_wait = new WaitForSeconds(gathering_speed);
		deposit_wait = new WaitForSeconds(deposit_speed);
	}
	protected override void init_unit()
	{
		unit_base_hp = 600;
		unit_hp = 600;
		can_attack = false;
		unit_range = 0f;
		unit_damage = 0;
	}
	private void Update()
	{
		switch (current_state)
		{
			case gather_states.Moving_to_yard:
				if (assigned_yard != null && (transform.position - assigned_yard.transform.position).sqrMagnitude < 1f * 1f)
				{
					current_state = gather_states.Gathering;
					start_gathering();
				}
				break;
			case gather_states.Returning_to_base:
				if (supply_base != null && (transform.position - supply_base.transform.position).sqrMagnitude < 1f * 1f)
				{
					start_deposit();
				}
				break;
		}
	}

	private void start_deposit()
	{
		stop_unit();
		current_state = gather_states.Idle;
		current_deposit = StartCoroutine(deposit_resources());
		// TODO add visual feedback
	}

	private IEnumerator deposit_resources()
	{
		yield return deposit_wait;
		if (carrying_amount > 0)
		{
			supply_base.add_money(carrying_amount);
			carrying_amount = 0;
		}
		if (assigned_yard != null && !assigned_yard.is_empty())
		{
			current_state = gather_states.Moving_to_yard;
			set_move_order(assigned_yard.transform.position);
		}
		else
		{
			current_state = gather_states.Idle;
		}
	}

	public void set_assigned_yard(funds_building yard)
	{
		assigned_yard = yard;
		set_move_order(yard.transform.position);
		current_state = gather_states.Moving_to_yard;
	}
	private void start_gathering()
	{
		current_state = gather_states.Gathering;
		stop_unit();
		current_gathering = StartCoroutine(gather_resources());
	}

	private IEnumerator gather_resources()
	{
		while (carrying_amount < max_capacity && current_state == gather_states.Gathering)
		{
			yield return gathering_wait;
			if (assigned_yard == null || assigned_yard.is_empty())
			{
				break;
			}
			carrying_amount += assigned_yard.get_money(gather_amount);
		}
		if (carrying_amount > 0 && supply_base != null)
		{
			current_state = gather_states.Returning_to_base;
			set_move_order(supply_base.transform.position);
		}
		else
		{
			current_state = gather_states.Idle;
		}
	}
	public override void set_move_order(Vector3 dest)
	{
		if (current_gathering != null)
		{
			StopCoroutine(current_gathering);
			current_gathering = null;
		}
		if (current_deposit != null)
		{
			StopCoroutine(current_deposit);
			current_deposit = null;
		}
		current_state = gather_states.Idle;
		base.set_move_order(dest);
	}
	public void set_supply_base(supply_base base_building)
	{
		supply_base = base_building;
	}
}
