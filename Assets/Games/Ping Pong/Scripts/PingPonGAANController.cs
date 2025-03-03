////using PlutoDataStructures;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.UI;
//using Random = UnityEngine.Random;

//public class PingPonGAANController : MonoBehaviour
//{

//    public static PingPonGAANController instance;


//    // static int trailNumber;
//    //public Text trailNUmber;


//    public float trailDuration = 0f;
//    public float playSize = 0;
//    public float targetAngle;
//    GameObject target;
//    GameObject player;

//    private float ballTrajetoryPrediction;

//    bool wasNonZero;
//    BaallTrajectoryPlotter btp;

//    public Toggle isFlaccidToggle;
//    public bool isFlaccidControlOn;
//    bool targetSpwan = false;
//    private enum DiscreteMovementTrialState { Rest, Moving }
//    private DiscreteMovementTrialState trialState = DiscreteMovementTrialState.Rest;
//    private DiscreteMovementTrialState _trialState;

//    private float targetPosition;
//    private float playerPosition;

//    public float trialDuration = 0f;
//    public float _initialTarget = 0f;
//    public float _finalTarget = 0f;
//    private void Awake()
//    {
//        if (instance == null)
//        {
//            instance = this;
//        }
//        else
//            Destroy(gameObject);

//        Application.targetFrameRate = 300;
//        QualitySettings.vSyncCount = 0;

//    }

//    void Start()
//    {
//        PlutoComm.setControlType("POSITIONAAN");
//        playSize = Camera.main.orthographicSize;
//        Application.targetFrameRate = 300;
//    }


//    void Update()
//    {
//        playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position.y;

//        RunTrialStateMachine();
//        if (GameObject.FindGameObjectsWithTag("Target").Length > 0)
//        {
//            btp = GameObject.FindGameObjectWithTag("Target").GetComponent<BaallTrajectoryPlotter>();
//            ballTrajetoryPrediction = btp.targetPosition;

//            if ((Mathf.Abs(btp.ballDistance) / Mathf.Abs(btp.ballVelocity.magnitude)) < 4 && btp.ballVelocity.x > 0 && (Mathf.Abs(btp.ballDistance) / Mathf.Abs(btp.ballVelocity.magnitude)) > 1)
//            {
//                if (btp.transform.position.x < 3.5)
//                {
//                    targetPosition = ScreentoAngle(ballTrajetoryPrediction);
//                    targetAngle = ScreenPositionToAngle(ballTrajetoryPrediction);
//                    targetSpwan = true;

//                }
//            }
//        }
//       }

//    private void UpdateControlBoundSmoothly()
//    {
//        if (!gameData.targetSpwan) return;
//        float t = trialDuration / 2f;
//        float smoothedControlBound = Mathf.Lerp(0f, 0.5f, t);
//        PlutoComm.setControlBound(smoothedControlBound);
//    }
//    private float ScreenPositionToAngle(float screenPosition)
//    {
//        float calibAngleRange = PlutoComm.CALIBANGLE[PlutoComm.mechanism];
//        float angle = Mathf.Lerp(
//            -calibAngleRange / 2,
//            calibAngleRange / 2,
//            (screenPosition + playSize) / (2 * playSize)
//        );
//        return angle;
//    }
//    private void UpdatePositionTargetSmoothly()
//    {
//        float t = trialDuration / 2.5f;
//        float smoothedTargetPosition = Mathf.Lerp(_initialTarget, _finalTarget, t);
//        PlutoComm.setControlTarget(smoothedTargetPosition);
//    }
//    private void RunTrialStateMachine()
//    {
//        trialDuration += Time.deltaTime;

//        switch (_trialState)
//        {
//            case DiscreteMovementTrialState.Rest:
//                if ( trialDuration >= 1f && gameData.targetSpwan==true && targetSpwan==true)
//                {
//                    SetTrialState(DiscreteMovementTrialState.Moving);

//                    Debug.Log("smoothedTarget :");
//                }
//                break;

//            case DiscreteMovementTrialState.Moving:

//                    UpdateControlBoundSmoothly();
//                    UpdatePositionTargetSmoothly();

//                    if (trialDuration >= 2.8f)
//                    {
//                        SetTrialState(DiscreteMovementTrialState.Rest);
//                    Debug.Log("I'm Off");
//                    }
//                break;
//        }

//    }
//    private void SetTrialState(DiscreteMovementTrialState newState)
//    {
//        _trialState = newState;

//        switch (newState)
//        {
//            case DiscreteMovementTrialState.Rest:
//                trialDuration = 0f;
//                gameData.targetSpwan = false;
//                targetSpwan = false;
//                _finalTarget = 0f;
//                break;

//            case DiscreteMovementTrialState.Moving:
//                trialDuration = 0f;
//                _initialTarget = PlutoComm.angle;
//                _finalTarget = targetAngle;
//                Debug.Log(" _initialTarget "+_initialTarget+"               _finalTarget :"+ _finalTarget);
//                PlutoComm.setControlDir((sbyte)(targetPosition > playerPosition ? 1 : -1));

//                break;
//        }
//    }
//    public float ScreentoAngle(float y_pos)
//    {
//        float calibAngleRange = PlutoComm.CALIBANGLE[PlutoComm.mechanism];
//        float angle = Mathf.Lerp(
//            -calibAngleRange / 2,
//            calibAngleRange / 2,
//            (y_pos + playSize) / (2 * playSize)
//        );
//        return angle;
//    }
//    float getDirection()
//    {
//        return Mathf.Sign(targetAngle - PlutoComm.angle);
//    }

//    public static float Angle2Screen(float angle)
//    {
//        float playSize = 5;
//        ROM promAng = new ROM(AppData.selectedMechanism);
//        float tmin = promAng.promTmin;
//        float tmax = promAng.promTmax;
//        return Mathf.Clamp(-playSize + (angle - tmin) * (2 * playSize) / (tmax - tmin), -100, 100);

//    }
//}


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    // This value will hold the predicted y position of the ball at the player bound.
    private float ballTrajectoryPrediction;

    // Instead of relying solely on the BaallTrajectoryPlotter,
    // we will use our simulation (TrajectoryPredictor) to get the predicted y.
    // Remove btp if you don't need it:
    // BaallTrajectoryPlotter btp;  

    public Toggle isFlaccidToggle;
    public bool isFlaccidControlOn;
    bool targetSpwan = false; // Signals when a target is available.

    // A simple state machine to control the discrete movement trial.
    private enum DiscreteMovementTrialState { Rest, Moving }
    private DiscreteMovementTrialState trialState = DiscreteMovementTrialState.Rest;
    private DiscreteMovementTrialState _trialState;

    private float targetPosition; // This is the target “position” in angle-space.
    private float playerPosition; // Player paddle’s current y position.

    public float trialDuration = 0f;
    public float _initialTarget = 0f;
    public float _finalTarget = 0f;

    // --- Predictor parameters ---
    // x coordinate of the player's bound (where the ball will hit).
    public float playerBoundX = 6.0f;
    // The top and bottom bounds (y coordinates).
    public float topBound = 5.5f;
    public float bottomBound = -5.5f;
    // Bounce multiplier applied when the ball bounces off top/bottom.
    public float bounceMultiplier = 1.41f;

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
        PlutoComm.setControlType("POSITIONAAN");
        playSize = Camera.main.orthographicSize;
        Application.targetFrameRate = 300;
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
                if (timeToArrival < 4f && timeToArrival > 1f)
                {
                    // Convert the predicted screen y (world y) to an angle for the mechanism.
                    targetPosition = ScreentoAngle(ballTrajectoryPrediction);
                    targetAngle = ScreenPositionToAngle(ballTrajectoryPrediction);
                    targetSpwan = true;
                    Debug.Log(PlutoComm.CONTROLTYPE[PlutoComm.controlType]);
                }
            }
        }

        // Run the trial state machine to update the control target.
        RunTrialStateMachine();
    }

    // Smoothly update the control bound (if needed).
    private void UpdateControlBoundSmoothly()
    {
        if (!gameData.targetSpwan)
            return;
        float t = trialDuration / 2.8f;
        float smoothedControlBound = Mathf.Lerp(0f, 0.5f, t);
        PlutoComm.setControlBound(smoothedControlBound);
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
        return angle;
    }

    // Smoothly update the mechanism's target position (angle) over time.
    private void UpdatePositionTargetSmoothly()
    {
        float t = trialDuration / 3.3f;
        float smoothedTargetPosition = Mathf.Lerp(_initialTarget, _finalTarget, t);
        PlutoComm.setControlTarget(smoothedTargetPosition);
    }

    // A simple state machine to drive the control target update.
    private void RunTrialStateMachine()
    {
        trialDuration += Time.deltaTime;

        switch (_trialState)
        {
            case DiscreteMovementTrialState.Rest:
                if (trialDuration >= 0.6f &&  targetSpwan == true)
                {
                    SetTrialState(DiscreteMovementTrialState.Moving);
                    Debug.Log("Starting movement toward predicted target");
                }
                break;

            case DiscreteMovementTrialState.Moving:
                UpdateControlBoundSmoothly();
                UpdatePositionTargetSmoothly();

                if (trialDuration >= 3.9f)
                {
                    SetTrialState(DiscreteMovementTrialState.Rest);
                    Debug.Log("Movement trial complete");
                }
                break;
        }
    }

    // Sets a new state for the trial state machine.
    private void SetTrialState(DiscreteMovementTrialState newState)
    {
        _trialState = newState;

        switch (newState)
        {
            case DiscreteMovementTrialState.Rest:
                trialDuration = 0f;
                gameData.targetSpwan = false;
                targetSpwan = false;
                _finalTarget = 0f;
                break;

            case DiscreteMovementTrialState.Moving:
                trialDuration = 0f;
                _initialTarget = PlutoComm.angle; // current mechanism angle
                _finalTarget = targetAngle;         // target angle derived from prediction
                Debug.Log("Initial Angle: " + _initialTarget + "  Final Angle: " + _finalTarget);
                // Set control direction based on whether the target is above or below the current player position.
                PlutoComm.setControlDir((sbyte)(targetPosition > playerPosition ? 1 : -1));
                break;
        }
    }

    // Converts a y position to an angle value (using calibration settings).
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


    public static float Angle2Screen(float angle)
    {
        float playSize = 5f;
        ROM promAng = new ROM(AppData.selectedMechanism);
        float tmin = promAng.promTmin;
        float tmax = promAng.promTmax;
        return Mathf.Clamp(-playSize + (angle - tmin) * (2 * playSize) / (tmax - tmin), -100, 100);
    }
}
