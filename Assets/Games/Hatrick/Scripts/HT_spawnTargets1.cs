﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using System.IO;

public class HT_spawnTargets1 : MonoBehaviour
{
    public static HT_spawnTargets1 instance;

    // Class level constants
    public float PLAYSIZE = 0;
    //runnnin game 
    public float trailDuration = 3.5f;
    public float stopClock;
    public bool reached;
    public bool onceReached;
    public float reduceOppositeTimer = 0;
    private string mech;
    private string hospitalnum;
    public static float[] aRom = { 0, 0 };
    public static float[] pRom = { 0, 0 };
    float prevAng;
    bool angChange;
    public static float targetAngle;
    //GameObject target;
    float toqAmp;
    public int count = 0;
    GameObject target;
    GameObject player;


    public float blockduration = 10;
    public static bool stopAssistance = true;
    public float initialDirection;
    public float initialTorque;
    public float prevTorq;
    public int win;
    int index;

    // 
    int[] successRate;
    float avgSuccessRate;
    bool dontAssistTrial;

    float[] First4Targets;
    int targetcount = 0;

    public Toggle isFlaccidToggle;

    public bool isFlaccidControlOn;

    bool paramSet;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
            Destroy(gameObject);

        Application.targetFrameRate = 300;
        QualitySettings.vSyncCount = 0;

        PLAYSIZE = Camera.main.orthographicSize * Camera.main.aspect;
    }

    // Start is called before the first frame update
    void Start()
    {
        paramSet = false;
        successRate = new int[5] { 0, 0, 0, 0, 0 };
        System.Random rnd = new System.Random();
        setPrameters();
    }

    // Update is called once per frame
    void Update()
    {
        stopClock -= Time.deltaTime;

        //PlutoComm.sendHeartbeat();



        if (!HatGameController.instance.IsPlaying || Time.timeScale == 0 || Mathf.Abs(PlutoComm.angle) > 130)
        {
            stopClock = trailDuration;
            prevTorq = 0;
            initialTorque = 0;

        }



       // Debug.Log(PlutoComm.CONTROLTYPE[PlutoComm.controlType]);



    }

    public Vector2 TargetSpawn()
    {

        Debug.Log(isInPROM(targetAngle) + "," + avgSuccessRate + "," + dontAssistTrial);
      
        count++;
        player = GameObject.FindGameObjectWithTag("Player");
        target = GameObject.FindGameObjectWithTag("Target");


        UpdateSuccessRate();



        onceReached = false;


        reduceOppositeTimer = 0;

        Vector2 targetPos = new Vector2(0, 6f);
        targetcount++;

        if (targetcount > 3)
        {
            targetAngle = RandomAngle();
        }
        else
        {

            targetAngle = First4Targets[targetcount];

        }
        // Debug.Log(angle);
        //targetAngle = angle;
        dontAssistTrial = false;
        if (isInPROM(targetAngle) && avgSuccessRate >= 0.8)
        {
            dontAssistTrial = true;
        }
        targetPos.x = Angle2Screen(targetAngle, AppData.Instance.selectedMechanism.currRom.promMin, AppData.Instance.selectedMechanism.currRom.promMax);

        prevAng = PlutoComm.angle;
        initialDirection = getDirection();
        initialTorque = prevTorq;
        


        return targetPos;

    }

    public bool isInPROM(float angle)
    {
        float tmin = AppData.Instance.selectedMechanism.currRom.aromMin;
        float tmax = AppData.Instance.selectedMechanism.currRom.aromMax;
        if (angle < tmin || angle > tmax)
        {
            Debug.Log("prom target");
            return true;
        }
        else
            return false;
    }

    public void UpdateSuccessRate()
    {
        if (isInPROM(targetAngle))
        {
            int val = onceReached || reached ? 1 : 0;
            Debug.Log(val);
            for (int i = 0; i < successRate.Length; i++)
            {
                if (i <= successRate.Length - 2)
                {
                    successRate[i] = successRate[i + 1];
                }
                else
                    successRate[i] = val;
            }
        }
        avgSuccessRate = (float)successRate.Sum() / (float)successRate.Length;
    }

    float getDirection()
    {
        return Mathf.Sign(targetAngle - PlutoComm.angle);
    }

    public float RandomAngle()
    {
        float prevtargetAngle = targetAngle;
       // AppData.newPROM = new ROM(AppData.selectedMechanism);
        float newPROM_tmin = AppData.Instance.selectedMechanism.currRom.promMin;
        float newPROM_tmax = AppData.Instance.selectedMechanism.currRom.promMax;
        float tempAngle = Random.Range(newPROM_tmin, newPROM_tmax);

        while (Mathf.Abs(tempAngle - prevtargetAngle) < Mathf.Abs(newPROM_tmax - newPROM_tmin) / 2.5f)
        {
            tempAngle = Random.Range(newPROM_tmin, newPROM_tmax);
        }
        return tempAngle;
    }

    public float Angle2Screen(float angle, float promMin, float promMax)
    {
        return Mathf.Lerp(-PLAYSIZE, PLAYSIZE, (angle - promMin) / (promMax- promMin));
    }

    public void setPrameters()
    {
        isFlaccidControlOn = false;
        initialTorque = 0;
        stopClock = trailDuration;
        onceReached = false;
        paramSet = true;
    }

    private void OnApplicationQuit()
    {
        // make 


    }
   

   
}




