using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OneTimeConfig : MonoBehaviour
{
    public TMP_InputField nameField;
    public TMP_InputField ageField;
    public TMP_InputField hospitalIdField;
    public TMP_InputField startDateField;
    public TMP_InputField endDateField;

    public TMP_InputField wfeField;
    public TMP_InputField wurdField;
    public TMP_InputField fpsField;
    public TMP_InputField hocField;
    public TMP_InputField fme1Field;
    public TMP_InputField fme2Field;
    public TMP_Dropdown affectedSideDropdown;

    public TextMeshProUGUI totalDurationText;


    private void Start()
    {
        // Automatically set startDateField and endDateField
        DateTime startDate = DateTime.Now;
        DateTime endDate = startDate.AddDays(30);

        startDateField.text = startDate.ToString("dd-MM-yyyy");
        endDateField.text = endDate.ToString("dd-MM-yyyy");

        wfeField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        wurdField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        fpsField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        hocField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        fme1Field.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        fme2Field.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
    }

    private void UpdateTotalDuration()
    {
        int totalDuration = 0;

        totalDuration += ParseField(wfeField);
        totalDuration += ParseField(wurdField);
        totalDuration += ParseField(fpsField);
        totalDuration += ParseField(hocField);
        totalDuration += ParseField(fme1Field);
        totalDuration += ParseField(fme2Field);

        totalDurationText.text = totalDuration.ToString();
    }

    private int ParseField(TMP_InputField field)
    {
        if (int.TryParse(field.text, out int value))
        {
            return value;
        }
        return 0; 
    }

    public void saveConfig()
    {
        if (string.IsNullOrWhiteSpace(nameField.text) ||
          string.IsNullOrWhiteSpace(ageField.text) ||
          string.IsNullOrWhiteSpace(hospitalIdField.text) ||
          string.IsNullOrWhiteSpace(startDateField.text) ||
          string.IsNullOrWhiteSpace(endDateField.text))
        {
            Debug.LogError("Name, Age, Hospital ID, Start Date, and End Date fields must not be empty.");
            return;
        }

        string date = DateTime.Now.ToString("dd-MM-yyyy");
        string name = nameField.text;
        string age = ageField.text;
        string hospitalId = hospitalIdField.text;
        string startDate = startDateField.text;
        string endDate = endDateField.text;
        
        // Set null to "0".
        string wfe = string.IsNullOrEmpty(wfeField.text) ? "0" : wfeField.text;
        string wurd = string.IsNullOrEmpty(wurdField.text) ? "0" : wurdField.text;
        string fps = string.IsNullOrEmpty(fpsField.text) ? "0" : fpsField.text;
        string hoc = string.IsNullOrEmpty(hocField.text) ? "0" : hocField.text;
        string fme1 = string.IsNullOrEmpty(fme1Field.text) ? "0" : fme1Field.text;
        string fme2 = string.IsNullOrEmpty(fme2Field.text) ? "0" : fme2Field.text;
        //temp
        string Location = "CMC";
        string totalDuration = totalDurationText.text;

        string trainingSide = affectedSideDropdown.options[affectedSideDropdown.value].text;

        string headers = "Date,name,HospitalNumber,Startdate,end ,age,time,WFE,WURD,FPS,HOC,FME1,FME2,TrainingSide,Location";
        string data = $"{date},{name},{hospitalId},{startDate},{endDate},{age},{totalDuration},{wfe},{wurd},{fps},{hoc},{fme1},{fme2},{trainingSide},{Location}";


        string directoryPath = Application.dataPath + "/data";
        string datapath = Path.Combine(directoryPath, "configdata.csv");

        if (File.Exists(datapath))
        {
            Debug.Log("Configuration File Already Exists. you can't update Here");
        }
        else {
            if (!File.Exists(datapath))
            {
                File.WriteAllText(datapath, headers + Environment.NewLine);
                Debug.Log("Data saved to CSV: " + datapath);
            }
            File.AppendAllText(datapath, data + Environment.NewLine);

            SceneManager.LoadScene("MAIN");

        }
        
    }
}
