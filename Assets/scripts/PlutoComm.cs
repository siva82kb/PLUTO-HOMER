using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

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
