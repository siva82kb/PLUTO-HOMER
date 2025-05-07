
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using Unity.Mathematics;
using System.IO;
using Unity.VisualScripting;

public class HatGameController : MonoBehaviour
{
    public static HatGameController Instance { get; private set; }

    // Constant game related variables.
    private static readonly float BALLSPEED = 1f + 0.3f * (1 + 1);
    private static readonly float BALLSTARTY = 6.0f;
    private static readonly float BALLENDY = -2.0f;
    private static readonly float MOVEDURATION = 0.5f * (BALLSTARTY - BALLENDY) / BALLSPEED;

    // Game graphics related variables.
    public Text ScoreText;
    public Text timeLeftText;
    public GameObject GameOverObject;
    public GameObject StartButton, ExitButton;
    public GameObject PauseButton;
    public GameObject ResumeButton;
    public GameObject player;
    public Camera cam;
    public GameObject[] ball;
    public GameObject aromLeft;
    public GameObject aromRight;
    private GameObject PlayerObj;

    public GameObject SuccessRateBanner;
    public Text prevSR , currSR;
    private GameObject[] pauseObjects, finishObjects;
    public AudioClip[] audioClips; // win, level complete, loose
    public AudioSource gameSound;
    public Image targetImage;
    public AudioSource gamesound;
    public AudioClip loose;


    // Target and player positions
    public Vector3? TargetPosition { get; private set; }
    public Vector3 PlayerPosition { get; private set; }


    // Graphics variables.
    private float PLAYSIZE;
    // public int score = 0;
    private float maxwidth;
    // private float trialTime = 60f;
    private Vector3 scale;
    int HTGameLevel;

    private bool isPlaying = false;
    public bool targetSpwan = false;
    bool paramSet = false;
    
    // Game timing related variables
    private float triaTimeLeft;
    private float moveTimeLeft;

    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;
    public float currSuccessRate => nTargets == 0 ? 0f : 100f * nSuccess / nTargets; 

    private float ballFallingTime = 0f;
    private int totalTargetsSpawned = 0;

    private int randomTargetIndex;

    private System.Random random = new System.Random();

    private string prevScene = "CHGAME";

    // Game event to be reported to the game state machine.
    // private HatTrickGame.GameEvents gEvent = HatTrickGame.GameEvents.NONE;

    // HatTrick game logic related variables.
    public enum GameStates
    {
        WAITING = 0,
        START,
        STOP,
        PAUSED,
        SPAWNBALL,
        MOVE,
        SUCCESS,
        FAILURE,
        DONE
    }
    private GameStates _gameState;
    public GameStates gameState
    {
        get => _gameState;
        private set => _gameState = value;
    }
    private GameStates _prevGameState = GameStates.WAITING;

    // Bunch of event flags
    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isBallSpawned { get; private set; } = false;
    public bool isBallCaught { get; private set; } = false;
    public bool isBallMissed { get; private set; } = false;

    // Target and player positions.
    private float[] arom;
    private float[] prom;
    private float targetAngle;
    private float maxTargetDur;
    private float targetPosition;
    private float playerPosition;
    private  GameObject targetTemp;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        PLAYSIZE = Camera.main.orthographicSize * Camera.main.aspect;
    }

    void Start()
    { 
        InitializeGame();
        // Initialize the game objects.
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        // Do not show the paused and finished objects at the start.
        HidePaused();
        HideFinished();
        // Set the position of the AROM lines.
        aromLeft.transform.position = new Vector3(
            AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMin),
            aromLeft.transform.position.y,
            aromLeft.transform.position.z
        );
        aromRight.transform.position = new Vector3(
            AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMax),
            aromRight.transform.position.y,
            aromRight.transform.position.z
        );
    }
    
    private void Update()
    {

        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();

        Debug.Log($"ControlType : {Time.timeScale}+{ PlutoComm.CONTROLTYPETEXT[PlutoComm.controlType]}");
    }

    void FixedUpdate()
    {
        // Send PLUTO heartbeat
        PlutoComm.sendHeartbeat();

        // Handle the current game state.
        RunGameStateMachine();

        // Update player and target positions
        PlayerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        targetTemp = GameObject.FindGameObjectWithTag("Target");
        TargetPosition = targetTemp != null ? targetTemp.transform.position : null;   
    }

    public void BallCaught() {
        isBallCaught = true;
        isBallMissed = false;
        nSuccess++;
    }

    public void BallMissed() {
        isBallCaught = false;
        isBallMissed = true;
        nFailure++;
    }

    public void OnStartButtonClick() {
        isGameStarted = true;
    }

    public void StartGame()
    {
        // Start new trial.
        AppData.Instance.StartNewTrial();

        // Put PLUTO in the AAN mode.
        PlutoComm.setControlType("POSITIONAAN");
        PlutoComm.setControlBound(AppData.Instance.CurrentControlBound);
        PlutoComm.setControlDir(0);

        // Reset the AAN controller.
        AppData.Instance.aanController.ResetTrial();
        
        // Initialize game variables.
        triaTimeLeft = HomerTherapy.TrialDuration;

        // Reset score related variables.
        nTargets = 0;
        nSuccess = 0;
        nFailure = 0;

        // Disable buttons except the pause button.
        StartButton.SetActive(false);
        PauseButton.SetActive(true);
        ResumeButton.SetActive(false);
    }

    public void PauseGame()
    {
        _prevGameState = gameState;
        gameState = GameStates.PAUSED;
        isGamePaused = true;
        Time.timeScale = 0;
        ShowPaused();
        PauseButton.SetActive(false);
        ResumeButton.SetActive(true);
        ExitButton.SetActive(false);
    }

    public void ResumeGame()
    {
        HidePaused();
        Debug.Log($"prev GS :{_prevGameState}");
        isGamePaused = false;
        gameState = _prevGameState;
        Time.timeScale = 1;
        PauseButton.SetActive(true);
        ResumeButton.SetActive(false);
        ExitButton.SetActive(true);
    }

    public bool IsGamePlaying()
    {
        return gameState != GameStates.WAITING 
            && gameState != GameStates.PAUSED
            && gameState != GameStates.STOP;
    }

    private void RunGameStateMachine()
    {
        // Check if the game is to be paused or unpaused.
        Debug.Log("Game Update");
        if (isGamePaused) PauseGame();
        else if (gameState == GameStates.PAUSED) ResumeGame();

        // Run the game timer
        if (IsGamePlaying()) triaTimeLeft -= Time.deltaTime;
        Debug.Log(isGameStarted);
        // Act according to the current game state.
        bool isTimeUp = triaTimeLeft <= 0;
        switch (gameState)
        {
            case GameStates.WAITING:
                ShowPaused();
                // Check of game has been started.
                if (isGameStarted) gameState = GameStates.START;
                break;
            case GameStates.START:
                HidePaused();
               // HideFinished();
                // Start the game.
                StartGame();
                gameState = GameStates.SPAWNBALL;
                break;
            case GameStates.SPAWNBALL:
                // Spawn a new ball.
                AppData.Instance.aanController.ResetTrial();
                // Get new target position.
                // targetAngle = HomerTherapy.GetNewTargetPosition(arom, prom);
                targetAngle = HomerTherapy.GetNewTargetPositionUniformFull(arom, prom);
                targetPosition = AngleToScreen(targetAngle);
                SpawnTarget();
                // Set new trial in the AAN controller.
                AppData.Instance.aanController.SetNewTrialDetails(PlutoComm.angle, targetAngle, MOVEDURATION);
                gameState = GameStates.MOVE;
                break;
            case GameStates.MOVE:
                // Update AANController.
                AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, false);
                // Set AAN target if needed.
                if (AppData.Instance.aanController.stateChange) UpdatePlutoAANTarget();
                // Wait for the user to success or fail.
                if (isBallCaught) gameState = GameStates.SUCCESS;
                if (isBallMissed) gameState = GameStates.FAILURE;
                break;
            case GameStates.SUCCESS:
            case GameStates.FAILURE:
                // Wait for the user to score.
                gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNBALL;
                isBallCaught = false;
                isBallMissed = false;
                break;
            case GameStates.PAUSED:
                Debug.Log(isGamePaused);
                break;
            case GameStates.STOP:
                // Trial complete.
                // Update AANController.
                AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
                // Set AAN target if needed.

                AppData.Instance.previousSuccessRates =null;

                if (AppData.Instance.aanController.stateChange) UpdatePlutoAANTarget();
                // Change to done only when the AAN Controller is AromMoving or Idle state.
                if (AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.AromMoving
                    || AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.Idle) 
                {

                    AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
                    gameState = GameStates.DONE;
                   if(AppData.Instance.previousSuccessRates ==null)
                   { 
                    AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(AppData.Instance.selectedMechanism.name, AppData.Instance.selectedGame);
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    }
                }
                break;
        }
        UpdateText();
    }

    private void UpdatePlutoAANTarget()
    {
        switch(AppData.Instance.aanController.state)
        {
            case PlutoAANController.PlutoAANState.AromMoving:
                // Reset AAN Target
                PlutoComm.ResetAANTarget();
                break;
            case PlutoAANController.PlutoAANState.RelaxToArom:
            case PlutoAANController.PlutoAANState.AssistToTarget:
                // Set AAN Target to the nearest AROM edge.
                float[] _newAanTarget = AppData.Instance.aanController.GetNewAanTarget();
                PlutoComm.setAANTarget(_newAanTarget[0], _newAanTarget[1], _newAanTarget[2], _newAanTarget[3]);
                break;
        }
    }

    public float AngleToScreen(float angle) => Mathf.Lerp(-PLAYSIZE, PLAYSIZE, (angle - prom[0]) / (prom[1]- prom[0]));

    public void SpawnTarget()
    {
        nTargets++;
        Vector3 spawnPosition = new Vector3(targetPosition, 6f, 0);
        PlayerObj = GameObject.FindGameObjectWithTag("Player");
        Quaternion spawnRotation = Quaternion.identity;

        int ballIndex = UnityEngine.Random.Range(0, ball.Length);
        GameObject target = Instantiate(ball[ballIndex], spawnPosition, spawnRotation);
        target.GetComponent<Rigidbody2D>().velocity = new Vector2(0, -BALLSPEED);
        target.transform.localScale = scale;
    }

    private void InitializeGame()
    {
        // Initialize the game objects.
        player = GameObject.FindGameObjectWithTag("Player");
        scale = new Vector3(1f, 1f, 1f);
        player.transform.localScale = scale;
        
        // Intialize text
        timeLeftText = GameObject.FindGameObjectWithTag("TimeLeftText").GetComponent<Text>();
        ScoreText = GameObject.FindGameObjectWithTag("ScoreText").GetComponent<Text>();

        
        // Enable the buttons
        StartButton.SetActive(true);
        PauseButton.SetActive(false);
        ResumeButton.SetActive(false);

        // Initailize camera
        maxwidth = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0)).x - 0.5f;
        randomTargetIndex = random.Next(1, 11);

        // Intialize game logic variables
        gameState = GameStates.WAITING;
        // Clear even flags.
        isGameStarted = false;
        isGameFinished = false;
        isGamePaused = false;
        isBallSpawned = false;
        isBallCaught = false;
        isBallMissed = false;

        // Set current AROM and PROM.
        arom = AppData.Instance.selectedMechanism.CurrentArom;
        prom = AppData.Instance.selectedMechanism.CurrentProm;

        // Attach PLUTO button event.
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
    }

    private void UpdateText()
    {
        timeLeftText.text = $"Time Left: {(int)triaTimeLeft}";
        ScoreText.text = $"Score: {nSuccess}";
    }

    public void exitGame()
    {
        if(gameState == GameStates.DONE || gameState == GameStates.WAITING){
            Time.timeScale = 1f;
            SceneManager.LoadScene(prevScene);
        }
        else
        {
            gameState = GameStates.STOP;
            AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
             AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
             gameState = GameStates.DONE;
             Time.timeScale = 1f;
             SceneManager.LoadScene(prevScene);
        }
    }

    public void ShowPaused()
    {
          if(AppData.Instance.previousSuccessRates!=null)
        {
            SuccessRateBanner.SetActive(true);
            prevSR.text = $" previous SR : {AppData.Instance.previousSuccessRates[0]}%";
            currSR.text = $"Current Success Rate : {AppData.Instance.previousSuccessRates[1]}%";
        }
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
        SuccessRateBanner.SetActive(false);
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
            Destroy(collision.gameObject);
            BallMissed();
        }
    }

    private void onPlutoButtonReleased()
    {
        // This can mean different things depending on the game state.
        if (gameState == GameStates.WAITING) isGameStarted = true;
        else if (gameState != GameStates.STOP) isGamePaused = !isGamePaused;
    }
}