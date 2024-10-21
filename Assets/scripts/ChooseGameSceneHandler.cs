using UnityEngine;
using UnityEngine.SceneManagement;  
using UnityEngine.UI;  
using System.Collections.Generic; 
using System.Collections; 

public class ChooseGameSceneHandler : MonoBehaviour
{
    public GameObject toggleGroup;  
    public Button playButton;   
    public Button changeMech;  

    private bool toggleSelected = false;  
    private string selectedGame; 
    private static bool isButtonPressed = false;


    void Start()
    {
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;

        isButtonPressed = false;
        AttachToggleListeners();
        playButton.onClick.AddListener(OnPlayButtonClicked);
        changeMech.onClick.AddListener(OnPlayButtonClickedx);
        DeselectAllToggles(); 
    }
    void Update()
    {
        AppData.selectedGame = selectedGame;  
        if (isButtonPressed)
        {
            LoadSelectedGameScene(selectedGame);
        }
    }

    // Resets all toggles to off
    void DeselectAllToggles()
    {
        foreach (Transform child in toggleGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null)
            {
                toggleComponent.isOn = false; 
            }
        }
    }

    
    void AttachToggleListeners()
    {
        foreach (Transform child in toggleGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null)
            {
                toggleComponent.onValueChanged.AddListener(delegate { CheckToggleStates(); });
            }
        }
    }

    
    void CheckToggleStates()
    {
        toggleSelected = false;  

        foreach (Transform child in toggleGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null && toggleComponent.isOn)
            {
                selectedGame = toggleComponent.name;  
                toggleSelected = true; 
                break; 
            }
        }
    }

    private void OnPlayButtonClicked()
    {
        if (toggleSelected)
        {
            Debug.Log("Play Button Pressed. Selected Game: " + selectedGame);
            LoadSelectedGameScene(selectedGame);
        }
        else
        {
            Debug.Log("No game selected. Please select a game.");
        }
    }

    private void OnPlayButtonClickedx()
    {
        SceneManager.LoadScene("chooseMech");
     
    }

    private void LoadSelectedGameScene(string game)
    {
        Debug.Log("ASG : " + AppData.selectedGame);
        switch (game)
        {
            case "pingPong":
                Debug.Log("Selected game:" + game);
                // SceneManager.LoadScene("pong_menu");
                break;

            case "Game2":
                SceneManager.LoadScene("Game2Scene");
                break;

            case "Game3":
                SceneManager.LoadScene("Game3Scene");
                break;

            default:
                Debug.LogWarning("Unknown game selected: " + game);
                break;
        }
    }

    public void onPlutoButtonReleased()
    {
        if (toggleSelected)
        {
            isButtonPressed = true;
        }
    }


}
