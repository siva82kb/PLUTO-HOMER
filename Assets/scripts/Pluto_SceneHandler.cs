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

public class Pluto_SceneHandler : MonoBehaviour
{
    public GameObject panel;
    public GameObject calibPanel;
    public GameObject testPanel;
    public GameObject wel_panel;

    public Image statusImage;
    public GameObject loading;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI statusModeText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI controlText;
    public TextMeshProUGUI torqueText;
    public TextMeshProUGUI targetleText;
    public TextMeshProUGUI errText;
    public TextMeshProUGUI errsumText;
    public TextMeshProUGUI errdiffText;
    public TextMeshProUGUI mech;
    public TextMeshProUGUI actuated;
    public TextMeshProUGUI buttonState;
    public TextMeshProUGUI Calibration;
    public TextMeshProUGUI time;
    public TextMeshProUGUI controlType;
    public TextMeshProUGUI calibStatus;
    public TextMeshProUGUI distance;
    public TextMeshProUGUI name;
    public TextMeshProUGUI buttonMessage;
    public TextMeshProUGUI lblFeedforwardTorqueValue;
    public TextMeshProUGUI lblPositionTargetValue;
    public ToggleGroup RadioOptions;  // For the 3 options
    public TextMeshProUGUI welcome_ph;

    public Slider torque;                  // For torque controlslider
    public Slider positionSlider;         //for positionSlider
    //public string[] availabe;
    public bool diagonistic = false;
    static public byte pressed { get; private set; }
    static public byte released { get; private set; }
    public static int calibState { get; private set; }
    public static bool iscalib { get; private set; }
    public String jsonUserData;


    // Start is called before the first frame update
    void Start()
    {
        // Set default values for sliders
        torque.minValue = -1.0f;  // Set minimum value for torque slider
        torque.maxValue = 1.0f;   // Set maximum value for torque slider
        torque.value = 0.0f; ;
        // Set default values and range for position slider
        positionSlider.minValue = -135.0f;  // Set minimum value for position slider
        positionSlider.maxValue = 0.0f;     // Set maximum value for position slider
        //positionSlider.value = -135.0f;     // Set initial value to -135�
        statusImage.enabled = false;
        ConnectToRobot.Connect("COM3");
        panel.SetActive(false);
        testPanel.SetActive(false);
        calibPanel.SetActive(false);
        wel_panel.SetActive(false);
        // Attach controls callback
        AttachControlCallbacks();
        // Update the UI when starting
        UpdateUI();
    }

    // Update is called once per frame
    void Update()
    {
        if (ConnectToRobot.isPLUTO)
        {
            updateAngVal();
            if (iscalib)
            {
                update_calib_ui();
            }
            if (AppData.PlutoRobotData.buttonst == 0)
            {
                if (iscalib)
                {
                    pressed = 1;
                    released = 0;
                }
            }
            else
            {
                released = 1;
            }

            _Calibration.calibrationSetState();
            statusText.text = "connected PLUTO";
            welcome_ph.text = "Welcome Mr.Pluto";
            wel_panel.SetActive(true);
            statusImage.enabled = true;
            loading.SetActive(false);
            statusImage.color = new Color(0f, 1f, 0f, 0.741f);
            statusText.color = Color.green;
        }
        //Debug.Log(calibState + "calibState");
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D))
        {
            panel.SetActive(!panel.activeSelf);
            // Toggle showing the angle
            if (panel.activeSelf)
            {
                updateAngVal();
            }
        }
        //if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
        //{
        //    calibPanel.SetActive(!calibPanel.activeSelf);
        //    // Toggle showing the angle
        //    iscalib = false;
        //    calibState = -1;
        //    if (testPanel.activeSelf)
        //    {
        //        testPanel.SetActive(false);
        //    }

        //    if (calibPanel.activeSelf)
        //    {
        //        //updateAngVal();

        //        if (calibState == -1)
        //        {
        //            if (AppData.PlutoRobotData.Statusmode.Equals(AppData.PlutoRobotData.outDataType[2]))
        //            {
        //                JediComm.SendMessage(new byte[] { (byte)AppData.PlutoRobotData.inDataType[2] });

        //            }

        //            //connection._Calibration.update_calib_ui();
        //           _Calibration.calibrate("NOMECH");
        //            calibState = 0;//to set zero
        //            iscalib = true;
        //        }

        //    }
        //}
        //if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
        //{
        //    testPanel.SetActive(!testPanel.activeSelf);
        //    if (calibPanel.activeSelf)
        //    {
        //       calibPanel.SetActive(false);
        //    }
        //        // Toggle showing the angle
        //        if (testPanel.activeSelf)
        //    {
        //        JediComm.SendMessage(new byte[] { (byte)AppData.PlutoRobotData.inDataType[6] });
        //    }
        //}
        //UpdateTorquePositionalControl();
    }

    private void UpdateTorquePositionalControl()
    {
        // Handle torque and positional control slider updates
        float torqueValue = GetTorqueSliderValue();
        float positionalValue = GetPositionSliderValue();

        Debug.Log("Torque Value: " + torqueValue);
        Debug.Log("Positional Control Value: " + positionalValue);
    }

    public static class _Calibration
    {

        public static void calibrate(string mec)
        {
            int in_dtype = AppData.PlutoRobotData.inDataType[1];

            int index = Array.IndexOf(AppData.PlutoRobotData.mechanisum, mec);
            byte[] data = new byte[] { ((byte)in_dtype), ((byte)index) };
            JediComm.SendMessage(data);


        }
        public static void calibrationSetState()
        {
            Debug.Log("Pressed:" + pressed);
            Debug.Log("rld:" + released);

            //Zeroset
            if (pressed == released && calibState == 0)
            {
                calibrate("HOC");
                Debug.Log("pressed");

                if (AppData.PlutoRobotData.calibStatus.Equals(AppData.PlutoRobotData.calibration[1]))
                {

                    calibState = 2;
                    pressed = 0;
                   
                }
                else
                {
                    calibState = 0;

                }

            }
            //romset
            if (pressed == released && calibState == 2)
            {
                //romcheck
                if (-AppData.PlutoRobotData.AngVal >= 0.9 * (Double)AppData.PlutoRobotData.calibAngle[3] && -AppData.PlutoRobotData.AngVal <= 1.1 * (Double)AppData.PlutoRobotData.calibAngle[3])
                {
                    calibState = 3;
                    pressed=0;

                }
                else
                {
                    calibState = 4;
                    pressed=0;

                }
                //Debug.Log("pressed");

            }
            if ((pressed == released && calibState == 3) || (pressed == released && calibState == 4))
            {
                pressed = 0;
            }
            if (pressed == released && calibState == -1)
            {
                pressed = 0;
            }

        }

    }
    public void update_calib_ui()
    {
        switch (calibState)
        {
            case 0:
                calibStatus.text = "NoData";
                distance.text = "_NA_";
                buttonMessage.text = "Press the PLUTO button zero set";
                break;


            case 2:
                calibStatus.text = "Zero Set";
                distance.text =AppData.PlutoRobotData.hocdis.ToString("F2") + "cm";
                buttonMessage.text = "Press the PLUTO button rom set";
                break;

            case 3:
                calibStatus.text = "All Done!";
                distance.text =AppData.PlutoRobotData.hocdis.ToString("F2") + "cm";
                buttonMessage.text = "Press ctrl+C to close";
                break;

            case 4:
                calibStatus.text = "Error";
                distance.text = AppData.PlutoRobotData.hocdis.ToString("F2") + "cm";
                buttonMessage.text = "Press ctrl+C to close";
                break;

            default:
                Debug.LogError("Invalid calibration state");
                break;
        }
    }
    //testcontrol type
    public void AttachControlCallbacks()
    {
        // Attach the Toggle event listeners
        RadioOptions.transform.Find("No_ctrl").GetComponent<Toggle>().onValueChanged.AddListener(delegate { OnControlTypeSelected(); });
        RadioOptions.transform.Find("Torque").GetComponent<Toggle>().onValueChanged.AddListener(delegate { OnControlTypeSelected(); });
        RadioOptions.transform.Find("posistion").GetComponent<Toggle>().onValueChanged.AddListener(delegate { OnControlTypeSelected(); });

        // Attach the Slider event listeners
        torque.onValueChanged.AddListener(delegate { OnTorqueTargetChanged(); });
        positionSlider.onValueChanged.AddListener(delegate { OnPositionTargetChanged(); });
    }

    public void OnControlTypeSelected()
    {
        // Reset sliders when a control option is selected
        torque.value = 0;
        Debug.Log("working");
        // Handle control types based on the selected toggle
        Toggle activeToggle = RadioOptions.ActiveToggles().FirstOrDefault();
        if (activeToggle != null)
        {
            string selectedOption = activeToggle.name;
            int in_dtype = AppData.PlutoRobotData.inDataType[4];
            if (selectedOption == "No_ctrl")
            {
                int ctrl_type = AppData.PlutoRobotData.controlType_[0];

                JediComm.SendMessage(new byte[] { (byte)in_dtype, (byte)ctrl_type });
               
            }
            else if (selectedOption == "Torque")
            {
                int ctrl_type = AppData.PlutoRobotData.controlType_[3];

                JediComm.SendMessage(new byte[] { (byte)in_dtype, (byte)ctrl_type });
            }
            else if (selectedOption == "posistion")
            {
                int ctrl_type = AppData.PlutoRobotData.controlType_[1];

                JediComm.SendMessage(new byte[] { (byte)in_dtype, (byte)ctrl_type });
                //DeviceControl.SetControlType("POSITION");
                positionSlider.value = GetCurrentAngle();
            }
        }

        // Update UI accordingly
        UpdateUI();
    }
    private float GetCurrentAngle()
    {
        float val = AppData.PlutoRobotData.AngVal;
      
        return val; // Replace with actual value
    }
    private void OnTorqueTargetChanged()
    {
        // Handle torque target changes and send them to the device
        float torqueValue = GetTorqueSliderValue();

        SetControlTarget(torqueValue);
        UpdateUI();
    }

    private void OnPositionTargetChanged()
    {
        // Handle position target changes and send them to the device
        float positionValue = GetPositionSliderValue();
        SetControlTarget(positionValue);
        UpdateUI();
    }

    private void UpdateUI()
    {
        bool noControl = !RadioOptions.transform.Find("Torque").GetComponent<Toggle>().isOn &&
                         !RadioOptions.transform.Find("posistion").GetComponent<Toggle>().isOn;

        // Enable/disable sliders
        torque.interactable = RadioOptions.transform.Find("Torque").GetComponent<Toggle>().isOn;
        positionSlider.interactable = RadioOptions.transform.Find("posistion").GetComponent<Toggle>().isOn;
        // Using PlutoDefs1.GetTargetRange for torque and position ranges
        double[] torqueRange = AppData.PlutoRobotData.TORQUE;
        double[] positionRange = AppData.PlutoRobotData.POSITION;

        if (noControl)
        {
            lblFeedforwardTorqueValue.text = "Feedforward Torque Value (Nm):";
            lblPositionTargetValue.text = "Target Position Value (deg):";
        }
        else
        {
            lblFeedforwardTorqueValue.text = $"Feedforward Torque Value (Nm) [{torqueRange[0]}, {torqueRange[1]}]: {torque.value:F1}Nm";
            lblPositionTargetValue.text = $"Target Position Value (deg) [{positionRange[0]}, {positionRange[1]}]: {positionSlider.value:F1}deg";
        }
    }

    // Get torque slider value and convert it to the appropriate target range
    private float GetTorqueSliderValue()
    {
        float sliderMin = torque.minValue;
        float sliderMax = torque.maxValue;
        double[] torqueRange = AppData.PlutoRobotData.TORQUE;
        float targetMin = (float)torqueRange[0];
        float targetMax = (float)torqueRange[1];
        return targetMin + (targetMax - targetMin) * (torque.value - sliderMin) / (sliderMax - sliderMin);
    }

    // Get position slider value and convert it to the appropriate target range
    private float GetPositionSliderValue()
    {
        float sliderMin = positionSlider.minValue;
        float sliderMax = positionSlider.maxValue;
        double[] positionRange = AppData.PlutoRobotData.POSITION;
        float targetMin = (float)positionRange[0];
        float targetMax = (float)positionRange[1];
        return targetMin + (targetMax - targetMin) * (positionSlider.value - sliderMin) / (sliderMax - sliderMin);
    }
    // Function to set the controller target position
    public static void SetControlTarget(float target)
    {
        int in_dtype = AppData.PlutoRobotData.inDataType[5];
        byte[] targetBytes = BitConverter.GetBytes(target);
        List<byte> payload = new List<byte> { (byte)in_dtype };
        payload.AddRange(targetBytes);
        if (payload.Count > 0)
        {
            UnityEngine.Debug.Log("Out data");
            foreach (var elem in payload)
            {
                UnityEngine.Debug.Log(elem + "set_control_target");
            }
        }
        JediComm.SendMessage(payload.ToArray());
    }

    public void updateAngVal()
    {
        angleText.text = AppData.PlutoRobotData.AngVal.ToString("F2") + "degree";
        controlText.text =  AppData.PlutoRobotData.controlVal.ToString("F2");
        torqueText.text =AppData.PlutoRobotData.torqueVal.ToString("F2") + "Nm";
        targetleText.text = AppData.PlutoRobotData.TargetVal.ToString("F2");
        errText.text =AppData.PlutoRobotData.errVal.ToString("F2");
        errdiffText.text = AppData.PlutoRobotData.errdiffVal.ToString("F2");
        errsumText.text = AppData.PlutoRobotData.errsumVal.ToString("F2");
        buttonState.text = AppData.PlutoRobotData.buttonst.ToString();
        mech.text = AppData.PlutoRobotData.mech;
        actuated.text =AppData.PlutoRobotData.actu.ToString();
        statusModeText.text = AppData.PlutoRobotData.Statusmode;
        Calibration.text =AppData.PlutoRobotData.calibStatus;
        time.text = AppData.PlutoRobotData.current_time;
        controlType.text =  AppData.PlutoRobotData.controlTypeData;
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