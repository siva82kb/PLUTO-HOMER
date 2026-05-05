/*
 * Miscellaneous definitions used in the application.
 * 
 * Author: Sivakumar Balasubramanian
 * Date: 07 April 2025
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Data;

using System.Globalization;
using System.Linq;
using System.Text;
// using XCharts.Runtime;
using UnityEngine;
using System.Collections;

public static class PlutoDefs
{
    public static readonly string[] Mechanisms = new string[] { "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };

    public static int getMechanimsIndex(string mech)
    {
        return Array.IndexOf(Mechanisms, mech);
    }
}

public static class HomerTherapy
{
    public static readonly float SuccessRateThForSpeedIncrement = 0.9f;
    public static readonly float TrialDuration = 60.0f;
    public static readonly Dictionary<string, float> GameSpeedIncrements = new Dictionary<string, float>  {
        { "PING-PONG", 0.5f },
        { "TUK-TUK", 0.2f },
        { "HAT-Trick", 1f },
        { "FRUITCH", 1f },
        { "RNR", 1f }
    };
    
    // public static readonly float MinSpeedOfMechFPSAndFME = 18.0f;
    // public static readonly float MaxSpeedOfMechFPSAndFME = 4.5f;

    // public static readonly float MinSpeedOfMechWFEAndWURD = 13.6f;
    // public static readonly float MaxSpeedOfMechWFEAndWURD = 3.4f;
    // public static readonly float MinSpeedOfMechOfHOC = 10.0f;
    // public static readonly float MaxSpeedOfMechofHOC = 2.25f;
    public static readonly float MaxSpeed = 40.0f;
    public static readonly float MinSpeed = 10.0f;

    // Dynamically calculated mechanism speeds
    public static float MaxDurationOfMechFPSAndFME => CalculateMechDuration(PlutoComm.CALIBANGLE[3], MinSpeed);
    public static float MinDurationOfMechFPSAndFME => CalculateMechDuration(PlutoComm.CALIBANGLE[3], MaxSpeed);
    public static float MaxDurationOfMechWFEAndWURD => CalculateMechDuration(PlutoComm.CALIBANGLE[1], MinSpeed);
    public static float MinDurationOfMechWFEAndWURD => CalculateMechDuration(PlutoComm.CALIBANGLE[1], MaxSpeed);
    public static float MaxDurationOfMechOfHOC => CalculateMechDuration(PlutoComm.CALIBANGLE[4], MinSpeed);
    public static float MinDurationOfMechofHOC => CalculateMechDuration(PlutoComm.CALIBANGLE[4], MaxSpeed);
    
    private static float CalculateMechDuration(float maxangle, float Speed)
    {
        return maxangle / Speed;
    }
    private static float? lastTarget = null;
    private static float threshold = 0f;

    public enum TrialType
    {
        SR85PCCATCH,
        TRAIN,
        SR85PCTRAIN
    }

    private static float[] SuccessRateForTrials = new float[] {
        85, 85, 85, 85, 85,
        90, 90, 90, 87, 84,
        79, 79, 79, 79, 79,
        81, 83, 85, 90, 90
    };
    private static TrialType[] TrialTypeForTrials = new TrialType[] {
        TrialType.SR85PCTRAIN, TrialType.SR85PCTRAIN, TrialType.SR85PCTRAIN, TrialType.SR85PCTRAIN, TrialType.SR85PCCATCH,
        TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN,
        TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN,
        TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN,
    };


    // private static float[] SuccessRateForTrials = new float[] {
    //     85, 90, 90, 87, 84,
    //     79, 79, 79, 79, 79,
    //     79, 79, 81, 83, 85,
    //     85, 85, 85, 85, 85
    // };

    // private static TrialType[] TrialTypeForTrials = new TrialType[] {
    //     TrialType.SR85PCTRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN,
    //     TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN,
    //     TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN, TrialType.TRAIN,
    //     TrialType.SR85PCCATCH, TrialType.SR85PCTRAIN, TrialType.SR85PCTRAIN, TrialType.SR85PCTRAIN, TrialType.SR85PCTRAIN, 
    // };

    // Function to return the success rate and trial type.
    public static (float sRate, TrialType tType) GetTrailTypeAndSuccessRate(int trialNo)
    {
        float sRate;
        TrialType tType;

        trialNo = (trialNo - 1) % 20;
        sRate = SuccessRateForTrials[trialNo];
        tType = TrialTypeForTrials[trialNo];
        // Updat success rate.
        sRate += tType == TrialType.TRAIN ? UnityEngine.Random.Range(-4, 5) : 0;
        return (sRate, tType);
    }

    // Generate new target position
    private static float[] GetRomBoundariesForTargets(float[] arom, float[] prom)
    {
        if (prom[0] == 0 && arom[0] == 0)
        {
            return new float[] {
                arom[0],
                arom[1] / 2,
                arom[1],
                (prom[1] - arom[1]) / 2,
                prom[1]
            };
        }
        return new float[] {
            prom[0],
            (arom[0] + prom[0]) / 2,
            arom[0],
            arom[0] + (arom[1] + arom[0]) / 4,
            (arom[1] + arom[0]) / 2,
            arom[0] + 3 * (arom[1] + arom[0]) / 4,
            arom[1],
            (prom[1] - arom[1]) / 2,
            prom[1]
        };
    }


    public static float GetNewTargetPositionUniformFull(float[] arom, float[] prom)
    {
        float target;
        threshold = (AppData.Instance.selectedMechanism.currRom.promMax - AppData.Instance.selectedMechanism.currRom.promMin) * 0.2f;
        int attempts = 0;

        do
        {
            target = UnityEngine.Random.Range(prom[0], prom[1]);
            attempts++;

            if (attempts > 20) break;

        } while (lastTarget != null && Mathf.Abs((float)lastTarget - target) < threshold);

        lastTarget = target;
        return target;
    }
  
}


public class MechanismSpeed
{
    public float gameSpeed { get; private set; } = -1f;
    public float MOVEDURATION{get; private set;}

    // private string AppData.Instance.selectedMechanism.name;

    private DataTable sessionTable;
    private string mechParamsCsvPath;
    private static readonly string[] speedChMode = new string[] {"DEFAULT","MANUAL", "AUTO" };
    public static readonly Dictionary<string, float> DefaultMechanismSpeeds = new Dictionary<string, float>
    {
        { "WFE", 10.0f },
        { "WURD", 10.0f },
        { "FPS", 10.0f },
        { "HOC", 10.0f },
        { "FME1", 10.0f },
        { "FME2", 10.0f },
    };
    public MechanismSpeed()
    {
        this.sessionTable = AppData.Instance.userData.dTableSession;
        this.mechParamsCsvPath = DataManager.GetMechFileName(AppData.Instance.selectedMechanism.name);
        EvaluateAndUpdateGameSpeed();
    }

    public void setGameSpeed(float gamespeed)
    {
        gameSpeed = gamespeed;
        updateGameSpeedfromGame(gamespeed);
    }
    public void setMoveDuration(float duration)
    {
        MOVEDURATION = duration;
    }
    public void EvaluateAndUpdateGameSpeed()
    {
        if (!File.Exists(mechParamsCsvPath))
        {
            WriteInitialSpeed();
            return;
        }
        var mechData = sessionTable.AsEnumerable()
            .Where(row => row.Field<string>("Mechanism") == AppData.Instance.selectedMechanism.name)
            .ToList();
        // Debug.Log($"mechData:{mechData.Count}");
        var groupedByDate = mechData
            .GroupBy(row => DateTime.ParseExact(row.Field<string>("DateTime"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).Date)
            .OrderBy(g => g.Key)
            .ToList();
        // Debug.Log($"mechData:{groupedByDate.Count}");

        if (groupedByDate.Count < 3)
        {
            GetLastDateFromMechParams();
            Debug.Log("Not enough different dates for evaluation.");
            AppLogger.LogWarning("Not enough different dates for evaluation.");
            return;
        }

        var firstDay = groupedByDate[0];
        var thirdDay = groupedByDate[2];

        float avgTrainSR1 = GetAvgSuccessRate(firstDay, "SR85PCTRAIN");
        float avgTrainSR3 = GetAvgSuccessRate(thirdDay, "SR85PCTRAIN");

        float catchSR1 = GetSuccessRate(firstDay, "SR85PCCATCH");
        float catchSR3 = GetSuccessRate(thirdDay, "SR85PCCATCH");

        float avgCB1 = GetAvgControlBound(firstDay, "SR85PCTRAIN");
        float avgCB3 = GetAvgControlBound(thirdDay, "SR85PCTRAIN");

        Debug.Log($"Train SR Day1: {avgTrainSR1}, Train SR Day3: {avgTrainSR3}");
        Debug.Log($"Catch SR Day1: {catchSR1}, Catch SR Day3: {catchSR3}");
        Debug.Log($"CB Day1: {avgCB1}, CB Day3: {avgCB3}");

        if (avgTrainSR3 > avgTrainSR1 && catchSR3 > catchSR1 && avgCB3 < avgCB1)
        {
            DateTime? lastUpdate = GetLastDateFromMechParams();
            if (lastUpdate == null)
            {
                Debug.Log("Mechanism params file not found. Creating new file with default speed.");
                WriteInitialSpeed();
                return;
            }

            var sessionDatesBetween = groupedByDate
                .Where(g => g.Key > lastUpdate.Value.Date && g.Key < DateTime.Today)
                .Select(g => g.Key)
                .Distinct()
                .ToList();

            Debug.Log($"Dates between last update and today: {sessionDatesBetween.Count}");

            if ((DateTime.Today - lastUpdate.Value).Days >= 3 && sessionDatesBetween.Count >= 2)
            {
                if (gameSpeed < PlutoAANController.MAX_SPEED) UpdateGameSpeed();
                else
                {
                    GetLastDateFromMechParams();
                    AppLogger.LogInfo(" Maximum Limit has been reached.");
                }
            }
            else
            {
                Debug.Log("Not enough session activity since last update to warrant game speed change.");
            }
        }
        else
        {
            GetLastDateFromMechParams();
            AppLogger.LogInfo("Game speed not updated. Conditions not met");
            Debug.Log("Conditions for game speed update not met.");
        }
    }

    private float GetAvgSuccessRate(IEnumerable<DataRow> rows, string trialType)
    {
        var selected = rows.Where(r => r.Field<string>("TrialType") == trialType)
                            .Take(4)
                            .Select(r => float.TryParse(r.Field<string>("SuccessRate"), out var sr) ? sr : -1f)
                            .Where(sr => sr >= 0)
                            .ToList();

        return selected.Count > 0 ? selected.Average() : 0;
    }

    private float GetSuccessRate(IEnumerable<DataRow> rows, string trialType)
    {
        return rows.Where(r => r.Field<string>("TrialType") == trialType)
                   .Select(r => float.TryParse(r.Field<string>("SuccessRate"), out var sr) ? sr : -1f)
                   .FirstOrDefault(sr => sr >= 0);
    }

    private float GetAvgControlBound(IEnumerable<DataRow> rows, string trialType)
    {
        var selected = rows.Where(r => r.Field<string>("TrialType") == trialType)
                            .Take(4)
                            .Select(r => float.TryParse(r.Field<string>("CurrentControlBound"), out var cb) ? cb : -1f)
                            .Where(cb => cb >= 0)
                            .ToList();

        return selected.Count > 0 ? selected.Average() : 0;
    }

    private DateTime? GetLastDateFromMechParams()
    {
        if (!File.Exists(mechParamsCsvPath))
            return null;

        DataTable mechData = DataManager.loadCSV(mechParamsCsvPath);

        if (mechData.Rows.Count == 0)
            return null;

        DataRow lastRow = mechData.Rows[mechData.Rows.Count - 1];

        DateTime? lastDate = null;
        float parsedSpeed;

        try
        {
            string dateStr = lastRow["DateTime"].ToString();
            string speedStr = lastRow["Speed"].ToString();

            if (DateTime.TryParseExact(dateStr, DataManager.DATEFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                lastDate = dt;

            if (float.TryParse(speedStr, out parsedSpeed))
            {
                //currSpeed = parsedSpeed;
                gameSpeed = parsedSpeed;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing mechParams: " + ex.Message);
        }

        return lastDate;
    }

    private void WriteInitialSpeed()
    {
        gameSpeed = DefaultMechanismSpeeds[AppData.Instance.selectedMechanism.name];
        using (var writer = new StreamWriter(mechParamsCsvPath, false))
        {
            writer.WriteLine($":Location: {AppData.Instance.userData.GetDeviceLocation()}");
            writer.WriteLine($":Device: PLUTO");
            writer.WriteLine($":User: {AppData.Instance.userData.hospNumber}");
            writer.WriteLine("DateTime,Mode,Speed");
            writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{speedChMode[0]},{gameSpeed}");
            AppLogger.LogInfo($"{AppData.Instance.selectedMechanism.name} - Mech and Game speed initiated to {gameSpeed} deg/sec in {speedChMode[0]}");
        }
    }

    private void UpdateGameSpeed(int mode = 2)
    {
        if (gameSpeed <= 0)
        {
            gameSpeed = DefaultMechanismSpeeds[AppData.Instance.selectedMechanism.name];
        }

        string chMode = speedChMode[mode];
        gameSpeed = gameSpeed * 1.1f;

        using (var writer = new StreamWriter(mechParamsCsvPath, true))
        {
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{chMode},{gameSpeed}");
        }
        AppLogger.LogInfo($"Game speed updated to {gameSpeed} deg/sec in {chMode}");
        Debug.Log($"Game speed updated to: {gameSpeed}");
    }

    public void updateGameSpeedfromGame(float gs, int mode = 1)
    {
        if (gs <= 0)
        {
            gs= DefaultMechanismSpeeds[AppData.Instance.selectedMechanism.name];
        }

        string chMode = speedChMode[mode];

        using (var writer = new StreamWriter(mechParamsCsvPath, true))
        {
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{chMode},{gs}");
        }

        AppLogger.LogInfo($"Game speed updated to {gameSpeed} deg/sec");
        Debug.Log($"Game speed updated to: {gs}");

    }
}



// PLUTO UserData Class
public class PlutoUserData
{
    public DataTable dTableConfig { private set; get; } = null;
    public DataTable dTableSession { private set; get; } = null;
    public string hospNumber { private set; get; }
    public int FME1 { private set; get; }
    public int FME2 { private set; get; }
    public float totalTime{private set; get;}


    public bool rightHand { private set; get; }
    public DateTime startDate { private set; get; }
    public DateTime endDate { private set; get;}
    public Dictionary<string, float> mechMoveTimePrsc { get; private set; } // Prescribed movement time
    public Dictionary<string, float> mechMoveTimePrev { get; private set; } // Previous movement time 
    public Dictionary<string, float> mechMoveTimeCurr { get; private set; } // Current movement time
    public bool isExceeded { private set; get; }
    public const string DATETIME = "DateTime";
    // Total movement times.
    public float totalMoveTimePrsc
    {
        get => mechMoveTimePrsc == null ? -1f : mechMoveTimePrsc.Values.Sum();
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

    // public float totalMoveTimeRemaining
    // {
    //     get
    //     {
    //         float _total = 0f;

    //         if (mechMoveTimePrsc != null && (mechMoveTimePrev == null || mechMoveTimeCurr == null))
    //         {
    //             foreach (string mech in PlutoDefs.Mechanisms)
    //             {
    //                 _total += mechMoveTimePrsc[mech];
    //             }
    //             return _total;
    //         }
    //         else
    //         {
    //             foreach (string mech in PlutoDefs.Mechanisms)
    //             {
    //                 _total += mechMoveTimePrsc[mech] - mechMoveTimePrev[mech] - mechMoveTimeCurr[mech];
    //             }
    //             return _total;
    //         }
    //     }
    // }

    // Constructor

    public int totalMoveTimeRemaining

    {

        get

        {

            float _total = 0f;

            float _Prsc = 0f;

            foreach (string mech in PlutoDefs.Mechanisms)

            {

                _Prsc += mechMoveTimePrsc[mech];

                _total += mechMoveTimePrev[mech] - mechMoveTimeCurr[mech];

            }

            if (_Prsc < _total)

            {

                isExceeded = true;

                _total = (_total - _Prsc);

                return (int)_total;

            }

            else

            {

                isExceeded = false;

                _total = (_Prsc - _total);

                return (int)_total;

            }

        }

    }


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
                .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
                .Where(row => row.Field<string>("Mechanism") == PlutoDefs.Mechanisms[i])
                .Sum(row => 60);
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
            .Select(row => DateTime.ParseExact(row.Field<string>("DateTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture).Date)
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
        foreach (var _gameName in HomerTherapy.GameSpeedIncrements.Keys)
        {
            var rows = dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture).Date == lastUsageDate)
                .Where(row => row.Field<string>("GameName") == _gameName && row.Field<string>("Mechanism") == AppData.Instance.selectedMechanism.name);

            float previousGameSpeed = rows.Any() ? rows.Average(row => Convert.ToSingle(row["GameSpeed"])) : 0f;
            float avgSuccessRate = rows.Any() ? rows.Average(row => Convert.ToSingle(row["SuccessRate"])) : 0f;

            if (avgSuccessRate >= HomerTherapy.SuccessRateThForSpeedIncrement)
            {
                updatedGameSpeeds[_gameName] = previousGameSpeed + HomerTherapy.GameSpeedIncrements[_gameName];
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
                // gameData.gameSpeedPP = game.Value;
            }
            else if (game.Key == "TUK-TUK")
            {
                // gameData.gameSpeedTT = game.Value;
            }
            else if (game.Key == "HAT-Trick")
            {
                // gameData.gameSpeedHT = game.Value;
            }
        }
    }

    private void parseTherapyConfigData()
    {
        DataRow lastRow = dTableConfig.Rows[dTableConfig.Rows.Count - 1];
        hospNumber = lastRow.Field<string>("HomerID");
        rightHand = lastRow.Field<string>("TrainingSide") == "right";
        Debug.Log(lastRow.Field<string>("FME1ID"));
        FME1 = int.Parse(lastRow.Field<string>("FME1ID"));
        FME2 = int.Parse(lastRow.Field<string>("FME2ID"));
        totalTime = float.Parse(lastRow.Field<string>("TotalTime"));
        //AppData.trainingSide = ; // lastRow.Field<string>("TrainingSide");
        startDate = DateTime.ParseExact(lastRow.Field<string>("StartDate"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        endDate = DateTime.ParseExact(lastRow.Field<string>("endDate"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);

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
            .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
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
                .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture).Date == _day)
                .Sum(row => 60);

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

    public List<float> GetLastTwoSuccessRates(string mechanism, string gameName)
    {
        List<float> lastTwoSuccessRates = new List<float>();

        dTableSession = DataManager.loadCSV(DataManager.sessionFile);

        if (dTableSession == null || dTableSession.Rows.Count == 0)
        {
            return new List<float> { 0f, 0f };
        }

        var today = DateTime.Today;

        var filteredRows = dTableSession.AsEnumerable()
            .Where(row =>
                row.Field<string>("Mechanism") == mechanism &&
                row.Field<string>("GameName") == gameName)
            .OrderByDescending(row => DateTime.ParseExact(row.Field<string>("TrialStartTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture))
            .ToList();
        // var successRows = dTableSession.AsEnumerable()
        // .Where(row =>
        //     row.Field<string>("Mechanism") == mechanism &&
        //     row.Field<string>("GameName") == gameName &&
        //     !string.IsNullOrWhiteSpace(row.Field<string>("SuccessRate")))
        // .ToList();

        //     if (successRows.Any())
        //     {
        //         Others.highestSuccessRate = successRows
        //             .Max(row => float.Parse(row.Field<string>("SuccessRate"), CultureInfo.InvariantCulture));
        //             Debug.Log(Others.highestSuccessRate);
        //     }
        //     else
        //     {
        //         Others.highestSuccessRate = 0f; // or float.NaN, or handle as needed
        //     }

        var successRows = dTableSession.AsEnumerable()
        .Where(row =>
            row.Field<string>("Mechanism") == mechanism &&
            row.Field<string>("GameName") == gameName &&
            !string.IsNullOrWhiteSpace(row.Field<string>("SuccessRate")) &&
            !string.IsNullOrWhiteSpace(row.Field<string>("CurrentControlBound")))
        .ToList();

        if (successRows.Any())
        {
            Others.highestSuccessRate = successRows
                .Max(row =>
                {
                    float successRate = float.Parse(row.Field<string>("SuccessRate"), CultureInfo.InvariantCulture);
                    float controlBound = float.Parse(row.Field<string>("CurrentControlBound"), CultureInfo.InvariantCulture);
                    return successRate * (PlutoAANController.MAXCONTROLBOUND - controlBound);
                });
        }
        else
        {
            Others.highestSuccessRate = 0f;
        }


        if (!filteredRows.Any())
        {
            return null;
        }

        // Get all success rates from today
        var todayRates = filteredRows
            .Where(row => DateTime.ParseExact(row.Field<string>("TrialStartTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture).Date == today)
            .Select(row => Convert.ToSingle(row["SuccessRate"]))
            .ToList();

        if (todayRates.Count >= 2)
        {
            lastTwoSuccessRates.Add(todayRates[1]);
            lastTwoSuccessRates.Add(todayRates[0]);
        }
        else if (todayRates.Count == 1)
        {

            var previousDayRate = filteredRows
                .Where(row => DateTime.ParseExact(row.Field<string>("TrialStartTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture).Date < today)
                .Select(row => Convert.ToSingle(row["SuccessRate"]))
                .FirstOrDefault();

            lastTwoSuccessRates.Add(previousDayRate);
            lastTwoSuccessRates.Add(todayRates[0]);

        }
        else
        {
            var previousDayRate = filteredRows
                .Where(row => DateTime.ParseExact(row.Field<string>("TrialStartTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture).Date < today)
                .Select(row => Convert.ToSingle(row["SuccessRate"]))
                .FirstOrDefault();

            lastTwoSuccessRates.Add(previousDayRate);
            lastTwoSuccessRates.Add(0f);
        }

        while (lastTwoSuccessRates.Count < 2)
            lastTwoSuccessRates.Add(0f);

        return lastTwoSuccessRates;
    }

    public void ReadFile()
    {
        if (!File.Exists(DataManager.GetUploadStatusFile))
        {
            Debug.LogError("File not found: " + DataManager.GetUploadStatusFile);
            return;
        }

        string[] lines = File.ReadAllLines(DataManager.GetUploadStatusFile);
        string status;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(',');

            if (parts.Length > 1)
            {
                status = parts[1].Trim(); // second column
                DataManager.setStatus(status);

                if (status == "upload_needed")
                {
                    // dataStatus.text = "Upload needed";
                    Debug.Log("Upload is needed!");
                }
                else if (status == "no_upload")
                {
                    // dataStatus.text = "No upload required";
                    Debug.Log("No upload required.");
                }
                else
                {
                    Debug.Log("Unknown status: " + status);
                }
            }
        }
    }

    public int[] readCummulativeHitsMissesForGameMovement(string gameName, string mech)
    {
        // Get the last row for the given game.
        var lastGameRows = dTableSession.AsEnumerable()?
            .Where(row => row.Field<string>("GameName") == gameName && row.Field<string>("Mechanism") == mech).LastOrDefault();
        // If there are no rows, set the cummulative score to zero.
        if (lastGameRows == null)
        {
            AppLogger.LogInfo($"No previous data found for game '{gameName}' and movement '{mech}'. Cummulative hits and misses set to zero.");
            return new int[] { 0, 0, 0 };
        }
        // Get the cummulative hits and misses for the game from the last row.
        int[] cuScores = new int[]
        {
            Convert.ToInt32(lastGameRows.Field<string>("CummulativeTargets")),
            Convert.ToInt32(lastGameRows.Field<string>("CummulativeHits")),
            Convert.ToInt32(lastGameRows.Field<string>("CummulativeMisses"))
        };
        AppLogger.LogInfo($"Cummulative hits and misses for game '{gameName}' and '{mech}' updated. Targets: {cuScores[0]} | Hits: {cuScores[1]} | Misses: {cuScores[2]}.");
        return cuScores;
    }

    public int[] ReadCumulativeHitsForAllGames()
    {
        string[] games = { "PONG", "TUK", "HAT", "FRUITCH", "RNR" };
        string[] mechanisms = { "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };  // change if different

        int[] cumulativeHitsArray = new int[games.Length];

        for (int i = 0; i < games.Length; i++)
        {
            string gameName = games[i];
            int totalHits = 0;

            foreach (string mech in mechanisms)
            {
                var lastRow = dTableSession.AsEnumerable()?
                    .Where(row => row.Field<string>("GameName") == gameName &&
                                row.Field<string>("Mechanism") == mech)
                    .LastOrDefault();

                if (lastRow != null)
                {
                    totalHits += Convert.ToInt32(lastRow.Field<string>("CummulativeHits"));
                }
            }

            cumulativeHitsArray[i] = totalHits;
            AppLogger.LogInfo($"Game '{gameName}' total cumulative hits from all mechanisms = {totalHits}");
        }

        return cumulativeHitsArray;
    }

    public int[] readStarCounts(string gameName)
    {
        var lastRow = dTableSession.AsEnumerable()?
            .Where(row => row.Field<string>("GameName") == gameName &&
                        row.Field<string>("Mechanism") == AppData.Instance.selectedMechanism.name)
            .LastOrDefault();

        if (lastRow == null)
        {
            AppLogger.LogInfo($"No data found for game '{gameName}' and mechanism '{AppData.Instance.selectedMechanism.name}'. Stars set to 0.");
            return new int[] { 0, 0 };
        }

        int cumulativeStars = Convert.ToInt32(lastRow.Field<string>("CummulativeStars"));
        DateTime today = DateTime.Today;

        int currentStarCount = dTableSession.AsEnumerable()
            .Where(row =>
                row.Field<string>("GameName") == gameName &&
                row.Field<string>("Mechanism") == AppData.Instance.selectedMechanism.name &&
                DateTime.ParseExact(row.Field<string>(DATETIME).Trim(),
                                    DataManager.DATEFORMAT,
                                    CultureInfo.InvariantCulture).Date == today.Date)
            .Sum(row => Convert.ToInt32(row.Field<string>("currentStar")));

        return new int[] { cumulativeStars, currentStarCount };
    }

    public int[] getLastTwoDifferentDatesScore(String gameName)
    {
        // AppData.Instance.reloadSessionDetails();
        var table = AppData.Instance.userData.dTableSession;

        if (table == null || table.Rows.Count == 0)
            return new[] { 0, 0 };

       
        var lastRow = table.Rows[table.Rows.Count - 1];
        DateTime lastDate = DateTime.ParseExact(lastRow.Field<string>(DATETIME),DataManager.DATEFORMAT,CultureInfo.InvariantCulture);
        Debug.Log($"{lastDate}");
    
        //confirms only lastDate and Today data Comparison
        if (lastDate.Date != DateTime.Today.Date)
        {
            int score = GetScoreForDate(lastDate, gameName);
            return new[] { 0,score}; 
        }

        //collect all dates
        List<DateTime> allDates = new List<DateTime>();

        foreach (var row in table.AsEnumerable())
        {
            string dateStr = row.Field<string>(DATETIME);

            if (DateTime.TryParseExact(
                    dateStr,
                    DataManager.DATEFORMAT,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime dt))
            {
                allDates.Add(dt.Date);
            }
        }

        if (allDates.Count == 0)
            return new[] { 0, 0 };

        var distinctDates = allDates
            .Distinct()
            .OrderByDescending(d => d)
            .Take(2)
            .ToList();

        // If there is only ONE unique date:
        if (distinctDates.Count == 1)
            return new[] { GetScoreForDate(distinctDates[0],gameName), 0 };

        DateTime date1 = distinctDates[0]; // today date
        DateTime date2 = distinctDates[1]; // yesterday date

        int score1 = GetScoreForDate(date1, gameName);
        int score2 = GetScoreForDate(date2, gameName);
        Debug.Log($"{score1} - {date1},{score2} - {date2} from getfuntion");
        return new[] { score1, score2 };
    }

    private int GetScoreForDate(DateTime targetDate, string gameName)
    {
        var table = AppData.Instance.userData.dTableSession;

        int total = table.AsEnumerable()
            .Where(row =>
                DateTime.ParseExact(row.Field<string>(DATETIME),
                                    DataManager.DATEFORMAT,
                                    CultureInfo.InvariantCulture).Date == targetDate.Date &&
                row.Field<string>("GameName") == gameName &&
                row.Field<string>("Mechanism") == AppData.Instance.selectedMechanism.name
            )
            .Sum(row => Convert.ToInt32(row["CurrentHits"]));

        return total;
    }



    public class GameStats
    {
        public string GameName;
        public int CumulativeHits;    
        public int PreviousDayHits; 
        public int TodayHits; 
    }

    public List<GameStats> ReadGameStats()
    {
        string[] games = { "PONG", "TUK", "HAT", "FRUITCH", "RNR" };
        string[] mechanisms = { "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };

        DateTime today = DateTime.Today;
        DateTime previousDay = today.AddDays(-1);

        List<GameStats> result = new List<GameStats>();

        foreach (string gameName in games)
        {
            int cumulativeHits = 0;
            int todayHits = 0;
            int previousDayHits = 0;

            foreach (string mech in mechanisms)
            {
                var lastRow = dTableSession.AsEnumerable()
                    .Where(row =>
                        row.Field<string>("GameName") == gameName &&
                        row.Field<string>("Mechanism") == mech)
                    .LastOrDefault();

                if (lastRow != null)
                {
                    cumulativeHits += Convert.ToInt32(lastRow["CummulativeHits"]);
                }

                todayHits += dTableSession.AsEnumerable()
                    .Where(row =>
                        row.Field<string>("GameName") == gameName &&
                        row.Field<string>("Mechanism") == mech &&
                        DateTime.ParseExact(row.Field<string>(DATETIME).Trim(),
                            DataManager.DATEFORMAT,
                            CultureInfo.InvariantCulture).Date == today)
                    .Sum(row => Convert.ToInt32(row["CurrentHits"]));

                previousDayHits += dTableSession.AsEnumerable()
                    .Where(row =>
                        row.Field<string>("GameName") == gameName &&
                        row.Field<string>("Mechanism") == mech &&
                        DateTime.ParseExact(row.Field<string>(DATETIME).Trim(),
                            DataManager.DATEFORMAT,
                            CultureInfo.InvariantCulture).Date == previousDay)
                    .Sum(row => Convert.ToInt32(row["CurrentHits"]));
            }

            result.Add(new GameStats
            {
                GameName = gameName,
                CumulativeHits = cumulativeHits,
                PreviousDayHits = previousDayHits,
                TodayHits = todayHits
            });

            AppLogger.LogInfo(
                $"GAME: {gameName} | Cumulative={cumulativeHits} | Yesterday={previousDayHits} | Today={todayHits}");
        }

        return result;
    }
    public class MechanismStats
{
    public string Mechanism;
    public int TodayStars;
    public int YesterdayStars;
    public int CumulativeStars;
    public int CumulativeStarsYesterday;

}
    public List<MechanismStats> ReadMechanismStarStats()
    {
        string[] mechanisms = { "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };
        List<MechanismStats> results = new List<MechanismStats>();

        DateTime today = DateTime.Today;
        DateTime yesterday = today.AddDays(-1);

        // ⭐ TOTAL stars of ALL mechanisms (till today)
        int cumulativeStarsAll = dTableSession.AsEnumerable()
            .Sum(r => Convert.ToInt32(r["currentStar"]));
        
        // ⭐ Cumulative stars till yesterday (ACROSS ALL mechanisms)
        int cumulativeUntilYesterday = dTableSession.AsEnumerable()
            .Where(r => DateTime.ParseExact(
                    r.Field<string>("DateTime"), DataManager.DATEFORMAT, null).Date <= yesterday)
            .Sum(r => Convert.ToInt32(r["currentStar"]));

        foreach (string mech in mechanisms)
        {
            // ⭐ Today stars for this mechanism
            int todayStars = dTableSession.AsEnumerable()
                .Where(r =>
                    r.Field<string>("Mechanism") == mech &&
                    DateTime.ParseExact(r.Field<string>("DateTime"),
                        DataManager.DATEFORMAT, null).Date == today)
                .Sum(r => Convert.ToInt32(r["currentStar"]));

            // ⭐ Yesterday stars for this mechanism
            int yStars = dTableSession.AsEnumerable()
                .Where(r =>
                    r.Field<string>("Mechanism") == mech &&
                    DateTime.ParseExact(r.Field<string>("DateTime"),
                        DataManager.DATEFORMAT, null).Date == yesterday)
                .Sum(r => Convert.ToInt32(r["currentStar"]));

            results.Add(new MechanismStats
            {
                Mechanism = mech,
                TodayStars = todayStars,
                YesterdayStars = yStars,

                // ⭐ SAME for ALL mechanisms
                CumulativeStars = cumulativeStarsAll,
                CumulativeStarsYesterday = cumulativeUntilYesterday
            });
        }

        return results;
    }
}



public static class ConfigData
{
    // =========================
    // STATIC DATA (loaded once)
    // =========================
    public static string HomerID;
    public static string StartDate;
    public static string EndDate;
    public static string TrainingSide;
    public static string Location;
    public static string Group;

    // =========================
    // EDITABLE DATA (runtime)
    // =========================
    public static int WFE;
    public static int WURD;
    public static int FPS;
    public static int HOC;

    public static int FME1Time;
    public static int FME2Time;

    public static int FME1ID = -1;
    public static int FME2ID = -1;

    public static int TotalTime;

    private static DataTable cachedTable;

    // =========================
    // LOAD FROM CSV
    // =========================
    public static void LoadFromConfig(string path)
    {
        cachedTable = DataManager.loadCSV(path);

        if (cachedTable == null || cachedTable.Rows.Count == 0)
        {
            Debug.LogError("Config file empty or missing");
            return;
        }

        DataRow row = cachedTable.Rows[cachedTable.Rows.Count - 1];

        // STATIC DATA (never change)
        HomerID = row["HomerID"].ToString();
        StartDate = row["StartDate"].ToString();
        EndDate = row["EndDate"].ToString();
        TrainingSide = row["TrainingSide"].ToString();
        Location = row["Location"].ToString();
        Group = row["Group"].ToString();
        
        // EDITABLE DATA
        int.TryParse(row["WFE"].ToString(), out WFE);
        int.TryParse(row["WURD"].ToString(), out WURD);
        int.TryParse(row["FPS"].ToString(), out FPS);
        int.TryParse(row["HOC"].ToString(), out HOC);

        int.TryParse(row["FME1"].ToString(), out FME1Time);
        int.TryParse(row["FME2"].ToString(), out FME2Time);

        int.TryParse(row["FME1ID"].ToString(), out FME1ID);
        int.TryParse(row["FME2ID"].ToString(), out FME2ID);

        CalculateTotalTime();

        Debug.Log("Config loaded into session");
        
    Debug.Log($"Config loaded: FME1ID={FME1ID}, FME2ID={FME2ID}");
    }

    // =========================
    // UPDATE METHODS
    // =========================
    public static void SetFME1(int index)
    {
        if (index == FME2ID)
        {
            Debug.LogWarning("FME1 cannot be same as FME2");
            return;
        }

        FME1ID = index;
    }

    public static void SetFME2(int index)
    {
        if (index == FME1ID)
        {
            Debug.LogWarning("FME2 cannot be same as FME1");
            return;
        }

        FME2ID = index;
    }

    public static void SetTimes(int wfe, int wurd, int fps, int hoc, int fme1, int fme2)
    {
        WFE = wfe;
        WURD = wurd;
        FPS = fps;
        HOC = hoc;
        FME1Time = fme1;
        FME2Time = fme2;

        CalculateTotalTime();
    }

    private static void CalculateTotalTime()
    {
        TotalTime = WFE + WURD + FPS + HOC + FME1Time + FME2Time;
    }

    // =========================
    // SAVE TO CSV
    // =========================
    public static void SaveToConfig(string path)
    {
        if (cachedTable == null || cachedTable.Rows.Count == 0)
        {
            Debug.LogError("No cached config to save");
            return;
        }

        // Create a new row instead of modifying the last one
        DataRow newRow = cachedTable.NewRow();

        // Copy static data from last row
        DataRow lastRow = cachedTable.Rows[cachedTable.Rows.Count - 1];
        newRow["HomerID"] = lastRow["HomerID"];
        newRow["StartDate"] = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"); // Update to current date
        newRow["EndDate"] = lastRow["EndDate"];
        newRow["TrainingSide"] = lastRow["TrainingSide"];
        newRow["Location"] = lastRow["Location"];
        newRow["Group"] = lastRow["Group"];

        // Set editable values
        newRow["WFE"] = WFE.ToString();
        newRow["WURD"] = WURD.ToString();
        newRow["FPS"] = FPS.ToString();
        newRow["HOC"] = HOC.ToString();
        newRow["FME1"] = FME1Time.ToString();
        newRow["FME2"] = FME2Time.ToString();

        newRow["FME1ID"] = FME1ID.ToString();
        newRow["FME2ID"] = FME2ID.ToString();

        newRow["TotalTime"] = TotalTime.ToString();

        // Add new row to table
        cachedTable.Rows.Add(newRow);

        DataManager.saveCSV(cachedTable, path);

        Debug.Log("Config saved as new row in session");
    }

    // =========================
    // RESET (optional)
    // =========================
    public static void ResetSession()
    {
        FME1ID = -1;
        FME2ID = -1;
        WFE = WURD = FPS = HOC = 0;
        FME1Time = FME2Time = 0;
        TotalTime = 0;
    }
}

public class PlutoGame
{
    public string name { get; private set; } = null;
    public string mech { get; set; } = null;
    public float gameSpeed { get; private set; }
    public float gameDuration { get; set; } = 0f;
    // public MarsArom arom { get; private set; } = null;
    public int currentTargets { get; private set; } = 0;
    public int currentHits { get; private set; } = 0;
    public int currentMisses { get; private set; } = 0;
    public int cummulativeTargets { get; private set; } = 0;
    public int cummulativeHits { get; private set; } = 0;
    public int cummulativeMisses { get; private set; } = 0;
    public int cummulativeStars {  get; private set; } = 0;
    public int currentStar {  get; private set; } = 0;
    public int todayStar {  get; private set; } = 0;
    public PlutoGame(string gName, string mName, int gCuTargets, int gCuHits, int gCuMisses, int gCuStars,int TodayStars)
    {
        name = gName?.ToUpper() ?? string.Empty;
        mech = mName?.ToUpper() ?? string.Empty;
        cummulativeTargets = gCuTargets;
        cummulativeHits = gCuHits;
        cummulativeMisses = gCuMisses;
        cummulativeStars = gCuStars;
        todayStar = TodayStars;
    }

    public void ResetCummulativeScore()
    {
        cummulativeTargets = 0;
        cummulativeHits = 0;
        cummulativeMisses = 0;
    }
    public void updateCummulativeStars()
    {
       
        cummulativeStars++;
        currentStar = 1;
        todayStar += currentStar;
    }
    //Reset the trailStar Count
    public void resetstarCount()
    {
        currentStar = 0;
    }
   
    //To check if they achieved Today  or not
    public bool isAchievedToday()
    {
        return todayStar > 0 ;
    }
    

    public void UpdateTargetsHitsMisses(int targets, int hits, int misses)
    {
        currentTargets = targets;
        currentHits = hits;
        currentMisses = misses;
        cummulativeTargets += targets;
        cummulativeHits += hits;
        cummulativeMisses += misses;
    }
}


public static class MovementTracker
{
    private static Vector3 previousPlayerPosition;
    private static Coroutine movementCoroutine;
    private static float playerMovementTime = 0f;
    private static MonoBehaviour coroutineRunner; // To run coroutines from a static class

    public static float PlayerMovementTime => playerMovementTime; // Public getter

    private static bool isInitialized = false;
    private static bool isMoving = false;

    public static void Initialize(MonoBehaviour runner, Vector3 startPosition)
    {
        if (!isInitialized)
        {
            coroutineRunner = runner;
            playerMovementTime = 0f;
            previousPlayerPosition = startPosition;
            isInitialized = true;
            movementCoroutine = coroutineRunner.StartCoroutine(TrackMovementTime()); // Start immediately
        }
    }

    public static void UpdatePosition(Vector3 currentPosition)
    {
        if (!isInitialized)
            return;

        float playerDistanceMoved = Vector3.Distance(currentPosition, previousPlayerPosition);
        isMoving = playerDistanceMoved > 0.001f;
        previousPlayerPosition = currentPosition;
    }

    private static IEnumerator TrackMovementTime()
    {
        while (true)
        {
            if (isMoving)
            {
                playerMovementTime += Time.deltaTime;
            }
            yield return null;
        }
    }

}

public static class GameFuncs
{
    //Game Achievement Data
        public static int[] GetScores()
        {
            return AppData.Instance.userData.getLastTwoDifferentDatesScore(AppData.Instance.selectedGameName);
        }

        public static int[] GetStarsCount()
        {
            return AppData.Instance.userData.readStarCounts(AppData.Instance.selectedGameName);
        }

        public static int[] GetCummulativeScores()
        {
            return AppData.Instance.userData.readCummulativeHitsMissesForGameMovement(AppData.Instance.selectedGameName, AppData.Instance.selectedMechanism.name);
        }

        public static bool IsAchievedToday()
        {
            var starsCount = GetStarsCount();
            return starsCount[1] > 0;
        }
}
public static class Others
{
    public static float gameTime = 0f;
    public static float highestSuccessRate = 0f;
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
    public bool apromCompleted { get; private set; }
    public ROM oldRom { get; private set; }
    public ROM newRom { get; private set; }
    public ROM currRom { get => newRom.isSet ? newRom : (oldRom.isSet ? oldRom : null); }
    public float currSpeed { get; private set; } = -1f;
    // Trial details for the mechanism.
    public int trialNumberDay { get; private set; }
    public int trialNumberSession { get; private set; }
    

    public PlutoMechanism(string name, string side, int sessno)
    {
        this.name = name?.ToUpper() ?? string.Empty;
        this.side = side;
        oldRom = new ROM(this.name);
        newRom = new ROM();
        promCompleted = false;
        aromCompleted = false;
        apromCompleted = false;
        this.side = side;
        currSpeed = -1f;
        UpdateTrialNumbers(sessno);
    }

    public bool IsMechanism(string mechName) => string.Equals(name, mechName, StringComparison.OrdinalIgnoreCase);

    public bool IsSide(string sideName) => string.Equals(side, sideName, StringComparison.OrdinalIgnoreCase);

    public bool IsSpeedUpdated() => currSpeed > 0;

    public void NextTrail()
    {
        trialNumberDay += 1;
        trialNumberSession += 1;
    }

    public float[] CurrentArom => currRom == null ? null : new float[] { currRom.aromMin, currRom.aromMax };
    public float[] CurrentProm => currRom == null ? null : new float[] { currRom.promMin, currRom.promMax };
    public float[] CurrentAProm => currRom == null ? null : new float[] { currRom.apromMin, currRom.apromMax };
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
    public void ResetAPromValues()
    {
        newRom.SetAProm(0, 0);
        apromCompleted = false;
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

public void SetNewAPromValues(float apmin, float apmax)
    {
        newRom.SetAProm(apmin, apmax);
        if (apmin != 0 || apmax != 0) apromCompleted = true;
    }

    public void SetAromCPM(bool value)
    {
        newRom.SetCPM(value);
    }

    public void SaveAssessmentData()
    {
        if (promCompleted && aromCompleted && apromCompleted)
        {
            // Save the new ROM values to the file.
            newRom.WriteToAssessmentFile();
        }
    }
    /*
     * Function to update the trial numbers for the day and session for the mechanism for today.
     */
    public void UpdateTrialNumbers(int sessno)
    {
        // Get the last row for the today, for the selected mechanism.
        var selRows = AppData.Instance.userData.dTableSession.AsEnumerable()?
            .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
            .Where(row => row.Field<string>("Mechanism") == this.name);

        // Check if the selected rows is null.
        if (selRows.Count() == 0)
        {
            // Set the trial numbers to 1.
            trialNumberDay = 0;
            trialNumberSession = 0;
            return;
        }
        // Get the trial number as the maximum number for the trialNumber Day.
        trialNumberDay = selRows.Max(row => Convert.ToInt32(row.Field<string>("TrialNumberDay")));

        // Now let's get the session number for the current session.
        selRows = AppData.Instance.userData.dTableSession.AsEnumerable()?
            .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), DataManager.DATEFORMAT, CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
            .Where(row => Convert.ToInt32(row.Field<string>("SessionNumber")) == sessno)
            .Where(row => row.Field<string>("Mechanism") == this.name);
        if (selRows.Count() == 0)
        {
            // Set the trial numbers to 1.
            trialNumberSession = 0;
            return;
        }
        // Get the maximum trial number for the session.
        UnityEngine.Debug.Log(selRows.Count());
        trialNumberSession = selRows.Max(row => Convert.ToInt32(row.Field<string>("TrialNumberSession")));
    }
}

public class ROM
{
public static string[] FILEHEADER = new string[] {
        "DateTime", "PromMin", "PromMax", "AromMin", "AromMax","APromMin","APromMax", "CPM"
    };
    // Class attributes to store data read from the file
    public string datetime;
    public float promMin { get; private set; }
    public float promMax { get; private set; }
    public float aromMin { get; private set; }
    public float aromMax { get; private set; }
    public float apromMin { get; private set; }
    public float apromMax { get; private set; }
    public bool cpm { get; private set; }
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
            apromMin = 0;
            apromMax = 0;
            cpm = false;
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
        apromMin = 0;
        apromMax = 0;
        cpm = false;
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
public void SetAProm(float min, float max)
    {
        apromMin = min;
        apromMax = max;
        datetime = DateTime.Now.ToString();
    }

    public void SetCPM(bool value)
    {
        cpm = value;
    }


public void WriteToAssessmentFile()
    {
        string fileName = DataManager.GetRomFileName(mechanism); ;
        using (StreamWriter file = new StreamWriter(fileName, true))
        {
            file.WriteLine(string.Join(",", new string[] { datetime, promMin.ToString(), promMax.ToString(), aromMin.ToString(), aromMax.ToString(), apromMin.ToString(), apromMax.ToString(), cpm.ToString() }));
        }
    }

    private void ReadFromFile(string mechanismName)
    {
        string fileName = DataManager.GetRomFileName(mechanismName);

        if (!File.Exists(fileName))
        {
            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                // Write the preheader details
                writer.WriteLine($":Location: {AppData.Instance.userData.GetDeviceLocation()}");
                writer.WriteLine($":Device: PLUTO");
                writer.WriteLine($":User: {AppData.Instance.userData.hospNumber}");
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
            apromMin = 0;
            apromMax = 0;
            cpm = false;
            return;
        }
        // Assign ROM from the last row.
        datetime = romData.Rows[romData.Rows.Count - 1].Field<string>("DateTime");
        mechanism = mechanismName;
        promMin = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("PromMin"));
        promMax = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("PromMax"));
        aromMin = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("AromMin"));
        aromMax = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("AromMax"));
apromMin = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("APromMin"));
        apromMax = float.Parse(romData.Rows[romData.Rows.Count - 1].Field<string>("APromMax"));

        // Try to read CPM column (handle backward compatibility if column doesn't exist)
        try
        {
            string cpmStr = romData.Rows[romData.Rows.Count - 1].Field<string>("CPM");
            cpm = !string.IsNullOrEmpty(cpmStr) && bool.TryParse(cpmStr, out var result) && result;
        }
        catch
        {
            // Column doesn't exist, set CPM based on AROM range: true if ≤5 degrees
            float aromRange = Mathf.Abs(aromMax - aromMin);
            cpm = aromRange <= 5f;
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
