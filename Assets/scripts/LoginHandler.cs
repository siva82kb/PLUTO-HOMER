using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public class LoginHandler : MonoBehaviour
{
    public TMP_Dropdown userDropdown;
    public TMP_InputField[] configInputs; // Assign in inspector
    public Button saveButton, createButton, editButton;
    private string configFileName = "configdata.csv";
    private bool isEditing = false;

    private string baseDataPath;
    private string currentUserPath;
    private string[] headers;
    private string[] originalValues;
    private string hospitalID;

    void Start()
    {
        if (AppData.isNRSVersion)
        {
            baseDataPath = Path.Combine(Application.dataPath, "data");
            if (!Directory.Exists(baseDataPath))
            {
                Debug.LogWarning("Data folder not found.");
                SceneManager.LoadScene("CONFIG");
            }
            LoadUserFolders();
            userDropdown.onValueChanged.AddListener(OnUserSelected);
            saveButton.onClick.AddListener(SaveIfChanged);
            createButton.onClick.AddListener(createConfig);
            editButton.onClick.AddListener(EnableEditing);

            foreach (var input in configInputs)
            {
                input.interactable = false; 
            }
            if (userDropdown.options.Count > 0)
            {
                OnUserSelected(userDropdown.value); 
            }
        }
        else
        {
            SceneManager.LoadScene("MAIN");
            return;
        }

    }

    void LoadUserFolders()
    {
        if (!Directory.Exists(baseDataPath))
        {
            Debug.LogWarning("Data folder not found");
            SceneManager.LoadScene("CONFIG");
            return;
        }

        var folders = Directory.GetDirectories(baseDataPath)
                               .Select(Path.GetFileName)
                               .ToList();
        if (folders.Count == 0)
        {
            Debug.LogWarning("No user folders found inside data folder");
            SceneManager.LoadScene("CONFIG");
            return;
        }

        userDropdown.ClearOptions();
        userDropdown.AddOptions(folders);
    }

    void OnUserSelected(int index)
    {
        string selectedFolder = userDropdown.options[index].text;
        hospitalID = selectedFolder;
        currentUserPath = Path.Combine(baseDataPath, selectedFolder, "data");
        string csvPath = Path.Combine(currentUserPath, configFileName);
        //Debug.Log(csvPath);

        string[] requiredFields = new string[] { "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
        {
           // Debug.LogWarning("Config file is empty or invalid.");
            return;
        }

        headers = lines[0].Split(',');
        //Debug.Log(headers != null);
        //Debug.Log(headers[6]);
        string[] lastLine = lines[lines.Length - 1].Split(',');
        originalValues = lines[lines.Length - 1].Split(',');

        Dictionary<string, string> fieldValueMap = new Dictionary<string, string>();

        for (int i = 0; i < headers.Length; i++)
        {
            if (requiredFields.Contains(headers[i]))
            {
                fieldValueMap[headers[i]] = i < lastLine.Length ? lastLine[i] : "";
               // Debug.Log(lastLine[i]);
            }
        }

        for (int i = 0; i < configInputs.Length; i++)
        {
            string key = requiredFields[i];
            configInputs[i].text = fieldValueMap.ContainsKey(key) ? fieldValueMap[key] : "";
        }

    }
   
    void SaveIfChanged()
    {
        if (originalValues != null)
        {
            string[] updatedValues = (string[])originalValues.Clone();
            bool anyChanges = false;
            string[] editableFields = new string[] { "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };

            for (int i = 0; i < editableFields.Length; i++)
            {
                string field = editableFields[i];
                int index = Array.IndexOf(headers, field);

                if (index >= 0 && index < updatedValues.Length)
                {
                    string newValue = configInputs[i].text;

                    if (updatedValues[index] != newValue)
                    {
                        updatedValues[index] = newValue;
                        anyChanges = true;
                    }
                }
            }

            if (anyChanges)
            {
                string newLine = string.Join(",", updatedValues);
                string csvPath = Path.Combine(currentUserPath, configFileName);
                File.AppendAllText(csvPath, newLine + Environment.NewLine);
            }
            else
            {
                Debug.Log("No changes detected.");
            }
        }
        else
        {
            Debug.LogError("originalValues is null");
        }
        Debug.Log(hospitalID);
        AppData.Instance.setUser(hospitalID);
        SceneManager.LoadScene("MAIN");
    }


    void createConfig(){
        SceneManager.LoadScene("CONFIG");
    }
    public void EnableEditing()
    {
        isEditing = true;

        foreach (var input in configInputs)
        {
            input.interactable = true;
        }
    }

}
