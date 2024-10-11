//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;  // Required for scene management
//using UnityEngine.UI;  // Required for UI elements like Toggle

//public class ChooseGameSceneHandler : MonoBehaviour
//{
//    public GameObject toggleGroup;  // Reference to the GameObject containing the toggles
//    public Button playButton;   // Reference to the Play Button

//    private string selectedGame;  // Stores the currently selected game

//    // Start is called before the first frame update
//    void Start()
//    {
//        // Add listener for Play Button click event
//        playButton.onClick.AddListener(OnPlayButtonClicked);
//    }

//    void Update()
//    {
//        // Check if a new game option is selected
//        selectedGame = GetSelectedToggleName();
//    }

//    private string GetSelectedToggleName()
//    {
//        // Get all toggles under the toggleGroup GameObject
//        Toggle[] toggles = toggleGroup.GetComponentsInChildren<Toggle>();

//        foreach (Toggle toggle in toggles)
//        {
//            Debug.Log("Processing Toggle: " + toggle.name + ", isOn: " + toggle.isOn);
//            if (toggle.isOn)
//            {
//                AppData.game = toggle.name;
//                Debug.Log("Selected Toggle: " + toggle.name);
//                return toggle.name;
//            }
//        }
//        return null;
//    }

//    private void OnPlutoButtonClicked()
//    {
//        if (!string.IsNullOrEmpty(selectedGame))
//        {
//            Debug.Log("Pluto Button Pressed. Selected Game: " + selectedGame);
//            // Add logic here if Pluto button has a special function related to the selected game
//        }
//        else
//        {
//            Debug.Log("No game selected. Please select a game.");
//        }
//    }

//    // Event handler for Play Button click
//    private void OnPlayButtonClicked()
//    {
//        if (!string.IsNullOrEmpty(selectedGame))
//        {
//            Debug.Log("Play Button Pressed. Selected Game: " + selectedGame);

//            switch (selectedGame)
//            {
//                case "pingPong":
//                    SceneManager.LoadScene("pong_menu");
//                    break;

//                case "Game2":
//                    SceneManager.LoadScene("Game2Scene");
//                    break;

//                case "Game3":
//                    SceneManager.LoadScene("Game3Scene");
//                    break;

//                default:
//                    Debug.LogWarning("Unknown game selected: " + selectedGame);
//                    break;
//            }
//        }
//        else
//        {
//            Debug.Log("No game selected. Please select a game.");
//        }
//    }
//}









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

    void Start()
    {
        // Attach listeners to the toggles and Play button
        AttachToggleListeners();
        playButton.onClick.AddListener(OnPlayButtonClicked);
        exit.onClick.AddListener(OnExitButtonClicked);
        DeselectAllToggles();  // Reset all toggles at the start
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
