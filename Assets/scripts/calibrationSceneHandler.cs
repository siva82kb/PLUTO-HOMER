using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static AppData;

public class calibrationSceneHandler : MonoBehaviour
{
    private string selectedMechanism;
    private bool isCalibrating = false;
    private float togetherPosition = 0.0f;
    private float togetherAngle = 0f;
    private float separationPosition = 11.0f;
    private float separationAngle = 180.0f;
    private float separationAngleWFE = 140.0f;
    public TextMeshProUGUI textMessage;
    public TextMeshProUGUI mechText;
    public TextMeshProUGUI angText;
    private static bool connect = false;
    public Button exit;
    private string prevScene = "chooseMechanism";
    private string nextScene = "choosegame";



    void Start()
    {

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"{SceneManager.GetActiveScene().name} scene started.");
        selectedMechanism = AppData.selectedMechanism;
        mechText.text = PlutoComm.MECHANISMSTEXT[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, selectedMechanism)];
        exit.onClick.AddListener(OnExitButtonClicked);
    }

    void Update()
    {
        PlutoComm.sendHeartbeat();
        if (Input.GetKeyDown(KeyCode.C) && !isCalibrating)
        {
            PerformCalibration();
        }

        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
        }

        if (isCalibrating)
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

        switch (selectedMechanism)
        {
            case "HOC":
                StartCoroutine(autoCalibrateHOC());
                break;

            case "WFE":
            case "WURD":
                StartCoroutine(autoCalibrate(togetherAngle, separationAngleWFE));
                break;

            case "FPS":
                StartCoroutine(autoCalibrate(togetherAngle, separationAngle));
                break;

            case "FME1":
            case "FME2":
                StartCoroutine(autoCalibrate(togetherAngle, separationAngle));
                break;

            default:
                Debug.LogError("Unknown mechanism type selected: " + selectedMechanism);
                break;
        }
    }


    IEnumerator autoCalibrateHOC()
    {

        textMessage.color = Color.black;
        textMessage.text = "Calibrating...";

        float currentDistance = PlutoComm.getHOCDisplay(PlutoComm.angle);
        ApplyTorqueToSep(currentDistance, togetherPosition);
        yield return new WaitForSeconds(1.5f);


        PlutoComm.calibrate(selectedMechanism);
        
        ApplyTorque(PlutoComm.getHOCDisplay(PlutoComm.angle), separationPosition);

        yield return new WaitForSeconds(1.5f);

        if (!CheckPositionSeparation(PlutoComm.getHOCDisplay(PlutoComm.angle), separationPosition)) yield break;

        ApplyTorqueToSep(PlutoComm.getHOCDisplay(PlutoComm.angle), togetherPosition);

        yield return new WaitForSeconds(1.5f);
        if (!CheckPositionTogether(PlutoComm.getHOCDisplay(PlutoComm.angle), togetherPosition)) yield break;
        textMsg();
        Invoke("LoadNextScene", 0.4f);
    }

    IEnumerator autoCalibrate(float togetherAngle, float separationAngle)
    {
        textMessage.color = Color.black;
        textMessage.text = "Calibrating...";

        float currentAngle = PlutoComm.angle;
        float temp0 = -90f;
        float temp1 = 90f;
       // ApplyTorque(currentAngle, togetherAngle);
        ApplyTorque(currentAngle, temp0);
        yield return new WaitForSeconds(1.5f);

        PlutoComm.calibrate(AppData.selectedMechanism);

        //ApplyTorqueToSep(PlutoComm.angle, separationAngle);
        ApplyTorqueToSep(PlutoComm.angle, temp1);

        yield return new WaitForSeconds(1.5f); 
        if (!CheckPositionSeparation(PlutoComm.angle, temp1)) yield break;
        ApplyTorque(PlutoComm.angle, temp0);


        yield return new WaitForSeconds(1.5f);

        if (!CheckPositionTogether(PlutoComm.angle, temp0)) yield break;
        textMsg();
         
        Invoke("LoadNextScene", 0.4f);
    }

    void LoadNextScene()
    {
        AppLogger.LogInfo($"Switching scene to '{nextScene}'.");
        SceneManager.LoadScene(nextScene);
    }
    private void ApplyTorque(float currentPos, float targetPos)
    {
        float torqueValue = -0.1f;
        PlutoComm.setControlType("TORQUE");
        PlutoComm.setControlTarget(torqueValue);
    }
    private void ApplyTorqueToSep(float currentPos, float targetPos)
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

    private void textMsg()
    {
        isCalibrating = false;
        textMessage.text = "Calibration Done";
        textMessage.color = new Color32(62, 214, 111, 255);
        PlutoComm.setControlType(PlutoComm.CONTROLTYPE[0]);
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
