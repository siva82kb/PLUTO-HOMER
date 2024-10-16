using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;

public class UIManagerPP : MonoBehaviour
{
    //public RockVR.Video.VideoCapture vdc;
    GameObject[] pauseObjects, finishObjects;
    public BoundController rightBound;
    public BoundController leftBound;
    public bool isFinished;
    public bool isPressed = false;
    public bool playerWon, enemyWon;
    public AudioClip[] audioClips; // winlevel loose
    public int winningScore = 7;
    public int win;
    // Use this for initialization
    void Start()
    {
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        hideFinished();
    }

    // Update is called once per frame
    void Update()
    {

        Debug.Log("SCORE" + winningScore);
        if (rightBound.enemyScore >= winningScore && !isFinished)
        {

            Debug.Log("WINSCORE" + winningScore);
            Debug.Log("rXWINSCORE" + rightBound.enemyScore);
            isFinished = true;
            enemyWon = true;
            Camera.main.GetComponent<AudioSource>().Stop();
            win = -1;
            isFinished = true;  
            playAudio(1);
            playerWon = false;         
            AppData.reps = 0;
        }
        else if (leftBound.playerScore >= winningScore && !isFinished)
        {
            Debug.Log("WINSCORE" + winningScore);
            Debug.Log("XWINSCORE" + leftBound.playerScore);

            Camera.main.GetComponent<AudioSource>().Stop();
       
            playAudio(0);
            isFinished = true;
            enemyWon = false;
            win = 1;
            playerWon = true;
            AppData.reps = 0;
        }

        if (isFinished)
        {
            showFinished();
        }


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

        if (isPressed && !isFinished)
        {
            Debug.Log("P key pressed. Current Time.timeScale: " + Time.timeScale);
            if (Time.timeScale == 1)
            {
                Time.timeScale = 0; // Pause the game
                Debug.Log("Game Paused");
                isPressed = false;
                showPaused();
                AppData.isGameLogging = false;

            }
            else if (Time.timeScale == 0)
            {
                Time.timeScale = 1; // Unpause the game
                Debug.Log("Game Unpaused");
                isPressed = false;
                hidePaused();
                AppData.isGameLogging = true;

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
                AppData.isGameLogging = false;


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
                AppData.isGameLogging = true;


            }
        }
    }
    private void onPlutoButtonReleased()
    {
        isPressed = true;
    }
    //Reloads the Level
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    //Reloads the Level
    public void Reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
            AppData.isGameLogging = false;

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

    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= onPlutoButtonReleased;
        }
    }



}
