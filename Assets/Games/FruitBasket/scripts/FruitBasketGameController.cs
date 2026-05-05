using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using static FruitBasketGameController;

public class FruitBasketGameController : MonoBehaviour
{
    RectTransform canvasRect;
    // Start is called before the first frame update
    public static FruitBasketGameController Instance;
    public GameObject[] basketPrefabs; // 5 basket prefabs
    public List<GameObject> currentBaskets = new List<GameObject>();
    public Transform[] basketPositions;
    public Canvas mainCanvas;
    public GameObject gardenerPrefeb;
    public Transform gardenerPosition;
    float lastTargetReachTime = -1f;
    float lastInterTargetDuration = 0f;
    public Button restartBtn;
    public GameObject gameOver;
    public GameObject onPause;
    private GameObject gardenerGameObj;
    public Text bestScore, status;
    public TextMeshProUGUI scoreTxt,timertxt;
    public Text messageTxt;
    public AudioSource audioSource;
    public AudioClip[] soundClips;  // Add multiple clips in Inspector
    public GameObject aromLeft;
    public GameObject aromRight;
    private GameObject[] detailObjects;
    public GameObject HSC;
    public Text newRecordTxt;
    public Text preSuccRate;
    public Text currSuccRate;
    public GameObject reminderPanel, successRateBanner;

    private float PLAYSIZE;
    private float trialTimeLeft;
    private float[] arom;
    private float[] prom;
    private float[] aprom;
    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isSuccess { get; private set; } = false;
    public bool isFailure { get; private set; } = false;

    private float eventDelayTimer = 0f, gameSpeed;
    private bool runOnce = false, changeScene = false;

    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;
    private float lastHighScore;


    private string prevScene = "CHGAME";

    private  static float FRUITSTARTY;
    private  static float FRUITENDY;
    bool speedControlsVisible = false;
    public  float FRUITSPEED, MOVEDURATION;
    private float mechMinDuration, mechMaxDuration, mechMinThreshold, mechMaxThreshold;

    public GameObject gameSpeedControl, gameOverPanel;
    public TextMeshProUGUI  finalScore;
   
    private GameSpeedController gsc = null;
    public Vector3? PlayerPosition;
    public Vector3? TargetPosition;
    private GameObject targetTemp;
    private GameObject playerTemp;
    public enum GameStates
    {
        WAITFORSTART,
        STARTGAME,
        SPAWNFRUIT,
        MOVE,
        FAILURE,
        SUCCESS,
        PAUSE,
        STOP,
        DONE,
        NONE
    }
    private GameStates previosState;
    public GameStates gameState = GameStates.WAITFORSTART;
        public GameObject celebrationPanel;
    public TextMeshProUGUI scoreComparisonTxt;
    public TextMeshProUGUI yesterdayScoreTxt;
    public TextMeshProUGUI todayScoreTxt;
    public TextMeshProUGUI starCount;
    public GameObject GameOverStar, starLabel, instructionPanel;
    public int _starCount;
    private int[] scores;
         [Header("Location BGMs")]
    [SerializeField] private AudioClip ranipetBGM;
    [SerializeField] private AudioClip manipalBGM;
    [SerializeField] private AudioClip ludianaBGM;
    public AudioSource bgmAudioSource;
  
    public void setGameState(GameStates state)
    {
        gameState = state;
    }
    private void Awake()
    {
        Instance = this;
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
    void Start()
    {
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        initializeGameSpeedController();
        if(AppData.Instance.selectedGame.isAchievedToday())starLabel.GetComponent<Image>().color = Color.white;

        canvasRect = mainCanvas.GetComponent<RectTransform>();
        PLAYSIZE = canvasRect.rect.width / 2f;//canvasWidth

        FRUITSTARTY = (canvasRect.rect.height / 2f) - 50;//just below screen start 
        FRUITENDY = -(canvasRect.rect.height / 2f) + 120f;//just above the screen end

        instructionPanel.SetActive(false);

        // Attach PLUTO button event.
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;

        // Set current AROM and PROM.
        arom = AppData.Instance.selectedMechanism.CurrentArom;
        prom = AppData.Instance.selectedMechanism.CurrentProm;
        aprom = AppData.Instance.selectedMechanism.CurrentAProm;
        setMinMaxDurationOfMech();

        detailObjects = GameObject.FindGameObjectsWithTag("detailViewer");
        SetVisibility(false);
        gameOverPanel.SetActive(false);
        // Set the position of the AROM lines.
        aromLeft.transform.localPosition = new Vector3(
        AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMin),
        aromLeft.transform.localPosition.y,
        aromLeft.transform.localPosition.z
        );
        aromRight.transform.localPosition = new Vector3(
            AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMax),
            aromRight.transform.localPosition.y,
            aromRight.transform.localPosition.z
        );
        bestScore.text = $"{(int)Others.highestSuccessRate:F0}%";

        if (AppData.Instance.previousSuccessRates != null)
        {
            // successRateBanner.SetActive(true);
            preSuccRate.text = $"PrevSuccessRate:{AppData.Instance.previousSuccessRates[0].ToString("F0")}";
            currSuccRate.text = $"currSuccessRate:{AppData.Instance.previousSuccessRates[1].ToString("F0")}";
        }

        gameSpeed = AppData.Instance.speedData.gameSpeed;
        // FRUITSPEED = 70f + ((gameSpeed - 10f) / 30f) * 120f;

        // FRUITSPEED = Mathf.Clamp(FRUITSPEED, 70f, 250f);
        MOVEDURATION = GetTargetEndTime(gameSpeed);

        FRUITSPEED = (FRUITSTARTY - FRUITENDY) / MOVEDURATION;
        celebrationPanel.SetActive(false);
        updateStarCount();
        
        scores = GameFuncs.GetScores();
        Debug.Log($"{scores[0]}/{scores[1]}");
        AppLogger.LogInfo($"YesterDay's Score: {scores[1]} | Today's Score: {scores[0]}");

        
        if (AppData.Instance.selectedMechanism.trialNumberDay >= AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
        {
            reminderPanel.SetActive(true);

        }
        else
        {
            reminderPanel.SetActive(false);

        }

    }
    public void updateStarCount()
    {
        starCount.text = $"{AppData.Instance.selectedGame.cummulativeStars.ToString("D2")}";
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

    // Update is called once per frame
    void Update()
    {

        if (isGamePaused && gameState != GameStates.PAUSE) pauseGame();
        else if (!isGamePaused && gameState == GameStates.PAUSE) resumeGame();
          if (changeScene && gameState == GameStates.DONE)
        {
            restartGame();
            changeScene = false;
        }
        else
        {
            changeScene = false;
        }

        updateGUI();
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            speedControlsVisible = !speedControlsVisible;
            SetVisibility(speedControlsVisible);

            Debug.Log("Speed controls " + (speedControlsVisible ? "enabled" : "disabled"));
        }
    }
    public void FixedUpdate()
    {
        PlutoComm.sendHeartbeat();

        runStateMachine();

        if (!isGamePlaying()) return;
        playerTemp = GameObject.FindGameObjectWithTag("Player");
        PlayerPosition = playerTemp != null ? playerTemp.transform.localPosition : null;
        targetTemp = GameObject.FindGameObjectWithTag("Target");
        TargetPosition = targetTemp != null ? targetTemp.transform.localPosition : null;

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
    public void restartGame()
    {
        gameOverPanel.SetActive(false);
        string currentSceneName = SceneManager.GetActiveScene().name;
        // AppLogger.LogInfo($"The Game is restarted {currentSceneName}");
        SceneManager.LoadScene(currentSceneName);
    }
    public void runStateMachine()
    {
        // if (isGamePlaying()) trialTimeLeft -= Time.deltaTime;
        if (isGamePlaying() && trialTimeLeft > 0f)
        {
            trialTimeLeft -= Time.deltaTime;
        }
        bool isTimeUp = trialTimeLeft < 0;
        switch (gameState)
        {
            case GameStates.WAITFORSTART:
                if (isGameStarted) gameState = GameStates.STARTGAME;
                break;
            case GameStates.STARTGAME:
                startGame();
                gameState = GameStates.SPAWNFRUIT;
                break;
            case GameStates.SPAWNFRUIT:
                if (eventDelayTimer <= 0f && !runOnce)
                {
                    // Reset AAN Controller
                    AppData.Instance.aanController.ResetTrial();

                    //Get random Target Angle
                    float targetAngle = HomerTherapy.GetNewTargetPositionUniformFull(arom, aprom);

                    //find position of the target angle
                    Vector3 targetpos = new Vector3(AngleToScreen(targetAngle), (canvasRect.rect.height / 2f) - 50, 0f);

                    //find the the correspond fruitBasket
                    GameObject targetBasket = GetNearestBasket(targetpos);

                    //spawn the Fruit
                    FruitSpawner.instance.spawnFruit(targetBasket);

                    //For AAN change the target angle to the fruitBasket
                    targetAngle = ScreenToAngle(targetBasket.transform.localPosition.x);

                    // Set new trial in the AAN controller.
                    float checkFME = ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2")) ? gameSpeed : 20.0f;

                    AppData.Instance.aanController.SetNewTrialDetails(PlutoComm.angle, targetAngle, MOVEDURATION, checkFME);
                    nTargets++;
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
                //check success or failure
                if (isSuccess) gameState = GameStates.SUCCESS;
                if (isFailure) gameState = GameStates.FAILURE;
                break;

            case GameStates.PAUSE:
                // AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game paused");
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
                        // Wait for the gamestate to be logged.
                        isFailure = false;
                        isSuccess = false;
                        gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNFRUIT;
                        runOnce = false;
                    }
                }
                break;
            // case GameStates.STOP:
              
            //     AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
            //     // Set AAN target if needed.

            //     AppData.Instance.previousSuccessRates = null;
            //     if (AppData.Instance.speedData.gameSpeed != gameSpeed)
            //     {
            //         AppData.Instance.speedData.setGameSpeed(gameSpeed);
            //     }
            //         AppData.Instance.speedData.setMoveDuration(MOVEDURATION);
            //      instructionPanel.SetActive(true);


            //     if (AppData.Instance.aanController.stateChange) UpdatePlutoAANTarget();
            //     // Change to done only when the AAN Controller is AromMoving or Idle state.
            //     if (AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.AROMMOVING
            //         || AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.IDLE)
            //     {

                   
            //         float gameTime = HomerTherapy.TrialDuration - trialTimeLeft;
            //         Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
            //         instructionPanel.SetActive(false);
                    
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
            //          lastHighScore = AppData.Instance.successRate * (PlutoAANController.MAXCONTROLBOUND - AppData.Instance.CurrentControlBound);
            //         if (AppData.Instance.previousSuccessRates == null)
            //         {
            //             Debug.Log($" LHS : {lastHighScore} -- {Others.highestSuccessRate}");
            //             // if (lastHighScore > Others.highestSuccessRate)
            //             // {
            //             //     StartCoroutine(ShowForSeconds(HSC, 1.3f));
            //             // }
            //             // else
            //             // {
            //                 AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(AppData.Instance.selectedMechanism.name, AppData.Instance.selectedGameName);
            //                 if (AppData.Instance.selectedMechanism.trialNumberDay == AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
            //                 {
            //                     AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game finished and changed to Choose Mechanism scene due to allocated trials has over.");
            //                     SceneManager.LoadScene("CHMECH");
            //                     return;
            //                 }
            //                 // finalScore.text = $"{AppData.Instance.selectedGame.cummulativeHits:D4}";

            //                 // gameOverPanel.SetActive(true);
            //                 // finalScore.text = $"{nSuccess:D3}";

            //                 AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game finished");

                            
            //                 // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            //             // }
                       

            //         }
                    
            //         endGame();

                   
            //     }
            //     break;
            
            case GameStates.STOP:
                AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
                
                if (AppData.Instance.aanController.stateChange) 
                    UpdatePlutoAANTarget();
                
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
                 if (AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.AROMMOVING 
                    || AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.IDLE
                )
                {
                    ProceedToGameEnd();
                }
                
                // Original logic for when state is AROMMOVING or after delay completes
               
                break;
            case GameStates.DONE:
           
                if (!gardener.instance.IsGardenerCollecting && !HSC.gameObject.activeSelf)
                {
                    //make the gardener visible and start collect the missed fruits

                    gardener.instance.gardenerprefeb.gameObject.SetActive(true);
                    gardener.instance.gardenerprefeb.transform.SetAsLastSibling();

                    gardener.instance.StartGardenerCollecting();
                }
              
                break;
        }
    }
    public void increaseGameSpeed()
    {
        if (gameSpeed >= PlutoAANController.MAX_SPEED) return;

        gameSpeed += 1.0f;
        gsc.gameSpeedText.text = $"{(int)gameSpeed}";

        UpdateFruitFallSpeedAndDuration();
        Debug.Log($"gs - {AppData.Instance.speedData.gameSpeed} + {gameSpeed} + {FRUITSPEED}");
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}'s game speed increased to {gameSpeed} and the Fruit Speed is {FRUITSPEED}");
        AppData.Instance.annotation=$"GS: {gameSpeed} | MT: {MOVEDURATION:F2}";

    }
    public void decreaseGameSpeed()
    {
        if (gameSpeed <= PlutoAANController.MIN_SPEED) return;

        gameSpeed -= 1.0f;
        gsc.gameSpeedText.text = $"{(int)gameSpeed}";

        UpdateFruitFallSpeedAndDuration();
        Debug.Log($"gs - {AppData.Instance.speedData.gameSpeed} + {gameSpeed} + {FRUITSPEED}");

        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}'s game speed increased to {gameSpeed} and the Fruit Speed is {FRUITSPEED}");
        AppData.Instance.annotation=$"GS: {gameSpeed} | MT: {MOVEDURATION:F2}";




    }
    float GetTargetEndTime(float gameSpeed)
    {
        float t = (gameSpeed - HomerTherapy.MinSpeed) / (HomerTherapy.MaxSpeed - HomerTherapy.MinSpeed);
        t = Mathf.Clamp01(t);

        return Mathf.Lerp(mechMaxDuration,mechMinDuration, t);
    }
    void UpdateFruitFallSpeedAndDuration()
    {
        MOVEDURATION = GetTargetEndTime(gameSpeed);

         FRUITSPEED= (FRUITSTARTY - FRUITENDY) / MOVEDURATION;
    }
    private void SetVisibility(bool state)
    {
        foreach (GameObject obj in detailObjects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
    private IEnumerator ShowForSeconds(GameObject obj, float seconds)
    {
        newRecordTxt.text = $"{(int)lastHighScore}%";
        obj.SetActive(true);
        obj.transform.SetAsLastSibling();
        //loadingImage.gameObject.SetActive(true);
        //loadingImage.fillAmount = 0f;
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game's highest score recorded ");


        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            //loadingImage.fillAmount = Mathf.Clamp01(elapsed / seconds);
            yield return null;
        }

        obj.SetActive(false);
        //loadingImage.gameObject.SetActive(false);
        AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(AppData.Instance.selectedMechanism.name, AppData.Instance.selectedGameName);
        if (AppData.Instance.selectedMechanism.trialNumberDay == AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
        {
            SceneManager.LoadScene("CHMECH");

            
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
           
    }
    public bool isGamePlaying()
    {
        return gameState != GameStates.WAITFORSTART &&
                gameState != GameStates.PAUSE&&
                gameState != GameStates.DONE;
    }
    public void PlaySound(int index)
    {
        if (index >= 0 && index < soundClips.Length)
        {
            audioSource.PlayOneShot(soundClips[index]);
        }
    }
    public GameObject GetNearestBasket(Vector3 targetPosition)
    {
        GameObject nearestBasket = null;
        float minDistance = Mathf.Infinity;

        foreach (var basket in currentBaskets)
        {
            float distance = Vector3.Distance(targetPosition, basket.transform.localPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestBasket = basket;
            }
        }

        return nearestBasket;
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
            case PlutoAANController.PlutoAANState.ASSISTTOTARGETINBOUNDARY:
            case PlutoAANController.PlutoAANState.ASSISTTOTARGETATBOUNDARY:
                // Set AAN Target to the nearest AROM edge.
                float[] _newAanTarget = AppData.Instance.aanController.GetNewAanTarget();
                PlutoComm.setAANTarget(_newAanTarget[0], _newAanTarget[1], _newAanTarget[2], _newAanTarget[3]);
                break;
        }
    }

    public float AngleToScreen(float angle) => Mathf.Lerp(-PLAYSIZE, PLAYSIZE, (angle - aprom[0]) / (aprom[1] - aprom[0]));
    public float ScreenToAngle(float screenX) =>
    Mathf.Lerp(aprom[0], aprom[1], Mathf.InverseLerp(-PLAYSIZE, PLAYSIZE, screenX));

    public void startGame()
    {
        reminderPanel.SetActive(false);
        successRateBanner.SetActive(false);
        setupBasketsForTrial();
        AppData.Instance.StartNewTrial();

        gsc.sessionDetailsText.text = $"sessionNo: {AppData.Instance.currentSessionNumber}\n" +
             $"trialNo: {AppData.Instance.selectedMechanism.trialNumberSession}\n" +
             $"CB: {AppData.Instance.CurrentControlBound}";
              
        preSuccRate.gameObject.SetActive(false);
        currSuccRate.gameObject.SetActive(false);

        // Put PLUTO in the AAN mode.
        if ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2"))
        {
            PlutoComm.setControlType("POSITIONAAN");
            PlutoComm.setControlBound(AppData.Instance.CurrentControlBound);
            PlutoComm.setControlDir(0);
        }
        // Reset the AAN controller.
        AppData.Instance.aanController.ResetTrial();

        //reset Game values
        nTargets = 0;
        nSuccess = 0;
        nFailure = 0;
        trialTimeLeft = HomerTherapy.TrialDuration;
     
        FruitSpawner.instance.setPrePosition(Vector3.zero);
      
    }
    public void endGame()
    {
        isGameFinished = true;
        
        gameState = GameStates.DONE;

    }
    public void pauseGame()
    {
        previosState = gameState;
        gameState = GameStates.PAUSE;
        Time.timeScale = 0f;
                AppLogger.LogInfo("Game Paused");

    }
    public void resumeGame()
    {
        gameState = previosState;
        Time.timeScale = 1f;
        isGamePaused = false;
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

    void OnHatTargetReached()
    {
        float now = Time.time;

        if (lastTargetReachTime > 0f)
        {
            lastInterTargetDuration = now - lastTargetReachTime;
            Debug.Log($"fruit → basket duration: {lastInterTargetDuration:F2} sec");
        }

        lastTargetReachTime = now;
    }
    
    public void setSuccess()
    {
        OnHatTargetReached();
        isSuccess  = true;
        nSuccess++;
    }
    public void setFailure()
    {
        OnHatTargetReached();
        isFailure = true;
        nFailure++;
    }

    public void restart()
    {
         Destroy(gardenerGameObj);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void exitGame()
    {if(gameState == GameStates.DONE || gameState == GameStates.WAITFORSTART || gameState == GameStates.PAUSE){
            Time.timeScale = 1f;
            AppLogger.LogInfo("Exit Game");

            SceneManager.LoadScene(prevScene);
        }
        else
        {
            gameState = GameStates.STOP;
            float gameTime = HomerTherapy.TrialDuration - trialTimeLeft;
            Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
            AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
            if (AppData.Instance.speedData.gameSpeed != gameSpeed)  AppData.Instance.speedData.setGameSpeed(gameSpeed);
            AppData.Instance.speedData.setMoveDuration(MOVEDURATION);

            // AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
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
                        todayScoreTxt.text = $"{(scores[0]+nSuccess):D4}";
                    }
                    if (celebrationPanel.gameObject.activeSelf)
                    {
                        updateStarCount();
                        scoreComparisonTxt.text = $"{(scores[0] + nSuccess).ToString("D3")}";
                    }
            gameState = GameStates.DONE;
            Time.timeScale = 1f;
            SceneManager.LoadScene(prevScene);
        // AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game exit");
            AppLogger.LogInfo("Exit Game");


        }
    }
    //shuffle the basket for every trail
    public void setupBasketsForTrial()
    {
        // Destroy old baskets if any
        foreach (var basket in currentBaskets)
        {
            if (basket != null) Destroy(basket);
        }
        currentBaskets.Clear();

        // Shuffle positions
        List<Transform> shuffledPositions = new List<Transform>(basketPositions);
        for (int i = 0; i < shuffledPositions.Count; i++)
        {
            Transform temp = shuffledPositions[i];
            int randomIndex = Random.Range(i, shuffledPositions.Count);
            shuffledPositions[i] = shuffledPositions[randomIndex];
            shuffledPositions[randomIndex] = temp;
        }

        // Spawn baskets at shuffled positions
        for (int i = 0; i < basketPrefabs.Length; i++)
        {
            GameObject basket = Instantiate(basketPrefabs[i], shuffledPositions[i].position, Quaternion.identity, mainCanvas.transform);
            basket.transform.localScale = Vector3.one; // Maintain proper UI scaling
            currentBaskets.Add(basket);
        }
        gardenerGameObj = Instantiate(gardenerPrefeb, gardenerPosition.position, Quaternion.identity, mainCanvas.transform);
        FruitSpawner.instance.targetPrefebs = currentBaskets.ToArray();
    }
    public void updateGUI()
    {

        onPause.gameObject.SetActive(isGamePaused);
        gameOver.gameObject.SetActive(gameState == GameStates.DONE);
        restartBtn.gameObject.SetActive(gameState == GameStates.DONE);
       
        messageTxt.text = (gameState == GameStates.WAITFORSTART)
                        ? "PRESS PLUTO BUTTON TO START GAME"
                        : "";

        timertxt.text = $"Timer:{Mathf.Max(0, Mathf.CeilToInt(trialTimeLeft)):D2}s";
        scoreTxt.text = $"Score:{nSuccess:D2}";
    }
    private void onPlutoButtonReleased()
    {
        if (gameState == GameStates.WAITFORSTART) isGameStarted = true;
        else if (gameState != GameStates.STOP && gameState != GameStates.DONE) isGamePaused = !isGamePaused;
        else if (gameState == GameStates.DONE && isGameFinished) changeScene = true;
        
        AppLogger.LogInfo("PLUTO button pressed");

    }

    private void ProceedToGameEnd()
{
    AppData.Instance.previousSuccessRates = null;
    if (AppData.Instance.speedData.gameSpeed != gameSpeed)
    {
        AppData.Instance.speedData.setGameSpeed(gameSpeed);
    }
    AppData.Instance.speedData.setMoveDuration(MOVEDURATION);
    instructionPanel.SetActive(true);

    float gameTime = HomerTherapy.TrialDuration - trialTimeLeft;
    Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
    instructionPanel.SetActive(false);
    
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
        todayScoreTxt.text = $"{(scores[0] + nSuccess):D4}";
    }
    if (celebrationPanel.gameObject.activeSelf)
    {
        updateStarCount();
        scoreComparisonTxt.text = $"{(scores[0] + nSuccess).ToString("D3")}";
    }
    
    lastHighScore = AppData.Instance.successRate * (PlutoAANController.MAXCONTROLBOUND - AppData.Instance.CurrentControlBound);
    
    if (AppData.Instance.previousSuccessRates == null)
    {
        Debug.Log($" LHS : {lastHighScore} -- {Others.highestSuccessRate}");
        AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(
            AppData.Instance.selectedMechanism.name, 
            AppData.Instance.selectedGameName);
            
    if (AppData.Instance.selectedMechanism.trialNumberDay == AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
    {
        AppLogger.LogInfo("Game over and changed to Choose Mechanism scene due to allocated trials has over.");
        SceneManager.LoadScene("CHMECH");
    }
        
        AppLogger.LogInfo("Game Over");
    }
            PlutoComm.setControlType("NONE");

    
    endGame();
}

private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= onPlutoButtonReleased;
        }
    }
}


