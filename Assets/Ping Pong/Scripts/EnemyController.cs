using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{

    //Speed of the enemy
    public static float speed = 1.9F;

    //the ball
    Transform ball;

    //the ball's rigidbody 2D
    Rigidbody2D ballRig2D;

    //bounds of enemy
    public float topBound = 4.5F;
    public float bottomBound = -4.5F;
    public static float stopWatch;

    // Use this for initialization
    private void Awake()
    {
        //if (AppData.subjd.side == "LEFT")
        //{
        //	this.transform.position = new Vector2(6, 0);
        //}

        stopWatch = 0;
    }
    void Start()
    {
        //Continously Invokes Move every x seconds (values may differ)
        InvokeRepeating("Move", .02F, .02F);
    }
    private void Update()
    {
        stopWatch += Time.deltaTime;

        Debug.Log(stopWatch);
    }

    // Movement for the paddle
    void Move()
    {

        float currSpeed = Mathf.Clamp(speed - (stopWatch / 90 * speed * 0.3f), 0.6f * speed, speed);
        Debug.Log(currSpeed);
        //finding the ball
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("Target").transform;
        }

        //setting the ball's rigidbody to a variable
        ballRig2D = ball.GetComponent<Rigidbody2D>();

        //checking x direction of the ball
        if (ballRig2D.velocity.x < 0)
        {

            //checking y direction of ball
            if (ball.position.y < this.transform.position.y - .3F)
            {
                //move ball down if lower than paddle
                transform.Translate(Vector3.down * currSpeed * Time.deltaTime);
            }
            else if (ball.position.y > this.transform.position.y + .3F)
            {
                //move ball up if higher than paddle
                transform.Translate(Vector3.up * currSpeed * Time.deltaTime);
            }

        }

        //set bounds of enemy
        if (transform.position.y > topBound)
        {
            transform.position = new Vector3(transform.position.x, topBound, 0);
        }
        else if (transform.position.y < bottomBound)
        {
            transform.position = new Vector3(transform.position.x, bottomBound, 0);
        }
    }
}
