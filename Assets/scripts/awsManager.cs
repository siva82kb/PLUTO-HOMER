using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;
public static class awsManager
{
    public static string pythonScriptPath = @"C:/pythonscripts/uploadToAWS.pyw";
    public static  string pythonExecutionPath = @"C:\Program Files\Python312\pythonw.exe";
    public static string filePathUploadStatus = @"C:/DeviceSetups/Pluto"; //change according to the device
    public static string[] status = new string[] {"upload_needed","no_upload"};
    public static  string taskName ="AWSUploaderPlutoTask"; //change according to the device
    public static string DeviceName = "Pluto";//change according to the device


    // Method to schedule the task using SCHTASKS
    public static void ScheduleTask()
    {
        
       
        string commandArguments = $"\"{pythonScriptPath}\"";
        string scheduleFrequency = "/SC MINUTE /MO 30";
        string command = $"schtasks /Create {scheduleFrequency} /TN \"{taskName}\" " +
                         $"/TR \"{pythonExecutionPath} {commandArguments}\" /F ";
        RunCommand(command);
        AppLogger.LogInfo("Task scheduled For AWSuploaderTask Successfully");
    }
  
  

public static void RunAWSpythonScript()
{
    
        if (!File.Exists(pythonScriptPath))
        {
            AppLogger.LogInfo("Python script does not exist");
            Debug.Log("File not found: Python script");
            return;
        }

        AppLogger.LogInfo("Starting to run Python script");

        try
        {
            Process process = new Process();
            process.StartInfo.FileName = pythonExecutionPath;
            process.StartInfo.Arguments = pythonScriptPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            AppLogger.LogInfo(output);
            AppLogger.LogError(error);
        }
        catch (System.Exception ex)
        {
            Debug.Log("An error occurred while running the Python script: " + ex.Message);
            AppLogger.LogError("An error occurred while running the Python script: " + ex.Message);
        }
    
}

    public static void changeUploadStatus(string status){
        string uploadFilePath = Path.Combine(filePathUploadStatus, "uploadStatus.txt");
            Debug.Log("nodata :"+AppData.Instance.userData.hospNumber);
            // You don't need `File.Create(...).Dispose()` manually � File.WriteAllText will create/write directly.
            File.WriteAllText(uploadFilePath, $"{Application.dataPath},{status},{DeviceName},{AppData.Instance.userData.hospNumber}");
        
    }


    //To create the uploadStatusFile
    public  static void createFile(string userID)
    {

        Directory.CreateDirectory(filePathUploadStatus);

        string uploadFilePath = Path.Combine(filePathUploadStatus, "uploadStatus.txt");

        // You don't need `File.Create(...).Dispose()` manually � File.WriteAllText will create/write directly.
        File.WriteAllText(uploadFilePath, $"{Application.dataPath},{status[0]},{DeviceName},{userID}");

    }
    // Method to check if the task is already scheduled
    public  static bool IsTaskScheduled(string taskName)
    {
        string command = $"schtasks /Query /TN \"{taskName}\"";
        var result = RunCommand(command);
        return result.Contains(taskName);
    }

    // Helper method to run a command in CMD
    public static string RunCommand(string command)
    {
        Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = $"/C {command}";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        AppLogger.LogInfo(output);
        AppLogger.LogWarning(error);
        return output;
    }
}


