using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static AppData;
using System;
using Unity.VisualScripting;

public class calibrationSceneHandler : MonoBehaviour
{
    public TextMeshProUGUI textMessage;
    public TextMeshProUGUI mechText;
    public TextMeshProUGUI angText;
    public Button exit;
    private bool startCalibration = false;
    private bool isCalibrating = false;
    private bool doneCalibration = false;
    private string prevScene = "chooseMechanism";
    private string nextScene = "chooseGame";

    void Start()
    {
        // Check if user is not initialized.

        // Set mechanism to NOMECH.
        PlutoComm.sendHeartbeat();
        // Set mechanism to the selected mechanism.
        PlutoComm.calibrate(AppData.selectedMechanism);
        
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        mechText.text = PlutoComm.MECHANISMSTEXT[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, selectedMechanism)];

        // Attach callback.
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
        }
        exit.onClick.AddListener(OnExitButtonClicked);
    }

    void Update()
    {
        PlutoComm.sendHeartbeat();
        angText.text = PlutoComm.angle.ToString("F3");
        // Check of calibration is started.
        if (!isCalibrating && startCalibration)
        {
            PerformCalibration();
            startCalibration = false;
        }
    }

    private void PerformCalibration()
    {
        if (string.IsNullOrEmpty(selectedMechanism))
        {
            Debug.LogError("No mechanism selected for calibration!");
            return;
        }

        // Start the calibration process.
        StartCoroutine(autoCalibrate());
    }

    IEnumerator autoCalibrate()
    {
        textMessage.color = Color.black;
        textMessage.text = "Calibrating...";

        // Move the robot to the extreme position.
        ApplyCounterClockwiseTorque();
        yield return new WaitForSeconds(1.5f);

        // Send the calibration command.
        PlutoComm.calibrate(AppData.selectedMechanism);
        yield return new WaitForSeconds(0.5f);

        //ApplyTorqueToSep(PlutoComm.angle, separationAngle);
        ApplyClockwiseTorque();
        yield return new WaitForSeconds(1.5f);

        // Check if the ROM is correct.
        int mechInx = Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism);
        float _angval = PlutoComm.angle + PlutoComm.MECHOFFSETVALUE[mechInx];
        isCalibrating = false;
        if (Math.Abs(_angval) < 0.9 * PlutoComm.CALIBANGLE[mechInx]
            || Math.Abs(_angval) > 1.1 * PlutoComm.CALIBANGLE[mechInx])
        {
            // Error in calibration
            PlutoComm.setControlType("NONE");
            PlutoComm.calibrate("NOMECH");
            textMessage.text = $"Try Again.";
            textMessage.color = Color.red;
            AppLogger.LogError($"Calibration failed for {AppData.selectedMechanism}.");
            isCalibrating = false;
            doneCalibration = false;
            yield break;
        }
        // All good.
        textMessage.text = "Calibration Done";
        textMessage.color = new Color32(62, 214, 111, 255);
        AppLogger.LogError($"Calibration was successful for {AppData.selectedMechanism}.");

        //HOC assessment UI  works based on closed position,
        if(PlutoComm.MECHANISMS[PlutoComm.mechanism] != "HOC") {
            // Move the robot to the neutral position.
            PlutoComm.setControlType("POSITION");
            // Set the target to zero slowly.
            float _initAngle = PlutoComm.angle;
            int N = 20;
            for (int i = 0; i < N; i++)
            {
                PlutoComm.setControlBound(1.0f * (i + 1) / N);
                PlutoComm.setControlTarget((N - i) * _initAngle / N);
                yield return new WaitForSeconds(0.1f);
            }
        }
        if (PlutoComm.MECHANISMS[PlutoComm.mechanism] == "HOC") PlutoComm.calibrate(AppData.selectedMechanism);

        PlutoComm.setControlTarget(0.0f);
        PlutoComm.setControlType("NONE");
        yield return new WaitForSeconds(1.5f);

        // Set selected mechanism.
        AppData.selectedMechanism = PlutoComm.MECHANISMS[PlutoComm.mechanism];
        AppLogger.SetCurrentMechanism(AppData.selectedMechanism);

        // Update flags.
        isCalibrating = false;
        doneCalibration = true;

        // Go to the next scene.
        Invoke("LoadNextScene", 0.4f);
    }

    void LoadNextScene()
    {
        AppLogger.LogInfo($"Switching scene to '{nextScene}'.");
        SceneManager.LoadScene(nextScene);
    }

    private void ApplyCounterClockwiseTorque()
    {
        float torqueValue = -0.1f;
        PlutoComm.setControlType("TORQUE");
        PlutoComm.setControlTarget(torqueValue);
    }

    private void ApplyClockwiseTorque()
    {
        float torqueValue = 0.1f;
        PlutoComm.setControlType("TORQUE");
        PlutoComm.setControlTarget(torqueValue);
    }

    private void OnPlutoButtonReleased()
    {
        if (!doneCalibration && !isCalibrating && !startCalibration)
        {
            startCalibration = true;
        }
    }

    private void OnExitButtonClicked()
    {
        SceneManager.LoadScene(prevScene);
    }

    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= OnPlutoButtonReleased;
        }
    }
}