using UnityEngine;

public class FlappyScrollingObject : MonoBehaviour
{
    private Rigidbody2D rb2d;
    // Start is called before the first frame update
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.velocity = new Vector2(FlappyGameControl.instance.scrollSpeed, 0);

    }

    // Update is called once per frame
    void Update()
    {
        rb2d.velocity = new Vector2(FlappyGameControl.instance.scrollSpeed, 0);

        if (FlappyGameControl.instance.gameOver)
        {
            rb2d.velocity = Vector2.zero;
        }

    }
}
