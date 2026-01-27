using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class FruitController : MonoBehaviour
{
    public float fallSpeed; // Falling speed
    public GameObject target;
    private RectTransform canvasRect;
    public GameObject fruit;
    public Vector3 prevPosition;
    
    
    void Start()
    {
        fallSpeed = FruitBasketGameController.Instance.FRUITSPEED;
        canvasRect = FruitBasketGameController.Instance.mainCanvas.GetComponent<RectTransform>();

        gameObject.tag = "Player";
        target.tag = "Target";
    }
   

    void Update()
    {

       Vector3 pos = transform.localPosition;

       float targetx = FruitBasketGameController.Instance.AngleToScreen(PlutoComm.angle);

        pos.x = Mathf.Lerp(pos.x, targetx, 6f * Time.deltaTime);
        pos.y = pos.y - fallSpeed * Time.deltaTime;
        transform.localPosition = pos;

        if (pos.y < -(canvasRect.rect.height / 2f) + 20)
        {
            FruitSpawner.instance.setPrePosition(transform.localPosition);
            FailFruit();
            FruitSpawner.instance.clearController();
            FruitSpawner.instance.clearCurrentFruit();
        }
        
         

    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name == target.name || fruit.gameObject.name == collision.gameObject.name)
        {
           
            FruitSpawner.instance.setPrePosition(transform.localPosition);
            CatchFruit();
            FruitSpawner.instance.clearController();
            FruitSpawner.instance.clearCurrentFruit();
            return;
        }
        FailFruit();
        
    }
    void CatchFruit()
    {
        FruitBasketGameController.Instance.PlaySound(0);
        FruitBasketGameController.Instance.setSuccess();
        transform.SetParent(target.transform); // Re-parent fruit to basket
        transform.localPosition = transform.localPosition;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true;
        }
       
        transform.rotation = Quaternion.identity; // Reset rotation
        // Optionally resize
        transform.localScale = Vector3.one * 0.8f;

        //reset the target
        gameObject.tag = "Untagged";
        target.tag = "Untagged";
    }
    void FailFruit()
    {
       
        FruitBasketGameController.Instance.PlaySound(1);
        FruitBasketGameController.Instance.setFailure();
        gardener.instance.AddFallenFruit(gameObject);
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true;
        }
        transform.localScale = Vector3.one * 0.5f;

        //reset the target
        gameObject.tag = "Untagged";
        target.tag = "Untagged";
    }
}
