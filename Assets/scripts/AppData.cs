
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
using NeuroRehabLibrary;

public static class PlutoDefs
{
    public static readonly string[] Mechanisms = new string[] { "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };

    public static int getMechanimsIndex(string mech)
    {
        return Array.IndexOf(Mechanisms, mech);
    }
}

public static class AppData
{
    // COM Port for the device
    public static readonly string COMPort = "COM4";

    //Options to drive 
    public static string selectMechanism = null;
    public static string selectedGame = null;
    public static int currentSessionNumber;
    public static string trialDataFileLocation;

    public static void initializeStuff()
    {
        DataManager.createFileStructure();
        ConnectToRobot.Connect(AppData.COMPort);
        UserData.readAllUserData();
    
    }

    // UserData Class
    public static class UserData
    {
        public static DataTable dTableConfig = null;
        public static DataTable dTableSession = null;
        public static string hospNumber;
        public static DateTime startDate;
        public static Dictionary<string, float> mechMoveTimePrsc { get; private set; } // Prescribed movement time
        public static Dictionary<string, float> mechMoveTimePrev { get; private set; } // Previous movement time 
        public static Dictionary<string, float> mechMoveTimeCurr { get; private set; } // Current movement time

        // Total movement times.
        public static float totalMoveTimePrsc
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

        public static float totalMoveTimePrev
        {
            get
            {
                if (!File.Exists(DataManager.filePathSessionData))
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

        public static float totalMoveTimeCurr
        {
            get
            {
                if (!File.Exists(DataManager.filePathSessionData))
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

        public static float totalMoveTimeRemaining
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
                else {
                    foreach (string mech in PlutoDefs.Mechanisms)
                    {
                        _total += mechMoveTimePrsc[mech] - mechMoveTimePrev[mech] - mechMoveTimeCurr[mech];
                    }
                    return _total;
                }

               
            }
        }
        public static void readAllUserData()
        {
            if (File.Exists(DataManager.filePathConfigData))
            {
                dTableConfig = DataManager.loadCSV(DataManager.filePathConfigData);
            }
            if (File.Exists(DataManager.filePathSessionData))
            {
                dTableSession = DataManager.loadCSV(DataManager.filePathSessionData);
            }
            mechMoveTimeCurr = createMoveTimeDictionary();
            // Read the therapy configuration data.
            parseTherapyConfigData();
            if (File.Exists(DataManager.filePathSessionData))
            {
                parseMechanismMoveTimePrev();
            }
        }
        private static Dictionary<string, float> createMoveTimeDictionary()
        {
            Dictionary<string, float> _temp = new Dictionary<string, float>();
            for (int i = 0; i < PlutoDefs.Mechanisms.Length; i++)
            {
                _temp.Add(PlutoDefs.Mechanisms[i], 0f);
            }
            return _temp;
        }

        public static float getRemainingMoveTime(string mechanism)
        {
            return mechMoveTimePrsc[mechanism] - mechMoveTimePrev[mechanism] - mechMoveTimeCurr[mechanism];
        }

        public static float getTodayMoveTimeForMechanism(string mechanism)
        {
            if ((mechMoveTimePrev == null || mechMoveTimeCurr == null))
            {
                return 0f;
            }
            else 
            {
                return mechMoveTimePrev[mechanism] + mechMoveTimeCurr[mechanism];
            }
            
        }
        public static int getCurrentDayOfTraining()
        {
            TimeSpan duration = DateTime.Now - startDate;
            return (int)duration.TotalDays;
        }

        private static void parseMechanismMoveTimePrev()
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

        private static void parseTherapyConfigData()
        {
            DataRow lastRow = dTableConfig.Rows[dTableConfig.Rows.Count - 1];
            hospNumber = lastRow.Field<string>("hospno");
            startDate = DateTime.ParseExact(lastRow.Field<string>("startdate"), "dd-MM-yyyy", CultureInfo.InvariantCulture);
            mechMoveTimePrsc = createMoveTimeDictionary();//prescribed time
            for (int i = 0; i < PlutoDefs.Mechanisms.Length; i++)
            {
                mechMoveTimePrsc[PlutoDefs.Mechanisms[i]] = float.Parse(lastRow.Field<string>(PlutoDefs.Mechanisms[i]));
            }
        }

        // Returns today's total movement time in minutes.
        public static float getPrevTodayMoveTime()
        {
            var _totalMoveTimeToday = dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
                .Sum(row => Convert.ToInt32(row["MoveTime"]));
            Debug.Log(_totalMoveTimeToday);
            return _totalMoveTimeToday / 60f;
        }
        public static DaySummary[] CalculateMoveTimePerDay(int noOfPastDays = 7)
        {
            // Check if the session file has been loaded and has rows
            if (dTableSession == null || dTableSession.Rows.Count == 0)
            {
                Debug.LogWarning("Session data is not available or the file is empty.");
                return new DaySummary[0]; // Return an empty array if no data is found
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

                // Populate the day summary
                daySummaries[i - 1] = new DaySummary
                {
                    Day = Miscellaneous.GetAbbreviatedDayName(_day.DayOfWeek),
                    Date = _day.ToString("dd/MM"),
                    MoveTime = _moveTime / 60f // Convert move time to minutes
                };

                Debug.Log($"{i} | {daySummaries[i - 1].Day} | {daySummaries[i - 1].Date} | {daySummaries[i - 1].MoveTime}");
            }

            return daySummaries;
        }
    }
}

public static class Miscellaneous
{
    public static string GetAbbreviatedDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek.ToString().Substring(0, 3);
    }
}

public class MechanismData
{
    // Class attributes to store data read from the file
    public string datetime;
    public string side;
    public float tmin;
    public float tmax;
    public string mech;
    public string filePath = DataManager.directoryMechData;

    // Constructor that reads the file and initializes values based on the mechanism
    public MechanismData(string mechanismName)
    {
        string lastLine = "";
        string[] values;
        string fileName = $"{filePath}/{mechanismName}.csv";
        Debug.Log(fileName);

        try
        {
            using (StreamReader file = new StreamReader(fileName))
            {
                while (!file.EndOfStream)
                {
                    lastLine = file.ReadLine();
                }
            }
            values = lastLine.Split(','); 
            if (values[0].Trim() != null)
            {
                // Assign values if mechanism matches
                datetime = values[0].Trim();
                side = values[1].Trim();
                tmin = float.Parse(values[2].Trim());
                tmax = float.Parse(values[3].Trim());
                mech = mechanismName;
            }
            else
            {
                // Handle case when no matching mechanism is found
                datetime = null;
                side = null;
                tmin = 0;
                tmax = 0;
                mech = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading the file: " + ex.Message);
        }
    }
    public (float tmin, float tmax) GetTminTmax()
    {
        return (tmin, tmax);
    }
}

/* Application Level Logger Class */
public enum LogMessageType
{
    INFO,
    WARNING,
    ERROR
}

public static class AppLogger
{
    private static string logFilePath;
    private static StreamWriter logWriter = null;
    private static readonly object logLock = new object();
    public static string currentScene { get; private set; } = "";
    public static string currentMechanism { get; private set; } = "";
    public static string currentGame { get; private set; } = "";

    public static bool isLogging
    {
        get
        {
            return logFilePath != null;
        }
    }

    public static void StartLogging(string scene)
    {
        // Start Log file only if we are not already logging.
        if (isLogging)
        {
            return;
        }
        string logDirectory = Path.Combine(DataManager.directoryPath, "applog");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
        logFilePath = Path.Combine(logDirectory, $"log-{DateTime.Now:dd-MM-yyyy-HH-mm-ss}.log");
        if (!File.Exists(logFilePath))
        {
            using (File.Create(logFilePath))
            {
                Debug.Log("created");
            }
        }
        logWriter = new StreamWriter(logFilePath, true);
        currentScene = scene;
        LogInfo("Created PLUTO log file.");
    }

    public static void SetCurrentScene(string scene)
    {
        if (isLogging)
        {
            currentScene = scene;
        }
    }

    public static void SetCurrentMechanism(string mechanism)
    {
        if (isLogging)
        {
            currentMechanism = mechanism;
        }
    }

    public static void SetCurrentGame(string game)
    {
        if (isLogging)
        {
            currentGame = game;
        }
    }

    public static void StopLogging()
    {
        if (logWriter != null)
        {
            LogInfo("Closing log file.");
            logWriter.Close();
            logWriter = null;
            logFilePath = null;
            currentScene = "";
        }
    }

    public static void LogMessage(string message, LogMessageType logMsgType)
    {
        lock (logLock)
        {
            if (logWriter != null)
            {
                logWriter.WriteLine($"{DateTime.Now:dd-MM-yyyy HH:mm:ss} {logMsgType,-7} [{currentScene}] [{currentMechanism}] [{currentGame}] {message}");
                logWriter.Flush();
            }
        }
    }

    public static void LogInfo(string message)
    {
        LogMessage(message, LogMessageType.INFO);
    }

    public static void LogWarning(string message)
    {
        LogMessage(message, LogMessageType.WARNING);
    }

    public static void LogError(string message)
    {
        LogMessage(message, LogMessageType.ERROR);
    }
}


public static class gameData
{
    //game
    public static bool isGameLogging;
    public static string game;
    public static int gameScore;
    public static int reps;
    public static int playerScore;
    public static int enemyScore;
    public static string playerPos = "0";
    public static string enemyPos;
    public static string playerHit = "0";
    public static string enemyHit = "0";
    public static string wallBounce = "0";
    public static string enemyFail = "0";
    public static string playerFail = "0";
    public static int winningScore = 3;
    public static float moveTime;
    public static readonly string[] pongEvents = new string[] { "moving", "wallBounce", "playerHit", "enemyHit", "playerFail", "enemyFail" };
    public static int events;
}