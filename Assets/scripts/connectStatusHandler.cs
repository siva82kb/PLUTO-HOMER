using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;

public class connectStatusHandler : MonoBehaviour
{
    private Image connectStatus;
    private GameObject loading;

    // Start is called before the first frame update
    void Start()
    {
        connectStatus = GetComponent<Image>(); // Uncomment if connectStatus is on the same GameObject
        loading = transform.Find("loading").gameObject; // Assuming loading is a child GameObject
    }

    // Update is called once per frame
    void Update()
    {
        // Update connection status
        if (ConnectToRobot.isPLUTO)
        {
            connectStatus.color = Color.green;
            loading.SetActive(false);
        } 
        else
        {
            connectStatus.color = Color.red;
            loading.SetActive(true);
        }
    }
}
