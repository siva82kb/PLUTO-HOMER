using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BirdControl : MonoBehaviour
{
    public static Rigidbody2D rb2d;
    public Image life;
    public FlappyGameControl FGC;

    public bool set = false;
    private bool isDead = false;

    int totalLife = 5;
    int currentLife = 0;
    bool columnHit;

    public float spriteBlinkingTimer = 0.0f;
    public float spriteBlinkingMiniDuration = 0.1f;
    public float spriteBlinkingTotalTimer = 0.0f;
    public float spriteBlinkingTotalDuration = 2f;
    public bool startBlinking = false;
    float targetAngle, position;
    float startTime, PLAYSIZE;
    float endTime;

    private float[] arom;
    private float[] prom, aprom;
    private bool side, mech;


    void Start()
    {
        // PLAYSIZE = Camera.main.orthographicSize * Camera.main.aspect;
        float fullHeight = Camera.main.orthographicSize * 2f; // Full camera height in world units
        PLAYSIZE = fullHeight * 0.8f; // 80% of the camera height
        startTime = 0;
        endTime = 0;
        currentLife = 0;
        rb2d = GetComponent<Rigidbody2D>();
        // MovementTracker.Initialize(this, this.transform.position);

        Time.timeScale = 0f;
        // Set current AROM and PROM.
        arom = AppData.Instance.selectedMechanism.CurrentArom;
        prom = AppData.Instance.selectedMechanism.CurrentProm;
        aprom = AppData.Instance.selectedMechanism.CurrentAProm;

        
        side = AppData.Instance.IsTrainingSide("RIGHT");
        mech = AppData.Instance.selectedMechanism.IsMechanism("HOC");
        
    }
    void Update()
    {
        if(FGC.isGameStarted && !FGC.isGamePaused && !FGC.isGameFinished) Time.timeScale=1f;

        Debug.Log($" min : {AngleToScreen(arom[0])}, max : { AngleToScreen( arom[1])}");

        // MovementTracker.UpdatePosition(this.transform.position);


    }
    void FixedUpdate()
    {
        if (startTime < 2)
        {
            startTime += Time.deltaTime;
        }
        if (startBlinking == true)
        {
            SpriteBlinkingEffect();

        }
        if(FGC.isGameStarted){
            
            targetAngle = approxRollingAverage(targetAngle, AngleToScreen((PlutoComm.angle)));
        transform.position = new Vector2(Mathf.SmoothStep(-13, -7, startTime / 2), Mathf.Clamp(targetAngle, -2.5f, 7));

        }
            

    }

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
                this.gameObject.GetComponent<SpriteRenderer>().enabled = false;  //make changes
            }
            else
            {
                this.gameObject.GetComponent<SpriteRenderer>().enabled = true;   //make changes
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "TopCollider" || collision.gameObject.tag == "BottomCollider")
        {
            startBlinking = true;
            currentLife++;
            life.fillAmount = ((float)currentLife / totalLife);
            columnHit = true;
            if (currentLife >= totalLife)
            {
                FlappyGameControl.Instance.gameOver = true;
                isDead = true;
            }
        }
    }
    public float AngleToScreen(float angle) =>  (-3f + (angle - aprom[0]) * (PLAYSIZE) / (aprom[1] - aprom[0]));



}
