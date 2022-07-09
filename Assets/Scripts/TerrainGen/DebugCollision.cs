using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugCollision : MonoBehaviour
{
    void Start()
    {
        //Debug.Log("Startup");
    }

    private void OnCollisionEnter(Collision other)
    {
        //Debug.Log("COLLIDES");
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("TRIGGERS");
    }
}
