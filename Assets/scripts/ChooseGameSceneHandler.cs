using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System.Data;
using System.Collections;

public class ChooseGameSceneHandler : MonoBehaviour
{
    public GameObject toggleGroup;
    public Button playButton;
    public Button changeMech;
    public TMP_Text result;
    public GameObject ImgBanner1, ImgBanner2;
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
        // Initialize if needed
        if (AppData.Instance.userData == null)
        {
            Debug.Log("User data is null");
            // Inialize the logger
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name, doNotResetMech: false);
        }
        // Create a new AAN controller.
        AppData.Instance.aanController = new PlutoAANController(
            mechanism: AppData.Instance.selectedMechanism,
            sessionData: AppData.Instance.userData.dTableSession,
            sessionNo: AppData.Instance.currentSessionNumber
        );
        Debug.Log(AppData.Instance.selectedMechanism.IsMechanism("FME1") || AppData.Instance.selectedMechanism.IsMechanism("FME2"));
        Debug.Log(!AppData.Instance.selectedMechanism.IsMechanism("FME1"));
        Debug.Log(!AppData.Instance.selectedMechanism.IsMechanism("FME2"));
        bool isFME = AppData.Instance.selectedMechanism.IsMechanism("FME1") || AppData.Instance.selectedMechanism.IsMechanism("FME2");
        Debug.Log($" isFME :{isFME}");

        ImgBanner1.SetActive(!isFME);
        ImgBanner2.SetActive(!isFME);
        
        // If no mechanism is selected, got to the scene to choose mechanism.
        if (AppData.Instance.selectedMechanism == null)
        {
            // Check if mechnism is set in PLUTO?
            if (PlutoComm.CALIBRATION[PlutoComm.calibration] == "YESCALIB")
            {
                AppData.Instance.SetMechanism(PlutoComm.MECHANISMS[PlutoComm.mechanism]);
            }
            else
            {
                SceneManager.LoadScene("CHMECH");
                return;
            }
        }
        if (File.Exists(DataManager.GetMechControlGainFileName(AppData.Instance.selectedMechanism.name)))
        {

            DataTable gainData = DataManager.loadCSV(DataManager.GetMechControlGainFileName(AppData.Instance.selectedMechanism.name));

            if (gainData.Rows.Count > 0)
            {

                DataRow lastRow = gainData.Rows[gainData.Rows.Count - 1];

                float parsedGain;

                string gainStr = lastRow["ControlGain"].ToString();

                if (float.TryParse(gainStr, out parsedGain))
                {
                    PlutoComm.setControlGain(parsedGain);
                }
            }
        }

        // Update App Logger
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        
        // Reset selected game.
        AppData.Instance.SetGame(null);
        AppData.Instance.previousSuccessRates =null;
        // Attach callback.
        AttachCallbacks();

        // Make sure No control is set
        PlutoComm.setControlType("NONE");

        Debug.Log($"Curr APROM: {AppData.Instance.selectedMechanism.currRom.apromMin:F2}, {AppData.Instance.selectedMechanism.currRom.apromMax:F2}, Curr ROM: {AppData.Instance.selectedMechanism.currRom.promMin:F2}, {AppData.Instance.selectedMechanism.currRom.promMax:F2},{AppData.Instance.selectedMechanism.currRom.aromMin:F2}, {AppData.Instance.selectedMechanism.currRom.aromMax:F2}");
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

        // if (AppData.Instance.selectedMechanism.trialNumberSession >= AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name])
        // {
        //     SceneManager.LoadScene("CHMECH");
        //     // AppData.Instance.setRawDataStringtoNull();
        //     // AppData.Instance.SetMechanism(null);
        //     return;
        // }
        
        if ((PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2"))
        {
            // Magic key cobmination for doing the assessment.
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene("ASSESS");
            }

            //Magic Key combination for control gain.
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
            {
                SceneManager.LoadScene("HATCV");
            }
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

                StartCoroutine(AutoLoadSelectedGame());
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
        AppData.Instance.aanController =null;
        AppData.Instance.userData = new PlutoUserData(DataManager.configFile, DataManager.sessionFile);
        SceneManager.LoadScene(changeScene);
    }
    IEnumerator AutoLoadSelectedGame()
    {
        yield return new WaitForSeconds(0.15f);
        LoadSelectedGameScene(gameSelected);
        toggleSelected = false;
        loadgame = false;
    }


    private void LoadSelectedGameScene(string game)
    {
        if (gameScenes.TryGetValue(game, out string sceneName))
        {
            AppLogger.LogInfo($"'{game}' game selected.");
            // Log the ROM information.
            AppLogger.LogInfo(
                $"Old  PROM: [{AppData.Instance.selectedMechanism.oldRom.promMin:F2}, {AppData.Instance.selectedMechanism.oldRom.promMax:F2}]" +
                $" | AROM: [{AppData.Instance.selectedMechanism.oldRom.aromMin:F2}, {AppData.Instance.selectedMechanism.oldRom.aromMax:F2}]" +
                $" | APROM: [{AppData.Instance.selectedMechanism.oldRom.apromMin:F2}, {AppData.Instance.selectedMechanism.oldRom.apromMax:F2}]");
            AppLogger.LogInfo(
                $"New  PROM: [{AppData.Instance.selectedMechanism.newRom.promMin:F2}, {AppData.Instance.selectedMechanism.newRom.promMax:F2}]" +
                $" | AROM: [{AppData.Instance.selectedMechanism.newRom.aromMin:F2}, {AppData.Instance.selectedMechanism.newRom.aromMax:F2}]" +
                $" | APROM: [{AppData.Instance.selectedMechanism.newRom.apromMin:F2}, {AppData.Instance.selectedMechanism.newRom.apromMax:F2}]");
            AppLogger.LogInfo(
                $"Curr PROM: [{AppData.Instance.selectedMechanism.currRom.promMin:F2}, {AppData.Instance.selectedMechanism.currRom.promMax:F2}]" +
                $" | AROM: [{AppData.Instance.selectedMechanism.currRom.aromMin:F2}, {AppData.Instance.selectedMechanism.currRom.aromMax:F2}]" +
                $" | APROM: [{AppData.Instance.selectedMechanism.currRom.apromMin:F2}, {AppData.Instance.selectedMechanism.currRom.apromMax:F2}]");
            // Instantitate the game object and load the appropriate scene.
            AppData.Instance.SetGame(game);

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