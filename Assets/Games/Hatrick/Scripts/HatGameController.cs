
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NeuroRehabLibrary;
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
    public Camera cam;
    public GameObject[] ball;

    public GameObject aromLeft;
    public GameObject aromRight;
    public GameObject PlayerObj;
    private Rigidbody2D rig2D;
    private float gameMoveTime = 0f;
    private float lastTimestamp = 0f;
    private float playSize;
    // private float gameSpeed = 1f;
    //private float successRate = 1f;
    public int score = 0;
    private float maxwidth;
    private float trialTime = 60f;
    private float timeLeft;
    public bool balldestroyed = true;
    private bool isPressed = false;
    private bool isPaused = false;
    private int count;
    private float x;
    private float targetAngle;

    private GameSession currentGameSession;

    private bool isPlaying = false;
    private float Player;
    private sbyte direction;
    private enum GameState { NotStarted, Playing, Paused, GameOver }
    private GameState currentState = GameState.NotStarted;

    // Target Display Scaling
    private const float xmax = 12f;
    private float[] aRomValue;


    // Control variables
    private bool isRunning = false;
    private const float tgtDuration = 3.0f;
    private float _currentTime = 0;
    private float _initialTarget = 0;
    private float _finalTarget = 0;
    private float ballFallingTime = 0f;
    //private bool _changingTarget = false; 

    // Discrete movements related variables
    private uint trialNo = 0;
    // Define variables for a discrete movement state machine
    // Enumerated variable for states
    private enum DiscreteMovementTrialState
    {
        Rest,           // Resting state
        SetTarget,      // Set the target
        Moving,         // Moving to target.
        Success,        // Successfull reach
        Failure,        // Failed reach
    }
    private DiscreteMovementTrialState _trialState;
    private static readonly IReadOnlyList<float> stateDurations = Array.AsReadOnly(new float[] {
        0.30f,          // Rest duration
        0.10f,          // Target set duration
        3.50f,          // Moving duration
        0.10f,          // Successful reach
        0.10f,          // Failed reach
    });
    private const float tgtHoldDuration = 0.2f;
    private float _trialTarget = 0f;
    private float _currTgtForDisplay;
    private float trialDuration = 0f;
    private float stateStartTime = 0f;
    private float _tempIntraStateTimer = 0f;

    // AAN Trajectory parameters. Set each trial.
    private float _assistPosition;
    private float _assistVelocity;
    private float _tgtInitial;
    private float _tgtFinal;
    private float _timeInitial;
    private float _timeDuration;

    // Control bound adaptation variables
    private float prevControlBound = 0.16f;
    // Magical minimum value where the mechanisms mostly move without too much instability.
    private float currControlBound = 0.16f;
    private const float cbChangeDuration = 2.0f;
    private sbyte currControlDir = 0;
    private float _currCBforDisplay;
    //private int successRate;

    // AAN class
    private HOMERPlutoAANController aanCtrler;
    private AANDataLogger dlogger;


    private string _dataLogDir = null;
    private string date = null;
    private string sessionNum = null;


    private float targetPosition;
    private float playerPosition;
    public bool targetSpwan = false;
    private bool tempSpawn = false;
    private int outsideAromRangeCount = 0;
    private int totalTargetsSpawned = 0;

    public bool aromRangeSpawn = false;
    public Toggle spawnAreaToggle;
    //private int successRate;
    public Image targetImage;
    private int randomTargetIndex;
    private int spawnCounter = 0;
    private System.Random random = new System.Random();
    public bool IsPlaying
    {
        get { return isPlaying; }
    }

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
        if (spawnAreaToggle != null)
        {
            spawnAreaToggle.onValueChanged.AddListener(OnToggleSpawnArea);
        }
        playSize = Camera.main.orthographicSize * Camera.main.aspect;
    }

    void Start()
    { 
        InitializeGame();
    }

    void FixedUpdate()
    {
        PlutoComm.sendHeartbeat();
        UI();
        Debug.Log(PlutoComm.CONTROLTYPE[PlutoComm.controlType]);
        //if (PlutoComm.CONTROLTYPE[PlutoComm.controlType] == "NONE" && !aromRangeSpawn) {
        //    PlutoComm.setControlType("POSITIONAAN");
        //    Debug.Log("AAN applied");
        //}
        if (currentState == GameState.Playing)
        {
            HandleGameUpdate();
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position.x;

        }
        if (isPressed)
        {
            if (!isPlaying && !isPaused)
            {
                StartGame();
                isPressed = false;
            }
            else if (isPlaying && !isPaused)
            {
                PauseGame();
                isPressed = false;
            }
            else if (isPlaying && isPaused)
            {
                ResumeGame();
                isPressed = false;
            }
        }



        // Check if the demo is running.
        if (isRunning == false) return;

        // Update trial time
        trialDuration += Time.deltaTime;

        // Run trial state machine
        RunTrialStateMachine();
        
    }


    private void UI()
    {
        aromLeft.transform.position = new Vector3(Angle2Screen(AppData.aRomValue[0]),
           aromLeft.transform.position.y,
           aromLeft.transform.position.z
       );
        aromRight.transform.position = new Vector3(
            Angle2Screen(AppData.aRomValue[1]),
            aromRight.transform.position.y,
            aromRight.transform.position.z
        );
    }



    private void RunTrialStateMachine()
    {
        float _deltime = trialDuration - stateStartTime;
        bool _statetimeout = _deltime >= stateDurations[(int)_trialState];
        // Time when target is reached.
        bool _intgt = Math.Abs(_trialTarget - PlutoComm.angle) <= 5.0f;
        //Debug.Log(_statetimeout);
        switch (_trialState)
        {
            case DiscreteMovementTrialState.Rest:
                if ((_statetimeout == false)&& !targetSpwan) return;
                SetTrialState(DiscreteMovementTrialState.SetTarget);
                dlogger.WriteAanStateInforRow();
                break;
            case DiscreteMovementTrialState.SetTarget:
                if (_statetimeout == false) return;
                SetTrialState(DiscreteMovementTrialState.Moving);
                dlogger.WriteAanStateInforRow();
                break;
            case DiscreteMovementTrialState.Moving:
                // Check of the target has been reached.
                _tempIntraStateTimer += _intgt ? Time.deltaTime : -_tempIntraStateTimer;
                // Target reached successfull.
                bool _tgtreached = _tempIntraStateTimer >= tgtHoldDuration;
                // Update AANController.
                aanCtrler.Update(PlutoComm.angle, Time.deltaTime, _statetimeout || _tgtreached);
                // Set AAN target if needed.
                if (aanCtrler.stateChange) UpdatePlutoAANTarget();
                // Change state if needed.
                if (_tgtreached || targetSpwan) SetTrialState(DiscreteMovementTrialState.Success);
                if (_statetimeout) SetTrialState(DiscreteMovementTrialState.Failure);
                dlogger.WriteAanStateInforRow();
                break;
            case DiscreteMovementTrialState.Success:
            case DiscreteMovementTrialState.Failure:
                if (_statetimeout) SetTrialState(DiscreteMovementTrialState.Rest);
                break;
        }
    }

    private void UpdatePlutoAANTarget()
    {
        switch (aanCtrler.state)
        {
            case HOMERPlutoAANController.HOMERPlutoAANState.AromMoving:
                // Reset AAN Target
                PlutoComm.ResetAANTarget();
                break;
            case HOMERPlutoAANController.HOMERPlutoAANState.RelaxToArom:
            case HOMERPlutoAANController.HOMERPlutoAANState.AssistToTarget:
                // Set AAN Target to the nearest AROM edge.
                float[] _newAanTarget = aanCtrler.GetNewAanTarget();
                PlutoComm.setAANTarget(_newAanTarget[0], _newAanTarget[1], _newAanTarget[2], _newAanTarget[3]);
                break;
        }
    }

    private void SetTrialState(DiscreteMovementTrialState newState)
    {
        switch (newState)
        {
            case DiscreteMovementTrialState.Rest:
                // Reset trial in the AANController.
                aanCtrler.ResetTrial();
                dlogger.UpdateLogFiles(trialNo);
                // Reset stuff.
                trialDuration = 0f;
                prevControlBound = PlutoComm.controlBound;
                currControlBound = 1.0f;
                if(targetSpwan && tempSpawn)
                {
                    trialNo += 1;
                    tempSpawn = false;

                }
                _tempIntraStateTimer = 0f;
                targetSpwan = false;
                break;
            case DiscreteMovementTrialState.SetTarget:
                // Random select target from the appropriate range.
              
                _trialTarget = targetAngle;
                PlutoComm.setControlBound(1f);
                break;
            case DiscreteMovementTrialState.Moving:
                // Reset the intrastate timer.
                _tempIntraStateTimer = 0f;
               // aanCtrler.SetNewTrialDetails(PlutoComm.angle, _trialTarget, stateDurations[(int)DiscreteMovementTrialState.Moving]);
               aanCtrler.SetNewTrialDetails(PlutoComm.angle, _trialTarget, ballFallingTime);

                break;
            case DiscreteMovementTrialState.Success:
            case DiscreteMovementTrialState.Failure:
                // Update adaptation row.
                byte _successbyte = newState == DiscreteMovementTrialState.Success ? (byte)1 : (byte)0;
                dlogger.WriteTrialRowInfo(_successbyte);
                break;
        }
        _trialState = newState;
        stateStartTime = trialDuration;
    }

    public float Angle2Screen(float angle)
    {
        return HT_spawnTargets1.instance.Angle2Screen(angle);
    }

    private float SpawnTargetArea()
    {
        AppData.newAROM = new ROM(AppData.selectedMechanism);
        float aromMin = AppData.newAROM.aromTmin;
        float aromMax = AppData.newAROM.aromTmax;

        float xMin = MapAROMToPROMPlaySize(aromMin);
        float xMax = MapAROMToPROMPlaySize(aromMax);

        float targetPosition = UnityEngine.Random.Range(xMin, xMax);

        Debug.Log($"Spawned Target Area Position: {targetPosition} (AROM Min: {aromMin}, Max: {aromMax}, Mapped X Min: {xMin}, Mapped X Max: {xMax})");
        return targetPosition;
    }
    private float MapAROMToPROMPlaySize(float angle)
    {
        AppData.newPROM = new ROM(AppData.selectedMechanism);
        float promMin = AppData.newPROM.promTmin;
        float promMax = AppData.newPROM.promTmax;
        float promRange = promMax - promMin;
        float normalizedAROM = (angle - promMin) / promRange;


        float scalingFactor = 0.8f;
        float adjustedRange = scalingFactor * 2 * playSize;

        return Mathf.Lerp(-adjustedRange / 2, adjustedRange / 2, normalizedAROM);
    }

    public void StartGame()
    {
        if (currentState == GameState.NotStarted || currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            isPlaying = true;
            timeLeft = trialTime;
            lastTimestamp = Time.unscaledTime;
            gameMoveTime = 0f;


            // Pluto AAN controller
            aanCtrler = new HOMERPlutoAANController(AppData.aRomValue, AppData.pRomValue, 0.85f);
            isRunning = true;
            dlogger = new AANDataLogger(aanCtrler);
            // Set Control mode.
            PlutoComm.setControlType("POSITIONAAN");
            PlutoComm.setControlBound(currControlBound);
            PlutoComm.setControlDir(0);
            trialNo = 0;
            //successRate = 0;
            // Start the state machine.
            SetTrialState(DiscreteMovementTrialState.Rest);


            StartNewGameSession();
            gameData.isGameLogging = true;

            StartButton.SetActive(false);
            PauseButton.SetActive(true);
            ResumeButton.SetActive(false);

            AppLogger.LogInfo("Game Started.");
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
            PauseButton.SetActive(false);
            ResumeButton.SetActive(true);

            AppLogger.LogInfo("Game Paused.");
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            isPlaying = true;

            Time.timeScale = 1;
            PauseButton.SetActive(true);
            ResumeButton.SetActive(false);

            AppLogger.LogInfo("Game Resumed.");
        }
    }

    public void RestartGame()
    {
        currentState = GameState.NotStarted;
        isPlaying = false;
        score = 0;
        HT_spawnTargets1.instance.count = 0;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HandleGameUpdate()
    {

        if (Time.timeScale > 0 && isPlaying)
        {
            float currentTime = Time.unscaledTime;
            gameMoveTime += currentTime - lastTimestamp;
            lastTimestamp = currentTime;

            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0)
            {
                timeLeft = 0;
                GameOver();
            }
        }

        UpdateText();
        gameData.moveTime = gameMoveTime;
    }

    private void GameOver()
    {
        currentState = GameState.GameOver;

        isPlaying = false;
        gameData.isGameLogging = false;
        PlutoComm.setControlType("NONE");
        EndCurrentGameSession();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        AppLogger.LogInfo("Game Over.");
    }

    private void OnToggleSpawnArea(bool isEnabled)
    {
        aromRangeSpawn = isEnabled;
        PlutoComm.setControlType("NONE");
        Debug.Log("Spawn Area Enabled: " + isEnabled);
    }
    public void SpawnTarget()
    {
        if (timeLeft > 0 && balldestroyed)
        {
            balldestroyed = false;
            float ballSpeed = 1f + 0.3f * (1 + gameData.gameSpeedHT);
            float trailDuration = (8.0f / ballSpeed) * 0.8f;
            HT_spawnTargets1.instance.trailDuration = trailDuration;
            totalTargetsSpawned++;

            if (aromRangeSpawn)
            {
                if (outsideAromRangeCount < 2 && totalTargetsSpawned % 10 <= 1)
                {
                    targetPosition = UnityEngine.Random.Range(-playSize + 0.5f, playSize - 0.5f);

                    Debug.Log(targetPosition);
                    outsideAromRangeCount++;
                }
                else
                {
                    targetPosition = SpawnTargetArea();
                }
            }
            else
            {

                targetPosition = UnityEngine.Random.Range(-playSize + 0.5f, playSize - 0.5f);

            }

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
            tempSpawn=true;
            target.GetComponent<Rigidbody2D>().velocity = new Vector2(0, -ballSpeed);
            target.transform.localScale = HTDifficultyManager.Scale;

            HT_spawnTargets1.instance.stopClock = trailDuration;
            targetAngle = ScreenPositionToAngle(targetPosition);
            if (totalTargetsSpawned == randomTargetIndex)
            {
                targetImage.gameObject.SetActive(true);
                Debug.Log("Displaying the target blocking image!");
            }
            else
            {
                targetImage.gameObject.SetActive(false);
            }
        }
    }

    private void InitializeGame()
    {
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene initialized.");

        rig2D = GetComponent<Rigidbody2D>();
        gameData.isGameLogging = false;

        timeLeftText = GameObject.FindGameObjectWithTag("TimeLeftText").GetComponent<Text>();
        ScoreText = GameObject.FindGameObjectWithTag("ScoreText").GetComponent<Text>();

        StartButton.SetActive(true);
        PauseButton.SetActive(false);
        ResumeButton.SetActive(false);

        if (cam == null)
        {
            cam = Camera.main;
        }

        lastTimestamp = Time.unscaledTime;
        maxwidth = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0)).x - 0.5f;
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        randomTargetIndex = random.Next(1, 11);
        Debug.Log("Random Target:" + randomTargetIndex);
        date = DateTime.Now.ToString("yyyy-MM-dd");
        string dateTime = DateTime.Now.ToString("Dyyyy-MM-ddTHH-mm-ss");
        sessionNum = "Session" + AppData.currentSessionNumber;

        AppData._dataLogDir = Path.Combine(DataManager.directoryPathSession, date, sessionNum, $"{AppData.selectedMechanism}_{AppData.selectedGame}_{dateTime}");
      
    }
    private float ScreenPositionToAngle(float screenPosition)
    {
        float calibAngleRange = PlutoComm.CALIBANGLE[PlutoComm.mechanism];
        float angle = Mathf.Lerp(
            -calibAngleRange / 2,
            calibAngleRange / 2,
            (screenPosition + playSize) / (2 * playSize)
        );
        return angle;
    }
    private void UpdateText()
    {
        timeLeftText.text = $"Time Left: {(int)timeLeft}";
        ScoreText.text = $"Score: {gameData.gameScore}";
        if (gameData.gameScore > 0 && gameData.gameScore < 11)
        {
            gameData.successRate = (float)gameData.gameScore / 10;
        }
    }

    private void StartNewGameSession()
    {
        currentGameSession = new GameSession
        {
            GameName = "HAT-Trick",
            Assessment = 0
        };

        SessionManager.Instance.StartGameSession(currentGameSession);
        AppLogger.LogInfo($"Game session {currentGameSession.SessionNumber} started.");
        SetSessionDetails();
    }
    private void SetSessionDetails()
    {
        string device = "PLUTO";
        string assistMode = "Null";
        string assistModeParameters = "Null";
        string deviceSetupLocation = "CMC-Bioeng-dpt";
        string gameParameter = "YourGameParameter";
        string mech = AppData.selectedMechanism;
        SessionManager.Instance.SetDevice(device, currentGameSession);
        SessionManager.Instance.SetAssistMode(assistMode, assistModeParameters, currentGameSession);
        SessionManager.Instance.SetDeviceSetupLocation(deviceSetupLocation, currentGameSession);
        SessionManager.Instance.SetGameParameter(gameParameter, currentGameSession);
        SessionManager.Instance.mechanism(mech, currentGameSession);
    }

    private void EndCurrentGameSession()
    {
        if (currentGameSession != null)
        {
            SessionManager.Instance.SetTrialDataFileLocation(AppData.trialDataFileLocation, currentGameSession);
            SessionManager.Instance.moveTime(gameData.moveTime.ToString("F0"), currentGameSession);
            SessionManager.Instance.gameSpeed(gameData.gameSpeedHT, currentGameSession);
            SessionManager.Instance.successRate(gameData.successRate, currentGameSession);
            SessionManager.Instance.EndGameSession(currentGameSession);
        }
    }
    public void exitGame()
    {
        EndCurrentGameSession();
        SceneManager.LoadScene("choosegame");
    }
    private void onPlutoButtonReleased()
    {
        isPressed = true;
    }



}