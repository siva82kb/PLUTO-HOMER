using System; 
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices.ComTypes;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Globalization;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using NeuroRehabLibrary;

public class welcomSceneHandler : MonoBehaviour
{
    //public GameObject loading;
    public TextMeshProUGUI userName;
    public TextMeshProUGUI timeRemainingToday;
    public TextMeshProUGUI todaysDay;
    public TextMeshProUGUI todaysDate;
    public int daysPassed;
    public TextMeshProUGUI[] prevDays = new TextMeshProUGUI[7];
    public TextMeshProUGUI[] prevDates = new TextMeshProUGUI[7];
    public Image[] pies = new Image[7];
    public bool piChartUpdated = false; 
    private DaySummary[] daySummaries;
    public readonly string nextScene = "chooseMechanism";

    // Private variables
    private bool attachPlutoButtonEvent = false;
    bool changeScene = false;

    // Start is called before the first frame update
    void Start()
    {
        // Inialize the logger
        AppLogger.StartLogging(SceneManager.GetActiveScene().name);
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        // Check if the directory exists
        if (!Directory.Exists(DataManager.directoryPath))
        {
            // If not, create the directory
            Directory.CreateDirectory(DataManager.directoryPath);
        }
            if (!File.Exists(DataManager.filePathConfigData))
        {
            SceneManager.LoadScene("configuration");
        }
        else
        {
            // Initialize.
            AppData.initializeStuff();
            // Neuro Library
            string baseDirectory = DataManager.directoryPathSession;
            Debug.Log(baseDirectory);
            SessionManager.Initialize(DataManager.directoryPathSession);
            SessionManager.Instance.Login();
            daySummaries = AppData.userData.CalculateMoveTimePerDay();
            // Update summary display
            if (!piChartUpdated)
            {
                UpdateUserData();
                UpdatePieChart();
            }
        }
        
    }

    void Update()
    {
        //PlutoComm.sendHeartbeat();
        // Attach PlutoButton release event after 2 seconds if it is not attached already.
        if (!attachPlutoButtonEvent && Time.timeSinceLevelLoad > 1)
        {
            attachPlutoButtonEvent = true;
            PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        }
        // Check if it time to switch to the next scene
        if (changeScene == true ) {
            LoadTargetScene();
            changeScene = false;
        }
    }

    public void onPlutoButtonReleased()
    {
        AppLogger.LogInfo("PLUTO button released.");
        changeScene = true;
    }

    private void LoadTargetScene()
    {
        AppLogger.LogInfo($"Switching to the next scene '{nextScene}'.");
        SceneManager.LoadScene(nextScene);
    } 


    private void UpdateUserData()
    {
        userName.text = AppData.userData.hospNumber;
        timeRemainingToday.text = $"{AppData.userData.totalMoveTimeRemaining} min";
        todaysDay.text = AppData.userData.getCurrentDayOfTraining().ToString();
        todaysDate.text = DateTime.Now.ToString("ddd, dd-MM-yyyy");
    }

    private void UpdatePieChart()
    {
        int N = daySummaries.Length;  
        for (int i = 0; i < N; i++)
        {
            Debug.Log($"{i} | {daySummaries[i].Day} | {daySummaries[i].Date} | {daySummaries[i].MoveTime}");
            prevDays[i].text = daySummaries[i].Day;
            prevDates[i].text = daySummaries[i].Date;
            pies[i].fillAmount = daySummaries[i].MoveTime / AppData.userData.totalMoveTimePrsc;
            pies[i].color = new Color32(148,234,107,255);
        }
        piChartUpdated = true;
    }

    private void OnApplicationQuit()
    {
        ConnectToRobot.disconnect();
        AppLogger.StopLogging();
    }
}