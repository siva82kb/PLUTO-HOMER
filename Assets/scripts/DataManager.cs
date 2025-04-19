using System;
using System.Linq;
using System.IO;
using System.Data;
using UnityEngine;
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
    public static readonly string basePath = FixPath(Path.Combine(Application.dataPath, "data"));
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

    // Sessions file definitions.
    public static string[] SESSIONFILEHEADER = new string[] {
        "SessionNumber", "DateTime",
        "TrialNumberDay", "TrialNumberSession", "TrialType", "TrialStartTime", "TrialStopTime", "TrialRawDataFile", 
        "Mechanism", 
        "GameName", "GameParameter", "GameSpeed",  
        "AssistMode", "DesiredSuccessRate", "SuccessRate", "CurrentControlBound", "NextControlBound"
    };

    // Raw data header.    
    public static string[] RAWFILEHEADER = new string[] {
        "DeviceRunTime", "PacketNumber", "Status", "DataType", "ErrorStatus", 
        "ControlType", "Calibration",  "Mechanism", 
        "Button", "Angle", "Torque", "Desired", "Control", "ControlBound", "ControlDir", "Target", 
        "Error", "ErrorDiff", "ErrorSum",
        "GamePlayerX", "GamePlayerY", "GameTargetX", "GameTargetY", "GameState",
        "AanTargetPosition", "AanInitialPosition", "AanState"
    };

    // Date format strict.
    public static string DATEFORMAT = "yyyy-MM-dd HH:mm:ss";

    // Functions to generate file names.
    public static string GetAanAdaptFileName(string mechanism) => FixPath(Path.Combine(aanAdaptPath, $"{mechanism}-adaptaan.csv"));
    public static string GetAanExecFileName(string mechanism) => FixPath(Path.Combine(aanExecPath, $"{mechanism}-execaan.csv"));
    public static string GetGameFileName(string game) => FixPath(Path.Combine(gamePath, $"{game}-gameparams.csv"));
    public static string GetMechFileName(string mechanism) => FixPath(Path.Combine(mechPath, $"{mechanism}-mechparams.csv"));
    public static string GetRawFileName(
        string game,
        string mechanism,
        string datetime) => FixPath(Path.Combine(rawPath, $"{datetime}-{game}-{mechanism}-raw.csv"));
    public static string GetRomFileName(string mechanism) => FixPath(Path.Combine(romPath, $"{mechanism}-rom.csv"));
    public static string GetTrialRawDataFileName(
        int sessNo, 
        int trialNo,
        string game,
        string mechanism) => FixPath(Path.Combine(rawPath, $"raw-sess{sessNo:D2}-trial{trialNo:D3}-{game}-{mechanism}.csv"));
        // string mechanism) => FixPath(Path.Combine(rawPath, $"session-{sessNo}", $"raw-sess{sessNo:D2}-trial{trialNo:D3}-{game}-{mechanism}.csv"));
    public static string GetTrialAanExecDataFileName(
        int sessNo,
        int trialNo,
        string game,
        string mechanism) => FixPath(Path.Combine(rawPath, $"aanexec-sess{sessNo:D2}-trial{trialNo:D3}-{game}-{mechanism}.csv"));
        // string mechanism) => FixPath(Path.Combine(rawPath, $"session-{sessNo}", $"aanexec-sess{sessNo:D2}-trial{trialNo:D3}-{game}-{mechanism}.csv"));

    // Fix stupid Window's path separator issue.
    public static string FixPath(string path) => path.Replace("\\", "/");
    
    public static void CreateFileStructure()
    {
        directoryPathConfig = FixPath(basePath + "/configuration");
        sessionPath = FixPath(Path.Combine(basePath, "sessions"));
        gamePath = FixPath(Path.Combine(basePath, "gameparams"));
        mechPath = FixPath(Path.Combine(basePath, "mechparams"));
        aanAdaptPath = FixPath(Path.Combine(basePath, "aanadapt"));
        aanExecPath = FixPath(Path.Combine(basePath, "aanexec"));
        rawPath = FixPath(Path.Combine(basePath, "rawdata"));
        romPath = FixPath(Path.Combine(basePath, "rom"));
        logPath = FixPath(Path.Combine(basePath, "applog"));
        sessionFile = FixPath(Path.Combine(sessionPath, "sessions.csv"));
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
    public static void CreateSessionFile(string device, string location, string[] header = null)
    {
        // Ensure the Sessions.csv file has headers if it doesn't exist
        if (!File.Exists(DataManager.sessionFile))
        {
            header??= SESSIONFILEHEADER;
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

    public static string StartLogging(string scene)
    {
        // Start Log file only if we are not already logging.
        if (isLogging)
        {
            return null;
        }
        if (!Directory.Exists(DataManager.logPath))
        {
            Directory.CreateDirectory(DataManager.logPath);
        }
        string _dtstr = DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");
        logFilePath = Path.Combine(DataManager.logPath, $"{_dtstr}-application.log");
        // if (!File.Exists(logFilePath)) File.Create(logFilePath);

        // Create the log file and write the header.
        logWriter = new StreamWriter(logFilePath, true, Encoding.UTF8);
        currentScene = scene;
        LogInfo("Created PLUTO log file.");
        return _dtstr;
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
                string _msg = $"{DateTime.Now:dd-MM-yyyy HH:mm:ss} {logMsgType,-7} {InBraces(_user), -10} {InBraces(currentScene), -12} {InBraces(currentMechanism), -8} {InBraces(currentGame), -8} >> {message}";
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
