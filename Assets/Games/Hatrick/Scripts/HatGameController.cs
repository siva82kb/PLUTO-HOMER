
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using Unity.Mathematics;
using System.IO;

public class HatGameController : MonoBehaviour
{
    public static HatGameController instance;

    public Text ScoreText;
    public Text timeLeftText;
    public GameObject GameOverObject;
    public GameObject StartButton;
    public GameObject PauseButton;
    public GameObject ResumeButton;
    public GameObject player;
    public Camera cam;
    public GameObject[] ball;
    public GameObject aromLeft;
    public GameObject aromRight;
    private GameObject PlayerObj;
    private GameObject[] pauseObjects, finishObjects;
    public AudioClip[] audioClips; // win, level complete, loose
    public AudioSource gameSound;
    public Image targetImage;
    public AudioSource gamesound;
    public AudioClip loose;


    private float playSize;
    public int score = 0;
    private float maxwidth;
    private float trialTime = 60f;
    private float timeLeft;
    private bool isPaused = false;
    private Vector3 scale;
    int HTGameLevel;

    private bool isPlaying = false;
    public bool targetSpwan = false;
    bool paramSet = false;

    private enum GameState { NotStarted, Playing, Paused, GameOver }
    private GameState currentState = GameState.NotStarted;

    //AAN
    private float ballFallingTime = 0f;
    private float targetPosition;
    private float playerPosition;
    private int totalTargetsSpawned = 0;

    private int randomTargetIndex;

    private System.Random random = new System.Random();

    private string prevScene = "CHGAME";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        playSize = Camera.main.orthographicSize * Camera.main.aspect;
    }

    void Start()
    { 
        InitializeGame();
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        HidePaused();
        HideFinished();
    }

    void FixedUpdate()
    {
        HandleGameState();
        if (!paramSet)
        {

            HTGameLevel = 1;
            player = GameObject.FindGameObjectWithTag("Player");
            scale = new Vector3(1f - 0.05f * HTGameLevel, 1f - 0.05f * HTGameLevel, 1f - 0.05f * HTGameLevel);
            player.transform.localScale = scale;
            paramSet = true;
        }
        if (currentState == GameState.Playing)
        {
            HandleGameUpdate();
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position.x;

        }
    }
    public float Angle2Screen(float angle)
    {
        float newPROM_tmin = 60f;
        float newPROM_tmax = -60f;

        return Mathf.Lerp(-playSize, playSize, (angle - newPROM_tmin) / (newPROM_tmax - newPROM_tmin));
    }

    public void StartGame()
    {
        if (currentState == GameState.NotStarted || currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            isPlaying = true;
            timeLeft = trialTime;
            StartButton.SetActive(false);
            PauseButton.SetActive(true);
            ResumeButton.SetActive(false);

            SpawnTarget();
        }
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            isPlaying = false;
            isPaused = true;
            Time.timeScale = 0;
            ShowPaused();
            PauseButton.SetActive(false);
            ResumeButton.SetActive(true);
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            isPlaying = true;
            HidePaused();
            Time.timeScale = 1;
            PauseButton.SetActive(true);
            ResumeButton.SetActive(false);
        }
    }

    public void RestartGame()
    {
        currentState = GameState.NotStarted;
        isPlaying = false;
        score = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HandleGameUpdate()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0)
        {
            timeLeft = 0;
            GameOver();
        }
        UpdateText();
    }

    private void GameOver()
    {
        currentState = GameState.GameOver;
        isPlaying = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SpawnTarget()
    {
        if (timeLeft > 0 )
        {
            float ballSpeed = 1f + 0.3f * (1 + 1);
            float trailDuration = (8.0f / ballSpeed) * 0.8f;
            totalTargetsSpawned++;

            targetPosition = UnityEngine.Random.Range(-playSize + 0.5f, playSize - 0.5f);
            Vector3 spawnPosition = new Vector3(targetPosition, 6f, 0);

            PlayerObj = GameObject.FindGameObjectWithTag("Player");

            // Calculate the total distance 
            float xDistance = spawnPosition.x - PlayerObj.transform.position.x;
            float yDistance = spawnPosition.y - PlayerObj.transform.position.y;
            float totalDistance = Mathf.Sqrt(xDistance * xDistance + yDistance * yDistance);

            // Calculate the time for the ball to reach the hat
            float fallTime = totalDistance / ballSpeed;
            ballFallingTime = fallTime - (fallTime * 0.25f);
            Quaternion spawnRotation = Quaternion.identity;

            int ballIndex = UnityEngine.Random.Range(0, ball.Length);
            GameObject target = Instantiate(
                ball[ballIndex],
                spawnPosition,
                spawnRotation
            );
            targetSpwan = ((ballIndex == 0) || (ballIndex == 1) || (ballIndex == 2) || (ballIndex == 3)); //it will be used when bomb added to the game Object 
            target.GetComponent<Rigidbody2D>().velocity = new Vector2(0, -ballSpeed);
            target.transform.localScale = scale;
        }
    }

    private void InitializeGame()
    {
        timeLeftText = GameObject.FindGameObjectWithTag("TimeLeftText").GetComponent<Text>();
        ScoreText = GameObject.FindGameObjectWithTag("ScoreText").GetComponent<Text>();

        StartButton.SetActive(true);
        PauseButton.SetActive(false);
        ResumeButton.SetActive(false);

        maxwidth = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0)).x - 0.5f;
        randomTargetIndex = random.Next(1, 11);
    }

    private void UpdateText()
    {
        timeLeftText.text = $"Time Left: {(int)timeLeft}";
        ScoreText.text = $"Score: {score}";
    }

    public void exitGame()
    {
        SceneManager.LoadScene(prevScene);
    }

    private void HandleGameState()
    {
        if (isPlaying && !isPaused)
        {
            HideFinished();
            HidePaused();
        }
        else if (!isPlaying)
        {
            ShowFinished();
        }
    }

    public void ShowPaused()
    {
        foreach (GameObject g in pauseObjects)
        {
            g.SetActive(true);
        }
    }

    public void HidePaused()
    {
        foreach (GameObject g in pauseObjects)
        {
            g.SetActive(false);
        }
    }

    public void ShowFinished()
    {
        foreach (GameObject g in finishObjects)
        {
            g.SetActive(true);
        }
    }

    public void HideFinished()
    {
        foreach (GameObject g in finishObjects)
        {
            g.SetActive(false);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Target")
        {
            gamesound = gameObject.GetComponent<AudioSource>();
            gamesound.clip = loose;
            gamesound.Play();
            SpawnTarget();
            Destroy(collision.gameObject);
        }
    }

}