using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using PlutoDataStructures;
using System;

public class GameLog : MonoBehaviour
{
   // public static GameLog instance;
    GameObject Player, Target;
    //public  static string aanProfile;
    public static int[] startPerformace; //[start difficulty,start performace]
    public static int[] endPerformace; // [end difficulty,end performace]
    string _fname;
    // dummy for testing
    float time;
    private void Awake()
    {
        //if (AppData.subjHospNum == "")
        //{
        //    AppData.subjHospNum = "admin";
        //    // Init connect to robot.
        //    ConnectToRobot.Connect(AppData.plutoData);
        //    AppData.game = "PING PONG";
        //    AppData.regime = "NO ASSIST";
        //    AppData.plutoData.mechIndex = 0;
        //    // Send all relevant parameter information.
        //    // SendToRobot.ControlParam(AppData.plutoData.mechs[AppData.plutoData.mechIndex], ControlType.NONE, false, false);
        //    //  SendToRobot.TorqueSensorParam();
        //    // AppData.WriteSessionInfo("Control set to NONE. | Sent torque sensor param.");
        //    AppData.plutoData.mechIndex = 0;
        //}
    }
    void Start()
    {
        //if (instance == null)
        //{
        //    instance = this;
        //}
        //else
        //    Destroy(gameObject);
      //  Debug.Log(AppData.plutoData.mechIndex);
        //aan = new AAN(AppData.subjHospNum, AppData.plutoData.mechs[AppData.plutoData.mechIndex]);
        //Debug.Log(AppData.plutoData.mechIndex);
        //_fname = AppData.GameRawDatafile(AppData.subjHospNum, AppData.plutoData.mechs[AppData.plutoData.mechIndex], AppData.game, AppData.regime);
        //Debug.Log(_fname);
        //AppData.StartGameDataLog(_fname);
        //AppData.gameLogFileName = _fname;
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log(AppData.GameRawDatafile(AppData.subjHospNum, AppData.plutoData.mechs[AppData.plutoData.mechIndex], AppData.game, AppData.regime));
       
        //if (AppData.isGameLogging)
        //{
        //   // Debug.Log(Time.timeScale +"logging");
        //    Player = GameObject.FindGameObjectWithTag("Player");
        //    Target = GameObject.FindGameObjectWithTag("Target");

        //    if ( AppData.game == "COMPENSATION")
        //    {
                
        //            AppData.PlayerPos = Player.transform.eulerAngles.z.ToString();                
        //            AppData.TargetPos = Target.transform.eulerAngles.z.ToString();
        //    }
        //    else
        //    {

        //        if (Player != null)
        //            AppData.PlayerPos = "\"" + Player.transform.position.x.ToString() + "," + Player.transform.position.y.ToString() + "\"";
        //        else
        //            AppData.PlayerPos = "\"" + "XXX" + "," + "XXX" + "\"";
        //        if (Target != null)
        //            AppData.TargetPos = "\"" + Target.transform.position.x.ToString() + "," + Target.transform.position.y.ToString() + "\"";
        //        else
        //            AppData.TargetPos = "\"" + "XXX" + "," + "XXX" + "\"";
      
        //    }
        //    AppData.aanProfile = aanProfile;
        //    AppData.LogGameData();

        //}
        //time += Time.deltaTime;

       ////Debug.Log(AppData.oldANN.profile[0]);
       // if (time > 10)
       // {
       //     // 
       //     if (!logged)
       //     {
       //         Debug.Log(_fname);
       //         //  AppData.StopLogging();
       //         logged = true;
       //     }
       // }
    }
}
