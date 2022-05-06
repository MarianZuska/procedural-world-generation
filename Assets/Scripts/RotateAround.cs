using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour
{
    public Transform target;
    public int rotationSpeed = 1;
    public int scrollSpeed = 10;
    public int moveSpeed = 10;

    void Update()
    {
        //scale
        gameObject.transform.Translate(0, 0, Input.GetAxis("Mouse ScrollWheel") * scrollSpeed);
        //rotate
        /*if (Input.GetMouseButton(0)) {
            transform.RotateAround(target.position, transform.right, -Input.GetAxis("Mouse Y") * rotationSpeed);
            transform.RotateAround(target.position, transform.up, Input.GetAxis("Mouse X") * rotationSpeed);
        }*/
        //transform.position += transform.forward * moveSpeed * Time.deltaTime * Input.GetAxis("Vertical");

    }
}
