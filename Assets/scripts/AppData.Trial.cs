
using System;
using System.IO;
using System.Linq;
using System.Text;
using Unity.VisualScripting;


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

        // Set the trial data files.
        trialRawDataFile = DataManager.GetTrialRawDataFileName(
            selectedMechanism.trialNumberSession,
            selectedMechanism.trialNumberDay,
            Instance.selectedGame,
            Instance.selectedMechanism.name);
        trialAanExecDataFile = DataManager.GetTrialAanExecDataFileName(
            selectedMechanism.trialNumberSession,
            selectedMechanism.trialNumberDay,
            Instance.selectedGame,
            Instance.selectedMechanism.name);

        // Write trial details to the log file.
        string _tdetails = string.Join(" | ",
            new string[] {
                $"Start Time: {trialStartTime:yyyy-MM-ddTHH:mm:ss}",
                $"Trial#Day: {selectedMechanism.trialNumberDay}",
                $"Trial#Sess: {selectedMechanism.trialNumberSession}",
                $"TrialType: ({(int)tSrType.tType}){tSrType.tType}",
                $"Desired SR: {tSrType.sRate}",
                $"Previous SR: {_prevControlBound}",
                $"Current CB: {_currControlBound}",
                $"TrialRawDataFile: {trialRawDataFile.Split('/').Last()}",
                $"TrialAanExecFile: {trialAanExecDataFile.Split('/').Last()}",
        });
        AppLogger.LogInfo($"<StartNewTrial> {_tdetails}");
    }

    public void StopTrial(int nTargets, int nSuccess, int nFailure)
    {
        trialStopTime = DateTime.Now;
        successRate = 100 * nSuccess / nTargets;
        // Write trial information to the session details file.
        WriteTrialToSessionsFile();
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
                $"Trial SR: {successRate}",
                $"Previous SR: {_prevControlBound}",
                $"Current CB: {_currControlBound}",
                $"TrialRawDataFile: {trialRawDataFile.Split('/').Last()}",
                $"TrialAanExecFile: {trialAanExecDataFile.Split('/').Last()}"
        });
        AppLogger.LogInfo($"<StopTrial> {_tdetails}");
    }

    private void WriteTrialToSessionsFile()
    {
        // Create folders for this session if they do not exist.
        DataManager.CreateSessinFolders(currentSessionNumber);

        // Build the trial row.
        string[] trialRow = new string[] {
            // "SessionNumber"
            $"{currentSessionNumber}",
            // "DateTime"
            startTime.ToString(DataManager.DATEFORMAT),
            // "TrialNumberDay"
            $"{selectedMechanism.trialNumberDay}",
            // "TrialNumberSession"
            $"{selectedMechanism.trialNumberSession}",
            // "TrialType"
            $"{trialType}",
            // "TrialStartTime"
            trialStartTime.ToString(DataManager.DATEFORMAT),
            // "TrialStopTime"
            trialStopTime?.ToString(DataManager.DATEFORMAT),
            // "TrialRawDataFile"
            trialRawDataFile.Split("/data/")[1],
            // "TrialAanExecFile", 
            trialAanExecDataFile.Split("/data/")[1],
            // "Mechanism"
            selectedMechanism.name, 
            // "GameName"
            selectedGame,
            // "GameParameter"
            null,
            // "GameSpeed"
            selectedMechanism.currSpeed.ToString(),
            // "AssistMode"
            trialType == HomerTherapy.TrialType.SR85PCCATCH ? "ACTIVE" : "AAN",
            // "AssistModeParameters"
            trialType == HomerTherapy.TrialType.SR85PCCATCH ? null : $"{_currControlBound:F3}", 
            // "DesiredSuccessRate"
            $"{desiredSuccessRate:F3}",
            // "SuccessRate"
            $"{successRate:F3}",
        };

        // Write the trial row to the session file.
        using (StreamWriter sw = new StreamWriter(DataManager.sessionFile, true, Encoding.UTF8))
        {
            // Write the trial row to the session file.
            sw.WriteLine(string.Join(",", trialRow));
        }
    }
}
