using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HatController : MonoBehaviour
{
    
    public float maxwidth;
    public static float playSize;
    float position = 0f;

    public Camera cam;
    public AudioSource gamesound;
    public AudioClip win;
    public AudioClip loose;

    void Start()
    {
        Vector3 UpperCorner = new Vector3(Screen.width, Screen.height, 0);
        float hatwidth = GameObject.Find("HatFrontSprite").GetComponent<Renderer>().bounds.extents.x;
        Vector3 targetWidth = cam.ScreenToWorldPoint(UpperCorner);
        maxwidth = targetWidth.x - hatwidth;
        //playSize = maxwidth * 0.9f;
        playSize = maxwidth * 1f;
    }

    void Update()
    {
        position = movementControl(this.transform.position.x);
        Vector2 targetPosition = new Vector2(position, this.transform.position.y);
        gameObject.GetComponent<Rigidbody2D>().MovePosition(targetPosition);
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Target")
        {
            gamesound.clip = win;
            gamesound.Play();
            HatGameController.instance.score++;
            HatGameController.instance.SpawnTarget();
            Destroy(collision.gameObject);
        }
    }
    private float movementControl(float targetX)
    {
        float val;
        if (Input.GetKey(KeyCode.RightArrow))
        {
           
            if ((targetX >= 0f)|| (targetX<8)) val = 0.2f + targetX;
            else val = 0.2f - targetX;

            return val;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            if ((targetX <= 0f) || (targetX < -8)) val = -0.2f + targetX;
            else val = targetX - 0.2f;

            return val;
        }
        else return targetX;
    }
}
