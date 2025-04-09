
using System;


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
