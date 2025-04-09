
using Newtonsoft.Json.Linq;
using System;
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
            action.Invoke(); 
        }

        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
    }

    // To load the data for a specific mechanism into the bar graph.
    public void mechanismClicked(Button button)
    {
        title = button.gameObject.name.ToUpper();
        Debug.Log("button name:" + title);
        int n = Array.IndexOf(PlutoComm.MECHANISMS, title);
        title = PlutoComm.MECHANISMSTEXT[n];
        sessionDataHandler.CalculateMovTimeForMechanism(button.gameObject.name.ToUpper());
        UpdateChartData();
       
    }
    //To disconnect the Robot 
    public void onPlutoButtonReleased()
    {
        _actionQueue.Enqueue(() =>
        {
            PlutoComm.stopSensorStream();

            ConnectToRobot.disconnect();
            Application.Quit();
        #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false; 
        #endif
        });
    }

    //To initialize the barchart with whole data of moveTime per day
    public void initializeChart()
    {
        Debug.Log("Is bar chart active: " + barchart.gameObject.activeSelf);

        sessionDataHandler = new SessionDataHandler(DataManager.sessionFile);

        sessionDataHandler.summaryCalculateMovTimePerDayWithLinq();

        barchart = gameObject.GetComponent<BarChart>();
        if (barchart == null)
        {
            barchart = gameObject.AddComponent<BarChart>();
            barchart.Init();;
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
        xAxis.type = Axis.AxisType.Category; 
        yAxis.type = Axis.AxisType.Value;
        yAxis.min = 0; 
        yAxis.max = sessionDataHandler.summaryElapsedTimeDay.Max(); 

        
        var dataZoom = barchart.EnsureChartComponent<DataZoom>();
        dataZoom.enable = true;
        dataZoom.supportInside = true;
        dataZoom.supportSlider = true;
        dataZoom.start = 0;
        dataZoom.end = 100;

        UpdateChartData();
    }
   
    public void UpdateChartData()
    {
        if (barchart == null)
        {
            Debug.LogWarning("BarChart is null. Make sure it is initialized.");
            return;
        }

        // Clear any previous data from the chart
        int n = Array.IndexOf(PlutoComm.MECHANISMS, title);

        barchart.RemoveData();
        barchart.EnsureChartComponent<Title>().text = title;
        barchart.AddSerie<Bar>();

        var xAxis = barchart.GetChartComponent<XAxis>();
        xAxis.data.Clear();
        foreach (string date in sessionDataHandler.summaryDate)
        {
            xAxis.data.Add(date); // Add x-axis labels (dates)
        }

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
