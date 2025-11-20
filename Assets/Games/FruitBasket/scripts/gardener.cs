using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class gardener : MonoBehaviour
{
    public List<GameObject> fallenFruits = new List<GameObject>();
    public RectTransform gardenerprefeb;
    //public RectTransform basket;
    public static gardener instance;
    public float gardenerSpeed = 500f;
    public bool IsGardenerCollecting { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
      instance = this;
      //initally hide the gardner
      gameObject.SetActive(!gardenerprefeb.gameObject.activeSelf);
        
    }

    // Update is called once per frame
    void Update()
    {
        
        
    }
    public void AddFallenFruit(GameObject fruit)
    {
        if (!fallenFruits.Contains(fruit))
            fallenFruits.Add(fruit);
    }

    public void StartGardenerCollecting()
    {
        if (!IsGardenerCollecting && fallenFruits.Count > 0)
            StartCoroutine(GardenerCollectRoutine());
    }
    IEnumerator GardenerCollectRoutine()
    {
        IsGardenerCollecting = true;
       
        // Save gardener's initial position
        Vector3 startPosition = gardenerprefeb.localPosition;
     

        foreach (GameObject fruit in fallenFruits)
        {
            if (fruit == null) continue;

            // Move gardener to fruit
            while (Vector3.Distance(gardenerprefeb.localPosition, fruit.transform.localPosition) > 10f)
            {
                gardenerprefeb.localPosition = Vector3.MoveTowards(gardenerprefeb.localPosition, fruit.transform.localPosition, gardenerSpeed * Time.deltaTime);
                yield return null;
            }

            // Pick up fruit
            fruit.transform.SetParent(gardenerprefeb);
            fruit.transform.localScale = Vector3.one * 0.3f;
        }

        // Return gardener to original start position
        while (Vector3.Distance(gardenerprefeb.localPosition, startPosition) > 10f)
        {
            gardenerprefeb.localPosition = Vector3.MoveTowards(gardenerprefeb.localPosition, startPosition, gardenerSpeed * Time.deltaTime);
            yield return null;
        }

        // Destroy fruits or keep them with gardener
        foreach (GameObject fruit in fallenFruits)
        {
            if (fruit != null) Destroy(fruit);
        }

        fallenFruits.Clear();
        gardenerprefeb.gameObject.SetActive(true); // stays visible in start position
        IsGardenerCollecting = false;
    }
}
