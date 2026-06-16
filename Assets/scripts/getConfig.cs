using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.VisualScripting;
using System.IO;
using UnityEngine.SceneManagement;

public class getConfig : MonoBehaviour
{
    public TextMeshProUGUI message;
    public TMP_InputField hospitalIdField;
    public TMP_Dropdown location;
    private string configFilePath;
    private bool isChecking = false;
    private float spinnerRotation = 0f;

    void Start()
    {

    }

    void Update()
    {
        SHOWMESSAGE();
        checkForConfigFile();

        if (isChecking)
        {
            spinnerRotation += 360f * Time.deltaTime / 0.8f; // Full rotation every 0.8 seconds
            if (spinnerRotation >= 360f)
                spinnerRotation -= 360f;
        }
    }

    public void onclickConfigure()
    {
        isChecking = true;
        message.text = "Checking 🔃";
        string hospitalID = hospitalIdField.text;
        configFilePath = $"{Application.dataPath}/data/{hospitalID}/data/configdata.csv";
        string Location = location.options[location.value].text;
        awsManager.AppSetups(hospitalID,Location);
        Task.Run(() =>  // Run in a background task
        {
            awsManager.RunAWSpythonScript();
        });

    }
    public void SHOWMESSAGE()
    {
        if (!isChecking)
            return;

        string data = awsManager.getmessage();
        if (data != "")
        {
            isChecking = false;
            message.text = awsManager.getmessage();
        }
    }

    public void checkForConfigFile()
    {
        if (File.Exists(configFilePath))
        {
            isChecking = false;
            Debug.Log("Logged in");
            SceneManager.LoadScene("MAIN");
        }
    }
}
