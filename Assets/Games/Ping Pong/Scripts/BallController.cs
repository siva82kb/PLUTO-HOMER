using UnityEngine;
using System.Collections;
using System;



public class BallController : MonoBehaviour
{

    //speed of the ball
    public  float speed = 1.5F;

    //the initial direction of the ball
    private Vector2 spawnDir;
    Vector2 preVel;

    //ball's components
    Rigidbody2D rig2D;
    private PongGameController PGC;
    public AudioClip[] audioClips;
    int rand = 1;
    void Start()
    {
        rig2D = this.gameObject.GetComponent<Rigidbody2D>();
        PGC = GameObject.FindAnyObjectByType<PongGameController>();
        //speed = speed + (0.1f * AppData.Instance.speedData.gameSpeed);
        int rand = UnityEngine.Random.Range(1, 5);

        if (rand == 1) spawnDir = new Vector2(-1, 1);
        else if (rand == 2) spawnDir = new Vector2(-1, -1);
        else if (rand == 3) spawnDir = new Vector2(-1, 1);
        else if (rand == 4) spawnDir = new Vector2(-1, -1);

        rig2D.velocity = (spawnDir * speed);
       

    }

    void FixedUpdate()
    {
        if(PGC.isFinished){
            rig2D.velocity=new Vector2(0f,0f);
        }
        else preVel = rig2D.velocity;

    }
    void playAudio(int clipNumber)
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = audioClips[clipNumber];
        audio.Play();
    }

    public void initVelocity(Vector2 velocity)
    {
        rig2D.velocity = velocity;
    }

    Vector2 GetDirectionToTarget(Vector2 ballPos, Vector2 targetPos)
    {
        Vector2 dir = (targetPos - ballPos).normalized;
        return dir;
    }


    void OnCollisionEnter2D(Collision2D col)
    {
        playAudio(0);

        if (col.gameObject.CompareTag("Enemy"))
        {
            float y = launchAngle(transform.position,
                                col.transform.position,
                                col.collider.bounds.size.y);

            // Determine wall to bounce based on y 
            float wallY = y > 0 ? 4.5f : -4.5f;
            PGC.enemyHit = true;
            PGC.enemyScore++;
            PGC.nTargets++;
            float reflectedY = 2 * wallY - PGC.targetPosition.y;
            Vector2 reflectedTarget = new Vector2(PGC.targetPosition.x, reflectedY);

            Vector2 launchDir = (reflectedTarget - (Vector2)transform.position).normalized;

            initVelocity(launchDir * speed);
        }

        if (col.gameObject.tag == "Player")
        {
           //  PGC.targetPosition= new Vector2(5.95f, UnityEngine.Random.Range(-4.5f, 4.5f));

            
            float y = launchAngle(transform.position,
                                col.transform.position,
                                col.collider.bounds.size.y);
            Vector2 d = new Vector2(-1, y).normalized;
            initVelocity(d * speed);
            PGC.playerScore++;
            PGC.BallHitted();


        }
        if (col.gameObject.name == "BottomBound")
        {
            rig2D.velocity = new Vector2(rig2D.velocity.x, Mathf.Abs(preVel.y));
        }
        if (col.gameObject.name == "TopBound")
        {
            rig2D.velocity = new Vector2(rig2D.velocity.x, -Mathf.Abs(preVel.y));
        }
    }

    float launchAngle(Vector2 ballPos, Vector2 paddlePos,
                    float paddleHeight)
    {
        return Mathf.Clamp(0.2f * Mathf.Sign(ballPos.y - paddlePos.y) + (ballPos.y - paddlePos.y) / paddleHeight, -2, 2);
    }

    public void pauseBall(){
        Time.timeScale = 0f;
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           