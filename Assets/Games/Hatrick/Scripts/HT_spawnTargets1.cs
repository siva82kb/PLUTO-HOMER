using System.Linq;
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



    //runnnin game 
    public float trailDuration = 3.5f;
    public float stopClock;
    public bool reached;
    public bool onceReached;
    public float reduceOppositeTimer = 0;
    public float playSize = 0;
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


    }

    // Start is called before the first frame update
    void Start()
    {
        paramSet = false;
        successRate = new int[5] { 0, 0, 0, 0, 0 };
        

        System.Random rnd = new System.Random();
        //First4Targets = First4Targets.OrderBy(x => rnd.Next()).ToArray();



        playSize = Camera.main.orthographicSize * Camera.main.aspect;



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
        targetPos.x = Angle2Screen(targetAngle);

        prevAng = PlutoComm.angle;
        initialDirection = getDirection();
        initialTorque = prevTorq;
        


        return targetPos;

    }

    public bool isInPROM(float angle)
    {
        //AppData.oldAROM = new ROM(AppData.selectedMechanism);

        float tmin = AppData.aRomValue[0];
        float tmax = AppData.aRomValue[1];
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
        //Debug.Log(avgSuccessRate);
    }


    float getDirection()
    {
        return Mathf.Sign(targetAngle - PlutoComm.angle);
    }

    public float RandomAngle()
    {
        float prevtargetAngle = targetAngle;
       // AppData.newPROM = new ROM(AppData.selectedMechanism);


        float newPROM_tmin = AppData.pRomValue[0];
        float newPROM_tmax = AppData.pRomValue[1];
        float tempAngle = Random.Range(newPROM_tmin, newPROM_tmax);


        while (Mathf.Abs(tempAngle - prevtargetAngle) < Mathf.Abs(newPROM_tmax - newPROM_tmin) / 2.5f)
        {
            tempAngle = Random.Range(newPROM_tmin, newPROM_tmax);
        }


        return tempAngle;

    }
    public float Angle2Screen(float angle)
    {
        //AppData.newPROM = new ROM(AppData.selectedMechanism);


        float newPROM_tmin = AppData.pRomValue[0];
        float newPROM_tmax = AppData.pRomValue[1];
        //Debug.Log(newPROM_tmin + "+" + newPROM_tmax);



        // return (-playSize + (angle - newPROM_tmin) * (2 * playSize) / (newPROM_tmax - newPROM_tmin));
        return Mathf.Lerp(-playSize, playSize, (angle - newPROM_tmin) / (newPROM_tmax - newPROM_tmin));
    }
    public void setPrameters()
    {

        isFlaccidControlOn = false;
        //checkIfFlaccid();
        initialTorque = 0;
        stopClock = trailDuration;
        onceReached = false;
        //Debug.Log(targetAngle + "," + index);
       
        paramSet = true;
    }


   

    private void OnApplicationQuit()
    {
        // make 


    }
   

   
}




