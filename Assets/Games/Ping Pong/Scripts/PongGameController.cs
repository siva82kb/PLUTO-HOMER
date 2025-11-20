using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using UnityEngine.Analytics;
using UnityEngine.UI;
using TMPro;

public class PongGameController : MonoBehaviour
{
    public static PongGameController Instance {  get; private set; }
    GameObject[] pauseObjects, finishObjects;
    public BoundController rightBound;
    public BoundController leftBound;

    public EnemyController enemy;
    public BallController ballSpeed;
    public GameObject ball;
    public Text pointCounter, gameOverText;
    public bool isFinished;
    private bool isButtonPressed = false;
    public bool playerWon, enemyWon;
    public AudioClip[] audioClips; 
    public int enemyScore, playerScore;
    public Vector2 targetPosition;
    public float targetPositiony;
    private GameObject reminderPanel;

    private bool isPaused = true;
    private int winningScore = 3;

    // Target and player positions
    public Vector3? TargetPosition { get; private set; }
    public Vector3 PlayerPosition { get; private set; }

    //pong game events and related variables.
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

    public bool isGameStarted { get; private set; } = false;
    public bool isGameFinished { get; private set; } = false;
    public bool isGamePaused { get; private set; } = false;
    public bool isBallSpawned { get; private set; } = false;
    public bool isBallHitted { get; private set; } = false;
    public bool isBallMissed { get; private set; } = false;
    public bool enemyHit = false;
    // Target and player positions.
    //scene
    private static string prevScene = "CHGAME";
    private float[] arom;
    private float[] prom, aprom;
    private float targetAngle;
    
    private float playerPosition;
    private  GameObject targetTemp;
    public  GameObject SuccessRateBanner,ExitButton;
    public Text prevSR, currSR, HS, status;
    public TextMeshProUGUI finalScore;

    public GameObject HSC; //HighScoreCanvas
    public TextMeshProUGUI score;
    private float lastHighScore;
    public Text timeLeftText, gameSpeedViewer;
    static float playSize;
    // static float topBound = 5.5F;
    static float topBound = 6F;
    private GameObject[] detailObjects;
    static float bottomBound = -6F;
    public GameObject aromLeft;
    public GameObject aromRight;
    private float triaTimeLeft;
    private float moveTimeLeft;
    public float gs;

    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;

    private float MOVEDURATION, eventDelayTimer = 0f, gameSpeed;
     public GameObject gameSpeedControl, gameOverPanel;
    private GameSpeedController gsc = null;
    public Image loadingImage;
   
    bool speedControlsVisible = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != null)
        {
            Destroy(gameObject);
        }
        enemy.speedDefault = 3.0f+ (0.04f * AppData.Instance.speedData.gameSpeed);
        ballSpeed.speed = 1.5f + (0.04f * AppData.Instance.speedData.gameSpeed);
    }
    void Start()
    {
        InitializeGame();
        initializeGameSpeedController();
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        detailObjects = GameObject.FindGameObjectsWithTag("detailViewer");
        targetPosition = new Vector2(5.95f, 0f);
        hideFinished();
        showPaused();
        SetVisibility(false);
        playSize = Camera.main.orthographicSize;
        GameObject ballClone;
        ballClone = Instantiate(ball, this.transform.position, this.transform.rotation) as GameObject;
        ballClone.transform.SetParent(this.transform);

        //arom
        aromLeft.transform.position = new Vector3(
            aromLeft.transform.position.x,
            AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMin),
            aromLeft.transform.position.z
        );
        aromRight.transform.position = new Vector3(
            aromRight.transform.position.x,
            AngleToScreen(AppData.Instance.selectedMechanism.currRom.aromMax),
            aromRight.transform.position.z
        );
        HS.text = $"{ Others.highestSuccessRate:F0} %";

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
    void Update()
    {

        pointCounter.text = enemyScore + "\t\t" +
            playerScore;

        //if (isGamePaused && gameState != GameStates.PAUSED) 

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            speedControlsVisible = !speedControlsVisible;
        SetVisibility(speedControlsVisible);

        }


        //Ball Spawn
        if (transform.childCount == 0)
        {
            GameObject ballClone;
            ballClone = Instantiate(ball, this.transform.position, this.transform.rotation) as GameObject;
            ballClone.transform.SetParent(this.transform);
            EnemyController.stopWatch = 0;
        }

        if (isFinished)
        {
            //showFinished();
        }
        else
        {
            if ((Time.timeScale == 0) && !isPaused && !isFinished)
            {
                Time.timeScale = 1;
            }
        }

        if ((Input.GetKeyDown(KeyCode.P) && !isFinished) || (isButtonPressed && !isFinished))
        {
            if (!isPaused)
            {
                pauseGame();
            }
            else
            {
                resumeGame();
                isGameStarted = true;
            }
            isButtonPressed = false;
        }
        // if (isGamePaused && gameState != GameStates.PAUSED) pauseGame();
        // else if (!isGamePaused && gameState == GameStates.PAUSED) resumeGame();
        if ((isFinished && Input.GetKeyDown(KeyCode.P)) || (isFinished && isButtonPressed))
        {
                Reload();
            isButtonPressed = false;
        }
        Debug.Log($" gamestate : {gameState} + {isGameStarted}");


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
    

    private void SetVisibility(bool state)
    {
        foreach (GameObject obj in detailObjects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }

    private void gameEnd()
    {
        Camera.main.GetComponent<AudioSource>().Stop();

        playAudio(enemyScore>playerScore ? 1 : 0);
        //showFinished();
        Time.timeScale = 0;
    }

    public void increaseGameSpeed()
    {
        if (gameSpeed >= 40.0f) return;

        gameSpeed += 1.0f;
        gsc.gameSpeedText.text = $"{(int)gameSpeed}";

        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}'s game speed increased to {gameSpeed}, Ball speed is {ballSpeed.speed}, Enemy Speed is {enemy.speedDefault}");


        UpdateGameSpeeds();
    }
    public void decreaseGameSpeed()
    {
        bool isFME = PlutoComm.MECHANISMS[PlutoComm.mechanism] == "FME1" || PlutoComm.MECHANISMS[PlutoComm.mechanism] == "FME2";

        if (isFME && gameSpeed <= 1.0f) return;
        if (!isFME && gameSpeed <= 10.0f) return;

        gameSpeed -= 1.0f;
        gsc.gameSpeedText.text = $"{(int)gameSpeed}";

        UpdateGameSpeeds();
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}'s game speed decreased to {gameSpeed}, Ball speed is {ballSpeed.speed}, Enemy Speed is {enemy.speedDefault}");


    }
    private void UpdateGameSpeeds()
    {
        float speed = 3.0f + (0.04f * gameSpeed);
        float ballSpd = 1.5f + (0.04f * gameSpeed);

        bool isFME = PlutoComm.MECHANISMS[PlutoComm.mechanism] == "FME1" || PlutoComm.MECHANISMS[PlutoComm.mechanism] == "FME2";

        enemy.speedDefault = Mathf.Clamp(speed, isFME ? 2.0f : 3.0f, 6.0f);
        ballSpeed.speed = Mathf.Clamp(ballSpd, isFME ? 0.9f : 1.5f, 5.0f);
        gs = ballSpeed.speed;
    }

    private void pauseGame()
    {
        _prevGameState = gameState;
        gameState = GameStates.PAUSED;
        Time.timeScale = 0;
        isGamePaused = true;
        isPaused = true;
        showPaused();
        // ExitButton.SetActive(false);
    }

    private void resumeGame()
    {
        gameState = _prevGameState;
        reminderPanel.SetActive(false);

        Time.timeScale = 1;
        isGamePaused = false;
        isPaused = false;
        hidePaused();
        ExitButton.SetActive(true);
        PlutoComm.sendHeartbeat();
        if ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2"))
        {
            PlutoComm.setControlType("POSITIONAAN");
            PlutoComm.setControlBound(AppData.Instance.CurrentControlBound);
            PlutoComm.setControlDir(0);
        }
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName} -- game resumed");
        
    }

    private float timeToReach(){
        if (targetTemp != null)
        {
            Rigidbody2D ballRB = targetTemp.GetComponent<Rigidbody2D>();

            // Only predict if the ball is moving toward the player (x velocity positive).
            if (ballRB.velocity.x > 0)
            {
                // Calculate approximate time for the ball to reach the player's bound.
                float timeToArrival = Mathf.Abs((6f - ball.transform.position.x) / ballRB.velocity.x);
                return 0.5f * timeToArrival;
            }
        }
        return 0f;
    }
    public void ExitGame()
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
            if (AppData.Instance.speedData.gameSpeed != gameSpeed)  AppData.Instance.speedData.setGameSpeed(gameSpeed);
            AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
            gameState = GameStates.DONE;
            Time.timeScale = 1f;
            SceneManager.LoadScene(prevScene);
            AppLogger.LogInfo($"{AppData.Instance.selectedGameName} -- Exit from game");
            
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
        // showFinished();
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName} -- game's highest score recorded");
        
        // gameEnd();
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
    }
    public void Reload()
    {
        // playerScore = enemyScore = 0;
        // hideFinished();
        string currentSceneName = SceneManager.GetActiveScene().name;
        AppLogger.LogInfo($"The Game is restarted {currentSceneName}");
        SceneManager.LoadScene(currentSceneName);

    }

    void playAudio(int clipNumber)
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = audioClips[clipNumber];
        audio.Play();
    } 

    public void showPaused()
    {
        if(AppData.Instance.previousSuccessRates!=null)
        {
            SuccessRateBanner.SetActive(true);
            prevSR.text = $" previous SR : {AppData.Instance.previousSuccessRates[0]}%";
            currSR.text = $"Current Success Rate : {AppData.Instance.previousSuccessRates[1]}%";
        }
        else
        {
            Debug.Log("Hello gamestate");
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

    public void showFinished()
    {
        // finalScore.text = $"{nSuccess:D3}";
        finalScore.text = $"{AppData.Instance.selectedGame.cummulativeHits:D4}";

        foreach (GameObject g in finishObjects)
        {
            g.SetActive(true);
        }
        
        // if(AppData.Instance.previousSuccessRates!=null)
        // {
        //     SuccessRateBanner.SetActive(true);
        //     prevSR.text = $" previous SR : {AppData.Instance.previousSuccessRates[0]}%";
        //     currSR.text = $"Current Success Rate : {AppData.Instance.previousSuccessRates[1]}%";
        // }
        gameOverText.text = (playerScore >= enemyScore) ? "GAME OVER!\nPLAYER WON!" : "GAME OVER!\nENEMY WON!";
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName} -- game finished");
         
    }

    public void hideFinished()
    {
        foreach (GameObject g in finishObjects)
        {
            g.SetActive(false);
        }
    }

 
//AAN

    private void RunGameStateMachine()
    {
        // Check if the game is to be paused or unpaused.
       // Debug.Log("Game Update");
        if (isGamePaused) pauseGame();
        else if (gameState == GameStates.PAUSED) resumeGame();

        // Run the game timer
        if (IsGamePlaying()) triaTimeLeft -= Time.deltaTime;
       // Debug.Log(isGameStarted);
        UpdateText();
        // Act according to the current game state.
        bool isTimeUp = triaTimeLeft <= 0;
        switch (gameState)
        {
            case GameStates.WAITING:
                showPaused();
                // Check of game has been started.
                if (isGameStarted) gameState = GameStates.START;
                Debug.Log($" gamestate x1: {gameState} + {isGameStarted}");

                break;
            case GameStates.START:
                hidePaused();
               // HideFinished();
                // Start the game.
                StartGame();
                gameState = GameStates.SPAWNBALL;
                break;
            case GameStates.SPAWNBALL:
                // Spawn a new ball.
                if(!enemyHit) return;

                AppData.Instance.aanController.ResetTrial();
                // Get new target position.
                // targetAngle = HomerTherapy.GetNewTargetPosition(arom, prom);
                // targetAngle = HomerTherapy.GetNewTargetPositionUniformFull(arom, prom);
                // targetPositiony = AngleToScreen(targetAngle);
                MOVEDURATION = timeToReach();
                //setTarget();
                // Set new trial in the AAN controller.
                float checkFME = ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2")) ? gameSpeed : 20.0f;
                AppData.Instance.aanController.SetNewTrialDetails(PlutoComm.angle, targetAngle, MOVEDURATION, checkFME);
                gameState = GameStates.MOVE;
                break;
            case GameStates.MOVE:
                // Update AANController.
                AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, false);
                enemyHit = false;
                // Set AAN target if needed.
                if (AppData.Instance.aanController.stateChange) UpdatePlutoAANTarget();
                // Wait for the user to success or fail.
                if (isBallHitted) gameState = GameStates.SUCCESS;
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
                       // Debug.Log(gameState);
                        // Wait for the user to score.
                        gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNBALL;

                        isBallHitted = false;
                        isBallMissed = false;
                        targetAngle = HomerTherapy.GetNewTargetPositionUniformFull(arom, aprom);
                        targetPositiony = AngleToScreen(targetAngle);
                        setTarget();
                    }
                }
                
                break;
            case GameStates.PAUSED:
                AppLogger.LogInfo($"{AppData.Instance.selectedGameName} -- game paused");

                //Debug.Log(isGamePaused);
                break;
            case GameStates.STOP:
                // Trial complete.
                isFinished = true;
                // Update AANController.
                AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
                // Set AAN target if needed.

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
                    float gameTime = HomerTherapy.TrialDuration - triaTimeLeft;
                    Others.gameTime = (gameTime < HomerTherapy.TrialDuration) ? gameTime : HomerTherapy.TrialDuration;
                    AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
                    gameState = GameStates.DONE;
                    lastHighScore = AppData.Instance.successRate * (PlutoAANController.MAXCONTROLBOUND - AppData.Instance.CurrentControlBound);
                     if (AppData.Instance.selectedMechanism.trialNumberDay == AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
                    {
                        AppLogger.LogInfo($"{AppData.Instance.selectedGameName}-- game finished and changed to Choose Mechanism scene due to allocated trials has over.");
                        SceneManager.LoadScene("CHMECH");
                    }
                    
                   if (AppData.Instance.previousSuccessRates == null)
                    {
                        score.text = $"{(int)lastHighScore}";
                        if (lastHighScore > Others.highestSuccessRate)
                        {
                            StartCoroutine(ShowForSeconds(HSC, 1.3f));
                        }
                        else
                        {
                            AppData.Instance.previousSuccessRates = AppData.Instance.userData.GetLastTwoSuccessRates(AppData.Instance.selectedMechanism.name, AppData.Instance.selectedGameName);
                            showFinished();
                            gameEnd();
                        }


                    }
                   
                }
                break;
        }
    }

    private void UpdatePlutoAANTarget()
    {
        switch(AppData.Instance.aanController.state)
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

    private void setTarget(){

        targetPosition = new Vector2(6f,targetPositiony);
        GameObject t = GameObject.FindGameObjectWithTag("targetPointer");
       // t.transform.position = targetPosition;
    }

    private void InitializeGame()
    {
        // Intialize game logic variables
        gameState = GameStates.WAITING;
        // Clear even flags.
        isGameStarted = false;
        isGameFinished = false;
        isGamePaused = false;
        isBallHitted = false;
        isBallMissed = false;

        reminderPanel = GameObject.FindGameObjectWithTag("ReminderPanel");

        // Set current AROM and PROM.
        arom = AppData.Instance.selectedMechanism.CurrentArom;
        prom = AppData.Instance.selectedMechanism.CurrentProm;
        aprom = AppData.Instance.selectedMechanism.CurrentAProm;
        gameSpeed = AppData.Instance.speedData.gameSpeed;
        // gameSpeed = 20.0f; //temp
        // Attach PLUTO button event.
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        // reminderPanel.SetActive(false);
        if (AppData.Instance.selectedMechanism.trialNumberDay >= AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
        {
            reminderPanel.SetActive(true);

        }
        else
        {
            reminderPanel.SetActive(false);
        }
        
    }
    private void onPlutoButtonReleased()
    {
        isButtonPressed = true;
        AppLogger.LogInfo($"{AppData.Instance.selectedGameName} -- pluto button pressed");

    }

    public float AngleToScreen(float angle) => Mathf.Clamp(-playSize + (angle - aprom[0]) * (2 * playSize) / (aprom[1] - aprom[0]), bottomBound, topBound);

    public void StartGame()
    {
        // Start new trial.
        AppData.Instance.StartNewTrial();

        gsc.sessionDetailsText.text = $"sessionNo: {AppData.Instance.currentSessionNumber}\n" +
            $"trialNo: {AppData.Instance.selectedMechanism.trialNumberSession}\n" +
            $"CB: {AppData.Instance.CurrentControlBound}";
              
        reminderPanel.SetActive(false);
        
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

        targetAngle = HomerTherapy.GetNewTargetPositionUniformFull(arom, aprom);
        targetPositiony = AngleToScreen(targetAngle);
        setTarget();
    }

    public void BallHitted()
    {
        isBallHitted = true;
        isBallMissed = false;
        nSuccess++;
    }

    public void BallMissed() {
        isBallHitted = false;
        isBallMissed = true;
        nFailure++;
    }

    public bool IsGamePlaying()
    {
        return gameState != GameStates.WAITING 
            && gameState != GameStates.PAUSED
            && gameState != GameStates.STOP;
    }
    private void UpdateText()
    {
        timeLeftText.text = $"TIME: {(int)triaTimeLeft}";
        // gameSpeedViewer.text = $"GS :{(int)gameSpeed}";
        //core.text = $"Score: {nSuccess}";
    }
}
