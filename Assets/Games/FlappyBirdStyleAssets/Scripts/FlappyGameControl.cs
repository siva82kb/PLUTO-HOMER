
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Michsky.UI.ModernUIPack;
using PlutoNeuroRehabLibrary;

public class FlappyGameControl : MonoBehaviour
{
    public AudioClip[] winClip;
    public AudioClip[] hitClip;
    public Text ScoreText;
    public ProgressBar timerObject;
    public static FlappyGameControl instance;
    public GameObject GameOverText, aromLeft, aromRight;
    public bool gameOver = false;
    public float scrollSpeed = -3f;
    private int score;
    public GameObject[] pauseObjects;
    public float gameduration = 60;
    public GameObject start;
    int win = 0;
    bool endValSet = false;
    private GameSession currentGameSession;
    private float gameMoveTime = 0f;
    private float lastTimestamp = 0f; // Last recorded time for time scale changes
    private string chooseGameScene = "choosegame";
    public BirdControl bc;
    float playSize = 0f;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != null)
        {
            Destroy(gameObject);
        }

    }

    void Start()
    {
        playSize = Camera.main.orthographicSize * Camera.main.aspect;
        //AppData.initializeStuff();
        Time.timeScale = 1;
        lastTimestamp = Time.unscaledTime;
        timerObject.isOn = true;
        timerObject.enabled = true;
        gameData.reps = 0;
        gameData.isGameLogging = true;
        //PlutoComm.calibrate(AppData.selectedMechanism);
        if (!AppData.runIndividualGame)
        {
            StartNewGameSession();
        }
    }

    void Update()
    {
        if (Time.timeScale > 0 && !gameOver)
        {
            float currentTime = Time.unscaledTime;
            gameMoveTime += currentTime - lastTimestamp;
            lastTimestamp = currentTime;
        }
        else
        {
            lastTimestamp = Time.unscaledTime; // Update timestamp even if paused or finished
        }
        UpdateGameDurationUI();

        if ((Input.GetKeyDown(KeyCode.P)))
        {
            if (!gameOver)
            {
                if (Time.timeScale == 1)
                {
                    Time.timeScale = 0;
                    showPaused();
                }
                else if (Time.timeScale == 0)
                {
                    Time.timeScale = 1;
                    hidePaused();
                }
            }
            else if (gameOver)
            {
                hidePaused();
                playAgain();
            }
        }

        if (!gameOver && Time.timeScale == 1)
        {
            gameduration -= Time.deltaTime;
        }
        if (gameData.gameScore > 0 && gameData.gameScore < 11)
        {
            gameData.successRate = (float)gameData.gameScore / 10;
        }

    }


    void UpdateGameDurationUI()
    {
        timerObject.specifiedValue = Mathf.Clamp(100 * (90 - gameduration) / 90f, 0, 100);
        gameData.moveTime = gameMoveTime;


        aromLeft.transform.position = new Vector3(aromRight.transform.position.x,
           Angle2Screen(AppData.aRomValue[0]),
           aromLeft.transform.position.z
       );
        aromRight.transform.position = new Vector3(
          aromRight.transform.position.x,
              Angle2Screen2(AppData.aRomValue[1]),
            aromRight.transform.position.z
        );

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
    public void BirdDied()
    {

        endValSet = true;
        gameData.moveTime = gameMoveTime;
        if (win == -1)
            GameOverText.GetComponent<Text>().text = "Try Again";
        GameOverText.SetActive(true);
        gameOver = true;

    }
    public void BirdScored()
    {


        if (gameduration < 0 && !endValSet)
        {

            gameduration = 0;


            if (!gameOver)
            {
                win = 1;
            }
            else
            {
                win = -1;
            }
            gameOver = true;
            Debug.Log(win);

            ;
            score = 0;
            BirdDied();
        }
        else
        {
            if (!bc.startBlinking)
            {
                int index = UnityEngine.Random.Range(0, winClip.Length);
                GetComponent<AudioSource>().clip = winClip[index];
                if (score != 0)
                {
                    GetComponent<AudioSource>().Play();
                }
                score += 1;
                gameData.gameScore++;


            }
            else
            {
                int index = UnityEngine.Random.Range(0, hitClip.Length);
                GetComponent<AudioSource>().clip = hitClip[index];
                GetComponent<AudioSource>().Play();
            }

            ScoreText.text = "Score: " + gameData.gameScore.ToString();/* score.ToString();*/
            FlappyColumnPool.instance.spawnColumn();
        }
    }
    public void playAgain()
    {
        if (gameOver == true)
        {
            gameData.isGameLogging = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        }
        if (!gameOver)
        {
            if (Time.timeScale == 1)
            {
                Time.timeScale = 0;
                gameData.isGameLogging = false;
                showPaused();
            }
            else if (Time.timeScale == 0)
            {
                Time.timeScale = 1;
                gameData.isGameLogging = true;
                hidePaused();
            }

        }

    }
    public float Angle2Screen(float angle)
    {
        ROM promAng = new ROM(AppData.selectedMechanism.name);
        float tmin = promAng.promMin;
        float tmax = promAng.promMax;

        return (-3.0f + (angle - tmin) * (playSize) / (tmax - tmin));


    }
    public float Angle2Screen2(float angle)
    {
        ROM promAng = new ROM(AppData.selectedMechanism.name);
        float tmin = promAng.promMin;
        float tmax = promAng.promMax;

        return (-4.3f+(angle - tmin) * (playSize) / (tmax - tmin));


    }
    public void PlayStart()
    {
        endValSet = false;
        gameMoveTime = 0f;
        lastTimestamp = Time.unscaledTime;
        start.SetActive(false);
        Time.timeScale = 1;
        if (!AppData.runIndividualGame)
        {
            EndCurrentGameSession();
        }
    }

    public void continueButton()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1;
            hidePaused();

        }
    }
    public void exitButton()
    {
        if (!AppData.runIndividualGame) {
        EndCurrentGameSession();
        }
        SceneManager.LoadScene(chooseGameScene);
    }
    void StartNewGameSession()
    {
        currentGameSession = new GameSession
        {
            GameName = "TUK-TUK",
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
        string mech = AppData.selectedMechanism.name;
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
            SessionManager.Instance.gameSpeed(gameData.gameSpeedTT, currentGameSession);
            SessionManager.Instance.successRate(gameData.successRate, currentGameSession);
            SessionManager.Instance.SetTrialDataFileLocation(trialdata, currentGameSession);
            SessionManager.Instance.moveTime(movetime, currentGameSession);
            SessionManager.Instance.EndGameSession(currentGameSession);
        }
    }

}