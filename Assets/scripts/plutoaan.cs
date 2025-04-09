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
using System.IO;
using System.Linq;

public class PlutoAANController
{
    public static readonly float MIN_AVG_SPEED = 10.0f;         // 10 deg per second is the minimum speed.
    public static readonly float MAX_AVG_SPEED = 20.0f;         // 20 deg per second is the maximum speed.
    public static readonly float MIN_REACH_TIME = 1.0f;         // Movement durations cannpt be shorter than 1 second.
    public static readonly float BOUNDARY = 0.9f;               // Boundary where assistnace is to be enabled.
    public static readonly float FORGETINGFACTOR = 0.9f;        // Forgetting factor for the control bound.
    public static readonly float ASSISTFACTOR = 0.01f;          // Assistance factor for the control bound.
    public static readonly float DEFAULTCONTROLBOUND = 0.5f;    // Default cotrol bound value.

    public static readonly string[] ADAPTFILEHEADER = new string[] {
        "SessionNumber", "TrialNumberSession", "TrialNumberDay", 
        "TargetPosition", "InitialPosition", 
        "Success", "SuccessRate", "DesiredSuccessRate", 
        "ControlBound", "ControlDir",
        "AanExecFileName"
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
        None = 0,           // None state. The AAN is not engaged.
        NewTrialTargetSet,  // Target set but not started moving.
        AromMoving,         // Moving in the AROM.
        RelaxToArom,        // Relax control to reach nearest AROM edge.
        AssistToTarget,     // Assisting to reach target.
        Idle                // Idle state. The AAN is engaged but doing nothing.
    }

    // Mechanism details
    private PlutoMechanism mechanism;
    public string mechanismName { 
        get => mechanism.name ;
    }

    public float initialPosition { private set; get; }
    public float targetPosition { private set; get; }
    public float maxDuration { private set; get; }
    public bool trialRunning { private set; get; }
    public float[] aRom => mechanism.CurrentArom;
    public float[] pRom => mechanism.CurrentProm;
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

    public float previousCtrlBound { private set; get; }
    public float currentCtrlBound { private set; get; }
    public int currentSuccessRate { private set; get; }
    public int desiredSuccessRate { private set; get; }

    // Logging variables
    private string _execFileName;
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
    
    public PlutoAANController(PlutoMechanism mechanism)
    {
        //forgetFactor = forget;
        //assistFactor = assist;
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
        state = PlutoAANState.None;
        positionQ = new Queue<float>();
        timeQ = new Queue<float>();
        trialTime = 0;
        _newAanTarget = new float[5];
        _newAanTarget[0] = 999; // Invalid target.
        
        // Adaptation related variables.
        ReadUpdateAdaptionParameters();
    }

    private void ReadUpdateAdaptionParameters()
    {
        // Check if the AAN adaptation file exists.
        if (!File.Exists(adaptFileName))
        {
            using (var writer = new StreamWriter(adaptFileName, false, System.Text.Encoding.UTF8))
            {
                // Preheader
                writer.WriteLine($":mechanism: {mechanismName}");
                // Header
                writer.WriteLine(string.Join(",", ADAPTFILEHEADER));
            }
        }
        // Read the adaptation file, and get the last controlbound value.
        DataTable adaptData = DataManager.loadCSV(adaptFileName);
        // Check the number of rows.
        if (adaptData.Rows.Count == 0)
        {
            // Default adaptation parameters.
            previousCtrlBound = DEFAULTCONTROLBOUND;
            currentCtrlBound = DEFAULTCONTROLBOUND;
        }
    }

    public void Update(float actual, float delT, bool trialDone)
    {
        // Reset state change.
        stateChange = false;

        // Do nothing if the state is None.
        if (state == PlutoAANState.None) return;

        // Update trial time
        trialTime += delT;

        // Check if max duration is reached.
        bool _timeoutDone = (trialTime >= maxDuration) || trialDone;

        // Update movement and time queues.
        UpdatePositionTimeQueues(actual, trialTime);

        // Act according to the state of the AAN.
        switch (state)
        {
            case PlutoAANState.NewTrialTargetSet:
                // Set the state of the AAN.
                switch (GetTargetType())
                {
                    case TargetType.InAromFromArom:
                    case TargetType.InPromFromArom:
                        state = PlutoAANState.AromMoving;
                        break;
                    case TargetType.InAromFromProm:
                    case TargetType.InPromFromPromCrossArom:
                        state = PlutoAANState.RelaxToArom;
                        // Generate target to relax to AROM.
                        GenerateRelaxToAromAanTarget(actual);
                        break;
                    case TargetType.InPromFromPromNoCrossArom:
                        state = PlutoAANState.AssistToTarget;
                        // Generate target to assist.
                        GenerateAssistToTargetAanTarget(actual);
                        break;
                }
                break;
            case PlutoAANState.AromMoving:
                // Check if the target is reached.
                if (IsTargetInArom()) return;
                // Check if the AROM boundary is reached.
                int _dir = Math.Sign(targetPosition - initialPosition);
                float _arompos = (actual - aRom[0]) / (aRom[1] - aRom[0]);
               // Debug.Log(_arompos);
                if ((_dir > 0 && _arompos >= BOUNDARY) || (_dir < 0 && _arompos <= (1 - BOUNDARY)))
                {
                    //Debug.Log("True");
                    state = PlutoAANState.AssistToTarget;
                    // Generate target to assist.
                    GenerateAssistToTargetAanTarget(actual);
                }
                // Timeout or Done
                if (_timeoutDone)
                {
                    state = PlutoAANState.Idle;
                }
                break;
            case PlutoAANState.RelaxToArom:
                // Check if AROM has not been reached.
                if (IsActualInArom(actual))
                {
                    // AROM reached.
                    state = PlutoAANState.AromMoving;
                    // Reset AAN target
                    _newAanTarget[0] = 999;
                    return;
                }
                // Timeout or Done
                if (_timeoutDone)
                {
                    state = PlutoAANState.Idle;
                    // Reset AAN target
                    _newAanTarget[0] = 999;
                }
                break;
            case PlutoAANState.AssistToTarget:
                // Timeout or Done
                if (_timeoutDone)
                {
                    state = PlutoAANState.Idle;
                    // Reset AAN target
                    _newAanTarget[0] = 999;
                    return;
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
        state = PlutoAANState.None;
        _newAanTarget[0] = 999;
        // Empty the queues.
        positionQ.Clear();
        timeQ.Clear();
        trialTime = 0;
    }

    public void SetNewTrialDetails(float actual, float target, float maxDur)
    {
        // Set the initial and target position for the trial.
        initialPosition = actual;
        targetPosition = target;
        maxDuration = maxDur;
        trialRunning = true;
        // Initialize the queues to keep track of the recent movement trajectory.
        positionQ.Enqueue(actual);
        timeQ.Enqueue(trialTime);
        stateChange = true;
        state = PlutoAANState.NewTrialTargetSet;
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

    //public sbyte getControlDirectionForTrial()
    //{
    //    return (sbyte)Math.Sign(targetPosition - initialPosition);
    //}

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
        _newAanTarget[4] = Math.Min(maxDuration, Math.Max(MIN_REACH_TIME, Math.Abs(_nearestAromEdge - actual) / MAX_AVG_SPEED));
    }

    private void GenerateAssistToTargetAanTarget(float actual)
    {
        // There is a valid target
        _newAanTarget[0] = 0;
        // Initial Position
        _newAanTarget[1] = actual;
        // Initial Time
        _newAanTarget[2] = 0;
        // Target Position
        _newAanTarget[3] = targetPosition;
        // Reach Duration
        float _maxAvgSpeed = Math.Max(MIN_AVG_SPEED, Math.Min(Math.Abs(actual - initialPosition) / trialTime, MAX_AVG_SPEED));
        _newAanTarget[4] = Math.Min(maxDuration, Math.Max(MIN_REACH_TIME, Math.Abs(targetPosition - actual) / _maxAvgSpeed));
    }

    //public void upateTrialResult(bool success)
    //{
    //    if (trialRunning == false) return;

    //    // Update success rate
    //    if (success)
    //    {
    //        if (successRate < 0)
    //        {
    //            successRate = 1;
    //        }
    //        else
    //        {
    //            successRate += 1;
    //        }
    //    }
    //    else
    //    {
    //        if (successRate >= 0)
    //        {
    //            successRate = -1;
    //        }
    //        else
    //        {
    //            successRate -= 1;
    //        }
    //    }
    //    // Update control bound.
    //    previousCtrlBound = currentCtrlBound;
    //    if (successRate >= 3)
    //    {
    //        currentCtrlBound = forgetFactor * currentCtrlBound;
    //    }
    //    else if (successRate < 0)
    //    {
    //        currentCtrlBound = Math.Min(1.0f, assistFactor * currentCtrlBound);
    //    }
    //    // Trial done. No more update possible for this trial.
    //    trialRunning = false;
    //}
}