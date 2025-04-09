using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro.EditorUtilities;
using UnityEngine;

public abstract class BaseGame
{
    public string name{ protected set; get; }
    public PlutoMechanism mechanism { protected set; get; }
    public bool isLogging { protected set; get; }
    public bool targetSpwan { protected set; get; } = false;

    public int score { protected set; get; }
    public int moveNo { protected set; get; } = 0;
    public abstract string[] events{ protected set; get; }
    public int eventNumber { protected set; get; }

    public float targetPosition { protected set; get; }
    public float playerPosition { protected set; get; }

    // Game parameters
    public string gameParamsFileName { get; protected set; } = null;
    public float? gameSpeed { protected set; get; }

    public float successRate { protected set; get; }
    public float desiredSuccessRate { protected set; get; }
    // public bool setNeutral { protected set; get; } = false;
    protected string dataLogDir = null;
    protected DataLogger dataLog;

    public BaseGame(PlutoMechanism mech)
    {
        mechanism = mech;
    }

    //private static readonly string[] gameHeader = new string[] {
    //    "time","controltype","error","buttonState","angle","control",
    //    "target","playerPosY","enemyPosY","events","playerScore","enemyScore"
    //};
    //private static readonly string[] tukTukHeader = new string[] {
    //    "time","controltype","error","buttonState","angle","control",
    //    "target","playerPosx","events","playerScore"
    //};
    //public static bool isLogging { get; private set; }
    //public static bool moving = true; // used to manipulate events in HAT TRICK
    //static public void StartDataLog(string fname)
    //{
    //    if (dataLog != null)
    //    {
    //        StopLogging();
    //    }
    //    // Start new logger
    //    if (AppData.selectedGame == "pingPong")
    //    {
    //        if (fname != "")
    //        {
    //            string instructionLine = "0 - moving, 1 - wallBounce, 2 - playerHit, 3 - enemyHit, 4 - playerFail, 5 - enemyFail\n";
    //            string headerWithInstructions = instructionLine + String.Join(", ", gameHeader) + "\n";
    //            dataLog = new DataLogger(fname, headerWithInstructions);
    //            isLogging = true;
    //        }
    //        else
    //        {
    //            dataLog = null;
    //            isLogging = false;
    //        }
    //    }
    //    else if (AppData.selectedGame == "hatTrick")
    //    {
    //        if (fname != "")
    //        {
    //            string instructionLine = "0 - moving, 1 - BallCaught, 2 - BombCaught, 3 - BallMissed, 4 - BombMissed\n";
    //            string headerWithInstructions = instructionLine + String.Join(", ", tukTukHeader) + "\n";
    //            dataLog = new DataLogger(fname, headerWithInstructions);
    //            isLogging = true;
    //        }
    //        else
    //        {
    //            dataLog = null;
    //            isLogging = false;
    //        }
    //    }
    //    else if (AppData.selectedGame == "tukTuk")
    //    {
    //        if (fname != "")
    //        {
    //            string instructionLine = "0 - moving, 1 - collided, 2 - passed\n";
    //            string headerWithInstructions = instructionLine + String.Join(", ", tukTukHeader) + "\n";
    //            dataLog = new DataLogger(fname, headerWithInstructions);
    //            isLogging = true;
    //        }
    //        else
    //        {
    //            dataLog = null;
    //            isLogging = false;
    //        }
    //    }
    //}
    //static public void StopLogging()
    //{
    //    if (dataLog != null)
    //    {
    //        UnityEngine.Debug.Log("Null log not");
    //        dataLog.stopDataLog(true);
    //        dataLog = null;
    //        isLogging = false;
    //    }
    //    else
    //        UnityEngine.Debug.Log("Null log");
    //}

    //static public void LogData()
    //{
    //    // Log only if the current data is SENSORSTREAM
    //    if (PlutoComm.SENSORNUMBER[PlutoComm.dataType] == 5)
    //    {
    //        string[] _data = new string[] {
    //           PlutoComm.currentTime.ToString(),
    //           PlutoComm.CONTROLTYPE[PlutoComm.controlType],
    //           PlutoComm.errorStatus.ToString(),
    //           PlutoComm.button.ToString(),
    //           PlutoComm.angle.ToString("G17"),
    //           PlutoComm.control.ToString("G17"),
    //           PlutoComm.target.ToString("G17"),
    //           playerPos,
    //           enemyPos,
    //           gameData.events.ToString("F2"),
    //           gameData.playerScore.ToString("F2"),
    //           gameData.enemyScore.ToString("F2")
    //        };
    //        string _dstring = String.Join(", ", _data);
    //        _dstring += "\n";
    //        dataLog.logData(_dstring);
    //    }
    //}
    //static public void LogDataHT()
    //{
    //    if (PlutoComm.SENSORNUMBER[PlutoComm.dataType] == 5)
    //    {
    //        string[] _data = new string[] {
    //           PlutoComm.currentTime.ToString(),
    //           PlutoComm.CONTROLTYPE[PlutoComm.controlType],
    //           PlutoComm.errorStatus.ToString(),
    //           PlutoComm.button.ToString(),
    //           PlutoComm.angle.ToString("G17"),
    //           PlutoComm.control.ToString("G17"),
    //           PlutoComm.target.ToString("G17"),
    //           playerPos,
    //           gameData.events.ToString("F2"),
    //           gameData.gameScore.ToString("F2")
    //        };
    //        string _dstring = String.Join(", ", _data);
    //        _dstring += "\n";
    //        dataLog.logData(_dstring);
    //    }
    //    else
    //    {
    //        UnityEngine.Debug.Log("PlutoComm:" + PlutoComm.OUTDATATYPE[PlutoComm.dataType]);
    //        UnityEngine.Debug.Log("PlutoComm:" + PlutoComm.dataType);
    //    }
    //}

    public void CreateGameMechanismFile()
    {
        // Create a new file for the game mechanism
        string fileName = Path.Combine(DataManager.gamePath,$"{name}_{mechanism}.csv");
        if (!File.Exists(fileName))
        {
            using (StreamWriter sw = File.CreateText(fileName))
            {
                sw.WriteLine("Game,Mechanism,TrialNumber,Score,EventNumber,TargetPosition,PlayerPosition,GameSpeed,SuccessRate");
            }
        }
    }
}

/// <summary>
/// HatTrickGame inherits from BaseGameClass and defines its own game events.
/// </summary>
public class HatTrickGame : BaseGame
{
    // Singleton instance (thread-safe via Lazy<T>)
    private static Lazy<HatTrickGame> _lazyInstance = new Lazy<HatTrickGame>(() => new HatTrickGame(null));
    public static HatTrickGame Instance => _lazyInstance.Value;
    // public string[] HEADER { get; } = new string[] { "DateTime", "GameSpeed" };
    // Default game parameters
    public const float DEFAULTGAMESPEED = 0.5f;

    public override string[] events
    {
        protected set { }
        get
        {
            return new string[] { "MOVING", "BALLCAUGHT", "BOMBCAUGHT", "BALLMISSED", "BOMBMISSED" };
        }
    }

    private HatTrickGame(PlutoMechanism mech) : base(mech)
    {
        // Initialize the game parameters
        name = "HAT";
        mechanism = mech;
        gameParamsFileName = Path.Combine(DataManager.gamePath,$"{name}_{mechanism}.csv");
        moveNo = 0;
        dataLog = null;

        // If null initialized, then return now.
        if (mechanism == null) return;
        
        // Call the intialization function to get the game speed.
        if (mechanism.IsSpeedUpdated())
        {
            gameSpeed = ConvertToGameSpeed(mechanism.currSpeed);
        }
        else
        {
            AppLogger.LogError("Game speed is null. Setting to default.");
            throw new ArgumentNullException();
        }
        
        // Read game parameters.
        ReadLastGameParameters();
    }

    // Initialize (thread-safe)
    public void Initialize(PlutoMechanism mech)
    {
        if (mech == null)
            throw new ArgumentNullException(nameof(mech));

        // If already initialized with the same mechanism, do nothing
        if (Instance.mechanism == mech)
            return;

        // Force reinitialization (works because Lazy is thread-safe)
        _lazyInstance = new Lazy<HatTrickGame>(() => new HatTrickGame(mech));
    }

    // Read and update game parameters from the game parameter file
    public void ReadLastGameParameters()
    {
    }

    // Function to convert the game speed
    public float ConvertToGameSpeed(float mechSpeed)
    {
        // TODO
        return 0.5f * mechSpeed;
    }
}




//public class BaseGameData
//{
//    // Assessment check
//    public bool isPROMcompleted = false;
//    public bool isAROMcompleted = false;

//    // AAN controller check
//    //public static bool isBallReached = false; PP
//    public bool targetSpwan = false;
//    //public static bool isAROMEnabled = false;

//    public bool isGameLogging;
//    public string gameName;
//    public int gameScore;
//    //public static int playerScore; PP
//    //public static int enemyScore; PP
//    //public static string playerPos = "0";
//    public string playerPosition = "0";
//    //public static string enemyPosition = "0"; PP
//    //public static string playerHit = "0"; PP
//    //public static string enemyHit = "0"; PP
//    //public static string wallBounce = "0"; PP
//    //public static string enemyFail = "0"; PP
//    //public static string playerFail = "0"; PP
//    //public static int winningScore = 3; PP
//    //public static float moveTime;
//    //public static readonly string[] pongEvents = new string[] { "moving", "wallBounce", "playerHit", "enemyHit", "playerFail", "enemyFail" }; PP
//    //public static readonly string[] hatEvents = new string[] { "moving", "BallCaught", "BombCaught", "BallMissed", "BombMissed" }; HT
//    //public static readonly string[] tukEvents = new string[] { "moving", "collided", "passed" }; TT
//    public int eventNumber;
//    public string targetPosition;
//    public float successRate;
//    public float gameSpeed;
//    //public static float gameSpeedPP;
//    //public static float gameSpeedHT;
//    //public static float predictedHitY; PP
//    public bool setNeutral = false;
//    private DataLogger dataLog;
//    //private static readonly string[] gameHeader = new string[] {
//    //    "time","controltype","error","buttonState","angle","control",
//    //    "target","playerPosY","enemyPosY","events","playerScore","enemyScore"
//    //};
//    //private static readonly string[] tukTukHeader = new string[] {
//    //    "time","controltype","error","buttonState","angle","control",
//    //    "target","playerPosx","events","playerScore"
//    //};
//    //public static bool isLogging { get; private set; }
//    //public static bool moving = true; // used to manipulate events in HAT TRICK
//    //static public void StartDataLog(string fname)
//    //{
//    //    if (dataLog != null)
//    //    {
//    //        StopLogging();
//    //    }
//    //    // Start new logger
//    //    if (AppData.selectedGame == "pingPong")
//    //    {
//    //        if (fname != "")
//    //        {
//    //            string instructionLine = "0 - moving, 1 - wallBounce, 2 - playerHit, 3 - enemyHit, 4 - playerFail, 5 - enemyFail\n";
//    //            string headerWithInstructions = instructionLine + String.Join(", ", gameHeader) + "\n";
//    //            dataLog = new DataLogger(fname, headerWithInstructions);
//    //            isLogging = true;
//    //        }
//    //        else
//    //        {
//    //            dataLog = null;
//    //            isLogging = false;
//    //        }
//    //    }
//    //    else if (AppData.selectedGame == "hatTrick")
//    //    {
//    //        if (fname != "")
//    //        {
//    //            string instructionLine = "0 - moving, 1 - BallCaught, 2 - BombCaught, 3 - BallMissed, 4 - BombMissed\n";
//    //            string headerWithInstructions = instructionLine + String.Join(", ", tukTukHeader) + "\n";
//    //            dataLog = new DataLogger(fname, headerWithInstructions);
//    //            isLogging = true;
//    //        }
//    //        else
//    //        {
//    //            dataLog = null;
//    //            isLogging = false;
//    //        }
//    //    }
//    //    else if (AppData.selectedGame == "tukTuk")
//    //    {
//    //        if (fname != "")
//    //        {
//    //            string instructionLine = "0 - moving, 1 - collided, 2 - passed\n";
//    //            string headerWithInstructions = instructionLine + String.Join(", ", tukTukHeader) + "\n";
//    //            dataLog = new DataLogger(fname, headerWithInstructions);
//    //            isLogging = true;
//    //        }
//    //        else
//    //        {
//    //            dataLog = null;
//    //            isLogging = false;
//    //        }
//    //    }
//    //}
//    //static public void StopLogging()
//    //{
//    //    if (dataLog != null)
//    //    {
//    //        UnityEngine.Debug.Log("Null log not");
//    //        dataLog.stopDataLog(true);
//    //        dataLog = null;
//    //        isLogging = false;
//    //    }
//    //    else
//    //        UnityEngine.Debug.Log("Null log");
//    //}

//    //static public void LogData()
//    //{
//    //    // Log only if the current data is SENSORSTREAM
//    //    if (PlutoComm.SENSORNUMBER[PlutoComm.dataType] == 5)
//    //    {
//    //        string[] _data = new string[] {
//    //           PlutoComm.currentTime.ToString(),
//    //           PlutoComm.CONTROLTYPE[PlutoComm.controlType],
//    //           PlutoComm.errorStatus.ToString(),
//    //           PlutoComm.button.ToString(),
//    //           PlutoComm.angle.ToString("G17"),
//    //           PlutoComm.control.ToString("G17"),
//    //           PlutoComm.target.ToString("G17"),
//    //           playerPos,
//    //           enemyPos,
//    //           gameData.events.ToString("F2"),
//    //           gameData.playerScore.ToString("F2"),
//    //           gameData.enemyScore.ToString("F2")
//    //        };
//    //        string _dstring = String.Join(", ", _data);
//    //        _dstring += "\n";
//    //        dataLog.logData(_dstring);
//    //    }
//    //}
//    //static public void LogDataHT()
//    //{
//    //    if (PlutoComm.SENSORNUMBER[PlutoComm.dataType] == 5)
//    //    {
//    //        string[] _data = new string[] {
//    //           PlutoComm.currentTime.ToString(),
//    //           PlutoComm.CONTROLTYPE[PlutoComm.controlType],
//    //           PlutoComm.errorStatus.ToString(),
//    //           PlutoComm.button.ToString(),
//    //           PlutoComm.angle.ToString("G17"),
//    //           PlutoComm.control.ToString("G17"),
//    //           PlutoComm.target.ToString("G17"),
//    //           playerPos,
//    //           gameData.events.ToString("F2"),
//    //           gameData.gameScore.ToString("F2")
//    //        };
//    //        string _dstring = String.Join(", ", _data);
//    //        _dstring += "\n";
//    //        dataLog.logData(_dstring);
//    //    }
//    //    else
//    //    {
//    //        UnityEngine.Debug.Log("PlutoComm:" + PlutoComm.OUTDATATYPE[PlutoComm.dataType]);
//    //        UnityEngine.Debug.Log("PlutoComm:" + PlutoComm.dataType);
//    //    }
//    //}
//}