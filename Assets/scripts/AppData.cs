
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
    public static readonly string COMPort = "COM3";

    //Options to drive 
    public static string selectMechanism = null;
    public static string selectedGame = null;
    public static string game;
    public static int gameScore;
    public static int reps;

    //game
    public static bool isGameLogging;

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
                    // Add all entries of the mechanism move time dictionary
                    return mechMoveTimePrsc.Values.Sum();

                    //return mechMoveTimePrsc.Sum();
                }
            }
        }
        public static float totalMoveTimePrev
        {
            get
            {
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
        public static float totalMoveTimeRemaining // Retuns the 
        {
            get
            {
                float _total = 0f;
                foreach (string mech in PlutoDefs.Mechanisms)
                {
                    _total += mechMoveTimePrsc[mech] - mechMoveTimePrev[mech] - mechMoveTimeCurr[mech];
                }
                return _total;
            }
        }

        // Function to read all the user data.
        public static void readAllUserData()
        {
            // Read the configuration da
            dTableConfig = DataManager.loadCSV(DataManager.filePathConfigData);
            // Read the session data
            dTableSession = DataManager.loadCSV(DataManager.filePathSessionData);
            // Initialize the mechanism current movement time dictionary
            mechMoveTimeCurr = createMoveTimeDictionary();
            // Read the therapy configuration data.
            parseTherapyConfigData();
            // Get total previous movement time
            parseMechanismMoveTimePrev();
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
            return mechMoveTimePrev[mechanism] + mechMoveTimeCurr[mechanism];
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
            // Hospital number
            hospNumber = lastRow.Field<string>("hospno");
            startDate = DateTime.ParseExact(lastRow.Field<string>("startdate"), "dd-MM-yyyy", CultureInfo.InvariantCulture);
            // Prescribed movement time
            mechMoveTimePrsc = createMoveTimeDictionary();
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

        /*
         * Calculate the movement time for each training day.
         */
        public static DaySummary[] CalculateMoveTimePerDay(int noOfPastDays = 7)
        {
            DateTime today = DateTime.Now.Date;
            DaySummary[] daySummaries = new DaySummary[noOfPastDays];
            // Find the move times for the last seven days excluding today. If the date is missing, then the move time is set to zero.
            for (int i = 1; i <= noOfPastDays; i++)
            {
                DateTime _day = today.AddDays(-i);
                // Get the summary data for this date.
                var _moveTime = dTableSession.AsEnumerable()
                    .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == _day)
                    .Sum(row => Convert.ToInt32(row["MoveTime"]));
                // Create the day summary.
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
        Debug.Log(fileName);

        try
        {
            // Read the entire file and capture the last line containing the desired data
            using (StreamReader file = new StreamReader(fileName))
            {
                while (!file.EndOfStream)
                {
                    lastLine = file.ReadLine();
                }
            }

            // Split the last line and process the data
            values = lastLine.Split(','); // Change to comma or other delimiter as necessary
            if (values[0].Trim() != null)
            {
                // Assign values if mechanism matches
                datetime = values[0].Trim();
                Debug.Log(datetime + "_ datetime");
                side = values[1].Trim();
                Debug.Log(side + "_ side");

                tmin = float.Parse(values[2].Trim());
                Debug.Log(tmin + "_ min");

                tmax = float.Parse(values[3].Trim());
                Debug.Log(tmax + "max");

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

    // Method to return tmin and tmax as a tuple
    public (float tmin, float tmax) GetTminTmax()
    {
        return (tmin, tmax);
    }

}
