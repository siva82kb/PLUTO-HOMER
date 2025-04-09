
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
 * Implements all the functions for running game trials.
 */
public partial class AppData
{
    // Start a new trial.
    public void StartNewTrial()
    {
        trialStartTime = DateTime.Now;
        trialStopTime = null;
        trialNumberDay += 1;
        trialNumberSession += 1;
        
    }
}
