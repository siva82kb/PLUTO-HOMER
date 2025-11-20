/*
 * This file contails definitions of all classes required for implementing the 
 * PLUTO AAN Controller.
 *
 * Author: Sivakumar Balasubramanian
 * Date: 09 Apri 2025
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

public class PlutoAANController
{
    public static readonly float MIN_AVG_SPEED = 10.0f;         // 10 deg per second is the minimum speed.
    public static readonly float MAX_AVG_SPEED = 20.0f;         // 20 deg per second is the maximum speed.
    public static readonly float MIN_REACH_TIME = 1.0f;         // Movement durations cannot be shorter than 1 second.
    public static readonly float BOUNDARY = 0.9f;               // Boundary where assistance is to be enabled.
    public static readonly float FORGETINGFACTOR = 0.9f;        // Forgetting factor for the control bound.
    public static readonly float ASSISTFACTOR = 0.01f;          // Assistance factor for the control bound.
    public static readonly float DEFAULTCONTROLBOUND = 0.6f;    // Default control bound value.
    public static readonly float MAXCONTROLBOUND = 1f;          // Maximum control bound value.
    public static readonly float MINCONTROLBOUND = 0.16f;       // Minimum control bound value.
    public static float MAX_SPEED = 40.0f;
    public float MECH_SPEED = 0f;

    public static readonly string[] ADAPTFILEHEADER = new string[] {
        "SessionNumber", "TrialNumberSession", "TrialNumberDay",
        "SuccessRate", "DesiredSuccessRate",
        "ControlBound", "AanExecFileName"
    };

    public enum TargetType
    {
        InAromFromArom,
        InAromFromProm,
        InPromFromArom,
        InPromFromPromCrossArom,
        InPromFromPromNoCrossArom,
        None
    }

    public enum PlutoAANState
    {
        NONE = 0,                       // None state. The AAN is not engaged.
        NEWTRAILTARGETSET,              // Target set but not started moving.
        AROMMOVING,                     // Moving in the AROM.
        RELAXTOAROM,                    // Relax control to reach nearest AROM edge.
        ASSISTTOTARGETINBOUNDARY,       // Assisting to reach target.
        ASSISTTOTARGETATBOUNDARY,       // Assisting to reach target.
        IDLE                            // Idle state. The AAN is engaged but doing nothing.
    }

    // Mechanism details
    private PlutoMechanism mechanism;
    public string mechanismName
    {
        get => mechanism.name;
    }

    // AAN real-time execution related variables.
    public float initialPosition { private set; get; }
    public float targetPosition { private set; get; }
    public float maxDuration { private set; get; }
    public bool trialRunning { private set; get; }
    public float[] aRom => mechanism.CurrentArom;
    public float[] pRom => mechanism.CurrentProm;
    public float[] apRom => mechanism.CurrentAProm;
    // Setter will automatically change the stateChange variable to true/false
    // depending on whether a new state value has been set.
    private PlutoAANState _state;
    public PlutoAANState state
    {
        get => _state;
        private set
        {
            stateChange = _state != value;
            _state = value;
        }
    }
    public bool stateChange { private set; get; }
    public Queue<float> positionQ { private set; get; }
    public Queue<float> timeQ { private set; get; }
    public float trialTime { private set; get; }
    private float[] _newAanTarget;
    private float lastCheckedPosition = float.NaN;
    private bool checkVolMov = false;         // voluntry movement check
    private const float POS_TOLERANCE = 0.1f; 
    private Stopwatch positionStopwatch = new Stopwatch();
    private const int NO_MOVEMENT_THRESHOLD = 1500; // 1.5 seconds in ms

    // AAN control bound adaptation related variables.
    public float currentCtrlBound { private set; get; }

    // Logging variables
    private string _execFileName;
    private bool setARInitPos = false;
    private float activeRangeInitPos;
    private StreamWriter _execFileHandler = null;
    public string execFileName
    {
        get => _execFileName;
        private set
        {
            _execFileName = value;
            _execFileHandler?.Dispose();
            _execFileHandler = !string.IsNullOrEmpty(value)
                ? new StreamWriter(value, false, System.Text.Encoding.UTF8)
                : null;
        }
    }

    public string adaptFileName { private set; get; }

    public PlutoAANController(PlutoMechanism mechanism, DataTable sessionData, int sessionNo)
    {
        if (mechanism == null)
        {
            // Throw null exception.
            throw new ArgumentNullException();
        }
        // Initialize controller
        this.mechanism = mechanism;

        // Logging files
        execFileName = null;
        adaptFileName = DataManager.GetAanAdaptFileName(mechanismName);

        // Execution related variables
        initialPosition = 0;
        targetPosition = 0;
        maxDuration = 0;
        trialRunning = false;
        state = PlutoAANState.NONE;
        positionQ = new Queue<float>();
        timeQ = new Queue<float>();
        trialTime = 0;
        _newAanTarget = new float[5];
        _newAanTarget[0] = 999; // Invalid target.
        activeRangeInitPos = 0;
        setARInitPos = false;

        // Adaptation related variables.
        ReadUpdateAdaptionParameters(sessionData, sessionNo);
    }

    private void ReadUpdateAdaptionParameters(DataTable sessionData, int sessionNo)
    {
        // Get the rows for the current mechanism and session.
        var selRows = sessionData.AsEnumerable()?
            .Where(row => row.Field<string>("Mechanism") == mechanism.name)
            .OrderBy(row => Convert.ToInt32(row.Field<string>("SessionNumber")))
            .ThenBy(row => Convert.ToInt32(row.Field<string>("TrialNumberSession")));
        // Set default value if there are no rows.
        if (selRows.Count() == 0)
        {
            // Default adaptation parameters.
            currentCtrlBound = DEFAULTCONTROLBOUND;
        }
        else
        {
            // // Now order the selRows by the trailNumberDay in increasing order and get the last row.
            DataRow lastRow = selRows.LastOrDefault();

            //     string nextBoundStr = lastRow?.Field<string>("NextControlBound");
            //     if (string.IsNullOrWhiteSpace(nextBoundStr) || !float.TryParse(nextBoundStr, out currentCtrlBound))
            //     {
            //         currentCtrlBound = DEFAULTCONTROLBOUND;
            //     }

            // //currentCtrlBound = Convert.ToSingle(lastRow.Field<string>("NextControlBound"));

            float tempBound;
            string nextBoundStr = lastRow?.Field<string>("NextControlBound");

            if (string.IsNullOrWhiteSpace(nextBoundStr) || !float.TryParse(nextBoundStr, out tempBound))
            {
                currentCtrlBound = DEFAULTCONTROLBOUND;
            }
            else
            {
                currentCtrlBound = tempBound;
            }

        }
        PlutoAanLogger.LogInfo($"Currrent Control Bound: {currentCtrlBound}");
    }


private bool CheckNoMovement(float actual, float aromInitPos)
{
    float aromRange = aRom[1] - aRom[0];

    if (aromRange <= 0f) return false;

    float gateDistance = 0.25f * aromRange;   
    float movedFromInit = Math.Abs(actual - aromInitPos);

    if (!checkVolMov)
    {
        if (movedFromInit + POS_TOLERANCE >= gateDistance)
        {
            checkVolMov = true;              
            positionStopwatch.Reset();        
            lastCheckedPosition = actual;
        }
        else
        {

            positionStopwatch.Reset();
            lastCheckedPosition = actual;
            return false;
        }
    }

    if (float.IsNaN(lastCheckedPosition))
        lastCheckedPosition = actual;

    if (Math.Abs(actual - lastCheckedPosition) <= POS_TOLERANCE)
    {
        if (!positionStopwatch.IsRunning)
            positionStopwatch.Start();

        if (positionStopwatch.ElapsedMilliseconds >= NO_MOVEMENT_THRESHOLD)
        {
            state = PlutoAANState.ASSISTTOTARGETINBOUNDARY;
            GenerateAssistToTargetAanTarget(actual, true);
            UnityEngine.Debug.Log("Assist triggered");
            PlutoAanLogger.LogInfo(
                $"Assist due to no movement in active range for 2 sec | {state} | " +
                $"[{_newAanTarget[0]}, {_newAanTarget[1]}, {_newAanTarget[2]}, {_newAanTarget[3]}, {_newAanTarget[4]}]"
            );
            checkVolMov = false;
            positionStopwatch.Reset();
            return true;
        }
    }
    else
    {
        // if there was movement,reset stall timer and update 
        positionStopwatch.Reset();
        lastCheckedPosition = actual;
    }

    return false;
}



    public void Update(float actual, float delT, bool trialDone)
    {
        // Reset state change.
        stateChange = false;

        // UnityEngine.Debug.Log($"state : {state}");

        // Do nothing if the state is None.
        if (state == PlutoAANState.NONE) return;

        // Update trial time
        trialTime += delT;

        // Check if max duration is reached.
        // bool _timeoutDone = (trialTime >= maxDuration) || trialDone;

        // Update movement and time queues.
        UpdatePositionTimeQueues(actual, trialTime);

        // Act according to the state of the AAN.
        PlutoAANState _prevstate = state;
        switch (state)
        {
            case PlutoAANState.NEWTRAILTARGETSET:

                //temp add
                checkVolMov = false;
                lastCheckedPosition = float.NaN;
                positionStopwatch.Reset();


                // Set the state of the AAN.
                switch (GetTargetType())
                {
                    case TargetType.InAromFromArom:
                    case TargetType.InPromFromArom:
                        state = PlutoAANState.AROMMOVING;
                        PlutoAanLogger.LogInfo($"Update | {_prevstate} -> {state} | {GetTargetType()}");
                        break;
                    case TargetType.InAromFromProm:
                    case TargetType.InPromFromPromCrossArom:
                        state = PlutoAANState.RELAXTOAROM;
                        // Generate target to relax to AROM.
                        GenerateRelaxToAromAanTarget(actual);
                        PlutoAanLogger.LogInfo($"Update | {_prevstate} -> {state} | [{_newAanTarget[0]}, {_newAanTarget[1]}, {_newAanTarget[2]}, {_newAanTarget[3]}, {_newAanTarget[4]}]");
                        break;
                    case TargetType.InPromFromPromNoCrossArom:
                        state = PlutoAANState.ASSISTTOTARGETATBOUNDARY;
                        // Generate target to assist.
                        GenerateAssistToTargetAanTarget(actual, false);
                        PlutoAanLogger.LogInfo($"Update | {_prevstate} -> {state} | [{_newAanTarget[0]}, {_newAanTarget[1]}, {_newAanTarget[2]}, {_newAanTarget[3]}, {_newAanTarget[4]}]");
                        break;
                }
                break;
            case PlutoAANState.AROMMOVING:
                // UnityEngine.Debug.Log($"y state : {state}");

                // Check if the trial is done.
                if (trialDone)
                {
                    state = PlutoAANState.IDLE;
                    return;
                }
                if (!setARInitPos)
                {
                    activeRangeInitPos = actual;
                    checkVolMov = false;
                    lastCheckedPosition = float.NaN;
                    positionStopwatch.Reset();
                    setARInitPos = true;
                }


                if (CheckNoMovement(actual, activeRangeInitPos)) return;


                // Check if the AROM boundary is reached.
                int _dir = Math.Sign(targetPosition - initialPosition);
                float _arompos = (actual - aRom[0]) / (aRom[1] - aRom[0]);
                if ((_dir > 0 && _arompos >= BOUNDARY) || (_dir < 0 && _arompos <= (1 - BOUNDARY)))
                {
                    state = PlutoAANState.ASSISTTOTARGETATBOUNDARY;
                    // Generate target to assist.
                    GenerateAssistToTargetAanTarget(actual, true);
                    PlutoAanLogger.LogInfo($"Update | {_prevstate} -> {state} | [{_newAanTarget[0]}, {_newAanTarget[1]}, {_newAanTarget[2]}, {_newAanTarget[3]}, {_newAanTarget[4]}]");
                }
                break;
            case PlutoAANState.RELAXTOAROM:

                // Check if AROM has not been reached.
                //   if (CheckNoMovement(actual)) return;

                if (setARInitPos)
                {
                    checkVolMov = false;
                    positionStopwatch.Reset();
                    lastCheckedPosition = float.NaN;
                    setARInitPos = false;
                }

                if (IsActualInArom(actual))
                {
                    // AROM reached.
                    state = PlutoAANState.AROMMOVING;
                    // Reset AAN target
                    _newAanTarget[0] = 999;
                    PlutoAanLogger.LogInfo($"Update | {_prevstate} -> {state} | [{_newAanTarget[0]}, {_newAanTarget[1]}, {_newAanTarget[2]}, {_newAanTarget[3]}, {_newAanTarget[4]}]");
                    return;
                }
                break;
            case PlutoAANState.ASSISTTOTARGETATBOUNDARY:
                // Check if the trial is done.
                if (trialDone)
                {
                    // We need to relax to the AroM.
                    // Generate target to relax to AROM.
                    GenerateRelaxToAromAanTarget(actual);
                    state = PlutoAANState.RELAXTOAROM;
                    PlutoAanLogger.LogInfo($"Update | {_prevstate} -> {state} | [{_newAanTarget[0]}, {_newAanTarget[1]}, {_newAanTarget[2]}, {_newAanTarget[3]}, {_newAanTarget[4]}]");
                }
                break;
            case PlutoAANState.ASSISTTOTARGETINBOUNDARY:
                // Check if the trial is done.
                if (trialDone)
                {
                    GenerateRelaxToAromAanTarget(actual);
                    state = PlutoAANState.RELAXTOAROM;
                    PlutoAanLogger.LogInfo($"Update | {_prevstate} -> {state} | [{_newAanTarget[0]}, {_newAanTarget[1]}, {_newAanTarget[2]}, {_newAanTarget[3]}, {_newAanTarget[4]}]");     
                }
                break;
        }
    }

    public void ResetTrial()
    {
        initialPosition = 0;
        targetPosition = 0;
        maxDuration = 0;
        trialRunning = false;
        state = PlutoAANState.NONE;
        _newAanTarget[0] = 999;
        setARInitPos = false;
        activeRangeInitPos = 0;
        checkVolMov = false;
        positionStopwatch.Reset();
        lastCheckedPosition = float.NaN;
        // Empty the queues.
        positionQ.Clear();
        timeQ.Clear();
        trialTime = 0;
        PlutoAanLogger.LogInfo($"Reset | {state} | [{_newAanTarget[0]}, {_newAanTarget[1]}, {_newAanTarget[2]}, {_newAanTarget[3]}, {_newAanTarget[4]}]");
    }

    public void SetNewTrialDetails(float actual, float target, float maxDur, float mechSpeed)
    {
        // Set the initial and target position for the trial.
        initialPosition = actual;
        targetPosition = target;
        maxDuration = maxDur;
        trialRunning = true;
        MECH_SPEED = mechSpeed;
        // Initialize the queues to keep track of the recent movement trajectory.
        positionQ.Enqueue(actual);
        timeQ.Enqueue(trialTime);
        stateChange = true;
        state = PlutoAANState.NEWTRAILTARGETSET;
        PlutoAanLogger.LogInfo($"SetNewTrialDetails | {initialPosition} -> {targetPosition} in {maxDuration}");
    }

    public float[] GetNewAanTarget()
    {
        return _newAanTarget[0] == 999 ? null : _newAanTarget.Skip(1).ToArray();
    }

    public bool IsActualInArom(float actual)
    {
        return (actual >= aRom[0] && actual <= aRom[1]);
    }

    public TargetType GetTargetType()
    {
        // UnityEngine.Debug.Log($"arom min : {aRom[0]}, max :{aRom[1]}");
        bool _initInArom = (initialPosition >= aRom[0] && initialPosition <= aRom[1]);
        if (trialRunning == false) return TargetType.None;
        // Check if target is in aRom
        if (targetPosition >= aRom[0] && targetPosition <= aRom[1])
        {
            // Check if initial postiion is in aRom
            return _initInArom ? TargetType.InAromFromArom : TargetType.InAromFromProm;
        }
        // Target in pRom
        // Check if initial position is in aRom
        if (_initInArom) return TargetType.InPromFromArom;
        // Initial position is in pRom. We need to check which side of aRom.
        if ((targetPosition < aRom[0] && initialPosition < aRom[0])
            || (targetPosition > aRom[1] && initialPosition > aRom[1]))
        {
            return TargetType.InPromFromPromNoCrossArom;
        }
        return TargetType.InPromFromPromCrossArom;
    }

    public bool IsTargetInArom()
    {
        if (trialRunning == false) return false;
        return (targetPosition >= aRom[0] && targetPosition <= aRom[1]);
    }

    public float GetNearestAromEdge(float actual)
    {
        return Math.Abs(actual - aRom[0]) < Math.Abs(actual - aRom[1]) ? aRom[0] : aRom[1];
    }

    public bool IsAromBoundaryReached(float actual)
    {
        // Check the direction of movement to the target.
        if (targetPosition >= actual)
        {
            return actual >= aRom[1];
        }
        return actual <= aRom[0];
    }

    public void AdaptControLBound(float desiredSuccessRate, float previousSuccessRate)
    {
        string _logstr = $"AdaptControlBound | {currentCtrlBound}";
        // First do some forgetting
        currentCtrlBound *= FORGETINGFACTOR;
        // Now, do some learning or error correction.
        currentCtrlBound += ASSISTFACTOR * (desiredSuccessRate - previousSuccessRate);
        // Limit the control bound to 0.0 and 1.0.
        currentCtrlBound = Math.Max(MINCONTROLBOUND, Math.Min(MAXCONTROLBOUND, currentCtrlBound));
        PlutoAanLogger.LogInfo($"{_logstr} -> {currentCtrlBound} | {desiredSuccessRate} | {previousSuccessRate}");
    }

    private void UpdatePositionTimeQueues(float actPos, float tTime)
    {
        // Check if there is already data for the last 1 second.
        if (tTime - timeQ.Peek() >= 1.0)
        {
            positionQ.Dequeue();
            timeQ.Dequeue();
        }
        // Update the position queue.
        positionQ.Enqueue(actPos);
        timeQ.Enqueue(tTime);
    }
     private void GenerateRelaxToAromAanTarget(float actual)
    {
        // Find the nearest AROM edge.
        float _nearestAromEdge = GetNearestAromEdge(actual);
        // There is valid target
        _newAanTarget[0] = 0;
        // Initial Position
        _newAanTarget[1] = actual;
        // Initial Time
        _newAanTarget[2] = 0;
        // Target Position
        _newAanTarget[3] = _nearestAromEdge;
        // Reach Duration
        _newAanTarget[4] = Math.Min(maxDuration, Math.Max(MIN_REACH_TIME, Math.Abs(_nearestAromEdge - actual) / MECH_SPEED));
    }

    private void GenerateAssistToTargetAanTarget(float actual, bool fromArom)
    {
        // Reach Duration
        float _maxAvgSpeed =Math.Min(MAX_SPEED ,Math.Max(MIN_AVG_SPEED, Math.Min(Math.Abs(actual - initialPosition) / trialTime, MECH_SPEED)));
        float _maxDur = Math.Min(maxDuration, Math.Max(MIN_REACH_TIME, Math.Abs(targetPosition - actual) / _maxAvgSpeed));
        // There is a valid target
        _newAanTarget[0] = 0;
        // Initial Position
        _newAanTarget[1] = actual;
        // Initial Time
        _newAanTarget[2] = fromArom ? - 0.25f * _maxDur : 0;
        // Target Position
        _newAanTarget[3] = targetPosition;
        // Target Time
        _newAanTarget[4] = _maxDur;
    }
}


public static class PlutoAanLogger
{
    private static string logFilePath;
    private static StreamWriter logWriter = null;
    private static readonly object logLock = new object();

    public static bool DEBUG = false;
    public static string InBraces(string text) => $"[{text}]";

    public static bool isLogging
    {
        get
        {
            return logFilePath != null;
        }
    }

    public static void StartLogging(string dtstr)
    {
        // Start Log file only if we are not already logging.
        if (isLogging) return;
        if (!Directory.Exists(DataManager.logPath)) Directory.CreateDirectory(DataManager.logPath);
        // Create the log file name.
        logFilePath = Path.Combine(DataManager.logPath, $"{dtstr}-plutoaan.log");

        // Create the log file writer.
        logWriter = new StreamWriter(logFilePath, true);
        LogInfo("Created PLUTO AAN log file.");
    }

    public static void StopLogging()
    {
        if (logWriter != null)
        {
            LogInfo("Closing PLUTO AAN log file.");
            logWriter.Close();
            logWriter = null;
            logFilePath = null;
        }
    }

    public static void LogMessage(string message, LogMessageType logMsgType)
    {
        lock (logLock)
        {
            if (logWriter != null)
            {
                string _user = AppData.Instance.userData != null ? AppData.Instance.userData.hospNumber : "";
                string _trialno = AppData.Instance.selectedMechanism != null ? AppData.Instance.selectedMechanism.trialNumberDay.ToString() : "";
                string _msg = $"{DateTime.Now:dd-MM-yyyy HH:mm:ss} {logMsgType,-7} {InBraces(_user), -10} {InBraces(AppLogger.currentScene), -12} {InBraces(AppLogger.currentMechanism), -8} {InBraces(AppLogger.currentGame), -8} {InBraces(_trialno), -4} >> {message}";
                logWriter.WriteLine(_msg);
                logWriter.Flush();
                if (DEBUG) UnityEngine.Debug.Log(_msg);
            }
        }
    }

    public static void LogInfo(string message)
    {
        LogMessage(message, LogMessageType.INFO);
    }

    public static void LogWarning(string message)
    {
        LogMessage(message, LogMessageType.WARNING);
    }

    public static void LogError(string message)
    {
        LogMessage(message, LogMessageType.ERROR);
    }
}
