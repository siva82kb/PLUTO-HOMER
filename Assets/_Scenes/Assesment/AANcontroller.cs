//using System.Linq;
//using PlutoDataStructures;
//using System.Collections;
//using System.Collections.Generic;

//using UnityEngine;

//using System;
//using Random = UnityEngine.Random;
//using UnityEngine.SceneManagement;
//using System.IO;
//public class AANcontroller : MonoBehaviour
//{
//    public static AANcontroller instance;
//    float prevAng;
//    AAN aan = new AAN();

//    // static int trailNumber;
//    //public Text trailNUmber;

//    // details of AAN;
//    static int steps = 10;
//    static float stepSize = 10;
//    public static float[] assistanceAngle = new float[steps];
//    public static float[] assistanceTorque = new float[steps];
//    public static float[] assistancePerformace = new float[steps];

//    //runnnin game 
//    public float trailDuration = 3;
//    public float stopClock;
//    public bool reached;
//    public bool onceReached;

//    public bool startTrial;

//    public float playSize = 0;
//    private string mech;
//    private string hospitalnum;
//    public static float[] aRom = { 0, 0 };
//    public static float[] pRom = { 0, 0 };

//    public  float targetAngle;
//    //GameObject target;

//    GameObject target;
//    GameObject player;

//    float gameduration = 0;
//    public static bool stopAssistance = true;
//    public float initialDirection = 0;
  
//    public int win;
//    public int index = 0;
//    public float reduceOppositeTimer = 0;
//    public float initialTorque;
//    public float prevTorq;
//    float prevSpawnTime = 0;
//    int val;
//    bool setZeroTorque;
//    public aanAssesment aanAssess;
//    private void Awake()
//    {
//        Resources.UnloadUnusedAssets();
//        if (instance == null)
//        {
//            instance = this;
//        }
//        else
//            Destroy(gameObject);

//        Application.targetFrameRate = 300;
//        QualitySettings.vSyncCount = 0;


//    }

//    // Start is called before the first frame update
//    void Start()
//    { 
//        playSize = 9;
        
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        if (startTrial)
//        {
//            stopClock -= Time.deltaTime;



//            AppData.plutoData.desTorq = TorqueProfile(getTorque(targetAngle));
//           // Debug.Log(AppData.plutoData.desTorq);
//            AppData.plutoData.desTorq = Mathf.Clamp(AppData.plutoData.desTorq, -1.2f, 1.2f);
//            SendToRobot.ControlParam(AppData.plutoData.mechs[AppData.plutoData.mechIndex], ControlType.TORQUE, true, false);

//        }
                
        
//        else
//        {
//            //do nothing
//        }

           


//    }

    

//    public void setPrameters()
//    {
        
//        mech = AppData.plutoData.mechs[AppData.plutoData.mechIndex];
//        aRom = AppData.aROM();
//        pRom = AppData.pROM();
       
//       // PlutoDataStructures.AAN aanprofile = new PlutoDataStructures.AAN(AppData.subjHospNum, AppData.plutoData.mechs[AppData.plutoData.mechIndex]);
//        assistanceTorque = aanAssesment.tempProfile;
//        Debug.Log(String.Join(",", assistanceTorque));
//        initialTorque = prevTorq;
//        initialDirection = getDirection();
//        stopClock = trailDuration;
//        onceReached = false; 
//        index = (int)aan.GetindexCorrected(targetAngle);
//        Debug.Log(targetAngle + "," + index);
//        stepSize = (pRom[1] - pRom[0]) / (steps - 1);
        
//        for (int i = 0; i < assistanceAngle.Length; i++)
//        {
//            assistanceAngle[i] = pRom[0] + stepSize * i;
//            if (i == assistanceAngle.Length)
//            {
//                assistanceAngle[i] = pRom[1];
//            }

//        }



//    }



//    public float getTorque(float targetAngle)
//    {
        
//        int i = Array.FindIndex(assistanceAngle, k => targetAngle <= k);
//        float torque = assistanceTorque[i - 1] + (targetAngle - assistanceAngle[i - 1]) * (assistanceTorque[i] - assistanceTorque[i - 1]) / (assistanceAngle[i] - assistanceAngle[i - 1]);
//        //if( targetAngle >= AppData.aROM()[0] && targetAngle <= AppData.aROM()[1])
//        //{
//        //    Debug.Log("here");
//        //    torque = torque * 0.1f;
//        //}
//        //Debug.Log(torque);
//        return (torque);
//    }

//    private void OnApplicationQuit()
//    {
//        // make 


//    }
//    float getDirection()
//    {
//        return Mathf.Sign(targetAngle - AppData.plutoData.angle);
//    }

//    public float TorqueProfile(float amp)
//    {
//        float time = trailDuration - stopClock;
//        time = (time / trailDuration);

       
//            if (Mathf.Abs(targetAngle - AppData.plutoData.angle) > 5 && initialDirection == getDirection() && !onceReached)
//            {
//                reduceOppositeTimer = 0;
//                prevTorq = Mathf.SmoothStep(initialTorque, amp, Mathf.Clamp(time, 0, trailDuration));

//            }
//            else
//            {
//            Debug.Log("Decreasing");
//                onceReached = true;

//                if (Mathf.Abs(targetAngle - AppData.plutoData.angle) > 3  && initialDirection != getDirection())
//                {
//                    reduceOppositeTimer += Time.deltaTime;
//                    reduceOppositeTimer = Mathf.Min(reduceOppositeTimer, 3);
//                        prevTorq = prevTorq - Mathf.Sign(prevTorq)* reduceOppositeTimer * 0.01f;
//                }


//            }
       
//        return prevTorq;

//    }
    
//    public class AAN
//    {


//        public bool isInAROM(float angle)
//        {
//            if(aRom[0] <= angle && angle <= aRom[1])
//            {
//                return true;
//            }

//            else
//            {
//                return false;
//            }
                
//        }
     

      
//        public float Getindex(float angle)
//        {
//            float temp = -999;
//            temp = (angle - pRom[0]) / stepSize;

//            return temp;
//        }

//        public int GetindexCorrected(float angle)
//        {
//            int i = Array.FindIndex(assistanceAngle, k => angle <= k);

//            return i;
           
//        }

        
       

//    }
//}



