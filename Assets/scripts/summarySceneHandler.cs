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
using System.IO;

public class summarySceneHandler : MonoBehaviour
{
    public SessionDataHandler sessionDataHandler;
    // public BarChart lineChart;
    public LineChart lineChart;

    public string title;
    private ConcurrentQueue<System.Action> _actionQueue = new ConcurrentQueue<System.Action>();

    public GameObject WFEstar;
    public GameObject WURDstar;
    public GameObject FPSstar;
    public GameObject HOCstar;
    public GameObject FME1star;
    public GameObject FME2star;
    public GameObject TOTstar;
    // public GameObject WFE;
    // public GameObject WURD;
    // public GameObject FPS;
    // public GameObject HOC;
    // public GameObject FME1;
    // public GameObject FME2;

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
    public TextMeshProUGUI compTOTStarText;
    public TextMeshProUGUI TOTStarText;
    private int totalstar;

    public Image[] mechimages;

    [Header("Mechanism List Parent")]
    public RectTransform mechanismList;

    [Header("Mechanism Rows (Images)")]
    public GameObject WFE;
    public GameObject WURD;
    public GameObject FPS;
    public GameObject HOC;
    public GameObject FME1;
    public GameObject FME2;

    [Header("Star Colors")]
    [SerializeField] private Color starEmpty = Color.black;
    [SerializeField] private Color starFilled = new Color(1f, 0.84f, 0f);

    private Dictionary<string, GameObject> mechMap;


    
    // public void Start()
    // {
    //     //debugger
    //     // AppData.Instance.Initialize(SceneManager.GetActiveScene().name);

    //     title = "summary";
    //     ShowOnlyPrescribedMechanisms();   // hide unused ones
    //     initializeChart();

    //     List<MechanismStats> mechStats = AppData.Instance.userData.ReadMechanismStarStats();

    //     mechMap = new Dictionary<string, GameObject>()
    //     {
    //         { "WFE", WFE },
    //         { "WURD", WURD },
    //         { "FPS", FPS },
    //         { "HOC", HOC },
    //         { "FME1", FME1 },
    //         { "FME2", FME2 }
    //     };

    //     // UpdateMechanismRows();
    //     displayMechanismStars(mechStats);
    // }

    public void Start()
{
     AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
    AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
    title = "Unlock Your Potential Through Play";
    initializeChart();

    List<MechanismStats> mechStats =
        AppData.Instance.userData.ReadMechanismStarStats();

    mechMap = new Dictionary<string, GameObject>()
    {
        { "WFE", WFE },
        { "WURD", WURD },
        { "FPS", FPS },
        { "HOC", HOC },
        { "FME1", FME1 },
        { "FME2", FME2 }
    };

    UpdateMechanismRows(mechStats);
    displayMechanismStars(mechStats);
    PlutoComm.OnButtonReleased += onPlutoButtonReleased;

}


    private Dictionary<string, MechanismStats> BuildStatLookup(List<MechanismStats> stats)
    {
        Dictionary<string, MechanismStats> map = new Dictionary<string, MechanismStats>();
        foreach (var s in stats)
            map[s.Mechanism] = s;

        return map;
    }
    private void UpdateMechanismRows(List<MechanismStats> stats)
{
    var statMap = BuildStatLookup(stats);

    foreach (var mech in mechMap)
    {
        string mechName = mech.Key;
        GameObject row = mech.Value;

        bool isPrescribed =
            AppData.Instance.userData.mechMoveTimePrsc[mechName] > 0;

        row.SetActive(isPrescribed);
        if (!isPrescribed) continue;

        // ⭐ Use TodayStars FROM THE LIST
        if (!statMap.TryGetValue(mechName, out MechanismStats mechStat))
            continue;

        Transform starsParent = row.transform.Find("Stars");
        if (starsParent != null)
        {
            UpdateStars(starsParent, mechStat.TodayStars);
        }
    }

    // Force vertical stretch of remaining rows
    LayoutRebuilder.ForceRebuildLayoutImmediate(mechanismList);
}


    private void ShowOnlyPrescribedMechanisms()
    {
        Dictionary<string, GameObject> mechObjects = new Dictionary<string, GameObject>()
        {
            { "WFE", WFE },
            { "WURD", WURD },
            { "FPS", FPS },
            { "HOC", HOC },
            { "FME1", FME1 },
            { "FME2", FME2 }
        };

        foreach (var kvp in mechObjects)
        {
            string mech = kvp.Key;
            GameObject obj = kvp.Value;

            bool isPrescribed = AppData.Instance.userData.mechMoveTimePrsc[mech] > 0;

            obj.SetActive(isPrescribed);   // Only show if prescribed
        }
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
                case "WFE": parent = WFEStarParent; txt = WFEStarText; comptxt = compWFEStarText; if(s.TodayStars>0)WFEstar.GetComponent<Image>().color = Color.white; break;
                case "WURD": parent = WURDStarParent; txt = WURDStarText; comptxt = compWURDStarText; if(s.TodayStars>0)WURDstar.GetComponent<Image>().color = Color.white; break;
                case "FPS": parent = FPSStarParent; txt = FPSStarText; comptxt = compFPSStarText; if(s.TodayStars>0)FPSstar.GetComponent<Image>().color = Color.white; break;
                case "HOC": parent = HOCStarParent; txt = HOCStarText; comptxt = compHOCStarText; if(s.TodayStars>0)HOCstar.GetComponent<Image>().color = Color.white; break;
                case "FME1": parent = FME1StarParent; txt = FME1StarText; comptxt = compFME1StarText; if(s.TodayStars>0)FME1star.GetComponent<Image>().color = Color.white; break;
                case "FME2": parent = FME2StarParent; txt = FME2StarText; comptxt = compFME2StarText; if(s.TodayStars>0)FME2star.GetComponent<Image>().color = Color.white; break;
            }

            if (parent == null || txt == null) continue;

            for (int i = 0; i < parent.childCount; i++)
                parent.GetChild(i).GetComponent<Image>().color = Color.black;

            for (int i = 0; i < s.TodayStars; i++)
                parent.GetChild(i).GetComponent<Image>().color = Color.white;

            // Text format => Today / Yesterday / Total
            txt.text = $"{s.TodayStars:D1}";
            comptxt.text = $"{s.CumulativeStars:D3}/{s.CumulativeStarsYesterday:D3}";
            totalstar=s.CumulativeStars;

            TOTStarText.text = $"{s.CumulativeStars:D3}";
            compTOTStarText.text=$"{s.CumulativeStarsYesterday:D3}";
        }
        if(totalstar>0)TOTstar.GetComponent<Image>().color = Color.white;

    }

    void Update()
    {
        PlutoComm.sendHeartbeat();

        while (_actionQueue.TryDequeue(out var action))
        {
            action.Invoke(); 
        }

    }

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
        PlutoComm.stopSensorStream();


        _actionQueue.Enqueue(() =>
        {
            
            ConnectToRobot.disconnect();
            if (AppData.isNRSVersion)
            {
                Application.Quit();

            #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
            #endif
            }
            else
            {
            SceneManager.LoadScene("DATAUPLOAD");
            }

                    if (AppData.isNRSVersion) {
 
            try
            {
                // Create marker file for NRS device setup check
                string dirPath = "C:/DeviceSetups/Pluto";
                string filePath = Path.Combine(dirPath, "pluto_demo_done.txt");
                Directory.CreateDirectory(dirPath);
                File.WriteAllText(filePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                AppLogger.LogInfo("Created marker file at: " + filePath);
                Application.Quit();
                #if UNITY_EDITOR
                         UnityEditor.EditorApplication.isPlaying = false;
                #endif
            }
            catch (System.Exception ex)
            {
                // Debug.LogError("Failed to create marker file: " + ex.Message);
            }
            return;
        }
        });
    }
   
    public void initializeChart()
    {
        sessionDataHandler = new SessionDataHandler(DataManager.sessionFile);

        sessionDataHandler.summaryCalculateMovTimePerDayWithLinq();
        sessionDataHandler.LoadConfigDates(AppData.Instance.userData.dTableConfig);

        lineChart = gameObject.GetComponent<LineChart>();
        if (lineChart == null)
        {
            lineChart = gameObject.AddComponent<LineChart>();
            lineChart.Init();
        }

        // Title
        var titleComp = lineChart.EnsureChartComponent<Title>();
        titleComp.show = true;
        titleComp.text = title;

        // Tooltip & Legend
        lineChart.EnsureChartComponent<Tooltip>().show = true;
        lineChart.EnsureChartComponent<Legend>().show = true;

        // Axes
        var xAxis = lineChart.EnsureChartComponent<XAxis>();
        var yAxis = lineChart.EnsureChartComponent<YAxis>();

        xAxis.show = true;
        yAxis.show = true;

        xAxis.type = Axis.AxisType.Category;
        yAxis.type = Axis.AxisType.Value;

        // Fixed Y-axis limit 0 - 100
        yAxis.min = 0;
        yAxis.max = 100;

        // Add horizontal deadline at 60
        var markLine = lineChart.EnsureChartComponent<MarkLine>();
        markLine.show = true;
        markLine.data.Clear();

        // Add line at y = 60
        markLine.data.Add(new MarkLine.Data()
        {
            yValue = 60,
            name = "Deadline"
        });

        // Enable zoom
        var dataZoom = lineChart.EnsureChartComponent<DataZoom>();
        dataZoom.enable = true;
        dataZoom.supportInside = true;
        dataZoom.supportSlider = true;
        dataZoom.start = 0;
        dataZoom.end = 100;

        UpdateChartData();
    }

    public void UpdateChartData()
    {
        if (lineChart == null) return;
        // AppLogger.LogInfo("linechart is not null");

        lineChart.RemoveData();
        lineChart.EnsureChartComponent<Title>().text = title;

        lineChart.AddSerie<Line>();

        var xAxis = lineChart.GetChartComponent<XAxis>();
        xAxis.data.Clear();

        DateTime today = DateTime.Today;

        for (int i = 0; i < sessionDataHandler.summaryDate.Length; i++)
        {
            string dateStr = sessionDataHandler.summaryDate[i];
            // xAxis.data.Add(dateStr);   // Always show labels

            // Parse date
            DateTime entryDate = DateTime.Parse(dateStr);
            // Always show labels in the formate of date/Month
            xAxis.data.Add(entryDate.ToString("dd/MM"));

            if (entryDate > today)
            {
                // Add empty value → line breaks here
                lineChart.AddData(0, null);
            }
            else
            {
                // Add actual data
                float value = sessionDataHandler.summaryElapsedTimeDay[i];
                lineChart.AddData(0, value);
            }
        }
          AppLogger.LogInfo("DataUpdated successfully");
        lineChart.RefreshAllComponent();
    }



    private void UpdateStars(Transform starsParent, int todayStars)
    {
        todayStars = Mathf.Clamp(todayStars, 0, 5);

        for (int i = 0; i < starsParent.childCount; i++)
        {
            Image star = starsParent.GetChild(i).GetComponent<Image>();
            if (star == null) continue;

            star.color = i < todayStars ? starFilled : starEmpty;
        }
    }

}
