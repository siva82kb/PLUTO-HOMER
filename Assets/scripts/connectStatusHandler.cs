using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class connectStatusHandler : MonoBehaviour
{
    private Image connectStatus;
    private GameObject loading;
    private TextMeshProUGUI statusText;

    // Start is called before the first frame update
    void Start()
    {
        connectStatus = GetComponent<Image>(); // Uncomment if connectStatus is on the same GameObject
        loading = transform.Find("loading").gameObject; // Assuming loading is a child GameObject
        statusText = transform.Find("statusText").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        // Update connection status
        if (ConnectToRobot.isPLUTO)
        {
            connectStatus.color = Color.green;
            loading.SetActive(false);
            statusText.text = $"{PlutoComm.version}\n[{PlutoComm.frameRate:F1}Hz]";
        }
        else
        {
            connectStatus.color = Color.red;
            loading.SetActive(true);
            statusText.text = "Not connected";
        }
    }
}
