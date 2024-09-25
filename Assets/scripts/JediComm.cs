using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using UnityEngine;
//using static connection;

public static class JediComm
{
    static public bool stop;
    static public bool pause;
    static public SerialPort serPort { get; private set; }
    static private Thread reader;
    static private uint _count;
    static byte[] packet;
    static int plcnt = 0;
    static public uint count
    {
        get { return _count; }
    }
    public static double HOCScale = 3.97 * Math.PI / 180;
    static private byte[] rawBytes = new byte[256];
    static private double t0, t1;

  
 
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

            reader = new Thread(serialreaderthread);

            reader.Priority = System.Threading.ThreadPriority.AboveNormal;
            t0 = 0.0;
            t1 = 0.0;
            _count = 0;
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

    static public void resetCount()
    {
        _count = 0;
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
            try
            {
                // Read full packet.

                if (readFullSerialPacket())
                {
                    ConnectToRobot.isPLUTO = true;
                    AppData.PlutoRobotData.parseByteArray(rawBytes, plcnt);
       

                }
                else
                {
                    ConnectToRobot.isPLUTO = false;
                }

                //  Debug.Log("connected");
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
        plcnt = 0;
        int chksum = 0;
        int _chksum;
      
        if ((serPort.ReadByte() == HeaderIn) && (serPort.ReadByte() == HeaderIn))
        {
            plcnt = 0;
            //SerialPayload.count++;
            chksum = 255 + 255;
            //// Number of bytes to read.
            rawBytes[plcnt++] = (byte)serPort.ReadByte();
            Debug.Log(rawBytes[0] + "rawbytes0no.ofbytes");
            chksum += rawBytes[0];

            DateTime now = DateTime.Now;
            byte[] dateTimeBytes = BitConverter.GetBytes(now.Ticks);
            if (rawBytes[0] != 255)
            {
                // read payload
                for (int i = 0; i < rawBytes[0] - 1; i++)
                {
                    rawBytes[plcnt++] = (byte)serPort.ReadByte();
                    chksum += rawBytes[plcnt - 1];
                }
                _chksum = serPort.ReadByte();
                // Add timestamp to rawBytes
                Array.Copy(dateTimeBytes, 0, rawBytes, plcnt, dateTimeBytes.Length);
                plcnt += dateTimeBytes.Length;
                return (_chksum == (chksum & 0xFF));
            }
            else
            {
                Debug.Log("data error");
                //Disconnect();
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
        List<byte> outPayload = new List<byte>
            {
             HeaderOut, // Header byte 1
             HeaderOut, // Header byte 2
             (byte)(outBytes.Length + 1) // Length of the message (+1 for checksum)
            };

        // Add the message bytes to the payload
        outPayload.AddRange(outBytes);

        // Calculate checksum (sum of all bytes modulo 256)
        byte checksum = (byte)(outPayload.Sum(b => b) % 256);

        // Add the checksum at the end of the payload
        outPayload.Add(checksum);

        // If debugging is enabled, print the outgoing data
        bool outDebug = true; // Set this to true or false based on your debugging needs
        if (outDebug)
        {
            Console.Write("\nOut data: ");
            foreach (var elem in outPayload)
            {
                Debug.Log($"{elem} ");
            }
            Console.WriteLine();
        }

        // Send the message to the serial port
        try
        {
            serPort.Write(outPayload.ToArray(), 0, outPayload.Count);
            Debug.Log("Message sent to device.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }


}
