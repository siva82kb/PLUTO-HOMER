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
        { "HAT-Trick", 1f }
    };
    
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

    private string mechanismToCheck;

    private DataTable sessionTable;
    private string mechParamsCsvPath;
    private static readonly string[] speedChMode = new string[] { "manual", "automatic" };
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
        this.mechanismToCheck = AppData.Instance.selectedMechanism.name;
        this.sessionTable = AppData.Instance.userData.dTableSession;
        this.mechParamsCsvPath = DataManager.GetMechFileName(AppData.Instance.selectedMechanism.name);
    }

    public void setGameSpeed(float gs)
    {
        gameSpeed = gs;
}
    public void EvaluateAndUpdateGameSpeed()
    {
        if (!File.Exists(mechParamsCsvPath))
        {
            WriteInitialSpeed();
            return;
        }
        var mechData = sessionTable.AsEnumerable()
            .Where(row => row.Field<string>("Mechanism") == mechanismToCheck)
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
                UpdateGameSpeed();
            }
            else
            {
                Debug.Log("Not enough session activity since last update to warrant game speed change.");
            }
        }
        else
        {
            GetLastDateFromMechParams();
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
        gameSpeed = DefaultMechanismSpeeds[mechanismToCheck];
        using (var writer = new StreamWriter(mechParamsCsvPath, false))
        {
            writer.WriteLine("DateTime,Mode,Speed");
            writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},Default,{gameSpeed}");
        }
    }

    private void UpdateGameSpeed(int mode = 1)
    {
        if (gameSpeed <= 0)
        {
            gameSpeed = DefaultMechanismSpeeds[mechanismToCheck];
        }

        string chMode = speedChMode[mode];
        gameSpeed = gameSpeed * 1.1f;

        using (var writer = new StreamWriter(mechParamsCsvPath, true))
        {
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{chMode},{gameSpeed}");
        }

        Debug.Log($"Game speed updated to: {gameSpeed}");
    }

    public void updateGameSpeedfromGame(float gs, int mode = 0)
    {
        if (gs <= 0)
        {
            gs= DefaultMechanismSpeeds[mechanismToCheck];
        }

        string chMode = speedChMode[mode];

        using (var writer = new StreamWriter(mechParamsCsvPath, true))
        {
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{chMode},{gs}");
        }

        Debug.Log($"Game speed updated to: {gs}");

    }
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

            Debug.Log(Others.highestSuccessRate);
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
        "DateTime", "PromMin", "PromMax", "AromMin", "AromMax","APromMin","APromMax"
    };
    // Class attributes to store data read from the file
    public string datetime;
    public float promMin { get; private set; }
    public float promMax { get; private set; }
    public float aromMin { get; private set; }
    public float aromMax { get; private set; }
    public float apromMin { get; private set; }
    public float apromMax { get; private set; }
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


    public void WriteToAssessmentFile()
    {
        string fileName = DataManager.GetRomFileName(mechanism); ;
        using (StreamWriter file = new StreamWriter(fileName, true))
        {
            file.WriteLine(string.Join(",", new string[] { datetime, promMin.ToString(), promMax.ToString(), aromMin.ToString(), aromMax.ToString(), apromMin.ToString(), apromMax.ToString() }));
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
            apromMin = 0;
            apromMax = 0;
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
