using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BirdControl : MonoBehaviour
{
    private bool isDead = false;
    public static Rigidbody2D rb2d;
    Animator anime;
    // player controls

    public static float playSize;
    static int FlipAngle = 1;
    static float tempRobot, tempBird;
    bool set = false;


    int totalLife = 5;
    int currentLife = 0;
    bool columnHit;
    public Image life;

    
    float spriteBlinkingTimer = 0.0f;
    
    float spriteBlinkingMiniDuration = 0.1f;
    
    float spriteBlinkingTotalTimer = 0.0f;
    
    float spriteBlinkingTotalDuration = 2f;
    
    public bool startBlinking = false;

    float startTime;
    float endTime;

    float targetAngle;
    Rigidbody2D rig2D;
    public FlappyGameControl FGC;
    void Start()
    {
        rig2D = this.gameObject.GetComponent<Rigidbody2D>();

        startTime = 0;
        endTime = 0;
        currentLife = 0;
        anime = GetComponent<Animator>();
        rb2d = GetComponent<Rigidbody2D>();

        playSize = 2.3f + 5.5f;




    }
    void FixedUpdate()
    {
       
        gameData.events = Array.IndexOf(gameData.tukEvents, "moving");
        //checkPlayerMovement();


        if (startTime < 2)
        {
            startTime += Time.deltaTime;
        }
        if (startBlinking == true)
        {
            SpriteBlinkingEffect();

        }
        if (!isDead && !FGC.gameOver)
        {
            if (columnHit)
            {
                anime.SetTrigger("Idle");
                columnHit = false;
            }
            if (gameData.isAROMEnabled) { 
            targetAngle = approxRollingAverage(targetAngle, playerMovementAreaAROM(PlutoComm.angle));
            }
            else { 
            targetAngle = approxRollingAverage(targetAngle, Angle2Screen(PlutoComm.angle));
            }
            transform.position = new Vector2(Mathf.SmoothStep(-13, -7, startTime / 2), Mathf.Clamp(targetAngle, -2.5f, 7));
        }
        else if (FGC.gameOver)
        {
            endTime += Time.deltaTime;

            float y = Mathf.Abs(2 * Mathf.Sin(2 * endTime));
            transform.localPosition = new Vector3(transform.position.x + 3 * Time.deltaTime, transform.position.y, 0);
        }

        anime.updateMode = AnimatorUpdateMode.UnscaledTime;

    }
    //private void checkPlayerMovement()
    //{
    //    Vector3 currentPlayerPosition = transform.position;
    //    float playerDistanceMoved = Vector3.Distance(currentPlayerPosition, previousPlayerPosition); // Calculate the distance moved by the player
    //    if (playerDistanceMoved > 0.001f)
    //    {
    //        if (movementCoroutine == null)
    //        {
    //            movementCoroutine = StartCoroutine(trackMovementTime());
    //        }
    //    }
    //    else
    //    {
    //        if (movementCoroutine != null)
    //        {
    //            StopCoroutine(movementCoroutine);
    //            movementCoroutine = null;
    //        }
    //    }
    //    previousPlayerPosition = currentPlayerPosition;
    //}

    //private IEnumerator trackMovementTime()
    //{
    //    while (true)
    //    {
    //        playerMovementTime += Time.deltaTime;
    //        gameData.moveTime = playerMovementTime;
    //        yield return null;
    //    }
    //}
    float approxRollingAverage(float avg, float new_sample)
    {

        avg = avg * 0.9f + 0.1f * new_sample;

        return avg;
    }
    public void SpriteBlinkingEffect()
    {
        spriteBlinkingTotalTimer += Time.deltaTime;
        if (spriteBlinkingTotalTimer >= spriteBlinkingTotalDuration)
        {
            startBlinking = false;
            spriteBlinkingTotalTimer = 0.0f;
            this.gameObject.GetComponent<SpriteRenderer>().enabled = true;   
            return;
        }

        spriteBlinkingTimer += Time.deltaTime;
        if (spriteBlinkingTimer >= spriteBlinkingMiniDuration)
        {
            spriteBlinkingTimer = 0.0f;
            if (this.gameObject.GetComponent<SpriteRenderer>().enabled == true)
            {
                this.gameObject.GetComponent<SpriteRenderer>().enabled = false;  
            }
            else
            {
                this.gameObject.GetComponent<SpriteRenderer>().enabled = true;   
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

        //Debug.Log("Collion " + collision.gameObject.tag);
        if (collision.gameObject.tag == "TopCollider" || collision.gameObject.tag == "BottomCollider")
        {
            gameData.events = Array.IndexOf(gameData.tukEvents, "collided");

            startBlinking = true;
            currentLife++;
            life.fillAmount = ((float)currentLife / totalLife);
            // anime.SetTrigger("Die");
            columnHit = true;
            if (currentLife >= totalLife)
            {

                FlappyGameControl.instance.gameduration = -1;
                FlappyGameControl.instance.gameOver = true;
                anime.SetTrigger("Die");
                isDead = true;
                anime.SetTrigger("Die");
            }
            //gameData.birdCollided = true;
        }
    }

    public static float Angle2Screen(float angle)
    {
        float tmin = AppData.Instance.selectedMechanism.currRom.promMin;
        float tmax = AppData.Instance.selectedMechanism.currRom.promMax;
        return (-2.3f + (angle - tmin) * (playSize) / (tmax - tmin));
    }

    public static float playerMovementAreaAROM(float angle)
    {
        //ROM aromAng = new ROM(AppData.selectedMechanism);
        float tmin = AppData.Instance.selectedMechanism.currRom.aromMin;
        float tmax = AppData.Instance.selectedMechanism.currRom.aromMax;
        return (-2.3f + (angle - tmin) * (playSize) / (tmax - tmin));
    }
}
