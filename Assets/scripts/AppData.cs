
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

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
    public const string COMPort = "COM21";


    // What is this used for?
    public string _dataLogDir = null;

    // Property with default fallback
    // public string userID = null;

    public string userID { get; private set; } = null;

    /*
     * USED AND THERAPY RELATED DATA.
     */
    public PlutoUserData userData;
    public MechanismSpeed speedData;
    public PlutoMechanism selectedMechanism { get; private set; }
    public string selectedGame { get; private set; } = null;

    /*
     * SESSION DETAILS
     */
    public int currentSessionNumber { get; set; }
    public DateTime startTime { get; private set; }
    public DateTime? stopTime { get; private set; }
    public DateTime trialStartTime { get; set; }
    public DateTime? trialStopTime { get; set; }

    public void SetStopTime() => stopTime = DateTime.Now;

    /*
     * Logging file names.
     */
    public string trialRawDataFile { get; private set; } = null;
    private StringBuilder rawDataString = null;
    private readonly object rawDataLock = new object();
    private StringBuilder aanExecDataString = null;

    /*
     * Game trial data
     */
    public List<float> previousSuccessRates = null;
    public float desiredSuccessRate { get; private set; }
    public float successRate { get; private set; } = 0f;
    public HomerTherapy.TrialType trialType;


    /*
     * AAN Data
     */
    public PlutoAANController aanController = null;
    private float _currControlBound;
    public float CurrentControlBound => _currControlBound;

    private AppData()
    {
    }

    public void Initialize(string scene, bool doNotResetMech = true)
    {
        UnityEngine.Debug.Log(Application.persistentDataPath);

        // Set sesstion start time.
        startTime = DateTime.Now;

        // Create file structure.
        DataManager.CreateFileStructure();

        // Start logging.
        string _dtstr = AppLogger.StartLogging(scene);

        // Connect and init robot.
        InitializeRobotConnection(doNotResetMech, _dtstr);

        // Intialize the PLUTO AAN logger.
        PlutoAanLogger.StartLogging(_dtstr);

        // Initialize the session manager.
        //SessionManager.Initialize(DataManager.sessionPath);
        //SessionManager.Instance.Login();

        // Initialize the user data.
        UnityEngine.Debug.Log(DataManager.configFile);
        UnityEngine.Debug.Log( DataManager.sessionFile);

        userData = new PlutoUserData(DataManager.configFile, DataManager.sessionFile);
        // Selected mechanism and game.
        selectedMechanism = null;
        selectedGame = null;

        // Get current session number.
        currentSessionNumber = userData.dTableSession.Rows.Count > 0 ? 
            Convert.ToInt32(userData.dTableSession.Rows[userData.dTableSession.Rows.Count - 1]["SessionNumber"]) + 1 : 1;        
        AppLogger.LogWarning($"Session number set to {currentSessionNumber}.");

        //set to upload the data to the AWS
       // awsManager.changeUploadStatus(awsManager.status[0]);
    }

    private void InitializeRobotConnection(bool doNotResetMech, string datetimestr = null)
    {
        // Initialize the PLUTO Comm logger.
        if (datetimestr != null)
        {
            PlutoComLogger.StartLogging(datetimestr);
        }
        
        if(!ConnectToRobot.isPLUTO) {
            ConnectToRobot.Connect(COMPort);
        }
        // Check if the connection is successful.
        if (!ConnectToRobot.isConnected)
        {
            AppLogger.LogError($"Failed to connect to PLUTO @ {COMPort}.");
            throw new Exception($"Failed to connect to PLUTO @ {COMPort}.");
        }
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
        PlutoComm.setDiagnosticMode();
        // PlutoComm.startSensorStream();
        AppLogger.LogInfo($"PLUTO SensorStream started.");
    }

    public void SetMechanism(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            selectedMechanism = null;
            aanController = null;
            AppLogger.LogInfo($"Selected mechanism set to null.");
            return;
        }
        // Set the mechanism name.
        selectedMechanism = new PlutoMechanism(name: name, side: trainingSide, sessno: currentSessionNumber);
        AppLogger.LogInfo($"Selected mechanism '{selectedMechanism.name}'.");
        AppLogger.SetCurrentMechanism(selectedMechanism.name);
        AppLogger.LogInfo($"Trial numbers for ' {selectedMechanism.name}' updated. Day: {selectedMechanism.trialNumberDay}, Session: {selectedMechanism.trialNumberSession}.");
    }

    public void setUser(string user){
        userID = user;
        UnityEngine.Debug.Log($" id : {userID}");
    }

    public void SetGame(string gameName)
    {
        selectedGame = gameName;
        previousSuccessRates =AppData.Instance.userData.GetLastTwoSuccessRates(selectedMechanism.name , selectedGame);
        // // Cannot set game before selecting mechanism.
        // if (selectedMechanism == null) 
        // {
        //     AppLogger.LogError($"Setting game before mechanism not possible.");
        //     throw new ArgumentNullException(nameof(selectedMechanism));
        // }

        // // Set game to null when gameName is empty or null.
        // if (string.IsNullOrEmpty(gameName))
        // {
        //     AppLogger.SetCurrentGame("");
        //     selectedGame = null;
        //     return;
        // }
        
        // // Set the game object appropriately.
        // switch (gameName)
        // {
        //     case "HAT":
        //         selectedGame = new HatTrickGame(selectedMechanism);
        //         break;
        //     default:
        //         AppLogger.LogError($"Unknow game selected '{gameName}'.");
        //         AppLogger.SetCurrentGame("");
        //         selectedGame = null;
        //         return;
        // }
        // Set selected game.
        AppLogger.LogInfo($"Selected game '{selectedGame}'.");
        AppLogger.SetCurrentGame(selectedGame);
    }

    public string trainingSide => userData?.rightHand == true ? "RIGHT" : "LEFT";
    
    // Check training size.
    public bool IsTrainingSide(string side) => string.Equals(trainingSide, side, StringComparison.OrdinalIgnoreCase);
}
