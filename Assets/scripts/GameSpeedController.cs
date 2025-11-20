using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEditor;
using System.IO;
using TMPro;
using Unity.VisualScripting;


public class GameSpeedController : MonoBehaviour
{
    // TextMeshPro text
    public TextMeshProUGUI gameSpeedText, sessionDetailsText;
    public Button decreaseButton;
    public Button increaseButton;
    private float gameSpeed;

    void Start()
    {

    }

    void Update()
    {
        // Only if the game object is active
        if (gameObject.activeSelf)
        {
            // gameSpeed = AppData.Instance.speedData.gameSpeed;
            // gameSpeedText.text = $"{gameSpeed:F2}";
        }
    }
}