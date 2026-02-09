using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
public class LaunchDemo : MonoBehaviour
{
   

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) &&
            Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.D)) // magic key combo
        {
            LaunchNewApp();
        }
    }

    void LaunchNewApp()
    {
        string appPath = @"C:\HOMER_DEMO_PLUTO\PLUTO.exe";

        try
        {
            Process.Start(appPath);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to start app: " + e.Message);
        }

        Application.Quit(); // close current Unity instance
    }


}
