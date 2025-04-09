using UnityEngine;

public class WaterTap : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject water;
    public float spawnTime;
    public float timeToSpawnWater;
    [SerializeField] private GameObject waterFill;
    float trailTime;
    float stopWatch;
    int waterdropCounter;
    int dropsToSpawn;
    AudioSource ads;
    public AudioClip[] clips;
    [SerializeField] GameObject targetClue;

    void Start()
    {
        trailTime = 3;
        dropsToSpawn = 50;
        ads = GetComponent<AudioSource>();
        ads.loop = false;

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        stopWatch += Time.deltaTime;
        if (stopWatch < trailTime)
        {
            targetClue.SetActive(true);

            waterFill.transform.localScale = new Vector3(1.5f, Mathf.SmoothStep(0.4f, 4.7f, stopWatch / trailTime), 1);
            if (ads.clip != clips[0])
            {
                ads.clip = clips[0];
                ads.Play();
            }
        }
        else if (stopWatch >= trailTime && waterdropCounter < dropsToSpawn)
        {
            targetClue.SetActive(false);
            if (ads.clip != clips[1])
            {
                ads.clip = clips[1];
                ads.Play();
            }
            GameObject GO = Instantiate(water,
                   new Vector2(transform.position.x, transform.position.y), Quaternion.identity) as GameObject;
            waterdropCounter++;
        }
        else
        {
            ads.Stop();
        }


    }

    public void startWaterFill()
    {
        // trialTime;
        stopWatch = 0;
        waterdropCounter = 0;
    }
}
