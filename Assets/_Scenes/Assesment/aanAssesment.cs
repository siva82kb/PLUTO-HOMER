//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using PlutoDataStructures;
//using System;
//using UnityEngine.SceneManagement;
//using System.IO;
//using UnityEngine.UI;

//public class aanAssesment : MonoBehaviour
//{

//    private int CW;
//    public Text updateText;
//    int prevCW;
//    private bool done;
//    private bool changed;
//    public static float time;
//    // Start is called before the first frame update
//    int val;
//    static int steps = 10;
//    static float stepSize;
//    public static float[] assistanceAngle = new float[steps];
//    public static float[] angleIndex = new float[steps];
//    public static float[] assistanceTorque = new float[steps];
//    public static float[] tempProfile = new float[steps];
//    int currentIndex;
//    int prevIndex;
//    float cuurentTorque;
//    float prevTorque;



//    public static float[] assistancePerformace = new float[steps];
//    private string mech;
//    public string hospitalnum { get; private set; }

//    public static float[] aRom = { 0, 0 };
//    public static float[] pRom = { 0, 0 };
//    private float torque;
//    public float maxTor;
//    public float minTor;
//    bool reached;
//    public GameObject[] doneObj;
//    public GameObject[] assesbj;

//    public Text relaxtext;
//    bool debugActive;

//    public bool startAssessment;

//    public GameObject startButton;
//    void Start()
//    {

//        prevCW = 99;
//        CW = 1;
//        if (AppData.subjHospNum == "")
//        {

//            Debug.Log("EMPTY login");
//            AppData.subjHospNum = "admin";
//            AppData._port = "COM3";
//            ConnectToRobot.Connect(AppData.plutoData);
//            AppData.plutoData.mechIndex = 0;
//        }
//        Application.targetFrameRate = 300;
//        hospitalnum = AppData.subjHospNum;

//        //AppData.plutoData.mechIndex = 0;
//        //Debug.Log(AppData.plutoData.mechs[AppData.plutoData.mechIndex]);

//        mech = AppData.plutoData.mechs[AppData.plutoData.mechIndex];


//        pRom = AppData.pROM();
//        aRom = AppData.aROM();
//        stepSize = (pRom[1] - pRom[0]) / (steps - 1);
//        for (int i = 0; i < assistanceAngle.Length; i++)
//        {
//            assistanceAngle[i] = pRom[0] + stepSize * i;
//            if (i == assistanceAngle.Length)
//            {
//                assistanceAngle[i] = pRom[1];
//            }

//        }

//        for (int i = 0; i < assistanceAngle.Length; i++)
//        {
//            angleIndex[i] = Getindex(assistanceAngle[i]);
//        }

//        SendToRobot.ControlParam(mech, ControlType.TORQUE, false, false);
//        // Set control parameters
//        SendToRobot.ControlParam(mech, ControlType.TORQUE, false, true);
//        AppData.plutoData.desTorq = 0;
//        SendToRobot.ControlParam(mech, ControlType.TORQUE, true, false);


//        //foreach (var val in assistanceTorque){
//        //    val =val +0.1f

//        //}

//        /// read previous game performace
//       // AppData.ReadGamePerformance();
//        PlutoDataStructures.AAN aanprofile = new PlutoDataStructures.AAN(AppData.subjHospNum, AppData.plutoData.mechs[AppData.plutoData.mechIndex]);
//        prevIndex = Getindex(AppData.plutoData.angle);
//        //currentIndex = prevIndex;
//        debugActive = false;
//        done = true;
//        startAssessment = false;

//    }

//    // Update is called once per frame
//    void FixedUpdate()
//    {

//        Debug.Log(done); ;

//        //time += Time.deltaTime;
//        //Debug.Log(aRom[0] + "," + aRom[1] + ", " +Getindex(aRom[0]) + ", " + Getindex(aRom[1]);
//        //  AppData.plutoData.desTorq = 0.05f;
//        if (startAssessment)
//        {
//            AppData.plutoData.desTorq = applyRamp();
//        }

//        if (!done)
//        {
//            relaxtext.text = "Please relax and let the robot move your hand";
//            relaxtext.color = Color.white;



//            UpdateAssistanceIndex();
//        }

//        UpdatetextBox();
//        SendToRobot.ControlParam(AppData.plutoData.mechs[AppData.plutoData.mechIndex], ControlType.TORQUE, true, false);

//        if (Input.GetKeyDown("d"))
//        {
//            debugActive = !debugActive;


//        }

//        if (AppData.inputPressed() || Input.GetKeyDown(KeyCode.KeypadEnter))
//        {
//            if (startAssessment == false)
//            {
//                startAssess();
//            }
//            else
//            {
//                is_Done();
//            }
//        }
//    }

//    private void UpdatetextBox()
//    {
//        if (debugActive)
//        {
//            PlutoDataStructures.AAN aanprofile = new PlutoDataStructures.AAN(AppData.subjHospNum, AppData.plutoData.mechs[AppData.plutoData.mechIndex]);

//            updateText.text = "AAN profile = " + String.Join("|",
//                 new List<float>(tempProfile)
//                 .ConvertAll(i => i.ToString("0.0"))
//                 .ToArray()) + "\n" +
//                 "AAN Angle = " + String.Join("|",
//                 new List<float>(assistanceAngle)
//                 .ConvertAll(i => i.ToString("0.0"))
//                 .ToArray()) + "\n" +
//                 "AAN Index = " + String.Join("|",
//                 new List<float>(angleIndex)
//                 .ConvertAll(i => i.ToString())
//                 .ToArray()) + "\n" + 
//                 "AROM Angles/indes = " + aRom[0] + "," + aRom[1] + ", " + Getindex(aRom[0]) + ", " + Getindex(aRom[1]);
//        }
//        else
//        {
//            updateText.text = "";
//        }


//    }
//    public void UpdateAssistanceIndex()
//    {
//        prevTorque = AppData.plutoData.desTorq;
//        if (IndexChanged())
//        {
//            if (currentIndex == -1 || currentIndex == steps)
//            {

//            }
//            else
//            {
//                currentIndex = (int)Mathf.Clamp(currentIndex, 0, tempProfile.Length);

//                if (Mathf.Abs(tempProfile[currentIndex]) > 0.1)
//                {
//                    if (Mathf.Abs(tempProfile[currentIndex]) < Mathf.Abs(prevTorque))
//                    {
//                        tempProfile[currentIndex] = (tempProfile[currentIndex] + prevTorque) / 2;
//                    }
//                    else
//                    {
//                        tempProfile[currentIndex] = 0.7f * tempProfile[currentIndex] + 0.3f * prevTorque;
//                    }

//                }
//                else
//                    tempProfile[currentIndex] = prevTorque;
//            }

//            prevIndex = currentIndex;
//        }

//        Debug.Log(String.Join(",", tempProfile));
//        //else
//        //{
//        //    prevTorque = Mathf.Abs(prevTorque) < Mathf.Abs(AppData.plutoData.desTorq)? AppData.plutoData.desTorq: prevTorque;
//        //}



//    }

//    bool IndexChanged()
//    {
//        currentIndex = Getindex(AppData.plutoData.angle);
//        Debug.Log(currentIndex);
//        if (currentIndex != prevIndex)
//        {
//            return true;
//        }
//        else
//            return false;

//    }


//    public float getTorque(float targetAngle)
//    {
//        int i = Array.FindIndex(assistanceAngle, k => targetAngle <= k);
//        float torque = tempProfile[i - 1] + (targetAngle - assistanceAngle[i - 1]) * (tempProfile[i] - tempProfile[i - 1]) / (assistanceAngle[i] - assistanceAngle[i - 1]);
//        Debug.Log(torque);
//        return (torque);
//    }

//    public bool isNotFlaccid()
//    {
//        float[] maxROM = { 100, 50, 120, 75, 100, 100 };

//        if (Mathf.Abs(aRom[1] - aRom[0]) > 15)
//        {
//            Debug.Log("NOT Flaccid");
//            return true;
//        }
//        return false;
//    }


//    public void UpdateAssessmentProfile()
//    {

//        if (isNotFlaccid())
//        {
//            AppData.isflalccidControl = 0;
//            int aromMinIndex = Getindex(aRom[0]) == -1 ? 0 : Getindex(aRom[0]);
//            int aromMaxIndex = Getindex(aRom[1]) == -1 ? 0 : Getindex(aRom[1]); ;

//            float armMaxTorque = getTorque(aRom[1]) * 0.5f;
//            float armMinTorque = getTorque(aRom[0]) * 0.5f;

//            Debug.Log("initial" + String.Join(",", tempProfile));
//            Debug.Log(armMaxTorque + "- max, min" + armMinTorque);
//            float cuetorque = 0.08f;
//            for (int i = 0; i < tempProfile.Length; i++)
//            {
//                if (i < aromMinIndex)
//                {
//                    tempProfile[i] -= armMinTorque;




//                }
//                else if (i >= aromMinIndex && i <= aromMaxIndex)
//                {
//                    if (Mathf.Abs(tempProfile[i]) > cuetorque)
//                    {
//                        tempProfile[i] = 0.3f * tempProfile[i];
//                    }

//                }

//                else
//                {
//                    tempProfile[i] -= armMaxTorque;
//                }


//            }
//            AppData.isflalccidControl = 0;
//        }
//        else
//        {
//            AppData.isflalccidControl = 1;
//        }
//        Debug.Log("initial" + String.Join(",", tempProfile));



//    }
//    public void writeAssesmentFileAndExit()
//    {
//        UpdateAssessmentProfile();
//        string _fname = Path.Combine(SubjectData.Get_Subj_Assessment_Dir(AppData.subjHospNum), "aan_" + mech + ".csv");
//        using (StreamWriter file = new StreamWriter(_fname, true))
//        {
//            AppData.dateTime = DateTime.Now.ToString("Dyyyy-MM-ddTHH-mm-ss");
//            string res = String.Join(",", tempProfile);
//            file.WriteLine(AppData.dateTime + ", " + AppData.pROM()[0].ToString() + ", " + AppData.pROM()[1].ToString() + ", " + "10" + "," + res.ToString() + "," + AppData.isflalccidControl.ToString());
//            Debug.Log(_fname);
//        }
//        SceneManager.LoadScene("gameSelection");
//    }

//    public float applyRamp()
//    {


//        if ((AppData.plutoData.angle) > AppData.pROM()[1])
//        {
//            if (prevCW != 1)
//            {
//                time = 0;
//                CW = CW * -1;
//                prevCW = 1;
//            }

//        }
//        else if ((AppData.plutoData.angle) < AppData.pROM()[0])
//        {
//            if (prevCW != 0)
//            {
//                time = 0;
//                CW = CW * -1;
//                prevCW = 0;
//            }
//        }
//        if (!done)
//        {
//            float clampTorque = 1.2f;
//            torque = Mathf.Clamp(torque + CW * 0.0005f, -clampTorque, clampTorque);
//            if (Mathf.Abs(torque) == clampTorque)
//            {
//                time += Time.deltaTime;
//                if (time >= 3)
//                {
//                    time = 0;
//                    CW = CW * -1;
//                    prevCW = prevCW == 1 ? 0 : 1;

//                }
//            }
//        }
//        else
//        {
//            time += Time.deltaTime;


//            torque = Mathf.SmoothStep(torque, 0, Mathf.Clamp(time, 0, 5) / 5);
//            if (time > 5)
//            {
//                AppData.plutoData.desTorq = 0;
//                startAssessment = false;
//            }



//        }
//        if ((AppData.plutoData.angle < (1.2f * AppData.pROM()[0] - 30)) || (AppData.plutoData.angle > (AppData.pROM()[1] * 1.2f + 30)))
//        {
//            torque = 0;
//        }
//        return torque;

//    }



//    public int Getindex(float angle)
//    {
//        int i = Array.FindIndex(assistanceAngle, k => angle <= k);

//        return i;
//    }

//    public void is_Done()
//    {
//        done = true;
//        relaxtext.text = " Press FINISH to save Assistance Profile";
//        AppData.plutoData.desTorq = 0;
//        foreach (var obj in doneObj)
//        {
//            obj.SetActive(true);
//        }
//        foreach (var obj in assesbj)
//        {
//            obj.SetActive(false);
//        }
//        time = 0;
//        //  AppData.plutoData.desTorq = 0;

//    }

//    public void startAssess()
//    {
//        if (AppData.plutoData.angle >= AppData.pROM()[0] && AppData.plutoData.angle <= AppData.pROM()[1])
//        {
//            done = false;
//            startAssessment = true;
//            foreach (var obj in assesbj)
//            {
//                obj.SetActive(true);
//            }


//            time = 0;
//            startButton.SetActive(false);
//        }

//        else
//        {
//            relaxtext.text = "Move inside PROM & Press START";
//            relaxtext.color = Color.red;
//        }



//        //  this.gameObject.SetActive(false);
//    }


//    public void redo()
//    {
//        startAssessment = true;
//        done = false;
//        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
//    }




//}
