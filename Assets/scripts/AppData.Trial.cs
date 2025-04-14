
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
        
        // Set current control bound.
        _currControlBound = trialType  == HomerTherapy.TrialType.SR85PCCATCH ? 0.0f : aanController.currentCtrlBound;
        
        // Set the trial data files.
        trialRawDataFile = DataManager.GetTrialRawDataFileName(
            currentSessionNumber,
            selectedMechanism.trialNumberDay,
            Instance.selectedGame,
            Instance.selectedMechanism.name);
        trialAanExecDataFile = DataManager.GetTrialAanExecDataFileName(
            currentSessionNumber,
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

        // Update the control bound if needed.
        if (trialType  != HomerTherapy.TrialType.SR85PCCATCH)
        {
            aanController.AdaptControLBound(desiredSuccessRate, successRate);
        }

        // Write trial information to the session details file.
        WriteTrialToSessionsFile();
        // Write trial details to the log file.
        float? _currcb = trialType == HomerTherapy.TrialType.SR85PCCATCH ? null : _currControlBound;
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
                $"Current CB: {_currcb.Value:F3}",
                $"Next CB: {aanController.currentCtrlBound:F3}",
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
            // "DesiredSuccessRate"
            $"{desiredSuccessRate:F3}",
            // "SuccessRate"
            $"{successRate:F3}",
            // "CurrentControlBound"
            trialType == HomerTherapy.TrialType.SR85PCCATCH ? null : $"{_currControlBound:F3}",
            // "NextControlBound"
            trialType == HomerTherapy.TrialType.SR85PCCATCH ? null : $"{aanController.currentCtrlBound:F3}"
        };

        // Write the trial row to the session file.
        using (StreamWriter sw = new StreamWriter(DataManager.sessionFile, true, Encoding.UTF8))
        {
            // Write the trial row to the session file.
            sw.WriteLine(string.Join(",", trialRow));
        }
    }
}
