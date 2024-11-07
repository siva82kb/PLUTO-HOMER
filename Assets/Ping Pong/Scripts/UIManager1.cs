using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using static AppData;
using UnityEditor.SearchService;

public class UIManager1 : MonoBehaviour
{
    public BoundController rightBound;
    public BoundController leftBound;
    public static bool isButtonPressed=false;
    void Start()
    {
        isButtonPressed = false;
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
    public void LoadLevel(string level)
    {

        SceneManager.LoadSceneAsync(level);
    }
    public void onPlutoButtonReleased()
    {
        
            isButtonPressed = true;
      
    }
    void LoadNextScene()
    {
        SceneManager.LoadScene("pong_game");

    }
    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= onPlutoButtonReleased;
        }
    }

}
