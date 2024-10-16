using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Globalization;

using System.Linq;
using System.Runtime.Remoting.Messaging;
using XCharts.Runtime;
using System.Text;


public static class AppData
{

    public static string game;
    public static int gameScore;
    public static int reps;
    // Data logging related stuff.
    private static string[] _dheader = new string[] {
        "time",
        "ang", "ang_vel", "torque", "m_curr", "control", "desired",
        "status", "error1", "erro2", "ctype", "dtye"
    };

    private static string[] _perforamceHeader = new string[]
    {
        "start_difficulty_rom",  "start_difficulty_speed","start_performance","end_performace", "end_difficulty_rom","end_difficulty_speed","reps","time_on_trial", "log_file"
    };

    private static string[] _gameheader = gameLogHeader();
    private static DataLogger _dlog;
    private static DataLogger _gamelog;

    public static bool isLogging { get; private set; }
    public static bool isGameLogging { get; private set; }
    public static class fileCreation
    {
        static string directoryPath;
        static string directoryPathConfig;
        static string directoryPathSession;
        static string directoryPathRawData;
        public static string directoryMechConfig;
        public static string filePathUserData { get; set; }
        public static string filePathSessionData { get; set; }
        public static void initializeFilePath()
        {
            directoryPath = Application.dataPath + "/data";
            directoryPathConfig = directoryPath + "/Configuration";
            directoryPathSession = directoryPath + "/sessions";
            directoryPathRawData = directoryPath + "/RawData";
            filePathUserData = directoryPath + "/config_data.csv";
            directoryMechConfig = directoryPath + "/mech";
            filePathSessionData = directoryPathSession + "/sessions.csv";
            directoryPathConfig = directoryPath + "/Configuration";
        }

        public static void createFileStructure()
        {
            
            // Check if the directory exists
            if (Directory.Exists(directoryPath))
            {
                //Debug.Log("Directory already exists: " + directoryPath);
            }
            else
            {
                // If not, create the directory
                Directory.CreateDirectory(directoryPath);
                Directory.CreateDirectory(directoryPathConfig);
                Directory.CreateDirectory(directoryPathSession);
                Directory.CreateDirectory(directoryPathRawData);
                File.Create(filePathUserData).Dispose(); // Ensure the file handle is released
                File.Create(filePathSessionData).Dispose(); // Ensure the file handle is released
                Debug.Log("Directory created at: " + directoryPath);
            }

            writeHeader(filePathSessionData);
        }

        public static void writeHeader(string path)
        {
            try
            {
                // Check if the file exists and if it is empty (i.e., no lines in the file)
                if (File.Exists(path) && File.ReadAllLines(path).Length == 0)
                {
                    // Define the CSV header string, separating each column with a comma
                    string headerData = "SessionNumber,DateTime,Assessment,StartTime,StopTime,GameName,TrialDataFileLocation,DeviceSetupFile,AssistMode,AssistModeParameter,mec,MovTime";

                    // Write the header to the file
                    File.WriteAllText(path, headerData + "\n"); // Add a new line after the header
                    Debug.Log("Header written successfully.");
                }
                else
                {
                    //Debug.Log("Writing failed or header already exists.");
                }
            }
            catch (Exception ex)
            {
                // Catch any other generic exceptions
                Debug.LogError("An error occurred while writing the header: " + ex.Message);
            }
        }

    }
    public static class MechanismSelection
    {
        public static string selectedOption;
    }

    public class MechanismData
    {
        // Class attributes to store data read from the file
        public string datetime;
        public string side;
        public float tmin;
        public float tmax;
        public string mech;
        public string filePath = fileCreation.directoryMechConfig;

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
    static public void StartDataLog(string fname)
    {
        // If you are already logging.
        if (_dlog != null)
        {
            StopLogging();
        }
        // Start new logger
        if (fname != "")
        {
            _dlog = new DataLogger(fname, String.Join(", ", _dheader) + "\n");
            isLogging = true;
        }
        else
        {
            _dlog = null;
            isLogging = false;
        }
    }
    static public void StartGameDataLog(string fname)
    {
        // If you are already logging.
        if (_gamelog != null)
        {
            StopLogging();
        }
        // Start new logger
        if (fname != "")
        {
            _gamelog = new DataLogger(fname, String.Join(", ", _gameheader) + "\n");
            isGameLogging = true;
        }
        else
        {
            _gamelog = null;
            isGameLogging = false;
        }
        AppData.gameScore = 0;
        //AppData.trailNumber = 0;
    }
    static public void StopLogging()
    {
        if (_dlog != null)
        {
            _dlog.stopDataLog(true);
            _dlog = null;
            isLogging = false;
        }
        else
            UnityEngine.Debug.Log("Null log");
    }

    static string[] gameLogHeader()
    {
        string target = "targetpos";
        string player = "playerpos";
        string[] _header = new string[]
        {
            "time" ,
             player,
             target,
             "trail",
             "isAssistedTrail",
             "isFlacccid",
               "score",
            "ang",
            "actualtorque",
            "desiredTorque",
            "aan",
        };
        return _header;
    }

    static public void LogGameData()
    {


        if (AppData.isGameLogging)
        {


            string[] _data = new string[] {
               // CurrentTime.ToString("G17"),
               // PlayerPos, //defined Above in APPData
               // TargetPos, //defined Above in APPData      
               // trailNumber.ToString(),
               // isAssisted.ToString(),
               // isflalccidControl.ToString(),
               // gameScore.ToString(),
               // plutoData.angle.ToString("G17"),
               // //plutoData.angvel.ToString("G17"),
               // plutoData.torq.ToString("G17"),
               // //plutoData.mcurr.ToString("G17"),
               //// plutoData.control.ToString("G17"),
               // plutoData.desTorq.ToString("G17"),
                
                //aanProfile,//defined Above in APPData
                //plutoData.ctrl.ToString("G17"),
                //plutoData.lc1.ToString("G17"),
                //plutoData.lc2.ToString("G17"),
                //plutoData.lc3.ToString("G17"),
            };
            string _gstring = String.Join(",", _data);
            _gstring += "\n";
            _gamelog.logData(_gstring);
            //UnityEngine.Debug.Log(_gstring);
        }

    }
    static public void StopGameLogging()
    {
        if (_gamelog != null)
        {
            AppData.gameScore = 0;
            //AppData.trailNumber = 0;
            _gamelog.stopDataLog(true);
            _gamelog = null;
            isGameLogging = false;

        }
        else
            UnityEngine.Debug.Log("NULL GAMELOG");

    }
    public class DataLogger
    {
        public string curr_fname { get; private set; }
        public StringBuilder _filedata;
        public bool stillLogging
        {
            get { return (_filedata != null); }
        }

        public DataLogger(string filename, string header)
        {
            curr_fname = filename;

            _filedata = new StringBuilder(header);
        }

        public void stopDataLog(bool log = true)
        {
            if (log)
            {
                //  UnityEngine.Debug.Log(curr_fname);
                File.AppendAllText(curr_fname, _filedata.ToString());
            }
            curr_fname = "";
            _filedata = null;
        }

        public void logData(string data)
        {
            if (_filedata != null)
            {
                _filedata.Append(data);
            }
        }
    }


}