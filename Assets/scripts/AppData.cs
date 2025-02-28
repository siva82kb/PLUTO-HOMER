
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
using NeuroRehabLibrary;
using System.Text;
using XCharts.Runtime;
using System.Diagnostics;
using UnityEngine;
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

    static public float[] offsetAtNeutral = new float[] { 68, 68, 90, 0, 90 , 90  };

    // Old and new PROM
    public static ROM oldPROM;
    public static ROM newPROM;

    public static ROM oldAROM;
    public static ROM newAROM;

    public static float[] aRomValue=new float[2];
    public static float[] pRomValue =new float[2];
    //temp storage for PROM min and max

    public static float promTmin=0f;
    public static float promTmax=0f;

    //// Counts to keep track of time for different GUI updatess
    //public static int[] count = new int[] { 0, 0 };
    //private static int[] _Th;
    //public static int[] Th
    //{
    //    get { return new int[] { 10, 50, }; }
    //}
    // Keeping track of time.
    static private double nanosecPerTick = 1.0 / Stopwatch.Frequency;
    static private Stopwatch stp_watch = new Stopwatch();
    static public double CurrentTime
    {
        get { return stp_watch.ElapsedTicks * nanosecPerTick; }
    }
    //Options to drive 
    public static string trainingSide = null;
    public static string selectedMechanism=null;
    public static string selectedGame = null;
    //handling the data
    public static int currentSessionNumber;
    public static string trialDataFileLocation;
    public static string trialDataFileLocation1;


    //change true to run game from choosegamescene
    public static bool runIndividualGame = false;
    public static void initializeStuff()
    {
        DataManager.createFileStructure();
        ConnectToRobot.Connect(AppData.COMPort);
        UserData.readAllUserData();
        PlutoComm.startSensorStream();
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


        public static void CalculateGameSpeedForLastUsageDay()
        {
            if (dTableSession == null || dTableSession.Rows.Count == 0)
            {
                UnityEngine.Debug.LogWarning("Session data is not available.");
                return;
            }
            Dictionary<string, float> gameIncrements = new Dictionary<string, float>
                {
                    { "PING-PONG", 0.5f },
                    { "TUK-TUK", 0.2f },
                    { "HAT-Trick", 1f }
                };


            var lastUsageDate = dTableSession.AsEnumerable()
                .Where(row => row.Field<string>("Mechanism") == selectedMechanism)
                .Select(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date)
                .Where(date => date < DateTime.Now.Date) // Exclude today
                .OrderByDescending(date => date)
                .FirstOrDefault();

            if (lastUsageDate == default(DateTime))
            {
                UnityEngine.Debug.LogWarning($"No usage data found for mechanism: {selectedMechanism}");
                return;
            }

            UnityEngine.Debug.Log($"Last usage date for mechanism {selectedMechanism}: {lastUsageDate:dd-MM-yyyy}");

            Dictionary<string, float> updatedGameSpeeds = new Dictionary<string, float>();

            foreach (var game in gameIncrements.Keys)
            {
                var rows = dTableSession.AsEnumerable()
                    .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == lastUsageDate)
                    .Where(row => row.Field<string>("GameName") == game && row.Field<string>("Mechanism") == selectedMechanism);

                float previousGameSpeed = rows.Any() ? rows.Average(row => Convert.ToSingle(row["GameSpeed"])) : 0f;
                float avgSuccessRate = rows.Any() ? rows.Average(row => Convert.ToSingle(row["SuccessRate"])) : 0f;

                if (avgSuccessRate >= 0.9f)
                {
                    updatedGameSpeeds[game] = previousGameSpeed + gameIncrements[game];
                }
                else
                {
                    updatedGameSpeeds[game] = previousGameSpeed;
                }
            }
            UnityEngine.Debug.Log($"Updated GameSpeeds for Mechanism: {selectedMechanism}");
            foreach (var game in updatedGameSpeeds)
            {
                UnityEngine.Debug.Log($"Game: {game.Key}, Updated GameSpeed: {game.Value}");
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

        private static void parseTherapyConfigData()
        {
            DataRow lastRow = dTableConfig.Rows[dTableConfig.Rows.Count - 1];
            hospNumber = lastRow.Field<string>("hospno");
            AppData.trainingSide = lastRow.Field<string>("TrainingSide");
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
            UnityEngine.Debug.Log(_totalMoveTimeToday);
            return _totalMoveTimeToday / 60f;
        }
        public static DaySummary[] CalculateMoveTimePerDay(int noOfPastDays = 7)
        {
            // Check if the session file has been loaded and has rows
            if (dTableSession == null || dTableSession.Rows.Count == 0)
            {
                UnityEngine.Debug.LogWarning("Session data is not available or the file is empty.");
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

                UnityEngine.Debug.Log($"{i} | {daySummaries[i - 1].Day} | {daySummaries[i - 1].Date} | {daySummaries[i - 1].MoveTime}");
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

public class ROM
{
    // Class attributes to store data read from the file
    public string datetime;
    public string side;
    public float promTmin;
    public float promTmax;
    public float aromTmin;
    public float aromTmax;
    public string mech;
    public string filePath = DataManager.directoryAPROMData;

    // Constructor that reads the file and initializes values based on the mechanism
    public ROM(string mechanismName)
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
                promTmin = float.Parse(values[1].Trim());
                promTmax = float.Parse(values[2].Trim());
                aromTmin=float.Parse(values[3].Trim());
                aromTmax = float.Parse(values[4].Trim());
            }
            else
            {
                // Handle case when no matching mechanism is found
                datetime = null;
                promTmin = 0;
                promTmax = 0;
                aromTmin = 0;
                aromTmax = 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading the file: " + ex.Message);
        }
    }


    public ROM( float angmin, float angmax, float aromAngMin, float aromAngMax, string mch, bool tofile)
    {
        promTmin = angmin;
        promTmax = angmax;
        aromTmin = aromAngMin;
        aromTmax=aromAngMax;
        mech = mch;
        datetime = DateTime.Now.ToString();

        if (tofile)
        {
            // Write data to assessment file.
            WriteToAssessmentFile();
        }
    }

    public void WriteToAssessmentFile()
    {
        string _fname = Path.Combine(filePath, mech + ".csv");
        //UnityEngine.Debug.Log(_fname);
        using (StreamWriter file = new StreamWriter(_fname, true))
        {
            file.WriteLine(datetime + ", " + promTmin.ToString() + ", " + promTmax.ToString() + ", " +  aromTmin.ToString() + ", " + aromTmax.ToString()+"");
        }

       
    }


    public (float tmin, float tmax) GetTminTmax()
    {
        return (promTmin, promTmax);
    }
}



public static class gameData
{
    //Assessment check
    public static bool isPROMcompleted=false;
    public static bool isAROMcompleted = false;
    //AAN controller check
    public static bool isBallReached = false;
    public static bool targetSpwan = false;
    public static bool isAROMEnabled = false;
    //game
    public static bool isGameLogging;
    public static string game;
    public static int gameScore;
    public static int reps;
    public static int playerScore;
    public static int enemyScore;
    public static string playerPos = "0";
    public static string playerPosition = "0";
    public static string enemyPos="0";
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
        if (AppData.selectedGame == "pingPong")
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
        else if(AppData.selectedGame == "hatTrick")
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
        else if (AppData.selectedGame == "tukTuk")
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
            if(fileData != null)
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

