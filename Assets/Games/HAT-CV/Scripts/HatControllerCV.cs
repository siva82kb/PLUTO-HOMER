using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HatControllerCV : MonoBehaviour
{
    public float maxwidth;
    public static float playSize;
    float position = 0f;

    public Camera cam;
    public AudioSource gamesound;
    public AudioClip win;
    public AudioClip loose;
    private bool side, mech;

    void Start()
    {
        Vector3 UpperCorner = new Vector3(Screen.width, Screen.height, 0);
        float hatwidth = GameObject.Find("HatFrontSprite").GetComponent<Renderer>().bounds.extents.x;
        // MovementTracker.Initialize(this, this.transform.position);

        Vector3 targetWidth = cam.ScreenToWorldPoint(UpperCorner);
        maxwidth = targetWidth.x - hatwidth;
        playSize = maxwidth * 1f;
        side = AppData.Instance.IsTrainingSide("RIGHT");
        mech = AppData.Instance.selectedMechanism.IsMechanism("HOC");

    }

    void Update()
    {
         position = HatGameControllerCV.Instance.AngleToScreen(PlutoComm.angle);

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
            HatGameControllerCV.Instance.BallCaught();
        }
    }

}
