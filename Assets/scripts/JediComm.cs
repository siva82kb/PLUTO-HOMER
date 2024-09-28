using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;

/*
 * JediCom
 * Class to handle serial communication with a device using the JEDI (Jolly sErial Data Interface)
 * format for data communication.
 */
public static class JediComm
{
    private static readonly bool OUTDEBUG = false; // Set this to true or false based on your debugging needs
    static public bool stop;
    static public bool pause;
    static public SerialPort serPort { get; private set; }
    static private Thread reader;
    static byte[] packet;
    static private int plCount = 0;
    static public double HOCScale = 3.97 * Math.PI / 180;
    static private byte[] rawBytes = new byte[256];
    static private DateTime plTime;

    // Headers for Rx and Tx.
    static public byte HeaderIn = 0xFF;
    static public byte HeaderOut = 0xAA;

    static public void InitSerialComm(string port)
    {
        serPort = new SerialPort();
        // Allow the user to set the appropriate properties.
        serPort.PortName = port;
        serPort.BaudRate = 115200;
        serPort.Parity = Parity.None;
        serPort.DataBits = 8;
        serPort.StopBits = StopBits.One;
        serPort.Handshake = Handshake.None;
        serPort.DtrEnable = true;

        // Set the read/write timeouts
        serPort.ReadTimeout = 250;
        serPort.WriteTimeout = 250;
    }

    static public void Connect()
    {
        stop = false;
        if (serPort.IsOpen == false)
        {
            try
            {
                serPort.Open();
            }
            catch (Exception ex)
            {
                Debug.Log("exception: " + ex);
            }
            // Create a new thread to read the serial port data.
            reader = new Thread(serialreaderthread);
            reader.Priority = System.Threading.ThreadPriority.AboveNormal;
            reader.Start();
        }
    }

    static public void Disconnect()
    {
        stop = true;
        if (serPort.IsOpen)
        {
            reader.Abort();
            serPort.Close();
        }
    }

    static private void serialreaderthread()
    {
        byte[] _floatbytes = new byte[4];

        // start stop watch.
        while (stop == false)
        {
            // Do nothing if paused
            if (pause)
            {
                continue;
            }
            // Check if the serial port is open.
            if (serPort.IsOpen == false)
            {
                Debug.Log("Serial port is not open.");
                continue;
            }
            try
            {
                // Read full packet.
                if (readFullSerialPacket())
                {
                    plTime = DateTime.Now;
                    ConnectToRobot.isPLUTO = true;
                    PlutoComm.parseByteArray(rawBytes, plCount, plTime);
                }
                else
                {
                    ConnectToRobot.isPLUTO = false;
                }
            }
            catch (TimeoutException)
            {
                continue;
            }

        }
        serPort.Close();
    }


    // Read a full serial packet.
    static private bool readFullSerialPacket()
    {
        plCount = 0;
        int chksum = 0;
        int _chksum;
      
        if ((serPort.ReadByte() == HeaderIn) && (serPort.ReadByte() == HeaderIn))
        {
            plCount = 0;
            chksum = HeaderIn + HeaderIn;
            // Number of bytes to read.
            rawBytes[plCount++] = (byte)serPort.ReadByte();
            chksum += rawBytes[0];
            if (rawBytes[0] != 255)
            {
                // Read all the payload bytes.
                for (int i = 0; i < rawBytes[0] - 1; i++)
                {
                    rawBytes[plCount++] = (byte)serPort.ReadByte();
                    chksum += rawBytes[plCount - 1];
                }
                _chksum = serPort.ReadByte();
                return (_chksum == (chksum & 0xFF));
            }
            else
            {
                Debug.Log("Data Error. The number of data packets cannot be 255.");
                return false;
            }
        }
        else
        {
            //Disconnect();
            return false;
        }
    }

   public static void SendMessage(byte[] outBytes)
   {
        // Prepare the payload (with the header, length, message, and checksum)
        List<byte> outPayload = new List<byte> {
            HeaderOut,                     // Header byte 1
            HeaderOut,                     // Header byte 2
            (byte)(outBytes.Length + 1)    // Length of the message (+1 for checksum)
        };

        // Add the message bytes to the payload
        outPayload.AddRange(outBytes);

        // Calculate checksum (sum of all bytes modulo 256)
        byte checksum = (byte)(outPayload.Sum(b => b) % 256);

        // Add the checksum at the end of the payload
        outPayload.Add(checksum);

        // If debugging is enabled, print the outgoing data
        if (OUTDEBUG)
        {
            Debug.Log("Out data: ");
            foreach (var elem in outPayload)
            {
                Debug.Log($"{elem} ");
            }
        }

        // Send the message to the serial port
        try
        {
            serPort.Write(outPayload.ToArray(), 0, outPayload.Count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }
}