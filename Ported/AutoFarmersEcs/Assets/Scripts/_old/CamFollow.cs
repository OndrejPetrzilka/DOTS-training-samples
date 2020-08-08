using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;

public class CamFollow : MonoBehaviour
{
    public Vector2 viewAngles;
    public float viewDist;
    public float mouseSensitivity;

    public bool freeCamera = false;
    public bool rotate = false;
    public float moveSpeed = 1;
    public float zoomSpeed = 1;

    void Start()
    {
        viewAngles.y = transform.localEulerAngles.x;
        viewAngles.x = transform.localEulerAngles.y;
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            freeCamera = !freeCamera;
        }

        if (freeCamera)
        {
            if (Input.GetKey(KeyCode.W))
            {
                Move(Vector3.forward);
            }
            if (Input.GetKey(KeyCode.S))
            {
                Move(Vector3.back);
            }
            if (Input.GetKey(KeyCode.A))
            {
                Move(Vector3.left);
            }
            if (Input.GetKey(KeyCode.D))
            {
                Move(Vector3.right);
            }
            if (Input.mouseScrollDelta.y != 0)
            {
                var pos = transform.position;
                pos.y = Mathf.Clamp(pos.y * Mathf.Pow(1 + zoomSpeed, -Input.mouseScrollDelta.y), 1, 50);
                transform.position = pos;
            }
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    int value = i == 0 ? 10 : i;
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        value = value * value;
                    }
                    Time.timeScale = value;
                }
            }
        }
        else
        {
            Vector3 pos = FarmerManager.instance.firstFarmer.GetSmoothWorldPos();
            transform.position = pos - transform.forward * viewDist;
        }

        transform.rotation = Quaternion.Euler(viewAngles.y, viewAngles.x, 0f);
        if (rotate)
        {
            viewAngles.x += Input.GetAxis("Mouse X") * mouseSensitivity / Screen.height;
            viewAngles.y -= Input.GetAxis("Mouse Y") * mouseSensitivity / Screen.height;
        }
        viewAngles.y = Mathf.Clamp(viewAngles.y, 7f, 80f);
        viewAngles.x -= Mathf.Floor(viewAngles.x / 360f) * 360f;
    }

    private void Move(Vector3 localDirection)
    {
        // Move only in flat plane
        var worldDirection = transform.TransformDirection(localDirection);
        worldDirection.y = 0;
        worldDirection = worldDirection.normalized;
        transform.position += worldDirection * moveSpeed * Time.unscaledDeltaTime;
    }
}
