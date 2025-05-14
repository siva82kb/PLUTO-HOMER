using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseSuperStar : MonoBehaviour
{
    // Start is called before the first frame update

    public SpriteRenderer superstar;
    public Sprite[] starsImages;
    public int index;
    void Start()
    {
        index = Random.Range(0,starsImages.Length);
    }

    // Update is called once per frame
    void Update()
    {
        superstar.sprite = starsImages[index];
    }

}
