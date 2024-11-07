using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Diagnostics;
using Unity.VisualScripting;
using System.Drawing;


public class GameLog : MonoBehaviour
{
    public static GameLog instance;
    GameObject Player, Target, Enemy;
    public static string dateTime;
    public static string date;
    public static string sessionNum;

    string _fname;
    float time;
    bool logged = false;

    private static string _Pfname;
    // Game data logging Items
    public static string playerPos;
    public static string enemyPos;
    public static string TargetPos;
    private static string[] _dheader = new string[] {
        "time","controltype","error","buttonState","angle","control",
        "target","playerPosY","enemyPosY","events","playerScore","enemyScore"
    };
    private static DataLogger _dlog;
    public static bool isLogging { get; private set;}
    void Start()
    {
        dateTime = DateTime.Now.ToString("Dyyyy-MM-ddTHH-mm-ss");
        date = DateTime.Now.ToString("yyyy-MM-dd");
        sessionNum = "Session"+AppData.currentSessionNumber.ToString();
        string dir = Path.Combine(DataManager.directoryPathSession, date, sessionNum);
        Directory.CreateDirectory(dir);
        _fname = Path.Combine(dir,AppData.selectMechanism+"_"+ AppData.selectedGame+"_"+dateTime+".csv");
        AppData.trialDataFileLocation = _fname;
        File.Create(_fname).Dispose();
        UnityEngine.Debug.Log(_fname+ "Created successfully");
        StartDataLog(_fname);

    }
    static public void StartDataLog(string fname)
    {
        if (_dlog != null)
        {
            StopLogging();
        }
        // Start new logger
        if (fname != "")
        {
            string instructionLine = "0 - moving, 1 - wallBounce, 2 - playerHit, 3 - enemyHit, 4 - playerFail, 5 - enemyFail\n";
            string headerWithInstructions = instructionLine + String.Join(", ", _dheader) + "\n";
            _dlog = new DataLogger(fname, headerWithInstructions);
            isLogging = true;
        }
        else
        {
            _dlog = null;
            isLogging = false;
        }
    }
    static public void StopLogging()
    {
        if (_dlog != null)
        {
            UnityEngine.Debug.Log("Null log not");
            _dlog.stopDataLog(true);
            _dlog = null;
            isLogging = false;
        }
        else
            UnityEngine.Debug.Log("Null log");
    }

  
    static private double nanosecPerTick = 1.0 / Stopwatch.Frequency;
    static private Stopwatch stp_watch = new Stopwatch();
    static public double CurrentTime
    {
        get { return stp_watch.ElapsedTicks * nanosecPerTick; }
    }
    static public bool isTimerRunning
    {
        get { return stp_watch.IsRunning; }
    }
 
    void Update()
    {
        if (gameData.isGameLogging)
        {
            UnityEngine.Debug.Log(gameData.isGameLogging + "logging");
            Player = GameObject.FindGameObjectWithTag("Player");
            Target = GameObject.FindGameObjectWithTag("Target");
            Enemy = GameObject.FindGameObjectWithTag("Enemy");
            if (gameData.game == "COMPENSATION")
            {

                playerPos = Player.transform.eulerAngles.z.ToString();
                TargetPos = Target.transform.eulerAngles.z.ToString();
                enemyPos = Enemy.transform.eulerAngles.z.ToString();
            }
            else
            {
                if (Player != null)
                {
                    playerPos = Player.transform.position.y.ToString();
                }
                else
                {
                    playerPos = "\"" + "XXX" + "," + "XXX" + "\"";
                }

                if (Target != null)
                    TargetPos = "\"" + Target.transform.position.x.ToString() + "," + Target.transform.position.y.ToString() + "\"";
                else
                    TargetPos = "\"" + "XXX" + "," + "XXX" + "\"";
                if (Enemy != null) {
                    enemyPos =  Enemy.transform.position.y.ToString();
                }
                else
                    enemyPos = "\"" + "XXX" + "," + "XXX" + "\"";
            }
        
            LogData();
        }
        time += Time.deltaTime;
    }
    static public void LogData()
    {
        // Log onle if the current data is SENSORSTREAM
        if (PlutoComm.SENSORNUMBER[PlutoComm.dataType] == 4)
        {
            string[] _data = new string[] {
               PlutoComm.currentTime.ToString(),
               PlutoComm.CONTROLTYPE[PlutoComm.controlType],
               PlutoComm.errorStatus.ToString(),
               PlutoComm.button.ToString(),
               PlutoComm.angle.ToString("G17"),
               PlutoComm.control.ToString("G17"),
               PlutoComm.target.ToString("G17"),
               playerPos,
               enemyPos,
               gameData.events.ToString("F2"),
               gameData.playerScore.ToString("F2"),
               gameData.enemyScore.ToString("F2")
            };
            string _dstring = String.Join(", ", _data);
            UnityEngine.Debug.Log("Xmen");
            _dstring += "\n";
            _dlog.logData(_dstring);
        }
    }

    public class DataLogger
    {
        public string curr_fname { get; private set; }
        public StringBuilder _filedata;
        public bool stillLogging
        {
            get { return (_filedata != null); }
        }

        public DataLogger(string filename, string header)
        {
            curr_fname = filename;

            _filedata = new StringBuilder(header);
        }

        public void stopDataLog(bool log = true)
        {
            if (log)
            {
                File.AppendAllText(curr_fname, _filedata.ToString());
            }
            curr_fname = "";
            _filedata = null;
        }

        public void logData(string data)
        {
            if (_filedata != null)
            {
                _filedata.Append(data);
            }
        }
    }
    public void OnDestroy()
    {
        StopLogging();
    }
}
