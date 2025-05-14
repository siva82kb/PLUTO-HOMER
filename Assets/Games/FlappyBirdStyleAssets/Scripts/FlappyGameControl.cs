
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Michsky.UI.ModernUIPack;
using Unity.VisualScripting;


public class FlappyGameControl : MonoBehaviour
{
    public AudioClip[] winClip;
    public AudioClip[] hitClip;
    public Text ScoreText;
    public static FlappyGameControl Instance { get; private set; }
    public GameObject GameOverText;
    public GameObject[] pauseObjects;
    public ProgressBar timerObject;

    bool birdDied = false;
    bool skipFirstPoint = false;
    public bool gameOver = false;
    public float scrollSpeed = 0f;
    private int score;
    public BirdControl bc;

    enum AssessStates
    {
        DAY = 1,
        EVE = 2,
        NIGHT = 3
    };

    public int _state;
    public int columnPoolSize = 5;
    private float MOVEDURATION =4f;
    private GameObject[] columns;
    public GameObject[] columnPrefab;
    public GameObject[] backgrounds;
    public Vector2 objectPoolPosition = new Vector2(-15, -25);
    private float spawnXposition = 16;
    private int CurrentColumn = 0;
    private GameObject[] top;
    private GameObject[] bottom;
    public GameObject StartButton, ResumeButton, PauseButton, ExitButton;
    public GameObject SuccessRateBanner;

    public GameObject promLeft, promRight, targetPointer;
    public Text prevSR, currSR;
    bool setup;
    float prevSpawnTime;
    // Target and player positions
    public Vector3? TargetPosition { get; private set; }
    public Vector3 PlayerPosition { get; private set; }
    private float PLAYSIZE;
    private float triaTimeLeft;
    
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;
    private string prevScene = "CHGAME";
     public Text timeLeftText;
    public enum GameStates
    {
        WAITING = 0,
        START,
        STOP,
        PAUSED,
        SPAWNTARGET,
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
    public bool isTargetHit { get; private set; } = false;
    public bool isTargetMissed { get; private set; } = false;

    // Target and player positions.
    private float[] arom;
    private float[] prom;
    private float targetAngle;
    private float targetPosition;
    public GameObject aromLeft;
    public GameObject aromRight;
    private GameObject targetTemp;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != null)
        {
            Destroy(gameObject);
        }

        float fullHeight = Camera.main.orthographicSize * 2f; // Full camera height in world units
         PLAYSIZE  = fullHeight * 0.8f; // 80% of the camera height

    }

    private void InitializeGame()
    {
        // Enable the buttons
        StartButton.SetActive(true);
        PauseButton.SetActive(false);
        ResumeButton.SetActive(false);

        // Intialize game logic variables
        gameState = GameStates.WAITING;
        // Clear even flags.
        isGameStarted = false;
        isGameFinished = false;
        isGamePaused = false;
        isBallSpawned = false;
        isTargetHit = false;
        isTargetMissed = false;

        // Set current AROM and PROM.
        arom = AppData.Instance.selectedMechanism.CurrentArom;
        prom = AppData.Instance.selectedMechanism.CurrentProm;

        // Attach PLUTO button event.
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
    }
    
    public float AngleToScreen(float angle) =>  ( -3f + (angle - prom[0]) * (PLAYSIZE) / (prom[1] - prom[0]));
    void Start()
    {
        InitializeGame();
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        setup = false;

         aromLeft.transform.position = new Vector3(
            aromLeft.transform.position.x,
            AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMin),
            aromLeft.transform.position.z
        );
        Debug.Log($" aromMin :{ AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMin)},aromMax :{ AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMax)}, promMin :{ AngleToScreen(AppData.Instance.selectedMechanism.currRom.promMin)}, promMax :{ AngleToScreen(AppData.Instance.selectedMechanism.currRom.promMax)}");
       
        aromRight.transform.position = new Vector3(
            aromRight.transform.position.x,
            AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMax),
            aromRight.transform.position.z
        );

        
    }

    void Update()
    {

        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();

        if (!setup)
        {
            int y = UnityEngine.Random.Range(0, 3);
            _state = y;
            columns = new GameObject[columnPoolSize];
            for (int i = 0; i < columnPoolSize; i++)
            {
                columns[i] = (GameObject)Instantiate(columnPrefab[_state], objectPoolPosition, Quaternion.identity);
            }
            top = GameObject.FindGameObjectsWithTag("Top");

            chooseBackground();
            setup = true;
        }
    }
    void FixedUpdate()
    {
        if (isGameStarted)
        {UpdateGameTimerUI(); }
        // Send PLUTO heartbeat
        PlutoComm.sendHeartbeat();

        // Handle the current game state.
        RunGameStateMachine();

        // Update player and target positions
        PlayerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        targetTemp = GameObject.FindGameObjectWithTag("Target");
        TargetPosition = targetTemp != null ? targetTemp.transform.position : null;  
        prevSpawnTime += Time.deltaTime;
    }

    public void chooseBackground()
    {
        foreach (GameObject obj in backgrounds)
        {
            obj.SetActive(false);
        }
        backgrounds[_state].SetActive(true);
    }

    public void spawnColumn()
    {
        if (!gameOver && prevSpawnTime > 2)
        {
            prevSpawnTime = 0;
            nTargets++;
            columns[CurrentColumn].transform.position = new Vector3(BirdControl.rb2d.transform.position.x + spawnXposition,targetPosition, 0);
            columns[CurrentColumn].tag = "Target";

            if (CurrentColumn == 0)
            {
                columns[columnPoolSize - 1].tag = "Untagged";
            }
            else
            {
                columns[CurrentColumn - 1].tag = "Untagged";

            }

            CurrentColumn += 1;

            if (CurrentColumn >= columnPoolSize)
            {
                CurrentColumn = 0;
            }

        }
    }

    public void PauseGame()
    {
        _prevGameState = gameState;
        gameState = GameStates.PAUSED;
        isGamePaused = true;
        Time.timeScale = 0;
        showPaused();
        PauseButton.SetActive(false);
        ResumeButton.SetActive(true);
        ExitButton.SetActive(false);
    }

    public void ResumeGame()
    {
        hidePaused();
        isGamePaused = false;
        gameState = _prevGameState;
        Time.timeScale = 1;
        PauseButton.SetActive(true);
        ResumeButton.SetActive(false);
        ExitButton.SetActive(true);
    }

    void UpdateGameTimerUI()
    {
        timerObject.specifiedValue = Mathf.Clamp(100 * (90 - triaTimeLeft) / 90f, 0, 100);
    }

    public void showPaused()
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

    public void hidePaused()
    {
        foreach (GameObject g in pauseObjects)
        {
            g.SetActive(false);
        }
        SuccessRateBanner.SetActive(false);
    }

    public void BallCaught() {
        isTargetHit = true;
        isTargetMissed = false;
        if (skipFirstPoint) nSuccess++;
        else skipFirstPoint = true; 
        
    }

    public void BallMissed() {
        isTargetHit = false;
        isTargetMissed = true;
        nFailure++;
    }

    public void BirdDied()
    {
        birdDied = true;
        gameOver = true;
    }

    public void BirdScored()
    {
        if (triaTimeLeft < 0 && !birdDied)
        {
            gameOver = true;
            score = 0;
            Debug.Log("not died");
            BirdDied();
        }
        else
        {
            if (!bc.startBlinking )
            {
                int index = UnityEngine.Random.Range(0, winClip.Length);
                GetComponent<AudioSource>().clip = winClip[index];

                if (score != 0) GetComponent<AudioSource>().Play();
                BallCaught();
            }
            else
            {
                int index = UnityEngine.Random.Range(0, hitClip.Length);
                GetComponent<AudioSource>().clip = hitClip[index];
                GetComponent<AudioSource>().Play();

                BallMissed();
            }
        }
    }


    public void StartGame()
    {
        scrollSpeed = -2 - 1 * .1f;
            hidePaused();
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
        Debug.Log($"trial time left :{triaTimeLeft}");
        // Reset score related variables.
        nTargets = 0;
        nSuccess = 0;
        nFailure = 0;

        timerObject.isOn = true;
        timerObject.enabled = true;

        // Disable buttons except the pause button.
        StartButton.SetActive(false);
        PauseButton.SetActive(true);
        ResumeButton.SetActive(false);
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
        Debug.Log($"Game Update : {gameState}");
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
                showPaused();
                // Check of game has been started.
                if (isGameStarted) gameState = GameStates.START;
                break;
            case GameStates.START:
                hidePaused();
               // HideFinished();
                // Start the game.
                StartGame();
                gameState = GameStates.SPAWNTARGET;
                break;
            case GameStates.SPAWNTARGET:
                // Spawn a new ball.
                AppData.Instance.aanController.ResetTrial();
                // Get new target position.
                // targetAngle = HomerTherapy.GetNewTargetPosition(arom, prom);
                targetAngle = HomerTherapy.GetNewTargetPositionUniformFull(arom, prom);
                if(targetAngle < prom[0] + 20f){
                    targetAngle = targetAngle + 40f;
                    Debug.Log("target negative");
                }
                 else if (targetAngle > prom[1] -20f) 
                 {
                    targetAngle = targetAngle - 40f;
                    Debug.Log("positive target");
                 }
                targetPosition = AngleToScreen(targetAngle);
                spawnColumn();
                MOVEDURATION = MoveDuration();
                Debug.Log($"mm :{MOVEDURATION}");
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
                if (isTargetHit) gameState = GameStates.SUCCESS;
                if (isTargetMissed || isTimeUp ) gameState = GameStates.FAILURE;
                break;
            case GameStates.SUCCESS:
            case GameStates.FAILURE:
                // Wait for the user to score.
                gameState = (isTimeUp || gameOver) ? GameStates.STOP : GameStates.SPAWNTARGET;
                isTargetHit = false;
                isTargetMissed = false;
                break;
            case GameStates.PAUSED:
                Debug.Log(isGamePaused);
                break;
            case GameStates.STOP:
                // Trial complete.
                // Update AANController.
                AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
                // Set AAN target if needed.
                isGameFinished = true;
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
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    }
                }
                break;
        }
        UpdateText();
    }
     private void UpdateText()
    {
        timeLeftText.text = $"Time Left: {(int)triaTimeLeft}";
        ScoreText.text = $"Score: {nSuccess}";
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

    private float MoveDuration()
    {
        float movduration= 0.5f * ((PlayerPosition.x + spawnXposition) - (PlayerPosition.x))/ -scrollSpeed ;
        return movduration;
    }
    public void OnStartButtonClick() 
    {
        isGameStarted = true;
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

    private void onPlutoButtonReleased()
    {
        // This can mean different things depending on the game state.
        if (gameState == GameStates.WAITING) isGameStarted = true;
        else if (gameState != GameStates.STOP) isGamePaused = !isGamePaused;
    }

}
