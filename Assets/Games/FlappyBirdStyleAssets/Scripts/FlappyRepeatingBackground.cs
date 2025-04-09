using UnityEngine;

public class FlappyRepeatingBackground : MonoBehaviour
{
    private BoxCollider2D groundCollider;
    float groundHorizontalLength;
    // Start is called before the first frame update
    void Start()
    {
        groundCollider = GetComponent<BoxCollider2D>();
        groundHorizontalLength = groundCollider.size.x;
    }

    // Update is called once per frame
    void Update()
    {
        //   Debug.Log(transform.position.x);
        if (transform.position.x < -groundHorizontalLength)
        {
            RepositionBackgound();
        }

    }
    private void RepositionBackgound()
    {
        Vector2 groundoffset = new Vector2(groundHorizontalLength * 1.98f, 0);
        transform.position = (Vector2)transform.position + groundoffset;
    }
}
