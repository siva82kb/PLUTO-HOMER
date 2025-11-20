using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.U2D.IK;
using UnityEngine.UI;
using UnityEditor;

public class GameManager : MonoBehaviour
{
    public GameObject cloudPrefab;
    public AudioSource audioSource;
    public AudioClip[] audioClips;
    
    public static GameManager Instance { get; private set; }
    private CloudController playerCloud;
    public TextMeshProUGUI Score, Timer, rainT;
    private float gameDuration = 60f;
    private float gameTimer = 0f;
    public TextMeshProUGUI ScoreText, speed;
    private float lastHighScore;
     public Image loadingImage;
    public Text HST;
    private float PLAYSIZE;
    public GameObject SuccessRateBanner;

    private bool gameOver = false;
    private int mechanismSpeed = 30; // set from outside
    public int totalTargets;
    public int score = 0;
    private float triaTimeLeft;
    public TextMeshProUGUI scorex, finalScore;
    public GameObject HSC; //HighScoreCanvas
    private GameObject reminderPanel;
    private GameObject[] pauseObjects, finishObjects;
    public GameObject StartButton, PauseButton, ResumeButton, ExitButton, RestartButton;
    public TextMeshProUGUI prevSR, currSR, HS, status;

    public GameObject aromLeft;
    public GameObject aromRight;
    public GameObject gameSpeedControl, gameOverPanel;
    bool speedControlsVisible = false;
    private string exitScene = "CHGAME";

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
    public bool isTargetReached { get; private set; } = false;
    public bool isTargetMissed { get; private set; } = false;
    private bool changeScene = false;
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;
    public List<SeedController> seeds;
    private SeedController currentHighlighted;
    private GameObject[] detailObjects;

    private float rainTimer = 0f, convertedAngle=0f;
    private float highlightTimer = 0f;

    private float rainDurationToGrow = 0.5f;    // needs 1s of rain
    public float highlightDuration;     // highlighted for 3s
    private bool hasGrownThisCycle = false, runOnce = false;
    private SeedController lastHighlighted = null; // store last seed
    private GameSpeedController gsc = null;
    private float eventDelayTimer = 0f , gameSpeed, trialTimeLeft;
     private float targetAngle;
    private float maxTargetDur;
    private float targetPosition;
     private float[] arom;
    private float[] prom,aprom;
    
    private bool isButtonPressed = false, isPaused = true, isFinished = false;
    public Vector3? TargetPosition { get; private set; }
    public Vector3 PlayerPosition { get; private set; }


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
    }
    void Start()
    {
        initializeGame();
        GameObject cloudObj = Instantiate(cloudPrefab, new Vector3(0, 3.5f, 0), Quaternion.identity);
        playerCloud = cloudObj.GetComponent<CloudController>();
        PLAYSIZE = Camera.main.orthographicSize * Camera.main.aspect;

        // if (mechanismSpeed < 20) totalTargets = 12;
        // else if (mechanismSpeed < 30) totalTargets = 16;
        // else totalTargets = 20;

        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        detailObjects = GameObject.FindGameObjectsWithTag("detailViewer");

        // showPaused();
        HideFinished();
        // HidePaused();
        SetVisibility(false);

        //arom
        aromLeft.transform.position = new Vector3(
            AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMin),
            aromLeft.transform.position.y,
            aromLeft.transform.position.z
        );
        aromRight.transform.position = new Vector3(
            AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMax),
            aromRight.transform.position.y,
            aromRight.transform.position.z);
        HST.text = $"{Others.highestSuccessRate:F0} %";

         if (AppData.Instance.selectedMechanism.trialNumberDay >= AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
        {
            reminderPanel.SetActive(true);
        }
        else
        {
            reminderPanel.SetActive(false);
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

        HidePaused();
    }
    private float CalculateHighlightDuration(float mechanismSpeed)
    {
        float slope = (0.2f - 1.0f) / (40f - 10f);  // (-0.8 / 30)
        rainDurationToGrow = 1.0f + slope * (mechanismSpeed - 10f);

        // Clamp to safe range
        rainDurationToGrow = Mathf.Clamp(rainDurationToGrow, 0.2f, 1.0f);
        // Linear relation between speed (10 → 40) and duration (7s → 4.5s)
        float duration = -0.0833f * mechanismSpeed + 7.833f;

        // Clamp so it doesn’t go below or above intended range
        return Mathf.Clamp(duration, 4.5f, 7f);
    }

    void Update()
    {

        // if (gameOver || currentHighlighted == null) return;

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

        if (currentHighlighted == null) return;

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            speedControlsVisible = !speedControlsVisible;
            SetVisibility(speedControlsVisible);

        }

        // if (isGamePaused && gameState != GameStates.PAUSED) pauseGame();
        // else if (!isGamePaused && gameState == GameStates.PAUSED) resumeGame();
        // if ((isFinished && Input.GetKeyDown(KeyCode.P)) || (isFinished && isButtonPressed))
        // {

        //     if (AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.AROMMOVING
        //             || AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.IDLE)
        //     {
        // OnReStartButtonClick();
        //     }
        //     isButtonPressed = false;
        // }
        // PlayerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
//        Debug.Log($"chageScene - {changeScene && gameState == GameStates.DONE},   --{changeScene},--{gameState}");
       

        if (currentHighlighted != null)
        {
            highlightTimer += Time.deltaTime;

            // Calculate remaining time
            float remainingTime = Mathf.Max(0, highlightDuration - highlightTimer);

            // Fade out based on remaining time
            if (currentHighlighted.highLighter != null)
            {
                Renderer highlighterRenderer = currentHighlighted.highLighter.GetComponent<Renderer>();
                if (highlighterRenderer != null && !currentHighlighted.IsBeingRainedOn)
                {
                    Color originalColor = highlighterRenderer.material.color;
                    float alpha = Mathf.Clamp01(remainingTime / highlightDuration); // 1 → 0 as time runs out
                    originalColor.a = alpha;
                    highlighterRenderer.material.color = originalColor;

                    // When time runs out, ensure it stays invisible
                    if (remainingTime <= 0.01f)
                    {
                        originalColor.a = 0f;
                        highlighterRenderer.material.color = originalColor;
                        currentHighlighted.highLighter.SetActive(false);
                    }
                }
                else if (highlighterRenderer != null && currentHighlighted.IsBeingRainedOn)
                {
                    // Color originalColor = highlighterRenderer.material.color;
                    // originalColor.a = 1f;
                    // highlighterRenderer.material.color = originalColor;

                    //                 Color target = new Color(0f, 1f, 0f, 1f); // pure green, full alpha
                    // highlighterRenderer.material.color = Color.Lerp(
                    //     highlighterRenderer.material.color, 
                    //     target, 
                    //     Time.deltaTime * 5f // speed of transition
                    // );

                }

            }
            if (highlightTimer >= highlightDuration && !hasGrownThisCycle)
            {
                TargetMissed();
            }
        }


        // Growth logic
        if (!hasGrownThisCycle)
        {
            if (currentHighlighted.IsBeingRainedOn)
            {
                rainTimer += Time.deltaTime;
                
                if (rainTimer >= rainDurationToGrow)
                {
                    currentHighlighted.Grow();
                    TargetReached();
                    hasGrownThisCycle = true;
                    score++;
                }
            }
            else
            {
                rainTimer = Mathf.Max(0, rainTimer - Time.deltaTime);
            }
        }

        // Score.text = $"Score : {score}";
        Score.text = $"SCORE: {score}";
        Timer.text = "TIME: " + trialTimeLeft.ToString("F0");
    }

    void FixedUpdate()
    {
        PlutoComm.sendHeartbeat();

        RunGameStateMachine();
        PlayerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        TargetPosition = currentHighlighted != null ? currentHighlighted.transform.position : null;

    }

        public void increaseGameSpeed()
    {
           if (gameSpeed >= 40.0f) return;

        gameSpeed += 1.0f;
        gsc.gameSpeedText.text = $"{gameSpeed:F2}";
        highlightDuration = CalculateHighlightDuration(gameSpeed);

        Debug.Log($"gs - {AppData.Instance.speedData.gameSpeed} + {gameSpeed}");
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}'s  game speed decreased to {gameSpeed} - HightlightDuration decreased - set to {highlightDuration}");

        
    }
    public void decreaseGameSpeed()
    {
        string mech = PlutoComm.MECHANISMS[PlutoComm.mechanism];

        if ((mech != "FME1" && mech != "FME2" && gameSpeed <= 10.0f) ||
            ((mech == "FME1" || mech == "FME2") && gameSpeed <= 1.0f))
            return;

        gameSpeed -= 1.0f;
        gsc.gameSpeedText.text = $"{gameSpeed:F2}";
        highlightDuration = CalculateHighlightDuration(gameSpeed);
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}'s  game speed decreased to {gameSpeed} - HightlightDuration decreased - set to {highlightDuration}");
    }
    private void SetVisibility(bool state)
    {
        foreach (GameObject obj in detailObjects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }


    void initializeGame()
    {
        reminderPanel = GameObject.FindGameObjectWithTag("ReminderPanel");
        gameState = GameStates.WAITING;
        isGameStarted = false;
        isGameFinished = false;
        isGamePaused = false;
        isTargetMissed = false;
        isTargetReached = false;
        // Set current AROM and PROM.
        arom = AppData.Instance.selectedMechanism.CurrentArom;
        prom = AppData.Instance.selectedMechanism.CurrentProm;
        aprom = AppData.Instance.selectedMechanism.CurrentAProm;

        gameSpeed = AppData.Instance.speedData.gameSpeed; // degrees/sec
        highlightDuration = CalculateHighlightDuration(gameSpeed);
        // Attach PLUTO button event.
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        reminderPanel.SetActive(false);
        initializeGameSpeedController();

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

    private void onPlutoButtonReleased()
    {
        if (gameState == GameStates.WAITING) isGameStarted = true;
        else if (gameState != GameStates.STOP && gameState != GameStates.DONE) isGamePaused = !isGamePaused;
        else if (gameState == GameStates.DONE && isGameFinished) changeScene = true;

        AppLogger.LogInfo("PLUTO button pressed");
    }
  

    public void restartGame()
    {
        HideFinished();
        string currentSceneName = SceneManager.GetActiveScene().name;
        AppLogger.LogInfo($"The Game is restarted {currentSceneName}");
        SceneManager.LoadScene(currentSceneName);
    }

    public bool IsGamePlaying()
    {
        return gameState != GameStates.WAITING
            && gameState != GameStates.PAUSED
            && gameState != GameStates.STOP;
    }

    private void RunGameStateMachine()
    {
        if (IsGamePlaying()) trialTimeLeft -= Time.deltaTime;
        bool isTimeUp = trialTimeLeft <= 0;

        switch (gameState)
        {
            case GameStates.WAITING:
                showPaused();
                if (isGameStarted) gameState = GameStates.START;
                break;

            case GameStates.START:
                HidePaused();
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
                    //  targetPosition = AngleToScreen(targetAngle);
                    if (currentHighlighted != null)
                    {
                        currentHighlighted.SetHighlight(false);
                        currentHighlighted.highLighter.SetActive(false);
                        currentHighlighted = null;
                    }
                    HighlightRandomSeed();
                    highlightTimer = 0f;
                    rainTimer = 0f;
                    hasGrownThisCycle = false;
                    // Set new trial in the AAN controller.
                    float checkFME = ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2")) ? gameSpeed : 20.0f;
                    AppData.Instance.aanController.SetNewTrialDetails(PlutoComm.angle, convertedAngle, highlightDuration, checkFME);
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
                
                //gameState = GameStates.MOVE;
                break;

            case GameStates.MOVE:
                // Update AANController.
                AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, false);
                // Set AAN target if needed.
                if (AppData.Instance.aanController.stateChange) UpdatePlutoAANTarget();
                if (isTargetReached) gameState = GameStates.SUCCESS;
                if (isTargetMissed) gameState = GameStates.FAILURE;
                break;

            case GameStates.SUCCESS:
            case GameStates.FAILURE:
                if (eventDelayTimer <= 0f)
                {
                    eventDelayTimer = 0.6f;
                    if (currentHighlighted != null)
                    {
                        currentHighlighted.SetHighlight(false);
                        currentHighlighted.highLighter.SetActive(false);
                        currentHighlighted = null;
                    }
                }
                else
                {
                    eventDelayTimer -= Time.deltaTime;
                    if (eventDelayTimer <= 0f)
                    {
                        // Wait for the user to score.
                        gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNTARGET;
                        isTargetReached = false;
                        isTargetMissed = false;
                        runOnce = false;
                    }

                }
                
                break;

            case GameStates.PAUSED:
                AppLogger.LogInfo($"{AppData.Instance.selectedGameName} -- Game Paused");
                break;

            case GameStates.STOP:
                foreach (var seed in seeds)
                {
                 seed.SetHighlight(false);
                 seed.highLighter.SetActive(false);
                }

                // Update AANController.
                AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
                // Set AAN target if needed.
                isGameFinished = true;
                AppData.Instance.previousSuccessRates =null;
                if (AppData.Instance.speedData.gameSpeed != gameSpeed)
                {
                    AppData.Instance.speedData.setGameSpeed(gameSpeed);
                }
                
                if (AppData.Instance.aanController.stateChange) UpdatePlutoAANTarget();
                // Change to done only when the AAN Controller is AromMoving or Idle state.
                if (AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.AROMMOVING
                    || AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.IDLE)
                {
                    float gameTime = HomerTherapy.TrialDuration - trialTimeLeft;
                    Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
                    AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
                    gameState = GameStates.DONE;
                    lastHighScore = AppData.Instance.successRate * (PlutoAANController.MAXCONTROLBOUND - AppData.Instance.CurrentControlBound);
                    if (AppData.Instance.previousSuccessRates == null)
                    {
                        scorex.text = $"{(int)lastHighScore}";
                        Debug.Log($" Others.highestSuccessRate :{Others.highestSuccessRate} + {lastHighScore}");
                        if (lastHighScore > Others.highestSuccessRate)
                        {
                            StartCoroutine(ShowForSeconds(HSC, 1.3f));
                        }
                        else
                        {
                            AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(AppData.Instance.selectedMechanism.name, AppData.Instance.selectedGameName);
                            // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                            ShowFinished();
                        }


                    }
                    if (AppData.Instance.selectedMechanism.trialNumberDay == AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
                    {
                        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game finished and changed to Choose Mechanism scene due to allocated trials has over.");
                        SceneManager.LoadScene("CHMECH");
                    }
                }

                break;
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
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName} - highest score recorded");
        AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(AppData.Instance.selectedMechanism.name, AppData.Instance.selectedGameName);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
    float GetHighlightedSeedAngle()
    {
        if (currentHighlighted == null) return 0f;

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        // find true min/max X among all seeds
        foreach (var seed in seeds)
        {
            float x = seed.transform.position.x;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }

        float seedX = currentHighlighted.transform.position.x;

        // map seedX → angle
        return Mathf.Lerp(aprom[0], aprom[1], Mathf.InverseLerp(minX, maxX, seedX));
    }
float GetXPositionFromAngle(float targetAngle)
{
    float minX = float.MaxValue;
    float maxX = float.MinValue;

    // find true min/max X among all seeds (same as your original function)
    foreach (var seed in seeds)
    {
        float x = seed.transform.position.x;
        if (x < minX) minX = x;
        if (x > maxX) maxX = x;
    }

    // map angle → normalized position → X position
    float normalizedPosition = Mathf.InverseLerp(aprom[0], aprom[1], targetAngle);
    return Mathf.Lerp(minX, maxX, normalizedPosition);
}



    // public float AngleToScreen(float angle) => Mathf.Lerp(-PLAYSIZE, PLAYSIZE, (angle - aprom[0]) / (aprom[1] - aprom[0]));
    public float AngleToScreen(float angle) => Mathf.Lerp(-7.5f, 7.5f, (angle - aprom[0]) / (aprom[1] - aprom[0]));

    private void showPaused()
    {
         if(AppData.Instance.previousSuccessRates!=null)
        {
            SuccessRateBanner.SetActive(true);
            Debug.Log($" previous SR : {AppData.Instance.previousSuccessRates[0]}%");
            Debug.Log($"Current Success Rate : {AppData.Instance.previousSuccessRates[1]}%");
            prevSR.text = $" previous SR : {AppData.Instance.previousSuccessRates[0]}%";
            currSR.text = $"Current Success Rate : {AppData.Instance.previousSuccessRates[1]}%";
        }
        // Time.timeScale = 0;
        foreach (GameObject g in pauseObjects) g.SetActive(true);
    }

    public void HidePaused()
    {
        // Time.timeScale = 1;
         SuccessRateBanner.SetActive(false);
        foreach (GameObject g in pauseObjects) g.SetActive(false);
    }

    public void ShowFinished()
    {
        // Time.timeScale = 0;
        finalScore.text = $"{AppData.Instance.selectedGame.cummulativeHits:D4}";
        AppLogger.LogInfo($" {AppData.Instance.selectedGameName} - Game finished");
        // RestartButton.SetActive(true);
        foreach (GameObject g in finishObjects) g.SetActive(true);
    }

    public void HideFinished()
    {
        foreach (GameObject g in finishObjects) g.SetActive(false);
    }

    public void PauseGame()
    {
        _prevGameState = gameState;
        gameState = GameStates.PAUSED;
        isGamePaused = true;
        isPaused = true;
        Time.timeScale = 0;
        showPaused();
        // PauseButton.SetActive(false);
        // ResumeButton.SetActive(true);
        // ExitButton.SetActive(false);
    }

    public void EndGame()
    {
        gameOver = true;
        Time.timeScale = 1f;
        if (AppData.Instance.selectedMechanism.trialNumberDay >= AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
        {
              reminderPanel.SetActive(true);
            
        }
        else
        {
            reminderPanel.SetActive(false);

        }
        // ShowFinished();
        Debug.Log("Game Over – 60s finished!");
    }

    public void ResumeGame()
    {
        HidePaused();
        isGamePaused = false;
        isPaused = false;
        gameState = _prevGameState;
        Time.timeScale = 1;
        // PauseButton.SetActive(true);
        // ResumeButton.SetActive(false);
        ExitButton.SetActive(true);
        reminderPanel.SetActive(false);
            

         // Send PLUTO heartbeat
        PlutoComm.sendHeartbeat();

        if ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2"))
        {
            PlutoComm.setControlType("POSITIONAAN");
            PlutoComm.setControlBound(AppData.Instance.CurrentControlBound);
            PlutoComm.setControlDir(0);
        }
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName} -- game resumed");
    }

    public void TargetReached()
    {
        audioSource.PlayOneShot(audioClips[0]);
        isTargetReached = true;
        isTargetMissed = false;
        nSuccess++;
        Debug.Log("Target Reached");

    }

    public void TargetMissed()
    {
        if (isTargetMissed) return; // prevent double trigger
        audioSource.PlayOneShot(audioClips[1]);
        isTargetReached = false;
        isTargetMissed = true;
        highlightTimer = 0f;
        nFailure++;
        Debug.Log("Target Missed");
    }

    public void HighlightRandomSeed()
    {
        // clear old highlights
        foreach (var seed in seeds)
        {
            seed.SetHighlight(false);
            seed.highLighter.SetActive(false);
        }

        // if (score >= totalTargets || nTargets >= totalTargets)
        // {
        //     currentHighlighted = null;
        //     EndGame();
        //     return;
        // }

        // Clamp angle to -90..90
        //targetPosition = Mathf.Clamp(targetPosition, aprom[0], aprom[1]);

        float r = (aprom[1] - aprom[0]) / 5f;
        if (AppData.Instance.selectedMechanism.IsMechanism("HOC")) r = -r;
        Debug.Log($" aprom min :{aprom[0]}and max is {aprom[1]} and the r is {r} and the tarPOs {targetAngle}");
    // Map angle to bin (0..4)
        int bin = Mathf.FloorToInt((targetAngle + aprom[1]) / r); // (-90→0, 90→4)
        Debug.Log($" Bin Num: {bin}");
    bin = Mathf.Clamp(bin, 0, 4); // safety

        // pick the corresponding seed
        if (bin < seeds.Count)
        {
            currentHighlighted = seeds[bin];
        
        currentHighlighted.SetHighlight(true);
            nTargets++;
            lastHighlighted = currentHighlighted;
            // Convert its X position back to angle
            // float seedX = currentHighlighted.transform.position.x;
            convertedAngle = GetHighlightedSeedAngle();
     
            Debug.Log($"Angle {targetAngle:F1} → Highlighting Seed {bin+1} -> {convertedAngle}-> X POSITION{GetXPositionFromAngle(convertedAngle)}");
            // if (!currentHighlighted.IsFullyGrown)
            // {
            //     currentHighlighted.SetHighlight(true);
            //     nTargets++;
            //     lastHighlighted = currentHighlighted;
            //     Debug.Log($"Angle {targetPosition:F1} → Highlighting Seed {bin+1}");
            // }
            // else
            // {
            //     Debug.Log($"Seed {bin+1} is fully grown. Skipping.");
            // }
        }
        else
        {
            Debug.LogWarning($"No seed found for bin {bin}");
        }
    }

    public void Exit()
    {
        if(gameState == GameStates.DONE || gameState == GameStates.WAITING ){
            Time.timeScale = 1f;
            SceneManager.LoadScene(exitScene);
        }
        else
        {
            gameState = GameStates.STOP;
            float gameTime = HomerTherapy.TrialDuration - trialTimeLeft;
            Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
            AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
            if (AppData.Instance.speedData.gameSpeed != gameSpeed)  AppData.Instance.speedData.setGameSpeed(gameSpeed);
            AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
            gameState = GameStates.DONE;
            Time.timeScale = 1f;
            SceneManager.LoadScene(exitScene);
        }
    
    }
}