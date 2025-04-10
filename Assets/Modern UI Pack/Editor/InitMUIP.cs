﻿using UnityEngine;
using UnityEditor;

public class InitMUIP : MonoBehaviour
{
    [InitializeOnLoad]
    public class InitOnLoad
    {
        static InitOnLoad()
        {
            if (!EditorPrefs.HasKey("MUIPv3.Installed"))
            {
                EditorPrefs.SetInt("MUIPv3.Installed", 1);
                EditorUtility.DisplayDialog("Hello there!", "Thank you for purchasing Modern UI Pack.\r\rFirst of all, import TextMesh Pro from Package Manager if you haven't already." +
                    "\r\rTo change UI element values, go to Modern UI Pack > Resources > MUIP Manager.\r\rYou can contact me at isa.steam@outlook.com for support.", "Got it!");
            }
        }
    }
}