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


public static class AppData
{
    // COM Port for the device
    public static readonly string COMPort = "COM3";

    // UserData Class
    public static class UserData
    {
        public static DataTable dTableConfig;
        public static DataTable dTableSession;
        // Movement time related data.
        public static float prevTotalMoveTime = -1f;
        public static float currTotalMoveTime = 0f;

        // Function to read all the user data.
        public static void readAllUserData()
        {
            // Read the configuration da
            dTableConfig = DataManager.loadCSV(DataManager.filePathConfigData);
            // Read the session data
            dTableSession = DataManager.loadCSV(DataManager.filePathSessionData);
            // Get total previous movement time
            prevTotalMoveTime = getPrevTotalMoveTime();
        }

        private static float getPrevTotalMoveTime()
        {
            var _totalMoveTimeToday = dTableSession.AsEnumerable()
                .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Date == DateTime.Now.Date)
                .Sum(row => Convert.ToInt32(row["MoveTime"]));
            Debug.Log(_totalMoveTimeToday);
            return _totalMoveTimeToday / 60f;
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