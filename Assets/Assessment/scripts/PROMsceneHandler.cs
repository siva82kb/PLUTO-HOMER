using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TS.DoubleSlider;
using System.IO;


public class PROMsceneHandler : MonoBehaviour
{

    enum AssessStates
    {
        INIT = 0,
        ASSESS = 1
    };
    bool assessmentSaved;
    private bool isPaused = false;
    private bool isButtonPressed = false;
    public TMP_Text lText;
    public TMP_Text rText;
    public TMP_Text insText;
    public TMP_Text cText;
    //public TMP_Text statusText;
    public TMP_Text relaxText;

    public TMP_Text JointAngle;

    public TMP_Text JointAngleHoc;

    bool AssessmentValid;

    private float _tmin, _tmax;

    public GameObject nextButton;
    public GameObject startButton;
    public GameObject CurrPositioncursor;
    public GameObject CurrPositioncursorHoc;
    private AssessStates _state;

    private float angLimit;
    public DoubleSlider promSlider;

    public DoubleSlider promSliderHOC;

    //public DoubleSlider promSlider1;

    public bool isSelected = false;
    public bool inst = false;
    public assessmentSceneHandler panelControl;


    private List<string[]> DirectionText = new List<string[]>
     {
         new string[] { "Flexion", "Extension" },
         new string[] { "Ulnar Dev.", "Radial Dev."},
         new string[] { "Pronation", "Supination" },
         new string[]{"Open", "Open"},
         new string[] {"",""},
         new string[] {"",""}
     };

    private int _linx, _rinx;
    internal bool interactable;

    void Start()
    {
        InitializeAssessment();

    }

    public void InitializeAssessment()
    {
        nextButton.SetActive(false);
        AppData.oldPROM = new ROM(AppData.selectedMechanism);

        int mechanismIndex = Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism);
        angLimit = (mechanismIndex == 4) ? 100.42f : AppData.offsetAtNeutral[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.selectedMechanism)];
        promSlider.Setup(-angLimit, angLimit, AppData.oldPROM.promTmin, AppData.oldPROM.promTmax);

        cText.gameObject.SetActive(mechanismIndex == 4);
        cText.text = (mechanismIndex == 4) ? "Closed" : "";

        (_rinx, _linx) = AppData.trainingSide == "right" ? (1, 0) : (0, 1);
        rText.text = DirectionText[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.selectedMechanism)][_rinx];
        lText.text = DirectionText[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.selectedMechanism)][_linx];

        _tmin = 180f;
        _tmax = -180f;
      

        _state = AssessStates.INIT;

        UpdateGUI();
        PlutoComm.setControlType("NONE");
    }


    private void DisablePromGameObjects()
    {
        startButton.SetActive(false);
        nextButton.SetActive(false);
    }

    public void OnStartButtonClick()
    {
        startAssessment();
        startButton.SetActive(false);
        nextButton.SetActive(true);
    }


    void Update()
    {


        JointAngle.text = ((int)PlutoComm.angle).ToString();
        JointAngleHoc.text = ((int)PlutoComm.getHOCDisplay(PlutoComm.angle)).ToString();

        if (isSelected)
        {
            CurrPositioncursor.SetActive(true);
            CurrPositioncursorHoc.SetActive(Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4);




            switch (_state)
            {

                case AssessStates.INIT:

                    startButton.SetActive(true);
                    PlutoComm.OnButtonReleased += OnPlutoButtonReleased;


                    if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                    {

                        startAssessment();
                        isButtonPressed = false;
                    }
                    relaxText.text = FormatRelaxText(AppData.oldPROM.promTmin, AppData.oldPROM.promTmax);

                    break;
                case AssessStates.ASSESS:

                    startButton.SetActive(false);
                    _tmin = promSlider.minAng;
                    _tmax = promSlider.maxAng;
                    relaxText.text = FormatRelaxText(AppData.oldPROM.promTmin, AppData.oldPROM.promTmax);
                    nextButton.SetActive(true);

                    if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                    {
                        OnNextButtonClick();
                        nextButton.SetActive(false);
                        DisablePromGameObjects();
                        isButtonPressed = false;
                    }



                    break;

            }
            UpdateGUI();
        }
        else
        {
            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
            {
                float currentMinCM = ConvertToCM(promSlider.minAng);
                float currentMaxCM = ConvertToCM(promSlider.maxAng);

                relaxText.text = "Assessment Completed \n " + FormatRelaxText(AppData.oldPROM.promTmin, AppData.oldPROM.promTmax) +
                                    "Current PROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0") + "cm (Aperture: "
                                    + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
            }
            else
            {
                relaxText.text = "Assessment Completed \n " + FormatRelaxText(AppData.oldPROM.promTmin, AppData.oldPROM.promTmax) + "|| " + "Current PROM: " + (int)promSlider.minAng + " : "
                        + (int)promSlider.maxAng + " (" + (int)(promSlider.maxAng - promSlider.minAng) + "°)\n";
            }

        }
    }


    public void OnRedoPromClick()
    {
        _state = AssessStates.INIT;
        AssessmentValid = false;
        isButtonPressed = false;
        gameData.isAROMcompleted = false;
        // Reinitialize the assessment process
        InitializeAssessment();

        UpdateGUI();
        panelControl.SelectpROM();
        Debug.Log("Redo PROM: Reset to INIT state.");
    }

    public void OnPlutoButtonReleased()
    {
        isButtonPressed = true;

    }

    private float ConvertToCM(float value) => Mathf.Abs(Mathf.Deg2Rad * value * 6f);

    public void OnNextButtonClick()
    {
        OnSaveClick();
        panelControl.SelectAROM();
        DisablePromGameObjects();

    }
    public void OnrestartButtonClick()
    {
        Start();
    }


    public void OnSaveClick()
    {
        nextButton.SetActive(false);
        _tmin = promSlider.minAng;
        _tmax = promSlider.maxAng;
        assessmentSaved = true;
        Debug.Log("Onsave : " + _tmin + " , " + _tmax);
        gameData.isPROMcompleted = true;
        AppData.promTmin = _tmin;
        AppData.promTmax = _tmax;

        promSlider.UpdateMinMaxvalues = false;
        CurrPositioncursor.SetActive(false);
        CurrPositioncursorHoc.SetActive(false);


        promSlider.minAng = AppData.promTmin;

        nextButton.SetActive(false);

        if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
        {
            float currentMinCM = Mathf.Abs(Mathf.Deg2Rad * _tmin * 6f);
            float currentMaxCM = Mathf.Abs(Mathf.Deg2Rad * -_tmax * 6f);

            relaxText.text = "Assessment Completed \n" +
                            "Current PROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0")
                            + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
        }
        else
        {
            relaxText.text = "Assessment Completed \n " +
            "Current PROM: " + (int)_tmin + " : " + (int)_tmax + " (" + (int)(_tmax - _tmin) + " °)\n";
        }
    }


    private string FormatRelaxText(float min, float max)
    {
        return Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4 ?
            $"Prev Prom: {ConvertToCM(min)}cm : {ConvertToCM(max)}cm (Aperture: {ConvertToCM(max - min)}cm)" :
            $"Prev PROM: {(int)min} : {(int)max} ({(int)(max - min)}°)";
    }

    public void startAssessment()
    {
        _state = AssessStates.ASSESS;
        promSlider.minAng = 0;
        promSlider.maxAng = 0;

        promSlider.startAssessment(PlutoComm.angle);
        promSlider.UpdateMinMaxvalues = true;

    }

    void OnApplicationQuit()
    {
        JediComm.Disconnect();
    }


    private void UpdateGUI()
    {
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) != 4)
        {
            JointAngle.text = (PlutoComm.angle).ToString("0.0");
        }
        else
        {
            JointAngle.text = "Aperture" + ConvertToCM(PlutoComm.angle).ToString("0.0") + "cm";

            JointAngleHoc.text = "Aperture" + ConvertToCM(PlutoComm.angle).ToString("0.0") + "cm";
        }

    }


}
