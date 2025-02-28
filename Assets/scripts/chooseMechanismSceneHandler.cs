using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Concurrent;

public class MechanismSceneHandler : MonoBehaviour
{
    public GameObject mehcanismSelectGroup;
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

    public Button nextButton;
    public Button exit;
    private static bool changeScene = false;
    private bool toggleSelected = false;
    private string nextScene = "calibration";
    private string exitScene = "Summary";

    void Start()
    {
        // Initialize if needed
        if (AppData.UserData.dTableConfig == null)
        {
            AppLogger.StartLogging(SceneManager.GetActiveScene().name);
            AppData.initializeStuff();
        }
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        AppLogger.SetCurrentMechanism("");
        // Attach PLUTO button event
        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
        PlutoComm.calibrate("NOMECH");
        //checking time scale 
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1;
        }
        exit.onClick.AddListener(OnExitButtonClicked);
        nextButton.onClick.AddListener(OnNextButtonClicked);
        UpdateMechanismToggleButtons();

        // Attach listeners to the toggles to update the toggleSelected variable
        StartCoroutine(DelayedAttachListeners());
    }

    void Update()
    {
        PlutoComm.sendHeartbeat();
        // Check if a scene change is needed.
        if (changeScene == true)
        {
            LoadNextScene();
            changeScene = false;
        }
    }

    private void UpdateMechanismToggleButtons()
    {
        foreach (Transform child in mehcanismSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            bool isPrescribed = AppData.UserData.mechMoveTimePrsc[toggleComponent.name] > 0;
            // Hide the component if it has no prescribed time.
            toggleComponent.interactable = isPrescribed;
            toggleComponent.gameObject.SetActive(isPrescribed);
            // Update the time trained in the timeLeft component of toggleCompoent.
            Transform timeLeftTransform = toggleComponent.transform.Find("timeLeft");
            if (timeLeftTransform != null)
            {
                // Get the TextMeshPro component from the timeLeft GameObject
                TextMeshProUGUI timeLeftText = timeLeftTransform.GetComponent<TextMeshProUGUI>();
                if (timeLeftText != null)
                {
                    // Set the text to your desired value
                    timeLeftText.text = $"{AppData.UserData.getTodayMoveTimeForMechanism(toggleComponent.name)} / {AppData.UserData.mechMoveTimePrsc[toggleComponent.name]} min";
                }
                else
                {
                    Debug.LogError("TextMeshProUGUI component not found in timeLeft GameObject.");
                }
            }
            else
            {
                Debug.LogError("timeLeft GameObject not found in " + toggleComponent.name);
            }
        }
    }

    IEnumerator DelayedAttachListeners()
    {
        yield return new WaitForSeconds(0.3f);  
        AttachToggleListeners();
    }

    void AttachToggleListeners()
    {
        foreach (Transform child in mehcanismSelectGroup.transform)
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
        foreach (Transform child in mehcanismSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null && toggleComponent.isOn)
            {
                toggleSelected = true;
                AppData.selectedMechanism = child.name;
                AppLogger.SetCurrentMechanism(AppData.selectedMechanism);
                AppLogger.LogInfo($"Selected '{AppData.selectedMechanism}'.");
                break;
            }
        }
    }

    public void OnPlutoButtonReleased()
    {
        if (toggleSelected)
        {
            changeScene = true;
            toggleSelected = false;
        }
        else
        {
            Debug.LogWarning("Select at least one toggle to proceed.");
        }
    }

    void LoadNextScene()
    {
        AppLogger.LogInfo($"Switching scene to '{nextScene}'.");
        gameData.setNeutral = false;
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator LoadSummaryScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("summaryScene");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private void OnExitButtonClicked()
    {
        StartCoroutine(LoadSummaryScene());
    }
    private void OnNextButtonClicked()
    {
        if (toggleSelected)
        {
            LoadNextScene();
            toggleSelected = false;
        }
    }
    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= OnPlutoButtonReleased;
        }
    }
}

