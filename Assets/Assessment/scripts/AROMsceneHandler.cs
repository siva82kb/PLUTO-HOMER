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
        INIT = 0,
        ASSESS = 1
    };
    bool assessmentSaved;
    public TMP_Text lText;
    public TMP_Text rText;
    public TMP_Text cText;
    public TMP_Text relaxText;

    public TMP_Text JointAngle;
    public TMP_Text JointAngleHoc;

    public TMP_Text warningText;
    bool AssessmentValid;
    private float _tmin, _tmax, _tmin1, _tmax1;
    private float prommin, prommax;

    public GameObject nextButton;
    public GameObject startButton;
    public GameObject curreposition;
    public GameObject currepositionHoc;
    private AssessStates _state;
    private float angLimit;
    public DoubleSlider aromSlider;
    public DoubleSlider promSlider;

    public bool isSelected = false;
    public bool isInteractable = false;
    public assessmentSceneHandler panelControl;

    private bool isRestarting = false;
    private bool isButtonPressed = false;

    private List<string[]> DirectionText = new List<string[]>
     {
         new string[] { "Flexion", "Extension" },
         new string[] { "Ulnar Dev.", "Radial Dev."},
         new string[] { "Pronation", "Supination" },
         new string[]{ "Open", "Open"},
         new string[] {"",""},
         new string[] {"",""}
     };

    private int _linx, _rinx;

    void Start()
    {

        //InitializeAssessment();
        //aromSlider.UpdateMinMaxvalues = false;
    }

    private void InitializeAssessment()
    {
        aromSlider.UpdateMinMaxvalues = false;
        gameData.isAROMcompleted = false;
        nextButton.SetActive(false);

        string dir = Path.Combine(DataManager.directoryAPROMData, AppData.selectedMechanism + ".csv");
        if (!Directory.Exists(DataManager.directoryAPROMData))
        {
            Directory.CreateDirectory(DataManager.directoryAPROMData);
        }
        if (!File.Exists(dir))
        {
            using (var writer = new StreamWriter(dir, false, Encoding.UTF8))
            {
                writer.WriteLine("datetime,promTmin,promTmax,aromTmin,aromTmax");
            }
        }

        int mechanismIndex = Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism);
        angLimit = (mechanismIndex != 4) ? AppData.offsetAtNeutral[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.selectedMechanism)] : 100.42f;

        aromSlider.Setup(-angLimit, angLimit, AppData.oldAROM.aromTmin, AppData.oldAROM.aromTmax);
        aromSlider.minAng = aromSlider.maxAng = 0;

        bool isSpecialCase = (mechanismIndex == 4);
        cText.gameObject.SetActive(isSpecialCase);
        rText.gameObject.SetActive(true);
        lText.gameObject.SetActive(true);
        cText.text = isSpecialCase ? "Closed" : "";

        (_rinx, _linx) = AppData.trainingSide == "right" ? (1, 0) : (0, 1);
        rText.text = DirectionText[mechanismIndex][_rinx];
        lText.text = DirectionText[mechanismIndex][_linx];

        _tmin = 180f;
        _tmax = -180f;
        _tmin1 = 180f;
        _tmax1 = -180f;



        _state = AssessStates.INIT;

        UpdateGUI();
        PlutoComm.setControlType("NONE");
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
            switch (_state)
            {
                case AssessStates.INIT:
                    gameData.isAROMcompleted = false;
                    if (!isInteractable)
                    {

                        AppData.oldAROM = new ROM(AppData.selectedMechanism);
                        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
                        InitializeAssessment();
                        isInteractable = true;
                    }
                    startButton.SetActive(true);


                    prommin = AppData.promTmin;

                    prommax = AppData.promTmax;

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
                    else

                        relaxText.color = Color.white;
                   relaxText.text = FormatRelaxText(AppData.oldAROM.aromTmin, AppData.oldAROM.aromTmax);
                    break;
                case AssessStates.ASSESS:

                    _tmin = aromSlider.minAng;
                    _tmax = aromSlider.maxAng;
                    relaxText.color = Color.white;
                    relaxText.text = FormatRelaxText(AppData.oldAROM.aromTmin, AppData.oldAROM.aromTmax);

                    nextButton.SetActive(true);

                    gameData.isAROMcompleted = true;

                    if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                    {
                        OnNextButtonClick();
                        isButtonPressed = false;
                    }

                    checkAromLimits();

                    break;
            }
            UpdateGUI();
        }

    }

    private void checkAromLimits()
    {
        if (aromSlider._currePostion.value <= prommin || aromSlider._currePostion.value >= prommax)
        {

            aromSlider.UpdateMinMaxvalues = false;
            RestartAssessment();
            isRestarting = true;
            gameData.isAROMcompleted = false;
            curreposition.SetActive(true);
            currepositionHoc.SetActive(Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4);

            relaxText.text = " AROM Do not Exceed PROM \n " +
            "Please REDO PROM AGAIN";
        }
        else
        {
            aromSlider.UpdateMinMaxvalues = true;
            curreposition.SetActive(true);
            currepositionHoc.SetActive(Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4);

        }

    }

    public void OnRedoaromButtonClick()
    {

        InitializeAssessment();
        Debug.Log("Assessment Restarted");
        Start();
        aromSlider.UpdateMinMaxvalues = false;
    }

    public void aromButton()
    {
        Start();
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

        AppData.newAROM = new ROM(AppData.promTmin, AppData.promTmax, _tmin, _tmax,
         AppData.selectedMechanism, true);

        if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
        {
            float currentMinCM = ConvertToCM(_tmin);
            float currentMaxCM = ConvertToCM(_tmax);

            relaxText.text = "Assessment Completed \n" +
                            "Current AROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0")
                            + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
        }
        else
        {
            relaxText.text = "Assessment Completed \n " +
            "Current AROM: " + (int)_tmin + " : " + (int)_tmax + " (" + (int)(_tmax - _tmin) + " °)\n";
        }

        nextButton.SetActive(false);
        aromSlider.UpdateMinMaxvalues = false;

        if (gameData.isPROMcompleted && gameData.isAROMcompleted)
        {
            SceneManager.LoadScene("choosegame");
        }
    }

    private string FormatRelaxText(float min, float max)
    {
        return Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4 ?
            $"Prev AROM: {ConvertToCM(min)}cm : {ConvertToCM(max)}cm (Aperture: {ConvertToCM(max - min)}cm)" :
            $"Prev AROM: {(int)min} : {(int)max} ({(int)(max - min)}°)";
    }


    private float ConvertToCM(float value) => Mathf.Abs(Mathf.Deg2Rad * value * 6f);
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
