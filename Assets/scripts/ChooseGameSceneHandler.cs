using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEditor.U2D.Aseprite;

public class ChooseGameSceneHandler : MonoBehaviour
{
    public GameObject toggleGroup;
    public Button playButton;
    public Button changeMech;
    public TMP_Text result;

    private bool toggleSelected = false;
    private string selectedGame;
    private string changeScene = "chooseMechanism";
    //private static bool isButtonPressed = false;
    private readonly Dictionary<string, string> gameScenes = new Dictionary<string, string>
    {
        { "pingPong", "pong_menu" },
        { "tukTuk", "FlappyGame" },
        { "hatTrick", "HatrickGame" }
    };
    private bool lisRunning = false;
    private bool targetReached = false;
    private const float targetTolerance = 5.0f;
    private bool isRunning = false;

    private float targetAngle = 0;
    private bool loadgame = false;


    void Start()
    {
        // Initialize if needed
        if (AppData.userData == null)
        {
            // Inialize the logger
            AppLogger.StartLogging(SceneManager.GetActiveScene().name);
            AppData.initializeStuff();
        }

        // If no mechanism is selected, got to the scene to choose mechanism.
        if (AppData.selectedMechanism == null)
        {
            SceneManager.LoadScene("chooseMechanism");
            return;
        }

        // Update App Logger
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        AppLogger.SetCurrentGame("");

        // Attach callback.
        AttachCallbacks();

        // Make sure No control is set
        PlutoComm.setControlType("NONE");
    }

    void Update()
    {
        PlutoComm.sendHeartbeat();
        if (loadgame)
        {
            toggleSelected = false;
            LoadSelectedGameScene(selectedGame);
            loadgame = false;
        }

        // Magic key cobmination for doing the assessment.
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            //assessment();
            SceneManager.LoadScene("Assessment");
        }
    }

    void AttachCallbacks()
    {
        // Scene controls callback
        AttachToggleListeners();
        playButton.onClick.AddListener(OnPlayButtonClicked);
        changeMech.onClick.AddListener(OnMechButtonClicked);
        // PLUTO Button
        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
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
        if (toggleSelected && !loadgame)
        {
            loadgame = true;
        }
    }

    private void OnMechButtonClicked()
    {
        SceneManager.LoadScene(changeScene);
    }

    private void LoadSelectedGameScene(string game)
    {
        if (gameScenes.TryGetValue(game, out string sceneName))
        {
            AppLogger.LogInfo($"{game} selected.");
            // Instantitate the game object and load the appropriate scene.
            switch(game)
            {
                case "pingPong":
                    AppData.selectedGame = "pingPong";
                    break;
                case "tukTuk":
                    AppData.selectedGame = "tukTuk";
                    break;
                case "hatTrick":
                    AppData.selectedGame = "hatTrick";
                    AppData.hatTrickGame = new HatTrickGame(AppData.selectedMechanism.name);
                    break;
            }
            SceneManager.LoadScene(sceneName);
        }
    }
    
    public void OnPlutoButtonReleased()
    {
        if (toggleSelected & !loadgame)
        {
            toggleSelected = false;
            loadgame = true;
        }
    }

    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= OnPlutoButtonReleased;
        }
    }

}