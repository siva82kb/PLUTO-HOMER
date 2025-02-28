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
    public int win;
    int index = 0;
    public float reduceOppositeTimer = 0;
    public float initialTorque;
    public float trialDuration = 0f;
    public float _initialTarget = 0f;
    public float _finalTarget = 0f;
    float prevSpawnTime = 0;
    int val;
    bool setZeroTorque;

    float[] First4Targets;

    public Toggle isFlaccidToggle;

    public bool isFlaccidControlOn;

    int targetcount = 0;
    bool targetSpwan = false;
    private enum DiscreteMovementTrialState { Rest, Moving }
    private DiscreteMovementTrialState trialState = DiscreteMovementTrialState.Rest;
    private DiscreteMovementTrialState _trialState;

    private float targetPosition;
    private float playerPosition;
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
    }

    void Update()
    {

        PlutoComm.sendHeartbeat();
        prevSpawnTime += Time.deltaTime;

        stopClock -= Time.deltaTime;

        RunTrialStateMachine();

        playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position.y;

    }
    private void UpdateControlBoundSmoothly()
    {
        if (!targetSpwan) return;
        float t = trialDuration / 3.7f;
        float smoothedControlBound = Mathf.Lerp(0f, 0.5f, t);
        PlutoComm.setControlBound(smoothedControlBound);
    }
    private void UpdatePositionTargetSmoothly()
    {
        float t = trialDuration / 4.5f;
        float smoothedTargetPosition = Mathf.Lerp(_initialTarget, _finalTarget, t);
        PlutoComm.setControlTarget(smoothedTargetPosition);
    }
    private void RunTrialStateMachine()
    {
        trialDuration += Time.deltaTime;

        switch (_trialState)
        {
            case DiscreteMovementTrialState.Rest:
                if (targetSpwan && trialDuration >= 0.15f)
                {
                    SetTrialState(DiscreteMovementTrialState.Moving);
                }
                break;

            case DiscreteMovementTrialState.Moving:
                if (targetSpwan)
                {
                    UpdateControlBoundSmoothly();
                    UpdatePositionTargetSmoothly();

                    if (trialDuration >= 4.5f)
                    {
                        if (_finalTarget == _initialTarget)
                        {
                            Debug.Log("Target reached. Returning to Rest state.");
                        }
                        SetTrialState(DiscreteMovementTrialState.Rest);
                    }
                }
                else
                {
                    Debug.Log("Not executed");
                }

                break;
        }
    }
    private void SetTrialState(DiscreteMovementTrialState newState)
    {
        _trialState = newState;

        switch (newState)
        {
            case DiscreteMovementTrialState.Rest:
                trialDuration = 0f;
                targetSpwan = false;
                break;

            case DiscreteMovementTrialState.Moving:
                trialDuration = 0f;
                _initialTarget = PlutoComm.angle;
                _finalTarget = targetAngle;
                PlutoComm.setControlDir((sbyte)(targetPosition > playerPosition ? 1 : -1));

                //aanCtrler.setNewTrialDetails(_initialTarget, _finalTarget);
                break;
        }
    }
    public Vector2 TargetSpawn()
    {
        playSize = BirdControl.playSize;
        targetSpwan = true;

        targetPos = new Vector2(0, 0);
        targetAngle = RandomAngle();
      
        targetPos.y = Angle2Screen(targetAngle);
        targetPosition=ScreenPositionToAngle(targetAngle);
        initialDirection = getDirection();

        target = GameObject.FindGameObjectWithTag("Target");
        return targetPos;

    }
    private float ScreenPositionToAngle(float screenPosition)
    {
        AppData.newPROM = new ROM(AppData.selectedMechanism);


        float newPROM_tmin = AppData.newPROM.promTmin;
        float newPROM_tmax = AppData.newPROM.promTmax;
        float angle = Mathf.Lerp(
            newPROM_tmin / 2,
            newPROM_tmax/ 2,
            (screenPosition + playSize) / (2 * playSize)
        );
        return angle;
    }
    public bool isInPROM(float angle)
    {

        AppData.newPROM = new ROM(AppData.selectedMechanism);


        float newPROM_tmin = AppData.newPROM.promTmin;
        float newPROM_tmax = AppData.newPROM.promTmax;
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
        float tmin = promAng.promTmin;
        float tmax = promAng.promTmax;
        float prevtargetAngle = targetAngle;
        float tempAngle = Random.Range(tmin,tmax);
        while (Mathf.Abs(tempAngle - prevtargetAngle) < Mathf.Abs(tmax - tmin) / 2.5f)
        {
            tempAngle = Random.Range(tmin, tmax);
        }


        return tempAngle;

    }
    public float Angle2Screen(float angle)
    {
        ROM promAng = new ROM(AppData.selectedMechanism);
        float tmin = promAng.promTmin;
        float tmax = promAng.promTmax;

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




