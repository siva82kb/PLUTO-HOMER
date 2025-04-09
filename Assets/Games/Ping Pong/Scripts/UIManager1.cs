using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager1 : MonoBehaviour
{
    public BoundController rightBound;
    public BoundController leftBound;
    public Button playButton;
    public Button exitButton;
    public static bool isButtonPressed = false;
    private string choosegameScene = "choosegame";
    private string gameScene = "pong_game";
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
        SceneManager.LoadScene(choosegameScene);
        AppLogger.LogInfo("Switching scene to choosegame.");
    }
    public void onPlutoButtonReleased()
    {
            isButtonPressed = true;
    }
    void LoadNextScene()
    {
        SceneManager.LoadScene(gameScene);
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
