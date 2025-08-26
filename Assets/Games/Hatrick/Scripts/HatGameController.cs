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
    // private float BALLSPEED = 1f + 0.3f * (1 + 1);
    private static readonly float BALLSTARTY = 6.0f;
    private static readonly float BALLENDY = -2.0f;
    // private static readonly float MOVEDURATION = 0.5f * (BALLSTARTY - BALLENDY) / BALLSPEED;
    private static float BALLSPEED, MOVEDURATION;
    // Game graphics related variables.
    public Text ScoreText, speed;
    public Text timeLeftText, status;
    public GameObject GameOverObject;
    public GameObject StartButton, ExitButton;
    public GameObject PauseButton;
    public GameObject ResumeButton;
    public GameObject player;
    public Camera cam;
    public GameObject[] ball;
    private GameObject[] detailObjects;

    public GameObject aromLeft;
    public GameObject aromRight;
    private GameObject PlayerObj;

    public GameObject SuccessRateBanner;
    public Text prevSR , currSR,HS;
    private GameObject[] pauseObjects, finishObjects;
    public AudioClip[] audioClips; // win, level complete, loose
    public AudioSource gameSound;
    public Image targetImage;
    public AudioSource gamesound;
    public AudioClip loose;
    public TextMeshProUGUI score;
    public GameObject HSC; //HighScoreCanvas
    private GameObject reminderPanel;



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
    private float lastHighScore;

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
     public Image loadingImage;
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
    private float[] prom,aprom;
    private float targetAngle;
    private float maxTargetDur;
    private float targetPosition;
    private float playerPosition;
    private  GameObject targetTemp;

    private float eventDelayTimer = 0f , gameSpeed;
    private bool runOnce = false;
    public GameObject increaseSpeed, decreaseSpeed;
    bool speedControlsVisible = false;


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
        detailObjects = GameObject.FindGameObjectsWithTag("detailViewer");

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
        SetVisibility(false);
        HS.text = $"{(int)Others.highestSuccessRate:F0} %";
        status.text = $"s.no: {AppData.Instance.currentSessionNumber}\n" +
             $"trialNo: {AppData.Instance.selectedMechanism.trialNumberSession}\n" +
             $"CB: {AppData.Instance.CurrentControlBound}";
        if (AppData.Instance.selectedMechanism.trialNumberDay >= AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
        {
              reminderPanel.SetActive(true);
            
        }
        else
        {
            reminderPanel.SetActive(false);

        }
        
    }

    private void Update()
    {
        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();

        // Magic key cobmination for doing the speed control.

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            speedControlsVisible = !speedControlsVisible;

            increaseSpeed.SetActive(speedControlsVisible);
            decreaseSpeed.SetActive(speedControlsVisible);
            SetVisibility(speedControlsVisible);
            Debug.Log("Speed controls " + (speedControlsVisible ? "enabled" : "disabled"));
        }

        Debug.Log($" ball speed : {BALLSPEED}");
        

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
        Debug.Log(gameSpeed);
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

    public void increaseGameSpeed()
    {
           if (gameSpeed >= 40.0f) return;

            gameSpeed += 1.0f;
            UpdateBallSpeedAndDuration();
            Debug.Log($"gs - {AppData.Instance.speedData.gameSpeed} + {gameSpeed}");
    }
    public void decreaseGameSpeed()
    {
        string mech = PlutoComm.MECHANISMS[PlutoComm.mechanism];

        if ((mech != "FME1" && mech != "FME2" && gameSpeed <= 10.0f) ||
            ((mech == "FME1" || mech == "FME2") && gameSpeed <= 1.0f))
            return;

        gameSpeed -= 1.0f;
        UpdateBallSpeedAndDuration();
        

    }
    private void SetVisibility(bool state)
    {
        foreach (GameObject obj in detailObjects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }

    private void UpdateBallSpeedAndDuration()
    {
        string mech = PlutoComm.MECHANISMS[PlutoComm.mechanism];
        bool isFME = mech == "FME1" || mech == "FME2";

        BALLSPEED = (isFME ? 0.7f : 1.2f) + ((gameSpeed - 10f) / 30f) * 1.3f;
        BALLSPEED = Mathf.Clamp(BALLSPEED, 0.7f, 3.5f); // safety clamp
        MOVEDURATION = 0.5f * (BALLSTARTY - BALLENDY) / BALLSPEED;
    }

    public void StartGame()
    {
        // Start new trial.
        AppData.Instance.StartNewTrial();
        reminderPanel.SetActive(false);

        status.text = $"s.no: {AppData.Instance.currentSessionNumber}\n" +
              $"trialNo: {AppData.Instance.selectedMechanism.trialNumberSession}\n" +
              $"CB: {AppData.Instance.CurrentControlBound}";
        // Put PLUTO in the AAN mode.
        if ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2"))
        {
            PlutoComm.setControlType("POSITIONAAN");
            PlutoComm.setControlBound(AppData.Instance.CurrentControlBound);
            PlutoComm.setControlDir(0);
        }
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
        //  gameSpeed = AppData.Instance.speedData.gameSpeed;
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
        // Send PLUTO heartbeat
        PlutoComm.sendHeartbeat();
        
         if ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2"))
        {
            PlutoComm.setControlType("POSITIONAAN");
            PlutoComm.setControlBound(AppData.Instance.CurrentControlBound);
            PlutoComm.setControlDir(0);
        }
        
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
        // Debug.Log("Game Update");
        // if (isGamePaused) PauseGame();
        // else if (gameState == GameStates.PAUSED) ResumeGame();

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

                if (eventDelayTimer <= 0f && !runOnce)
                {
                    // Spawn a new ball.
                    AppData.Instance.aanController.ResetTrial();
                    // Get new target position.
                    // targetAngle = HomerTherapy.GetNewTargetPosition(arom, prom);
                    targetAngle = HomerTherapy.GetNewTargetPositionUniformFull(arom, aprom);
                    targetPosition = AngleToScreen(targetAngle);
                    SpawnTarget();
                    // Set new trial in the AAN controller.
                    float checkFME = ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2")) ? gameSpeed : 20.0f;
                    AppData.Instance.aanController.SetNewTrialDetails(PlutoComm.angle, targetAngle, MOVEDURATION, checkFME);
                    //AppData.Instance.aanController.SetNewTrialDetails(PlutoComm.angle, targetAngle, MOVEDURATION, AppData.Instance.speedData.gameSpeed);
                    eventDelayTimer = 0.05f;
                    runOnce = true;
                }
                else
                {
                    eventDelayTimer -= Time.deltaTime;
                    if (eventDelayTimer <= 0f)
                    {
                        gameState = GameStates.MOVE;   
                    }
                }
                
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
                if (eventDelayTimer <= 0f)
                {
                    eventDelayTimer = 0.05f;
                }
                else
                {
                    eventDelayTimer -= Time.deltaTime;
                    if (eventDelayTimer <= 0f)
                    {
                        // Wait for the user to score.
                        gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNBALL;
                        isBallCaught = false;
                        isBallMissed = false;
                        runOnce = false;
                    }
                    
                }
                
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
                if (AppData.Instance.speedData.gameSpeed != gameSpeed)
                {
                    AppData.Instance.speedData.updateGameSpeedfromGame(gameSpeed);
                    AppData.Instance.speedData.setGameSpeed(gameSpeed);
                }
                
                if (AppData.Instance.aanController.stateChange) UpdatePlutoAANTarget();
                // Change to done only when the AAN Controller is AromMoving or Idle state.
                if (AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.AromMoving
                    || AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.Idle)
                {
                    float gameTime = HomerTherapy.TrialDuration - triaTimeLeft;
                    Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
                    AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
                    gameState = GameStates.DONE;
                    lastHighScore = AppData.Instance.successRate * (PlutoAANController.MAXCONTROLBOUND - AppData.Instance.CurrentControlBound);
                    if (AppData.Instance.previousSuccessRates == null)
                    {
                        score.text = $"{(int)lastHighScore}";
                        if (lastHighScore > Others.highestSuccessRate)
                        {
                            StartCoroutine(ShowForSeconds(HSC, 1.3f));
                        }
                        else
                        {
                            AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(AppData.Instance.selectedMechanism.name, AppData.Instance.selectedGame);
                            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                        }


                    }
                    if (AppData.Instance.selectedMechanism.trialNumberDay == AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
                    {
                        SceneManager.LoadScene("CHMECH");
                    }
                }
                break;
        }
        UpdateText();
    }

    private IEnumerator ShowForSeconds(GameObject obj, float seconds)
    {
        obj.SetActive(true);
        loadingImage.gameObject.SetActive(true);
        loadingImage.fillAmount = 0f;

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            loadingImage.fillAmount = Mathf.Clamp01(elapsed / seconds);
            yield return null;
        }

        obj.SetActive(false);
        loadingImage.gameObject.SetActive(false);
        AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(AppData.Instance.selectedMechanism.name, AppData.Instance.selectedGame);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void UpdatePlutoAANTarget()
    {
        switch (AppData.Instance.aanController.state)
        {
            case PlutoAANController.PlutoAANState.AromMoving:
                // Reset AAN Target
                PlutoComm.ResetAANTarget();
                break;
            case PlutoAANController.PlutoAANState.RelaxToArom:
            case PlutoAANController.PlutoAANState.AssistToTargetAtBoundary:
            case PlutoAANController.PlutoAANState.AssistToTargetInBoundary:
                // Set AAN Target to the nearest AROM edge.
                float[] _newAanTarget = AppData.Instance.aanController.GetNewAanTarget();
                PlutoComm.setAANTarget(_newAanTarget[0], _newAanTarget[1], _newAanTarget[2], _newAanTarget[3]);
                break;
        }
    }

    public float AngleToScreen(float angle) => Mathf.Lerp(-PLAYSIZE, PLAYSIZE, (angle - aprom[0]) / (aprom[1]- aprom[0]));

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
        reminderPanel = GameObject.FindGameObjectWithTag("ReminderPanel");


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
        aprom = AppData.Instance.selectedMechanism.CurrentAProm;


        // Attach PLUTO button event.
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;

        gameSpeed = AppData.Instance.speedData.gameSpeed; // degrees/sec
        //if (gameSpeed < 10.0f) gameSpeed = 10.0f;
        float ballSpeed = 1.2f + ((gameSpeed - 10f) / 30f) * 1.3f;
        Debug.Log($"bc:{ballSpeed}");
        BALLSPEED = Mathf.Clamp(ballSpeed, 0.7f, 2.5f); 
        Debug.Log(AppData.Instance.speedData.gameSpeed);
        MOVEDURATION = 0.5f * (BALLSTARTY - BALLENDY) / BALLSPEED;
    }

    private void UpdateText()
    {
        timeLeftText.text = $": {(int)triaTimeLeft}";
        ScoreText.text = $"Score: {nSuccess}";
        speed.text = $"GS: {(int) gameSpeed}";
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
            float gameTime = HomerTherapy.TrialDuration - triaTimeLeft;
            Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
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