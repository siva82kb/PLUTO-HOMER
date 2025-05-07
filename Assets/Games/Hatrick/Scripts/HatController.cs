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
        MovementTracker.Initialize(this, this.transform.position);

        Vector3 targetWidth = cam.ScreenToWorldPoint(UpperCorner);
        maxwidth = targetWidth.x - hatwidth;
        playSize = maxwidth * 1f;
    }

    void Update()
    {
        position = HatGameController.Instance.AngleToScreen(PlutoComm.angle);
        MovementTracker.UpdatePosition( this.transform.position);

        Vector2 targetPosition = new Vector2(position, this.transform.position.y);
        gameObject.GetComponent<Rigidbody2D>().MovePosition(targetPosition);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Target")
        {
            gamesound.clip = win;
            gamesound.Play();
            Destroy(collision.gameObject);
            HatGameController.Instance.BallCaught();
        }
    }

    private float movementControl(float targetX)
    {
        float val;
        if (Input.GetKey(KeyCode.RightArrow)) val = Math.Abs(targetX) <= 8 ? targetX + 0.2f : targetX;
        else if (Input.GetKey(KeyCode.LeftArrow)) val = Math.Abs(targetX) <= 8 ? targetX - 0.2f : targetX;
        else val = targetX;
        // Clip value to +/-8.
        if (val > 8) val = 8;
        else if (val < -8) val = -8;
        return val;
    }
}
