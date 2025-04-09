using UnityEngine;

public class glassLine : MonoBehaviour
{
    public int hitCount;
    AudioSource ads;
    // Start is called before the first frame update
    void Start()
    {
        ads = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnTriggerEnter2D(Collider2D collision)
    {

        Debug.Log(collision.gameObject.tag);

        if (collision.gameObject.tag == "water")
        {
            //Debug.Log("her");
            collision.tag = "waterInGlass";
            collision.GetComponent<Rigidbody2D>().gravityScale = 0.3f;
            collision.GetComponent<Rigidbody2D>().velocity = collision.GetComponent<Rigidbody2D>().velocity / 10;
            hitCount++;

            if (hitCount > 40)
            {
                //make happy point scored
                GetComponentInParent<glasssSprite>().ChangeSpriteToHappy();
            }
            else
            {
                //sad
                GetComponentInParent<glasssSprite>().ChangeSpriteToSuspense();

            }
        }
    }
}
