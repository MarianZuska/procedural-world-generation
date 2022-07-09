using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5;
    public float boostMultiplier = 3;
    public float rotationRate = 50;

    private Rigidbody rbody;

    private Vector3 moveInput;
    private Vector3 rotationInput;

    private Quaternion prevRotation;

    private bool boostActive = false;

    void Awake()
    {   
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        TryGetComponent(out rbody);
    }


    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.None) ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = Cursor.lockState == CursorLockMode.None;
            
            Debug.Log( Cursor.lockState + " " + Cursor.visible);
        }

        moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        rotationInput = new Vector3(-Input.GetAxis("Mouse Y") * 1.5f, Input.GetAxis("Mouse X") * 1.5f, 0);

        boostActive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private void FixedUpdate()
    {
        Vector3 movement = transform.right * (moveInput.x) + transform.forward * (moveInput.z);
        movement *= moveSpeed * Time.fixedDeltaTime;
        if (boostActive) movement *= boostMultiplier;

        rbody.position += movement;

        if (rotationInput.sqrMagnitude > 0)
            transform.Rotate(rotationInput * rotationRate);

        //moveSpeed += Time.deltaTime * 0.5f;
    }
}
