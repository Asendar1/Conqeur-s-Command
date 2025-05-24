using System.Runtime.CompilerServices;
using UnityEditor.Rendering;
using UnityEngine;

public class funds_building : MonoBehaviour
{
    [SerializeField] private GameObject group_1;
    [SerializeField] private GameObject group_2;
    [SerializeField] private GameObject group_3;
    [SerializeField] private GameObject group_4;
    [SerializeField] private int money = 6000;
    // ! for testing visuals of the groups ONLY
    // [SerializeField] bool test = false;
    private int max_money = 6000;

    // void Update()
    // {
    //     if (test)
    //         update_fund_indicators();
    // }
    public int get_money(int amount)
    {
        update_fund_indicators();
        if (money >= amount)
        {
            money -= amount;
            update_fund_indicators();
            return amount;
        }
        else
        {
            int available = money;
            money = 0;
            update_fund_indicators();
            return available;
        }
    }
    private void update_fund_indicators()
    {
        if (money < max_money * 0.75)
            group_1.SetActive(false);
        if (money < max_money * 0.5)
            group_2.SetActive(false);
        if (money < max_money * 0.25)
            group_3.SetActive(false);
        if (money <= 0)
            group_4.SetActive(false);
    }
    public bool is_empty()
    {
        return money <= 0;
    }
}
