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
   
    string filepath_user;
    string filepath_session;
    public Image connectStatu;
    public GameObject loading;
    public TextMeshProUGUI name;
    public TextMeshProUGUI timeRemaining;
    public TextMeshProUGUI days;
    public int daysPassed;
    public DataTable dataTablesession;
    public DataTable dataTableConfig;
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
    public static bool scene = false;
    // Start is called before the first frame update
    void Start()
    {
        AppData.fileCreation.createFileStructure();
        filepath_user = AppData.fileCreation.filePath_UserData ;
        filepath_session = AppData.fileCreation.filePath_SessionData ;
        //statusText.text = "connecting..";
        ConnectToRobot.Connect("COM5");

    }

    // Update is called once per frame
    void Update()
    {
        

        // 
        if ((AppData.fileCreation.filePath_SessionData != null && !piChartUpdated)&& AppData.fileCreation.filePath_UserData!=null)
        {
            dataTableConfig = new DataTable();
            dataTablesession = new DataTable();
            LoadCSV(AppData.fileCreation.filePath_SessionData, dataTablesession);
            LoadCSV(AppData.fileCreation.filePath_UserData, dataTableConfig);
            CalculateMovTimePerDayWithLinq();
            updateUserData();
            UpdatePieChart();
        }
        
        
        if (ConnectToRobot.isPLUTO)
        {
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;

            connectStatu.color = Color.green;
            loading.SetActive(false);
            if (scene == true ) {
                LoadTargetScene();
                scene = false;
            }
            
        }
       
        
    }
    private void updateUserData()
    {

        if (dataTableConfig.Rows.Count > 0)
        {
           
           
            DataRow lastRow = dataTableConfig.Rows[dataTableConfig.Rows.Count - 1];
            DateTime today = DateTime.Now.Date;
            var totalMovTimeToday = dataTablesession.AsEnumerable()
           .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date == today)
           .Sum(row => Convert.ToInt32(row["movTime"]));
            float totalMovTimeInMinutes = totalMovTimeToday / 60f;
            float prescribedTime = float.Parse(lastRow.Field<string>("time"));
          
            name.text = lastRow.Field<string>("name");
           
            String end = lastRow.Field<string>("end "); ;
            String start = lastRow.Field<string>("start");
            string dateFormat = "dd-MM-yyyy";

            
            startDate = DateTime.ParseExact(start, dateFormat, CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(end, dateFormat, CultureInfo.InvariantCulture);
           
            TimeSpan difference = endDate - startDate;
            days.text = calculateDaypassed()+"/"+ difference.Days.ToString();
           
            timeRemaining.text = (totalMovTimeInMinutes - prescribedTime).ToString("F0");
            if ((totalMovTimeInMinutes - prescribedTime) > 0)
            {
                timeRemaining.color = Color.green;
            }
        }
        else
        {
            Debug.Log("The DataTable is empty.");
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
    private  void LoadCSV(string filePath,DataTable dataTable)
    {
       
        // Read all lines from the CSV file
        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0) return;

        // Read the header line to create columns
        var headers = lines[0].Split(','); 
        foreach (var header in headers)
        {
            dataTable.Columns.Add(header);
        }

        // Read the rest of the data lines
        for (int i = 1; i < lines.Length; i++)
        {
            var row = dataTable.NewRow();
            var fields = lines[i].Split(',');
            for (int j = 0; j < headers.Length; j++)
            {
                row[j] = fields[j];
            }
            dataTable.Rows.Add(row);
        }

        //Debug.Log("CSV loaded into DataTable with " + dataTable.Rows.Count + " rows.");
    }
    
    public  void CalculateMovTimePerDayWithLinq()
    {
        
        DateTime maxDate = dataTablesession.AsEnumerable()
            .Select(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture))
            .Max();
        DateTime sevenDaysAgo = maxDate.AddDays(-7);
        var movTimePerDay = dataTablesession.AsEnumerable()
             .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date >= sevenDaysAgo) // Filter the last 7 days
            .GroupBy(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date) // Group by date only
            .Select(group => new
            {
                Date = group.Key,      // Format date as "yyyy-MM-dd"
                DayOfWeek = group.Key.DayOfWeek,   // Get the day of the week
                TotalMovTime = group.Sum(row => Convert.ToInt32(row["movTime"]))
            }).OrderByDescending(item => item.Date) // Order by date descending
            .ToList();
       

        
        for (int i = 0; i < movTimePerDay.Count; i++)
        {
            elapsedTimeDay[i] = movTimePerDay[i].TotalMovTime / 60f; // Convert seconds to minutes
            day[i] = GetAbbreviatedDayName(movTimePerDay[i].DayOfWeek);
            date[i] = movTimePerDay[i].Date.ToString("dd/MM");
           
        }
      
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
