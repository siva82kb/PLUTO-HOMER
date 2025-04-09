
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
    public const string COMPort = "COM5";

    // Keeping track of time.
    private double nanosecPerTick = 1.0f / Stopwatch.Frequency;
    private Stopwatch stp_watch = new Stopwatch();
    public double CurrentTime => stp_watch.ElapsedTicks * nanosecPerTick;

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
    public PlutoMechanism selectedMechanism { get; private set; }
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
    public int trialNumberDay { get; set; }
    public int trialNumberSession { get; set; }
    public string trialType { get; set; }
    public DateTime trialStartTime { get; set; }
    public DateTime? trialStopTime { get; set; }

    public void SetStopTime() => stopTime = DateTime.Now;

    /*
     * AAN Data
     */
    public PlutoAANController aanController = null;

    //public static string aanDataFileLocation = null;
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
        DataManager.CreateFileStructure();

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
        currentSessionNumber = userData.dTableSession.Rows.Count > 0 ? 
            Convert.ToInt32(userData.dTableSession.Rows[userData.dTableSession.Rows.Count - 1]["SessionNumber"]) + 1 : 1;
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

    public void SetMechanism(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            selectedMechanism = null;
            aanController = null;
            trialNumberDay = -1;
            trialNumberSession = -1;
            AppLogger.LogInfo($"Selected mechanism set to null.");
            return;
        }
        // Set the mechanism name.
        selectedMechanism = new PlutoMechanism(name: name, side: trainingSide);
        // Set the day and session trial numbers.
        trialNumberDay = selectedMechanism.trialNumberDay;
        trialNumberSession = selectedMechanism.trialNumberSession;
        AppLogger.LogInfo($"Selected mechanism '{selectedMechanism.name}'.");
        AppLogger.SetCurrentMechanism(selectedMechanism.name);
        AppLogger.LogInfo($"Trial numbers for '{selectedMechanism.name}' updated. Day: {trialNumberDay}, Session: {trialNumberSession}.");
    }

    public string trainingSide => userData?.rightHand == true ? "RIGHT" : "LEFT";
    
    // Check training size.
    public bool IsTrainingSide(string side) => string.Equals(trainingSide, side, StringComparison.OrdinalIgnoreCase);
}
