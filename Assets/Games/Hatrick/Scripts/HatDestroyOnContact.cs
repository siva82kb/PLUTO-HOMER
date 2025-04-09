using System;
using UnityEngine;

public class HatDestroyOnContact : MonoBehaviour
{
    public AudioSource gamesound;
    public AudioClip loose;

    void Start()
    {

    }
    void Update()
    {

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Bomb(Clone)")
        {
            gamesound = gameObject.GetComponent<AudioSource>();
            gamesound.clip = loose;
            gamesound.Play();
            gameData.moving = false;
            gameData.events = Array.IndexOf(gameData.hatEvents, "BombMissed");

            if (GameObject.FindGameObjectWithTag("Target") != null)
            {
                Destroy(GameObject.FindGameObjectWithTag("Target"));
            }
            HatGameController.instance.balldestroyed = true;
            HatGameController.instance.SpawnTarget(HatTrickGame.Instance.gameSpeed.Value);
            Destroy(collision.gameObject);
        }

        if (collision.gameObject.tag == "Target")
        {
            gamesound = gameObject.GetComponent<AudioSource>();
            gamesound.clip = loose;
            gamesound.Play();
            gameData.moving = false;
            gameData.events = Array.IndexOf(gameData.hatEvents, "BallMissed");

            HT_spawnTargets1.instance.reached = false;
            HatGameController.instance.balldestroyed = true;
            HatGameController.instance.SpawnTarget(HatTrickGame.Instance.gameSpeed.Value);
            Destroy(collision.gameObject);
        }
    }
}
