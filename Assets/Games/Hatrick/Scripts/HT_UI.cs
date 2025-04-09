


using UnityEngine;
using UnityEngine.SceneManagement;

public class HT_UI : MonoBehaviour
{
    private GameObject[] pauseObjects, finishObjects;
    public AudioClip[] audioClips; // win, level complete, loose
    public AudioSource gameSound;
    public int winScore = 7;
    private bool isPaused;

    void Start()
    {
        isPaused = false;
        pauseObjects = GameObject.FindGameObjectsWithTag("ShowOnPause");
        finishObjects = GameObject.FindGameObjectsWithTag("ShowOnFinish");
        HidePaused();
        HideFinished();
    }

    void Update()
    {
        HandleGameState();
        HandleInput();
    }

    private void HandleGameState()
    {
        if (HatGameController.instance != null)
        {
            if (HatGameController.instance.IsPlaying && !isPaused)
            {
                gameData.isGameLogging = true;
                HideFinished();
                HidePaused();
            }
            else if (!HatGameController.instance.IsPlaying)
            {
                gameData.isGameLogging = false;
                ShowFinished();
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (HatGameController.instance != null && HatGameController.instance.IsPlaying)
            {
                Debug.Log("Game paused via P key.");
                gameData.isGameLogging = false;
                TogglePause();
            }
            else if (HatGameController.instance != null && !HatGameController.instance.IsPlaying)
            {
                HatGameController.instance.RestartGame();
                Debug.Log("Game restarted via P key.");
            }
        }
    }

    public void Reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void PlayAudio(int clipIndex)
    {
        if (clipIndex >= 0 && clipIndex < audioClips.Length && gameSound != null)
        {
            gameSound.clip = audioClips[clipIndex];
            gameSound.Play();
        }
        else
        {
            Debug.LogWarning("Invalid audio clip index or missing AudioSource component.");
        }
    }

    public void TogglePause()
    {
        if (Time.timeScale == 1)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    private void PauseGame()
    {
        Time.timeScale = 0;
        ShowPaused();
        isPaused = true;

        if (HatGameController.instance != null)
        {
            HatGameController.instance.PauseGame();
        }
    }

    private void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1;
        HidePaused();

        if (HatGameController.instance != null)
        {
            HatGameController.instance.ResumeGame();
        }
    }

    public void ShowPaused()
    {
        foreach (GameObject g in pauseObjects)
        {
            g.SetActive(true);
        }
    }

    public void HidePaused()
    {
        foreach (GameObject g in pauseObjects)
        {
            g.SetActive(false);
        }
    }

    public void ShowFinished()
    {
        foreach (GameObject g in finishObjects)
        {
            g.SetActive(true);
        }
    }

    public void HideFinished()
    {
        foreach (GameObject g in finishObjects)
        {
            g.SetActive(false);
        }
    }
}
