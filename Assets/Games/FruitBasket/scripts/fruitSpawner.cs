using UnityEngine;
using System.Collections;
using System;

public class FruitSpawner : MonoBehaviour
{
    public GameObject[] fruitPrefebs;
    public GameObject[] targetPrefebs;
    private GameObject fruit;
    private FruitController controller;
    public static FruitSpawner instance;
    private Vector3 prevPosition = Vector3.zero;
    private int lastFruitIndex;
    private Vector3 targetScale = Vector3.one * 2.2f;
    private float scaleDuration = 0.5f; // time to scale up
    public void setPrePosition(Vector3 pos)
    {
        prevPosition = pos;
    }
    private void Awake()
    {
        // Ensure only one instance exists
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
       
    }
    public void spawnFruit(GameObject targetBasket)
    {

        if (fruit != null)
        {
            return;
        }
        int index;
    
        RectTransform canvasRect = FruitBasketGameController.Instance.mainCanvas.GetComponent<RectTransform>();
      
        float x = (prevPosition.x != 0f)
            ? prevPosition.x
            : FruitBasketGameController.Instance.AngleToScreen(PlutoComm.angle);//Random.Range(-(canvasRect.rect.width / 2f), (canvasRect.rect.width / 2f));
       
        float y = (canvasRect.rect.height / 2f) - 20; // Top of the screen
       

        index = Array.FindIndex(targetPrefebs, prefeb => prefeb.name == targetBasket.name);

        fruit = Instantiate(fruitPrefebs[index], FruitBasketGameController.Instance.mainCanvas.transform);
        StartCoroutine(ScaleUp());
        fruit.transform.localPosition = new Vector3(x, y, 0f);
       
        controller = fruit.AddComponent<FruitController>();
        controller.target = targetBasket;
        controller.fruit = fruit;
   

    }
    public void clearCurrentFruit()
    {
        fruit = null;  // Now spawnFruit() can create a new fruit
    }
    public void clearController()
    {
        if (fruit != null)
        {
            FruitController controller = fruit.GetComponent<FruitController>();
            if (controller != null)
            {
                Destroy(controller);  // Remove the FruitController script
            }
        }
    }
    IEnumerator ScaleUp()
    {
        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            float t = elapsed / scaleDuration;
            fruit.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale; // ensure final scale

    }


}
