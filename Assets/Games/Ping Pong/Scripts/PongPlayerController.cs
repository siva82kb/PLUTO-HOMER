using UnityEngine;
using System.Collections;

public class PongPlayerController : MonoBehaviour
{
    public float speed = 10;

    static float topBound = 4.5F;
    static float bottomBound = -4.5F;
    public static float playSize;
    private float[] arom, prom,aprom;
    public PongGameController PGC;
    private bool side, mech;
    private float position;

    void Start()
    {
        playSize = Camera.main.orthographicSize;
        Time.timeScale = 0;
        topBound = playSize - this.transform.localScale.y / 4;
        // MovementTracker.Initialize(this, this.transform.position);
        bottomBound = -topBound;
        // Set current AROM and PROM.
        arom = AppData.Instance.selectedMechanism.CurrentArom;
        prom = AppData.Instance.selectedMechanism.CurrentProm;
        aprom = AppData.Instance.selectedMechanism.CurrentAProm;
        side = AppData.Instance.IsTrainingSide("RIGHT");
        mech = AppData.Instance.selectedMechanism.IsMechanism("HOC");
    }
    void FixedUpdate()
    {

        float position = AngleToScreen(PlutoComm.angle);
        this.transform.position = new Vector2(this.transform.position.x, position);
        // MovementTracker.UpdatePosition(this.transform.position);
    }


    public float AngleToScreen(float angle) => Mathf.Clamp(-playSize + (angle - aprom[0]) * (2 * playSize) / (aprom[1] - aprom[0]), bottomBound, topBound);


    private void OnCollisionEnter2D(Collision2D collision)
    {
        //PP.targetPosition = new Vector2(6f, Random.Range(-5f, 6f));
    }

}
