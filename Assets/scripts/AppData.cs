using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Globalization;

using System.Linq;


public static class AppData
{
    public static string game;
    public static int gameScore;
    public static int reps;

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

}