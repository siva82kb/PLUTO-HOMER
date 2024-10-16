//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using PlutoDataStructures;


//public class PongLevelManager : MonoBehaviour

//{

//    public static PongLevelManager instance;
//    public int PongGameLevel;
//    GameObject rgbd;
//    public GameObject levelIncreaseObject;
//    bool showLevelIncrease;
//    int paddleSize;
//    float ballSpeed = 0.5f;
//    float enemSpeed = 0.5f;
//    // Start is called before the first frame update
//    void Start()
//    {
//        showLevelIncrease = false;
//        AppData.ReadGamePerformance();
//        PongGameLevel = AppData.startGameLevelRom;

//        SendToRobot.ControlParam(AppData.plutoData.mechs[AppData.plutoData.mechIndex], ControlType.TORQUE, false, false);
//        // Set control parameters
//        SendToRobot.ControlParam(AppData.plutoData.mechs[AppData.plutoData.mechIndex], ControlType.TORQUE, false, true);
//        AppData.plutoData.desTorq = 0;

//        //PongGameLevel = GameDifficulty.level;
//        ballSpeed = 2f + 0.3f * AppData.startGameLevelSpeed;
//        enemSpeed = 1.2f + 0.32f * AppData.startGameLevelSpeed;
//        rgbd = GameObject.FindGameObjectWithTag("Player");
//        rgbd.transform.localScale = new Vector3(0.2f, Mathf.Clamp(3f - 2 * 0.3f, .8f, 3f), 1f);

//        EnemyController.speed = enemSpeed;
//        BallController.speed = ballSpeed;


//    }

//    // Update is called once per frame
//    void Update()
//    {


//        if (Input.GetKey(KeyCode.LeftControl))
//        {
//            if (Input.GetKeyUp(KeyCode.L))
//            {
//                //showLevelIncrease = !showLevelIncrease;

//                ShowLevelControl();
//                //Code here
//                Debug.Log("skjn");
//            }

//        }
//    }

//    public void ShowLevelControl()
//    {
//        showLevelIncrease = !showLevelIncrease;
//        levelIncreaseObject.SetActive(showLevelIncrease);

//    }

//    public void OnSpeedUpButton()
//    {
//        AppData.startGameLevelSpeed += 1;
//        Debug.Log("Manual Increase" + AppData.startGameLevelSpeed);
//        ballSpeed = 2f + 0.3f * AppData.startGameLevelSpeed;
//        enemSpeed = 1.2f + 0.32f * AppData.startGameLevelSpeed;
//        EnemyController.speed = enemSpeed;
//        BallController.speed = ballSpeed;
//    }
//    public void OnSpeedDownButton()
//    {
//        AppData.startGameLevelSpeed -= 1;
//        Debug.Log("Manual decrease" + AppData.startGameLevelSpeed);
//        ballSpeed = 2f + 0.3f * AppData.startGameLevelSpeed;
//        enemSpeed = 1.2f + 0.32f * AppData.startGameLevelSpeed;
//        EnemyController.speed = enemSpeed;
//        BallController.speed = ballSpeed;
//    }
//}
