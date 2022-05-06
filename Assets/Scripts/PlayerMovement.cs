using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5;
    public float rotationRate = 50;

    private Rigidbody rbody;

    private Vector3 moveInput;
    private Vector3 rotationInput;

    private Quaternion prevRotation;
    
    // Start is called before the first frame update
    void Awake()
    {
        TryGetComponent(out rbody);
    }

    // Update is called once per frame
    void Update()
    {
        moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        rotationInput = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
        
    }

    private void FixedUpdate()
    {
        Vector3 movement = transform.right * (moveInput.x) + 
                   transform.forward * (moveInput.z);
        movement *= moveSpeed * Time.fixedDeltaTime;
        
        rbody.position += movement;
        //transform.position += transform.forward * moveSpeed * Time.deltaTime * Input.GetAxis("Vertical");
        //transform.position += transform.right * moveSpeed * Time.deltaTime * Input.GetAxis("Horizontal");

        //rotation
        //transform.Rotate(0, Input.GetAxis("Horizontal") * Time.deltaTime * rotateSpeed, 0, Space.World);
        if (rotationInput.sqrMagnitude > 0)
        {
            Vector3 rotationVec = rotationInput * rotationRate;
            
            //rbody.MoveRotation(rbody.rotation * Quaternion.Euler(rotationVec));
            transform.Rotate(rotationVec.x, rotationVec.y, rotationVec.z);
            
            //transform.RotateAround(target.position, transform.right, -Input.GetAxis("Mouse Y") * rotationSpeed);
            //transform.RotateAround(target.position, transform.up, Input.GetAxis("Mouse X") * rotationSpeed);

        }
        
        //moveSpeed += Time.deltaTime * 0.5f;
    }
}
