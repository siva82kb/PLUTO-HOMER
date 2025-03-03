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
    private static bool isButtonPressed = false;
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


    void Start()
    {

        // Initialize if needed
        if (AppData.UserData.dTableConfig == null)
        {
            // Inialize the logger
            AppLogger.StartLogging(SceneManager.GetActiveScene().name);
            AppData.initializeStuff();
            AppData.selectedMechanism = "FPS";
            AppLogger.SetCurrentMechanism(AppData.selectedMechanism);
        }
        AppData.oldPROM = new ROM(AppData.selectedMechanism);
        //targetAngle = (AppData.oldPROM.tmax + AppData.oldPROM.tmin)/2;
        targetAngle= AppData.offsetAtNeutral[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS,AppData.selectedMechanism)];
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        AppLogger.SetCurrentGame("");
        AppData.UserData.CalculateGameSpeedForLastUsageDay();
        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;

        //tes
        Debug.Log("AppData.selectedMechanism: " + AppData.selectedMechanism);
        Debug.Log("AppData.aRomValue is " + (AppData.aRomValue == null ? "null" : "initialized"));

        ROM vall = new ROM(AppData.selectedMechanism);
        if (vall == null)
        {
            Debug.LogError("ROM instance failed to initialize!");
        }
        else
        {
            Debug.Log("ROM instance created successfully!");
        }

        if (AppData.aRomValue == null)
            AppData.aRomValue = new float[2]; // Ensure array exists
        AppData.pRomValue = new float[2];
        AppData.aRomValue[0] = vall.aromTmin;
        AppData.aRomValue[1] = vall.aromTmax;
        AppData.pRomValue[0] = vall.promTmin;
        AppData.pRomValue[1] = vall.promTmax;


        AttachToggleListeners();
        PlutoComm.setControlType("NONE");
        playButton.onClick.AddListener(OnPlayButtonClicked);
        changeMech.onClick.AddListener(OnMechButtonClicked);
        AppData.oldAROM=new ROM(AppData.selectedMechanism);
        if (!gameData.setNeutral)
        {
            StartCoroutine(SetMechanismToTargetAfterDelay(1.0f));
        }
    }
    void Update()
    {   
        PlutoComm.sendHeartbeat();
        if (isButtonPressed)
        {
            LoadSelectedGameScene(selectedGame);
            isButtonPressed = false;
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            //assessment();
           SceneManager.LoadScene("Assessment");
        }

        if (isRunning && !targetReached)
        {
            float currentAngle = PlutoComm.angle;
            if (Mathf.Abs(currentAngle - 0f) <= targetTolerance) //for now,its 0 in future we need to change according to mech.
            {
                targetReached = true;
                isRunning = false;
                PlutoComm.setControlType("NONE");
                Debug.Log($"Target reached: {currentAngle}. Control type set to NONE.");
            }
        }
        //aromValue();
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
                AppData.selectedGame = selectedGame;
                AppLogger.SetCurrentGame(AppData.selectedGame);
                AppLogger.LogInfo($"Selected game '{AppData.selectedGame}'.");
                toggleSelected = true; 
                break; 
            }
        }
    }

    private void OnPlayButtonClicked()
    {
        if (toggleSelected)
        {
            LoadSelectedGameScene(selectedGame);
            toggleSelected = false;
           
        }
        else
        {
            Debug.Log("No game selected. Please select a game.");
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
            Debug.Log("Scene name:"+ sceneName);
            if (AppData.selectedMechanism != "HOC" && !gameData.setNeutral)
            {
                gameData.setNeutral = true;
                //PlutoComm.calibrate(AppData.selectedMechanism); //its temp, needs to set 0 using control type 
            }
            SceneManager.LoadScene(sceneName);
        }
    }
    public void OnPlutoButtonReleased()
    {
        if (toggleSelected)
        {
            isButtonPressed=true;
            toggleSelected = false;
        }
        else
        {
            Debug.Log("No game selected. Please select a game.");
        }
    }
    private void assessment()
    {
        string date = AppData.oldAROM.datetime; 
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
