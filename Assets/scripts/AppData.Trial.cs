
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
        selectedMechanism.NextTrail();
        
        // Desired success rate and trial type for the game.
        var tSrType = HomerTherapy.GetTrailTypeAndSuccessRate(selectedMechanism.trialNumberDay);
        desiredSuccessRate = tSrType.sRate;
        trialType = tSrType.tType;
        
        // Compute AAN control bound.
        if (selectedMechanism.trialNumberDay ==  1) 
        {
            _prevControlBound = aanController.currentCtrlBound;
            _currControlBound = aanController.currentCtrlBound;
        } 
        else 
        {
            // Compute the control bound based on the success rate, depending 
            // on the trial type.
            _prevControlBound = _currControlBound;
            if (tSrType.tType  == HomerTherapy.TrialType.SR85PCCATCH)
            {
                _currControlBound = 0.0f;
            } 
            else
            {
                aanController.AdaptControLBound(desiredSuccessRate, _prevSuccessRate);
                _currControlBound = aanController.currentCtrlBound;
            }
        }

        // Write trial details to the log file.
        string _tdetails = string.Join(" | ",
            new string[] {
                $"Start Time: {trialStartTime:yyyy-MM-ddTHH:mm:ss}",
                $"Trial#Day: {selectedMechanism.trialNumberDay}",
                $"Trial#Sess: {selectedMechanism.trialNumberSession}",
                $"TrialType: ({(int)tSrType.tType}){tSrType.tType}",
                $"Desired SR: {tSrType.sRate}",
                $"Previous SR: {_prevControlBound}",
                $"Current CB: {_currControlBound}"
        });
        AppLogger.LogInfo($"<StartNewTrial> {_tdetails}");
    }

    public void StopTrial(int nTargets, int nSuccess, int nFailure)
    {
        trialStopTime = DateTime.Now;
        // Write trial details to the log file.
        string _tdetails = string.Join(" | ",
            new string[] {
                $"Start Time: {trialStartTime:yyyy-MM-ddTHH:mm:ss}",
                $"Stop Time: {trialStopTime:yyyy-MM-ddTHH:mm:ss}",
                $"Trial#Day: {selectedMechanism.trialNumberDay}",
                $"Trial#Sess: {selectedMechanism.trialNumberSession}",
                $"TrialType: ({(int)trialType}){trialType}",
                $"NTargets: {nTargets}",
                $"NSuccess: {nSuccess}",
                $"NFailure: {nFailure}",
                $"Desired SR: {desiredSuccessRate}",
                $"Trial SR: {100f * nSuccess / nTargets}",
                $"Previous SR: {_prevControlBound}",
                $"Current CB: {_currControlBound}"
        });
        AppLogger.LogInfo($"<StopTrial> {_tdetails}");
    }
}
