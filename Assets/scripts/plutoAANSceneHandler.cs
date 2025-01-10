using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditorInternal;
using static UnityEditor.LightingExplorerTableColumn;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using System.Runtime.CompilerServices;
using XCharts;
using XCharts.Runtime;
using UnityEditor.Compilation;

public class Pluto_AAN_SceneHandler : MonoBehaviour
{
    public TextMeshProUGUI textDataDisplay;
    public TextMeshProUGUI textTrialDetails;
    public TextMeshProUGUI textCBAdaptDetailsDisplay;

    // Target and actual circles
    public GameObject targetCircle;
    public GameObject actualCircle;

    // Start/Stop Demo button
    public UnityEngine.UI.Button btnStartStop;

    // Pluto Diagnostics Button
    public UnityEngine.UI.Button btnDiagnsotics;

    // Data logging
    public UnityEngine.UI.Toggle tglDataLog;

    // Control variables
    private bool isRunning = false;
    //private float controlTarget = 0.0f;
    //private float controlBound = 0.0f;
    private const float tgtDuration = 3.0f;
    private float _currentTime = 0;
    private float _initialTarget = 0;
    private float _finalTarget = 0;
    //private bool _changingTarget = false;

    // Discrete movements related variables
    private uint trialNo = 0;
    // Define variables for a discrete movement state machine
    // Enumerated variable for states
    private enum DiscreteMovementTrialState
    {
        Rest,           // Resting state
        SetTarget,      // Set the target
        Moving,         // Start Movement.
        Success,        // Successfull reach
        Failure,        // Failed reach
    }
    private DiscreteMovementTrialState _trialState;
    private static readonly IReadOnlyList<float> stateDurations = Array.AsReadOnly(new float[] {
        2.50f,          // Rest duration
        0.25f,          // Target set duration
        5.00f,          // Maximum movement duration
        0.25f,          // Successful reach
        0.25f,          // Failed reach
    });
    private const float tgtHoldDuration = 1f;
    private float _trialTarget = 0f;
    private float _currTgtForDisplay;
    private float trialDuration = 0f;
    private float stateStartTime =   0f;
    private float _tempIntraStateTimer = 0f;

    // Control bound adaptation variables
    private float prevControlBound = 0.16f;
    // Magical minimum value where the mechanisms mostly move without too much instability.
    private float currControlBound = 0.16f;
    private const float cbChangeDuration = 2.0f;
    private sbyte currControlDir = 0;
    private float _currCBforDisplay;
    //private int successRate;

    // AAN class
    private PlutoAANController aanCtrler;

    // Target Display Scaling
    private const float xmax = 12f;

    // Logging related variables
    // Variable to indicate if logging is to be started from the start of the next trial,
    // if the demo is already running.
    //private bool readyToLog = false;
    //private bool isLogging = false;
    private string fileNamePrefix = null;
    private string logRawFileName = null;
    private StreamWriter logRawFile = null;
    private string logAdaptFileName = null;
    private StreamWriter logAdaptFile = null;
    private string _dataLogDir = "Assets\\data\\aan_demo\\";

    // Start is called before the first frame update
    void Start()
    {
        // Ensure the application continues running even when in the background
        Application.runInBackground = true;
        
        // Initialize UI
        InitializeUI();
        // Attach callbacks
        AttachControlCallbacks();
        // Connect to the robot.
        ConnectToRobot.Connect(AppData.COMPort);
        // Get device version.
        PlutoComm.getVersion();
        // First make sure the robot is not in any control mode
        // and set it in the diagnostics mode.
        PlutoComm.setControlType("NONE");
        PlutoComm.setDiagnosticMode();
        // Update the UI when starting
        UpdateUI();
        // Create the data directory if needed.
        CreateDirectoryIfNeeded(_dataLogDir);
    }

    // Update is called once per frame
    void Update()
    {
        // PLUTO heartbeat.
        PlutoComm.sendHeartbeat();

        UpdateUI();

        // Update trial detials
        UpdateTrialDetailsDisplay();

        // Update CB adapt details
        UpdateCBAdaptDetailsDisplay();

        // Check if the demo is running.
        if (isRunning == false) return;

        // Update trial time
        trialDuration += Time.deltaTime;

        // Run trial state machine
        RunTrialStateMachine();
    }

    void FixedUpdate()
    {
        if (PlutoComm.CALIBANGLE[PlutoComm.mechanism] != 0)
        {
            // Update actual position
            actualCircle.transform.position = new Vector3(
                (2 * PlutoComm.angle / PlutoComm.CALIBANGLE[PlutoComm.mechanism] - 1) * xmax,
                actualCircle.transform.position.y,
                actualCircle.transform.position.z
            );
        }
    }

    private void RunTrialStateMachine()
    {
        float _deltime = trialDuration - stateStartTime;
        bool _statetimeout = _deltime >= stateDurations[(int)_trialState];
        // Time when target is reached.
        bool _tgtreached = Math.Abs(_trialTarget - PlutoComm.angle) <= 5.0f;
        switch (_trialState)
        {
            case DiscreteMovementTrialState.Rest:
                // Check if the rest time has run out.
                if (_statetimeout)
                {
                    SetTrialState(DiscreteMovementTrialState.SetTarget);
                }
                break;
            case DiscreteMovementTrialState.SetTarget:
                if (_statetimeout)
                {
                    SetTrialState(DiscreteMovementTrialState.Moving);
                }
                break;
            case DiscreteMovementTrialState.Moving:
                // Update control bound smoothly.
                UpdateControlBoundSmoothly();
                // Update the position control target smoothly.
                UpdatePositionTargetSmoothly();
                // Check if the target has been reached
                if (_tgtreached)
                {
                    _tempIntraStateTimer += Time.deltaTime;
                }
                else
                {
                    _tempIntraStateTimer = 0;
                }
                // Check if target time has been reached.
                if (_tempIntraStateTimer >= tgtHoldDuration)
                {
                    SetTrialState(DiscreteMovementTrialState.Success);
                }
                else if (_statetimeout)
                {
                    SetTrialState(DiscreteMovementTrialState.Failure);
                }
                break;
            case DiscreteMovementTrialState.Success:
            case DiscreteMovementTrialState.Failure:
                if (_statetimeout) SetTrialState(DiscreteMovementTrialState.Rest);
                break;
        }
    }

    private void SetTrialState(DiscreteMovementTrialState newState)
    {
        _trialState = newState;
        switch (newState)
        {
            case DiscreteMovementTrialState.Rest:
                trialDuration = 0f;
                prevControlBound = PlutoComm.controlBound;
                currControlBound = aanCtrler.getControlBoundForTrial();
                trialNo += 1; 
                UpdateCBAdaptDetailsDisplay();
                // Break if logging is not selected.
                if (tglDataLog.isOn == false) break;
                // Log data.
                UpdateLogFiles();
                // Reset target timer (for display purposes).
                _tempIntraStateTimer = 0f;
                break;
            case DiscreteMovementTrialState.SetTarget:
                // Random select target from the appropriate range.
                float _tgtscale = UnityEngine.Random.Range(0.0f, 1.0f);
                _trialTarget = _tgtscale * PlutoComm.CALIBANGLE[PlutoComm.mechanism];
                // Change target location.
                targetCircle.transform.position = new Vector3(
                    (2 * _tgtscale - 1) * xmax,
                    targetCircle.transform.position.y,
                    targetCircle.transform.position.z
                );
                break;
            case DiscreteMovementTrialState.Moving:
                // Start the position control to the tatget location.
                _initialTarget = PlutoComm.angle;
                _finalTarget = _trialTarget;
                // Set new trial target.
                aanCtrler.setNewTrialDetails(_initialTarget, _finalTarget);
                // Set control direction
                PlutoComm.setControlDir(aanCtrler.getControlDirectionForTrial());
                _tempIntraStateTimer = 0f;
                break;
            case DiscreteMovementTrialState.Success:
                // Update trial result.
                aanCtrler.upateTrialResult(true);
                // Update adaptation row.
                WriteTrialRowInfo(1);
                break;
            case DiscreteMovementTrialState.Failure:
                aanCtrler.upateTrialResult(false);
                WriteTrialRowInfo(0);
                break;
        }
        stateStartTime = trialDuration;
    }

    public void AttachControlCallbacks()
    {
        // Toggle button
        //tglCalibSelect.onValueChanged.AddListener(delegate { OnCalibrationChange(); });
        //tglControlSelect.onValueChanged.AddListener (delegate { OnControlChange(); });
        tglDataLog.onValueChanged.AddListener(delegate { OnDataLogChange(); });

        // Button click.
        btnStartStop.onClick.AddListener(delegate { OnStartStopDemo(); });

        // PLUTO Diagnostics Button click.
        btnDiagnsotics.onClick.AddListener(() => SceneManager.LoadScene(0));

        // Listen to PLUTO's event
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        //PlutoComm.OnControlModeChange += onPlutoControlModeChange;
        PlutoComm.OnNewPlutoData += onNewPlutoData;
    }

    private void UpdateControlBoundSmoothly()
    {
        if ((prevControlBound == currControlBound) ||
            ((trialDuration - stateStartTime) >= cbChangeDuration))
        {
            return;
        }
        // Implement the minimum jerk trajectory.
        float _t = (trialDuration - stateStartTime) / cbChangeDuration;
        // Limit _t between 0 and 1.
        _t = Mathf.Clamp(_t, 0, 1);
        // Compute the CB value using the minimum jerk trajectory.
        _currCBforDisplay = prevControlBound + (currControlBound - prevControlBound) * (10 * Mathf.Pow(_t, 3) - 15 * Mathf.Pow(_t, 4) + 6 * Mathf.Pow(_t, 5));
        // Update control bound.
        PlutoComm.setControlBound(_currCBforDisplay);
    }

    private void UpdatePositionTargetSmoothly()
    {
        float _t = (trialDuration- stateStartTime) / tgtDuration;
        // Limit _t between 0 and 1.
        _t = Mathf.Clamp(_t, 0, 1);
        // Compute the current target value using the minimum jerk trajectory.
        _currTgtForDisplay = _initialTarget + (_finalTarget - _initialTarget) * (10 * Mathf.Pow(_t, 3) - 15 * Mathf.Pow(_t, 4) + 6 * Mathf.Pow(_t, 5));
        // Update position target
        PlutoComm.setControlTarget(_currTgtForDisplay);
    }

    private void onNewPlutoData()
    {
        // Log data if needed. Else move on.
        if (logRawFile == null) return;

        // Log data
        String[] rowcomps = new string[]
        {
            $"{PlutoComm.runTime}",
            $"{PlutoComm.packetNumber}",
            $"{PlutoComm.status}",
            $"{PlutoComm.dataType}",
            $"{PlutoComm.errorStatus}",
            $"{PlutoComm.controlType}",
            $"{PlutoComm.calibration}",
            $"{PlutoComm.mechanism}",
            $"{PlutoComm.button}",
            $"{PlutoComm.angle}",
            $"{PlutoComm.torque}",
            $"{PlutoComm.control}",
            $"{PlutoComm.controlBound}",
            $"{PlutoComm.controlDir}",
            $"{PlutoComm.target}",
            $"{PlutoComm.err}",
            $"{PlutoComm.errDiff}",
            $"{PlutoComm.errSum}"
        };
        if (logRawFile != null)
        { 
            logRawFile.WriteLine(String.Join(", ", rowcomps));
        }
    }

    private void OnStartStopDemo()
    {
        if (isRunning)
        {
            btnStartStop.GetComponentInChildren<TMP_Text>().text = "Start Demo";
            isRunning = false;
            // Stop control.
            PlutoComm.setControlType("NONE");
        }
        else
        {
            // Pluto AAN controller
            aanCtrler = new PlutoAANController();
            // Change button text
            btnStartStop.GetComponentInChildren<TMP_Text>().text = "Stop Demo";
            isRunning = true;
            // Set Control mode.
            PlutoComm.setControlType("POSITIONAAN");
            PlutoComm.setControlBound(currControlBound);
            PlutoComm.setControlDir(0);
            trialNo = 0;
            //successRate = 0;
            // Start the state machine.
            SetTrialState(DiscreteMovementTrialState.Rest);
        }
    }

    private (float currTgtValue, bool isTgtChanging) computeCurrentTarget()
    {
        float _t = (Time.time - _currentTime) / tgtDuration;
        // Limit _t between 0 and 1.
        _t = Mathf.Clamp(_t, 0, 1);
        // Compute the current target value using the minimum jerk trajectory.
        float _currtgt = _initialTarget + (_finalTarget - _initialTarget) * (10 * Mathf.Pow(_t, 3) - 15 * Mathf.Pow(_t, 4) + 6 * Mathf.Pow(_t, 5));
        // return if the final target has been reached.
        return (_currtgt, _t < 1);
    }

    private void OnDataLogChange()
    {
        // Close file.
        CloseRawLogFile();
        CloseAdaptLogFile();
        logRawFile = null;
        logAdaptFile = null;
        fileNamePrefix = null;
    }

    private void UpdateLogFiles()
    {
        if (fileNamePrefix == null)
        {
            fileNamePrefix = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        }
        CreateDirectoryIfNeeded(_dataLogDir + fileNamePrefix + "\\");
        // Create the adaptation log file.
        if (logAdaptFile == null)
        {
            CreateAdaptLogFile();
        }
        // Create the raw log file after closing the current file.
        CloseRawLogFile();
        CreateRawLogFile();
    }

    private void CreateRawLogFile()
    {
        // Set the file name.
        logRawFileName = $"rawlogfile_{trialNo:D3}.csv";
        logRawFile = new StreamWriter(_dataLogDir + fileNamePrefix + "\\" + logRawFileName, false);
        // Write the header row.
        logRawFile.WriteLine($"DeviceID = {PlutoComm.deviceId}");
        logRawFile.WriteLine($"FirmwareVersion = {PlutoComm.version}");
        logRawFile.WriteLine($"CompileDate = {PlutoComm.compileDate}");
        logRawFile.WriteLine($"Actuated = {PlutoComm.actuated}");
        logRawFile.WriteLine($"Start Datetime = {DateTime.Now:yyyy/MM/dd HH-mm-ss.ffffff}");
        logRawFile.WriteLine("time, packetno, status, datatype, errorstatus, controltype, calibration, mechanism, button, angle, torque, control, controlbound, controldir, target, error, errordiff, errorsum");
    }

    private void CreateAdaptLogFile()
    {
        // Set the file name.
        logAdaptFileName = $"adaptlogfile.csv";
        logAdaptFile = new StreamWriter(_dataLogDir + fileNamePrefix + "\\" + logAdaptFileName, false);
        // Write the header row.
        logAdaptFile.WriteLine($"DeviceID = {PlutoComm.deviceId}");
        logAdaptFile.WriteLine($"FirmwareVersion = {PlutoComm.version}");
        logAdaptFile.WriteLine($"CompileDate = {PlutoComm.compileDate}");
        logAdaptFile.WriteLine($"Actuated = {PlutoComm.actuated}");
        logAdaptFile.WriteLine($"Start Datetime = {DateTime.Now:yyyy/MM/dd HH-mm-ss.ffffff}");
        logAdaptFile.WriteLine("trialno, targetposition, initialposition, success, successrate, controlbound, controldir, filename");
    }

    private void WriteTrialRowInfo(byte successfailure)
    {
        // Log data if needed. Else move on.
        if (logAdaptFile == null) return;

        // Log data
        String[] rowcomps = new string[]
        {
            $"{trialNo}",
            $"{aanCtrler.targetPosition}",
            $"{aanCtrler.initialPosition}",
            $"{successfailure}",
            $"{aanCtrler.successRate}",
            $"{aanCtrler.previousCtrlBound}",
            $"{currControlDir}",
            $"{logRawFileName}"
        };
        if (logAdaptFile != null)
        {
            logAdaptFile.WriteLine(String.Join(", ", rowcomps));
        }
    }

    private void CloseRawLogFile()
    {
        if (logRawFile != null)
        {
            // Close the file properly and create a new handle.
            logRawFile.Close();
        }
        logRawFileName = null;
        logRawFile = null;
    }

    private void CloseAdaptLogFile()
    {
        if (logAdaptFile != null)
        {
            // Close the file properly and create a new handle.
            logAdaptFile.Close();

            // Close any raw file that is open.
            CloseRawLogFile();

            // Clear filename prefix.
            fileNamePrefix = null;
        }
        logAdaptFileName = null;
        logAdaptFile = null;
    }

    private void onPlutoButtonReleased()
    {
    }

    private void InitializeUI()
    {
    }

    private void UpdateUI()
    {
        // Update data dispaly
        UpdateDataDispay();

        // Enable/Disable control panel.
        string _mech = PlutoComm.MECHANISMS[PlutoComm.mechanism];
        string _ctrlType = PlutoComm.CONTROLTYPE[PlutoComm.controlType];
    }

    private void UpdateDataDispay()
    {
        // Update the data display.
        string _dispstr = $"Time          : {PlutoComm.currentTime.ToString()}";
        _dispstr += $"\nDev ID        : {PlutoComm.deviceId}";
        _dispstr += $"\nF/W Version   : {PlutoComm.version}";
        _dispstr += $"\nCompile Date  : {PlutoComm.compileDate}";
        _dispstr += $"\n";
        _dispstr += $"\nPacket Number : {PlutoComm.packetNumber}";
        _dispstr += $"\nDev Run Time  : {PlutoComm.runTime:F2}";
        _dispstr += $"\nFrame Rate    : {PlutoComm.frameRate:F2}";
        _dispstr += $"\nStatus        : {PlutoComm.OUTDATATYPE[PlutoComm.dataType]}";
        _dispstr += $"\nMechanism     : {PlutoComm.MECHANISMS[PlutoComm.mechanism]}";
        _dispstr += $"\nCalibration   : {PlutoComm.CALIBRATION[PlutoComm.calibration]}";
        _dispstr += $"\nError         : {PlutoComm.errorString}";
        _dispstr += $"\nControl Type  : {PlutoComm.CONTROLTYPE[PlutoComm.controlType]}";
        _dispstr += $"\nActuated      : {PlutoComm.actuated}";
        _dispstr += $"\nButton State  : {PlutoComm.button}";
        _dispstr += "\n";
        _dispstr += $"\nAngle         : {PlutoComm.angle,6:F2} deg";
        if (PlutoComm.MECHANISMS[PlutoComm.mechanism] == "HOC")
        {
            _dispstr += $" [{PlutoComm.getHOCDisplay(PlutoComm.angle),6:F2} cm]";
        }
        _dispstr += $"\nTorque        : {0f,6:F2} Nm";
        _dispstr += $"\nControl       : {PlutoComm.control,6:F2}";
        _dispstr += $"\nCtrl Bnd (Dir): {PlutoComm.controlBound,6:F2} ({PlutoComm.controlDir})";
        _dispstr += $"\nTarget        : {PlutoComm.target,6:F2}";
        if (PlutoComm.OUTDATATYPE[PlutoComm.dataType] == "DIAGNOSTICS")
        {
            _dispstr += $"\nError         : {PlutoComm.err,6:F2}";
            _dispstr += $"\nError Diff    : {PlutoComm.errDiff,6:F2}";
            _dispstr += $"\nError Sum     : {PlutoComm.errSum,6:F2}";
        }
        textDataDisplay.SetText(_dispstr);
    }

    private void UpdateTrialDetailsDisplay()
    {
        // Update the trial related data.
        if (isRunning == false)
        {
            textTrialDetails.SetText("No trial running.");
            return;
        }
        string _dispstr = "Trial Details\n";
        _dispstr += "-------------\n";
        _dispstr += $"Duration         : {trialDuration:F2}s ({_tempIntraStateTimer:F2}s)";
        _dispstr += $"\nState            : {_trialState}";
        _dispstr += $"\nState Durtation  : {trialDuration - stateStartTime:F2}s";
        if (_trialState == DiscreteMovementTrialState.Rest)
        {
            _dispstr += $"\nTarget           : -";
        } else
        {
            _dispstr += $"\nTarget           : {aanCtrler.targetPosition:F2} [{_currTgtForDisplay:F2}]";
        }
        _dispstr += $"\nControl Bound    : {_currCBforDisplay:F2}";
        textTrialDetails.SetText(_dispstr);
    }

    private void UpdateCBAdaptDetailsDisplay()
    {
        // Update the trial related data.
        if (isRunning == false)
        {
            textCBAdaptDetailsDisplay.SetText("No trial running.");
            return;
        }
        string _dispstr = "Control Bound Adpatation Details\n";
        _dispstr += "--------------------------------\n";
        _dispstr += $"Trial No.           : {trialNo}\n";
        _dispstr += $"Success Rate        : {aanCtrler.successRate}\n";
        _dispstr += $"Current Ctrl Bound  : {currControlBound:F2}\n";
        _dispstr += $"Prev Ctrl Bound     : {prevControlBound:F2}\n";
        _dispstr += $"Adaptation Log File : {logAdaptFileName}\n";
        _dispstr += $"Raw Log File        : {logRawFileName}";
        textCBAdaptDetailsDisplay.SetText(_dispstr);
    }

    private void CreateDirectoryIfNeeded(string dirname)
    {
        // Ensure the directory exists
        string directoryPath = Path.GetDirectoryName(dirname);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    void OnSceneUnloaded(Scene scene)
    {
        Debug.Log("Unloading AAN scene.");
        ConnectToRobot.disconnect();
    }

    private void OnApplicationQuit()
    {
        ConnectToRobot.disconnect();
    }

    public void quitApplication()
    {
        Application.Quit();
    }
}