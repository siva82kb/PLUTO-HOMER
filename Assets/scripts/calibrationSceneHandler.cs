using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static AppData;

public class calibrationSceneHandler : MonoBehaviour
{
    private string selectedMechanism;
    private bool isCalibrating = false;
    private float togetherPosition = 0.0f;    
    private float separationPosition = 11.0f;  
    public TextMeshProUGUI textMessage;
    public TextMeshProUGUI mechText;
    private static bool connect = false;

    void Start()
    {
        selectedMechanism = MechanismSelection.selectedOption;
        int mechNumber = PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, selectedMechanism);
        mechText.text = PlutoComm.MECHANISMSTEXT[mechNumber];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && !isCalibrating)
        {
         PerformCalibration();
        }

        if (ConnectToRobot.isPLUTO && !connect)
        {
            PlutoComm.OnButtonReleased += onPlutoButtonReleased;
            connect = true;
        }

        if (isCalibrating)
        {
            PerformCalibration();
            isCalibrating = false;
        }
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
                Debug.Log("WFE CALIBRATION");
                //StartCoroutine(autoCalibrateWFE());
                break;

            case "WUD":
                Debug.Log("WUD CALIBRATION");
                //StartCoroutine(autoCalibrateWUD());
                break;

            case "FPS":
                Debug.Log("FPS CALIBRATION");
                //StartCoroutine(autoCalibrateFPS());
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

        ApplyTorqueToMoveHandles(currentDistance, togetherPosition);
        yield return new WaitForSeconds(1.0f);

        float currentDistance1 = PlutoComm.getHOCDisplay(PlutoComm.angle);
        if (!CheckPositionTogether(currentDistance1, togetherPosition)) yield break;

        PlutoComm.calibrate(selectedMechanism);

        ApplyTorqueToMoveHandles(currentDistance, separationPosition);

        yield return new WaitForSeconds(1.0f);
        currentDistance = PlutoComm.getHOCDisplay(PlutoComm.angle);
        if (!CheckPositionSeparation(currentDistance, separationPosition)) yield break;

        ApplyTorqueToMoveHandles(currentDistance, togetherPosition);

        yield return new WaitForSeconds(1.0f);
        currentDistance = PlutoComm.getHOCDisplay(PlutoComm.angle);

        isCalibrating = false;
        textMessage.text = "Calibration Done";
        textMessage.color = new Color32(62, 214, 111, 255);
        PlutoComm.setControlType(PlutoComm.CONTROLTYPE[0]);
    }

    private void ApplyTorqueToMoveHandles(float currentPos, float targetPos)
    {
        float distance = targetPos - currentPos;
        float torqueValue = (distance > 0) ? -0.09f : 0.09f;   // torque values Nm
        PlutoComm.setControlType("TORQUE");
        PlutoComm.setControlTarget(torqueValue);
    }

    private void onPlutoButtonReleased()
    {
        isCalibrating = true;
    }

    
    private bool CheckPositionTogether(float currentPosition, float targetPosition)
    {
        if (currentPosition <= 1.5f)
        {
            return true;
        }
        else
        {
            textMessage.text = $"Error: Together Position NOT reached! Current: {currentPosition}";
            textMessage.color = Color.red;
            isCalibrating = false;
            return false;
        }
    }


    private bool CheckPositionSeparation(float currentPosition, float targetPosition)
    {
        if (currentPosition >= 9.0f)
        {
            return true;
        }
        else
        {
            textMessage.text = $"Error: Separation Position NOT reached! Current: {currentPosition}";
            textMessage.color = Color.red;
            isCalibrating = false;
            return false;
        }
    }

    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= onPlutoButtonReleased;
        }
    }
}
