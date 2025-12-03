using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XCharts.Runtime;
using TMPro;
using System.Collections.Generic;
using static PlutoUserData;

public class summarySceneHandler : MonoBehaviour
{
    public SessionDataHandler sessionDataHandler;
    public BarChart barchart;
    public string title;
    private ConcurrentQueue<System.Action> _actionQueue = new ConcurrentQueue<System.Action>();
    public TextMeshProUGUI ttCummulativeScoreTxt;
    public TextMeshProUGUI ppCummulativeScoreTxt;
    public TextMeshProUGUI htCummulativeScoreTxt;
    public TextMeshProUGUI fcCummulativeScoreTxt;
    public TextMeshProUGUI rgCummulativeScoreTxt;
    public TextMeshProUGUI ppCurrentScoreTxt;
    public TextMeshProUGUI ttCurrentScoreTxt;
    public TextMeshProUGUI htCurrentScoreTxt;
    public TextMeshProUGUI fcCurrentScoreTxt;
    public TextMeshProUGUI rgCurrentScoreTxt;

    public GameObject WFEstar;
    public GameObject WURDstar;
    public GameObject FPSstar;
    public GameObject HOCstar;
    public GameObject FME1star;
    public GameObject FME2star;

    int[] cummulativeScores;
    public Transform WFEStarParent;
public Transform WURDStarParent;
public Transform FPSStarParent;
public Transform HOCStarParent;
public Transform FME1StarParent;
public Transform FME2StarParent;

public TextMeshProUGUI WFEStarText;
public TextMeshProUGUI WURDStarText;
public TextMeshProUGUI FPSStarText;
public TextMeshProUGUI HOCStarText;
public TextMeshProUGUI FME1StarText;
public TextMeshProUGUI FME2StarText;
public TextMeshProUGUI compWFEStarText;
public TextMeshProUGUI compWURDStarText;
public TextMeshProUGUI compFPSStarText;
public TextMeshProUGUI compHOCStarText;
public TextMeshProUGUI compFME1StarText;
public TextMeshProUGUI compFME2StarText;


    
    public void Start()
    {
        title = "summary";
        initializeChart();

        List<MechanismStats> mechStats = AppData.Instance.userData.ReadMechanismStarStats();
        displayMechanismStars(mechStats);
    }
    private void displayMechanismStars(List<MechanismStats> stats)
    {
        foreach (var s in stats)
        {
            Transform parent = null;
            TextMeshProUGUI txt = null;
            TextMeshProUGUI comptxt = null;


            switch (s.Mechanism)
            {
                case "WFE": parent = WFEStarParent; txt = WFEStarText; comptxt = compWFEStarText; if(s.CumulativeStars>0)WFEstar.GetComponent<Image>().color = Color.white; break;
                case "WURD": parent = WURDStarParent; txt = WURDStarText; comptxt = compWURDStarText; if(s.CumulativeStars>0)WURDstar.GetComponent<Image>().color = Color.white; break;
                case "FPS": parent = FPSStarParent; txt = FPSStarText; comptxt = compFPSStarText; if(s.CumulativeStars>0)FPSstar.GetComponent<Image>().color = Color.white; break;
                case "HOC": parent = HOCStarParent; txt = HOCStarText; comptxt = compHOCStarText; if(s.CumulativeStars>0)HOCstar.GetComponent<Image>().color = Color.white; break;
                case "FME1": parent = FME1StarParent; txt = FME1StarText; comptxt = compFME1StarText; if(s.CumulativeStars>0)FME1star.GetComponent<Image>().color = Color.white; break;
                case "FME2": parent = FME2StarParent; txt = FME2StarText; comptxt = compFME2StarText; if(s.CumulativeStars>0)FME2star.GetComponent<Image>().color = Color.white; break;
            }

            if (parent == null || txt == null) continue;

            // Reset all stars to grey
            for (int i = 0; i < parent.childCount; i++)
                parent.GetChild(i).GetComponent<Image>().color = Color.black;

            // Color today’s stars white
            for (int i = 0; i < s.TodayStars; i++)
                parent.GetChild(i).GetComponent<Image>().color = Color.white;

            // Text format => Today / Yesterday / Total
            txt.text = $"{s.CumulativeStars:D3}";
            comptxt.text = $"{s.TodayStars:D2}/{s.YesterdayStars:D2}";
        }
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
        // Debug.Log("button name:" + title);
        int n = Array.IndexOf(PlutoComm.MECHANISMS, title);
        title = PlutoComm.MECHANISMSTEXT[n-1];
        sessionDataHandler.CalculateMovTimeForMechanism(button.gameObject.name.ToUpper());
        UpdateChartData();
       
    }
    //To disconnect the Robot 
    public void onPlutoButtonReleased()
    {
            // SceneManager.LoadScene("DATAUPLOAD");

        _actionQueue.Enqueue(() =>
        {
            PlutoComm.stopSensorStream();

            ConnectToRobot.disconnect();
            SceneManager.LoadScene("DATAUPLOAD");
        });
    }

    //To initialize the barchart with whole data of moveTime per day
    public void initializeChart()
    {
        // Debug.Log("Is bar chart active: " + barchart.gameObject.activeSelf);

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
            // Debug.LogWarning("BarChart is null. Make sure it is initialized.");
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
