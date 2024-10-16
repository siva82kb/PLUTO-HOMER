 using UnityEngine;
using System.Collections;

public class PongPlayerController : MonoBehaviour
{

    //speed of player
    public float speed = 10;

    //bounds of player
    static float topBound = 4.5F;
    static float bottomBound = -4.5F;
    // player controls

    public static float playSize;
    public static float[] rom;
    //  public static int FlipAngle = 1;
    //public static float tempRobot, tempBird;

    public float ballTrajetoryPrediction;

    public static int reps;


 
    // Use this for initialization
    void Start()
    {
        playSize = Camera.main.orthographicSize;
        AppData.reps = 0;

        //AppData.timeOnTrail = 0;
        Time.timeScale = 0;
        topBound = playSize - this.transform.localScale.y / 4;
        bottomBound = -topBound;
        //calculateROM();
        //AppData.WriteTrainingSummaryFile(5, 10);
    }



    // Update is called once per frame
    void Update()
    {
       
        this.transform.position = new Vector2(this.transform.position.x, Angle2Screen(PlutoComm.angle));




    }
    public static float Angle2Screen(float angle)
    {
        AppData.MechanismData mechanismData = new AppData.MechanismData(AppData.MechanismSelection.selectedOption);
        float tmin = mechanismData.tmin;
        float tmax = mechanismData.tmax;
        Debug.Log("tmin_"+ tmin +"tmax_"+tmax);

        return Mathf.Clamp(-playSize + (angle - tmin) * (2 * playSize) / (tmax - tmin), bottomBound, topBound);

    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Target")
        {
            AppData.reps += 1;
            Debug.Log(AppData.reps);

        }
    }
    private void OntriggerEnter2D(Collision2D collision)
    {
        Debug.Log("hello");
    }
}
