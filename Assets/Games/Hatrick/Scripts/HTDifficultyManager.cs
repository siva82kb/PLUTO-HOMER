using UnityEngine;

public class HTDifficultyManager : MonoBehaviour
{
    public int HTGameLevel;

    GameObject rgbdPlayer;
    public static float ballSpeed = 0.5f;
    public static Vector3 Scale;
    public bool paramSet = false;
    // Start is called before the first frame update
    void Start()

    {

        Time.timeScale = 1;
    }

    // Update is called once per frame
    void Update()
    {

        if (!paramSet)
        {

            HTGameLevel = 1;
            ballSpeed = 2f + 0.3f * 1;
            //enemSpeed = 1f + 0.32f * HTGameLevel;
            rgbdPlayer = GameObject.FindGameObjectWithTag("Player");
            //rgbdPlayer = GameObject.FindGameObjectWithTag("Target");
            Scale = new Vector3(1f - 0.05f * HTGameLevel, 1f - 0.05f * HTGameLevel, 1f - 0.05f * HTGameLevel);
            rgbdPlayer.transform.localScale = Scale;
            //Scale = new Vector3(1f - 0.2f * HTGameLevel, 1f - 0.2f * HTGameLevel, 1f - 0.2f * HTGameLevel);
            paramSet = true;
            //Debug.Log(AppData.startGameLevel);
        }
    }


 


}
