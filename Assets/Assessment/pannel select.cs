
using PlutoNeuroRehabLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class assessmentSceneHandler : MonoBehaviour
{

    public Button promButton;

    public Button RedoPRom;
    public Button aromButton;
    public TextMeshProUGUI mechName;
    
    public PROMsceneHandler promHandler;
    public AROMsceneHandler aromHandler;
    public Image promImage;
    public Image aromImage;
    public Image promImagedisabled, aromImageDisabled;
    public TMP_Text Ins;
    public GameObject[] aromSelected; 
    public GameObject[] promSelected;

    private string mech;
    private string mechScene = "CHMECH";
    private string chooseGameScene = "CHGAME";

    void Start()
    {
        // Check if userData is null.
        if (AppData.Instance.userData == null)
        {
            // Initialize.
            AppData.Instance.Initialize("Assessment");
        }
        AppLogger.SetCurrentScene(SceneManager.GetActiveScene().name);
        AppLogger.LogInfo($"'{SceneManager.GetActiveScene().name}' scene started.");
        
        // Select PROM first.
        SelectpROM();

        // Set mechanism name
        mechName.text = PlutoComm.MECHANISMSTEXT[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name)];
    }

    void Update()
    {
        PlutoComm.sendHeartbeat();
    }

    public void SelectpROM()
    {
        promButton.Select();
        if(AppData.Instance.selectedMechanism.IsMechanism("HOC"))
        {
           // Ins.text = "set mechanism to zero position and press  'K'  to set zero";
        }
        promImage.color = new Color(0f / 255f, 55f / 255f, 52f / 255f);
        aromImage.color = new Color(220f / 255f, 83f / 255f, 87f / 255f, 1f);
        promHandler.isSelected = true;
        aromHandler.isSelected = false;
        SetActiveStatus(aromSelected, false);
        SetActiveStatus(promSelected, true);
        aromImageDisabled.gameObject.SetActive(true);
        promImagedisabled.gameObject.SetActive(false);   
    }

    public void SelectAROM()
    {
        promImage.color = new Color(220f / 255f, 83f / 255f, 87f / 255f, 1f);
        aromImage.color =  new Color(0f / 255f, 55f / 255f, 52f / 255f);
        promHandler.isSelected = false;
        aromHandler.isSelected = true;
        aromHandler.isButtonPressed = false;
        SetActiveStatus(aromSelected, true);
        SetActiveStatus(promSelected, true);
        aromImageDisabled.gameObject.SetActive(false);
        promImagedisabled.gameObject.SetActive(true);
    }

    private void SetActiveStatus(GameObject[] objects, bool status)
    {
        foreach (GameObject obj in objects)
        {
            obj.SetActive(status);
        }
    }

    public void chanceMech()
    {
        SceneManager.LoadScene(mechScene);
    }

    public void gameScene()
    {
        SceneManager.LoadScene(chooseGameScene);
    }
}
