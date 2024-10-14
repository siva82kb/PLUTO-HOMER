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

    // UserData Class
    public static class UserData
    {
        public static DataTable dTableConfig = null;
        public static DataTable dTableSession = null;
        public static string hospNumber;
        public static float[] mechMoveTimePrsc { get; private set; } // Prescribed movement time
        public static float[] mechMoveTimePrev { get; private set; } // Previous movement time
        public static float[] mechMoveTimeCurr { get; private set; } // Current movement time

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
                    return mechMoveTimePrsc.Sum();
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
                    return mechMoveTimePrev.Sum();
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
                    return mechMoveTimeCurr.Sum();
                }
            }
        }
        public static float totalMoveTimeRemaining // Retuns the 
        {
            get
            {
                float _total = 0f;
                for (int i = 0; i < PlutoDefs.Mechanisms.Length; i++)
                {
                    _total += mechMoveTimePrsc[i] - mechMoveTimePrev[i] - mechMoveTimeCurr[i];
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
            // Initialize to 0.
            mechMoveTimeCurr = new float[PlutoDefs.Mechanisms.Length];
            // Read the therapy configuration data.
            parseTherapyConfigData();
            // Get total previous movement time
            parseMechanismMoveTimePrev();
        }

        public static float getRemainingMoveTime(string mechanism)
        {
            int mechInx = PlutoDefs.getMechanimsIndex(mechanism);
            return mechMoveTimePrsc[mechInx] - mechMoveTimePrev[mechInx] - mechMoveTimeCurr[mechInx];
        }

        private static void parseMechanismMoveTimePrev()
        {
            mechMoveTimePrev = new float[PlutoDefs.Mechanisms.Length];
            for (int i = 0; i < PlutoDefs.Mechanisms.Length; i++)
            {
                // Get the total movement time for each mechanism
                var _totalMoveTime = dTableSession.AsEnumerable()
                    .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
                    .Where(row => row.Field<string>("Mechanism") == PlutoDefs.Mechanisms[i])
                    .Sum(row => Convert.ToInt32(row["MoveTime"]));
                mechMoveTimePrev[i] = _totalMoveTime / 60f;
            }
        }

        private static void parseTherapyConfigData()
        {
            DataRow lastRow = dTableConfig.Rows[dTableConfig.Rows.Count - 1];
            // Hospital number
            hospNumber = lastRow.Field<string>("hospno");
            Debug.Log("HostNumber: " + hospNumber);
            // Prescribed movement time
            mechMoveTimePrsc = new float[PlutoDefs.Mechanisms.Length];
            for (int i = 0; i < PlutoDefs.Mechanisms.Length; i++)
            {
                mechMoveTimePrsc[i] = float.Parse(lastRow.Field<string>(PlutoDefs.Mechanisms[i]));
            }
        }

        // Read the remaining time.
        //public static float getRemainingTime()
        //{
        //    return prescrivedMoveTime - currTodayMoveTime;
        //}

        // Returns today's total movement time in minutes.
        public static float getPrevTodayMoveTime()
        {
            var _totalMoveTimeToday = dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
                .Sum(row => Convert.ToInt32(row["MoveTime"]));
            Debug.Log(_totalMoveTimeToday);
            return _totalMoveTimeToday / 60f;
        }

        //// Returns the most recent training date so far.
        //private static DateTime getRecentTrainingDay()
        //{
        //    return dTableSession.AsEnumerable()
        //        .Select(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture))
        //        .Where(date => date.Date < DateTime.Now.Date)
        //        .Max();
        //}

        /*
         * Calculate the movement time for each training day.
         */
        public static DaySummary[] CalculateMoveTimePerDay(int noOfPostDays=7)
        {
            DateTime today = DateTime.Now.Date;
            DaySummary[] daySummaries = new DaySummary[noOfPostDays];
            // Find the move times for the last seven days excluding today. If the date is missing, then the move time is set to zero.
            for (int  i = 1; i <= noOfPostDays; i++)
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
            }
            return daySummaries;
        }
    }

    //public static class fileCreation
    //{
    //    static string directoryPath;
    //    static string directoryPathConfig;
    //    static string directoryPathSession;
    //    static string directoryPathRawData;
    //    public static string filePathUserData { get; set; }
    //    public static string filePathSessionData { get; set; }

    //    public static void createFileStructure()
    //    {
    //        directoryPath = Application.dataPath + "/data";
    //        directoryPathConfig = directoryPath + "/configuration";
    //        directoryPathSession = directoryPath + "/sessions";
    //        directoryPathRawData = directoryPath + "/rawdata";
    //        filePathUserData = directoryPath + "/configdata.csv";
    //        filePathSessionData = directoryPathSession + "/sessions.csv";
    //        // Check if the directory exists
    //        if (!Directory.Exists(directoryPath))
    //        {
    //            // If not, create the directory
    //            Directory.CreateDirectory(directoryPath);
    //            Directory.CreateDirectory(directoryPathConfig);
    //            Directory.CreateDirectory(directoryPathSession);
    //            Directory.CreateDirectory(directoryPathRawData);
    //            File.Create(filePathUserData).Dispose(); // Ensure the file handle is released
    //            File.Create(filePathSessionData).Dispose(); // Ensure the file handle is released
    //            Debug.Log("Directory created at: " + directoryPath);
    //        }
    //        writeHeader(filePathSessionData);
    //    }

    //    public static void writeHeader(string path)
    //    {
    //        try
    //        {
    //            // Check if the file exists and if it is empty (i.e., no lines in the file)
    //            if (File.Exists(path) && File.ReadAllLines(path).Length == 0)
    //            {
    //                // Define the CSV header string, separating each column with a comma
    //                string headerData = "SessionNumber,DateTime,Assessment,StartTime,StopTime,GameName,TrialDataFileLocation,DeviceSetupFile,AssistMode,AssistModeParameter,mec,MovTime";

    //                // Write the header to the file
    //                File.WriteAllText(path, headerData + "\n"); // Add a new line after the header
    //                Debug.Log("Header written successfully.");
    //            }
    //            else
    //            {
    //                //Debug.Log("Writing failed or header already exists.");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            // Catch any other generic exceptions
    //            Debug.LogError("An error occurred while writing the header: " + ex.Message);
    //        }
    //    }
    //}
}

public static class Miscellaneous
{
    public static string GetAbbreviatedDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek.ToString().Substring(0, 3);
    }
}