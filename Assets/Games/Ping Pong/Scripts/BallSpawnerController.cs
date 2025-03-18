using UnityEngine;
using System.Collections;

public class BallSpawnerController : MonoBehaviour
{

    public GameObject ball;
    void Start()
    {
        GameObject ballClone;
        ballClone = Instantiate(ball, this.transform.position, this.transform.rotation) as GameObject;
        ballClone.transform.SetParent(this.transform);
    }

    void Update()
    {
        if (transform.childCount == 0)
        {

            GameObject ballClone;
            ballClone = Instantiate(ball, this.transform.position, this.transform.rotation) as GameObject;
            ballClone.transform.SetParent(this.transform);
            EnemyController.stopWatch = 0;
            gameData.targetSpwan = true;
        }



    }


}
