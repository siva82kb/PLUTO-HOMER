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
    private string gameSelected;
    private string changeScene = "chooseMechanism";
    private readonly Dictionary<string, string> gameScenes = new Dictionary<string, string>
    {
        { "PINGPONG", "pong_menu" },
        { "TUKTUK", "FlappyGame" },
        { "HATTRICK", "HatrickGame" }
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
            AppData.initializeStuff(doNotResetMech: false);
        }

        // If no mechanism is selected, got to the scene to choose mechanism.
        if (AppData.selectedMechanism == null)
        {
            // Check if mechnism is set in PLUTO?
            if (PlutoComm.CALIBRATION[PlutoComm.calibration] == "YESCALIB")
            {
                AppData.selectedMechanism = new PlutoMechanism(name: PlutoComm.MECHANISMS[PlutoComm.mechanism], side: AppData.trainingSide);
                AppLogger.SetCurrentMechanism(AppData.selectedMechanism.name);
            } else
            {
                SceneManager.LoadScene("chooseMechanism");
                return;
            }
        }

        // Update App Logger
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        AppLogger.SetCurrentGame("NONE");
        
        // Reset selected game.
        AppData.selectedGame = null;

        // Attach callback.
        AttachCallbacks();

        // Make sure No control is set
        PlutoComm.setControlType("NONE");

        Debug.Log($"Curr ROM: {AppData.selectedMechanism.currRom.promMin:F2}, {AppData.selectedMechanism.currRom.promMax:F2}, {AppData.selectedMechanism.currRom.aromMin:F2}, {AppData.selectedMechanism.currRom.aromMax:F2}");
    }

    void Update()
    {
        PlutoComm.sendHeartbeat();
        if (loadgame)
        {
            toggleSelected = false;
            LoadSelectedGameScene(gameSelected);
            loadgame = false;
        }

        // Magic key cobmination for doing the assessment.
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
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
                gameSelected = toggleComponent.name;
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
            AppLogger.LogInfo($"'{game}' game selected.");
            // Instantitate the game object and load the appropriate scene.
            AppData.selectedGame = game;
            switch (game)
            {
                case "PINGPONG":
                    break;
                case "TUKTUK":
                    break;
                case "HATTRICK":
                    Debug.Log("HATTRICK Game case.");
                    AppData.hatTrickGame = HatTrickGame.Initialize(AppData.selectedMechanism);
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