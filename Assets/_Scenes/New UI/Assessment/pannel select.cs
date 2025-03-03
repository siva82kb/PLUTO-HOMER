
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
    public TMP_Text Ins;
    public GameObject[] aromSelected; 
    public GameObject[] promSelected;
    private string mech;
    static int steps = 10;
    public static float[] assistProfile = new float[steps];


    void Start()
    {
        //AppData.initializeStuff();

        SelectpROM();
        mech = AppData.selectedMechanism;
        //UpdateAssistProfile();
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
            SceneManager.LoadScene("choosegame");
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
            Ins.text = "set mechanism to zero position and press  'K'  to set zero";

        }
        promImage.color = new Color(0f / 255f, 55f / 255f, 52f / 255f);
        aromImage.color = new Color(220f / 255f, 83f / 255f, 87f / 255f, 1f);
        promHandler.isSelected = true;
        aromHandler.isSelected = false;
        SetActiveStatus(aromSelected, false);
        SetActiveStatus(promSelected, true);


    }

    public void SelectAROM()
    {
        promImage.color = new Color(220f / 255f, 83f / 255f, 87f / 255f, 1f);

        aromImage.color =  new Color(0f / 255f, 55f / 255f, 52f / 255f);

        promHandler.isSelected = false;
        aromHandler.isSelected = true;

        SetActiveStatus(aromSelected, true);
        SetActiveStatus(promSelected, true);

        AppData.newPROM = new ROM(AppData.selectedMechanism);

        float newPROM_tmin = AppData.promTmin;
        float newPROM_tmax = AppData.promTmax;
        updateAROM();

    }

    public void updateAROM()
    {
        if (aromHandler.isSelected == true)
        {
            AppData.newPROM = new ROM(AppData.selectedMechanism);

            float newPROM_tmin = AppData.promTmin;
            float newPROM_tmax = AppData.promTmax;
        }
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
        SceneManager.LoadScene("chooseMechanism");
    }
    public void gameScene()
    {
        SceneManager.LoadScene("choosegame");
    }

}
