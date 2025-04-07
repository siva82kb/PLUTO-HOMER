using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PingPonGAANController : MonoBehaviour
{
    public static PingPonGAANController instance;

    public float playSize = 0f;
    public float targetAngle; // The target angle for the mechanism.
    // (Optional: You can remove the target GameObject reference if not needed.)
    GameObject player;
   // public GameObject aromLeft, aromRight;
    // This value will hold the predicted y position of the ball at the player bound.
    private float ballTrajectoryPrediction;



    public Toggle isFlaccidToggle;
    public bool isFlaccidControlOn;
    bool targetSpwan = false; // Signals when a target is available.


    private float targetPosition; // This is the target “position” in angle-space.
    private float playerPosition; // Player paddle’s current y position.

    // --- Predictor parameters ---
    // x coordinate of the player's bound (where the ball will hit).
    public float playerBoundX = 6.0f;
    // The top and bottom bounds (y coordinates).
    public float topBound = 5.5f;
    public float bottomBound = -5.5f;
    // Bounce multiplier applied when the ball bounces off top/bottom.
    public float bounceMultiplier = 1.41f;
    private float ps = 0f;
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
        18.00f,          // Rest duration
        0.25f,          // Target set duration
        4.00f,          // Moving duration
        0.05f,          // Successful reach
        0.05f,          // Failed reach
    });
    private const float tgtHoldDuration = 0.5f;
    private float _trialTarget = 0f;
    private float _currTgtForDisplay;
    private float trialDuration = 0f;
    private float stateStartTime = 0f;
    private float _tempIntraStateTimer = 0f;

    // Control bound adaptation variables
    private float prevControlBound = 0.16f;
    // Magical minimum value where the mechanisms mostly move without too much instability.
    private float currControlBound = 0.16f;
    private const float cbChangeDuration = 2.0f;
    private HOMERPlutoAANController aanCtrler;
    private AANDataLogger dlogger;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        Application.targetFrameRate = 300;
        QualitySettings.vSyncCount = 0;
    }

    void Start()
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd");
        string dateTime = DateTime.Now.ToString("Dyyyy-MM-ddTHH-mm-ss");
        string sessionNum = "Session" + AppData.currentSessionNumber;

        AppData._dataLogDir = Path.Combine(DataManager.sessionPath, date, sessionNum, $"{AppData.selectedMechanism}_{AppData.selectedGame}_{dateTime}");

        ps = Camera.main.orthographicSize * Camera.main.aspect;

        playSize = Camera.main.orthographicSize;

        topBound = playSize - this.transform.localScale.y / 4;
        bottomBound = -topBound;
        Application.targetFrameRate = 300;


        // Pluto AAN controller
        aanCtrler = new HOMERPlutoAANController(AppData.aRomValue, AppData.pRomValue, 0.85f);
        isRunning = true;
        dlogger = new AANDataLogger(aanCtrler);
        // Set Control mode.
        PlutoComm.setControlType("POSITIONAAN");
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
        // Get the current y position of the player object.
        playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position.y;


        // *** PREDICTION STEP ***
        // Look for the ball (tagged "Target") in the scene.
        GameObject ball = GameObject.FindGameObjectWithTag("Target");
        if (ball != null)
        {
            Rigidbody2D ballRB = ball.GetComponent<Rigidbody2D>();

            // Only predict if the ball is moving toward the player (x velocity positive).
            if (ballRB.velocity.x > 0)
            {
                // Use the predictor helper to simulate the ball's path.
                ballTrajectoryPrediction = TrajectoryPredictor.PredictHitY(
                    ball.transform.position,
                    ballRB.velocity,
                    playerBoundX,
                    topBound,
                    bottomBound,
                    bounceMultiplier
                );

                // Calculate approximate time for the ball to reach the player's bound.
                float timeToArrival = Mathf.Abs((playerBoundX - ball.transform.position.x) / ballRB.velocity.x);

                // You may choose to only update your control target if timeToArrival is within a window.
                if (timeToArrival < 5f && timeToArrival > 1f)
                {
                    // Convert the predicted screen y (world y) to an angle for the mechanism.
                    targetPosition = ScreentoAngle(ballTrajectoryPrediction);
                    targetAngle = ScreenPositionToAngle(ballTrajectoryPrediction);

                    if (timeToArrival < 4.7f)
                    {
                        Debug.Log($"target Angle :{targetAngle}");
                        targetSpwan = true;
                    }
                    //Debug.Log(PlutoComm.CONTROLTYPE[PlutoComm.controlType]);
                }
            }
        }
     //   UI();
        // Check if the demo is running.
        if (isRunning == false) return;

        // Update trial time
        trialDuration += Time.deltaTime;

        // Run trial state machine
        RunTrialStateMachine();
    }



    private void RunTrialStateMachine()
    {
        float _deltime = trialDuration - stateStartTime;
        bool _statetimeout = _deltime >= stateDurations[(int)_trialState];
        // Time when target is reached.
        bool _intgt = Math.Abs(_trialTarget - PlutoComm.angle) <= 5.0f;
        Debug.Log($"trialTarget-{_trialTarget},diff - {Math.Abs(_trialTarget - PlutoComm.angle)} ,bool - {_intgt}, angle-{PlutoComm.angle}");
        switch (_trialState)
        {
            case DiscreteMovementTrialState.Rest:

                if ((_statetimeout == false) && (gameData.events!=3)) return;
                SetTrialState(DiscreteMovementTrialState.SetTarget);
                dlogger.WriteAanStateInforRow();
                break;
            case DiscreteMovementTrialState.SetTarget:

                //  Debug.Log("In target st");
                if (_statetimeout == false) return;
                SetTrialState(DiscreteMovementTrialState.Moving);
                dlogger.WriteAanStateInforRow();
                break;
            case DiscreteMovementTrialState.Moving:
                // Check of the target has been reached.

                //Debug.Log("In moving st");
                _tempIntraStateTimer += _intgt ? Time.deltaTime : -_tempIntraStateTimer;
                Debug.Log($"temp:{_tempIntraStateTimer}");
                // Target reached successfull.
                bool _tgtreached = _tempIntraStateTimer >= tgtHoldDuration;
                Debug.Log($" Target Reached : {_tgtreached}");
                // Update AANController.
                aanCtrler.Update(PlutoComm.angle, Time.deltaTime, _statetimeout || _tgtreached);
                // Set AAN target if needed.
                if (aanCtrler.stateChange) UpdatePlutoAANTarget();
                //Debug.Log(gameData.playerHitt);
                //if (gameData.events == 2) Debug.Log("hitted");
                // Change state if needed.
                if ( (gameData.events == 2))  SetTrialState(DiscreteMovementTrialState.Success);
                if (_statetimeout || (gameData.events==3) ||(gameData.events == 4)) SetTrialState(DiscreteMovementTrialState.Failure);
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
                
                // Reset stuff.
                trialDuration = 0f;
                prevControlBound = PlutoComm.controlBound;
                currControlBound = 1.0f;
                if (gameData.targetSpwan )//&& gameData.enemyHitt)
                {
                    dlogger.UpdateLogFiles(trialNo);
                    trialNo += 1;
                    //tempSpawn = false
                    gameData.targetSpwan = false;
                    //gameData.enemyHitt = false; 

                }
                _tempIntraStateTimer = 0f;
                //gameData.targetSpwan = false;
                break;
            case DiscreteMovementTrialState.SetTarget:
                targetSpwan = false;

                _trialTarget = targetAngle;
                PlutoComm.setControlBound(1f);
                break;
            case DiscreteMovementTrialState.Moving:

                _tempIntraStateTimer = 0f;
                aanCtrler.SetNewTrialDetails(PlutoComm.angle, _trialTarget, stateDurations[(int)DiscreteMovementTrialState.Moving]);
                //aanCtrler.SetNewTrialDetails(PlutoComm.angle, _trialTarget, ballFallingTime);

                break;
            case DiscreteMovementTrialState.Success:
            case DiscreteMovementTrialState.Failure:
                // Update adaptation row.
                byte _successbyte = newState == DiscreteMovementTrialState.Success ? (byte)1 : (byte)0;
               // gameData.playerHitt = false;
                dlogger.WriteTrialRowInfo(_successbyte);
                break;
        }
        _trialState = newState;
        stateStartTime = trialDuration;
    }


    // Convert a screen y position (world y) to an angle based on calibration.
    private float ScreenPositionToAngle(float screenPosition)
    {
        float calibAngleRange = PlutoComm.CALIBANGLE[PlutoComm.mechanism];
        float angle = Mathf.Lerp(
            -calibAngleRange / 2,
            calibAngleRange / 2,
            (screenPosition + playSize) / (2 * playSize)
        );
        Debug.Log($"demo ang :{angle},{PlutoComm.CALIBANGLE[PlutoComm.mechanism]}");
        return angle;
    }


    public float ScreentoAngle(float y_pos)
    {
        float calibAngleRange = PlutoComm.CALIBANGLE[PlutoComm.mechanism];
        float angle = Mathf.Lerp(
            -calibAngleRange / 2,
            calibAngleRange / 2,
            (y_pos + playSize) / (2 * playSize)
        );
        return angle;
    }

    //private void UI()
    //{
    //    aromLeft.transform.position = new Vector2(aromRight.transform.position.x,
    //       Angle2Screen(AppData.aRomValue[0])
    //   );
    //  //  Debug.Log($"{AppData.aRomValue[0]},{AppData.aRomValue[1]}");
    //    //Debug.Log(PlutoComm.angle);
    //   // Debug.Log($"{Angle2Screen(PlutoComm.angle)}");
    //   // Debug.Log($"{Angle2Screen(AppData.aRomValue[0])},{Angle2Screen(AppData.aRomValue[1])}");
    //    aromRight.transform.position = new Vector2(aromRight.transform.position.x,
    //        Angle2Screen(AppData.aRomValue[1]));

    //    Debug.Log($"y-pos-left-{Angle2Screen(AppData.aRomValue[0])},right-{Angle2Screen(AppData.aRomValue[1])}+ currentangle-{PlutoComm.angle},current-yPos_{Angle2Screen(PlutoComm.angle)}");
      
    //}
    public float Angle2Screen(float angle)
    {
        //ROM aromAng = new ROM(AppData.selectedMechanism);
        float tmin = AppData.aRomValue[0];
        float tmax = AppData.aRomValue[1];
        return Mathf.Clamp(-playSize + (angle - tmin) * (2 * playSize) / (tmax - tmin), bottomBound, topBound);

    }

    //public float Angle2Screen2(float angle)
    //{
    //    float tmin = AppData.aRomValue[0];
    //    float tmax = AppData.aRomValue[1];
    //    // return ( -2.7f + (angle - tmin) * (ps) / (tmax - tmin));
    //    return (-2.5f + (angle - tmin) * (2 * playSize) / (tmax - tmin));

    //}

}
