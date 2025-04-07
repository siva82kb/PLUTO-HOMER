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
    private string mechSelected = null;
    private string nextScene = "CALIB";
    private string exitScene = "SUMM";

    void Start()
    {
        // Reset mechanisms.
        PlutoComm.sendHeartbeat();
        PlutoComm.calibrate("NOMECH");
        AppData.Instance.selectedMechanism = null;

        // Initialize if needed
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        Debug.Log(PlutoComm.MECHANISMS[PlutoComm.mechanism]);
        AppLogger.SetCurrentMechanism(PlutoComm.MECHANISMS[PlutoComm.mechanism]);

        // Update timescale
        Time.timeScale = Time.timeScale == 0 ? 1 : Time.timeScale;

        // Attach callbacks.
        AttachCallbacks();

        // Update the options that are to be displayed.
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

    private void AttachCallbacks()
    {
        // Attach PLUTO button event
        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;

        // Exit and Next buttons
        exit.onClick.AddListener(OnExitButtonClicked);
        nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void UpdateMechanismToggleButtons()
    {
        foreach (Transform child in mehcanismSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            bool isPrescribed = AppData.Instance.userData.mechMoveTimePrsc[toggleComponent.name] > 0;

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
                    timeLeftText.text = $"{AppData.Instance.userData.getTodayMoveTimeForMechanism(toggleComponent.name)} / {AppData.Instance.userData.mechMoveTimePrsc[toggleComponent.name]} min";
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
                mechSelected = child.name;
                AppData.Instance.selectedMechanism = new PlutoMechanism(mechSelected, AppData.Instance.trainingSide);
                AppLogger.LogInfo($"Selected mechanism '{mechSelected}'.");
                return;
            }
        }
        mechSelected = null;
        AppData.Instance.selectedMechanism = null;
    }

    private void OnPlutoButtonReleased()
    {
        if (mechSelected != null)
        {
            changeScene = true;
            mechSelected = null;
        }
    }

    void LoadNextScene()
    {
        // Set the mechanism.
        AppLogger.LogInfo($"Switching scene to '{nextScene}'.");
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator LoadSummaryScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("SUMM");
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
        if (mechSelected != null)
        {
            LoadNextScene();
            mechSelected = null;
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