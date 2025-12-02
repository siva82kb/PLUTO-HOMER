using UnityEngine;

public class CloudController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public SpriteRenderer cloudRenderer;   
    public Sprite[] emotionSprites;        
    public float emotionChangeInterval = 12f, PLAYSIZE; 
    private float emotionTimer = 0f, position;
    private float[] aprom;
    public ParticleSystem rainEffect;
    public Collider2D rainAreaCollider;
    private bool isRaining = false;
    private float cloudHalfWidth;

    void Start()
    {
        ChangeEmotion();
        aprom = AppData.Instance.selectedMechanism.CurrentAProm;
        PLAYSIZE = Camera.main.orthographicSize * Camera.main.aspect;

        // Calculate the cloud's half width based on its sprite bounds
        if (cloudRenderer != null)
        {
            cloudHalfWidth = cloudRenderer.bounds.extents.x;
        }
        else
        {
            // Fallback if no renderer found
            cloudHalfWidth = 0.5f;
        }

        // Optional auto-find if you forgot to assign:
        if (rainAreaCollider == null)
        {
            var child = transform.Find("RainArea");
            if (child != null) rainAreaCollider = child.GetComponent<Collider2D>();
        }

        if (rainAreaCollider != null) rainAreaCollider.enabled = false; // start off
    }

    void Update()
    {
        position = AngleToScreen(PlutoComm.angle);

        Vector2 pos = new Vector2(position, this.transform.position.y);
        
        // Clamp to camera view with cloud bounds consideration
        float cameraHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        
        // Calculate bounds that ensure the entire cloud stays within camera
        float leftBound = -cameraHalfWidth + cloudHalfWidth;
        float rightBound = cameraHalfWidth - cloudHalfWidth;
        
        // Additional safety check to prevent negative bounds if cloud is too large
        if (leftBound > rightBound)
        {
            // If cloud is larger than screen, center it
            pos.x = 0f;
        }
        else
        {
            pos.x = Mathf.Clamp(pos.x, leftBound, rightBound);
        }
        
        transform.position = pos;

        // Emotion change
        emotionTimer += Time.deltaTime;
        if (emotionTimer >= emotionChangeInterval)
        {
            ChangeEmotion();
            emotionTimer = 0f;
            
            // Recalculate cloud bounds if sprite changes size
            if (cloudRenderer != null)
            {
                cloudHalfWidth = cloudRenderer.bounds.extents.x;
            }
        }
    }

    public void ChangeEmotion()
    {
        if (emotionSprites.Length > 0)
        {
            int index = Random.Range(0, emotionSprites.Length);
            cloudRenderer.sprite = emotionSprites[index];

            // Update cloud bounds after sprite change
            cloudHalfWidth = cloudRenderer.bounds.extents.x;
        }
    }

    // public float AngleToScreen(float angle) => Mathf.Lerp(-PLAYSIZE, PLAYSIZE, (angle - aprom[0]) / (aprom[1] - aprom[0]));
    public float AngleToScreen(float angle) => Mathf.Lerp(-6.8f, 6.8f, (angle - aprom[0]) / (aprom[1] - aprom[0]));
    
}   