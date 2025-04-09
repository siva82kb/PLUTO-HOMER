using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ChooseGameSceneHandler : MonoBehaviour
{
    public GameObject toggleGroup;
    public Button playButton;
    public Button changeMech;
    public TMP_Text result;

    private bool toggleSelected = false;
    private string gameSelected;
    private string changeScene = "CHMECH";
    private readonly Dictionary<string, string> gameScenes = new Dictionary<string, string>
    {
        { "PONG", "PONGMENU" },
        { "TUK", "TUK" },
        { "HAT", "HAT" }
    };
    private bool loadgame = false;

    void Start()
    {
        Debug.Log("start");
        // Initialize if needed
        if (AppData.Instance.userData == null)
        {
            Debug.Log("User data is null");
            // Inialize the logger
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name, doNotResetMech: false);
        }

        // If no mechanism is selected, got to the scene to choose mechanism.
        if (AppData.Instance.selectedMechanism == null)
        {
            // Check if mechnism is set in PLUTO?
            if (PlutoComm.CALIBRATION[PlutoComm.calibration] == "YESCALIB")
            {
                AppData.Instance.SetMechanism(PlutoComm.MECHANISMS[PlutoComm.mechanism]);
            } else
            {
                SceneManager.LoadScene("CHMECH");
                return;
            }
        }

        // Update App Logger
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        AppLogger.SetCurrentGame("NONE");
        
        // Reset selected game.
        AppData.Instance.selectedGame = null;

        // Attach callback.
        AttachCallbacks();

        // Make sure No control is set
        PlutoComm.setControlType("NONE");

        Debug.Log($"Curr ROM: {AppData.Instance.selectedMechanism.currRom.promMin:F2}, {AppData.Instance.selectedMechanism.currRom.promMax:F2}, {AppData.Instance.selectedMechanism.currRom.aromMin:F2}, {AppData.Instance.selectedMechanism.currRom.aromMax:F2}");
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
        Debug.Log("adsgadsg");
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("ASSESS");
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
            // Log the ROM information.
            AppLogger.LogInfo(
                $"Old  PROM: [{AppData.Instance.selectedMechanism.oldRom.promMin:F2}, {AppData.Instance.selectedMechanism.oldRom.promMax:F2}]" +
                $" | AROM: [{AppData.Instance.selectedMechanism.oldRom.aromMin:F2}, {AppData.Instance.selectedMechanism.oldRom.aromMax:F2}]");
            AppLogger.LogInfo(
                $"New  PROM: [{AppData.Instance.selectedMechanism.newRom.promMin:F2}, {AppData.Instance.selectedMechanism.newRom.promMax:F2}]" +
                $" | AROM: [{AppData.Instance.selectedMechanism.newRom.aromMin:F2}, {AppData.Instance.selectedMechanism.newRom.aromMax:F2}]");
            AppLogger.LogInfo(
                $"Curr PROM: [{AppData.Instance.selectedMechanism.currRom.promMin:F2}, {AppData.Instance.selectedMechanism.currRom.promMax:F2}]" +
                $" | AROM: [{AppData.Instance.selectedMechanism.currRom.aromMin:F2}, {AppData.Instance.selectedMechanism.currRom.aromMax:F2}]");
            // Instantitate the game object and load the appropriate scene.
            AppData.Instance.selectedGame = game;
            switch (game)
            {
                case "PONG":
                    break;
                case "TUK":
                    break;
                case "HAT":
                    HatTrickGame.Instance.Initialize(AppData.Instance.selectedMechanism);
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