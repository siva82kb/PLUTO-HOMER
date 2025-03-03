using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class FlappyColumnPool : MonoBehaviour
{
    enum AssessStates
    {
        DAY = 1,
        EVE = 2,
        NIGHT = 3
    };

    public int _state; 
    public static FlappyColumnPool instance;
    public int columnPoolSize = 5;
    private GameObject[] columns;
    public GameObject[] columnPrefab;
    public GameObject[] backgrounds;
    public float columnMin = -5.3f;
    public float ColumnMax = 1.3f;
    public Vector2 objectPoolPosition = new Vector2(-15,-25);
    private float timeSinceLastSpawn = 3;
    public float spawnRate = 4;
    private float spawnXposition = 16;
    private int CurrentColumn = 0;
    private GameObject[] top;
    private GameObject[] bottom;
    public Image targetImage;
    private int randomTargetIndex;
    private int spawnCounter = 0;
    private System.Random random = new System.Random();

    public  int difficultyLevel =10;
    bool setup;

    float prevSpawnTime;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != null)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

        setup = false;
        randomTargetIndex= random.Next(1,11);


    }

    void Update()
    {
        prevSpawnTime += Time.deltaTime;
        if (!setup)
        {
            int y = Random.Range(0, 3);
            _state = y;
           
            FlappyColumnPool.instance.difficultyLevel = 6;
            columns = new GameObject[columnPoolSize];
            for (int i = 0; i < columnPoolSize; i++)
            {
                columns[i] = (GameObject)Instantiate(columnPrefab[_state], objectPoolPosition, Quaternion.identity);
            }
            top = GameObject.FindGameObjectsWithTag("Top");

            chooseBackground();
            FlappyGameControl.instance.scrollSpeed = -2 - 1 * .1f;
            setup = true;
        }

        FlappyGameControl.instance.scrollSpeed = -2 - 2 * (.1f + gameData.gameSpeedTT);
        if(spawnCounter == randomTargetIndex) {
            targetImage.gameObject.SetActive(true);
            Debug.Log("Displaying the target image!");
        }
        else
        {
            targetImage.gameObject.SetActive(false);
        }
    }

    public void chooseBackground()
    {
        foreach (GameObject obj in backgrounds)
        {
            obj.SetActive(false);
        }
        backgrounds[_state].SetActive(true);
    }
    public void spawnColumn()
    {
        if (!FlappyGameControl.instance.gameOver && prevSpawnTime > 2)
        {
            prevSpawnTime = 0;
            spawnCounter++;
            float x = Random.Range(1, 7);
            columns[CurrentColumn].transform.position = new Vector2(BirdControl.rb2d.transform.position.x+ spawnXposition, FB_spawnTargets.instance.TargetSpawn().y);
            columns[CurrentColumn].tag = "Target";
            if(CurrentColumn == 0)
            {
                columns[columnPoolSize - 1].tag = "Untagged";
            }
            else
            {
                columns[CurrentColumn - 1].tag = "Untagged";

            }
                       
           //FB_spawnTargets.instance.trailDuration = Mathf.Clamp((BirdControl.rb2d.transform.position.x + -columns[CurrentColumn].transform.position.x) / FlappyGameControl.instance.scrollSpeed,2,4);
            CurrentColumn += 1;
            if (CurrentColumn >= columnPoolSize)
            {
                CurrentColumn = 0;
            }
            
        }
    }
}
