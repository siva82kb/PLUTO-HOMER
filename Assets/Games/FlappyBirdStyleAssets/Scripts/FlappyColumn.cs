using System;
using UnityEngine;

public class FlappyColumn : MonoBehaviour

{
    public AudioClip saw;

    float prevSpawnTime = 0;
    void Start()
    {
        GetComponent<AudioSource>().playOnAwake = false;
        GetComponent<AudioSource>().clip = saw;
    }

    void Update()
    {
        prevSpawnTime += Time.deltaTime;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player" && collision.GetComponent<BirdControl>() != null && prevSpawnTime > 1)
        {
            prevSpawnTime = 0;
            gameData.events = Array.IndexOf(gameData.tukEvents, "passed");
            Debug.Log("Passed");
            FlappyGameControl.instance.BirdScored();
          //  gameData.birdPassed = true;
        }

    }


}