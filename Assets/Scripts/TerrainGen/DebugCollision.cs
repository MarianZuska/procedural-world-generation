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
        Debug.Log(gameObject + " COLLIDES WITH: " + other.gameObject.tag);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(gameObject + "TRIGGERS");
    }
}
