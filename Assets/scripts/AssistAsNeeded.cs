using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Globalization;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using System.Text;


public class PlutoAANController
{
    public float initialPosition { private set; get; }
    public float targetPosition { private set; get; }
    private float currentCtrlBound;
    public float previousCtrlBound { private set; get; }
    public int successRate { private set; get; }
    public float forgetFactor { private set; get; }
    public float assistFactor { private set; get; }
    public bool trialRunning { private set; get; }

    public PlutoAANController(float forget = 0.9f, float assist = 1.1f)
    {
        forgetFactor = forget;
        assistFactor = assist;
        initialPosition = 0;
        targetPosition = 0;
        currentCtrlBound = 0.16f;
        previousCtrlBound = 0.16f;
        successRate = 0;
        trialRunning = false;
    }

    public void setNewTrialDetails(float actPos, float tgtPos)
    {
        initialPosition = actPos;
        targetPosition = tgtPos;
        trialRunning = true;
    }

    public float getControlBoundForTrial()
    {
        return currentCtrlBound;
    }

    public sbyte getControlDirectionForTrial()
    {
        return (sbyte)Math.Sign(targetPosition - initialPosition);
    }

    public void upateTrialResult(bool success)
    {
        if (trialRunning == false) return;

        // Update success rate
        if (success)
        {
            if (successRate < 0)
            {
                successRate = 1;
            }
            else
            {
                successRate += 1;
            }
        } 
        else
        {
            if (successRate >= 0)
            {
                successRate = -1;
            }
            else
            {
                successRate -= 1;
            }
        }
        // Update control bound.
        previousCtrlBound = currentCtrlBound;
        if (successRate >= 3)
        {
            currentCtrlBound = forgetFactor * currentCtrlBound;
        }
        else if (successRate < 0)
        {
            currentCtrlBound = Math.Min(1.0f, assistFactor * currentCtrlBound);
        }
        // Trial done. No more update possible for this trial.
        trialRunning = false;
    }
}

public struct HOMERPlutoAANTarget
{
    public float initialPosition;
    public float targetPosition;
    public float initialTimr;
    public float reachDuration;

    public HOMERPlutoAANTarget(float initPos, float tgtPos, float initTime, float reachDur)
    {
        initialPosition = initPos;
        targetPosition = tgtPos;
        initialTimr = initTime;
        reachDuration = reachDur;
    }
}

public class HOMERPlutoAANController
{
    public enum TargetType
    {
        InAromFromArom,
        InAromFromProm,
        InPromFromArom,
        InPromFromPromCrossArom,
        InPromFromPromNoCrossArom,
        None
    }
    public enum HOMERPlutoAANState
    {
        None = 0,           // None state. The AAN is not engaged.
        NewTrialTargetSet,  // Target set but not started moving.
        AromMoving,         // Moving in the AROM.
        RelaxToArom,        // Relax control to reach nearest AROM edge.
        AssistToTarget,     // Assisting to reach target.
        Idle                // Idle state. The AAN is engaged but doing nothing.
    }

    public float initialPosition { private set; get; }
    public float targetPosition { private set; get; }
    public float maxDuration { private set; get; }
    public float currentCtrlBound { private set; get; }
    public float previousCtrlBound { private set; get; }
    public int successRate { private set; get; }
    public float forgetFactor { private set; get; }
    public float assistFactor { private set; get; }
    public float aromBoundary { private set; get; }
    public bool trialRunning { private set; get; }
    public float[] aRom { private set; get; }
    public float[] pRom { private set; get; }
    // Setter will automatically change the stateChange variable to true/false
    // depending on whether a new state value has been set.
    private HOMERPlutoAANState _state;
    public HOMERPlutoAANState state {
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
    private HOMERPlutoAANTarget newAanTarget;

    public HOMERPlutoAANController(float[] aRomValue, float[] pRomValue, float ctrlBound=0.16f, float forget = 0.9f, float assist = 1.1f, float bndry=0.8f)
    {
        forgetFactor = forget;
        assistFactor = assist;
        aromBoundary = bndry;
        initialPosition = 0;
        targetPosition = 0;
        maxDuration = 0;
        currentCtrlBound = ctrlBound;
        previousCtrlBound = ctrlBound;
        successRate = 0;
        trialRunning = false;
        aRom = aRomValue;
        pRom = pRomValue;
        state = HOMERPlutoAANState.None;
        positionQ = new Queue<float>();
        timeQ = new Queue<float>();
        trialTime = 0;
        newAanTarget = new HOMERPlutoAANTarget(0, 0, 0, 0);
    }

    public void Update(float actual, float delT, bool trialDone)
    {
        // Reset state change.
        stateChange = false;

        // Do nothing if the state is None.
        if (state == HOMERPlutoAANState.None) return;

        // Update trial time
        trialTime += delT;

        // Check if max duration is reached.
        bool _timeoutDone = (trialTime >= maxDuration) || trialDone;
        Debug.Log($"AAN Timeout: {_timeoutDone}, Trial Time: {trialTime}s, Max Duration: {maxDuration}, TrialDone: {trialDone}");

        // Update movement and time queues.
        UpdatePositionTimeQueues(actual, trialTime);

        // Act according to the state of the AAN.
        switch (state)
        {
            case HOMERPlutoAANState.NewTrialTargetSet:
                // Set the state of the AAN.
                switch (getTargetType())
                {
                    case HOMERPlutoAANController.TargetType.InAromFromArom:
                    case HOMERPlutoAANController.TargetType.InPromFromArom:
                        state = HOMERPlutoAANState.AromMoving;
                        break;
                    case HOMERPlutoAANController.TargetType.InAromFromProm:
                    case HOMERPlutoAANController.TargetType.InPromFromPromCrossArom:
                        state = HOMERPlutoAANState.RelaxToArom;
                        break;
                    case HOMERPlutoAANController.TargetType.InPromFromPromNoCrossArom:
                        state = HOMERPlutoAANState.AssistToTarget;
                        break;
                }
                break;
            case HOMERPlutoAANState.AromMoving:
                // Check if the target is reached.
                if (isTargetInArom()) return;
                // Check if the AROM boundary is reached.
                int _dir = Math.Sign(targetPosition - initialPosition);
                if ((_dir > 0 && actual >= aromBoundary * aRom[1]) || (_dir < 0 && actual <= aromBoundary * aRom[0]))
                {
                    state = HOMERPlutoAANState.AssistToTarget;
                }
                // Timeout or Done
                if (_timeoutDone)
                {
                    state = HOMERPlutoAANState.Idle;
                }
                break;
            case HOMERPlutoAANState.RelaxToArom:
                // Check if AROM has not been reached.
                if (!isActualInArom(actual))
                {
                    // Timeout or Done
                    if (_timeoutDone)
                    {
                        state = HOMERPlutoAANState.Idle;
                        return;
                    }
                    return;
                }
                // AROM reached.
                state = HOMERPlutoAANState.AromMoving;
                break;
            case HOMERPlutoAANState.AssistToTarget:
                // Timeout or Done
                if (_timeoutDone)
                {
                    state = HOMERPlutoAANState.Idle;
                    return;
                }
                break;
        }
    }

    public void resetTrial()
    {
        initialPosition = 0;
        targetPosition = 0;
        maxDuration = 0;
        trialRunning = false;
        state = HOMERPlutoAANState.None;
        // Empty the queues.
        positionQ.Clear();
        timeQ.Clear();
        trialTime = 0;
    }

    public void setNewTrialDetails(float actual, float target, float maxDur)
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
        state = HOMERPlutoAANState.NewTrialTargetSet;
    }

    public bool isActualInArom(float actual)
    {
        return (actual >= aRom[0] && actual <= aRom[1]);
    }

    public TargetType getTargetType()
    {
        bool _initInArom = (initialPosition >= aRom[0] && initialPosition <= aRom[1]);
        if (trialRunning == false) return TargetType.None;
        // Check if target is in aRom
        if (targetPosition >= aRom[0] && targetPosition <= aRom[1])
        {
            // Check if initial postiion is in aRom
            return  _initInArom ? TargetType.InAromFromArom : TargetType.InAromFromProm;
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

    public bool isTargetInArom()
    {
        if (trialRunning == false) return false;
        return (targetPosition >= aRom[0] && targetPosition <= aRom[1]);
    }

    public float getNearestAromEdge(float actual)
    {
        return Math.Abs(actual - aRom[0]) < Math.Abs(actual - aRom[1]) ? aRom[0] : aRom[1];
    }

    public bool isAromBoundaryReached(float actual)
    {
        // Check the direction of movement to the target.
        if (targetPosition >= actual)
        {
            return actual >= aRom[1];
        }
        return actual <= aRom[0];
    }
    public float getControlBoundForTrial()
    {
        return currentCtrlBound;
    }

    public sbyte getControlDirectionForTrial()
    {
        return (sbyte)Math.Sign(targetPosition - initialPosition);
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

    public void upateTrialResult(bool success)
    {
        if (trialRunning == false) return;

        // Update success rate
        if (success)
        {
            if (successRate < 0)
            {
                successRate = 1;
            }
            else
            {
                successRate += 1;
            }
        }
        else
        {
            if (successRate >= 0)
            {
                successRate = -1;
            }
            else
            {
                successRate -= 1;
            }
        }
        // Update control bound.
        previousCtrlBound = currentCtrlBound;
        if (successRate >= 3)
        {
            currentCtrlBound = forgetFactor * currentCtrlBound;
        }
        else if (successRate < 0)
        {
            currentCtrlBound = Math.Min(1.0f, assistFactor * currentCtrlBound);
        }
        // Trial done. No more update possible for this trial.
        trialRunning = false;
    }
}
