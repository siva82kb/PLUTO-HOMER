using UnityEngine;

public class AutoPlayer : MonoBehaviour
{

    public float speed = 2.75F;

    Transform ball;

    Rigidbody2D ballRig2D;

    public float topBound = 4.5F;
    public float bottomBound = -4.5F;

    void Start()
    {
        InvokeRepeating("Move", .02F, .02F);
    }

    void Move()
    {

        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("Target").transform;
        }

        ballRig2D = ball.GetComponent<Rigidbody2D>();

        if (ballRig2D.velocity.x > 0)
        {
            if (ball.position.y < this.transform.position.y - .3F)
            {
                transform.Translate(Vector3.down * speed * Time.deltaTime);
            }
            else if (ball.position.y > this.transform.position.y + .3F)
            {
                //move ball up if higher than paddle
                transform.Translate(Vector3.up * speed * Time.deltaTime);
            }

        }

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
