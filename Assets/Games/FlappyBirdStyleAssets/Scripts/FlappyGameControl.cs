
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Michsky.UI.ModernUIPack;
using Unity.VisualScripting;
using TMPro;


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
    private GameObject[] detailObjects;

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
    public Text prevSR, currSR,HS;
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
     public Text timeLeftText, status, gameSpeedViewer;
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
    private float[] prom, aprom;
    private float targetAngle;
    private float targetPosition;
    public GameObject aromLeft;
    public GameObject aromRight;
    private GameObject targetTemp;
    public GameObject HSC; //HighScoreCanvas
    public TextMeshProUGUI score1;
    private float lastHighScore, eventDelayTimer = 0f, gameSpeed;
    private bool runOnce = false;
    public Image loadingImage;
    private GameObject reminderPanel;

    
    public GameObject increaseSpeed, decreaseSpeed;
    bool speedControlsVisible = false;


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
        reminderPanel = GameObject.FindGameObjectWithTag("ReminderPanel");

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
        aprom = AppData.Instance.selectedMechanism.CurrentAProm;

        gameSpeed = AppData.Instance.speedData.gameSpeed;
        //gameSpeed = 20.0f; //temp
        // Attach PLUTO button event.
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
    }
    
    public float AngleToScreen(float angle) =>  ( -3f + (angle - aprom[0]) * (PLAYSIZE) / (aprom[1] - aprom[0]));
    void Start()
    {
        InitializeGame();
        detailObjects = GameObject.FindGameObjectsWithTag("detailViewer");
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        setup = false;

        aromLeft.transform.position = new Vector3(
           aromLeft.transform.position.x,
           AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMin),
           aromLeft.transform.position.z
       );
        //Debug.Log($" aromMin :{ AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMin)},aromMax :{ AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMax)}, promMin :{ AngleToScreen(AppData.Instance.selectedMechanism.currRom.promMin)}, promMax :{ AngleToScreen(AppData.Instance.selectedMechanism.currRom.promMax)}");

        aromRight.transform.position = new Vector3(
            aromRight.transform.position.x,
            AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMax),
            aromRight.transform.position.z
        );
        SetVisibility(false);
        HS.text = $" BEST :{Others.highestSuccessRate:F0} %";
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

    void Update()
    {
        

        if (isGamePaused && gameState != GameStates.PAUSED) PauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSED) ResumeGame();
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            speedControlsVisible = !speedControlsVisible;

            increaseSpeed.SetActive(speedControlsVisible);
            decreaseSpeed.SetActive(speedControlsVisible);
            SetVisibility(speedControlsVisible);
            Debug.Log("Speed controls " + (speedControlsVisible ? "enabled" : "disabled"));
        }

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
        // Send PLUTO heartbeat
        PlutoComm.sendHeartbeat();
        if (isGameStarted)
        { UpdateGameTimerUI(); }
        // Send PLUTO heartbeat
        // PlutoComm.sendHeartbeat();

        // Handle the current game state.
        RunGameStateMachine();

        // Update player and target positions
        PlayerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        targetTemp = GameObject.FindGameObjectWithTag("Target");
        TargetPosition = targetTemp != null ? targetTemp.transform.position : null;
        prevSpawnTime += Time.deltaTime;
        Debug.Log(scrollSpeed);
    }

    public void chooseBackground()
    {
        foreach (GameObject obj in backgrounds)
        {
            obj.SetActive(false);
        }
        backgrounds[_state].SetActive(true);
    }
    private void SetVisibility(bool state)
    {
        foreach (GameObject obj in detailObjects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
    public void increaseGameSpeed()
    {
         if (gameSpeed >= 40.0f) return;

        gameSpeed += 1.0f;
        UpdateScrollSpeed();
        Debug.Log($"gs - {AppData.Instance.speedData.gameSpeed} + {gameSpeed}");
    }
    public void decreaseGameSpeed()
    {

        string mech = PlutoComm.MECHANISMS[PlutoComm.mechanism];
        bool isFME = mech == "FME1" || mech == "FME2";

        if ((isFME && gameSpeed <= 1.0f) || (!isFME && gameSpeed <= 10.0f)) return;

        gameSpeed -= 1.0f;
        UpdateScrollSpeed();

    }
    private void UpdateScrollSpeed()
    {
        // Use finer scaling for scroll speed at lower increments
        float scrollFactor =  0.05f;
        scrollSpeed = -2f - (scrollFactor * gameSpeed);
    }

    public void spawnColumn()
    {
        float spawnInterval = Mathf.Max(0.5f, 2f - (gameSpeed - 10f) * 0.05f);

        if (!gameOver && prevSpawnTime > spawnInterval)
        {
            prevSpawnTime = 0;
            nTargets++;
            columns[CurrentColumn].transform.position = new Vector3(BirdControl.rb2d.transform.position.x + spawnXposition, targetPosition, 0);
            columns[CurrentColumn].tag = "Target";
            // Debug.Log($"{(BirdControl.rb2d.transform.position.x + spawnXposition, targetPosition, 0)}");
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
        // Send PLUTO heartbeat
        PlutoComm.sendHeartbeat();
        
          if ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2"))
        {
            PlutoComm.setControlType("POSITIONAAN");
            PlutoComm.setControlBound(AppData.Instance.CurrentControlBound);
            PlutoComm.setControlDir(0);
        }
    }

    void UpdateGameTimerUI()
    {
        timerObject.specifiedValue = Mathf.Clamp(100 * (90 - triaTimeLeft) / 90f, 0, 100);
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

    public void showPaused()
    {
        if (AppData.Instance.previousSuccessRates != null)
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
        // scrollSpeed = -2 - 1 * (0.02f * AppData.Instance.speedData.gameSpeed);
        //if (AppData.Instance.speedData.gameSpeed > 38f) gameSpeed = 38.0f;
        scrollSpeed = -2f - (0.05f * gameSpeed);

            hidePaused();
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
      //  Debug.Log($"trial time left :{triaTimeLeft}");
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
       // Debug.Log($"Game Update : {gameState}");
        if (isGamePaused) PauseGame();
        else if (gameState == GameStates.PAUSED) ResumeGame();

        // Run the game timer
        if (IsGamePlaying()) triaTimeLeft -= Time.deltaTime;
        // Debug.Log(isGameStarted);
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
                if (eventDelayTimer <= 0f && !runOnce)
                {
                    // Spawn a new ball.
                    AppData.Instance.aanController.ResetTrial();
                    // Get new target position.
                    // targetAngle = HomerTherapy.GetNewTargetPosition(arom, prom);
                    targetAngle = HomerTherapy.GetNewTargetPositionUniformFull(arom, aprom);
                    targetPosition = AngleToScreen(targetAngle);
                    spawnColumn();
                    MOVEDURATION = MoveDuration();
                  //  Debug.Log($"mm :{MOVEDURATION}");
                    // Set new trial in the AAN controller.
                    float checkFME = ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2")) ? gameSpeed : 20.0f;
                    AppData.Instance.aanController.SetNewTrialDetails(PlutoComm.angle, targetAngle, MOVEDURATION, checkFME);
                    runOnce = true;
                    eventDelayTimer = 0.05f;

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
                if (isTargetHit) gameState = GameStates.SUCCESS;
                if (isTargetMissed || isTimeUp ) gameState = GameStates.FAILURE;
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
                        gameState = (isTimeUp || gameOver) ? GameStates.STOP : GameStates.SPAWNTARGET;
                        isTargetHit = false;
                        isTargetMissed = false;
                        runOnce = false;
                    }
                    
                }
                // Wait for the user to score.
             
                break;
            case GameStates.PAUSED:
                //Debug.Log(isGamePaused);
                break;
            case GameStates.STOP:
                // Trial complete.
                // Update AANController.
                AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
                // Set AAN target if needed.
                isGameFinished = true;
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
                        score1.text = $"{(int)lastHighScore}";
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

    private void UpdateText()
    {
        timeLeftText.text = $": {(int)triaTimeLeft}";
        ScoreText.text = $"Score: {nSuccess}";
        gameSpeedViewer.text = $" GS : {(int)gameSpeed}";
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
            case PlutoAANController.PlutoAANState.AssistToTargetAtBoundary:
            case PlutoAANController.PlutoAANState.AssistToTargetInBoundary:
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
            float gameTime = HomerTherapy.TrialDuration - triaTimeLeft;
            Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
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
