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
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SHOWMESSAGE();
        checkForConfigFile();
        
    }
    public void onclickConfigure()
    {
        message.text = "fetching detials ....";
        string hospitalID = hospitalIdField.text;
        configFilePath = $"{Application.dataPath}/data/{hospitalID}/data/configdata.csv";
        string Location = location.options[location.value].text;
        awsManager.AppSetups(hospitalID,Location);
        Task.Run(() =>  // Run in a background task
        {
           
            awsManager.RunAWSpythonScript();
            
        });
    
    }
    public  void SHOWMESSAGE()
    {
        string data = awsManager.getmessage();
        if (data != "")
        {
            message.text = awsManager.getmessage();
            //SceneManager.LoadScene("MAIN");
        }
        
    }
    public void checkForConfigFile()
    {
        if (File.Exists(configFilePath))
        {
            Debug.Log("Logged in");
            SceneManager.LoadScene("MAIN");
        }
    }
}
