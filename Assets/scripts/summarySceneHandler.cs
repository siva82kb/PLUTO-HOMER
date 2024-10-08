
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XCharts.Runtime;

public class summarySceneHandler : MonoBehaviour
{
    public SessionDataHandler sessionDataHandler;
    public BarChart barchart;
    public Button updateButton; // Reference to the UI Button
    public string title;
    
    public void Start()
    {
       
        title = "summary";
        initializeChart();
    
    }

    void Update()
    {
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
    }
    //To load particular Mechanism data in bargraph
    public void mechanismClicked(Button button)
    {
        title = button.gameObject.name.ToUpper();
        int n = PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, title);
        title = PlutoComm.MECHANISMSTEXT[n];
        sessionDataHandler.CalculateMovTimeForMechanism(button.gameObject.name.ToUpper());
        UpdateChartData();
       
    }
    //To disconnect the Robot 
    public void onPlutoButtonReleased()
    {
        ConnectToRobot.disconnect();
    }
    //To initialize the barchart with whole data of moveTime per day
    public void initializeChart()
    {
        AppData.fileCreation.initializeFilePath();
        sessionDataHandler = new SessionDataHandler(AppData.fileCreation.filePathSessionData);
        sessionDataHandler.summaryCalculateMovTimePerDayWithLinq();
        // Get or add the BarChart component
        barchart = gameObject.GetComponent<BarChart>();
        if (barchart == null)
        {
            barchart = gameObject.AddComponent<BarChart>();
            barchart.Init();
            Debug.Log("BarChart initialized");
        }

        // Set chart title and tooltip visibility
        barchart.EnsureChartComponent<Title>().show = true;
        barchart.EnsureChartComponent<Title>().text = title;

        barchart.EnsureChartComponent<Tooltip>().show = true;
        barchart.EnsureChartComponent<Legend>().show = true;

        // Ensure x and y axes are created
        var xAxis = barchart.EnsureChartComponent<XAxis>();
        var yAxis = barchart.EnsureChartComponent<YAxis>();
        xAxis.show = true;
        yAxis.show = true;
        xAxis.type = Axis.AxisType.Category; // Set x-axis type to Category
        yAxis.type = Axis.AxisType.Value; // Set y-axis type to Value
        yAxis.min = 0; // Make sure bars start from the y=0 line
        yAxis.max = 90; // You can adjust the maximum value as needed

        // Set zoom properties
        var dataZoom = barchart.EnsureChartComponent<DataZoom>();
        dataZoom.enable = true;
        dataZoom.supportInside = true;
        dataZoom.supportSlider = true;
        dataZoom.start = 0;
        dataZoom.end = 100;

        // Initial population of the chart
        UpdateChartData();
    }
    //To update chart with data
   
    public void UpdateChartData()
    {
        if (barchart == null)
        {
            Debug.LogWarning("BarChart is null. Make sure it is initialized.");
            return;
        }

        // Clear any previous data from the chart
        int n = PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, title);

        barchart.RemoveData();
        barchart.EnsureChartComponent<Title>().text = title;
        barchart.AddSerie<Bar>();

        // Update the x-axis data
        var xAxis = barchart.GetChartComponent<XAxis>();
        xAxis.data.Clear();
        foreach (string date in sessionDataHandler.summaryDate)
        {
            xAxis.data.Add(date); // Add x-axis labels (dates)
        }

        // Update the y-axis data (movement time)
        var yAxis = barchart.GetChartComponent<YAxis>();
        yAxis.data.Clear();
        //for (int i = 0; i < sessionDataHandler.summaryDate.Length; i++)
        //{
        //    barchart.AddData(0, sessionDataHandler.summaryElapsedTimeDay[i]); // Add y-axis values

        //}
        // Update the y-axis data (movement time)
        for (int i = 0; i < sessionDataHandler.summaryDate.Length; i++)
        {
            float yValue = sessionDataHandler.summaryElapsedTimeDay[i];

            // Add data and set the color based on the value
            barchart.AddData(0, yValue);
        
        }
        

        // Force the chart to refresh and display the updated data
        barchart.RefreshAllComponent();
        
    }
   
}
