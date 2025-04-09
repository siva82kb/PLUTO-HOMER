using UnityEngine;

[RequireComponent(typeof(LineRenderer))]

public class BaallTrajectoryPlotter : MonoBehaviour
{

    public int reflections;
    public float maxLength;

    private LineRenderer lineRenderer;
    private Ray2D ray;
    private RaycastHit2D hit;
    public float targetPosition = 0;
    public Vector2 ballVelocity;

    public float ballDistance;

    public bool ishittingplayer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {

        //drawTraj();

    }

    private void FixedUpdate()
    {
        ballVelocity = GetComponent<Rigidbody2D>().velocity;

        if (transform.position.x < 5 && ballVelocity.x > 0 && transform.position.x > -5)
        {
            drawTraj();
        }
        else
        {
            lineRenderer.positionCount = 0;

        }

    }


    public void drawTraj()
    {


        ray = new Ray2D(transform.position, ballVelocity);
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, transform.position);
        float remainingLength = maxLength;
        Physics2D.queriesHitTriggers = true;


        for (int i = 0; i < reflections; i++)
        {



            hit = Physics2D.Raycast(ray.origin + ray.direction.normalized, ray.direction);

            lineRenderer.positionCount += 1;
            remainingLength -= Vector3.Distance(ray.origin, hit.point);
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, hit.point);


            if (hit.collider.tag == "Player")
            {
                ishittingplayer = true;
            }
            else
            {
                ishittingplayer = false;

            }
            if (hit.collider.tag == "Player" || hit.collider.tag == "PlayerBound")
            {
                targetPosition = hit.point.y;

                ballDistance = maxLength - remainingLength;


                break;
            }
            else
            {
                ishittingplayer = false;
                Vector2 reflect = Vector2.Reflect(ray.direction, hit.normal);
                ray = new Ray2D(hit.point + reflect, reflect);

            }

        }
    }
}



