using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;


public static class PlutoComm
{
    public static double HOCScale = 3.97 * Math.PI / 180;
    static private byte[] rawBytes = new byte[256];
    static private double t0, t1;
    static public float framerate { get; private set; }
    static public float AngVal { get; private set; }
    static public float torqueVal { get; private set; }
    static public float controlVal { get; private set; }
    static public float TargetVal { get; private set; }
    static public float errVal { get; private set; }
    static public float errsumVal { get; private set; }
    static public float errdiffVal { get; private set; }
    static public String mech { get; private set; }
    static public int actu { get; private set; }
    static public byte buttonst { get; private set; }
    static public String Statusmode { get; private set; }
    static public String calibStatus { get; private set; }
    static public String current_time { get; private set; }
    static public String controlTypeData { get; private set; }

    static public double hocdis { get; private set; }

    public static String[] outDataType = new string[] { "SENSORSTREAM", "CONTROLPARAM", "DIAGNOSTICS" };
    public static String[] mechanisum = new string[] { "WFE", "WUD", "WPS", "HOC", "NOMECH" };
    public static String[] calibration = new string[] { "NOCALLIB", "YESCALLIB" };
    public static String[] controlType = new string[] { "NONE", "POSITION", "RESIST", "TORQUE" };
    public static int[] sensorDataNumber = new int[] { 4, 0, 7 };//0:sesnorStream;2:diagonistic;
    public static readonly double PLUTOMaxTorque = 1.0; // Nm
    public static int[] inDataType = new int[] { 0, 1, 2, 3, 4, 5, 6 };//"GET_VERSION":0x00,"CALIBRATE":0x01,"START_STREAM": 0x02,"STOP_STREAM": 0x03,"SET_controlType":0x04,"SET_CONTROL_TARGET": 0x05,"SET_DIAGNOSTICS":0x06,
    public static int[] calibAngle = new int[] { 120, 120, 120, 140 };
    public static int[] controlType_ = new int[] { 0, 1, 2, 3 };//none,position, resist, torque
    public static double[] TORQUE = new double[] { -PLUTOMaxTorque, PLUTOMaxTorque };
    public static double[] POSITION = new double[] { -135, 0 };
    // Hand Opening and Closing Mechanism Conversion Factor

    public static void parseByteArray(byte[] receivedbytes, int datalength)
    {
        rawBytes = receivedbytes;
        byte status = rawBytes[1];//status
        Debug.Log(status + "status");
        int error = 255 * rawBytes[3] + rawBytes[2];//error
        Debug.Log(error + "error");
        int statusmode = getstatusMode(status);//statusmode[Sensorstream,Diagonistics]
        Debug.Log(statusmode + "mode");
        int ismech = getmech(rawBytes[4]);//mechanisum
        Debug.Log(ismech + "mec");
        int isact = getisact(rawBytes[4]);//Actuated
        Debug.Log(isact + "act");
        int dtype = getdatatype(status);//sensordatatype
        Debug.Log(dtype + "datatype");
        //if sensorDataNumber is 4 - sensorstream
        if (dtype == sensorDataNumber[0] || dtype == sensorDataNumber[2])  // Check for both types
        {
            // Create arrays based on the dtype
            byte[][] floatBytesArray = new byte[dtype][];
            float[] sensordata = new float[dtype];

            // Extract floats from rawBytes
            for (int i = 0; i < dtype; i++)
            {
                floatBytesArray[i] = new byte[] { rawBytes[5 + (i * 4)], rawBytes[6 + (i * 4)], rawBytes[7 + (i * 4)], rawBytes[8 + (i * 4)] };
                float floatValue = BitConverter.ToSingle(floatBytesArray[i], 0);
                sensordata[i] = floatValue;
            }

            // Assign common sensor values
            AngVal = sensordata[0];
            torqueVal = sensordata[1];
            controlVal = sensordata[2];
            TargetVal = sensordata[3];

            if (dtype == sensorDataNumber[0])  // dtype = 4 case
            {
                buttonst = rawBytes[(dtype + 1) * 4 + 1];
                Debug.Log(rawBytes[21] + "buttonstatus");
            }
            else if (dtype == sensorDataNumber[2])  // dtype = 7 case
            {
                errVal = sensordata[4];
                errdiffVal = sensordata[5];
                errsumVal = sensordata[6];
                buttonst = rawBytes[(dtype + 1) * 4 + 1];
                Debug.Log(rawBytes[33] + "buttonstatus");
            }
        }

        int dateTimeStartIndex = datalength - 8;  // The last 8 bytes are for the DateTime
        byte[] dateTimeBytes = new byte[8];
        Array.Copy(receivedbytes, dateTimeStartIndex, dateTimeBytes, 0, 8);

        // Convert the 8-byte array back to a long (Ticks)
        long dateTimeTicks = BitConverter.ToInt64(dateTimeBytes, 0);

        // Create a DateTime from the ticks
        DateTime timestamp = new DateTime(dateTimeTicks);
        hocdis = gethocdis(AngVal);
        current_time = timestamp.ToString();
        Statusmode = outDataType[statusmode];
        mech = mechanisum[ismech];
        actu = isact;
        controlTypeData = getcontroltype(status);
        calibStatus = getcalibstatus(status);
        Debug.Log(mech + "mech");
        Debug.Log(actu + "actuat");
        Debug.Log(rawBytes[1] + "status");

    }

    static double gethocdis(float angle)
    {
        return HOCScale * Math.Abs(angle);

    }
    //extract mechanisum from actuated byte
    static int getmech(byte data)
    {
        return (data >> 4);

    }
    //extract actuated  from actuated byte
    static int getisact(byte data)
    {
        return (data & 0x01);

    }
    //extract statusmode from status byte
    static int getstatusMode(byte data)
    {
        return (data >> 4);

    }
    //extract sensorDataNumber from status byte 
    static int getdatatype(byte data)
    {
        int num = data >> 4;
        int datatype = sensorDataNumber[num];
        return datatype;
    }
    //extract calibrationstatus from status byte
    static String getcalibstatus(byte data)
    {
        int num = data & 0x01;
        return (calibration[num]);
    }
    //extract controltype from status byte
    static String getcontroltype(byte data)
    {
        int num = (data & 0x0E) >> 1;
        return (controlType[num]);
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
