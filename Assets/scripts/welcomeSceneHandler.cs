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
using PlutoNeuroRehabLibrary;

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
    public readonly string nextScene = "CHMECH";

    // Private variables
    private bool attachPlutoButtonEvent = false;
    bool changeScene = false;

    // Start is called before the first frame update
    void Start()
    {
        // Check if the directory exists
        if (!Directory.Exists(DataManager.basePath)) Directory.CreateDirectory(DataManager.basePath);
        if (!File.Exists(DataManager.configFile)) SceneManager.LoadScene("CONFIG");
        
        // Initialize.
        AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        daySummaries = AppData.Instance.userData.CalculateMoveTimePerDay();
        
        // Update summary display
        if (!piChartUpdated)
        {
            UpdateUserData();
            UpdatePieChart();
        }
    }

    void Update()
    {
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
        userName.text = AppData.Instance.userData.hospNumber;
        timeRemainingToday.text = $"{AppData.Instance.userData.totalMoveTimeRemaining} min";
        todaysDay.text = AppData.Instance.userData.getCurrentDayOfTraining().ToString();
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
            pies[i].fillAmount = daySummaries[i].MoveTime / AppData.Instance.userData.totalMoveTimePrsc;
            pies[i].color = new Color32(148,234,107,255);
        }
        piChartUpdated = true;
    }

    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= onPlutoButtonReleased;
        }
    }

    private void OnApplicationQuit()
    {
        ConnectToRobot.disconnect();
        AppLogger.StopLogging();
    }
}