using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public static class PlutoComm
{
    // Device Level Constants
    public static readonly string[] OUTDATATYPE = new string[] { "SENSORSTREAM", "CONTROLPARAM", "DIAGNOSTICS", "VERSION" };
    public static readonly string[] MECHANISMS = new string[] { "NOMECH", "WFE", "WURD", "FPS", "HOC", "FME1", "FME2" };
    public static readonly string[] MECHANISMSTEXT = new string[] {
        "Wrist Flex/Extension",
        "Wrist Ulnar/Radial Deviation",
        "Forearm Pron/Supination",
        "Hand Open/Closing",
        "FME1",
        "FME2"
    };
    public static readonly string[] CALIBRATION = new string[] { "NOCALIB", "YESCALIB" };
    public static readonly string[] CONTROLTYPE = new string[] { "NONE", "POSITION", "RESIST", "TORQUE", "POSITIONAAN" };
    public static readonly string[] CONTROLTYPETEXT = new string[] {
        "None",
        "Position",
        "Resist",
        "Torque",
        "Position-AAN"
    };
    public static readonly int[] SENSORNUMBER = new int[] {
        5,  // SENSORSTREAM 
        0,  // CONTROLPARAM
        8   // DIAGNOSTICS
    };
    public static readonly double MAXTORQUE = 1.0; // Nm
    public static readonly int[] INDATATYPECODES = new int[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x80 };
    public static readonly string[] INDATATYPE = new string[] {
        "GET_VERSION",
        "CALIBRATE",
        "START_STREAM",
        "STOP_STREAM",
        "SET_CONTROL_TYPE",
        "SET_CONTROL_TARGET",
        "SET_DIAGNOSTICS",
        "SET_CONTROL_BOUND",
        "RESET_PACKETNO",
        "SET_CONTROL_DIR",
        "SET_AAN_TARGET",
        "HEARTBEAT"
    };
    public static readonly string[] ERRORTYPES = new string[] {
        "ANGSENSERR",
        "MCURRSENSERR",
        "NOHEARTBEAT"
    };
    public static readonly int[] CALIBANGLE = new int[] { 0, 136, 136, 180, 93 }; // The first zero value is a dummy value.
    public static readonly float[] MECHOFFSETVALUE = new float[] {
        0,    // Dummy. No mechanism 
        68,   // Wrist Flexion/Extension     
        68,   // Wrist Ulnar/Radial Deviation
        90,   // Forearm Prono/Sunpination
        0,    // Hand Opening/Closing
        0,    // Functional mechanism 1
        0,    // Functional mechanism 2
    };
    public static readonly double[] TORQUE = new double[] { -MAXTORQUE, MAXTORQUE };
    public static readonly double[] POSITION = new double[] { -135, 0 };
    public static readonly double HOCScale = 0.10752; // 3.97 * Math.PI / 180;

    // Button released event.
    public delegate void PlutoButtonReleasedEvent();
    public static event PlutoButtonReleasedEvent OnButtonReleased;

    // Control change event.
    public delegate void PlutoControlModeChangeEvent();
    public static event PlutoControlModeChangeEvent OnControlModeChange;

    // Mechanism change event.
    public delegate void PlutoMechanismChangeEvent();
    public static event PlutoMechanismChangeEvent OnMechanismChange;

    // New data event.
    public delegate void PlutoNewDataEvent();
    public static event PlutoNewDataEvent OnNewPlutoData;

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
    static public String deviceId { get; private set; }
    static public String version { get; private set; }
    static public String compileDate { get; private set; }
    static public ushort packetNumber { get; private set; }
    static public float runTime { get; private set; }
    static public float prevRunTime { get; private set; }
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
    static public string errorString
    {
        get
        {
            if (currentStateData[2] == 0) return "NOERROR";
            string _str = "";
            for (int i = 0; i < 16; i++)
            {
                if ((errorStatus & (1 << i)) != 0)
                {
                    _str += (_str != "") ? " | " : "";
                    _str += ERRORTYPES[i];
                }
            }
            return _str;
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
            return currentStateData[6];
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
    static public float controlBound
    {
        get
        {
            return currentStateData[4] / 255f;
        }
    }
    static public sbyte controlDir
    {
        get
        {
            return (sbyte) currentStateData[5];
        }
    }
    static public float target
    {
        get
        {
            return currentSensorData[4];
        }
    }
    static public float desired
    {
        get
        {
            return currentSensorData[5];
        }
    }
    static public float err
    {
        get
        {
            return currentSensorData[6];
        }
    }
    static public float errDiff
    {
        get
        {
            return currentSensorData[7];
        }
    }
    static public float errSum
    {
        get
        {
            return currentSensorData[8];
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
        prevRunTime = runTime;

        // Updat current state data
        // Status
        currentStateData[1] = rawBytes[1];
        // Error
        currentStateData[2] = 255 * rawBytes[3] + rawBytes[2];
        // Actuated - Mech
        currentStateData[3] = rawBytes[4];
        
        // Handle data based on what type of data it is.
        byte _datatype = (byte)(currentStateData[1] >> 4);
        switch (OUTDATATYPE[_datatype])
        {
            case "SENSORSTREAM":
            case "DIAGNOSTICS":
                // Get the packet number.
                packetNumber = BitConverter.ToUInt16(new byte[] { rawBytes[5], rawBytes[6] });

                // Get the runtime.
                runTime = 0.001f * BitConverter.ToUInt32(new byte[] { rawBytes[7], rawBytes[8], rawBytes[9], rawBytes[10] });

                // Udpate current sensor data
                int offset = 10;
                int nSensors = SENSORNUMBER[_datatype];
                currentSensorData[0] = nSensors;
                for (int i = 0; i < nSensors; i++)
                {
                    currentSensorData[i + 1] = BitConverter.ToSingle(
                        new byte[] { 
                            rawBytes[offset + 1 + (i * 4)],
                            rawBytes[offset + 2 + (i * 4)], 
                            rawBytes[offset + 3 + (i * 4)],
                            rawBytes[offset + 4 + (i * 4)] },
                        0
                    );
                }
                // Update the control bound
                currentStateData[4] = rawBytes[(nSensors + 1) * 4 + 6 + 1];
                // Update the control direction
                currentStateData[5] = rawBytes[(nSensors + 1) * 4 + 6 + 2];
                // Update the button state
                currentStateData[6] = rawBytes[(nSensors + 1) * 4 + 6 + 3];

                // Number of current state data
                currentStateData[0] = 3;

                // Updat framerate
                frameRate = 1 / (runTime - prevRunTime);

                // Check if the button has been released.
                if (previousStateData[6] == 0 && currentStateData[6] == 1)
                {
                    OnButtonReleased?.Invoke();
                }

                // Check if the control mode has been changed.
                if (getControlType(previousStateData[1]) != getControlType(currentStateData[1]))
                {
                    OnControlModeChange?.Invoke();
                }

                // Check if the mechanism has been changed.
                if ((previousStateData[3] >> 4)  != (currentStateData[3] >> 4))
                {
                    OnMechanismChange?.Invoke();
                }

                // Invoke the new data event only for SENSORSTREAM or DIAGNOSTICS data.
                OnNewPlutoData?.Invoke();
                break;
            case "VERSION":
                // Read the bytes into a string.
                Debug.Log("Version");
                deviceId = Encoding.ASCII.GetString(rawBytes, 5, rawBytes[0] - 4 - 1).Split(",")[0];
                version = Encoding.ASCII.GetString(rawBytes, 5, rawBytes[0] - 4 - 1).Split(",")[1];
                compileDate = Encoding.ASCII.GetString(rawBytes, 5, rawBytes[0] - 4 - 1).Split(",")[2];
                break;
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
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "START_STREAM")] });
    }

    public static void stopSensorStream()
    {
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "STOP_STREAM")] });
    }

    public static void setDiagnosticMode()
    {
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_DIAGNOSTICS")] });
    }

    public static void getVersion()
    {
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "GET_VERSION")] });
    }

    public static void calibrate(string mech)
    {   
        JediComm.SendMessage(
            new byte[] {
                (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "CALIBRATE")],
                (byte)Array.IndexOf(MECHANISMS, mech)
            }
        );
        Debug.Log($"Calibrate {mech}");
    }

    public static void setControlType(string controlType)
    {
        JediComm.SendMessage(
            new byte[] {
                (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_CONTROL_TYPE")],
                (byte)Array.IndexOf(CONTROLTYPE, controlType)
            }
        );
    }

    public static void setControlTarget(float target)
    {
        byte[] targetBytes = BitConverter.GetBytes(target);
        JediComm.SendMessage(
            new byte[] {
                (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_CONTROL_TARGET")],
                targetBytes[0],
                targetBytes[1],
                targetBytes[2],
                targetBytes[3]
            }
        );
    }

    public static void setAANTarget(float tgt0, float t0, float tgt1, float dur)
    {
        Debug.Log($"tgt0: {tgt0:F2} | t0: {t0:F2} | tgt1: {tgt1:F2} | dur: {dur:F2}");
        byte[] tgt0Bytes = BitConverter.GetBytes(tgt0);
        byte[] t0Bytes = BitConverter.GetBytes(t0);
        byte[] tgt1Bytes = BitConverter.GetBytes(tgt1);
        byte[] durBytes = BitConverter.GetBytes(dur);
        JediComm.SendMessage(
            new byte[] {
                (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_AAN_TARGET")],
                tgt0Bytes[0], tgt0Bytes[1], tgt0Bytes[2], tgt0Bytes[3],
                t0Bytes[0], t0Bytes[1], t0Bytes[2], t0Bytes[3],
                tgt1Bytes[0], tgt1Bytes[1], tgt1Bytes[2], tgt1Bytes[3],
                durBytes[0], durBytes[1], durBytes[2], durBytes[3]
            }
        );
    }
    public static void setControlBound(float ctrlBound)
    {
        // Limit the value to be between 0 and 1.
        ctrlBound = Math.Max(0, Math.Min(1, ctrlBound));
        byte _ctrlboundbyte = (byte) (ctrlBound * 255);
        JediComm.SendMessage(
            new byte[] {
                (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_CONTROL_BOUND")],
                _ctrlboundbyte
            }
        );
    }


    public static void setControlDir(sbyte ctrlDir)
    {
        // Limit the value to be between 0 and 1.
        if ((ctrlDir != 1) && (ctrlDir != -1))
        {
            ctrlDir = 0;
        }
        JediComm.SendMessage(
            new byte[] {
                (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_CONTROL_DIR")],
                (byte) ctrlDir
            }
        );
    }
    //public static void setAPRom(sbyte aRomMin, sbyte aRomMax, sbyte pRomMin, sbyte pRomMax)
    //{
    //    Debug.Log($"{(sbyte)aRomMin}, {(sbyte)aRomMax}, {(sbyte)pRomMin}, {(sbyte)pRomMax}");
    //    Debug.Log($"{(byte)aRomMin}, {(byte)aRomMax}, {(byte)pRomMin}, {(byte)pRomMax}");
    //    JediComm.SendMessage(
    //        new byte[] {
    //            (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "SET_APROM")],
    //            (byte) aRomMin, (byte) aRomMax, (byte) pRomMin, (byte) pRomMax
    //        }
    //    );
    //}
    public static void resetPacketNo()
    {
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "RESET_PACKETNO")] });
    }

    public static void sendHeartbeat()
    {
        JediComm.SendMessage(new byte[] { (byte)INDATATYPECODES[Array.IndexOf(INDATATYPE, "HEARTBEAT")] });
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
        if (JediComm.serPort != null)
        {
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
