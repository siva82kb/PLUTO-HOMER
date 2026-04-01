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
     public GameObject gameSpeedControl;
    private GameSpeedController gsc = null;
    // Game graphics related variables.
    public Text  speed,status;
    public TextMeshProUGUI timeLeftText, ScoreText ;
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
    public TextMeshProUGUI  finalScore;



    // Graphics variables.
    private float PLAYSIZE;
    private float mechMinDuration, mechMaxDuration, mechMinThreshold, mechMaxThreshold;

    // public int score = 0;
    private float maxwidth;
    // private float trialTime = 60f;
    private Vector3 scale;
    int HTGameLevel;

    private bool isPlaying = false;
    public bool targetSpwan = false;
    bool paramSet = false;
    
    // Game timing related variables
    private float trialTimeLeft;
    private float lastHighScore;

    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;
    private int[] scores;
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
    bool speedControlsVisible = false, changeScene = false;

    
    public GameObject celebrationPanel;
    public TextMeshProUGUI scoreComparisonTxt;
    public TextMeshProUGUI yesterdayScoreTxt;
    public TextMeshProUGUI todayScoreTxt;
    public TextMeshProUGUI starCount;
    public GameObject GameOverStar,gameOverPanel , starLabel, instructionPanel;
    public int _starCount;
    float lastTargetReachTime = -1f;
    float lastInterTargetDuration = 0f;
    [Header("Location BGMs")]
    [SerializeField] private AudioClip ranipetBGM;
    [SerializeField] private AudioClip manipalBGM;
    [SerializeField] private AudioClip ludianaBGM;
    public AudioSource bgmAudioSource;


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
    setUpLocationBGM();
    }
    private void setUpLocationBGM()
    {
           string location = AppData.Instance.userData.GetDeviceLocation();
            
            if (!string.IsNullOrEmpty(location))
            {
                string loc = location.Trim().ToLower();
                if (loc.Contains("ranipet") && ranipetBGM != null)
                    bgmAudioSource.clip = ranipetBGM;
                else if (loc.Contains("manipal") && manipalBGM != null)
                    bgmAudioSource.clip = manipalBGM;
                else if (loc.Contains("ludhiana") && ludianaBGM != null)
                    bgmAudioSource.clip = ludianaBGM;

                if (bgmAudioSource.clip != null)
                    bgmAudioSource.Play();
            }
    } 
    private void setMinMaxDurationOfMech()
    {
        string mech = AppData.Instance.selectedMechanism.name;
        mechMinDuration = (aprom[1]-aprom[0])/HomerTherapy.MaxSpeed;
        mechMaxDuration = (aprom[1]-aprom[0])/HomerTherapy.MinSpeed;
        switch (mech)
        {
            case"WFE":
            case"WURD":
                mechMinThreshold = HomerTherapy.MinDurationOfMechWFEAndWURD;
                mechMaxThreshold = HomerTherapy.MaxDurationOfMechWFEAndWURD;
                break;
            case"HOC":
                mechMinThreshold = HomerTherapy.MinDurationOfMechofHOC;
                mechMaxThreshold = HomerTherapy.MaxDurationOfMechOfHOC;
                break;
            case"FPS":
            case"FME1":
            case"FME2":
                mechMinThreshold = HomerTherapy.MinDurationOfMechFPSAndFME;
                mechMaxThreshold = HomerTherapy.MaxDurationOfMechFPSAndFME;
                break;
        }
        if(mechMinDuration < mechMinThreshold) mechMinDuration= mechMinThreshold;
        if(mechMaxDuration > mechMaxThreshold) mechMaxDuration = mechMaxThreshold;

        Debug.Log($" mech Min speed : { mechMaxDuration}, max :{mechMinDuration}");
    }
    float GetTargetEndTime(float gameSpeed)
    {
        float t = (gameSpeed - HomerTherapy.MinSpeed) / (HomerTherapy.MaxSpeed - HomerTherapy.MinSpeed);
        t = Mathf.Clamp01(t);

        return Mathf.Lerp(mechMaxDuration,mechMinDuration, t);
    }

    void Start()
    {
        InitializeGame();
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        // AppLogger.LogInfo($"YesterDay's Score: {scores[1]} | Today's Score: {scores[0]}");

        initializeGameSpeedController();
        // Initialize the game objects.
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        detailObjects = GameObject.FindGameObjectsWithTag("detailViewer");
        instructionPanel.SetActive(false);

        // Do not show the paused and finished objects at the start.
        HidePaused();
        HideFinished();
        updateStarCount();
        
        scores = GameFuncs.GetScores();
        Debug.Log($"{scores[0]}/{scores[1]}");
        AppLogger.LogInfo($"YesterDay's Score: {scores[1]} | Today's Score: {scores[0]}");
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
        // status.text = $"s.no: {AppData.Instance.currentSessionNumber}\n" +
        //      $"trialNo: {AppData.Instance.selectedMechanism.trialNumberSession}\n" +
        //      $"CB: {AppData.Instance.CurrentControlBound}";
        if (AppData.Instance.selectedMechanism.trialNumberDay >= AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
        {
              reminderPanel.SetActive(true);
            
        }
        else
        {
            reminderPanel.SetActive(false);

        }
        
    }
    void OnHatTargetReached()
    {
        float now = Time.time;

        if (lastTargetReachTime > 0f)
        {
            lastInterTargetDuration = now - lastTargetReachTime;
            Debug.Log($"Hat → Hat duration: {lastInterTargetDuration:F2} sec");
        }

        lastTargetReachTime = now;
    }

    private void initializeGameSpeedController()
    {
        // Hide game speed control initially
        // gameSpeedControl.SetActive(false);

        gsc = gameSpeedControl.GetComponent<GameSpeedController>();
        if (gsc == null) return;

        // Attach the buttons
        if (gsc.decreaseButton != null)
            gsc.decreaseButton.onClick.AddListener(() => decreaseGameSpeed());
        if (gsc.increaseButton != null)
            gsc.increaseButton.onClick.AddListener(() => increaseGameSpeed());

        // Set the initial game speed
        gsc.gameSpeedText.text = $"{AppData.Instance.speedData.gameSpeed:F2}";
    }
    public void updateStarCount()
    {
        starCount.text = $"{AppData.Instance.selectedGame.cummulativeStars.ToString("D2")}";
    }
    private void Update()
    {
        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();

        if (changeScene && gameState == GameStates.DONE)
        {
            restartGame();
            changeScene = false;
        }
        else
        {
            changeScene = false;
        }

        // Magic key cobmination for doing the speed control.

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            speedControlsVisible = !speedControlsVisible;
            SetVisibility(speedControlsVisible);
            Debug.Log("Speed controls " + (speedControlsVisible ? "enabled" : "disabled"));
        }

        // Debug.Log($" ball speed : {BALLSPEED}");
        // Debug.Log($" ball speed : {gameState}--{changeScene}");

        

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
        OnHatTargetReached();
        isBallCaught = true;
        isBallMissed = false;
        nSuccess++;
    }

    public void BallMissed() {
        OnHatTargetReached();

        isBallCaught = false;
        isBallMissed = true;
        nFailure++;
    }

    public void OnStartButtonClick() {
        isGameStarted = true;
    }

     public void restartGame()
    {
        HideFinished();
        string currentSceneName = SceneManager.GetActiveScene().name;
        // AppLogger.LogInfo($"The Game is restarted {currentSceneName}");
        SceneManager.LoadScene(currentSceneName);
    }

    public void increaseGameSpeed()
    {
        if (gameSpeed >= PlutoAANController.MAX_SPEED) return;

        gameSpeed += 1.0f;
        gsc.gameSpeedText.text = $"{(int)gameSpeed}";

        UpdateBallSpeedAndMoveDuration();
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}'s game speed increased to {gameSpeed}, Ball speed is {BALLSPEED}");

        // Debug.Log($"gs - {AppData.Instance.speedData.gameSpeed} + {gameSpeed}");
        AppData.Instance.annotation=$"GS: {gameSpeed} | MT: {MOVEDURATION:F2}";

    }

    public void decreaseGameSpeed()
    {
        if (gameSpeed <= PlutoAANController.MIN_SPEED) return;

        gameSpeed -= 1.0f;
        gsc.gameSpeedText.text = $"{(int)gameSpeed}";

        UpdateBallSpeedAndMoveDuration();
        AppData.Instance.annotation=$"GS: {gameSpeed} | MT: {MOVEDURATION:F2}";

        
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}'s game speed decreased to {gameSpeed}, Ball speed is {BALLSPEED}");
    }
    private void SetVisibility(bool state)
    {
        foreach (GameObject obj in detailObjects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }

    public void StartGame()
    {
        // Start new trial.
        AppData.Instance.StartNewTrial();
        reminderPanel.SetActive(false);
        gsc.sessionDetailsText.text = $"sessionNo: {AppData.Instance.currentSessionNumber}\n" +
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
        trialTimeLeft = HomerTherapy.TrialDuration;

        // Reset score related variables.
        nTargets = 0;
        nSuccess = 0;
        nFailure = 0;
        // AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game started");
    }

    public void PauseGame()
    {
        _prevGameState = gameState;
        gameState = GameStates.PAUSED;
        isGamePaused = true;
        Time.timeScale = 0;
        ShowPaused();
                AppLogger.LogInfo("Game Paused");

    }

    public void ResumeGame()
    {

        HidePaused();
        Debug.Log($"prev GS :{_prevGameState}");
        isGamePaused = false;
        gameState = _prevGameState;
        Time.timeScale = 1;
        // PauseButton.SetActive(true);
        // ResumeButton.SetActive(false);
        ExitButton.SetActive(true);
        // Send PLUTO heartbeat
        PlutoComm.sendHeartbeat();
                AppLogger.LogInfo("Game Resumed");

        
         if ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2"))
        {
            PlutoComm.setControlType("POSITIONAAN");
            PlutoComm.setControlBound(AppData.Instance.CurrentControlBound);
            PlutoComm.setControlDir(0);
        }
        // AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game resumed");
        
    }

    public bool IsGamePlaying()
    {
        return gameState != GameStates.WAITING 
            && gameState != GameStates.PAUSED
            && gameState != GameStates.STOP;
    }

    private void RunGameStateMachine()
    {
        // Run the game timer
        // if (IsGamePlaying()) trialTimeLeft -= Time.deltaTime;
        if (IsGamePlaying() && trialTimeLeft > 0f)
        {
            trialTimeLeft -= Time.deltaTime;
        }

        // Act according to the current game state.
        bool isTimeUp = trialTimeLeft <= 0;
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
                // AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game paused");
                break;
            // case GameStates.STOP:
            //     // Trial complete.
            //     // Update AANController.
            //     AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
            //     // Set AAN target if needed.
            //     isGameFinished = true;
            //     instructionPanel.SetActive(true);

            //     AppData.Instance.previousSuccessRates =null;
            //     if (AppData.Instance.speedData.gameSpeed != gameSpeed)
            //     {
            //         AppData.Instance.speedData.setGameSpeed(gameSpeed);
            //     }
            //     AppData.Instance.speedData.setMoveDuration(MOVEDURATION);
                
            //     if (AppData.Instance.aanController.stateChange) UpdatePlutoAANTarget();
            //     // Change to done only when the AAN Controller is AromMoving or Idle state.
            //     if (AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.AROMMOVING
            //         || AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.IDLE)
            //     {
            //         float gameTime = HomerTherapy.TrialDuration - trialTimeLeft;
            //         Debug.Log($" Scores : {scores[0]}  + {nSuccess} + {scores[1]} ++ ");
            //         Debug.Log($" scor : {AppData.Instance.selectedGame.isAchievedToday()}");
            //         instructionPanel.SetActive(false);

            //         Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
            //         // AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
            //           // Stop the current game trial
            //         if ((scores[0] + nSuccess) > scores[1] && !AppData.Instance.selectedGame.isAchievedToday())
            //         {
            //             AppData.Instance.selectedGame.updateCummulativeStars();
            //             celebrationPanel.SetActive(true);
            //         }
            //         AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
                    
            //         gameOverPanel.SetActive(!celebrationPanel.gameObject.activeSelf);
                    
            //         if (gameOverPanel.gameObject.activeSelf)
            //         {
            //             GameOverStar.SetActive(AppData.Instance.selectedGame.isAchievedToday());
            //             yesterdayScoreTxt.text = $"{scores[1]:D4}";
            //             todayScoreTxt.text = $"{(scores[0]+nSuccess):D4}";
            //         }
            //         if (celebrationPanel.gameObject.activeSelf)
            //         {
            //             updateStarCount();
            //             scoreComparisonTxt.text = $"{(scores[0] + nSuccess).ToString("D3")}";
            //         }
            //         gameState = GameStates.DONE;
            //         lastHighScore = AppData.Instance.successRate * (PlutoAANController.MAXCONTROLBOUND - AppData.Instance.CurrentControlBound);
            //         if (AppData.Instance.previousSuccessRates == null)
            //         {
            //             score.text = $"{(int)lastHighScore}";
            //             // if (lastHighScore > Others.highestSuccessRate)
            //             // {
            //             //     StartCoroutine(ShowForSeconds(HSC, 1.3f));
            //             // }
            //             // else
            //             // {
            //                 AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(AppData.Instance.selectedMechanism.name, AppData.Instance.selectedGameName);
            //                 // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            //                 ShowFinished();
            //             // }
            //         }
            //         if (AppData.Instance.selectedMechanism.trialNumberDay == AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
            //         {
            //             AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game finished and changed to Choose Mechanism scene due to allocated trials has over.");
            //             SceneManager.LoadScene("CHMECH");
            //         }
            //     }
            // break;
            case GameStates.STOP:
    // Trial complete.
    // Update AANController.
    AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
    // Set AAN target if needed.
    isGameFinished = true;
    instructionPanel.SetActive(true);

    AppData.Instance.previousSuccessRates = null;
    if (AppData.Instance.speedData.gameSpeed != gameSpeed)
    {
        AppData.Instance.speedData.setGameSpeed(gameSpeed);
    }
    AppData.Instance.speedData.setMoveDuration(MOVEDURATION);
    
    if (AppData.Instance.aanController.stateChange) 
        UpdatePlutoAANTarget();
    
    // Change to done only when the AAN Controller is AromMoving or Idle state or after delay completes
    if (AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.AROMMOVING
        || AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.IDLE)
    {
        ProceedToGameEnd();
    }
    // Add delay when game is over but not in AROM-moving state
    if (AppData.Instance.aanController.state != PlutoAANController.PlutoAANState.AROMMOVING)
    {
        // Calculate delay based on game speed
        float endGameDelay = 0f;
        if (Mathf.Approximately(gameSpeed, 40f))
            endGameDelay = 5f;
        else if (Mathf.Approximately(gameSpeed, 10f))
            endGameDelay = 12f;
        else
            endGameDelay = Mathf.Lerp(12f, 5f, (gameSpeed - 10f) / 30f); // Linear interpolation for other speeds
        
        // Use eventDelayTimer for the countdown
        if (eventDelayTimer <= 0f)
        {
            eventDelayTimer = endGameDelay;
        }
        else
        {
            eventDelayTimer -= Time.deltaTime;
            if (eventDelayTimer <= 0f)
            {
                // Delay completed - proceed with ending the game
                ProceedToGameEnd();
            }
            else
            {
                // Still waiting - don't proceed to game end yet
                break;
            }
        }
    }
    
    
    break;
        
        }
        UpdateText();
    }


private void ProceedToGameEnd()
{
    float gameTime = HomerTherapy.TrialDuration - trialTimeLeft;
    Debug.Log($" Scores : {scores[0]}  + {nSuccess} + {scores[1]} ++ ");
    Debug.Log($" scor : {AppData.Instance.selectedGame.isAchievedToday()}");
    instructionPanel.SetActive(false);

    Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
    
    // Stop the current game trial
    if ((scores[0] + nSuccess) > scores[1] && !AppData.Instance.selectedGame.isAchievedToday())
    {
        AppData.Instance.selectedGame.updateCummulativeStars();
                AppLogger.LogInfo($"Beat yesterday's score - {AppData.Instance.selectedGameName} game. 1 star added. Stars: {AppData.Instance.selectedGame.cummulativeStars:D2}");      

        celebrationPanel.SetActive(true);
    }
    AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
    
    gameOverPanel.SetActive(!celebrationPanel.gameObject.activeSelf);
    
    if (gameOverPanel.gameObject.activeSelf)
    {
        GameOverStar.SetActive(AppData.Instance.selectedGame.isAchievedToday());
        yesterdayScoreTxt.text = $"{scores[1]:D4}";
        todayScoreTxt.text = $"{(scores[0] + nSuccess):D4}";
    }
    if (celebrationPanel.gameObject.activeSelf)
    {
        updateStarCount();
        scoreComparisonTxt.text = $"{(scores[0] + nSuccess).ToString("D3")}";
    }
    
    gameState = GameStates.DONE;
    lastHighScore = AppData.Instance.successRate * (PlutoAANController.MAXCONTROLBOUND - AppData.Instance.CurrentControlBound);
    
    if (AppData.Instance.previousSuccessRates == null)
    {
        score.text = $"{(int)lastHighScore}";
        // if (lastHighScore > Others.highestSuccessRate)
        // {
        //     StartCoroutine(ShowForSeconds(HSC, 1.3f));
        // }
        // else
        // {
            AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(
                AppData.Instance.selectedMechanism.name, 
                AppData.Instance.selectedGameName);
            ShowFinished();
        // }
    }
    
    if (AppData.Instance.selectedMechanism.trialNumberDay == AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
    {
        AppLogger.LogInfo("Game over and changed to Choose Mechanism scene due to allocated trials has over.");
        SceneManager.LoadScene("CHMECH");
    }
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
        AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(AppData.Instance.selectedMechanism.name, AppData.Instance.selectedGameName);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game's hishest score recorded");

    }

    private void UpdatePlutoAANTarget()
    {
        switch (AppData.Instance.aanController.state)
        {
            case PlutoAANController.PlutoAANState.AROMMOVING:
                // Reset AAN Target
                PlutoComm.ResetAANTarget();
                break;
            case PlutoAANController.PlutoAANState.RELAXTOAROM:
            case PlutoAANController.PlutoAANState.ASSISTTOTARGETATBOUNDARY:
            case PlutoAANController.PlutoAANState.ASSISTTOTARGETINBOUNDARY:
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
        if(AppData.Instance.selectedGame.isAchievedToday())starLabel.GetComponent<Image>().color = Color.white;

        reminderPanel = GameObject.FindGameObjectWithTag("ReminderPanel");



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

        setMinMaxDurationOfMech();
        gameSpeed = AppData.Instance.speedData.gameSpeed; // degrees/sec

        MOVEDURATION = GetTargetEndTime(gameSpeed);

        // Attach PLUTO button event.
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;

        BALLSPEED = (BALLSTARTY - BALLENDY)/MOVEDURATION;
        celebrationPanel.SetActive(false);

    }

    private void UpdateText()
    {
        timeLeftText.text = $"Timer:{Mathf.Max(0, Mathf.CeilToInt(trialTimeLeft)):D2}s";
        ScoreText.text = $"Score:{nSuccess:D2}";
    }

    public void exitGame()
    {
        if(gameState == GameStates.DONE || gameState == GameStates.WAITING){
            Time.timeScale = 1f;
            AppLogger.LogInfo("Exit Game");

            SceneManager.LoadScene(prevScene);
        }
        else
        {
            gameState = GameStates.STOP;
            AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
            float gameTime = HomerTherapy.TrialDuration - trialTimeLeft;
                // Stop the current game trial
                    if ((scores[0] + nSuccess) > scores[1] && !AppData.Instance.selectedGame.isAchievedToday())
                    {
                        AppData.Instance.selectedGame.updateCummulativeStars();
                        celebrationPanel.SetActive(true);
                    }
                    AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
                    
                    gameOverPanel.SetActive(!celebrationPanel.gameObject.activeSelf);
                    
                    if (gameOverPanel.gameObject.activeSelf)
                    {
                        GameOverStar.SetActive(AppData.Instance.selectedGame.isAchievedToday());
                        yesterdayScoreTxt.text = $"{scores[1]:D4}";
                        todayScoreTxt.text = $"{(scores[0]+nSuccess):D4}";
                    }
                    if (celebrationPanel.gameObject.activeSelf)
                    {
                        updateStarCount();
                        scoreComparisonTxt.text = $"{(scores[0] + nSuccess).ToString("D3")}";
                    }
            Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
            if (AppData.Instance.speedData.gameSpeed != gameSpeed)  AppData.Instance.speedData.setGameSpeed(gameSpeed);
            AppData.Instance.speedData.setMoveDuration(MOVEDURATION);
            gameState = GameStates.DONE;
            Time.timeScale = 1f;
            SceneManager.LoadScene(prevScene);
            // AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game exit");
            AppLogger.LogInfo("Exit Game");

             
        }
    }

    public void ShowPaused()
    {
          if(AppData.Instance.previousSuccessRates!=null)
        {
            // SuccessRateBanner.SetActive(true);
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

    void UpdateBallSpeedAndMoveDuration()
    {
        MOVEDURATION = GetTargetEndTime(gameSpeed);

        BALLSPEED = (BALLSTARTY-BALLENDY ) / MOVEDURATION;
    }

    public void ShowFinished()
    {
        // finalScore.text = $"{AppData.Instance.selectedGame.cummulativeHits:D4}";
        AppLogger.LogInfo("Game Over");

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
        if (gameState == GameStates.WAITING) isGameStarted = true;
        else if (gameState != GameStates.STOP && gameState != GameStates.DONE) isGamePaused = !isGamePaused;
        else if (gameState == GameStates.DONE && isGameFinished) changeScene = true;

        AppLogger.LogInfo("PLUTO button pressed");

    }
    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= onPlutoButtonReleased;
        }
    }
}