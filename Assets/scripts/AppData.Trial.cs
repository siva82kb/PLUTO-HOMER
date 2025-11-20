
using System;
using System.Diagnostics;
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
        StartRawAndAanExecDataLogging();

        // Write trial details to the log file.
        string _tdetails = string.Join(" | ",
            new string[] {
                $"Start Time: {trialStartTime:yyyy-MM-ddTHH:mm:ss}",
                $"Trial#Day: {selectedMechanism.trialNumberDay}",
                $"Trial#Sess: {selectedMechanism.trialNumberSession}",
                $"TrialType: ({(int)tSrType.tType}){tSrType.tType}",
                $"Desired SR: {tSrType.sRate}",
                $"Current CB: {_currControlBound}",
                $"TrialRawDataFile: {trialRawDataFile.Split('/').Last()}"
        });
        AppLogger.LogInfo($"StartNewTrial | {_tdetails}");
    }

    public void StopTrial(int nTargets, int nSuccess, int nFailure)
    {
        // Dettach the event handler for data logging.
        // PlutoComm.OnNewPlutoData -= OnNewPlutoDataDataLogging;

        trialStopTime = DateTime.Now;
        nTargets =(nTargets == 0)? 1 : nTargets;
        successRate = 100 * nSuccess / nTargets;
                // Update targets, hits and misses.
        selectedGame.UpdateTargetsHitsMisses(nTargets, nSuccess, nFailure);
        // Update the control bound if needed.
        if (trialType  != HomerTherapy.TrialType.SR85PCCATCH)
        {
            aanController.AdaptControLBound(desiredSuccessRate, successRate);
        }

        // Write trial information to the session details file.
        WriteTrialToSessionsFile();
        // Write trial details to the log file.
        float? _currcb = trialType == HomerTherapy.TrialType.SR85PCCATCH ? 0.0f : _currControlBound;

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
                $"Current CB: {_currcb?.ToString("F3")??"N/A"}",
                $"Next CB: {aanController.currentCtrlBound:F3}",
                $"TrialRawDataFile: {trialRawDataFile.Split('/').Last()}"
        });
        AppLogger.LogInfo($"StopTrial | {_tdetails}");
        // Stop Raw and AAN real-time data logging.
        WriteTrialDataToRawDataFile();
        PlutoComm.OnNewPlutoData -= OnNewPlutoDataDataLogging;
        trialRawDataFile = null;
        //set to upload the data to the AWS
        awsManager.changeUploadStatus(awsManager.status[0]);
    }

    private void WriteTrialToSessionsFile()
    {
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
            $"{trialRawDataFile.Split('/').Last()}",
            // "Mechanism"
            selectedMechanism.name, 
            // "GameName"
            selectedGameName,
            // "GameParameter"
            null,
            // "GameSpeed"
            speedData.gameSpeed.ToString(),
            // "AssistMode"
            trialType == HomerTherapy.TrialType.SR85PCCATCH ? "ACTIVE" : "AAN",
            // "DesiredSuccessRate"
            $"{desiredSuccessRate:F3}",
            // "SuccessRate"
            $"{successRate:F3}",
            // "CurrentControlBound"
            trialType == HomerTherapy.TrialType.SR85PCCATCH ? "0" : $"{_currControlBound:F3}",
            // "NextControlBound"
            trialType == HomerTherapy.TrialType.SR85PCCATCH ?  "0": $"{aanController.currentCtrlBound:F3}",
            //gameTime
            Others.gameTime.ToString(),
            $"{selectedGame.currentTargets}",                       // CurrentTargets
            $"{selectedGame.currentHits}",                          // CurrentHits
            $"{selectedGame.currentMisses}",                        // CurrentMisses
            $"{selectedGame.cummulativeTargets}",                   // CummulativeTargets
            $"{selectedGame.cummulativeHits}",                      // CummulativeHits
            $"{selectedGame.cummulativeMisses}", 
        };

        // Write the trial row to the session file.
        using (StreamWriter sw = new StreamWriter(DataManager.sessionFile, true, Encoding.UTF8))
        {
            // Write the trial row to the session file.
            sw.WriteLine(string.Join(",", trialRow));
        }
    }

    public void StartRawAndAanExecDataLogging()
    {
        // Set the file name.
        trialRawDataFile = DataManager.GetTrialRawDataFileName(
            currentSessionNumber,
            selectedMechanism.trialNumberDay,
            Instance.selectedGameName,
            Instance.selectedMechanism.name);

        // Initialize the string builders.
        rawDataString = new StringBuilder();
        // Write pre-header and header information
        rawDataString.AppendLine($":Device: PLUTO");
        rawDataString.AppendLine($":Location: {userData.GetDeviceLocation()}");
        rawDataString.AppendLine($":Mechanism: {selectedMechanism.name}");
        rawDataString.AppendLine($":Game: {selectedGameName}");
        rawDataString.AppendLine($":TrialType: {trialType}");
        rawDataString.AppendLine($":TrialStartTime: {trialStartTime:yyyy-MM-ddTHH:mm:ss}");
        rawDataString.AppendLine($":TrialNumberDay: {selectedMechanism.trialNumberDay}");
        rawDataString.AppendLine($":AROM: [{selectedMechanism.CurrentArom[0]:F3},{selectedMechanism.CurrentArom[1]:F3}]");        
        rawDataString.AppendLine($":PROM: [{selectedMechanism.CurrentProm[0]:F3},{selectedMechanism.CurrentProm[1]:F3}]");
        rawDataString.AppendLine($":APROM: [{selectedMechanism.CurrentAProm[0]:F3},{selectedMechanism.CurrentAProm[1]:F3}]");
        rawDataString.AppendLine($":DesiredSuccessRate: {desiredSuccessRate:F3}");
        rawDataString.AppendLine($":ControlBound: {_currControlBound:F3}");
        rawDataString.AppendLine(string.Join(",", DataManager.RAWFILEHEADER));

        // Attach the event handler for data logging.
        PlutoComm.OnNewPlutoData += OnNewPlutoDataDataLogging;
    }

    public void OnNewPlutoDataDataLogging()
    {
        lock (rawDataLock)
        {
            if (rawDataString == null)
            {
                UnityEngine.Debug.LogWarning("rawDataString is null, skipping logging.");
                return;
            }

            // Device data
            rawDataString.Append($"{PlutoComm.runTime:F6},");
            rawDataString.Append($"{PlutoComm.packetNumber},");
            rawDataString.Append($"{PlutoComm.status},");
            rawDataString.Append($"{PlutoComm.dataType},");
            rawDataString.Append($"{PlutoComm.errorStatus},");
            rawDataString.Append($"{PlutoComm.controlType},");
            rawDataString.Append($"{PlutoComm.calibration},");
            rawDataString.Append($"{PlutoComm.MECHANISMS[PlutoComm.mechanism]},");
            rawDataString.Append($"{PlutoComm.button},");
            rawDataString.Append($"{PlutoComm.angle},");
            rawDataString.Append($"{PlutoComm.torque},");
            rawDataString.Append($"{PlutoComm.desired},");
            rawDataString.Append($"{PlutoComm.control},");
            rawDataString.Append($"{PlutoComm.controlBound},");
            rawDataString.Append($"{PlutoComm.controlDir},");
            rawDataString.Append($"{PlutoComm.target},");
            rawDataString.Append($"{PlutoComm.err},");
            rawDataString.Append($"{PlutoComm.errDiff},");
            rawDataString.Append($"{PlutoComm.errSum},");

            // Game Data
            rawDataString.Append($"{GetGamePlayerPosition()},");
            rawDataString.Append($"{GetGameTargetPosition()},");
            rawDataString.Append($"{GetGameState()},");
            rawDataString.Append($"{aanController.targetPosition:F3},");
            rawDataString.Append($"{aanController.initialPosition:F3},");
            rawDataString.Append($"{aanController.state}");

            // End of line
            rawDataString.Append("\n");
        }
    }

    private void WriteTrialDataToRawDataFile()
    {
        AppLogger.LogInfo($"Writing to: {trialRawDataFile}");
        AppLogger.LogInfo($"File exists before write? {File.Exists(trialRawDataFile)}");
        
        string _dir = Path.GetDirectoryName(trialRawDataFile);
        if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);

        lock (rawDataLock)  // locking
        {
            using (StreamWriter sw = new StreamWriter(trialRawDataFile, false, Encoding.UTF8))
            {
                sw.Write(rawDataString.ToString());
            }
            rawDataString.Clear();
            rawDataString = null;
        }
        AppLogger.LogInfo($"File exists before write? {File.Exists(trialRawDataFile)}");

    }


    private string GetGamePlayerPosition()
    {
        // Get the game target X position.
        if (selectedGameName == "HAT")
        {
            return $"{HatGameController.Instance.PlayerPosition.x:F3},{HatGameController.Instance.PlayerPosition.y:F3}";
        }
        else if(selectedGameName == "PONG"){
            return $"{PongGameController.Instance.PlayerPosition.x:F3},{PongGameController.Instance.PlayerPosition.y:F3}";
        }
         else if(selectedGameName == "TUK"){
            return $"{FlappyGameControl.Instance.PlayerPosition.x:F3},{FlappyGameControl.Instance.PlayerPosition.y:F3}";
        }
        else if(selectedGameName == "RNR"){
            return $"{GameManager.Instance.PlayerPosition.x:F3},{GameManager.Instance.PlayerPosition.y:F3}";
        }
         else if(selectedGameName == "FRUITCH"){
           if (FruitBasketGameController.Instance.PlayerPosition.HasValue) return $"{FruitBasketGameController.Instance.PlayerPosition.Value.x:F3},{FruitBasketGameController.Instance.PlayerPosition.Value.y:F3}";
        }
        
        
        return ",";
    }

    private string GetGameTargetPosition()
    {
        // Get the game target X position.
        if (selectedGameName == "HAT")
        {
            if (HatGameController.Instance.TargetPosition.HasValue)
            {
                return $"{HatGameController.Instance.TargetPosition.Value.x:F3},{HatGameController.Instance.TargetPosition.Value.y:F3}";
            }   
        }
        else if(selectedGameName == "PONG"){
            if (PongGameController.Instance.TargetPosition.HasValue) return $"{PongGameController.Instance.TargetPosition.Value.x:F3},{PongGameController.Instance.TargetPosition.Value.y:F3}";
        }
        else if(selectedGameName == "TUK"){
           if (FlappyGameControl.Instance.TargetPosition.HasValue) return $"{FlappyGameControl.Instance.TargetPosition.Value.x:F3},{FlappyGameControl.Instance.TargetPosition.Value.y:F3}";
        }
        else if(selectedGameName == "RNR"){
           if (GameManager.Instance.TargetPosition.HasValue) return $"{GameManager.Instance.TargetPosition.Value.x:F3},{GameManager.Instance.TargetPosition.Value.y:F3}";
        }
        else if(selectedGameName == "FRUITCH"){
           if (FruitBasketGameController.Instance.TargetPosition.HasValue) return $"{FruitBasketGameController.Instance.TargetPosition.Value.x:F3},{FruitBasketGameController.Instance.TargetPosition.Value.y:F3}";
        }
        return ",";
    }

    private string GetGameState()
    {
        // Get the game state.
        if (selectedGameName == "HAT")
        {
            return $"{HatGameController.Instance.gameState}";
        }
        else if(selectedGameName == "PONG"){
            return $"{PongGameController.Instance.gameState}";
        }
        else if(selectedGameName == "TUK"){
            return $"{FlappyGameControl.Instance.gameState}";
        }
        else if(selectedGameName == "RNR"){
            return $"{GameManager.Instance.gameState}";
        }
        else if(selectedGameName == "FRUITCH"){
            return $"{FruitBasketGameController.Instance.gameState}";
        }
        return "";
    }
}
