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
   

    public Image connectStatu;
    public GameObject loading;
    public TextMeshProUGUI name_;
    public TextMeshProUGUI timeRemaining;
    public TextMeshProUGUI days;
    public int daysPassed;
    public DataTable dataTablesession;
    public DataTable dataTableConfig;
  
    public Transform chartContainer;      // Parent container to hold the UI elements
    public GameObject dayEntryPrefab;
    private List<GameObject> createdElements = new List<GameObject>();
    DateTime startDate;
   
    public float totalTime = 90f;
    public bool piChartUpdated = false; 
   
    public float[] elapsedTimeDay = new float[] { 0,0,0,0,0,0,0 };
    public string[] day = new String[] { "","","","","","",""};
    public string[] date = new String[] { "", "", "", "", "", "", "" };
    public static bool scene = false;
    public SessionDataHandler sessionDataHandler;
    // Start is called before the first frame update
    void Start()
    {
        AppData.fileCreation.initializeFilePath();
        AppData.fileCreation.createFileStructure();
        ConnectToRobot.Connect("COM3");
        dayEntryPrefab.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        //To update pi chart
        if ((AppData.fileCreation.filePathSessionData != null && !piChartUpdated)&& AppData.fileCreation.filePathUserData!=null)
        {
            dataTableConfig = new DataTable();
            dataTablesession = new DataTable();
            LoadCSV(AppData.fileCreation.filePathUserData, dataTableConfig);
            sessionDataHandler = new SessionDataHandler(AppData.fileCreation.filePathSessionData);
            sessionDataHandler.CalculateMovTimePerDayWithLinq();
            updateUserData();
            UpdatePieChart();
            piChartUpdated = true;
        }
       
        //To load mech scene
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
    private void UpdatePieChart()
    {
        for (int i = 0; i < sessionDataHandler.elapsedTimeDay.Length; i++)
        {
            // Instantiate the prefab for each day
            GameObject dayEntry = Instantiate(dayEntryPrefab, chartContainer);
            dayEntry.SetActive(true);
            // Get references to the components in the prefab
            Image greenCircle = dayEntry.transform.Find("greencircle").GetComponent<Image>();
            TextMeshProUGUI dayText = dayEntry.transform.Find("day").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI dateText = dayEntry.transform.Find("date").GetComponent<TextMeshProUGUI>();

            // Set the day and date texts
            dayText.text = sessionDataHandler.day[i];
            dateText.text = sessionDataHandler.date[i];

            // Calculate the fill amount based on the elapsed time
            float elapsedPercentage = sessionDataHandler.elapsedTimeDay[i] / totalTime;
            greenCircle.fillAmount = elapsedPercentage;
            greenCircle.color = new Color32(148, 234, 107, 255); // Set to green
            //Debug.Log("woring");
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
            String end = lastRow.Field<string>("end "); ;
            String start = lastRow.Field<string>("start");
            string dateFormat = "dd-MM-yyyy";
            startDate = DateTime.ParseExact(start, dateFormat, CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(end, dateFormat, CultureInfo.InvariantCulture);
            TimeSpan difference = endDate - startDate;
            //ui update
            name_.text = lastRow.Field<string>("name");
            days.text=calculateDaypassed()+"/"+ difference.Days.ToString();
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
    //once pluto button  presses To load the mech scene
    public void onPlutoButtonReleased()
    {
        scene = true;
       
    }

    private void LoadTargetScene()
    {
    SceneManager.LoadScene("chooseMech");
    }
   
   //To read csv file and make it into dataTable
    private void LoadCSV(string filePath,DataTable dataTable)
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
    
    //To calculate how much days gone from the prescibed date
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
