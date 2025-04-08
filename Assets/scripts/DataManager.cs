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
using PlutoNeuroRehabLibrary;
using System.Text;


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
    public static readonly string basePath = Application.dataPath + "/data";
    static string directoryPathConfig;
    public static string sessionPath { get; private set; }
    public static string gamePath { get; private set; }
    public static string mechPath { get; private set; }
    public static string aanAdaptPath { get; private set; }
    public static string aanExecPath { get; private set; }
    public static string rawPath { get; private set; }
    public static string romPath { get; private set; }
    public static string logPath { get; private set; }

    public static readonly string configFile = basePath + "/configdata.csv";
    public static string sessionFile { get; private set; }

    public static void createFileStructure()
    {
        directoryPathConfig = basePath + "/configuration";
        sessionPath = basePath + "/sessions";
        gamePath = basePath + "/gameparams";
        mechPath = basePath + "/mechparams";
        aanAdaptPath = basePath + "/aanadapt";
        aanExecPath = basePath + "/aanexec";
        rawPath = basePath + "/rawdata";
        romPath = basePath + "/rom";
        logPath = basePath + "/applog";
        sessionFile = sessionPath + "/sessions.csv";
        // Check if the directory exists
        Directory.CreateDirectory(sessionPath);
        Directory.CreateDirectory(gamePath);
        Directory.CreateDirectory(mechPath);
        Directory.CreateDirectory(aanAdaptPath);
        Directory.CreateDirectory(aanExecPath);
        Directory.CreateDirectory(rawPath);
        Directory.CreateDirectory(romPath);
        Directory.CreateDirectory(logPath);
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

        // Ignore all preheaders that start with ':'
        int i = 0;
        while (lines[i].StartsWith(":")) i++;
        // Only preheader lines are present
        if (i >= lines.Length) return null;
        lines = lines.Skip(i).ToArray();
        // Nothing to read
        if (lines.Length == 0) return null;

        // Read and parse the header line
        var headers = lines[0].Split(',');
        foreach (var header in headers)
        {
            dTable.Columns.Add(header);
        }

        // Read the rest of the data lines
        for (i = 1; i < lines.Length; i++)
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
    
    // Create session file
    public static void CreateSessionFile(string device, string location, string[] header)
    {
        // Ensure the Sessions.csv file has headers if it doesn't exist
        if (!File.Exists(DataManager.sessionFile))
        {
            using (var writer = new StreamWriter(DataManager.sessionFile, false, Encoding.UTF8))
            {
                // Write the preheader details
                writer.WriteLine($":Device: {device}");
                writer.WriteLine($":Location: {location}");
                writer.WriteLine(String.Join(",", header));
            }
            AppLogger.LogWarning("Sessions.csv file not founds. Created one.");
        }
    }

    // Get the last session number.
    public static int GetPreviousSessionNumber()
    {
        if (!File.Exists(sessionFile)) return 0;
        // Read the last line of the file
        var lastLine = File.ReadLines(sessionFile).LastOrDefault();
        if (lastLine == null || lastLine.StartsWith("SessionNumber")) return 0;
        return int.TryParse(lastLine.Split(',')[0], out var sessionNumber) ? sessionNumber : 0;
    }
}

public enum LogMessageType
{
    INFO,
    WARNING,
    ERROR
}


public static class AppLogger
{
    private static string logFilePath;
    private static StreamWriter logWriter = null;
    private static readonly object logLock = new object();
    public static string currentScene { get; private set; } = "";
    public static string currentMechanism { get; private set; } = "";
    public static string currentGame { get; private set; } = "";

    public static bool DEBUG = true;
    public static string InBraces(string text) => $"[{text}]";

    public static bool isLogging
    {
        get
        {
            return logFilePath != null;
        }
    }

    public static void StartLogging(string scene)
    {
        // Start Log file only if we are not already logging.
        if (isLogging)
        {
            return;
        }
        if (!Directory.Exists(DataManager.logPath))
        {
            Directory.CreateDirectory(DataManager.logPath);
        }
        logFilePath = Path.Combine(DataManager.logPath, $"log-{DateTime.Now:dd-MM-yyyy-HH-mm-ss}.log");
        if (!File.Exists(logFilePath))
        {
            using (File.Create(logFilePath))
            {
                Debug.Log("created");
            }
        }
        logWriter = new StreamWriter(logFilePath, true);
        currentScene = scene;
        LogInfo("Created PLUTO log file.");
    }

    public static void SetCurrentScene(string scene)
    {
        if (isLogging)
        {
            currentScene = scene;
            LogInfo($"Scene set to '{currentScene}'.");
        }
    }

    public static void SetCurrentMechanism(string mechanism)
    {
        Debug.Log(mechanism);
        if (isLogging)
        {
            currentMechanism = mechanism;
            LogInfo($"PLUTO mechanism set to '{currentMechanism}'.");
        }
    }

    public static void SetCurrentGame(string game)
    {
        if (isLogging)
        {
            currentGame = game;
            LogInfo($"PLUTO game set to '{currentGame}'.");
        }
    }

    public static void StopLogging()
    {
        if (logWriter != null)
        {
            LogInfo("Closing log file.");
            logWriter.Close();
            logWriter = null;
            logFilePath = null;
            currentScene = "";
        }
    }

    public static void LogMessage(string message, LogMessageType logMsgType)
    {
        lock (logLock)
        {
            if (logWriter != null)
            {
                string _user = AppData.Instance.userData != null ? AppData.Instance.userData.hospNumber : "";
                string _msg = $"{DateTime.Now:dd-MM-yyyy HH:mm:ss} {logMsgType,-7} {InBraces(_user), -10} {InBraces(currentScene), -12} {InBraces(currentMechanism), -8} {InBraces(currentGame), -8} {message}";
                logWriter.WriteLine(_msg);
                logWriter.Flush();
                if (DEBUG) Debug.Log(_msg);
            }
        }
    }

    public static void LogInfo(string message)
    {
        LogMessage(message, LogMessageType.INFO);
    }

    public static void LogWarning(string message)
    {
        LogMessage(message, LogMessageType.WARNING);
    }

    public static void LogError(string message)
    {
        LogMessage(message, LogMessageType.ERROR);
    }
}



