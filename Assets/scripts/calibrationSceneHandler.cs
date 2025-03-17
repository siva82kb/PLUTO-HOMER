using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static AppData;
using System;

public class calibrationSceneHandler : MonoBehaviour
{
    public TextMeshProUGUI textMessage;
    public TextMeshProUGUI mechText;
    public TextMeshProUGUI angText;
    public Button exit;
    
    private string selectedMechanism;
    private bool isCalibrating = false;
    //private float togetherPosition = 0.0f;
    //private float togetherAngle = 0f;
    //private float separationPosition = 11.0f;
    //private float separationAngle = 180.0f;
    //private float separationAngleWFE = 140.0f;
    //private static bool connect = false;
    private string prevScene = "chooseMechanism";
    private string nextScene = "chooseGame";

    void Start()
    {
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        selectedMechanism = AppData.selectedMechanism;
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
        if ((Input.GetKeyDown(KeyCode.C) && !isCalibrating) || isCalibrating)
        {
            PerformCalibration();
            isCalibrating = false;
        }
        angText.text = PlutoComm.angle.ToString("F3");
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
        int mechInx = Array.IndexOf(PlutoComm.MECHANISMS, selectedMechanism);
        float _angval = PlutoComm.angle + PlutoComm.MECHOFFSETVALUE[mechInx];
        isCalibrating = false;
        if (Math.Abs(_angval) < 0.9 * PlutoComm.CALIBANGLE[mechInx]
            || Math.Abs(_angval) > 1.1 * PlutoComm.CALIBANGLE[mechInx])
        {
            // Error in calibration
            PlutoComm.setControlType("NONE");
            textMessage.text = $"Try Again.";
            textMessage.color = Color.red;
            yield break;
        }
        // All good.
        textMessage.text = "Calibration Done";
        textMessage.color = new Color32(62, 214, 111, 255);

        // Move the robot to the neutral position.
        PlutoComm.setControlType("POSITION");
        PlutoComm.setControlBound(1f);
        // Set the target to zero slowly.
        float _initAngle = PlutoComm.angle;
        int N = 20;
        for (int i = 0; i < N; i++)
        {
            PlutoComm.setControlTarget((N - i) * _initAngle / N);
            yield return new WaitForSeconds(0.1f);
        }
        PlutoComm.setControlTarget(0.0f);
        PlutoComm.setControlType("NONE");
        yield return new WaitForSeconds(1.5f);

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
        isCalibrating = true;
    }


    private bool CheckPositionTogether(float currentPosition, float targetPosition)
    {
        float targetPos = targetPosition + 1.5f;
        if (currentPosition <= targetPos)
        {
            return true;
        }
        else
        {
            errMsg();
            return false;
        }
    }

    private bool CheckPositionSeparation(float currentPosition, float targetPosition)
    {
        if (selectedMechanism == "HOC") {
            float targetPos = targetPosition - 3f;
            if (currentPosition >= targetPos)
            {
                return true;
            }
            else
            {
                errMsg();
                return false;
            }
        }
        else
        {
            float targetPos = targetPosition - 2f;
            if (currentPosition >= targetPos)
            {
                return true;
            }
            else
            {
                errMsg();
                return false;
            }
        }

    }

    private void errMsg()
    {
        textMessage.text = $"Try Again.";
        textMessage.color = Color.red;
        isCalibrating = false;
        PlutoComm.calibrate(AppData.selectedMechanism);
        PlutoComm.setControlType(PlutoComm.CONTROLTYPE[0]);
    }

    private void displaySuccessMessage()
    {
        isCalibrating = false;
        textMessage.text = "Calibration Done";
        textMessage.color = new Color32(62, 214, 111, 255);
        
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
