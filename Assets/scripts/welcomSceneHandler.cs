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

public class welcomSceneHandler : MonoBehaviour
{
    public GameObject loading;
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
    public static bool scene = false;

    // Private variables
    private bool attachPlutoButtonEvent = false;

    // Start is called before the first frame update
    void Start()
    {
        DataManager.createFileStructure();
        //ConnectToRobot.Connect(AppData.COMPort);

        // Update summary display
        if ((DataManager.filePathSessionData != null && !piChartUpdated) && DataManager.filePathConfigData != null)
        {
            AppData.UserData.readAllUserData();
            daySummaries = AppData.UserData.CalculateMoveTimePerDay();
            UpdateUserData();
            UpdatePieChart();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Attach PlutoButton release event after 2 seconds if it is not attached already.
        if (!attachPlutoButtonEvent && Time.timeSinceLevelLoad > 2)
        {
            attachPlutoButtonEvent = true;
            PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        }
        
        if (scene == true ) {
            LoadTargetScene();
            scene = false;
        }
    }

    public void onPlutoButtonReleased()
    {
        scene = true;
    }

    private void LoadTargetScene()
    {
    SceneManager.LoadScene("chooseMech");
    }
    private void UpdateUserData()
    {
        userName.text = AppData.UserData.hospNumber;
        timeRemainingToday.text = $"{AppData.UserData.totalMoveTimeRemaining} min";
        todaysDay.text = AppData.UserData.getCurrentDayOfTraining().ToString();
        todaysDate.text = DateTime.Now.ToString("ddd, dd-MM-yyyy");
    }

    private void UpdatePieChart()
    {
        for (int i = 0; i < daySummaries.Length; i++)
        {
            prevDays[i].text = daySummaries[i].Day;
            prevDates[i].text = daySummaries[i].Date;
            pies[i].fillAmount = daySummaries[i].MoveTime / AppData.UserData.totalMoveTimePrsc;
            pies[i].color = new Color32(148,234,107,255);
        }
        piChartUpdated = true;
    }
   
    private string GetAbbreviatedDayName(DayOfWeek dayOfWeek)
    {
        
        string[] abbreviatedDayNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;

        return abbreviatedDayNames[(int)dayOfWeek];
    }
    
    private void OnApplicationQuit()
    {
        ConnectToRobot.disconnect();
    }
}