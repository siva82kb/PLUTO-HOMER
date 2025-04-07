
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor.PackageManager;

using System.Globalization;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using PlutoNeuroRehabLibrary;
using System.Text;
using XCharts.Runtime;
using System.Diagnostics;
using UnityEngine;
using System.Diagnostics.Contracts;

/*
 * HOMER PLUTO Application Data Class.
 */
public partial class AppData
{
    // Singleton
    private static readonly Lazy<AppData> _instance = new Lazy<AppData>(() => new AppData());
    public static AppData Instance => _instance.Value;

    /*
     * CONSTANT FIXED VARIABLES.
     */
    // COM Port for the device
    public const string COMPort = "COM4";

    // Keeping track of time.
    private double nanosecPerTick = 1.0f / Stopwatch.Frequency;
    private Stopwatch stp_watch = new Stopwatch();
    public double CurrentTime => stp_watch.ElapsedTicks * nanosecPerTick;

    // Sessions file definitions.
    public string[] sessionFileHeader = new string[] {
        "SessionNumber", "DateTime", "Device", "Assessment", "StartTime", "StopTime",
        "GameName", "TrialDataFileLocation", "DeviceSetupLocation", "AssistMode",
        "AssistModeParameters", "GameParameter", "Mechanism", "MoveTime", "GameSpeed",
        "SuccessRate", "DesiredSuccessRate", "TrialNumber", "TrialType"
    };

    // Change true to run game from choosegamescene
    public bool runIndividualGame = false;


    /*
     * APP CONTROL FLOW VARIABLES.
     */
    private bool loggedIn = false;
    // Old and new PROM used by assessment scene
    //public static ROM oldROM;
    //public static ROM newROM;
    //public static ROM oldAROM;
    //public static ROM newAROM;

    //public static float[] aRomValue = new float[2];
    //public static float[] pRomValue = new float[2];
    //temp storage for PROM min and max

    //public static float promMin = 0f;
    //public static float promMax = 0f;

    // What is this used for?
    public string _dataLogDir = null;
    
    /*
     * USED AND THERAPY RELATED DATA.
     */
    public PlutoUserData userData;
    public HatTrickGame hatTrickGame;
    public PlutoMechanism selectedMechanism = null;
    public string selectedGame = null;
    
    /*
     * SESSION DETAILS
     */
    public string trialDataFileLocation1;
    //private bool _sessionStarted;
    //private DateTime _sessionDateTime;
    //private GameSession _currentSession;
    //private readonly string _sessionFilePath;
    //private bool _loginCalled; // Track if login has been called once
    //private readonly string csvFilePath;
    public int currentSessionNumber { get; set; }
    public DateTime startTime { get; private set; }
    public DateTime? stopTime { get; private set; }
    public bool assessment { get; private set; }
    public string trialDataFileLocation { get; set; }
    public string deviceSetupLocation { get; set; }
    public string assistMode { get; set; }
    public string assistModeParameters { get; set; }
    public string gameParameter { get; set; }
    public string mechanism { get; set; }
    public string moveTime { get; set; }
    public float gameSpeed { get; set; }
    public float successRate { get; set; }
    public float desiredSuccessRate { get; set; }
    public int trialNumber { get; set; }
    public string trialType { get; set; }

    public void SetStopTime() => stopTime = DateTime.Now;

    //// Options to drive 
    //public static string trainingSide
    //{
    //    get => AppData.userData?.rightHand == true ? "RIGHT" : "LEFT";
    //}

    //// Selected Mechanism
    //public static PlutoMechanism selectedMechanism = null;
    ////public static string selectedMechanism;
    //public static string selectedGame = null;

    // Handling the data
    //public static int currentSessionNumber;
    //public static string trialDataFileLocation;
    //public static string trialDataFileLocation1;

    private AppData()
    {
    }

    public void Initialize(string scene, bool doNotResetMech = true)
    {
        // Set sesstion start time.
        startTime = DateTime.Now;

        // Create file structure.
        DataManager.createFileStructure();

        // Start logging.
        AppLogger.StartLogging(scene);

        // Connect and init robot.
        InitializeRobotConnection(doNotResetMech);

        // Initialize the session manager.
        //SessionManager.Initialize(DataManager.sessionPath);
        //SessionManager.Instance.Login();

        // Initialize the user data.
        userData = new PlutoUserData(DataManager.configFile, DataManager.sessionFile);
        // Selected mechanism and game.
        selectedMechanism = null;
        selectedGame = null;

        // Get current session number.
        // Ensure the Sessions.csv file has headers if it doesn't exist
        if (!File.Exists(DataManager.sessionFile))
        {
            using (var writer = new StreamWriter(DataManager.sessionFile, false, Encoding.UTF8))
            {
                writer.WriteLine(String.Join(",", sessionFileHeader));
            }
            AppLogger.LogWarning("Session.csv file now founds. Created one.");
        }
        currentSessionNumber = DataManager.GetPreviousSessionNumber() + 1;
        AppLogger.LogWarning($"Session number set to {currentSessionNumber}.");
    }

    private void InitializeRobotConnection(bool doNotResetMech)
    {
        ConnectToRobot.Connect(COMPort);
        AppLogger.LogInfo($"Connected to PLUTO @ {COMPort}.");
        // Set control to NONE, calibrate and get version.
        PlutoComm.sendHeartbeat();
        PlutoComm.setControlType("NONE");
        // The following code is to ensure that this can be called from other scenes,
        // without having to go through the calibration scene.
        if (!doNotResetMech)
        {
            PlutoComm.calibrate("NOMECH");
        }
        PlutoComm.getVersion();
        // Start sensorstream.
        PlutoComm.sendHeartbeat();
        PlutoComm.startSensorStream();
        AppLogger.LogInfo($"PLUTO SensorStream started.");
    }

    public string trainingSide => userData?.rightHand == true ? "RIGHT" : "LEFT";
    // Check training size.
    public bool IsTrainingSide(string side) => string.Equals(trainingSide, side, StringComparison.OrdinalIgnoreCase);
}
