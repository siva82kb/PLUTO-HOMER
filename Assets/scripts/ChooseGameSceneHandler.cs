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
        if (string.IsNullOrEmpty(AppData.selectedMechanism))
        {
            SceneManager.LoadScene("chooseMechanism");
            return;
        }
        //AppData.oldPROM = new ROM(AppData.selectedMechanism);  
        //targetAngle = (AppData.oldPROM.tmax + AppData.oldPROM.tmin)/2;
        //targetAngle= AppData.offsetAtNeutral[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.selectedMechanism)];
        // Update App Logger
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        AppLogger.SetCurrentGame("");
        //AppData.userData.calculateGameSpeedForLastUsageDay();

        // Attach callback.
        AttachCallbacks();


        ////tes
        //Debug.Log("AppData.selectedMechanism: " + AppData.selectedMechanism);
        //Debug.Log("AppData.aRomValue is " + (AppData.aRomValue == null ? "null" : "initialized"));

        //ROM vall = new ROM(AppData.selectedMechanism);
        //if (vall == null)
        //{
        //    Debug.LogError("ROM instance failed to initialize!");
        //}
        //else
        //{
        //    Debug.Log("ROM instance created successfully!");
        //}

        //if (AppData.aRomValue == null)
        //    AppData.aRomValue = new float[2]; // Ensure array exists
        //AppData.pRomValue = new float[2];
        //AppData.aRomValue[0] = vall.aromTmin;
        //AppData.aRomValue[1] = vall.aromTmax;
        //AppData.pRomValue[0] = vall.promTmin;
        //AppData.pRomValue[1] = vall.promTmax;

        // Make sure No control is set
        PlutoComm.setControlType("NONE");
        //AppData.oldAROM=new ROM(AppData.selectedMechanism);
        //if (!gameData.setNeutral)
        //{
        //    StartCoroutine(SetMechanismToTargetAfterDelay(1.0f));
        //}
    }
    void Update()
    {
        PlutoComm.sendHeartbeat();
        if (loadgame)
        {
            LoadSelectedGameScene(selectedGame);
            loadgame = false;
        }
        // Check if PLUTO button is pressed for moving to the next scene.
        //if (isButtonPressed)
        //{
        //    LoadSelectedGameScene(selectedGame);
        //    isButtonPressed = false;
        //}
        // Magic key cobmination for doing the assessment.
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            //assessment();
            SceneManager.LoadScene("Assessment");
        }

        //if (isRunning && !targetReached)
        //{
        //    float currentAngle = PlutoComm.angle;
        //    if (Mathf.Abs(currentAngle - 0f) <= targetTolerance) //for now,its 0 in future we need to change according to mech.
        //    {
        //        targetReached = true;
        //        isRunning = false;
        //        PlutoComm.setControlType("NONE");
        //        Debug.Log($"Target reached: {currentAngle}. Control type set to NONE.");
        //    }
        //}
        //aromValue();
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

    private IEnumerator SetMechanismToTargetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        PlutoComm.setControlType("POSITIONAAN");
        PlutoComm.setControlBound(0.9f);
        PlutoComm.setControlDir(1);
        PlutoComm.setAANTarget(PlutoComm.angle, 0f, 0f, 2f);
        isRunning = true;

        Debug.Log($"Started moving mechanism to {targetAngle} degrees.");
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
            toggleSelected = false;
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
                    AppData.hatTrickGame = new HatTrickGame(AppData.selectedMechanism);
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

    private void assessment()
    {
        string date = AppData.oldROM.datetime;
        Debug.Log($"AppData.oldAROM.datetime: {date}");

        if (!string.IsNullOrEmpty(date))
        {
            DateTime oldDate;
            if (DateTime.TryParseExact(date, "dd-MM-yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out oldDate))
            {
                DateTime currentDate = DateTime.Now;
                TimeSpan timeDifference = currentDate - oldDate;

                result.text = $"Current Date: {currentDate}, Old Date: {oldDate}, Days Passed: {timeDifference.TotalDays:F2}";

                if (timeDifference.TotalDays >= 7)
                {
                    SceneManager.LoadScene("Assessment");
                }
                else
                {
                    Debug.Log($"Only {timeDifference.TotalDays} days have passed. 7 days required.");
                }
            }
            else
            {
                Debug.LogError($"Invalid date format: {date}. Expected format: 'dd-MM-yyyy HH:mm:ss'.");
            }
        }
        else
        {
            Debug.LogError("Date is null or empty.");
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