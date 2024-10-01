using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditorInternal;

public class Pluto_SceneHandler : MonoBehaviour
{
    public TextMeshProUGUI textDataDisplay;
    // Calibration
    public UnityEngine.UI.Toggle tglCalibSelect;
    public Dropdown ddCalibMech;
    public TextMeshProUGUI textCalibMessage;
    // Control
    public UnityEngine.UI.Toggle tglControlSelect;
    public Dropdown ddControlSelect;
    public TextMeshProUGUI textTarget;
    public UnityEngine.UI.Slider sldrTarget;

    // Calibration variables
    private bool isCalibrating = false;
    private enum CalibrationState { WAIT_FOR_ZERO_SET, ZERO_SET, ROM_SET, ERROR, ALL_DONE };
    private CalibrationState calibState = CalibrationState.WAIT_FOR_ZERO_SET;

    // Control variables
    private bool isControl = false;
    private bool _changeSliderLimits = false;
    private float controlTarget = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize UI
        InitializeUI();
        // Attach callbacks
        AttachControlCallbacks();
        // Connect to the robot.
        ConnectToRobot.Connect("COM3");
        // Set to diagnostics mode.
        PlutoComm.setDiagnosticMode();
        // Update the UI when starting
        UpdateUI();
    }

    // Update is called once per frame
    void Update()
    {
        // Udpate UI
        UpdateUI();
    }

    //testcontrol type
    public void AttachControlCallbacks()
    {
        // Toggle button
        tglCalibSelect.onValueChanged.AddListener(delegate { OnCalibrationChange(); });
        tglControlSelect.onValueChanged.AddListener (delegate { OnControlChange(); });

        // Dropdown value change.
        ddControlSelect.onValueChanged.AddListener(delegate { OnControlModeChange(); });

        // Slider value change.
        sldrTarget.onValueChanged.AddListener(delegate { OnControlTargetChange(); });

        // Listen to PLUTO's event
        PlutoComm.OnButtonReleased += onPlutoButtonReleased;
        PlutoComm.OnControlModeChange += onPlutoControlModeChange;

    }

    private void onPlutoControlModeChange()
    {
        _changeSliderLimits = true;
    }

    private void OnControlTargetChange()
    {
        string _mech = PlutoComm.MECHANISMS[PlutoComm.mechanism];
        string _ctrlType = PlutoComm.CONTROLTYPE[PlutoComm.controlType];
        if (_ctrlType == "TORQUE")
        {
            controlTarget = sldrTarget.value;
        }
        else if (_ctrlType == "POSITION")
        {
            if (_mech == "HOC")
            {
                controlTarget = PlutoComm.getHOCAngle(sldrTarget.value);
            }
            else
            {
                controlTarget = sldrTarget.value;
            }
        }
        PlutoComm.setControlTarget(controlTarget);
    }

    private void OnControlModeChange()
    {
        string _mech = PlutoComm.MECHANISMS[PlutoComm.mechanism];
        // Send control mode to PLUTO
        PlutoComm.setControlType(PlutoComm.CONTROLTYPE[ddControlSelect.value]);
    }

    private void OnControlChange()
    {
        isControl = tglControlSelect.isOn;
        PlutoComm.setControlType("NONE");
    }

    private void OnCalibrationChange()
    {
        isCalibrating = tglCalibSelect.isOn;
        if (isCalibrating)
        {
            PlutoComm.calibrate("NOMECH");
            calibState = CalibrationState.WAIT_FOR_ZERO_SET;
        }
    }

    private void onPlutoButtonReleased()
    {
        // Check if we are in Calibration Mode.
        if (isCalibrating == false)
        {
            return;
        }
        // Run the calibration state machine
        calibStateMachineOnButtonRelease();

    }

    private void OnCalibrate()
    {
        // Set calibration state.
        isCalibrating = true;
        // Set mechanism and start calinration.
        PlutoComm.calibrate(PlutoComm.MECHANISMS[ddCalibMech.value]);
    }

    private void InitializeUI()
    {
        // Fill dropdown list
        ddCalibMech.ClearOptions();
        ddCalibMech.AddOptions(PlutoComm.MECHANISMSTEXT.ToList());
        ddControlSelect.AddOptions(PlutoComm.CONTROLTYPETEXT.ToList());
        // Clear panel selections.
        tglCalibSelect.enabled = true;
        tglCalibSelect.isOn = false;
        tglControlSelect.enabled = true;
        tglControlSelect.isOn = false;
    }

    private void UpdateUI()
    {
        // Update UI Controls.
        ddCalibMech.enabled = tglCalibSelect.enabled && tglCalibSelect.isOn;
        ddControlSelect.enabled = tglControlSelect.enabled && tglControlSelect.isOn;

        // Update data dispaly
        UpdateDataDispay();

        // Check if calibration is in progress, and update UI accordingly.
        tglCalibSelect.enabled = !isControl;
        if (isCalibrating)
        {
            calibStateMachineOnUpdate();
        }
        else
        {
            textCalibMessage.SetText("");
        }

        // Check if control is in progress, and update UI accordingly.
        tglControlSelect.enabled = PlutoComm.MECHANISMS[PlutoComm.mechanism] != "NOMECH" && !isCalibrating;
        textTarget.SetText("Target: ");
        if (isControl)
        {
            // Update controller sliders
            string _mech = PlutoComm.MECHANISMS[PlutoComm.mechanism];
            string _ctrlType = PlutoComm.CONTROLTYPE[PlutoComm.controlType];
            sldrTarget.enabled = (_ctrlType == "TORQUE") || (_ctrlType == "POSITION");
            // Change slider limits if needed.
            if (_changeSliderLimits)
            {
                // Torque controller
                if (_ctrlType == "TORQUE")
                {
                    sldrTarget.enabled = true;
                    sldrTarget.minValue = (float)-PlutoComm.MAXTORQUE;
                    sldrTarget.maxValue = (float)PlutoComm.MAXTORQUE;
                    sldrTarget.value = 0f;
                }
                else if (_ctrlType == "POSITION")
                {
                    sldrTarget.enabled = true;
                    // Set the appropriate range for the slider.
                    if (_mech == "WFE" || _mech == "WURD" || _mech == "FPS")
                    {
                        sldrTarget.minValue = -PlutoComm.CALIBANGLE[PlutoComm.mechanism];
                        sldrTarget.maxValue = PlutoComm.CALIBANGLE[PlutoComm.mechanism];
                        sldrTarget.value = PlutoComm.angle;
                    }
                    else
                    {
                        sldrTarget.minValue = PlutoComm.getHOCDisplay(0);
                        sldrTarget.maxValue = PlutoComm.getHOCDisplay(PlutoComm.CALIBANGLE[PlutoComm.mechanism]);
                        sldrTarget.value = PlutoComm.getHOCDisplay(PlutoComm.angle);
                    }
                }
                _changeSliderLimits = false;
            }

            // Udpate target value.
            string _unit = (_ctrlType == "TORQUE") ? "Nm" : "deg";
            textTarget.SetText($"Target: {controlTarget,7:F2} {_unit}");
        }
        else
        {
            sldrTarget.enabled = false;
        }
    }

    private void UpdateDataDispay()
    {
        // Update the data display.
        string _dispstr = "";
        _dispstr += $"\nTime         : {PlutoComm.currentTime.ToString()}";
        _dispstr += $"\nStatus       : {PlutoComm.OUTDATATYPE[PlutoComm.dataType]}";
        _dispstr += $"\nControl Type : {PlutoComm.CONTROLTYPE[PlutoComm.controlType]}";
        _dispstr += $"\nCalibration  : {PlutoComm.CALIBRATION[PlutoComm.calibration]}";
        _dispstr += $"\nError        : {PlutoComm.errorStatus}";
        _dispstr += $"\nMechanism    : {PlutoComm.MECHANISMS[PlutoComm.mechanism]}";
        _dispstr += $"\nActuated     : {PlutoComm.actuated}";
        _dispstr += $"\nButton State : {PlutoComm.button}";
        _dispstr += "\n";
        _dispstr += $"\nAngle        : {PlutoComm.angle,6:F2} deg";
        if (PlutoComm.MECHANISMS[PlutoComm.mechanism] == "HOC")
        {
            _dispstr += $" [{PlutoComm.getHOCDisplay(PlutoComm.angle),6:F2} cm]";
        }
        _dispstr += $"\nTorque       : {0f,6:F2} Nm";
        _dispstr += $"\nControl      : {PlutoComm.control,6:F2}";
        _dispstr += $"\nTarget       : {PlutoComm.target,6:F2}";
        if (PlutoComm.OUTDATATYPE[PlutoComm.dataType] == "DIAGNOSTICS")
        {
            _dispstr += $"\nError        : {PlutoComm.err,6:F2}";
            _dispstr += $"\nError Diff   : {PlutoComm.errDiff,6:F2}";
            _dispstr += $"\nError Sum    : {PlutoComm.errSum,6:F2}";
        }
        textDataDisplay.SetText(_dispstr);
    }


    /*
     * Calibration State Machine Functions
     */
    private void calibStateMachineOnButtonRelease()
    {
        int _mechInx = ddCalibMech.value;
        // Run the calibration state machine.
        switch (calibState)
        {
            case CalibrationState.WAIT_FOR_ZERO_SET:
                calibState = CalibrationState.ZERO_SET;
                // Get the current mechanism for calibration.
                PlutoComm.calibrate(PlutoComm.MECHANISMS[_mechInx]);
                break;
            case CalibrationState.ZERO_SET:
                if (Math.Abs(PlutoComm.angle) >= 0.9 * PlutoComm.CALIBANGLE[_mechInx] 
                    && Math.Abs(PlutoComm.angle) <= 1.1 * PlutoComm.CALIBANGLE[_mechInx])
                {
                    calibState = CalibrationState.ROM_SET;
                }
                else
                {
                    calibState = CalibrationState.ERROR;
                    PlutoComm.calibrate("NOMECH");
                }
                break;
            case CalibrationState.ROM_SET:
            case CalibrationState.ERROR:
                calibState = CalibrationState.ALL_DONE;
                break;
        }
    }

    private void calibStateMachineOnUpdate()
    {
        string _mech = PlutoComm.MECHANISMSTEXT[ddCalibMech.value];
        // Run the calibration state machine.
        switch (calibState)
        {
            case CalibrationState.WAIT_FOR_ZERO_SET:
                textCalibMessage.SetText($"Bring '{_mech}' to zero position, and press PLUTO button to set zero.");
                break;
            case CalibrationState.ZERO_SET:
                textCalibMessage.SetText($"[{PlutoComm.angle,7:F2}] Zero set. Move to the other extreme position and press PLUTO button to set zero.");
                break;
            case CalibrationState.ROM_SET:
                textCalibMessage.SetText($"'{_mech}' calibrated. Press PLUTO button to exit calibration mode.");
                break;
            case CalibrationState.ERROR:
                textCalibMessage.SetText($"Error in calibration '{_mech}'. Press PLUTO button to exit calibration mode, and try again.");
                break;
            case CalibrationState.ALL_DONE:
                tglCalibSelect.isOn = false;
                break;
        }
    }

    private void OnApplicationQuit()
    {
        ConnectToRobot.disconnect();
    }

    public void quitApplication()
    {
        Application.Quit();
    }
}