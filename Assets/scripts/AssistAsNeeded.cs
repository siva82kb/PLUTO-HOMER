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
    public float initialPosition { private set; get; }
    public float targetPosition { private set; get; }
    private float currentCtrlBound;
    public float previousCtrlBound { private set; get; }
    public int successRate { private set; get; }
    public float forgetFactor { private set; get; }
    public float assistFactor { private set; get; }
    public bool trialRunning { private set; get; }
    public float[] aRom { private set; get; }
    public float[] pRom { private set; get; }

    public HOMERPlutoAANController(float[] aRomValue, float[] pRomValue, float forget = 0.9f, float assist = 1.1f)
    {
        forgetFactor = forget;
        assistFactor = assist;
        initialPosition = 0;
        targetPosition = 0;
        currentCtrlBound = 0.16f;
        previousCtrlBound = 0.16f;
        successRate = 0;
        trialRunning = false;
        aRom = aRomValue;
        pRom = pRomValue;
    }

    public void resetTrial()
    {
        initialPosition = 0;
        targetPosition = 0;
        trialRunning = false;
    }

    public void setNewTrialDetails(float actPos, float tgtPos)
    {
        initialPosition = actPos;
        targetPosition = tgtPos;
        trialRunning = true;
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

    public bool requiresAromCrossing(float actual)
    {
        int _moveDir = Math.Sign(targetPosition - initialPosition);
        // Actual position is below aRom[0] and target is on the other side.
        if (actual < aRom[0] && targetPosition > aRom[1]) return true;
        if (actual > aRom[1] && targetPosition < aRom[0]) return true;
    }

    public float getNearestAromEdge(float actual)
    {
        return Math.Abs(actual - aRom[0]) < Math.Abs(actual - aRom[1]) ? aRom[0] : aRom[1];
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
