/*
 * Miscellaneous definitions used in the application.
 * 
 * Author: Sivakumar Balasubramanian
 * Date: 07 April 2025
 */

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
// using XCharts.Runtime;
using System.Diagnostics;
using UnityEngine;
using System.Diagnostics.Contracts;

public static class PlutoDefs
{
    public static readonly string[] Mechanisms = new string[] { "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };

    public static int getMechanimsIndex(string mech)
    {
        return Array.IndexOf(Mechanisms, mech);
    }
}

public static class HomerTherapyConstants
{
    public static readonly float SuccessRateThForSpeedIncrement = 0.9f;
    static public readonly Dictionary<string, float> GameSpeedIncrements = new Dictionary<string, float>  {
        { "PING-PONG", 0.5f },
        { "TUK-TUK", 0.2f },
        { "HAT-Trick", 1f }
    };
}

// PLUTO UserData Class
public class PlutoUserData
{
    public DataTable dTableConfig { private set; get; } = null;
    public DataTable dTableSession { private set; get; } = null;
    public string hospNumber { private set; get; }
    public bool rightHand { private set; get; }
    public DateTime startDate { private set; get; }
    public Dictionary<string, float> mechMoveTimePrsc { get; private set; } // Prescribed movement time
    public Dictionary<string, float> mechMoveTimePrev { get; private set; } // Previous movement time 
    public Dictionary<string, float> mechMoveTimeCurr { get; private set; } // Current movement time

    // Total movement times.
    public float totalMoveTimePrsc
    {
        get
        {
            if (mechMoveTimePrsc == null)
            {
                return -1f;
            }
            else
            {
                return mechMoveTimePrsc.Values.Sum();
            }
        }
    }

    public float totalMoveTimePrev
    {
        get
        {
            if (!File.Exists(DataManager.sessionFile))
            {
                return -1f;
            }
            if (mechMoveTimePrev == null)
            {
                return -1f;
            }
            else
            {
                return mechMoveTimePrev.Values.Sum();
            }
        }
    }

    public float totalMoveTimeCurr
    {
        get
        {
            if (!File.Exists(DataManager.sessionFile))
            {
                return -1f;
            }
            if (mechMoveTimeCurr == null)
            {
                return -1f;
            }
            else
            {
                return mechMoveTimeCurr.Values.Sum();
            }
        }
    }

    public float totalMoveTimeRemaining
    {
        get
        {
            float _total = 0f;

            if (mechMoveTimePrsc != null && (mechMoveTimePrev == null || mechMoveTimeCurr == null))
            {
                foreach (string mech in PlutoDefs.Mechanisms)
                {
                    _total += mechMoveTimePrsc[mech];
                }
                return _total;
            }
            else
            {
                foreach (string mech in PlutoDefs.Mechanisms)
                {
                    _total += mechMoveTimePrsc[mech] - mechMoveTimePrev[mech] - mechMoveTimeCurr[mech];
                }
                return _total;
            }
        }
    }

    // Constructor
    public PlutoUserData(string configData, string sessionData)
    {
        if (File.Exists(configData))
        {
            dTableConfig = DataManager.loadCSV(configData);
        }
        // Create session file if it does not exist.
        if (!File.Exists(sessionData)) DataManager.CreateSessionFile("PLUTO", GetDeviceLocation());
        // Read the session file
        dTableSession = DataManager.loadCSV(sessionData);
        mechMoveTimeCurr = createMoveTimeDictionary();

        // Read the therapy configuration data.
        parseTherapyConfigData();
        if (File.Exists(DataManager.sessionFile))
        {
            parseMechanismMoveTimePrev();
        }

        // Is right training side
        //UnityEngine.Debug.Log(dTableConfig.Rows[0]["TrainingSide"].ToString());
        this.rightHand = dTableConfig.Rows[0]["TrainingSide"].ToString().ToUpper() == "RIGHT";
    }

    
    // // Get the last session number.
    // public static int GetPreviousSessionNumber()
    // {
    //     if (!File.Exists(sessionFile)) return 0;
    //     // Read the last line of the file
    //     var lastLine = File.ReadLines(sessionFile).LastOrDefault();
    //     if (lastLine == null || lastLine.StartsWith("SessionNumber")) return 0;
    //     return int.TryParse(lastLine.Split(',')[0], out var sessionNumber) ? sessionNumber : 0;
    // }

    public string GetDeviceLocation() => dTableConfig.Rows[dTableConfig.Rows.Count - 1].Field<string>("Location");

    private Dictionary<string, float> createMoveTimeDictionary()
    {
        Dictionary<string, float> _temp = new Dictionary<string, float>();
        for (int i = 0; i < PlutoDefs.Mechanisms.Length; i++)
        {
            _temp.Add(PlutoDefs.Mechanisms[i], 0f);
        }
        return _temp;
    }

    public float getRemainingMoveTime(string mechanism)
    {
        return mechMoveTimePrsc[mechanism] - mechMoveTimePrev[mechanism] - mechMoveTimeCurr[mechanism];
    }

    public float getTodayMoveTimeForMechanism(string mechanism)
    {
        if (mechMoveTimePrev == null || mechMoveTimeCurr == null)
        {
            return 0f;
        }
        else
        {
            float result = mechMoveTimePrev[mechanism] + mechMoveTimeCurr[mechanism];
            return Mathf.Round(result * 100f) / 100f; // Rounds to two decimal places
        }
    }

    public int getCurrentDayOfTraining()
    {
        TimeSpan duration = DateTime.Now - startDate;
        return (int)duration.TotalDays;
    }

    private void parseMechanismMoveTimePrev()
    {
        mechMoveTimePrev = createMoveTimeDictionary();
        for (int i = 0; i < PlutoDefs.Mechanisms.Length; i++)
        {
            // Get the total movement time for each mechanism
            var _totalMoveTime = dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
                .Where(row => row.Field<string>("Mechanism") == PlutoDefs.Mechanisms[i])
                .Sum(row => Convert.ToInt32(row["MoveTime"]));
            mechMoveTimePrev[PlutoDefs.Mechanisms[i]] = _totalMoveTime / 60f;
        }
    }

    public void calculateGameSpeedForLastUsageDay()
    {
        if (dTableSession == null || dTableSession.Rows.Count == 0)
        {
            AppLogger.LogError("Session data is not available.");
            return;
        }
        // Get the recent data of use for the selected mechanism.
        var lastUsageDate = dTableSession.AsEnumerable()
            .Where(row => row.Field<string>("Mechanism") == AppData.Instance.selectedMechanism.name)
            .Select(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date)
            .Where(date => date < DateTime.Now.Date) // Exclude today
            .OrderByDescending(date => date)
            .FirstOrDefault();
        if (lastUsageDate == default(DateTime))
        {
            AppLogger.LogWarning($"No usage data found for mechanism: {AppData.Instance.selectedMechanism}");
            return;
        }
        AppLogger.LogInfo($"Last usage date for mechanism {AppData.Instance.selectedMechanism}: {lastUsageDate:dd-MM-yyyy}");

        Dictionary<string, float> updatedGameSpeeds = new Dictionary<string, float>();
        foreach (var _gameName in HomerTherapyConstants.GameSpeedIncrements.Keys)
        {
            var rows = dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == lastUsageDate)
                .Where(row => row.Field<string>("GameName") == _gameName && row.Field<string>("Mechanism") == AppData.Instance.selectedMechanism.name);

            float previousGameSpeed = rows.Any() ? rows.Average(row => Convert.ToSingle(row["GameSpeed"])) : 0f;
            float avgSuccessRate = rows.Any() ? rows.Average(row => Convert.ToSingle(row["SuccessRate"])) : 0f;

            if (avgSuccessRate >= HomerTherapyConstants.SuccessRateThForSpeedIncrement)
            {
                updatedGameSpeeds[_gameName] = previousGameSpeed + HomerTherapyConstants.GameSpeedIncrements[_gameName];
            }
            else
            {
                updatedGameSpeeds[_gameName] = previousGameSpeed;
            }
        }
        AppLogger.LogInfo($"Updated GameSpeeds for Mechanism: {AppData.Instance.selectedMechanism}");
        foreach (var game in updatedGameSpeeds)
        {
            AppLogger.LogInfo($"Game speed for '{game.Key}' is set to {game.Value}.");
            if (game.Key == "PING-PONG")
            {
                gameData.gameSpeedPP = game.Value;
            }
            else if (game.Key == "TUK-TUK")
            {
                gameData.gameSpeedTT = game.Value;
            }
            else if (game.Key == "HAT-Trick")
            {
                gameData.gameSpeedHT = game.Value;
            }
        }
    }

    private void parseTherapyConfigData()
    {
        DataRow lastRow = dTableConfig.Rows[dTableConfig.Rows.Count - 1];
        hospNumber = lastRow.Field<string>("HospitalNumber");
        rightHand = lastRow.Field<string>("TrainingSide") == "right";
        //AppData.trainingSide = ; // lastRow.Field<string>("TrainingSide");
        startDate = DateTime.ParseExact(lastRow.Field<string>("startdate"), "dd-MM-yyyy", CultureInfo.InvariantCulture);
        mechMoveTimePrsc = createMoveTimeDictionary();//prescribed time
        for (int i = 0; i < PlutoDefs.Mechanisms.Length; i++)
        {
            mechMoveTimePrsc[PlutoDefs.Mechanisms[i]] = float.Parse(lastRow.Field<string>(PlutoDefs.Mechanisms[i]));
        }
    }

    // Returns today's total movement time in minutes.
    public float getPrevTodayMoveTime()
    {
        var _totalMoveTimeToday = dTableSession.AsEnumerable()
            .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
            .Sum(row => Convert.ToInt32(row["MoveTime"]));
        UnityEngine.Debug.Log(_totalMoveTimeToday);
        return _totalMoveTimeToday / 60f;
    }

    public DaySummary[] CalculateMoveTimePerDay(int noOfPastDays = 7)
    {
        // Check if the session file has been loaded and has rows
        if (dTableSession == null || dTableSession.Rows.Count == 0)
        {
            AppLogger.LogWarning("Session data is not available or the file is empty.");
            return new DaySummary[0];
        }
        DateTime today = DateTime.Now.Date;
        DaySummary[] daySummaries = new DaySummary[noOfPastDays];

        // Loop through each day, starting from the day before today, going back `noOfPastDays`
        for (int i = 1; i <= noOfPastDays; i++)
        {
            DateTime _day = today.AddDays(-i);

            // Calculate the total move time for the given day. If no data is found, _moveTime will be zero.
            int _moveTime = dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == _day)
                .Sum(row => Convert.ToInt32(row["MoveTime"]));

            daySummaries[i - 1] = new DaySummary
            {
                Day = Others.GetAbbreviatedDayName(_day.DayOfWeek),
                Date = _day.ToString("dd/MM"),
                MoveTime = _moveTime / 60f
            };
            UnityEngine.Debug.Log($"{i} | {daySummaries[i - 1].Day} | {daySummaries[i - 1].Date} | {daySummaries[i - 1].MoveTime}");
        }
        return daySummaries;
    }
}

public static class Others
{
    public static string GetAbbreviatedDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek.ToString().Substring(0, 3);
    }
}

public class PlutoMechanism
{
    public static readonly Dictionary<string, float> DefaultMechanismSpeeds = new Dictionary<string, float>
    {
        { "WFE", 10.0f },
        { "WURD", 10.0f },
        { "FPS", 10.0f },
        { "HOC", 10.0f },
        { "FME1", 10.0f },
        { "FME2", 10.0f },
    };
    // public static string MECHPATH { get; private set; } = DataManager.mechPath;
    public string name { get; private set; }
    public string side { get; private set; }
    public bool promCompleted { get; private set; }
    public bool aromCompleted { get; private set; }
    public ROM oldRom { get; private set; }
    public ROM newRom { get; private set; }
    public ROM currRom { get => newRom.isSet ? newRom : (oldRom.isSet ? oldRom : null); }
    public float currSpeed { get; private set; } = -1f;
    // Trial details for the mechanism.
    public int trialNumberDay { get; private set; }
    public int trialNumberSession { get; private set; }

    public PlutoMechanism(string name, string side)
    {
        this.name = name?.ToUpper() ?? string.Empty;
        this.side = side;
        oldRom = new ROM(this.name);
        newRom = new ROM();
        promCompleted = false;
        aromCompleted = false;
        this.side = side;
        currSpeed = -1f;
        UpdateTrialNumbers();
    }

    public bool IsMechanism(string mechName) => string.Equals(name, mechName, StringComparison.OrdinalIgnoreCase);

    public bool IsSide(string sideName) => string.Equals(side, sideName, StringComparison.OrdinalIgnoreCase);

    public bool IsSpeedUpdated() => currSpeed < 0;

    public float[] CurrentArom => currRom == null? null : new float[] { currRom.aromMin, currRom.aromMax };
    public float[] CurrentProm => currRom == null? null : new float[] { currRom.promMin, currRom.promMax };

    public void ResetPromValues()
    {
        newRom.SetProm(0, 0);
        promCompleted = false;
    }

    public void ResetAromValues()
    {
        newRom.SetArom(0, 0);
        aromCompleted = false;
    }

    public void SetNewPromValues(float pmin, float pmax)
    {
        newRom.SetProm(pmin, pmax);
        if (pmin != 0 || pmax != 0) promCompleted = true;
        // Cehck if newRom's mechanism needs to be set.
        if (newRom.mechanism == null)
        {
            newRom.SetMechanism(this.name);
        }
    }

    public void SetNewAromValues(float amin, float amax)
    {
        newRom.SetArom(amin, amax);
        if (amin != 0 || amax != 0) aromCompleted = true;
    }

    public void SaveAssessmentData()
    {
        if (promCompleted && aromCompleted)
        {
            // Save the new ROM values to the file.
            newRom.WriteToAssessmentFile();
        }
    }

    public void UpdateSpeed()
    {
        // Read the mechanism file.
        string fileName = DataManager.GetMechFileName(this.name);
        bool _updateFile = false;
        // Create file if needed.
        if (!File.Exists(fileName))
        {
            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                writer.WriteLine("DateTime,Speed");
            }
        }
        // Read the file and get the most recent speed value.
        DataTable speedData = DataManager.loadCSV(fileName);
        // Check the number of rows.

        // If the last line is empty, set default value for the speed.
        if (speedData.Rows.Count ==0)
        {
            currSpeed = DefaultMechanismSpeeds[name];
            _updateFile = true;
        }
        else
        {
            // Get datetime from the last row.
            DateTime lastDate = DateTime.ParseExact(
                speedData.Rows[speedData.Rows.Count - 1].Field<string>("DateTime"),
                "dd-MM-yyyy HH:mm:ss",
                CultureInfo.InvariantCulture
            );
            if (lastDate.Date == DateTime.Now.Date)
            {
                // Set the speed to that of the last row.
                currSpeed = float.Parse(speedData.Rows[speedData.Rows.Count - 1].Field<string>("Speed"));
            }
            else
            {
                // TODO
                // Call the update function to compute the new game speed.
                // For now this is set to default, but this will need to changed.
                // If the last date is not today, set default value for the speed.
                currSpeed = DefaultMechanismSpeeds[name];
                _updateFile = true;
            }
        }
        // Update file?
        if (_updateFile)
        {
            // Write the new speed to the file.
            using (StreamWriter file = new StreamWriter(fileName, true))
            {
                file.WriteLine(string.Join(",", new string[] { DateTime.Now.ToString(), currSpeed.ToString() }));
            }
        }
    }

    /*
     * Function to update the trial numbers for the day and session for the mechanism for today.
     */
    public void UpdateTrialNumbers()
    {
        // Get the last row for the today, for the selected mechanism.
        var lastRow = AppData.Instance.userData.dTableSession.AsEnumerable()?
            .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
            .Where(row => row.Field<string>("Mechanism") == this.name)
            .OrderByDescending(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture))
            .FirstOrDefault();
        // Check if the last row is null.
        if (lastRow == null)
        {
            // Set the trial numbers to 1.
            trialNumberDay = 0;
            trialNumberSession = 0;
            return;
        }
        else
        {
            // Last row is not null.
            // Get the trial numbers from the last row.
            trialNumberDay = Convert.ToInt32(lastRow.Field<string>("TrialNumberDay"));
            trialNumberSession = Convert.ToInt32(lastRow.Field<string>("TrialNumberSession"));
        }
    }
}

/// <summary>
/// This class contains all the necessary information to run the assessment scene.
/// </summary>
//public class AssessmentData
//{
//    public string mechanism { get; private set; }
//    public bool promCompleted { get; private set; }
//    public bool aromCompleted { get; private set; }
//    public ROM oldRom { get; private set; }
//    public ROM newRom { get; private set; }
//    public string side { get; private set; }

//    public AssessmentData(string mech, string side)
//    {
//        mechanism = mech;
//        oldRom = new ROM(mech);
//        newRom = new ROM();
//        promCompleted = false;
//        aromCompleted = false;
//        this.side = side;
//    }
//    public void ResetPromValues()
//    {
//        newRom.promMin = 0;
//        newRom.promMax = 0;
//        promCompleted = false;
//    }

//    public void ResetAromValues()
//    {
//        newRom.aromMin = 0;
//        newRom.aromMax = 0;
//        aromCompleted = false;
//    }

//    public void SetNewPromValues(float pmin, float pmax)
//    {
//        newRom.promMin = pmin;
//        newRom.promMax = pmax;
//        if (pmin != 0 || pmax != 0)
//        {
//            promCompleted = true;

//        }

//    }

//    public void SetNewAromValues(float amin, float amax)
//    {
//        newRom.aromMin = amin;
//        newRom.aromMax = amax;
//        if (amin != 0 || amax != 0)
//        {
//            aromCompleted = true;
//        }

//    }

//    public void SaveAssessmentData()
//    {
//        if (promCompleted && aromCompleted)
//        {
//            // Save the new ROM values to the file.
//            newRom.WriteToAssessmentFile();
//        }

//    }
//}

public class ROM
{
    public static string[] FILEHEADER = new string[] {
        "DateTime", "PromMin", "PromMax", "AromMin", "AromMax"
    };
    // Class attributes to store data read from the file
    public string datetime;
    public float promMin { get; private set; }
    public float promMax { get; private set; }
    public float aromMin { get; private set; }
    public float aromMax { get; private set; }
    public string mechanism { get; private set; }
    public bool isAromSet { get => aromMin != 0 || aromMax != 0; }
    public bool isPromSet { get => promMin != 0 || promMax != 0; }
    public bool isSet { get => isAromSet && isPromSet; }

    // Constructor that reads the file and initializes values based on the mechanism
    public ROM(string mechanismName, bool readFromFile = true)
    {
        if (readFromFile) ReadFromFile(mechanismName);
        else
        {
            // Handle case when no matching mechanism is found
            datetime = null;
            mechanism = mechanismName;
            promMin = 0;
            promMax = 0;
            aromMin = 0;
            aromMax = 0;
        }
    }

    public ROM(float angmin, float angmax, float aromAngMin, float aromAngMax, string mech, bool tofile)
    {
        promMin = angmin;
        promMax = angmax;
        aromMin = aromAngMin;
        aromMax = aromAngMax;
        mechanism = mech;
        datetime = DateTime.Now.ToString();
        if (tofile) WriteToAssessmentFile();
    }

    public ROM()
    {
        promMin = 0;
        promMax = 0;
        aromMin = 0;
        aromMax = 0;
        mechanism = null;
        datetime = null;
    }

    public void SetMechanism(string mech) => mechanism = (mechanism == null) ? mech : mechanism;

    public void SetProm(float min, float max)
    {
        promMin = min;
        promMax = max;
        datetime = DateTime.Now.ToString();
    }

    public void SetArom(float min, float max)
    {
        aromMin = min;
        aromMax = max;
        datetime = DateTime.Now.ToString();
    }

    public void WriteToAssessmentFile()
    {
        string fileName = DataManager.GetRomFileName(mechanism);;
        using (StreamWriter file = new StreamWriter(fileName, true))
        {
            file.WriteLine(string.Join(",", new string[] { datetime, promMin.ToString(), promMax.ToString(), aromMin.ToString(), aromMax.ToString() }));
        }
    }

    private void ReadFromFile(string mechanismName)
    {
        string fileName = DataManager.GetRomFileName(mechanismName);
        // Create the file if it doesn't exist
        if (!File.Exists(fileName))
        {
            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                writer.WriteLine(string.Join(",", FILEHEADER));
            }
        }
        // Read file.
        DataTable romData = DataManager.loadCSV(fileName);
        // Check the number of rows.
        if (romData.Rows.Count == 0)
        {
            // Set default values for the mechanism.
            datetime = null;
            mechanism = mechanismName;
            promMin = 0;
            promMax = 0;
            aromMin = 0;
            aromMax = 0;
            return;
        }
        // Assign ROM from the last row.
        datetime = romData.Rows[romData.Rows.Count - 1].Field<string>("DateTime");
        mechanism = mechanismName;
        promMin = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("PromMin"));
        promMax = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("PromMax"));
        aromMin = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("AromMin"));
        aromMax = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("AromMax"));
    }
}

/*
 * AAN Controller for HOMER
 */
// public class AANController : HOMERPlutoAANController
// {   
//     public const float BOUNDARY = 0.9f;
//     // Other variables for logging
//     private int sessNo;
//     private int trialNoDay;
//     private int trialNoSess;
//     // Logging realted variables.
//     private string  adaptFileName = null;
//     private StreamWriter execFileWriter = null;

//     private AANController(int sessNo, PlutoMechanism mechanism, float[] aRomValue, float[] pRomValue) : base(aRomValue, pRomValue, BOUNDARY)
//     {
//         // If AROM or PROM is null, then this is a null initialization.
//         if (aRomValue == null || pRomValue == null) return;
//     }

//     public void StartNewTrial(float actual, float target, float maxDur)
//     {
//         SetNewTrialDetails(actual, target, maxDur);
//         //
//     }
// }

/// <summary>
/// Stores global game-related data and settings.
/// </summary>
public static class gameData
{
    // Assessment check
    public static bool isPROMcompleted = false;
    public static bool isAROMcompleted = false;
    // AAN controller check
    public static bool isBallReached = false;
    public static bool targetSpwan = false;
    public static bool isAROMEnabled = false;
    // Game
    public static bool isGameLogging;
    public static string game;
    public static int gameScore;
    public static int reps;
    public static int playerScore;
    public static int enemyScore;
    public static string playerPos = "0";
    public static string playerPosition = "0";
    public static string enemyPos = "0";
    public static string playerHit = "0";
    public static string enemyHit = "0";
    public static string wallBounce = "0";
    public static string enemyFail = "0";
    public static string playerFail = "0";
    public static int winningScore = 3;
    public static float moveTime;
    public static readonly string[] pongEvents = new string[] { "moving", "wallBounce", "playerHit", "enemyHit", "playerFail", "enemyFail" };
    public static readonly string[] hatEvents = new string[] { "moving", "BallCaught", "BombCaught", "BallMissed", "BombMissed" };
    public static readonly string[] tukEvents = new string[] { "moving", "collided", "passed" };
    public static int events;
    public static string TargetPos;
    public static float successRate;
    public static float gameSpeedTT;
    public static float gameSpeedPP;
    public static float gameSpeedHT;
    public static float predictedHitY;
    public static bool setNeutral = false;
    private static DataLogger dataLog;
    private static readonly string[] gameHeader = new string[] {
        "time","controltype","error","buttonState","angle","control",
        "target","playerPosY","enemyPosY","events","playerScore","enemyScore"
    };
    private static readonly string[] tukTukHeader = new string[] {
        "time","controltype","error","buttonState","angle","control",
        "target","playerPosx","events","playerScore"
    };
    public static bool isLogging { get; private set; }
    public static bool moving = true; // used to manipulate events in HAT TRICK

    static public void StartDataLog(string fname)
    {
        if (dataLog != null)
        {
            StopLogging();
        }
        // Start new logger
        if (AppData.Instance.selectedGame == "PINGPONG")
        {
            if (fname != "")
            {
                string instructionLine = "0 - moving, 1 - wallBounce, 2 - playerHit, 3 - enemyHit, 4 - playerFail, 5 - enemyFail\n";
                string headerWithInstructions = instructionLine + String.Join(", ", gameHeader) + "\n";
                dataLog = new DataLogger(fname, headerWithInstructions);
                isLogging = true;
            }
            else
            {
                dataLog = null;
                isLogging = false;
            }
        }
        else if (AppData.Instance.selectedGame == "HATTRICK")
        {
            if (fname != "")
            {
                string instructionLine = "0 - moving, 1 - BallCaught, 2 - BombCaught, 3 - BallMissed, 4 - BombMissed\n";
                string headerWithInstructions = instructionLine + String.Join(", ", tukTukHeader) + "\n";
                dataLog = new DataLogger(fname, headerWithInstructions);
                isLogging = true;
            }
            else
            {
                dataLog = null;
                isLogging = false;
            }
        }
        else if (AppData.Instance.selectedGame == "TUKTUK")
        {
            if (fname != "")
            {
                string instructionLine = "0 - moving, 1 - collided, 2 - passed\n";
                string headerWithInstructions = instructionLine + String.Join(", ", tukTukHeader) + "\n";
                dataLog = new DataLogger(fname, headerWithInstructions);
                isLogging = true;
            }
            else
            {
                dataLog = null;
                isLogging = false;
            }
        }
    }

    static public void StopLogging()
    {
        if (dataLog != null)
        {
            UnityEngine.Debug.Log("Null log not");
            dataLog.stopDataLog(true);
            dataLog = null;
            isLogging = false;
        }
        else
            UnityEngine.Debug.Log("Null log");
    }

    static public void LogData()
    {
        // Log only if the current data is SENSORSTREAM
        if (PlutoComm.SENSORNUMBER[PlutoComm.dataType] == 5)
        {
            string[] _data = new string[] {
               PlutoComm.currentTime.ToString(),
               PlutoComm.CONTROLTYPE[PlutoComm.controlType],
               PlutoComm.errorStatus.ToString(),
               PlutoComm.button.ToString(),
               PlutoComm.angle.ToString("G17"),
               PlutoComm.control.ToString("G17"),
               PlutoComm.target.ToString("G17"),
               playerPos,
               enemyPos,
               gameData.events.ToString("F2"),
               gameData.playerScore.ToString("F2"),
               gameData.enemyScore.ToString("F2")
            };
            string _dstring = String.Join(", ", _data);
            _dstring += "\n";
            dataLog.logData(_dstring);
        }
    }
    static public void LogDataHT()
    {
        UnityEngine.Debug.Log("Data Log: " + dataLog);
        UnityEngine.Debug.Log("Data Log: " + dataLog);
        if (PlutoComm.SENSORNUMBER[PlutoComm.dataType] == 5)
        {
            string[] _data = new string[] {
               PlutoComm.currentTime.ToString(),
               PlutoComm.CONTROLTYPE[PlutoComm.controlType],
               PlutoComm.errorStatus.ToString(),
               PlutoComm.button.ToString(),
               PlutoComm.angle.ToString("G17"),
               PlutoComm.control.ToString("G17"),
               PlutoComm.target.ToString("G17"),
               playerPos,
               gameData.events.ToString("F2"),
               gameData.gameScore.ToString("F2")
            };
            string _dstring = String.Join(", ", _data);
            _dstring += "\n";
            dataLog.logData(_dstring);
        }
        else
        {
            UnityEngine.Debug.Log("PlutoComm:" + PlutoComm.OUTDATATYPE[PlutoComm.dataType]);
            UnityEngine.Debug.Log("PlutoComm:" + PlutoComm.dataType);
        }
    }
}

public class DataLogger
{
    public string currFileName { get; private set; }
    public StringBuilder fileData;

    public bool stillLogging
    {
        get { return (fileData != null); }
    }

    public DataLogger(string filename, string header)
    {
        currFileName = filename;

        fileData = new StringBuilder(header);
    }

    public void stopDataLog(bool log = true)
    {
        if (log)
        {
            UnityEngine.Debug.Log("Stored");
            if (fileData != null)
            {
                UnityEngine.Debug.Log("Data available");
            }
            else
            {
                UnityEngine.Debug.Log("Data not available");
            }
            File.AppendAllText(currFileName, fileData.ToString());
        }
        currFileName = "";
        fileData = null;
    }

    public void logData(string data)
    {
        if (fileData != null)
        {
            fileData.Append(data);
        }
    }
}


public class AANDataLogger
{
    // AAN class
    private HOMERPlutoAANController aanCtrler;

    // Logging related variables
    private string fileNamePrefix = null;
    private string logRawFileName = null;
    private StreamWriter logRawFile = null;
    private string logAdaptFileName = null;
    private StreamWriter logAdaptFile = null;
    private string logAanFileName = null;
    private StreamWriter logAanFile = null;

    private uint trialNo;

    public AANDataLogger(HOMERPlutoAANController controller)
    {
        aanCtrler = controller;
        PlutoComm.OnNewPlutoData += onNewPlutoData;
    }

    public void onNewPlutoData()
    {
        // Log data if needed. Else move on.
        if (logRawFile == null) return;

        // Log data
        String[] rowcomps = new string[]
        {
            $"{PlutoComm.runTime}",
            $"{PlutoComm.packetNumber}",
            $"{PlutoComm.status}",
            $"{PlutoComm.dataType}",
            $"{PlutoComm.errorStatus}",
            $"{PlutoComm.controlType}",
            $"{PlutoComm.calibration}",
            $"{PlutoComm.mechanism}",
            $"{PlutoComm.button}",
            $"{PlutoComm.angle}",
            $"{PlutoComm.torque}",
            $"{PlutoComm.control}",
            $"{PlutoComm.controlBound}",
            $"{PlutoComm.controlDir}",
            $"{PlutoComm.target}",
            $"{PlutoComm.desired}",
            $"{PlutoComm.err}",
            $"{PlutoComm.errDiff}",
            $"{PlutoComm.errSum}"
        };
        if (logRawFile != null)
        {
            logRawFile.WriteLine(String.Join(", ", rowcomps));
        }
    }

    public void WriteAanStateInforRow()
    {
        // Log data if needed. Else move on.
        if (logAanFile == null) return;

        // Log data
        float[] _aantgtvals = aanCtrler.GetNewAanTarget();
        string _aantgtdetails = _aantgtvals == null ? "" : $"{_aantgtvals[0]:F2}|{_aantgtvals[1]:F2}|{_aantgtvals[2]:F2}|{_aantgtvals[3]:F2}";
        int _stchng = aanCtrler.stateChange ? 1 : 0;
        String[] rowcomps = new string[]
        {
            $"{PlutoComm.runTime}",
            $"{aanCtrler.state}",
            $"{_stchng}",
            $"{_aantgtdetails}"
        };
        if (logAanFile != null)
        {
            logAanFile.WriteLine(String.Join(", ", rowcomps));
        }
        UnityEngine.Debug.Log("Writing ");
    }

    private void OnDataLogChange()
    {
        // Close file.
        CloseRawLogFile();
        CloseAdaptLogFile();
        logRawFile = null;
        logAdaptFile = null;
        fileNamePrefix = null;
    }

    public void UpdateLogFiles(uint trialNumber)
    {
        if (fileNamePrefix == null)
        {
            fileNamePrefix = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        }
        CreateDirectoryIfNeeded(AppData.Instance._dataLogDir + "\\");
        // Create the adaptation log file.
        if (logAdaptFile == null)
        {
            CreateAdaptLogFile();
        }
        trialNo = trialNumber;
        //// Create the raw log file after closing the current file.
        //CloseRawLogFile();
        //CreateRawLogFile();
        // Create the raw and AAN log file after closing the current file.
        CloseRawAndAanLogFile();
        CreateRawAndAanLogFile(trialNo);
    }


    public void CreateRawAndAanLogFile(uint trialNo)
    {
        string _writetime = $"{DateTime.Now:yyyy/MM/dd HH-mm-ss.ffffff}";
        // Raw log file
        // Set the file name.
        logRawFileName = $"rawlogfile_{trialNo:D3}.csv";
        logRawFile = new StreamWriter(AppData.Instance._dataLogDir + "\\" + logRawFileName, false);
        // Write the header row.
        //logRawFile.WriteLine($"DeviceID = {PlutoComm.deviceId}");
        //logRawFile.WriteLine($"FirmwareVersion = {PlutoComm.version}");
        //logRawFile.WriteLine($"CompileDate = {PlutoComm.compileDate}");
        //logRawFile.WriteLine($"Actuated = {PlutoComm.actuated}");
        logRawFile.WriteLine($"Start Datetime = {_writetime}");
        string[] headernames = { "time", "packetno", "status", "datatype", "errorstatus", "controltype", "calibration",
            "mechanism", "button", "angle", "torque", "control", "controlbound", "controldir", "target", "desired",
            "error", "errordiff", "errorsum"
        };
        logRawFile.WriteLine(String.Join(", ", headernames));

        // AAN log file
        // Set the file name.
        logAanFileName = $"aanlogfile_{trialNo:D3}.csv";
        logAanFile = new StreamWriter(AppData.Instance._dataLogDir + "\\" + logAanFileName, false);
        // Write the header row.
        //logAanFile.WriteLine($"DeviceID = {PlutoComm.deviceId}");
        //logAanFile.WriteLine($"FirmwareVersion = {PlutoComm.version}");
        //logAanFile.WriteLine($"CompileDate = {PlutoComm.compileDate}");
        //logAanFile.WriteLine($"Actuated = {PlutoComm.actuated}");
        logAanFile.WriteLine($"Start Datetime = {_writetime}");
        headernames = new string[] { "time", "aanstate", "aanstatechange", "aantargetdetails" };
        logAanFile.WriteLine(String.Join(", ", headernames));
    }

    public void CreateAdaptLogFile()
    {
        // Set the file name.
        logAdaptFileName = $"adaptlogfile.csv";
        logAdaptFile = new StreamWriter(AppData.Instance._dataLogDir + "\\" + logAdaptFileName, false);
        // Write the header row.
        //logAdaptFile.WriteLine($"DeviceID = {PlutoComm.deviceId}");
        //logAdaptFile.WriteLine($"FirmwareVersion = {PlutoComm.version}");
        //logAdaptFile.WriteLine($"CompileDate = {PlutoComm.compileDate}");
        //logAdaptFile.WriteLine($"Actuated = {PlutoComm.actuated}");
        logAdaptFile.WriteLine($"Start Datetime = {DateTime.Now:yyyy/MM/dd HH-mm-ss.ffffff}");
        logAdaptFile.WriteLine("trialno, targetposition, initialposition, success, successrate, controlbound, controldir, filename");
    }

    public void WriteTrialRowInfo(byte successfailure)
    {
        // Log data if needed. Else move on.
        if (logAdaptFile == null) return;

        // Log data
        String[] rowcomps = new string[]
        {
            $"{trialNo}",
            $"{aanCtrler.targetPosition}",
            $"{aanCtrler.initialPosition}",
            $"{successfailure}",
         //   $"{currControlDir}",
            $"{logRawFileName}"
        };
        if (logAdaptFile != null)
        {
            logAdaptFile.WriteLine(String.Join(", ", rowcomps));
        }
    }

    private void CloseRawLogFile()
    {
        if (logRawFile != null)
        {
            // Close the file properly and create a new handle.
            logRawFile.Close();
        }
        logRawFileName = null;
        logRawFile = null;
    }

    private void CloseRawAndAanLogFile()
    {
        // Close raw file
        if (logRawFile != null)
        {
            // Close the file properly and create a new handle.
            logRawFile.Close();
        }
        logRawFileName = null;
        logRawFile = null;
        // Close Aan file
        if (logAanFile != null)
        {
            // Close the file properly and create a new handle.
            logAanFile.Close();
        }
        logAanFileName = null;
        logAanFile = null;
    }

    private void CloseAdaptLogFile()
    {
        if (logAdaptFile != null)
        {
            // Close the file properly and create a new handle.
            logAdaptFile.Close();

            // Close any raw file that is open.
            CloseRawLogFile();

            // Clear filename prefix.
            fileNamePrefix = null;
        }
        logAdaptFileName = null;
        logAdaptFile = null;
    }

    private void CreateDirectoryIfNeeded(string dirname)
    {
        // Ensure the directory exists
        string directoryPath = Path.GetDirectoryName(dirname);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            UnityEngine.Debug.Log("Directory created");
        }
        else
        {
            UnityEngine.Debug.Log("already exist");
        }
    }
}