//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
////using Michsky.UI;
//using TMPro;
////using Michsky.UI.ModernUIPack;
//using UnityEngine.SceneManagement;
////using Michsky.MUIP;

//public class playerMovment : MonoBehaviour
//{
//    //public static playerMovment Instance { get; private set; }
//    public GameObject player;
//    public GameObject target;

//    public float isGazed = 0;

//    public TMP_Text scoreText;
//    public TMP_Text timerText;

//    public TMP_Text menuText;

//    public float gametimer = 120;

//    public float starttimer =5;

//   // public ProgressBar targetProgress;

//    public int score;

//    public float ontargetTime;

    
//    float newX;

//    public AudioSource audioSource;
//    public AudioClip scoredSound;
//    public AudioClip onTarget;


//    float playerYpos = -2;


//    //speed of player
//    public float speed = 10;

//    //bounds of player
//    public float topBound;
//    public float bottomBound;
//    // player controls

//    public static float playSize = 10;
//    public static float[] rom;
//    //  public static int FlipAngle = 1;
//    //public static float tempRobot, tempBird;

//    public float ballTrajetoryPrediction;

//    public float[] pROM ;
//    public float targetAngle = 0;

//    public GameObject GameOverCanvas;
//    public bool isGameOver;

//    public bool rightBar;
//    // Start is called before the first frame update
//    void Start()
//    {

//       // targetProgress.isOn = true;
//        ontargetTime = 0;
//       /// target.transform.position = new Vector2(Random.Range(-9, 9), playerYpos);

//        AppData.game = "PROPRIOCEPTION";
//        AppData.regime = "NO ASSIST";
//        isGameOver = false;
//        pROM = new float[] { -90, 0f };

//    }

//    // Update is called once per frame
//    void Update()
//    {
//        // Debug.Log(targetProgress.specifiedValue);
//        if (!isGameOver && gametimer>0)
//        {
            
//         //   gametimer -= Time.deltaTime;

//        //    timerText.text = "Time Left :" + ((int)gametimer).ToString();
//          //  scoreText.text = "Score :" + score.ToString();
//          //  targetProgress.specifiedValue = ontargetTime * 100;
//         //   targetAngle = Screen2Angle(target.transform.position.x);
//        }
//        if (gametimer < 0)
//        {
//            AppData.StopGameLogging();
//            GameOverCanvas.SetActive(true);
          
//            Debug.Log("here");
           
//        }


//        // Debug.Log(Input.mousePosition.x);
//        //float playerPos = Camera.main.ScreenToWorldPoint((Input.mousePosition)).x;

//        //player.transform.position = new Vector2(playerPos, playerYpos);
//        player.transform.position = new Vector3( Angle2Screen(AppData.plutoData.angle), playerYpos,0);

        
//      //  Debug.Log(AppData.plutoData.angle);
//        // moveTargt();
//    }


//    public float Angle2Screen(float angle)
//    {

//        if (rightBar)
//        {
//            return Mathf.Clamp(bottomBound + (angle - pROM[1]) * (topBound - bottomBound) / (pROM[0] - pROM[1]), bottomBound, topBound);
//        }
//        else
//        {
//            return Mathf.Clamp(bottomBound + (angle - pROM[0]) * (topBound - bottomBound) / (pROM[1] - pROM[0]), bottomBound, topBound);
//        }

//    }

//    public float Screen2Angle(float pos)
//    {
//        return (pROM[0] + (pos - (-playSize)) * (pROM[1]-pROM[0]) / (2*playSize));
//    }

//    public void targetSpawn()
//    {


//    }

//    void OnTriggerEnter2D(Collider2D col)
//    {
//        //Debug.Log(col.gameObject.name + " : " + gameObject.name + " : " + Time.time);
//        //ontargetTime = 0;
      
//        //audioSource.PlayOneShot(onTarget);


//    }
//    void OnTriggerExit2D(Collider2D col)
//    {
//        //Debug.Log("exited");
//        //audioSource.Stop();
//        //ontargetTime = 0;
      

//    }

//    private void OnTriggerStay2D(Collider2D collision)
//    {
       
//        //ontargetTime += Time.deltaTime;

     
//        //if (ontargetTime > 1)
//        //{
//        //    audioSource.Stop();
//        //    audioSource.clip = scoredSound;
//        //    audioSource.Play();
//        //     newX = Random.Range(-topBound, topBound);

//        //    while(Mathf.Abs(target.transform.position.x - newX) < 3)
//        //    {
//        //        newX = Random.Range(-topBound, topBound);
//        //    }


//        //    Debug.Log("Scored!");
//        //    score++;
//        //    AppData.gameScore = score;
//        //    target.transform.position = new Vector2(newX, playerYpos);
//        //    ontargetTime = 0;
//        //    targetProgress.specifiedValue = 0;
//        //}


//    }


//    public void moveTargt()
//    {
//        target.transform.position = new Vector2(newX +3 * Mathf.Sin(1.3f * Time.time)* Mathf.Sin(0.5f * Time.time), playerYpos);
//    }

//    public void startGame()
//    {
//       string  _fname = AppData.GameRawDatafile(AppData.subjHospNum, AppData.plutoData.mechs[AppData.plutoData.mechIndex], AppData.game, AppData.regime);
//        Debug.Log(_fname);
//        AppData.StartGameDataLog(_fname);
//        AppData.gameLogFileName = _fname;
//        gametimer = 90;
//        AppData.gameScore = 0;
//        isGameOver = false;
//        GameOverCanvas.SetActive(false);
//    }
//    public void ExitToMenu()
//    {
//        SceneManager.LoadScene("gameSelection");
//    }

    
//}
