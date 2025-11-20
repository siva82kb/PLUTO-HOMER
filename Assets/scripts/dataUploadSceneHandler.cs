// using UnityEngine;
// using TMPro;
// using System.Collections;
// using System.IO;
// using System.Diagnostics; // for Process
// using Debug = UnityEngine.Debug;
// using System.Collections.Concurrent;

// public class DataUploadSceneHandler : MonoBehaviour
// {
//     public TextMeshProUGUI dataStatus;
//     public string status = null;
//     private bool hasUploaded = false; // ensures script runs only 
//     private ConcurrentQueue<System.Action> _actionQueue = new ConcurrentQueue<System.Action>();
//     private string progressFilePath = @"C:\DeviceSetups\Pluto\uploadProgress.txt";
    

//     void Start()
//     {
//         Debug.Log($"status : {DataManager.status}");
//             // PlutoComm.stopSensorStream();

//             // ConnectToRobot.disconnect();
//         // });
//         // Start a coroutine that checks file every 60 seconds
//             StartCoroutine(CheckUploadStatusRoutine());
//      StartCoroutine(CheckProgress());
//     }

//     IEnumerator CheckProgress()
//     {
//         while (true)
//         {
//             if (File.Exists(progressFilePath))
//             {
//                 string content = File.ReadAllText(progressFilePath);
//                 Debug.Log($"{content}");
//                 dataStatus.text = content; // Example: "Status:Uploading,Uploaded:23.45MB,Total:120.50MB,Percent:19.45%"
//             }
//             yield return new WaitForSeconds(30); // check every 5 seconds
//         }
//     }

//     void Update()
//     {
//         if (DataManager.status != "no_upload")
//         {
//             // dataStatus.text = "Data is Uploading...";
//             dataStatus.color = Color.green;
//         }
//             // dataStatus.text = $"{DataManager.status}";

    
//     }
//     IEnumerator CheckUploadStatusRoutine()
//     {
//         while (true)
//         {
//             AppData.Instance.userData.ReadFile();
//             // dataStatus.text = $"{DataManager.status}";
//             if (DataManager.status == "upload_needed" && !hasUploaded)
//             {
//                 hasUploaded = true;
//                 Debug.Log("Upload started...");
//                 RunPythonUploader();
//             }
//             else if (DataManager.status == "no_upload" && hasUploaded)
//             {
//                 Debug.Log("Upload completed. Shutting down...");
//                 ShutdownSystem();
//                 yield break;
//             }
//             else if (DataManager.status == "no_upload")
//             {
                
//                 Debug.Log("Upload completed. Shutting down...");
//                 ShutdownSystem();
//                 yield break;
//             }

//             yield return new WaitForSeconds(60f); // wait 1 minute
//         }
//     }

    

//     void RunPythonUploader()
//     {
//         string pythonScriptPath = @"C:/pythonscripts/uploadToAWS.pyw";
//         // string pythonExecutionPath = @"C:/Users/Homer 6/AppData/Local/Programs/Python/Python313/pythonw.exe";
//         string pythonExecutionPath = @"C:/Program Files/Python312/pythonw.exe";

//         if (!File.Exists(pythonScriptPath))
//         {
//             Debug.LogError("Python script not found: " + pythonScriptPath);
//             return;
//         }

//         try
//         {
//             Process process = new Process();
//             process.StartInfo.FileName = pythonExecutionPath;
//             process.StartInfo.Arguments = $"\"{pythonScriptPath}\"";
//             process.StartInfo.UseShellExecute = false;
//             process.StartInfo.CreateNoWindow = true;
//             process.Start();
//         }
//         catch (System.Exception ex)
//         {
//             Debug.LogError("Error running Python script: " + ex.Message);
//         }
//     }

//     void ShutdownSystem()
//     {
//         try
//         {
//             Application.Quit();
//             // Process.Start("shutdown", "/s /t 0");
//             #if UNITY_EDITOR
//                 UnityEditor.EditorApplication.isPlaying = false; 
//             #endif
//             // Process.Start("shutdown", "/s /t 0");
//         }
//         catch (System.Exception ex)
//         {
//             Debug.LogError("Failed to shutdown: " + ex.Message);
//         }
//     }
// }


using System.Collections;

using System.Collections.Concurrent;

using System.Collections.Generic;

using System.Diagnostics;

using System.IO;

using System.Threading;

using TMPro;
 
using UnityEngine;

using UnityEngine.UI;

using XCharts.Runtime;

using Debug = UnityEngine.Debug;

public class dataUpload : MonoBehaviour

{

    public TextMeshProUGUI dataStatus;

    public string status = null;

    private bool hasUploaded = false; // ensures script runs only
 
    public string message = "waiting...";

    public float progress = 0f;

    public Image progressBar;

    public TextMeshProUGUI numFiles;

    int totalFiles;

    void Start()

    {

        Debug.Log($"status : {DataManager.status}");

        StartCoroutine(CheckUploadStatusRoutine());

    }
 
    void Update()

    {

        if (DataManager.status != "no_upload")

        {

            // dataStatus.text = "Data is Uploading...";

            dataStatus.color = Color.green;

        }

        // dataStatus.text = $"{DataManager.status}";

        dataStatus.text = message;

        numFiles.text = $"Files To Upload : {totalFiles.ToString()}";
 
        if (message == "DONE")

        {

            ShutdownSystem();

        }

        progressBar.fillAmount = progress / 100f;

    }

    public void uploadfile()

    {

        // dataStatus.text = $"{DataManager.status}";

        if (DataManager.status == "upload_needed" && !hasUploaded)

        {

            hasUploaded = true;

            Debug.Log("Upload started...");

            RunPythonUploader();

        }

        else if (DataManager.status == "no_upload" && hasUploaded)

        {

            Debug.Log("Upload completed. Shutting down...");

            ShutdownSystem();

        }

        else if (DataManager.status == "no_upload")

        {
 
            Debug.Log("Upload completed. Shutting down...");

            ShutdownSystem();

        }

    }

    IEnumerator CheckUploadStatusRoutine()

    {

        while (true)

        {

           ReadFile();

            // dataStatus.text = $"{DataManager.status}";

            if (DataManager.status == "upload_needed" && !hasUploaded)

            {

                hasUploaded = true;

                Debug.Log("Upload started...");

                RunPythonUploader();

            }

            else if (DataManager.status == "no_upload" && hasUploaded)

            {

                Debug.Log("Upload completed. Shutting down...");

                ShutdownSystem();

                yield break;

            }

            else if (DataManager.status == "no_upload")

            {
 
                Debug.Log("Upload completed. Shutting down...");

                ShutdownSystem();

                yield break;

            }
 
            yield return new WaitForSeconds(60f); // wait 1 minute

        }

    }

    public void ReadFile()

    {

        if (!File.Exists(DataManager.GetUploadStatusFile))

        {

            Debug.LogError("File not found: " + DataManager.GetUploadStatusFile);

            return;

        }
 
        string[] lines = File.ReadAllLines(DataManager.GetUploadStatusFile);

        string status;
 
        foreach (string line in lines)

        {

            if (string.IsNullOrWhiteSpace(line)) continue;
 
            string[] parts = line.Split(',');
 
            if (parts.Length > 1)

            {

                status = parts[1].Trim(); // second column

                DataManager.setStatus(status);
 
                if (status == "upload_needed")

                {

                    // dataStatus.text = "Upload needed";

                    Debug.Log("Upload is needed!");

                }

                else if (status == "no_upload")

                {

                    // dataStatus.text = "No upload required";

                    Debug.Log("No upload required.");

                }

                else

                {

                    Debug.Log("Unknown status: " + status);

                }

            }

        }

    }
 
 
    void RunPythonUploader()

    {

        string pythonScriptPath = @"C:/pythonscripts/uploadToAWS.pyw";

        // string pythonExecutionPath = @"C:/Users/Homer 6/AppData/Local/Programs/Python/Python313/pythonw.exe";

        // string pythonExecutionPath = @"C:/Users/gokul/AppData/Local/Programs/Python/Python313/pythonw.exe";
        string pythonExecutionPath = @"C:/Program Files/Python312/pythonw.exe";
 
        if (!File.Exists(pythonScriptPath))

        {

            Debug.LogError("Python script not found: " + pythonScriptPath);

            return;

        }
 
        try
        {

            Process process = new Process();

            process.StartInfo.FileName = pythonExecutionPath;

            process.StartInfo.Arguments = $"\"{pythonScriptPath}\"";

            process.StartInfo.UseShellExecute = false;

            process.StartInfo.CreateNoWindow = true;

            // ? Redirect both standard output and error

            process.StartInfo.RedirectStandardOutput = true;

            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += OnPythonOutput;

            process.ErrorDataReceived += OnPythonError;
 
            process.Start();
 
            process.BeginOutputReadLine();

            process.BeginErrorReadLine();
 
            // Optional: Thread to wait for process exit

            new Thread(() =>

            {

                process.WaitForExit();

                Debug.Log("Python uploader finished.");

            }).Start();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error running Python script: " + ex.Message);
        }
    }

    private void OnPythonOutput(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;

        Debug.Log($"[Python] {e.Data}");

        // --- Handle total file info (don't show on UI) ---

        if (e.Data.StartsWith("TOTAL_FILES:"))
        {
            totalFiles = int.Parse(e.Data.Split(':')[1]);
            return; // Skip showing in UI text
        }
        if (e.Data.StartsWith("COMMAND:"))
        {
            message = e.Data.Split(':')[1];
            return; // Skip showing in UI text
        }
        // Try to parse progress (Python prints numbers like 25.0, 50.0, etc.)
        if (float.TryParse(e.Data, out float p))
        {
            progress = Mathf.Clamp(p, 0f, 100f);
        }
        else if (e.Data == "DONE")
        {
            progress = 100f;
            Debug.Log("Upload complete!");
            message = e.Data;
        }
        else if (e.Data.StartsWith("ERROR"))
        {
            Debug.LogError(e.Data);
            message = e.Data;
            ShutdownSystem();
        }
    }
 
    private void OnPythonError(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            Debug.LogError("[Python ERROR] " + e.Data);
        }
    }
 
    void ShutdownSystem()
    {
        try
        {
            Application.Quit();
            // Process.Start("shutdown", "/s /t 0");

            #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
            #endif

            // Process.Start("shutdown", "/s /t 0");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to shutdown: " + ex.Message);
        }
    }
}

 