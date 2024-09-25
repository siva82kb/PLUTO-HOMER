using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Newtonsoft.Json;

public class AppConfig : MonoBehaviour
{
    public class UserRoot
    {
        public string hospno { get; set; }
        public string Name { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Age { get; set; }
    }
    // Declare directory and file paths
    string directoryPath;
    string directoryPathConfig;
    string directoryPathSession;
    string directoryPathRawData;
    string filePath_UserData;
    string filePath_SessionData;
    public String name{ get; private set; }
    public String hospno { get; private set; }
    public String start { get; private set; }
    public String end { get; private set; }
    public String Age { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
        // Initialize paths in Start method
        directoryPath = Application.dataPath + "/data";
        directoryPathConfig = directoryPath + "/Configuration";
        directoryPathSession = directoryPath + "/Session";
        directoryPathRawData = directoryPath + "/RawData";
        filePath_UserData = directoryPath + "/user.json";
        filePath_SessionData = directoryPathSession + "/sessions.csv";

        createFileStructure();
        readAndGetData();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void createFileStructure()
    {
        // Check if the directory exists
        if (Directory.Exists(directoryPath))
        {
            Debug.Log("Directory already exists: " + directoryPath);
        }
        else
        {
            // If not, create the directory
            Directory.CreateDirectory(directoryPath);
            Directory.CreateDirectory(directoryPathConfig);
            Directory.CreateDirectory(directoryPathSession);
            Directory.CreateDirectory(directoryPathRawData);
            File.Create(filePath_UserData).Dispose(); // Ensure the file handle is released
            File.Create(filePath_SessionData).Dispose(); // Ensure the file handle is released
            Debug.Log("Directory created at: " + directoryPath);
        }

        writeHeader(filePath_SessionData);
    }

    public void writeHeader(string path)
    {
        try
        {
            // Check if the file exists and if it is empty (i.e., no lines in the file)
            if (File.Exists(path) && File.ReadAllLines(path).Length == 0)
            {
                // Define the CSV header string, separating each column with a comma
                string headerData = "SessionNumber,DateTime,Assessment,StartTime,StopTime,GameName,TrialDataFileLocation,DeviceSetupFile,AssistMode,AssistModeParameter";

                // Write the header to the file
                File.WriteAllText(path, headerData + "\n"); // Add a new line after the header
                Debug.Log("Header written successfully.");
            }
            else
            {
                Debug.Log("Writing failed or header already exists.");
            }
        }
        catch (Exception ex)
        {
            // Catch any other generic exceptions
            Debug.LogError("An error occurred while writing the header: " + ex.Message);
        }
    }
    public void readAndGetData()
    {
        try
        {
            // Read the JSON file
            string jsonString = File.ReadAllText(filePath_UserData);
            UserRoot userRoot = JsonConvert.DeserializeObject<UserRoot>(jsonString);
            name = userRoot.Name;
            hospno = userRoot.hospno;
            start = userRoot.Start;
            end = userRoot.End;
            Age = userRoot.Age;
            
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading JSON file: {ex.Message}");
            
        }
    }
}
