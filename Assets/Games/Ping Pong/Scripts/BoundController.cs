using UnityEngine;
using System.Collections;
using UnityEngine.SocialPlatforms;
using System;

public class BoundController : MonoBehaviour
{
    public Transform enemy;
    public AudioClip[] audioClips; // win ,loose
    private PongGameController PGC;

    void Start()
    {
        PGC = GameObject.FindAnyObjectByType<PongGameController>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Target")
        {
            if (other.gameObject.GetComponent<Rigidbody2D>().velocity.x > 0)
            {
                playAudio(1);
                PGC.BallMissed();

               // PGC.enemyScore++;
            }
            else
            {
              //  PGC.playerScore++;
                playAudio(0);
            }
            Destroy(other.gameObject);
            enemy.position = new Vector3(-6, 0, 0);
        }
    } 
    void playAudio(int clipNumber)
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = audioClips[clipNumber];
        audio.Play();
    }
    
}
