using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using static AppData;
using UnityEngine.UI;
using UnityEditor.SearchService;

public class UIManager1 : MonoBehaviour
{
    public BoundController rightBound;
    public BoundController leftBound;
    public Button playButton;
    public Button exitButton;
    public static bool isButtonPressed=false;
    void Start()
    {

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        isButtonPressed = false;
        playButton.onClick.AddListener(LoadNextScene);
        exitButton.onClick.AddListener(onExitButtonClicked);
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
            LoadNextScene();
            isButtonPressed = false;
        }
    }
    //loads inputted level
    public void onExitButtonClicked()
    {
        SceneManager.LoadScene("choosegame");
        AppLogger.LogInfo("Switching scene to choosegame.");
    }
    public void onPlutoButtonReleased()
    {
            isButtonPressed = true;
    }
    void LoadNextScene()
    {
        SceneManager.LoadScene("pong_game");
        AppLogger.LogInfo("Switching scene to pong_game."); 

    }
    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= onPlutoButtonReleased;
        }
    }

}
