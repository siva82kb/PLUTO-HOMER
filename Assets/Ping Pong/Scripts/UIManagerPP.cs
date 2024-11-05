using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using UnityEditor.SceneManagement;

public class UIManagerPP : MonoBehaviour
{
    GameObject[] pauseObjects, finishObjects,hideGameObjects;
    public BoundController rightBound;
    public BoundController leftBound;
    public bool isFinished;
    public bool isPressed=false;
    public bool playerWon, enemyWon;
    public AudioClip[] audioClips; 
    public int win;
    private bool isPaused = true;

    void Start()
    {
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        hideGameObjects = new GameObject[] { GameObject.FindGameObjectWithTag("Target"), GameObject.FindGameObjectWithTag("Player"), 
                                                GameObject.FindGameObjectWithTag("Enemy"), GameObject.FindGameObjectWithTag("hideOnFinish") };
        hideFinished();

    }
    void Update()
    {
        CheckGameEndConditions();
        if (isFinished)
        {
            showFinished();
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

    //private IEnumerator WaitAndStopGameLogging()
    //{
    //    yield return new WaitForSeconds(2.0f);
    //    AppData.isGameLogging = false;
    //}
    private void CheckGameEndConditions()
    {
        if (rightBound.enemyScore >= gameData.winningScore && !isFinished)
        {
            isFinished = true;
            enemyWon = true;
            playerWon = false;
            gameEnd();
        }
        else if (leftBound.playerScore >= gameData.winningScore && !isFinished)
        {
            isFinished = true;
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
    }
 private void pauseGame()
    {
        Time.timeScale = 0;
        isPaused = true;
        showPaused();
        gameData.isGameLogging = false;
        Debug.Log("Game Paused");
    }

    private void resumeGame()
    {
        Time.timeScale = 1;
        isPaused = false;
        hidePaused();
        gameData.isGameLogging = true;
        Debug.Log("Game Unpaused");
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
        Debug.Log("Player movement time to this point: " + gameData.moveTime.ToString("F2") + " seconds");

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
    }
}
