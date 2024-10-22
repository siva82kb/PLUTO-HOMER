using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Globalization;
using System.Data;
using System.Linq;


/*
 * Summary Data Class
 */
public struct DaySummary
{
    public string Day { get; set; }
    public string Date { get; set; }
    public float MoveTime { get; set; }
    //public float TotalTime { get; set; }
    //public float RemainingTime { get; set; }
    //public float Percentage { get; set; }
    //public bool IsCompleted { get; set; }
}


public static class DataManager
{
    static string directoryPath;
    static string directoryPathConfig;
    static string directoryPathSession;
    static string directoryPathRawData;
    public static string directoryMechData;
    public static string filePathConfigData { get; set; }
    public static string filePathSessionData { get; set; }
    
    public static void createFileStructure()
    {
        directoryPath = Application.dataPath + "/data";
        directoryPathConfig = directoryPath + "/configuration";
        directoryPathSession = directoryPath + "/sessions";
        directoryPathRawData = directoryPath + "/rawdata";
        directoryMechData = directoryPath + "/mech";
        filePathConfigData = directoryPath + "/configdata.csv";
        filePathSessionData = directoryPathSession + "/sessions.csv";
        // Check if the directory exists
        if (!Directory.Exists(directoryPath))
        {
            // If not, create the directory
            Directory.CreateDirectory(directoryPath);
            Directory.CreateDirectory(directoryPathConfig);
            Directory.CreateDirectory(directoryPathSession);
            Directory.CreateDirectory(directoryPathRawData);
            File.Create(filePathConfigData).Dispose(); // Ensure the file handle is released
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

    /*
     * Load a CSV file into a DataTable.
     */
    public static DataTable loadCSV(string filePath)
    {
        DataTable dTable = new DataTable();

        // Read all lines from the CSV file
        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0) return null;

        // Read the header line to create columns
        var headers = lines[0].Split(',');
        foreach (var header in headers)
        {
            dTable.Columns.Add(header);
        }

        // Read the rest of the data lines
        for (int i = 1; i < lines.Length; i++)
        {
            var row = dTable.NewRow();
            var fields = lines[i].Split(',');
            for (int j = 0; j < headers.Length; j++)
            {
                row[j] = fields[j];
            }
            dTable.Rows.Add(row);
        }
        return dTable;
    }
}