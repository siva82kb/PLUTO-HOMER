using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using static AppData;
using System.Collections;

public class MechanismSceneHandler : MonoBehaviour
{
    public GameObject toggleGroup;
    public TMP_Text timePh_FE;
    public TMP_Text timePh_URD;
    public TMP_Text timePh_PS;
    public TMP_Text timePh_HOC;
    public TMP_Text timePh_FKT;

    public TMP_Text feVal;
    public TMP_Text urdVal;
    public TMP_Text psVal;
    public TMP_Text hocVal;
    public TMP_Text fktVal;

    public Button exitButton;
    public Button nextButton;
    private static bool scene = false;
    public static readonly string[] MECHANISMS = new string[] { "WFE", "WUD", "FPS", "HOC"};

    private string filePath = Application.dataPath + "/data/config_data.csv";

    private bool toggleSelected = false;  // Variable to track toggle selection state

    void Start()
    {
        //AttachToggleListeners();
        string csvPath = Application.dataPath + "/data/sessions/sessions.csv";
        SessionDataHandler sessionHandler = new SessionDataHandler(csvPath);
        Dictionary<string, double> mechanismTimes = sessionHandler.CalculateTotalTimeForMechanisms(DateTime.Now);

        foreach (var kvp in mechanismTimes)
        {
            Debug.Log($"{kvp.Key} - {kvp.Value} mins");
        }

        updateMechanismTimeBoxes(mechanismTimes);
        updateTogglesBasedOnCSV();

        PlutoComm.OnButtonReleased += onPlutoButtonReleased;

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitScene);

        if (nextButton != null)
            nextButton.onClick.AddListener(() =>
            {
                if (toggleSelected)  // Check the toggleSelected boolean
                    LoadNextScene();
                else
                    Debug.LogWarning("Select at least one toggle to proceed.");
            });
        DeselectAllToggles();
        // Attach listeners to the toggles to update the toggleSelected variable
        StartCoroutine(DelayedAttachListeners());

    }

    IEnumerator DelayedAttachListeners()
    {
        yield return new WaitForSeconds(1f);  // Allow UI to fully initialize
        AttachToggleListeners();
    }

    void DeselectAllToggles()
    {
        foreach (Transform child in toggleGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null)
            {
                toggleComponent.isOn = false;  // Reset all toggles to off
            }
        }
    }

    void AttachToggleListeners()
    {
        // Attach onValueChanged listener to each toggle in the toggle group
        foreach (Transform child in toggleGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null)
            {
                // Update toggleSelected whenever a toggle's value changes
                toggleComponent.onValueChanged.AddListener(delegate { CheckToggleStates(); });
            }
        }
    }

    void CheckToggleStates()
    {
        // Check if any toggle in the group is currently selected
        toggleSelected = false;
        foreach (Transform child in toggleGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null && toggleComponent.isOn)
            {
                toggleSelected = true;
                MechanismSelection.selectedOption = child.name;  // Store the selected toggle name
                break;
            }
        }
    }

    void Update()
    {
        if (scene == true)
        {
            LoadNextScene();
            scene = false;
        }
    }

    void SetToggleState(string toggleName, string value)
    {
        Toggle targetToggle = FindToggleByName(toggleName);

        if (targetToggle != null)
        {
            bool isEnabled = value.Trim() != "0";
            targetToggle.isOn = isEnabled;
            targetToggle.interactable = isEnabled;
            targetToggle.gameObject.SetActive(isEnabled);
            updateTextBoxVisibilityBasedOnToggle(targetToggle, isEnabled);
        }
        else
        {
            Debug.LogWarning("Toggle with name " + toggleName + " not found under the Toggle Group.");
        }
    }

    void updateTextBoxVisibilityBasedOnToggle(Toggle toggle, bool isEnabled)
    {
        switch (toggle.name.ToLower())
        {
            case "wfe":
                if (timePh_FE != null)
                    timePh_FE.gameObject.SetActive(isEnabled);
                break;
            case "wurd":
                if (timePh_URD != null)
                    timePh_URD.gameObject.SetActive(isEnabled);
                break;
            case "fps":
                if (timePh_PS != null)
                    timePh_PS.gameObject.SetActive(isEnabled);
                break;
            case "hoc":
                if (timePh_HOC != null)
                    timePh_HOC.gameObject.SetActive(isEnabled);
                break;
            default:
                Debug.LogWarning("No matching text box found for toggle: " + toggle.name);
                break;
        }
    }

    Toggle FindToggleByName(string toggleName)
    {
        foreach (Transform child in toggleGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null && child.name == toggleName)
            {
                return toggleComponent;
            }
        }
        return null;
    }

    void updateMechanismTimeBoxes(Dictionary<string, double> mechanismTimes)
    {
        if (timePh_FE != null && mechanismTimes.ContainsKey("WFE"))
        {
            timePh_FE.text = $"Time: {mechanismTimes["WFE"]}";
            timePh_FE.ForceMeshUpdate();
        }

        if (timePh_URD != null && mechanismTimes.ContainsKey("WURD"))
        {
            timePh_URD.text = $"Time: {mechanismTimes["WURD"]}";
            timePh_URD.ForceMeshUpdate();
        }

        if (timePh_PS != null && mechanismTimes.ContainsKey("PS"))
        {
            timePh_PS.text = $"Time: {mechanismTimes["PS"]}";
            timePh_PS.ForceMeshUpdate();
        }

        if (timePh_HOC != null && mechanismTimes.ContainsKey("HOC"))
        {
            timePh_HOC.text = $"Time: {mechanismTimes["HOC"]}";
            timePh_HOC.ForceMeshUpdate();
        }
    }

    void updateTogglesBasedOnCSV()
    {
        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length > 1)
            {
                string lastLine = lines[lines.Length - 1];
                string[] values = lastLine.Split(',');

                SetToggleState("WFE", values[7]);
                SetToggleState("WUD", values[8]);
                SetToggleState("FPS", values[9]);
                SetToggleState("HOC", values[10]);

                if (values.Length >= 10)
                {
                    updateTimeBoxes(values[7], values[8], values[9], values[10]);
                }
                else
                {
                    Debug.LogWarning("CSV file does not contain sufficient time data.");
                }
            }
            else
            {
                Debug.LogWarning("CSV file does not contain sufficient data.");
            }
        }
        else
        {
            Debug.LogError("CSV file not found at path: " + filePath);
        }
    }

    void updateTimeBoxes(string time1, string time2, string time3, string time4)
    {
        if (feVal != null)
            feVal.text = (time1.Trim() == "0" || string.IsNullOrEmpty(time1.Trim())) ? "" : "  / " + time1.Trim() + " Mins";

        if (urdVal != null)
            urdVal.text = (time2.Trim() == "0" || string.IsNullOrEmpty(time2.Trim())) ? "" : "  / " + time2.Trim() + " Mins";

        if (psVal != null)
            psVal.text = (time3.Trim() == "0" || string.IsNullOrEmpty(time3.Trim())) ? "" : "  / " + time3.Trim() + " Mins";

        if (hocVal != null)
            hocVal.text = (time4.Trim() == "0" || string.IsNullOrEmpty(time4.Trim())) ? "" : " / " + time4.Trim() + " Mins";
    }

    void ExitScene()
    {
        SceneManager.LoadScene("welcome");
    }

    public void onPlutoButtonReleased()
    {
        if (toggleSelected)  // Use the toggleSelected boolean to verify before scene change
            scene = true;
        else
            Debug.LogWarning("Select at least one toggle to proceed.");
    }

    void LoadNextScene()
    {
        Debug.Log(MechanismSelection.selectedOption + " selected Option");
        SceneManager.LoadScene("calibration");
    }
}

