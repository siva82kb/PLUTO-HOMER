
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
using System.Text;
using XCharts.Runtime;

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
    public static string selectedMechanism = null;
    public static string selectedGame = null;
    public static int currentSessionNumber;
    public static string trialDataFileLocation;

    //change true to run game from choosegamescene
    public static bool runIndividualGame = false;
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
                    Day = Miscellaneous.GetAbbreviatedDayName(_day.DayOfWeek),
                    Date = _day.ToString("dd/MM"),
                    MoveTime = _moveTime / 60f 
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
    public static string enemyPos="0";
    public static string playerHit = "0";
    public static string enemyHit = "0";
    public static string wallBounce = "0";
    public static string enemyFail = "0";
    public static string playerFail = "0";
    public static int winningScore = 3;
    public static float moveTime;
    public static readonly string[] pongEvents = new string[] { "moving", "wallBounce", "playerHit", "enemyHit", "playerFail", "enemyFail" };
    public static int events=0;
    public static string TargetPos;
    private static DataLogger dataLog;
    private static string[] gameHeader = new string[] {
        "time","controltype","error","buttonState","angle","control",
        "target","playerPosY","enemyPosY","events","playerScore","enemyScore"
    };
    public static bool isLogging { get; private set; }
    static public void StartDataLog(string fname)
    {
        if (dataLog != null)
        {
            StopLogging();
        }
        // Start new logger
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
    static public void StopLogging()
    {
        if (dataLog != null)
        {
            Debug.Log("Null log not");
            dataLog.stopDataLog(true);
            dataLog = null;
            isLogging = false;
        }
        else
            Debug.Log("Null log");
    }

    static public void LogData()
    {
        // Log only if the current data is SENSORSTREAM
        if (PlutoComm.SENSORNUMBER[PlutoComm.dataType] == 4)
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