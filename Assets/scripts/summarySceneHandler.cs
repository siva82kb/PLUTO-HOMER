

using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XCharts.Runtime;

public class summarySceneHandler : MonoBehaviour
{
    public SessionDataHandler sessionDataHandler;
    public BarChart barchart;
    public string title;
   
    // Existing variables...
    private ConcurrentQueue<System.Action> _actionQueue = new ConcurrentQueue<System.Action>();

    


public void Start()
    {
       
        title = "summary";
        initializeChart();
    
    }

    void Update()
    {
        while (_actionQueue.TryDequeue(out var action))
        {
            action.Invoke(); // Execute the action
        }

        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
    }

    // To load the data for a specific mechanism into the bar graph.
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
        Debug.Log("pressed");

        // Enqueue the disconnect and quit actions
        _actionQueue.Enqueue(() =>
        {
            ConnectToRobot.disconnect();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Stop play mode if in editor
#endif
        });
    }



    //To initialize the barchart with whole data of moveTime per day
    public void initializeChart()
    {
        AppData.fileCreation.initializeFilePath();
        Debug.Log("Is bar chart active: " + barchart.gameObject.activeSelf);

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
        yAxis.max = sessionDataHandler.summaryElapsedTimeDay.Max(); // You can adjust the maximum value as needed

        // Set zoom properties
        var dataZoom = barchart.EnsureChartComponent<DataZoom>();
        dataZoom.enable = true;
        dataZoom.supportInside = true;
        dataZoom.supportSlider = true;
        dataZoom.start = 0;
        dataZoom.end = 100;

        
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
      
        for (int i = 0; i < sessionDataHandler.summaryDate.Length; i++)
        {
            float yValue = sessionDataHandler.summaryElapsedTimeDay[i];
            barchart.AddData(0, yValue);
        
        }
        
        barchart.RefreshAllComponent();
        
    }
   
}
