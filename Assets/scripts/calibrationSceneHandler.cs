// using System.Collections;
// using UnityEngine;
// using TMPro;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;
// using System;

// public class calibrationSceneHandler : MonoBehaviour
// {
//     public TextMeshProUGUI textMessage;
//     public TextMeshProUGUI mechText;
//     public TextMeshProUGUI angText;
//     public Button exit;
//     private bool startCalibration = false;
//     private bool isCalibrating = false;
//     private bool doneCalibration = false;
//     private string prevScene = "CHMECH";
//     private string nextScene = "CHGAME";

//     void Start()
//     {
//         // Check if user is not initialized.

//         // Set mechanism to NOMECH.
//         PlutoComm.sendHeartbeat();
//         // Set mechanism to the selected mechanism.
//         PlutoComm.calibrate(AppData.Instance.selectedMechanism.name);
        
//         AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
//         AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
//         Debug.Log("Mechanism: " + AppData.Instance.selectedMechanism.name);
//         mechText.text = PlutoComm.MECHANISMSTEXT[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name)];

//         // Attach callback.
//         if (ConnectToRobot.isPLUTO)
//         {
//             PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
//         }
//         exit.onClick.AddListener(OnExitButtonClicked);
//     }

//     void Update()
//     {
//         PlutoComm.sendHeartbeat();
//         angText.text = $" {PlutoComm.angle.ToString("F3")} + {PlutoComm.CONTROLTYPETEXT[PlutoComm.controlType]} + {PlutoComm.MECHANISMSTEXT[PlutoComm.mechanism-1]}";
//         // Check of calibration is started.
//         if (!isCalibrating && startCalibration)
//         {
//             PerformCalibration();
//             startCalibration = false;
//         }
//     }

//     private void PerformCalibration()
//     {
//         if (string.IsNullOrEmpty(AppData.Instance.selectedMechanism.name))
//         {
//             Debug.LogError("No mechanism selected for calibration!");
//             return;
//         }

//         // Start the calibration process.
//         StartCoroutine(autoCalibrate());
//     }

//     IEnumerator autoCalibrate()
//     {
//         textMessage.color = Color.black;
//         textMessage.text = "Calibrating...";

//         // Move the robot to the extreme position.
//         ApplyCounterClockwiseTorque();
//         yield return new WaitForSeconds(1.5f);

//         // Send the calibration command.
//         PlutoComm.calibrate(AppData.Instance.selectedMechanism.name);
//         yield return new WaitForSeconds(0.5f);

//         //ApplyTorqueToSep(PlutoComm.angle, separationAngle);
//         ApplyClockwiseTorque();
//         yield return new WaitForSeconds(1.5f);

//         // Check if the ROM is correct.
//         int mechInx = Array.IndexOf(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name);
//         float _angval = PlutoComm.angle + PlutoComm.MECHOFFSETVALUE[mechInx];
//         isCalibrating = false;
//         if (Math.Abs(_angval) < 0.9 * PlutoComm.CALIBANGLE[mechInx]
//             || Math.Abs(_angval) > 1.1 * PlutoComm.CALIBANGLE[mechInx])
//         {
//             // Error in calibration
//             PlutoComm.setControlType("NONE");
//             PlutoComm.calibrate("NOMECH");
//             textMessage.text = $"Try Again.";
//             textMessage.color = Color.red;
//             AppLogger.LogError($"Calibration failed for {AppData.Instance.selectedMechanism.name}.");
//             isCalibrating = false;
//             doneCalibration = false;
//             yield break;
//         }
//         // All good.
//         textMessage.text = "Calibration Done";
//         textMessage.color = new Color32(62, 214, 111, 255);
//         AppLogger.LogError($"Calibration was successful for '{AppData.Instance.selectedMechanism.name}'.");

//         //HOC assessment UI  works based on closed position,
//         if(PlutoComm.MECHANISMS[PlutoComm.mechanism] != "HOC") {
//             // Move the robot to the neutral position.
//             PlutoComm.setControlType("POSITION");
//             // Set the target to zero slowly.
//             float _initAngle = PlutoComm.angle;
//             int N = 20;
//             for (int i = 0; i < N; i++)
//             {
//                 PlutoComm.setControlBound(1.0f * (i + 1) / N);
//                 PlutoComm.setControlTarget((N - i) * _initAngle / N);
//                 yield return new WaitForSeconds(0.1f);
//             }
//         }
//         if (PlutoComm.MECHANISMS[PlutoComm.mechanism] == "HOC") PlutoComm.calibrate(AppData.Instance.selectedMechanism.name);

//         PlutoComm.setControlTarget(0.0f);
//         PlutoComm.setControlType("NONE");
//         yield return new WaitForSeconds(1.5f);

//         // Set selected mechanism.
//         AppData.Instance.SetMechanism(PlutoComm.MECHANISMS[PlutoComm.mechanism]);

//         // Update flags.
//         isCalibrating = false;
//         doneCalibration = true;

//         // Go to the next scene.
//         Invoke("LoadNextScene", 0.4f);
//     }

//     void LoadNextScene()
//     {
//         // Updat game speed for the chosen mechanism.
//         AppData.Instance.selectedMechanism.UpdateSpeed();
//         Debug.Log(AppData.Instance.selectedMechanism.IsSpeedUpdated());
//         AppLogger.LogInfo($"Game speed set to {AppData.Instance.selectedMechanism.currSpeed} deg/sec.");

//         // Check make sure the current ROM is not null. If it is, then we need to 
//         // go do the assessment.
//         if (AppData.Instance.selectedMechanism.currRom == null)
//         {
//             AppLogger.LogInfo("Current ROM is null. Going to assessment scene.");
//             SceneManager.LoadScene("ASSESS");
//             return;
//         } 

//         // Load the next scene.
//         AppLogger.LogInfo($"Switching scene to '{nextScene}'.");
//         SceneManager.LoadScene(nextScene);
//     }

//     private void ApplyCounterClockwiseTorque()
//     {
//         float torqueValue = -0.07f;
//         PlutoComm.setControlType("TORQUE");
//         PlutoComm.setControlTarget(torqueValue);
//     }

//     private void ApplyClockwiseTorque()
//     {
//         float torqueValue = 0.07f;
//         PlutoComm.setControlType("TORQUE");
//         PlutoComm.setControlTarget(torqueValue);
//     }

//     private void OnPlutoButtonReleased()
//     {
//         if (!doneCalibration && !isCalibrating && !startCalibration)
//         {
//             startCalibration = true;
//         }
//     }

//     private void OnExitButtonClicked()
//     {
//         SceneManager.LoadScene(prevScene);
//     }

//     private void OnDestroy()
//     {
//         if (ConnectToRobot.isPLUTO)
//         {
//             PlutoComm.OnButtonReleased -= OnPlutoButtonReleased;
//         }
//     }
// }



using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class calibrationSceneHandler : MonoBehaviour
{
    public TextMeshProUGUI textMessage;
    public TextMeshProUGUI mechText;
    public TextMeshProUGUI angText;
    public Button exit;

    private bool startCalibration = false;
    private bool isCalibrating = false;
    private bool doneCalibration = false;
    private string prevScene = "CHMECH";
    private string nextScene = "CHGAME";

    private int calibrationStep = 0;
    private float stepStartTime = 0f;
    private float initAngle = 0f;
    private int N = 20;
    private int stepIndex = 0;
    private float[] targetAngles;

    void Start()
    {
        PlutoComm.sendHeartbeat();
        PlutoComm.calibrate(AppData.Instance.selectedMechanism.name);

        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        Debug.Log("Mechanism: " + AppData.Instance.selectedMechanism.name);
        mechText.text = PlutoComm.MECHANISMSTEXT[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name)];

        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
        }

        exit.onClick.AddListener(OnExitButtonClicked);
    }

    void Update()
    {
        PlutoComm.sendHeartbeat();
        angText.text = $" {PlutoComm.angle.ToString("F3")}";

        if (!isCalibrating && startCalibration)
        {
            StartCalibration();
        }

        if (isCalibrating)
        {
            RunCalibrationSteps();
        }
    }

    private void StartCalibration()
    {
        if (string.IsNullOrEmpty(AppData.Instance.selectedMechanism.name))
        {
            Debug.LogError("No mechanism selected for calibration!");
            return;
        }

        textMessage.color = Color.black;
        textMessage.text = "Calibrating...";
        isCalibrating = true;
        startCalibration = false;
        calibrationStep = 0;
        stepStartTime = Time.time;
    }

    private void RunCalibrationSteps()
    {
        int mechInx = Array.IndexOf(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name);
        float offset = PlutoComm.MECHOFFSETVALUE[mechInx];
        float expected = PlutoComm.CALIBANGLE[mechInx];

        switch (calibrationStep)
        {
            case 0: // Apply counter-clockwise torque
                PlutoComm.setControlType("TORQUE");
                ApplyCounterClockwiseTorque();
                stepStartTime = Time.time;
                calibrationStep = 1;
                break;

            case 1:
                if (Time.time - stepStartTime >= 3f)
                {
                    PlutoComm.calibrate(AppData.Instance.selectedMechanism.name);
                    Debug.Log(AppData.Instance.selectedMechanism.name);
                    stepStartTime = Time.time;
                    calibrationStep = 2;
                }
                break;

            case 2:
                if ((Time.time - stepStartTime >= 0.5f) && PlutoComm.angle<5f)
                {
                    ApplyClockwiseTorque();
                    stepStartTime = Time.time;
                    calibrationStep = 3;
                }else Debug.Log("Ang :"+ PlutoComm.angle);
                break;

            case 3:
                if (Time.time - stepStartTime >= 1f)
                {
                    float _angval = PlutoComm.angle + offset;
                    if (Math.Abs(_angval) < 0.9f * expected || Math.Abs(_angval) > 1.1f * expected)
                    {
                        PlutoComm.setControlType("NONE");
                        PlutoComm.calibrate("NOMECH");
                        textMessage.text = $"Try Again. + {PlutoComm.CONTROLTYPETEXT[PlutoComm.controlType]}";
                        textMessage.color = Color.red;
                        AppLogger.LogError($"Calibration failed for {AppData.Instance.selectedMechanism.name}.");
                        isCalibrating = false;
                        doneCalibration = false;
                        return;
                    }

                    textMessage.text = "Calibration Done";
                    textMessage.color = new Color32(62, 214, 111, 255);
                    AppLogger.LogError($"Calibration was successful for '{AppData.Instance.selectedMechanism.name}'.");

                    if (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "HOC")
                    {
                        PlutoComm.setControlType("POSITION");
                        initAngle = PlutoComm.angle;
                        targetAngles = new float[N];
                        for (int i = 0; i < N; i++)
                        {
                            targetAngles[i] = (N - i) * initAngle / N;
                        }
                        stepIndex = 0;
                        calibrationStep = 4;
                        stepStartTime = Time.time;
                    }
                    else
                    {
                        PlutoComm.calibrate(AppData.Instance.selectedMechanism.name);
                        calibrationStep = 6;
                        stepStartTime = Time.time;
                    }
                }
                break;

            case 4: // Gradually move to neutral
                if (stepIndex < N && Time.time - stepStartTime >= 0.1f)
                {
                    PlutoComm.setControlBound(1.0f * (stepIndex + 1) / N);
                    PlutoComm.setControlTarget(targetAngles[stepIndex]);
                    stepIndex++;
                    stepStartTime = Time.time;
                }
                else if (stepIndex >= N)
                {
                    calibrationStep = 5;
                    stepStartTime = Time.time;
                }
                break;

            case 5: // Final calibration for HOC or wrap-up
                if (Time.time - stepStartTime >= 0.1f)
                {
                    if (PlutoComm.MECHANISMS[PlutoComm.mechanism] == "HOC")
                    {
                        PlutoComm.calibrate(AppData.Instance.selectedMechanism.name);
                    }
                    calibrationStep = 6;
                    stepStartTime = Time.time;
                }
                break;

            case 6:
                if (Time.time - stepStartTime >= 1f)
                {
                    PlutoComm.setControlTarget(0.0f);
                    PlutoComm.setControlType("NONE");

                    AppData.Instance.SetMechanism(PlutoComm.MECHANISMS[PlutoComm.mechanism]);
                    isCalibrating = false;
                    doneCalibration = true;
                    Invoke("LoadNextScene", 0.4f);
                }
                break;
        }
    }

    void LoadNextScene()
    {
        AppData.Instance.selectedMechanism.UpdateSpeed();
        Debug.Log(AppData.Instance.selectedMechanism.IsSpeedUpdated());
        AppLogger.LogInfo($"Game speed set to {AppData.Instance.selectedMechanism.currSpeed} deg/sec.");

        if (AppData.Instance.selectedMechanism.currRom == null)
        {
            AppLogger.LogInfo("Current ROM is null. Going to assessment scene.");
            SceneManager.LoadScene("ASSESS");
            return;
        }

        AppLogger.LogInfo($"Switching scene to '{nextScene}'.");
        SceneManager.LoadScene(nextScene);
    }

    private void ApplyCounterClockwiseTorque()
    {
        // PlutoComm.setControlType("TORQUE");
        PlutoComm.setControlTarget(-0.07f);
    }

    private void ApplyClockwiseTorque()
    {
        // PlutoComm.setControlType("TORQUE");
        PlutoComm.setControlTarget(0.07f);
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
