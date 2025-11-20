using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RNRMenuManager : MonoBehaviour
{
    public static bool isButtonPressed = false;
    private string choosegameScene = "CHGAME";
    private string gameScene = "RNR";
    // Start is called before the first frame update
    void Start()
    {
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        isButtonPressed = false;

        StartCoroutine(AutoPlayAfterDelay());
    }

    // Update is called once per frame
    void Update()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        }
        if (isButtonPressed)
        {
            Play();
            isButtonPressed = false;
        }
    }
    public void Play()
    {
        SceneManager.LoadScene(gameScene);
        AppLogger.LogInfo("Switching scene to pong_game.");
    }
    public void Exit()
    {
        SceneManager.LoadScene(choosegameScene);
        AppLogger.LogInfo("Switching scene to choosegame.");
    }
    public void onPlutoButtonReleased()
    {
        isButtonPressed = true;
    }
    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= onPlutoButtonReleased;
        }
    }
    private IEnumerator AutoPlayAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(gameScene);
        AppLogger.LogInfo("Switching scene to RNR_game.");
    }
}
