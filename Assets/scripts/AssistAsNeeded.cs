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
