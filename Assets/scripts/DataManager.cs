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
using UnityEditor.VersionControl;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Bson;


/*
 * Summary Data Class
 */
public struct DaySummary
{
    public string Day { get; set; }
    public string Date { get; set; }
    public float MoveTime { get; set; }
}


public static class DataManager
{
    public static readonly string directoryPath = Application.dataPath + "/data";
    static string directoryPathConfig;
    public static string directoryPathSession;
    static string directoryPathRawData;
    public static string directoryMechData;
    public static string filePathConfigData { get; private set; }
    public static string filePathSessionData { get; private set; }

    public static void createFileStructure()
    {
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
            File.Create(filePathConfigData).Dispose();
            Debug.Log("Directory created at: " + directoryPath);
        };
    }
    public static DataTable loadCSV(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }
        DataTable dTable = new DataTable();
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



