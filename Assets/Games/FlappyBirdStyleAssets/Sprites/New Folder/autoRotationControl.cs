using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class autoRotationControl : MonoBehaviour
{
    float prevAng;
    float startAngle;
    float rotation;
    float rotationAvg;
    // Start is called before the first frame update
    private Rigidbody2D rb;
    float time = 0;
    float endTime = 0;
    public FlappyGameControl FGC;
    float gameOverTime;
    void Start()
    {
        gameOverTime = 0;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float currentAng = PlutoComm.angle;
        //transform.eulerAngles = new Vector3(0, 0, -12);
        //rb.AddTorque(AppData.plutoData.angle*10.0f);

        if (FGC.gameOver)
        {
            endTime += Time.deltaTime;

            transform.eulerAngles = (new Vector3(0, 0, Mathf.Abs(60 * Mathf.Sin(0.2f * endTime))));

        }
        else
        {

            rotation = (PlutoComm.angle - prevAng);
            rotationAvg = approxRollingAverage(rotationAvg, rotation);
            if (Mathf.Abs(PlutoComm.angle - prevAng) < 0.00011)
            {

                // Debug.Log(" smooth zero");
                if (time == 0)
                {
                    startAngle = transform.eulerAngles.z < 180 ? transform.eulerAngles.z : transform.eulerAngles.z - 360;
                    //Debug.Log(startAngle);
                }
                time += Time.deltaTime;


                transform.eulerAngles = new Vector3(0, 0, Mathf.SmoothStep(startAngle, 0, time * 3f));



            }
            else
            {

                // Debug.Log(" rotating");
                time = 0;
                transform.Rotate(new Vector3(0, 0, rotationAvg));

            }
        }


        prevAng = currentAng;
        //rotates 50 degrees per second around z axis
        //Debug.Log( transform.eulerAngles.z<180? transform.eulerAngles.z: transform.eulerAngles.z-360);
        //Debug.Log(rotation);





    }
    float approxRollingAverage(float avg, float new_sample)
    {

        avg = 0.3f * avg + 0.5f * new_sample;

        return avg;
    }
}
