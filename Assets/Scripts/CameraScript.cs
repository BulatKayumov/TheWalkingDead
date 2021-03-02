using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float angleSpeed = 2f;
    [SerializeField] private float zoomSpeed = 200f;
    [SerializeField] private float x = 20000f;
    [SerializeField] private float y = 20000f;
    [SerializeField] private float z = -15000f;
    [SerializeField] private float angle = 20f;


    private void Start()
    {
        transform.position = new Vector3(x, y, z);
        transform.rotation = Quaternion.Euler(new Vector3(angle, 0, 0));
    }
    private void Update()
    {
        transform.position += new Vector3(transform.forward.x, 0, transform.forward.z) * Input.GetAxis("Vertical") * speed;
        transform.position += new Vector3(transform.forward.z, 0, -transform.forward.x) * Input.GetAxis("Horizontal") * speed;
        transform.RotateAround(transform.position, new Vector3(0, Input.GetAxis("Rotation"), 0), angleSpeed);
        transform.position += new Vector3(transform.forward.x, transform.forward.y, transform.forward.z) * Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
    }
}
