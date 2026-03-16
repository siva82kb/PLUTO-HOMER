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

    // public static  string pythonExecutionPath = @"C:/Users/HOMER_09/AppData/Local/Python/bin/pythonw.exe"; //Device - 4
    // public static string pythonExecutionPath = @"C:/Program Files/Python313/pythonw.exe"; // Device - 5, Device-2
    
    // public static string pythonExecutionPath = @"C:/Program Files/Python312/pythonw.exe";
    // public static string pythonExecutionPath = @"C:/Program Files/Python313/pythonw.exe";// device-2, device - 1
    // public static string pythonExecutionPath = @"C:/Program Files/Python314/pythonw.exe"; //device 10
    public static string pythonExecutionPath;
    // public static  string pythonExecutionPath = @"C:/Users/HOMER_10/AppData/Local/Programs/Python/Python314/pythonw.exe";  //Device-7
    // public static  string pythonExecutionPath = @"C:/Users/HOMER_08/AppData/Local/Programs/Python/Python313/pythonw.exe"; //Device -8
    // public static string pythonExecutionPath = @"C:/Users/HOMER_11/AppData/Local/Programs/Python/Python314/pythonw.exe"; //Device-9

    public static string filePathUploadStatus = @"C:/DeviceSetups/Pluto"; //change according to the device
    public static string filePathAppsetups = @"C:/AppSetups/Pluto"; //change according to the device

    public static string[] status = new string[] {"upload_needed","no_upload"};
    public static  string taskName ="AWSUploaderPlutoTask"; //change according to the device
    public static string DeviceName = "Pluto"; //change according to the device
    public static string fullTaskAction = $"\\\"{pythonExecutionPath}\\\"\\\"{pythonScriptPath}\\\"";
    public static string message="";
    // Method to schedule the task using SCHTASKS
    // public static void ScheduleTask()
    // {
    //     string commandArguments = $"\"{pythonScriptPath}\"";
    //     string scheduleFrequency = "/SC MINUTE /MO 30";
    //     string command = $"schtasks /Create {scheduleFrequency} /TN \"{taskName}\" " +
    //                      $"/TR \"{fullTaskAction}\" /F ";
    //     RunCommand(command);
    // }
  
  

public static void RunAWSpythonScript()
{
        if (!File.Exists(pythonScriptPath))
        {
            Debug.Log("File not found: Python script");
            return;
        }

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
            Debug.Log(output);
            Debug.Log(error);
           message = output;
            
        }
        catch (System.Exception ex)
        {
            Debug.Log("An error occurred while running the Python script: " + ex.Message);
        }
    
}
    public static string getmessage()
    {
        return message;
    }

    public static void changeUploadStatus(string status)
    {
        string uploadFilePath = Path.Combine(filePathUploadStatus, "uploadStatus.txt");
        File.WriteAllText(uploadFilePath, $"{Path.Combine(Application.dataPath, "data", AppData.Instance.userID)},{status},{DeviceName},{AppData.Instance.userData.hospNumber},{AppData.Instance.userData.GetDeviceLocation()}");

        if (!File.Exists(Path.Combine(filePathUploadStatus, "uploadProgress.txt")))
        {
            File.Create(Path.Combine(filePathUploadStatus, "uploadProgress.txt"));
        }
        
        
    }
    public static void AppSetups(string hospitalId, string location)
    {
        Directory.CreateDirectory(filePathAppsetups);
        string appInfoFilePath = Path.Combine(filePathAppsetups, "appInfo.txt");
        File.WriteAllText(appInfoFilePath, $"{Path.Combine(Application.dataPath, "data")},{hospitalId},{location},{DeviceName}");
    }

    //To create the uploadStatusFile
    public  static void createFile(string userID)
    {

        Directory.CreateDirectory(filePathUploadStatus);

        string uploadFilePath = Path.Combine(filePathUploadStatus, "uploadStatus.txt");

        File.WriteAllText(uploadFilePath, $"{Path.Combine(Application.dataPath,"data", AppData.Instance.userID)},{status[0]},{DeviceName},{userID},{AppData.Instance.userData.GetDeviceLocation()}");

    }
    // Method to check if the task is already scheduled
    // public  static bool IsTaskScheduled(string taskName)
    // {
    //     string command = $"schtasks /Query /TN \"{taskName}\"";
    //     var result = RunCommand(command);
    //     return result.Contains(taskName);
    // }

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
        return output;
    }
}
