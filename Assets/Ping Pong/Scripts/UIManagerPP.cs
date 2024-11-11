using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using UnityEditor.SceneManagement;
using NeuroRehabLibrary;
using UnityEngine.Analytics;


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
    private GameSession currentGameSession;

    void Start()
    {
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        hideFinished();
        if (!AppData.runIndividualGame) {
            StartNewGameSession();
        }
    }
    void Update()
    {
        CheckGameEndConditions();
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
                resumeGame();
            }
            isPressed = false; 
        }
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
        showFinished();
        if (!AppData.runIndividualGame)
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
        if (!AppData.runIndividualGame)
        {
            EndCurrentGameSession();
        }
        SceneManager.LoadScene(sceneName);
        AppLogger.LogInfo($"switching scene to '{sceneName}'");
    }

    public void Reload()
    {
        if (!AppData.runIndividualGame)
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
        if (AppData.runIndividualGame)
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
        string mech = AppData.selectedMechanism;
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
            string trialdata = AppData.trialDataFileLocation;
            string movetime = gameData.moveTime.ToString("F0");
            SessionManager.Instance.SetTrialDataFileLocation(trialdata, currentGameSession);
            SessionManager.Instance.moveTime(movetime, currentGameSession);
            SessionManager.Instance.EndGameSession(currentGameSession);
        }
    }
}
