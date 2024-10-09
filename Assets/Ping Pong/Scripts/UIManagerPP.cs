using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class UIManagerPP : MonoBehaviour
{
    //public RockVR.Video.VideoCapture vdc;
    GameObject[] pauseObjects, finishObjects;
    public BoundController rightBound;
    public BoundController leftBound;
    public bool isFinished;
    public bool playerWon, enemyWon;
    public AudioClip[] audioClips; // winlevel loose
    public int winScore = 7;
    public int win;
    // Use this for initialization
    void Start()
    {
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        hideFinished();
        //vdc.customPath = false;
        //vdc.customPathFolder = "";
        //vdc.filePath = AppData.GameVideoLog(AppData.subjHospNum, AppData.plutoData.mechs[AppData.plutoData.mechIndex], AppData.game, AppData.regime);
        ////   Debug.Log(vdc.filePath);
        //vdc.StartCapture();

    }

    // Update is called once per frame
    void Update()
    {


        if (rightBound.enemyScore >= winScore && !isFinished)
        {
            isFinished = true;
            enemyWon = true;
            Camera.main.GetComponent<AudioSource>().Stop();
            win = -1;
            isFinished = true;
            //Debug.Log(HatGameController.instance.score + "," + 0.8 * HT_spawnTargets1.instance.count);
            //AppData.plutoData.desTorq = 0;
            //AppData.DifficultyManager(-1);
            //AppData.writeGamePerformace();

            //AppData.StopGameLogging();
            //AppData.WriteTrainingSummaryFile(AppData.reps, AppData.timeOnTrail);
            playAudio(1);
            playerWon = false;
            //AppData.timeOnTrail = 0;
            AppData.reps = 0;
        }
        else if (leftBound.playerScore >= winScore && !isFinished
            )
        {
            Camera.main.GetComponent<AudioSource>().Stop();
            //AppData.DifficultyManager(1);
            //AppData.writeGamePerformace();

            //string data = AppData.startGamePerformace.ToString() + "," + AppData.startGameLevel.ToString() + "," + AppData.endGamePerformace.ToString() + "," + AppData.endGameLevel.ToString() + "," + AppData.gameLogFileName + "\n";
            // AppData.CreateGamePerformacelog(data);

            //AppData.StopGameLogging();
            //AppData.WriteTrainingSummaryFile(AppData.reps, AppData.timeOnTrail);

            //playAudio(1);
            playAudio(0);
            isFinished = true;
            enemyWon = false;
            win = 1;
            playerWon = true;
            //AppData.timeOnTrail = 0;
            AppData.reps = 0;
        }

        if (isFinished)
        {
            showFinished();
        }
        //if (isFinished && AppData.inputPressed())
        //{
        //    LoadScene("pong_menu");
        //}


        //uses the p button to pause and unpause the game
        //if ((AppData.inputPressed() || Input.GetKeyDown(KeyCode.P)) && !isFinished)
        //{
        //    if (Time.timeScale == 1)
        //    {
        //        Time.timeScale = 0;
        //        showPaused();
        //    }
        //    else if (Time.timeScale == 0)
        //    {
        //        Time.timeScale = 1;
        //        hidePaused();
        //    }
        //}

        if (Input.GetKeyDown(KeyCode.P) && !isFinished)
        {
            Debug.Log("P key pressed. Current Time.timeScale: " + Time.timeScale);
            if (Time.timeScale == 1)
            {
                Time.timeScale = 0; // Pause the game
                Debug.Log("Game Paused");
                showPaused();
            }
            else if (Time.timeScale == 0)
            {
                Time.timeScale = 1; // Unpause the game
                Debug.Log("Game Unpaused");
                hidePaused();
            }
        }



        if (Time.timeScale == 0 && !isFinished)
        {
            //searches through pauseObjects for PauseText
            foreach (GameObject g in pauseObjects)
            {

                if (g.name == "PauseText")
                    //makes PauseText to Active
                    g.SetActive(true);
            }
        }
        else
        {
            //searches through pauseObjects for PauseText
            foreach (GameObject g in pauseObjects)
            {
                if (g.name == "PauseText")
                    //makes PauseText to Inactive
                    g.SetActive(false);
            }
        }
    }
    //Reloads the Level
    public void LoadScene(string sceneName)
    {
        Application.LoadLevel(sceneName);
    }

    //Reloads the Level
    public void Reload()
    {
        Application.LoadLevel(Application.loadedLevel);
    }
    void playAudio(int clipNumber)
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = audioClips[clipNumber];
        audio.Play();

    }
    //controls the pausing of the scene
    public void pauseControl()
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

    //shows objects with ShowOnPause tag
    public void showPaused()
    {
        foreach (GameObject g in pauseObjects)
        {
            g.SetActive(true);
        }
    }

    //hides objects with ShowOnPause tag
    public void hidePaused()
    {
        foreach (GameObject g in pauseObjects)
        {
            g.SetActive(false);
        }
    }

    //shows objects with ShowOnFinish tag
    public void showFinished()
    {


        foreach (GameObject g in finishObjects)
        {
            g.SetActive(true);
        }
    }

    //hides objects with ShowOnFinish tag
    public void hideFinished()
    {
        foreach (GameObject g in finishObjects)
        {
            g.SetActive(false);
        }
    }



}
