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


//    AAN aan = new AAN();

//    // static int trailNumber;
//    //public Text trailNUmber;

//    // details of AAN;
//    int val;
//    static int steps = 10;
//    static float stepSize;
//    public static float[] assistanceAngle = new float[steps];
//    public static float[] assistanceTorque = new float[steps];
//    public static float[] assistancePerformace = new float[steps];
//    public bool setZero;
//    //runnnin game 
//    public float trailDuration = 3.5f;
//    public float stopClock;
//    public bool reached;
//    public bool onceReached;
//    public float reduceOppositeTimer = 0;
//    public float playSize = 0;
//    private string mech;
//    private string hospitalnum;
//    public static float[] aRom = { 0, 0 };
//    public static float[] pRom = { 0, 0 };
//    float prevAng;
//    bool angChange;
//    public float targetAngle;
//    //GameObject target;
//    float toqAmp;
//    public int count = 0;
//    GameObject target;
//    GameObject player;


//    public float blockduration = 10;
//    public static bool stopAssistance = true;
//    public float initialDirection;
//    public float initialTorque;
//    public float prevTorq;
//    public int win;
//    int index;
//    float[] rom;
//    private float ballTrajetoryPrediction;

//    bool wasNonZero;
//    BaallTrajectoryPlotter btp;

//    public Toggle isFlaccidToggle;
//    public bool isFlaccidControlOn;
//    bool paramSet = false;
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

//    // Start is called before the first frame update
//    void Start()
//    {
//        paramSet = false;
//        //AppData.regime = "MINIMAL ASSIST";
//        playSize = Camera.main.orthographicSize;
//        Application.targetFrameRate = 300;
//        //hospitalnum = AppData.subjHospNum;
//        targetAngle = -999;
//        //AppData.plutoData.mechIndex = 0;
//        //Debug.Log(AppData.plutoData.mechs[AppData.plutoData.mechIndex]);


//        //ActiveRangeOfMotion activeRange = new ActiveRangeOfMotion(AppData.subjHospNum,
//        //           mech);
//        //PassiveRangeOfMotion passiveRange = new PassiveRangeOfMotion(AppData.subjHospNum,
//        //           mech);


//        stopClock = trailDuration;
//        stepSize = (pRom[1] - pRom[0]) / steps;
//        for (int i = 0; i < assistanceAngle.Length; i++)
//        {
//            assistanceAngle[i] = pRom[0] + stepSize * i;
//            //Debug.Log(assistanceAngle[i]);
//        }



//        //for (int i = 0; i < assistanceTorque.Length; i++)
//        //{
//        //    assistanceTorque[i] = 0.2F;
//        //}
//        //SendToRobot.ControlParam(mech, ControlType.TORQUE, false, false);
//        //// Set control parameters
//        //SendToRobot.ControlParam(mech, ControlType.TORQUE, false, true);
//        //AppData.plutoData.desTorq = 0;
//        //SendToRobot.ControlParam(mech, ControlType.TORQUE, true, false);


//        //foreach (var val in assistanceTorque){
//        //    val =val +0.1f

//        //}

//        /// read previous game performace

//        //PlutoDataStructures.AAN aanprofile = new PlutoDataStructures.AAN(AppData.subjHospNum, AppData.plutoData.mechs[AppData.plutoData.mechIndex]);


//        setPrameters();
//        AppData.ReadGamePerformance();

//        wasNonZero = true;

//        setZero = false;
//        paramSet = true;
//    }



//    // Update is called once per frame
//    void Update()
//    {


//        stopClock -= Time.deltaTime;

//        if (GameObject.FindGameObjectsWithTag("Target").Length > 0)
//        {
//            btp = GameObject.FindGameObjectWithTag("Target").GetComponent<BaallTrajectoryPlotter>();
//            AppData.isflalccidControl = isFlaccidControlOn ? 1 : 0;
//            ballTrajetoryPrediction = btp.targetPosition;
//            //Debug.Log(btp.ballDistance);
//            //Debug.Log(btp.ballVelocity.magnitude);

//            AppData.trailNumber++;
//            AppData.isAssisted = 1;

//            if ((Mathf.Abs(btp.ballDistance) / Mathf.Abs(btp.ballVelocity.magnitude)) < 4 && btp.ballVelocity.x > 0 && (Mathf.Abs(btp.ballDistance) / Mathf.Abs(btp.ballVelocity.magnitude)) > 1)
//            {
//                if (btp.transform.position.x < 5.5)
//                {
//                    // targetAngle = ScreentoAngle(ballTrajetoryPrediction);


//                }
//                if (wasNonZero == false)
//                {
//                    targetAngle = ScreentoAngle(ballTrajetoryPrediction);

//                    //Debug.Log("falsefunc" + Mathf.Abs(btp.ballDistance) / Mathf.Abs(btp.ballVelocity.magnitude)) ;
//                    trailDuration = Mathf.Abs(btp.ballDistance) / Mathf.Abs(btp.ballVelocity.magnitude);
//                    stopClock = trailDuration;
//                    onceReached = false;
//                    initialTorque = AppData.plutoData.desTorq;
//                    wasNonZero = true;
//                    initialDirection = getDirection();



//                }



//                AppData.plutoData.desTorq = TorqueProfile(getTorque(targetAngle));
//                //   Debug.Log(" assisting with: " + AppData.plutoData.desTorq + "," + btp.ishittingplayer);

//            }

//            if (btp.ballVelocity.x < 0 || Time.time == 0)
//            {
//                if (wasNonZero)
//                {
//                    Debug.Log("invert");
//                    trailDuration = 1.5f;
//                    stopClock = trailDuration;
//                    initialTorque = AppData.plutoData.desTorq;
//                    onceReached = false;
//                    wasNonZero = false;

//                }
//                AppData.plutoData.desTorq = TorqueProfile(0);
//                AppData.isAssisted = 0;
//            }
//            //  Debug.Log(" assissting: " + AppData.plutoData.desTorq);


//        }
//        else
//        {
//            AppData.plutoData.desTorq = 0;
//        }
//        //Debug.Log(" assissting: " + AppData.plutoData.desTorq);

//        SendToRobot.ControlParam(mech, ControlType.TORQUE, true, false);



//        //Debug.Log(String.Join(",", initialDirection == getDirection()));


//    }
//    public float ScreentoAngle(float y_pos)
//    {


//        //Debug.Log("actuaal Pos:" + ballTrajetoryPrediction +
//        //            "Calculated Pos" + Angle2Screen(-((AppData.pROM()[0]) + (y_pos + 5) * (AppData.pROM()[1] - AppData.pROM()[0]) / (2 * 5))));

//        //return b1 + (s - a1) * (b2 - b1) / (a2 - a1);

//        return Mathf.Clamp(((AppData.pROM()[0]) + (y_pos + playSize) * (AppData.pROM()[1] - AppData.pROM()[0]) / (2 * playSize)), AppData.pROM()[0], AppData.pROM()[1]);




//    }
//    float getDirection()
//    {
//        return Mathf.Sign(targetAngle - AppData.plutoData.angle);
//    }


//    public void setPrameters()
//    {

//        mech = AppData.plutoData.mechs[AppData.plutoData.mechIndex];
//        aRom = AppData.aROM();
//        pRom = AppData.pROM();

//        isFlaccidControlOn = false;

//        //checkIfFlaccid();
//        //isFlaccidToggle.isOn = isFlaccidControlOn;

//        PlutoDataStructures.AAN aanprofile = new PlutoDataStructures.AAN(AppData.subjHospNum, AppData.plutoData.mechs[AppData.plutoData.mechIndex]);
//        assistanceTorque = aanprofile.profile;

//        isFlaccidControlOn = aanprofile.isFlaccid == 1 ? true : false;

//        Debug.Log(String.Join(",", assistanceTorque));
//        // initialTorque = prevTorq;
//        stopClock = trailDuration;
//        onceReached = false;

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

//        paramSet = true;

//    }
//    void checkIfFlaccid()
//    {
//        float[] maxROM = { 100, 50, 120, 75, 100, 100 };

//        if (Mathf.Abs(aRom[1] - aRom[0]) < 10 && Mathf.Abs(pRom[1] - pRom[0]) >= maxROM[AppData.plutoData.mechIndex])
//        {
//            isFlaccidControlOn = true;
//        }
//        else
//            isFlaccidControlOn = false;
//    }
//    public static float Angle2Screen(float angle)
//    {
//        float playSize = 5;
//        return Mathf.Clamp(-playSize + (angle - AppData.pROM()[0]) * (2 * playSize) / (AppData.pROM()[1] - AppData.pROM()[0]), -100, 100);

//    }

//    public float getTorque(float targetAngle)
//    {
//        float torque;
//        targetAngle = Mathf.Clamp(targetAngle, AppData.pROM()[0], AppData.pROM()[1]);
//        int i = Array.FindIndex(assistanceAngle, k => targetAngle <= k);
//        i = i == -1 ? assistanceAngle.Length - 1 : i;

//        if (i > 0)
//        {
//            torque = assistanceTorque[i - 1] + (targetAngle - assistanceAngle[i - 1]) * (assistanceTorque[i] - assistanceTorque[i - 1]) / (assistanceAngle[i] - assistanceAngle[i - 1]);

//        }
//        else
//        {
//            torque = assistanceTorque[i];
//        }
//        //Debug.Log(String.Join(",", assistanceAngle));
//        //Debug.Log(String.Join(",", assistanceTorque));
//        // Debug.Log("Index:" + i + "TargetAngle :" + targetAngle + "TargetTorque:" + torque);


//        torque = Mathf.Clamp(torque, assistanceTorque.Min(), assistanceTorque.Max());
//        return (torque);

//    }

//    private void OnApplicationQuit()
//    {
//        // make 


//    }
//    public float TorqueProfile(float amp)
//    {


//        if (!isFlaccidControlOn)
//        {
//            return (normalController(amp));
//        }
//        else
//        {
//            float assistanceTorque = Mathf.Abs(amp) < 0.2 ? 0.2f : Mathf.Abs(amp);
//            Debug.Log("flaccid" + assistanceTorque);
//            return (flaccidController(assistanceTorque));
//        }




//    }

//    float flaccidController(float amp)
//    {
//        float time;
//        Debug.Log("amp" + amp);
//        if (stopClock == trailDuration)
//        {
//            time = 0;
//        }
//        else
//        {
//            time = (trailDuration - stopClock);
//            time = (time / trailDuration);
//        }

//        if (amp != 0)
//        {
//            if (Mathf.Abs(targetAngle - AppData.plutoData.angle) > 2 && initialDirection == getDirection())
//            {

//                reduceOppositeTimer = 0;

//                prevTorq = Mathf.SmoothStep(initialTorque, amp, Mathf.Clamp(time, 0, trailDuration));
//                //Debug.Log("here" + prevTorq);

//            }
//            else
//            {
//                onceReached = true;
//                // Debug.Log("Decreasing");

//                if (Mathf.Abs(targetAngle - AppData.plutoData.angle) > 3 && initialDirection != getDirection())
//                {

//                    reduceOppositeTimer += Time.deltaTime;
//                    reduceOppositeTimer = Mathf.Min(reduceOppositeTimer, 3);
//                    prevTorq = prevTorq - Mathf.Sign(prevTorq) * reduceOppositeTimer * 0.01f;
//                }


//            }
//        }
//        else
//        {
//            Debug.Log("zero");
//            prevTorq = Mathf.SmoothStep(initialTorque, 0, Mathf.Clamp(time, 0, trailDuration));

//        }
//        // Debug.Log("fromfunction" + prevTorq );
//        if (AppData.plutoData.mechIndex != 2)
//            return prevTorq;
//        else
//            return -prevTorq;

//        //float time = trailDuration - stopClock;
//        //time = (time / trailDuration);
//        //if (AppData.regime == "MINIMAL ASSIST" && amp != 0)
//        //{
//        //    if (Mathf.Abs(targetAngle - AppData.plutoData.angle) > 2)
//        //    {
//        //        if (getDirection() == initialDirection)
//        //        {
//        //            // reduceOppositeTimer = 0;
//        //            if (onceReached == false)
//        //            {
//        //                Debug.Log("starting");
//        //                prevTorq = Mathf.SmoothStep(initialTorque, getDirection() * Mathf.Abs(amp), Mathf.Clamp(time, 0, trailDuration));
//        //                if (AppData.plutoData.mechIndex != 2)
//        //                    return prevTorq;
//        //                else
//        //                    return -prevTorq;
//        //            }
//        //            else
//        //            {
//        //                reduceOppositeTimer += Time.deltaTime;
//        //                reduceOppositeTimer = Mathf.Min(reduceOppositeTimer, 3);
//        //                if (Mathf.Abs(prevTorq) > 0.05)
//        //                    prevTorq = prevTorq + Mathf.Sign(prevTorq) * reduceOppositeTimer * 0.01f;
//        //                if (AppData.plutoData.mechIndex != 2)
//        //                    return prevTorq;
//        //                else
//        //                    return -prevTorq;

//        //            }

//        //        }
//        //        else
//        //        {
//        //            reduceOppositeTimer += Time.deltaTime;
//        //            onceReached = true;
//        //            if (Mathf.Abs(prevTorq) > 0.05)
//        //                prevTorq = prevTorq - Mathf.Sign(prevTorq) * reduceOppositeTimer * 0.01f * Mathf.Abs(targetAngle - AppData.plutoData.angle);
//        //            if (AppData.plutoData.mechIndex != 2)
//        //                return prevTorq;
//        //            else
//        //                return -prevTorq;
//        //        }
//        //    }
//        //    else
//        //    {
//        //        if (AppData.plutoData.mechIndex != 2)
//        //            return prevTorq;
//        //        else
//        //            return -prevTorq;
//        //    }
//        //}

//        //else
//        //{
//        //    prevTorq = 0;
//        //    return prevTorq;
//        //}
//    }
//    float normalController(float amp)
//    {
//        float time;
//        Debug.Log("amp" + amp);
//        if (stopClock == trailDuration)
//        {
//            time = 0;
//        }
//        else
//        {
//            time = (trailDuration - stopClock);
//            time = (time / trailDuration);
//        }

//        if (amp != 0)
//        {
//            if (Mathf.Abs(targetAngle - AppData.plutoData.angle) > 2 && initialDirection == getDirection())
//            {

//                reduceOppositeTimer = 0;

//                prevTorq = Mathf.SmoothStep(initialTorque, getDirection() * amp, Mathf.Clamp(time, 0, trailDuration));
//                //Debug.Log("here" + prevTorq);

//            }
//            else
//            {
//                onceReached = true;
//                // Debug.Log("Decreasing");

//                if (Mathf.Abs(targetAngle - AppData.plutoData.angle) > 3 && initialDirection != getDirection())
//                {

//                    reduceOppositeTimer += Time.deltaTime;
//                    reduceOppositeTimer = Mathf.Min(reduceOppositeTimer, 3);
//                    prevTorq = prevTorq - Mathf.Sign(prevTorq) * reduceOppositeTimer * 0.01f;
//                }


//            }
//        }
//        else
//        {
//            Debug.Log("zero");
//            prevTorq = Mathf.SmoothStep(initialTorque, 0, Mathf.Clamp(time, 0, trailDuration));

//        }
//        // Debug.Log("fromfunction" + prevTorq );
//        return prevTorq;
//    }
//    public void OnFlaccidToggleSelect()
//    {
//        if (paramSet)
//        {
//            isFlaccidControlOn = isFlaccidToggle.isOn;
//            string _fname = Path.Combine(SubjectData.Get_Subj_Assessment_Dir(AppData.subjHospNum), "aan_" + mech + ".csv");
//            using (StreamWriter file = new StreamWriter(_fname, true))
//            {
//                AppData.dateTime = DateTime.Now.ToString("Dyyyy-MM-ddTHH-mm-ss");
//                string res = String.Join(",", assistanceTorque);
//                file.WriteLine(AppData.dateTime + ", " + AppData.pROM()[0].ToString() + ", " + AppData.pROM()[1].ToString() + ", " + "10" + "," + res.ToString() + "," + Convert.ToInt32(isFlaccidControlOn).ToString());
//                Debug.Log(_fname);
//            }
//        }
//    }
//    public class AAN
//    {


//        public bool isInAROM(float angle)
//        {
//            if (aRom[0] <= angle && angle <= aRom[1])
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
