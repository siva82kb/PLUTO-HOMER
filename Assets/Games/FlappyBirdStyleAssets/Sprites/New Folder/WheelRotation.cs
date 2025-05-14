using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelRotation : MonoBehaviour
{
    void Start()
    {

    }

    void Update()
    {

        float roatSpeed = Random.Range(-150, -100);
        transform.Rotate(0, 0, roatSpeed * Time.deltaTime); //rotates 50 degrees per second around z axis
    }
}
