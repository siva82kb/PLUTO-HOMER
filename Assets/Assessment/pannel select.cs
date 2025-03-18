
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
    public Image PromImagedisabled, aromImageDisabled;
    public TMP_Text Ins;
    public GameObject[] aromSelected; 
    public GameObject[] promSelected;

    private string mech;
    private string mechScene = "chooseMechanism";
    private string chooseGameScene = "choosegame";

  
    void Start()
    {
        SelectpROM();
        mech = AppData.selectedMechanism;
        mechName.text = PlutoComm.MECHANISMSTEXT[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, mech)];
      

    }
    void Update()
    {
        PlutoComm.sendHeartbeat();
    }

    

    public void writeAssesmentFileAndExit()
    {
        gameData.setNeutral = true;
        if (gameData.isPROMcompleted && gameData.isAROMcompleted)
        {
            SceneManager.LoadScene(chooseGameScene);
            Debug.Log("Wrote successfully");
        }
        else
        {
            Debug.Log("APROM not completed");
        }
    }
    public void SelectpROM()
    {
        promButton.Select();
        if(AppData.selectedMechanism != "HOC")
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
        PromImagedisabled.gameObject.SetActive(false);   
    }

    public void SelectAROM()
    {
        promImage.color = new Color(220f / 255f, 83f / 255f, 87f / 255f, 1f);

        aromImage.color =  new Color(0f / 255f, 55f / 255f, 52f / 255f);

        promHandler.isSelected = false;
        aromHandler.isSelected = true;

        SetActiveStatus(aromSelected, true);
        SetActiveStatus(promSelected, true);
        aromImageDisabled.gameObject.SetActive(false);
        PromImagedisabled.gameObject.SetActive(true);

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
