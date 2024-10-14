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
    public TextMeshProUGUI name;
    public TextMeshProUGUI timeRemaining;
    public TextMeshProUGUI days;
    public int daysPassed;
    //public DataTable dataTablesession;
    //public DataTable dataTableConfig;
    public TextMeshProUGUI day1;
    public TextMeshProUGUI day2;
    public TextMeshProUGUI day3;
    public TextMeshProUGUI day4;
    public TextMeshProUGUI day5;
    public TextMeshProUGUI day6;
    public TextMeshProUGUI day7;
    public TextMeshProUGUI date1;
    public TextMeshProUGUI date2;
    public TextMeshProUGUI date3;
    public TextMeshProUGUI date4;
    public TextMeshProUGUI date5;
    public TextMeshProUGUI date6;
    public TextMeshProUGUI date7;
    public Image greenCircleImageDay1; // Partially filled green circle
    public Image greenCircleImageDay2; // Partially filled green circle
    public Image greenCircleImageDay3; // Partially filled green circle
    public Image greenCircleImageDay4; // Partially filled green circle
    public Image greenCircleImageDay5; // Partially filled green circle
    public Image greenCircleImageDay6; // Partially filled green circle
    public Image greenCircleImageDay7; // Partially filled green circle
    public Image[] greenCircleImages;
    public TextMeshProUGUI[] weekDays;
    public TextMeshProUGUI[] dates;
    DateTime startDate;
    // Total time (e.g., 90 minutes)
    public float totalTime = 90f;
    public bool piChartUpdated = false; 
    // Elapsed time (e.g., 30 minutes)
    public float[] elapsedTimeDay = new float[] { 0,0,0,0,0,0,0 };
    public string[] day = new String[] { "","","","","","",""};
    public string[] date = new String[] { "", "", "", "", "", "", "" };
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
            // Read all the user data
            AppData.UserData.readAllUserData();
            // Compute the total movement time for every training day so far.
            daySummaries = AppData.UserData.CalculateMoveTimePerDay();
            //updateUserData();
            //UpdatePieChart();
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

    private void updateUserData()
    {
        //if (dataTableConfig.Rows.Count > 0)
        //{
        //    DataRow lastRow = dataTableConfig.Rows[dataTableConfig.Rows.Count - 1];
        //    DateTime today = DateTime.Now.Date;
        //    var totalMovTimeToday = dataTablesession.AsEnumerable()
        //   .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date == today)
        //   .Sum(row => Convert.ToInt32(row["movTime"]));
        //    float totalMovTimeInMinutes = totalMovTimeToday / 60f;
        //    float prescribedTime = float.Parse(lastRow.Field<string>("time"));
          
        //    name.text = lastRow.Field<string>("name");
           
        //    String end = lastRow.Field<string>("end "); ;
        //    String start = lastRow.Field<string>("start");
        //    string dateFormat = "dd-MM-yyyy";

            
        //    startDate = DateTime.ParseExact(start, dateFormat, CultureInfo.InvariantCulture);
        //    DateTime endDate = DateTime.ParseExact(end, dateFormat, CultureInfo.InvariantCulture);
           
        //    TimeSpan difference = endDate - startDate;
        //    days.text = calculateDaypassed()+"/"+ difference.Days.ToString();
           
        //    timeRemaining.text = (totalMovTimeInMinutes - prescribedTime).ToString("F0");
        //    if ((totalMovTimeInMinutes - prescribedTime) > 0)
        //    {
        //        timeRemaining.color = Color.green;
        //    }
        //}
        //else
        //{
        //    Debug.Log("The DataTable is empty.");
        //}

    }

    public void onPlutoButtonReleased()
    {
        scene = true;
    }

    private void LoadTargetScene()
    {
    SceneManager.LoadScene("chooseMech");
    }
    private void UpdatePieChart()
    {
        greenCircleImages = new Image[7]
       {
            greenCircleImageDay1, // Day 1
            greenCircleImageDay2, // Day 2
            greenCircleImageDay3, // Day 3
            greenCircleImageDay4, // Day 4
            greenCircleImageDay5, // Day 5
            greenCircleImageDay6, // Day 6
            greenCircleImageDay7  // Day 7
       };
        weekDays = new TextMeshProUGUI[7]
        {
            day1, day2, day3, day4, day5, day6, day7
        };
        dates = new TextMeshProUGUI[7]
        {
            date1, date2, date3, date4, date5, date6, date7
        };
        for (int i = 0; i < elapsedTimeDay.Length; i++)
        {
           
            
            float elapsedPercentage = elapsedTimeDay[i] / totalTime;
            weekDays[i].text = day[i];
            dates[i].text = date[i];
            
            greenCircleImages[i].fillAmount = elapsedPercentage;

            // Green color for the elapsed portion
            greenCircleImages[i].color = new Color32(148,234,107,255);
        }
        piChartUpdated = true;
    }
    
    public  void CalculateMovTimePerDayWithLinq()
    {
        
        //DateTime maxDate = dataTablesession.AsEnumerable()
        //    .Select(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture))
        //    .Max();
        //DateTime sevenDaysAgo = maxDate.AddDays(-7);
        //var movTimePerDay = dataTablesession.AsEnumerable()
        //     .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date >= sevenDaysAgo) // Filter the last 7 days
        //    .GroupBy(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date) // Group by date only
        //    .Select(group => new
        //    {
        //        Date = group.Key,      // Format date as "yyyy-MM-dd"
        //        DayOfWeek = group.Key.DayOfWeek,   // Get the day of the week
        //        TotalMovTime = group.Sum(row => Convert.ToInt32(row["movTime"]))
        //    }).OrderByDescending(item => item.Date) // Order by date descending
        //    .ToList();
       

        
        //for (int i = 0; i < movTimePerDay.Count; i++)
        //{
        //    elapsedTimeDay[i] = movTimePerDay[i].TotalMovTime / 60f; // Convert seconds to minutes
        //    day[i] = GetAbbreviatedDayName(movTimePerDay[i].DayOfWeek);
        //    date[i] = movTimePerDay[i].Date.ToString("dd/MM");
           
        //}
      
    }
   
    private string GetAbbreviatedDayName(DayOfWeek dayOfWeek)
    {
        
        string[] abbreviatedDayNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;

        return abbreviatedDayNames[(int)dayOfWeek];
    }
    public int calculateDaypassed()
    {
        
        TimeSpan duration = DateTime.Now - startDate;
        daysPassed = (int)duration.TotalDays;
        return daysPassed;
    }
    private void OnApplicationQuit()
    {
        ConnectToRobot.disconnect();
    }
  
}
