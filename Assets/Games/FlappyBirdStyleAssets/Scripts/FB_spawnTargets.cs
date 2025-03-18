using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;
using Unity.VisualScripting;

public class FB_spawnTargets : MonoBehaviour
{
    public static FB_spawnTargets instance;

    float prevAng;

    int[] successRate;
    float avgSuccessRate;
    bool dontAssistTrial;

    //runnnin game 
    public float trailDuration = 3;
    public float stopClock;
    public bool reached;
    public bool onceReached;

    public float playSize = 0;
    private string mech;
    private string hospitalnum;
    public static float[] aRom = { 0, 0 };
    public static float[] pRom = { 0, 0 };

    public static float targetAngle;
    //GameObject target;

    GameObject target;
    GameObject player;

    float gameduration = 0;
    public static bool stopAssistance = true;
    public float initialDirection = 0;
    Vector2 targetPos;
    float prevSpawnTime = 0;
    //AAN parameters
    // Control variables
    private bool isRunning = false;
    private const float tgtDuration = 3.0f;
    private float _currentTime = 0;
    private float _initialTarget = 0;
    private float _finalTarget = 0;
    //private bool _changingTarget = false; 

    // Discrete movements related variables
    private uint trialNo = 0;
    // Define variables for a discrete movement state machine
    // Enumerated variable for states
    private enum DiscreteMovementTrialState
    {
        Rest,           // Resting state
        SetTarget,      // Set the target
        Moving,         // Moving to target.
        Success,        // Successfull reach
        Failure,        // Failed reach
    }
    private DiscreteMovementTrialState _trialState;
    private static readonly IReadOnlyList<float> stateDurations = Array.AsReadOnly(new float[] {
        0.25f,          // Rest duration
        0.25f,          // Target set duration
        4.00f,          // Moving duration
        0.25f,          // Successful reach
        0.25f,          // Failed reach
    });
    private const float tgtHoldDuration = 1f;
    private float _trialTarget = 0f;
    private float _currTgtForDisplay;
    private float trialDuration = 0f;
    private float stateStartTime = 0f;
    private float _tempIntraStateTimer = 0f;

    // AAN Trajectory parameters. Set each trial.
    private float _assistPosition;
    private float _assistVelocity;
    private float _tgtInitial;
    private float _tgtFinal;
    private float _timeInitial;
    private float _timeDuration;

    // Control bound adaptation variables
    private float prevControlBound = 0.16f;
    // Magical minimum value where the mechanisms mostly move without too much instability.
    private float currControlBound = 0.16f;
    private const float cbChangeDuration = 2.0f;
    private HOMERPlutoAANController aanCtrler;
    private AANDataLogger dlogger;
    private float targetPosition;
    private float playerPosition;
    private bool targetSpawn = false;
    private float gameSpeed = 6f;
    private const float defaultScrollSpeed = -2.2f;
    private float targetReachingTime = 0f;
   // public GameObject aromLeft, aromRight;
    private void Awake()
    {
        Resources.UnloadUnusedAssets();
        if (instance == null)
        {
            instance = this;
        }
        else
            Destroy(gameObject);

        Application.targetFrameRate = 300;
        QualitySettings.vSyncCount = 0;


    }

    void Start()
    {
        PlutoComm.setControlType("POSITIONAAN");
        playSize = 2.3f + 5.5f;
        player = GameObject.FindGameObjectWithTag("Player");
        string date = DateTime.Now.ToString("yyyy-MM-dd");
        string dateTime = DateTime.Now.ToString("Dyyyy-MM-ddTHH-mm-ss");
        string sessionNum = "Session" + AppData.currentSessionNumber;

        AppData._dataLogDir = Path.Combine(DataManager.directoryPathSession, date, sessionNum, $"{AppData.selectedMechanism}_{AppData.selectedGame}_{dateTime}");




        // Pluto AAN controller
        aanCtrler = new HOMERPlutoAANController(AppData.aRomValue, AppData.pRomValue, 0.85f);
        isRunning = true;
        dlogger = new AANDataLogger(aanCtrler);
        // Set Control mode.
        //PlutoComm.setControlType("POSITIONAAN");
        PlutoComm.setControlBound(currControlBound);
        PlutoComm.setControlDir(0);
        trialNo = 0;
        //successRate = 0;
        // Start the state machine.

        SetTrialState(DiscreteMovementTrialState.Rest);
    }

    void Update()
    {

        PlutoComm.sendHeartbeat();
        prevSpawnTime += Time.deltaTime;

        if (PlutoComm.CONTROLTYPETEXT[PlutoComm.controlType] == "NONE")
        {
            PlutoComm.setControlType("POSITIONAAN");
        }
        stopClock -= Time.deltaTime;
        playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position.y;
       // Debug.Log($" scroll speed time :{FlappyGameControl.instance.scrollSpeed}");
        float diff = (FlappyGameControl.instance.scrollSpeed - defaultScrollSpeed);
        //Debug.Log($"Difference : {diff}");
        float div = (diff / defaultScrollSpeed);
        //Debug.Log($"Difference : {div}");
        targetReachingTime = gameSpeed - (gameSpeed* div);
       // Debug.Log($"target reaching Time - {targetReachingTime}");
        //UI();
        if (isRunning == false) return;

        trialDuration += Time.deltaTime;

        RunTrialStateMachine();


    }


    private void RunTrialStateMachine()
    {
        float _deltime = trialDuration - stateStartTime;
        bool _statetimeout = _deltime >= stateDurations[(int)_trialState];
        // Time when target is reached.
        bool _intgt = Math.Abs(_trialTarget - PlutoComm.angle) <= 5.0f;
        //Debug.Log(_statetimeout);
        switch (_trialState)
        {
            case DiscreteMovementTrialState.Rest:
                if (_statetimeout == false) return;
                SetTrialState(DiscreteMovementTrialState.SetTarget);
                dlogger.WriteAanStateInforRow();
                break;
            case DiscreteMovementTrialState.SetTarget:
                if (_statetimeout == false) return;
                SetTrialState(DiscreteMovementTrialState.Moving);
                dlogger.WriteAanStateInforRow();
                break;
            case DiscreteMovementTrialState.Moving:
                // Check of the target has been reached.
                _tempIntraStateTimer += _intgt ? Time.deltaTime : -_tempIntraStateTimer;
                // Target reached successfull.
                bool _tgtreached = _tempIntraStateTimer >= tgtHoldDuration;
                // Update AANController.
                aanCtrler.Update(PlutoComm.angle, Time.deltaTime, _statetimeout || _tgtreached);
                // Set AAN target if needed.
                if (aanCtrler.stateChange) UpdatePlutoAANTarget();
                // Change state if needed.

                //if (_tgtreached || gameData.birdPassed) SetTrialState(DiscreteMovementTrialState.Success);
                //if (_statetimeout || gameData.birdCollided) SetTrialState(DiscreteMovementTrialState.Failure);
                if (_tgtreached ) SetTrialState(DiscreteMovementTrialState.Success);
                if (_statetimeout) SetTrialState(DiscreteMovementTrialState.Failure);
                dlogger.WriteAanStateInforRow();
                break;
            case DiscreteMovementTrialState.Success:
            case DiscreteMovementTrialState.Failure:
                if (_statetimeout) SetTrialState(DiscreteMovementTrialState.Rest);
                break;
        }
    }

    private void UpdatePlutoAANTarget()
    {
        switch (aanCtrler.state)
        {
            case HOMERPlutoAANController.HOMERPlutoAANState.AromMoving:
                // Reset AAN Target
                PlutoComm.ResetAANTarget();
                break;
            case HOMERPlutoAANController.HOMERPlutoAANState.RelaxToArom:
            case HOMERPlutoAANController.HOMERPlutoAANState.AssistToTarget:
                // Set AAN Target to the nearest AROM edge.
                float[] _newAanTarget = aanCtrler.GetNewAanTarget();
                PlutoComm.setAANTarget(_newAanTarget[0], _newAanTarget[1], _newAanTarget[2], _newAanTarget[3]);
                break;
        }
    }

    private void SetTrialState(DiscreteMovementTrialState newState)
    {
        switch (newState)
        {
            case DiscreteMovementTrialState.Rest:
                // Reset trial in the AANController.
                aanCtrler.ResetTrial();
                dlogger.UpdateLogFiles(trialNo);
                // Reset stuff.
                trialDuration = 0f;
                prevControlBound = PlutoComm.controlBound;
                currControlBound = 1.0f;
                if (targetSpawn )//&& tempSpawn)
                {
                    trialNo += 1;
                    //tempSpawn = false;

                }
                _tempIntraStateTimer = 0f;
                targetSpawn = false;
                break;
            case DiscreteMovementTrialState.SetTarget:
                // Random select target from the appropriate range.

                _trialTarget = targetAngle;
                PlutoComm.setControlBound(1f);
                break;
            case DiscreteMovementTrialState.Moving:
                // Reset the intrastate timer.
                _tempIntraStateTimer = 0f;
                //aanCtrler.SetNewTrialDetails(PlutoComm.angle, _trialTarget, stateDurations[(int)DiscreteMovementTrialState.Moving]);
                aanCtrler.SetNewTrialDetails(PlutoComm.angle, _trialTarget, targetReachingTime);

                break;
            case DiscreteMovementTrialState.Success:
            case DiscreteMovementTrialState.Failure:
                // Update adaptation row.
                byte _successbyte = newState == DiscreteMovementTrialState.Success ? (byte)1 : (byte)0;
                dlogger.WriteTrialRowInfo(_successbyte);
                //gameData.birdCollided = false;
               // gameData.birdPassed = false;    
                break;
        }
        _trialState = newState;
        stateStartTime = trialDuration;
    }

    public Vector2 TargetSpawn()
    {
        playSize = BirdControl.playSize;
        targetSpawn = true;

        targetPos = new Vector2(0, 0);
        targetAngle = RandomAngle();

        targetPos.y = Angle2Screen(targetAngle);
        targetPosition = ScreenPositionToAngle(targetAngle);
        initialDirection = getDirection();

        target = GameObject.FindGameObjectWithTag("Target");
        return targetPos;

    }
    private float ScreenPositionToAngle(float screenPosition)
    {
        AppData.newROM = new ROM(AppData.selectedMechanism);


        float newPROM_tmin = AppData.newROM.promMin;
        float newPROM_tmax = AppData.newROM.promMax;
        float angle = Mathf.Lerp(
            newPROM_tmin / 2,
            newPROM_tmax / 2,
            (screenPosition + playSize) / (2 * playSize)
        );
        return angle;
    }
    public bool isInPROM(float angle)
    {

        AppData.newROM = new ROM(AppData.selectedMechanism);


        float newPROM_tmin = AppData.newROM.promMin;
        float newPROM_tmax = AppData.newROM.promMax;
        if (angle < newPROM_tmin || angle > newPROM_tmax)
        {
            Debug.Log("prom target");
            return true;
        }
        else
            return false;

    }
    public float RandomAngle()
    {
        ROM promAng = new ROM(AppData.selectedMechanism);
        float tmin = promAng.promMin;
        float tmax = promAng.promMax;
        float prevtargetAngle = targetAngle;
        float tempAngle = Random.Range(tmin, tmax);
        while (Mathf.Abs(tempAngle - prevtargetAngle) < Mathf.Abs(tmax - tmin) / 2.5f)
        {
            tempAngle = Random.Range(tmin, tmax);
        }


        return tempAngle;

    }
    public float Angle2Screen(float angle)
    {
        ROM promAng = new ROM(AppData.selectedMechanism);
        float tmin = promAng.promMin;
        float tmax = promAng.promMax;

        return (-2f + (angle - tmin) * (playSize) / (tmax - tmin));


    }


    private void OnApplicationQuit()
    {
    }
    float getDirection()
    {
        return Mathf.Sign(targetAngle - PlutoComm.angle);
    }

}




