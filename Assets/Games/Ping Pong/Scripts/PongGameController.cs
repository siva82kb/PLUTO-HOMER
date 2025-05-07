using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class PongGameController : MonoBehaviour
{
    public static PongGameController Instance {  get; private set; }
    GameObject[] pauseObjects, finishObjects;
    public BoundController rightBound;
    public BoundController leftBound;
    public GameObject ball;
    public Text pointCounter, gameOverText;
    public bool isFinished;
    private bool isButtonPressed = false;
    public bool playerWon, enemyWon;
    public AudioClip[] audioClips; 
    public int enemyScore, playerScore;
    public Vector2 targetPosition;
    public float targetPositiony;

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
    private static string prevScene = "PONGMENU";
    private float[] arom;
    private float[] prom;
    private float targetAngle;
    
    private float playerPosition;
    private  GameObject targetTemp;
    public  GameObject SuccessRateBanner,ExitButton;
    public Text prevSR, currSR;

    public Text timeLeftText;
    static float playSize;
    static float topBound = 5.5F;
    static float bottomBound = -5.5F;
    public GameObject aromLeft;
    public GameObject aromRight;
    private float triaTimeLeft;
    private float moveTimeLeft;

    // Game score related variables.
    public int nTargets = 0;
    public int nSuccess = 0;
    public int nFailure = 0;
    private  float MOVEDURATION;

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
    }
    void Start()
    {
        InitializeGame();
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        targetPosition = new Vector2(5.95f, 0f);
        hideFinished();
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


    }
    void Update()
    {
        pointCounter.text = enemyScore + "\t\t" +
            playerScore;

         //if (isGamePaused && gameState != GameStates.PAUSED) 

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

        if ((Input.GetKeyDown(KeyCode.P) && !isFinished) ||(isButtonPressed && !isFinished))
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
        if((isFinished && Input.GetKeyDown(KeyCode.P)) || (isFinished && isButtonPressed) ){

            if (AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.AromMoving
                    || AppData.Instance.aanController.state == PlutoAANController.PlutoAANState.Idle) 
                {
             Reload();
                }
             isButtonPressed = false;
        }

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

  

    private void gameEnd()
    {
        Camera.main.GetComponent<AudioSource>().Stop();
        playAudio(enemyScore>playerScore ? 1 : 0);
        //showFinished();
        Time.timeScale = 0;
    }

 private void pauseGame()
    {
        _prevGameState = gameState;
        gameState = GameStates.PAUSED;
        Time.timeScale = 0;
        isGamePaused = true;
        isPaused = true;
        showPaused();
        ExitButton.SetActive(false);
    }

    private void resumeGame()
    {
        gameState = _prevGameState;
        Time.timeScale = 1;
        isGamePaused = false;
        isPaused = false;
        hidePaused();
        ExitButton.SetActive(true);
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
            AppData.Instance.aanController.Update(PlutoComm.angle, Time.deltaTime, true);
             AppData.Instance.StopTrial(nTargets, nSuccess, nFailure);
             gameState = GameStates.DONE;
             Time.timeScale = 1f;
             SceneManager.LoadScene(prevScene);
        }
    }

    public void Reload()
    {
        playerScore = enemyScore = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        foreach (GameObject g in finishObjects)
        {
            g.SetActive(true);
        }
        if(AppData.Instance.previousSuccessRates!=null)
        {
            SuccessRateBanner.SetActive(true);
            prevSR.text = $" previous SR : {AppData.Instance.previousSuccessRates[0]}%";
            currSR.text = $"Current Success Rate : {AppData.Instance.previousSuccessRates[1]}%";
        }
         gameOverText.text = (playerScore >= enemyScore) ? "GAME OVER!\nPLAYER WON!" : "GAME OVER!\nENEMY WON!";
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
        Debug.Log("Game Update");
        if (isGamePaused) pauseGame();
        else if (gameState == GameStates.PAUSED) resumeGame();

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
                AppData.Instance.aanController.SetNewTrialDetails(PlutoComm.angle, targetAngle, MOVEDURATION);
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
                // Wait for the user to score.
                gameState = isTimeUp ? GameStates.STOP : GameStates.SPAWNBALL;

                isBallHitted = false;
                isBallMissed = false;
                targetAngle = HomerTherapy.GetNewTargetPositionUniformFull(arom, prom);
                targetPositiony = AngleToScreen(targetAngle);
                setTarget();
                break;
            case GameStates.PAUSED:
                Debug.Log(isGamePaused);
                break;
            case GameStates.STOP:
                // Trial complete.
                isFinished = true;
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
                    showFinished();
                    gameEnd();
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

        // Set current AROM and PROM.
        arom = AppData.Instance.selectedMechanism.CurrentArom;
        prom = AppData.Instance.selectedMechanism.CurrentProm;

        // Attach PLUTO button event.
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
    }
  private void onPlutoButtonReleased()
    {
        isButtonPressed = true;
    }

    public float AngleToScreen(float angle) => Mathf.Clamp(-playSize + (angle - prom[0]) * (2 * playSize) / (prom[1] - prom[0]), bottomBound, topBound);

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

        targetAngle = HomerTherapy.GetNewTargetPositionUniformFull(arom, prom);
        targetPositiony = AngleToScreen(targetAngle);
        setTarget();
    }

        public void BallHitted() {
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
        timeLeftText.text = $"Time Left: {(int)triaTimeLeft}";
        //core.text = $"Score: {nSuccess}";
    }
}
