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
    public static class fileCreation
    {
        static string directoryPath;
        static string directoryPathConfig;
        static string directoryPathSession;
        static string directoryPathRawData;
        public static string filePathUserData { get; set; }
        public static string filePathSessionData { get; set; }
        public static void initializeFilePath()
        {
            directoryPath = Application.dataPath + "/data";
            directoryPathConfig = directoryPath + "/Configuration";
            directoryPathSession = directoryPath + "/Session";
            directoryPathRawData = directoryPath + "/RawData";
            filePathUserData = directoryPath + "/config_data.csv";
            filePathSessionData = directoryPathSession + "/sessions.csv";

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
   

}