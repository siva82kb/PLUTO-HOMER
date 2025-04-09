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
        INIT,
        ASSESS
    };
    private bool isButtonPressed = false;
    public TMP_Text lText;
    public TMP_Text rText;
    public TMP_Text insText;
    public TMP_Text cText;
    public TMP_Text relaxText;

    public TMP_Text jointAngle;
    public TMP_Text jointAngleHoc;

    private int _linx, _rinx;
    private float _tmin = 0f, _tmax  =0f;

    public GameObject nextButton;
    public GameObject startButton;
    public GameObject CurrPositioncursor;
    public GameObject CurrPositioncursorHoc;

    private AssessStates _state;

    private float angLimit;
    public DoubleSlider promSlider;
    public DoubleSlider promSliderHOC;
    public bool isSelected = false;

    public assessmentSceneHandler panelControl;

    private List<string[]> DirectionText = new List<string[]>
     {
         new string[] { "Flexion", "Extension" },
         new string[] { "Ulnar Dev", "Radial Dev" },
         new string[] { "Pronation", "Supination" },
         new string[] { "Open", "Open"},
         new string[] { "", "" },
         new string[] { "", "" }
     };

    void Start()
    {
        // Initialize the assessment data.
        //AppData.assessData = new AssessmentData(AppData.selectedMechanism, AppData.trainingSide);
        AppLogger.LogInfo(
            $"ROM data loaded for mechanism {AppData.Instance.selectedMechanism.name}: "
            + $"AROM [{AppData.Instance.selectedMechanism.oldRom.aromMin}, "
            + $"{AppData.Instance.selectedMechanism.oldRom.aromMax}], "
            + $"PROM [{AppData.Instance.selectedMechanism.oldRom.promMin}, " 
            + $"  {AppData.Instance.selectedMechanism.oldRom.promMax}]"
        );
        InitializeAssessment();
    }

    public void InitializeAssessment()
    {
        // Disable control.
        PlutoComm.setControlType("NONE");

        // Disble the button to move to the next assessment.
        nextButton.SetActive(false);

        // Update the min and max values.
        angLimit = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? PlutoComm.CALIBANGLE[PlutoComm.mechanism] : PlutoComm.MECHOFFSETVALUE[PlutoComm.mechanism];
        promSlider.Setup(-angLimit, angLimit, AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax);
        promSlider.minAng = 0;
        promSlider.maxAng = 0;

        // Update central text.
        cText.gameObject.SetActive(AppData.Instance.selectedMechanism.IsMechanism("HOC"));
        cText.text = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? "Closed" : "";

        // Update the left and right text.
        (_rinx, _linx) = AppData.Instance.IsTrainingSide("RIGHT") ? (1, 0) : (0, 1);
        rText.text = DirectionText[PlutoComm.mechanism - 1][_rinx];
        lText.text = DirectionText[PlutoComm.mechanism - 1][_linx];

        // Set the state to INIT.
        _state = AssessStates.INIT;
        // Attach callback for PLUTO button release.
        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;

        UpdateStatusText();
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
        jointAngle.text = ((int)PlutoComm.angle).ToString();
        jointAngleHoc.text = ((int)PlutoComm.getHOCDisplay(PlutoComm.angle)).ToString();

        if (isSelected)
        {
            runaAssessmentStateMachine();
            UpdateStatusText();
        }
        else
        {
            if (AppData.Instance.selectedMechanism.IsMechanism("HOC"))
            {
                float currentMinCM = ConvertToCM(promSlider.minAng);
                float currentMaxCM = ConvertToCM(promSlider.maxAng);
                relaxText.text = "Assessment Completed \n"
                                 + FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax) 
                                 + "Current PROM: " + currentMinCM.ToString("0.0") + "cm : " 
                                 + currentMaxCM.ToString("0.0") + "cm (Aperture: "
                                 + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
            }
            else
            {
                relaxText.text = "Assessment Completed \n"
                                 + FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax)
                                 + "|| " + "Current PROM: " + (int)promSlider.minAng + " : "
                                 + (int)promSlider.maxAng + " (" + (int)(promSlider.maxAng - promSlider.minAng) + "°)\n";
            }
        }
    }

    void runaAssessmentStateMachine()
    {
        CurrPositioncursor.SetActive(true);
        CurrPositioncursorHoc.SetActive(AppData.Instance.selectedMechanism.IsMechanism("HOC"));
        switch (_state)
        {
            case AssessStates.INIT:
                startButton.SetActive(true);

                if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                {
                    startAssessment();
                    isButtonPressed = false;
                }
                relaxText.text = FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax);
                break;
            case AssessStates.ASSESS:
                startButton.SetActive(false);
                _tmin = promSlider.minAng;
                _tmax = promSlider.maxAng;
                relaxText.text = FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax);
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
    }

    public void OnRedoPromClick()
    {
        _state = AssessStates.INIT;
        isButtonPressed = false;

        // Reinitialize the assessment process
        InitializeAssessment();

        UpdateStatusText();
        panelControl.SelectpROM();
        AppData.Instance.selectedMechanism.ResetAromValues();
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
        // Update new PROM
        AppData.Instance.selectedMechanism.SetNewPromValues(promSlider.minAng, promSlider.maxAng);
        nextButton.SetActive(false);
        promSlider.UpdateMinMaxvalues = false;
        CurrPositioncursor.SetActive(false);
        CurrPositioncursorHoc.SetActive(false);
    }

    private string FormatRelaxText(float min, float max)
    {
        return AppData.Instance.selectedMechanism.IsMechanism("HOC") ?
            $"Prev PROM: {ConvertToCM(min).ToString("0.0")}cm : {ConvertToCM(max).ToString("0.0")}cm (Aperture: {ConvertToCM(max - min).ToString("0.0")}cm)" :
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

    private void UpdateStatusText()
    {
        if (AppData.Instance.selectedMechanism.IsMechanism("HOC") == false)
        {
            jointAngle.text = (PlutoComm.angle).ToString("0.0");
        }
        else
        {
            jointAngle.text = "Aperture" + ConvertToCM(PlutoComm.angle).ToString("0.0") + "cm";
            jointAngleHoc.text = "Aperture" + ConvertToCM(PlutoComm.angle).ToString("0.0") + "cm";
        }
    }
}
