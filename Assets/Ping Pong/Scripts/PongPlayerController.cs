using UnityEngine;
using System.Collections;

public class PongPlayerController : MonoBehaviour
{
    public float speed = 10;

    static float topBound = 4.5F;
    static float bottomBound = -4.5F;

    public static float playSize;
    public static float[] rom;
    public float ballTrajetoryPrediction;
    public static int reps;
    private Vector3 previousPlayerPosition;
    private float playerMovementTime = 0f;
    private Coroutine movementCoroutine;

    void Start()
    {
        playSize = Camera.main.orthographicSize;
        gameData.reps = 0;
        Time.timeScale = 0;
        topBound = playSize - this.transform.localScale.y / 4;
        bottomBound = -topBound;

        previousPlayerPosition = transform.position;
    }
    void Update()
    {
        checkPlayerMovement();
        this.transform.position = new Vector2(this.transform.position.x, playerMovementArea(PlutoComm.angle));
    }

    public static float playerMovementArea(float angle)
    {
        MechanismData mechanismData = new MechanismData(AppData.selectMechanism);
        float tmin = mechanismData.tmin;
        float tmax = mechanismData.tmax;
        return Mathf.Clamp(-playSize + (angle - tmin) * (2 * playSize) / (tmax - tmin), bottomBound, topBound);
    }

    private void checkPlayerMovement()
    {
        Vector3 currentPlayerPosition = transform.position;        
        float playerDistanceMoved = Vector3.Distance(currentPlayerPosition, previousPlayerPosition); // Calculate the distance moved by the player
        if (playerDistanceMoved > 0.001f) 
        {
            if (movementCoroutine == null)
            {
                movementCoroutine = StartCoroutine(trackMovementTime());
            }
        }
        else
        {
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
        }
        previousPlayerPosition = currentPlayerPosition;
    }

    private IEnumerator trackMovementTime()
    {
        while (true)
        {
            playerMovementTime += Time.deltaTime;
            gameData.moveTime = playerMovementTime;
            yield return null;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Target")
        {
            gameData.reps += 1;
            Debug.Log(gameData.reps + " target Hit");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Hello");
    }
}
