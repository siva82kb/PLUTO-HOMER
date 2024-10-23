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
    public static readonly string[] MECHANISMS = new string[] { "WFE", "WUD", "FPS", "HOC"};


    private bool toggleSelected = false;  // Variable to track toggle selection state
    private string nextScene = "calibration";
    private string exitScene = "Summary";

    void Start()
    {
        // Initialize if needed
        if (AppData.UserData.dTableConfig == null)
        {
            // Inialize the logger
            AppLogger.StartLogging(SceneManager.GetActiveScene().name);
            // Initialize.
            AppData.initializeStuff();
        }
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");

        // Attach PLUTO button event
        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;

        // Attach a callback to the exit button
        exit.onClick.AddListener(OnExitButtonClicked);
        // Attach a callback to the next button
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() =>
            {
                if (toggleSelected)
                {
                    LoadNextScene();
                }
            });
        }

        // Update toggle buttons.
        UpdateMechanismToggleButtons();
        DeselectAllToggles();

        // Attach listeners to the toggles to update the toggleSelected variable
        StartCoroutine(DelayedAttachListeners());
    }

    void Update()
    {
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

    void DeselectAllToggles()
    {
        foreach (Transform child in mehcanismSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null)
            {
                toggleComponent.isOn = false;  // Reset all toggles to off
            }
        }
    }
    IEnumerator DelayedAttachListeners()
    {
        yield return new WaitForSeconds(1f);  // Allow UI to fully initialize
        AttachToggleListeners();
    }

    void AttachToggleListeners()
    {
        // Attach onValueChanged listener to each toggle in the toggle group
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
        // Check if any toggle in the group is currently selected
        toggleSelected = false;
        AppData.selectMechanism = null;
        foreach (Transform child in mehcanismSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null && toggleComponent.isOn)
            {
                toggleSelected = true;
                AppData.selectMechanism = child.name;
                AppLogger.LogInfo($"Selected '{AppData.selectMechanism}'.");
                break;
            }
        }
    }

    void ExitScene()
    {
        SceneManager.LoadScene("welcome");
    }

    public void OnPlutoButtonReleased()
    {
        if (toggleSelected)
        {
            changeScene = true;
        }
        else
        {
            Debug.LogWarning("Select at least one toggle to proceed.");
        }
    }

    void LoadNextScene()
    {
        AppLogger.LogInfo($"Switching scene to '{nextScene}'.");
        SceneManager.LoadScene(nextScene);
        DeselectAllToggles();
    }

    IEnumerator LoadSummaryScene()
    {
        // Asynchronously load the summary scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("summaryScene");

        // Wait until the new scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Once the new scene is loaded, you don't need to unload the old scene manually
        Debug.Log("summaryScene loaded successfully.");
    }

    private void OnExitButtonClicked()
    {
        Debug.Log("Exit button pressed, loading summaryScene.");
        StartCoroutine(LoadSummaryScene());
    }
}

