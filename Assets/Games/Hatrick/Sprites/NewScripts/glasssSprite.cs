using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class glasssSprite : MonoBehaviour
{
    [SerializeField] private Sprite suspense, happy;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ChangeSpriteToHappy()
    {
        GetComponent<SpriteRenderer>().sprite = happy;

    }
    public void ChangeSpriteToSuspense()
    {
        GetComponent<SpriteRenderer>().sprite = suspense;


    }
}
