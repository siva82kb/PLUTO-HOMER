using System;
using UnityEngine;

public class Slidermech : MonoBehaviour
{

    public GameObject SingleSlider;
    void Start()
    {
        if(Array.IndexOf(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism) !=3)
        {
            SingleSlider.SetActive(true);
        }
        else {
            SingleSlider.SetActive(false);
        }
    }

    void Update()
    {
        
    }
}
