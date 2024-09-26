using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

public static class PlutoComm
{
    // Device Level Constants
    public static readonly string[] OUTDATATYPE = new string[] { "SENSORSTREAM", "CONTROLPARAM", "DIAGNOSTICS" };
    public static readonly string[] MECHANISMS = new string[] { "WFE", "WUD", "WPS", "HOC", "NOMECH" };
    public static readonly string[] CALIBRATION= new string[] { "NOCALLIB", "YESCALLIB" };
    public static readonly string[] CONTROLTYPE = new string[] { "NONE", "POSITION", "RESIST", "TORQUE" };
    public static readonly int[]    SENSORNUMBER = new int[] { 
        4,  // SENSORSTREAM 
        0,  // CONTROLPARAM
        7   // DIAGNOSTICS
    }; 
    public static readonly double   MAXTORQUE = 1.0; // Nm
    public static readonly int[]    INDATATYPE = new int[] { 0, 1, 2, 3, 4, 5, 6 };
    public static readonly int[]    CALIBANGLE= new int[] { 120, 120, 120, 140 };
    public static readonly double[] TORQUE = new double[] { -MAXTORQUE, MAXTORQUE };
    public static readonly double[] POSITION = new double[] { -135, 0 };
    public static readonly double   HOCScale = 3.97 * Math.PI / 180;

    // Function to get the number corresponding to a label.
    public static int GetPlutoCodeFromLabel(string[] array, string value)
    {
        return Array.IndexOf(array, value);
    }

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
    static public int status { 
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
            return (status & 0x0E) >> 1;
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
            return currentSensorData[2];
        }
    }
    static public float target
    {
        get
        {
            return currentSensorData[3];
        }
    }
    static public float err
    {
        get
        {
            return currentSensorData[4];
        }
    }
    static public float errDiff
    {
        get
        {
            return currentSensorData[5];
        }
    }
    static public float errSum
    {
        get
        {
            return currentSensorData[6];
        }
    }

    public static void parseByteArray(byte[] payloadBytes, int payloadCount, DateTime payloadTime)
    {
        Debug.Log(payloadBytes);
        if (payloadCount == 0)
        {
            return;
        }
        previousStateData = currentStateData;
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
        currentStateData[4]= rawBytes[(nSensors + 1) * 4 + 1];
        // Number of current state data
        currentStateData[0] = 3;

        // Updat framerate
        frameRate = 1 / (currentTime - previousTime).TotalSeconds;
    }

    static float getHOCDisplay(float angle)
    {
        return (float) HOCScale * Math.Abs(angle);

    }
}


public static class ConnectToRobot
{
    public static string _port;
    public static bool isPLUTO = false;

    public static string[] availablePorts()
    {
        string[] portNames = SerialPort.GetPortNames();
        string[] comPorts = new string[portNames.Length + 1];
        comPorts[0] = "Select Port";
        Array.Copy(portNames, 0, comPorts, 1, portNames.Length); // Copy the old values
        if (comPorts.Length > 1)
        {
            Debug.Log("Available Port: " + comPorts[1]);
        }
        else
        {
            Debug.LogWarning("No available serial ports found.");
        }
        return comPorts;
    }

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
            // Setup serial communication with the robot.
        }

        else
        {

            if (JediComm.serPort.IsOpen)
            {

                UnityEngine.Debug.Log("Already Opended");
                JediComm.Disconnect();
                //AppData.WriteSessionInfo("DisConnecting to robot.");
            }

            if (JediComm.serPort.IsOpen == false)
            {

                UnityEngine.Debug.Log(_port);
                JediComm.Connect();
                //AppData.WriteSessionInfo("Connecting to robot.");
            }
        }

    }
    public static void disconnect()
    {
        ConnectToRobot.isPLUTO = false;
        JediComm.Disconnect();
    }



}
