using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using UnityEditor.SceneManagement;
using PlutoNeuroRehabLibrary;
using UnityEngine.Analytics;
using UnityEngine.UI;


public class UIManagerPP : MonoBehaviour
{
    GameObject[] pauseObjects, finishObjects;
    public BoundController rightBound;
    public BoundController leftBound;

    public bool isFinished;
    public bool isPressed=false;
    public bool playerWon, enemyWon;
    public AudioClip[] audioClips; 
    public int win;
    private bool isPaused = true;
    private float gameMoveTime = 0f;
    private float lastTimestamp = 0f;       
    public Toggle aromRange;
    private bool imageOff = true;
    private bool imageOffx = true;
    public Image targetImage;
    private int randomTargetIndex;
    private int ps;
    private int es;
    private int spawnCounter = 0;
    private System.Random random = new System.Random();
    private GameSession currentGameSession;

    public GameObject aromLeft, aromRight;

    static float topBound = 5.5F;
    static float bottomBound = -5.5F;
    public static float playSize;

    void Start()
    {
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        lastTimestamp = Time.unscaledTime;
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        hideFinished();
        if (!AppData.Instance.runIndividualGame) {
            StartNewGameSession();
        }
        if (aromRange!= null)
        {
            aromRange.onValueChanged.AddListener(OnToggleSpawnArea);
        }
        randomTargetIndex = random.Next(1, gameData.winningScore);

        playSize = Camera.main.orthographicSize;
        topBound = playSize - this.transform.localScale.y / 4;
        bottomBound = -topBound;

    }
    void Update()
    {
        
        if (Time.timeScale > 0 && !isFinished)
        {
            float currentTime = Time.unscaledTime;
            gameMoveTime += currentTime - lastTimestamp;
            lastTimestamp = currentTime;
        }
        else 
        {
            lastTimestamp = Time.unscaledTime; // Update timestamp even if paused or finished
        }
        CheckGameEndConditions();
        if (isFinished)
        {
            showFinished();
            gameData.isGameLogging = false;
        }
        else
        {
            if ((Time.timeScale == 0) && !isPaused && !isFinished && !(playerWon || enemyWon))
            {
                Time.timeScale = 1;
            }
        }

        if ((Input.GetKeyDown(KeyCode.P) && !isFinished) || (isPressed && !isFinished))
        {
            
            if (!isPaused)
            { 
                pauseGame();
            }
            else
            {
                PlutoComm.setControlType("POSITIONAAN");
                resumeGame();
            }
            isPressed = false;  
        }
        if (gameData.playerScore > 0 && gameData.playerScore < gameData.winningScore + 1)
        {
           // Debug.Log((float)gameData.playerScore / 10+ " scrorrr");
            gameData.successRate = (float)gameData.playerScore / gameData.winningScore;
          //  Debug.Log((float)gameData.successRate+" scrorrr");
        }
        bool isPlayerAtTarget = (gameData.playerScore == randomTargetIndex);
        bool isEnemyAtTarget = (gameData.enemyScore == randomTargetIndex);

        if ((isPlayerAtTarget || isEnemyAtTarget) && imageOffx)
        { 
            ps=gameData.playerScore;
            es=gameData.enemyScore;
            imageOffx = false;        
        }

            if ((isPlayerAtTarget || isEnemyAtTarget) && imageOff)
        {
            if ((ps !=gameData.playerScore)||(es!=gameData.enemyScore))
            { 
                imageOff = false;
            }
            
            targetImage.gameObject.SetActive(true);
        }
        else
        {
            targetImage.gameObject.SetActive(false);
        }
        aromLeft.transform.position = new Vector2(aromLeft.transform.position.x, playerMovementAreaAROM(AppData.Instance.selectedMechanism.currRom.aromMin));
        aromRight.transform.position = new Vector2(aromLeft.transform.position.x, playerMovementAreaAROM(AppData.Instance.selectedMechanism.currRom.aromMax));
        //Debug.Log($"ypos--{playerMovementAreaAROM(PlutoComm.angle)}+ angle-{PlutoComm.angle},{playerMovementAreaAROM(AppData.aRomValue[1])}");
        //if (PlutoComm.angle < AppData.pRomValue[1] && PlutoComm.angle > AppData.pRomValue[0]) Debug.Log($"position {PlutoComm.angle},{playerMovementAreaAROM(PlutoComm.angle)}");
    }

    private void OnToggleSpawnArea(bool isEnabled)
    {
        gameData.isAROMEnabled = isEnabled;
        PlutoComm.setControlType("NONE");
    }

    private void CheckGameEndConditions()
    {
        if (rightBound.enemyScore >= gameData.winningScore && !isFinished)
        {
            isFinished = true;
            AppLogger.LogInfo("PingPong game Finished, Enemy won");
            enemyWon = true;
            playerWon = false;
            gameEnd();
        }
        else if (leftBound.playerScore >= gameData.winningScore && !isFinished)
        {
            isFinished = true;
            AppLogger.LogInfo("PingPong game Finished, Player won");
            enemyWon = false;
            playerWon = true;
            gameEnd();
        }
    }
    private void gameEnd()
    {
        Camera.main.GetComponent<AudioSource>().Stop();
        playAudio(enemyWon ? 1 : 0);
        gameData.reps = 0;
        gameData.moveTime = gameMoveTime;
        showFinished();
        if (!AppData.Instance.runIndividualGame)
        {
            EndCurrentGameSession();
        }
    }
 private void pauseGame()
    {
        Time.timeScale = 0;
        isPaused = true;
        showPaused();
        AppLogger.LogInfo("PingPong game Paused");
        gameData.isGameLogging = false;
    }

    public static float playerMovementAreaAROM(float angle)
    {
        //ROM aromAng = new ROM(AppData.selectedMechanism);
        float tmin = AppData.Instance.selectedMechanism.currRom.promMin;
        float tmax = AppData.Instance.selectedMechanism.currRom.promMax;
        return Mathf.Clamp(-playSize + (angle - tmin) * (2 * playSize) / (tmax - tmin), bottomBound, topBound);
    }

    private void resumeGame()
    {
        Time.timeScale = 1;
        isPaused = false;
        hidePaused();
        gameData.isGameLogging = true;
    }


    private void onPlutoButtonReleased()
    {
        isPressed = true;
    }
    public void LoadScene(string sceneName)
    {
        if (!AppData.Instance.runIndividualGame)
        {
            EndCurrentGameSession();
        }
        SceneManager.LoadScene(sceneName);
        AppLogger.LogInfo($"switching scene to '{sceneName}'");
    }

    public void Reload()
    {
        gameMoveTime = 0f;
        lastTimestamp = Time.unscaledTime;
        if (!AppData.Instance.runIndividualGame)
        {
            EndCurrentGameSession();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo("Game restarted again");
    }
    void playAudio(int clipNumber)
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = audioClips[clipNumber];
        audio.Play();
    } 
    public void showPaused()
    {
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
    }
    public void showFinished()
    {
        foreach (GameObject g in finishObjects)
        {
            g.SetActive(true);
        }
    }
    public void hideFinished()
    {
        foreach (GameObject g in finishObjects)
        {
            g.SetActive(false);
        }
    }
    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= onPlutoButtonReleased;
        }
        if (AppData.Instance.runIndividualGame)
        {
            EndCurrentGameSession();
        }
    }
    void StartNewGameSession()
    {
        currentGameSession = new GameSession
        {
            GameName = "PING-PONG",
            Assessment = 0 
        };

        SessionManager.Instance.StartGameSession(currentGameSession);
        Debug.Log($"Started new game session with session number: {currentGameSession.SessionNumber}");

        SetSessionDetails();
    }
    private void SetSessionDetails()
    {
        string device = "PLUTO"; 
        string assistMode = "Null"; 
        string assistModeParameters = "Null"; 
        string deviceSetupLocation = "CMC-Bioeng-dpt"; 
        string gameParameter = "YourGameParameter"; 
        string mech = AppData.Instance.selectedMechanism.name;
        SessionManager.Instance.SetDevice(device, currentGameSession);
        SessionManager.Instance.SetAssistMode(assistMode, assistModeParameters, currentGameSession);
        SessionManager.Instance.SetDeviceSetupLocation(deviceSetupLocation, currentGameSession);
        SessionManager.Instance.SetGameParameter(gameParameter, currentGameSession);
        SessionManager.Instance.mechanism(mech, currentGameSession);
    }
    void EndCurrentGameSession()
    {
        if (currentGameSession != null)
        {
            string trialdata = AppData.Instance.trialDataFileLocation;
            string movetime = gameData.moveTime.ToString("F0");
            SessionManager.Instance.gameSpeed(gameData.gameSpeedPP, currentGameSession);
            SessionManager.Instance.successRate(gameData.successRate, currentGameSession);
            Debug.Log("speed and sr :"+ gameData.gameSpeedPP+"+"+ gameData.successRate);
            SessionManager.Instance.SetTrialDataFileLocation(trialdata, currentGameSession);
            SessionManager.Instance.moveTime(movetime, currentGameSession);
            SessionManager.Instance.EndGameSession(currentGameSession);
        }
        gameData.isAROMEnabled = false; 
    }
}
