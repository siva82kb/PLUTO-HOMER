using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class connectStatusHandler : MonoBehaviour
{
    private Image connectStatus;
    private GameObject loading;
    private TextMeshProUGUI statusText;

    void Awake()
    {
        // Subscribe to shutdown events once per instance
        Application.quitting += CloseAppLogger; //for Exe file
        AppDomain.CurrentDomain.ProcessExit += (_, __) => CloseAppLogger(); // for external crash like OS Crash

        #if UNITY_EDITOR
                EditorApplication.quitting += CloseAppLogger; //for editor
        #endif
    }
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

     private void CloseAppLogger()
    {
        AppLogger.StopLogging(); 
        PlutoAanLogger.StopLogging();
        PlutoComLogger.StopLogging();
    }
}
