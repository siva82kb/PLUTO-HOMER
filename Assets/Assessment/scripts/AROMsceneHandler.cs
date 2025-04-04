using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TS.DoubleSlider;
using UnityEngine.UIElements;
using System.IO;


public class AROMsceneHandler : MonoBehaviour
{
    enum AssessStates
    {
        INIT ,
        ASSESS
    };
    public TMP_Text lText;
    public TMP_Text rText;
    public TMP_Text cText;
    public TMP_Text relaxText;
    public TMP_Text JointAngle;
    public TMP_Text JointAngleHoc;
    public TMP_Text warningText;

    private float _tmin = 0f, _tmax = 0f ,angLimit = 0f;
    private int _linx, _rinx;

    public GameObject nextButton;
    public GameObject startButton;
    public GameObject curreposition;
    public GameObject currepositionHoc;

    private AssessStates _state;

    
    public DoubleSlider aromSlider;
    public DoubleSlider promSlider;

    public bool isSelected = false;
    private bool isRestarting = false;
    public bool isButtonPressed = false;

    public assessmentSceneHandler panelControl;

    private List<string[]> DirectionText = new List<string[]>
     {
         new string[] { "Flexion", "Extension" },
         new string[] { "Ulnar Dev.", "Radial Dev."},
         new string[] { "Pronation", "Supination" },
         new string[]{ "Open", "Open"},
         new string[] {"",""},
         new string[] {"",""}
     };

    private string nextScene = "choosegame";
   
    void Start()
    {
    }

    private void InitializeAssessment()
    {
        // Set control to NONE.
        PlutoComm.setControlType("NONE");

        aromSlider.UpdateMinMaxvalues = false;
        nextButton.SetActive(false);
        
        //create rom file if not exists
        createFile();

        angLimit = AppData.selectedMechanism.IsMechanism("HOC") ? PlutoComm.CALIBANGLE[PlutoComm.mechanism] : PlutoComm.MECHOFFSETVALUE[PlutoComm.mechanism];
        aromSlider.Setup(-angLimit, angLimit, AppData.selectedMechanism.oldRom.aromMin, AppData.selectedMechanism.oldRom.aromMax);
        aromSlider.minAng = aromSlider.maxAng = 0;

        // Handle HOC and other mechanisms differently.
        cText.gameObject.SetActive(AppData.selectedMechanism.IsMechanism("HOC"));
        rText.gameObject.SetActive(true);
        lText.gameObject.SetActive(true);
        cText.text = AppData.selectedMechanism.IsMechanism("HOC")  ? "Closed" : "";

        // Handle the right and left sides differently.
        (_rinx, _linx) = AppData.trainingSide == "right" ? (1, 0) : (0, 1);
        rText.text = DirectionText[PlutoComm.mechanism - 1][_rinx];
        lText.text = DirectionText[PlutoComm.mechanism - 1][_linx];

        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
        // Set initial state.
        _state = AssessStates.INIT;

        UpdateStatusText();
    }

    public void OnStartButtonClick()
    {
        startAssessment();
        startButton.SetActive(false);
        nextButton.SetActive(true);
    }

    private void RestartAssessment()
    {
        InitializeAssessment();
    }
    public void OnPlutoButtonReleased()
    {

        isButtonPressed = true;

    }
    void Update()
    {

        if (isSelected)
        {
            runaAssessmentStateMachine();
            UpdateStatusText();
        }
        else
        {
            _state = AssessStates.INIT;
            isRestarting = false;
            relaxText.color = Color.white;
        }
      
    }
    void runaAssessmentStateMachine()
    {
        switch (_state)
        {
            case AssessStates.INIT:
                startButton.SetActive(true);
                InitializeAssessment();
                if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                {
                    startAssessment();
                    isButtonPressed = false;
                }
                if (isRestarting)
                {
                    relaxText.color = Color.red;
                    relaxText.text = " AROM Should not Exceed PROM \n " +
                                     "Please REDO PROM AGAIN";
                }
                else relaxText.text = FormatRelaxText(AppData.selectedMechanism.oldRom.aromMin, AppData.selectedMechanism.oldRom.aromMax);
                break;
            case AssessStates.ASSESS:
                startButton.SetActive(false);
                _tmin = aromSlider.minAng;
                _tmax = aromSlider.maxAng;
                relaxText.color= Color.white;
                relaxText.text = FormatRelaxText(AppData.selectedMechanism.oldRom.aromMin, AppData.selectedMechanism    .oldRom.aromMax);
                nextButton.SetActive(true);
                if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                {
                    OnNextButtonClick();
                    isButtonPressed = false;
                }
                checkAromLimits();
                break;
        }
    }
    private void checkAromLimits()
    {
        if (aromSlider._currePostion.value <= AppData.promMin || aromSlider._currePostion.value >= AppData.promMax)
        {

            aromSlider.UpdateMinMaxvalues = false;
            RestartAssessment();
            isButtonPressed = false;
            isRestarting = true;
            curreposition.SetActive(true);
            currepositionHoc.SetActive(AppData.selectedMechanism.IsMechanism("HOC"));
        }
        else
        {
            aromSlider.UpdateMinMaxvalues = true;
            curreposition.SetActive(true);
            currepositionHoc.SetActive(AppData.selectedMechanism.IsMechanism("HOC"));

        }

    }

    public void OnRedoaromButtonClick()
    {

        InitializeAssessment();
        Debug.Log("Assessment Restarted");
        aromSlider.UpdateMinMaxvalues = false;
    }

    public void OnNextButtonClick()
    {
        checkAromLimits();
        onSavePressed();
        nextButton.SetActive(false);
        aromSlider.UpdateMinMaxvalues = false;
    }

    public void startAssessment()
    {
        _state = AssessStates.ASSESS;
        nextButton.SetActive(false);
        startButton.SetActive(false);
        aromSlider.startAssessment(PlutoComm.angle);
        aromSlider.UpdateMinMaxvalues = true;
    }

    public void onSavePressed()
    {
        _tmin = aromSlider.minAng;
        _tmax = aromSlider.maxAng;

        AppData.selectedMechanism.SetNewAromValues(_tmin,_tmax);

        AppData.selectedMechanism.SaveAssessmentData();

        if (AppData.selectedMechanism.IsMechanism("HOC"))
        {
            float currentMinCM = ConvertToCM(_tmin);
            float currentMaxCM = ConvertToCM(_tmax);

            relaxText.text = " Assessment Completed \n" + FormatRelaxText(AppData.selectedMechanism.oldRom.aromMin, AppData.selectedMechanism.oldRom.aromMax)
                             + "Current AROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0")
                             + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
        }
        else
        {
            relaxText.text = " Assessment Completed \n "
                             + FormatRelaxText(AppData.selectedMechanism.oldRom.aromMin, AppData.selectedMechanism.oldRom.aromMax)
                             + "Current AROM: " + (int)_tmin + " : " + (int)_tmax + " (" + (int)(_tmax - _tmin) + " °)\n";
        }

        nextButton.SetActive(false);
        aromSlider.UpdateMinMaxvalues = false;

        if (AppData.selectedMechanism.promCompleted && AppData.selectedMechanism.aromCompleted) SceneManager.LoadScene(nextScene);
        else Debug.Log("Complete your PROM and AROM");

    }

    private string FormatRelaxText(float min, float max)
    {
        return AppData.selectedMechanism.IsMechanism("HOC") ?
            $"Prev AROM: {ConvertToCM(min).ToString("0.0")}cm : {ConvertToCM(max).ToString("0.0")}cm (Aperture: {ConvertToCM(max - min).ToString("0.0")}cm)" :
            $"Prev AROM: {(int)min} : {(int)max} ({(int)(max - min)}°)";
    }


    private float ConvertToCM(float value) => Mathf.Abs(Mathf.Deg2Rad * value * 6f);
   
    private void UpdateStatusText()
    {
        if (AppData.selectedMechanism.IsMechanism("HOC") == false)
        {
            JointAngle.text = (PlutoComm.angle).ToString("0.0");
        }
        else
        {
            JointAngle.text = "Aperture" + ConvertToCM(PlutoComm.angle).ToString("0.0") + "cm";

            JointAngleHoc.text = "Aperture" + ConvertToCM(PlutoComm.angle).ToString("0.0") + "cm";
        }

    }
    public void createFile()
    {
        string dir = Path.Combine(DataManager.directoryAPROMData, AppData.selectedMechanism + ".csv");
        if (!Directory.Exists(DataManager.directoryAPROMData))
        {
            Directory.CreateDirectory(DataManager.directoryAPROMData);
        }
        if (!File.Exists(dir))
        {
            using (var writer = new StreamWriter(dir, false, Encoding.UTF8))
            {
                writer.WriteLine("datetime,promMin,promMax,aromMin,aromMax");
            }
        }
    }

}
