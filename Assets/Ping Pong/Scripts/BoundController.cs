using UnityEngine;
using System.Collections;

public class BoundController : MonoBehaviour
{

    //enemy transform
    public Transform enemy;

    //public PingPonGAANController ppAAN;
    public int enemyScore;
    public int playerScore;
    public AudioClip[] audioClips; // win ,loose

    void Start()
    {
        enemyScore = 0;
        playerScore = 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //
        if (other.gameObject.tag == "Target")
        {
            if (other.gameObject.GetComponent<Rigidbody2D>().velocity.x > 0)
            {
                playAudio(1);
                enemyScore++;
            }
            else
            {
                playerScore++;
                AppData.gameScore++;
                playAudio(0);
            }


            //Destroys other object
            Destroy(other.gameObject);

            //sets enemy's position back to original
            enemy.position = new Vector3(-6, 0, 0);
            //pauses game
            Time.timeScale = 0;
        }
    }
    void playAudio(int clipNumber)
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = audioClips[clipNumber];
        audio.Play();

    }
}
