using UnityEngine;

public class WheelRotation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        float roatSpeed = Random.Range(-150, -100);
        transform.Rotate(0, 0, roatSpeed * Time.deltaTime); //rotates 50 degrees per second around z axis
    }
}
