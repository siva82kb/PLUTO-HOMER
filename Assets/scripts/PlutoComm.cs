using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

public static class PlutoComm
{
    // Device Level Constants
    public static readonly string[] OUTDATATYPE = new string[] { "SENSORSTREAM", "CONTROLPARAM", "DIAGNOSTICS" };
    public static readonly string[] MECHANISMS = new string[] { "WFE", "WUD", "FPS", "HOC", "NOMECH" };
    public static readonly string[] MECHANISMSTEXT = new string[] {
        "Wrist Flex/Extension",
        "Wrist Ulnar/Radial Deviation",
        "Forearm Pron/Supination",
        "Hand Open/Closing",
        "NO Mechanism"
    };
    public static readonly string[] CALIBRATION = new string[] { "NOCALLIB", "YESCALLIB" };
    public static readonly string[] CONTROLTYPE = new string[] { "NONE", "POSITION", "RESIST", "TORQUE" };
    public static readonly string[] CONTROLTYPETEXT = new string[] {
        "None",
        "Position",
        "Resist",
        "Torque",
    };
    public static readonly int[] SENSORNUMBER = new int[] {
        4,  // SENSORSTREAM 
        0,  // CONTROLPARAM
        7   // DIAGNOSTICS
    };
    public static readonly double MAXTORQUE = 1.0; // Nm
    public static readonly int[] INDATATYPECODES = new int[] { 0, 1, 2, 3, 4, 5, 6 };
    public static readonly string[] INDATATYPE = new string[] {
        "GET_VERSION",
        "CALIBRATE",
        "START_STREAM",
        "STOP_STREAM",
        "SET_CONTROL_TYPE",
        "SET_CONTROL_TARGET",
        "SET_DIAGNOSTICS"
    };
    public static readonly int[] CALIBANGLE = new int[] { 120, 120, 120, 140 };
    public static readonly double[] TORQUE = new double[] { -MAXTORQUE, MAXTORQUE };
    public static readonly double[] POSITION = new double[] { -135, 0 };
    public static readonly double HOCScale = 3.97 * Math.PI / 180;

    // Function to get the number corresponding to a label.
    public static int GetPlutoCodeFromLabel(string[] array, string value)
    {
        return Array.IndexOf(array, value);
    }

    // Button released event.
    public delegate void PlutoButtonReleasedEvent();
    public static event PlutoButtonReleasedEvent OnButtonReleased;

    // Control change event.
    public delegate void PlutoControlModeChangeEvent();
    public static event PlutoControlModeChangeEvent OnControlModeChange;

    // Private variables
    static private byte[] rawBytes = new byte[256];
    // For the following arrays, the first element represents the number of elements in the array.
    static private int[] previousStateData = new int[32];
    static private int[] currentStateData = new int[32];
    static private float[] currentSensorData = new float[10];


    // Public variables
    static public DateTime previousTime { get; private set; }
    static public DateTime currentTime { get; private set; }
    static public double frameRate { get; private set; }
    static public int status
    {
        get
        {
            return currentStateData[1];
        }
    }
    static public int dataType
    {
        get
        {
            return (status >> 4);
        }
    }
    static public int errorStatus
    {
        get
        {
            return currentStateData[2];
        }
    }
    static public int controlType
    {
        get
        {
            return getControlType(status);
        }
    }
    static public int calibration
    {
        get
        {
            return status & 0x01;
        }
    }
    static public int mechanism
    {
        get
        {
            return currentStateData[3] >> 4;
        }
    }
    static public int actuated
    {
        get
        {
            return currentStateData[3] & 0x01;
        }
    }
    static public int button
    {
        get
        {
            return currentStateData[4];
        }
    }
    static public float angle
    {
        get
        {
            return currentSensorData[1];
        }
    }
    static public float torque
    {
        get
        {
            return currentSensorData[1];
        }
    }
    static public float control
    {
        get
        {
            return currentSensorData[3];
        }
    }
    static public float target
    {
        get
        {
            return currentSensorData[4];
        }
    }
    static public float err
    {
        get
        {
            return currentSensorData[5];
        }
    }
    static public float errDiff
    {
        get
        {
            return currentSensorData[6];
        }
    }
    static public float errSum
    {
        get
        {
            return currentSensorData[7];
        }
    }

    private static int getControlType(int statusByte)
    {
        return (statusByte & 0x0E) >> 1;
    }

    public static void parseByteArray(byte[] payloadBytes, int payloadCount, DateTime payloadTime)
    {
        if (payloadCount == 0)
        {
            return;
        }
        Array.Copy(currentStateData, previousStateData, currentStateData.Length);
        rawBytes = payloadBytes;
        previousTime = currentTime;
        currentTime = payloadTime;

        // Updat current state data
        // Status
        currentStateData[1] = rawBytes[1];
        // Error
        currentStateData[2] = 255 * rawBytes[3] + rawBytes[2];
        // Actuated - Mech
        currentStateData[3] = rawBytes[4];

        // Udpate current sensor data
        int nSensors = SENSORNUMBER[dataType];
        currentSensorData[0] = nSensors;
        for (int i = 0; i < nSensors; i++)
        {
            currentSensorData[i + 1] = BitConverter.ToSingle(
                new byte[] { rawBytes[5 + (i * 4)], rawBytes[6 + (i * 4)], rawBytes[7 + (i * 4)], rawBytes[8 + (i * 4)] },
                0
            );
        }

        // Update the button state
        currentStateData[4] = rawBytes[(nSensors + 1) * 4 + 1];
        // Number of current state data
        currentStateData[0] = 3;

        // Updat framerate
        frameRate = 1 / (currentTime - previousTime).TotalSeconds;

        // Check if the button has been released.
        if (previousStateData[4] == 0 && currentStateData[4] == 1)
        {
            OnButtonReleased?.Invoke();
        }

        // Check if the control mode has been changed.
        if (getControlType(previousStateData[1]) != getControlType(currentStateData[1]))
        {
            OnControlModeChange?.Invoke();
        }
    }

    public static float getHOCDisplay(float angle)
    {
        return (float)HOCScale * Math.Abs(angle);

    }

    public static float getHOCAngle(float disp)
    {
        return (float) (-disp / HOCScale);

    }

    public static void startSensorStream()
    {
        JediComm.SendMessage(new byte[] { (byte)GetPlutoCodeFromLabel(INDATATYPE, "START_STREAM") });
    }

    public static void stopSensorStream()
    {
        JediComm.SendMessage(new byte[] { (byte)GetPlutoCodeFromLabel(INDATATYPE, "STOP_STREAM") });
    }

    public static void setDiagnosticMode()
    {
        JediComm.SendMessage(new byte[] { (byte)GetPlutoCodeFromLabel(INDATATYPE, "SET_DIAGNOSTICS") });
    }

    public static void calibrate(string mech)
    {
        JediComm.SendMessage(
            new byte[] {
                (byte)GetPlutoCodeFromLabel(INDATATYPE, "CALIBRATE"),
                (byte)GetPlutoCodeFromLabel(MECHANISMS, mech)
            }
        );
    }

    public static void setControlType(string controlType)
    {
        JediComm.SendMessage(
            new byte[] {
                (byte)GetPlutoCodeFromLabel(INDATATYPE, "SET_CONTROL_TYPE"),
                (byte)GetPlutoCodeFromLabel(CONTROLTYPE, controlType)
            }
        );
    }

    public static void setControlTarget(float target)
    {
        byte[] targetBytes = BitConverter.GetBytes(target);
        JediComm.SendMessage(
            new byte[] {
                (byte)GetPlutoCodeFromLabel(INDATATYPE, "SET_CONTROL_TARGET"),
                targetBytes[0],
                targetBytes[1],
                targetBytes[2],
                targetBytes[3]
            }
        );
    }
}

public static class ConnectToRobot
{
    public static string _port;
    public static bool isPLUTO = false;

    public static void Connect(string port)
    {
        _port = port;
        if (_port == null)
        {
            _port = "COM3";
            JediComm.InitSerialComm(_port);
        }
        else
        {

            JediComm.InitSerialComm(_port);
        }
        if (JediComm.serPort == null)
        {
        }

        else
        {

            if (JediComm.serPort.IsOpen)
            {

                UnityEngine.Debug.Log("Already Opended");
                JediComm.Disconnect();
            }

            if (JediComm.serPort.IsOpen == false)
            {

                UnityEngine.Debug.Log(_port);
                JediComm.Connect();
            }
        }

    }
    public static void disconnect()
    {
        ConnectToRobot.isPLUTO = false;
        JediComm.Disconnect();
    }
}
