
using UnityEngine;
using UnityEngine.SceneManagement;  // Required for scene management
using UnityEngine.UI;  // Required for UI elements like Toggle
using System.Collections.Generic; // Required for dictionary handling
using System.Collections; // Required for coroutines

public class ChooseGameSceneHandler : MonoBehaviour
{
    public GameObject toggleGroup;  // Reference to the ToggleGroup in the scene
    public Button playButton;   // Reference to the Play Button
    public Button exit;
    private bool toggleSelected = false;  // Variable to track if any toggle is selected
    private string selectedGame;  // Stores the currently selected game
    private bool scene = false;
    void Start()
    {
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        // Attach listeners to the toggles and Play button
        AttachToggleListeners();
        playButton.onClick.AddListener(OnPlayButtonClicked);
        exit.onClick.AddListener(OnExitButtonClicked);
        DeselectAllToggles();  // Reset all toggles at the start
        
    }

    

    private void Update()
    {
        if (scene)
        {
            LoadSelectedGameScene(selectedGame);
            scene = false;
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
                toggleComponent.isOn = false;  // Reset all toggles to off
            }
        }
    }

    // Attach listeners to all toggles in the toggle group
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

    // Check the states of toggles to see if any is selected
    void CheckToggleStates()
    {
        toggleSelected = false;  // Reset toggleSelected

        foreach (Transform child in toggleGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null && toggleComponent.isOn)
            {
                selectedGame = toggleComponent.name;  // Store the selected toggle name
                toggleSelected = true;  // Mark that at least one toggle is selected
                Debug.Log("Selected Toggle: " + selectedGame);
                break; // Exit loop as we found a selected toggle
            }
        }
    }
   
    // Event handler for Play Button click
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

    private void OnExitButtonClicked()
    {
        //StartCoroutine(LoadSummaryScene());
        SceneManager.LoadScene("chooseMech");
        PlutoComm.calibrate(PlutoComm.MECHANISMS[4]);
    }


    public void onPlutoButtonReleased()
    {
            scene = true;
    }



    // Load the corresponding scene based on selected game
    private void LoadSelectedGameScene(string game)
    {
        switch (game)
        {
            case "pingPong":
                SceneManager.LoadScene("pong_menu"); // Update with the correct scene name
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
}
