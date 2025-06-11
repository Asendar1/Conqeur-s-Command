using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class unit_movement : MonoBehaviour
{
    private unit_main unit;
    private movement_system movement_system;

    void Start()
    {
        movement_system = movement_system.instance;
    }


}
