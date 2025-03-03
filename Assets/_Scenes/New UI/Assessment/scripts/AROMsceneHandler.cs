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
        ASSESS = 1,
        RELAX = 2
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
    private int _stpCount;
    private int _stpCountTh = 00;
    private float _lowSpdTh = 20f;
    private float _tmin, _tmax;
    private float _tmin1, _tmax1;

    public float prommin;
    public float prommax;
    private double _strttime;
    private double _initDur = 0f;
    private double _assessDur = 180f;
    private double _relaxDur = 3.0f;
    private float _winT = 0;
    private float _freq = 0.1f;
    public static float _torqAmp = .7f;
    private float _currTorq = 0;
    private double _t;
    private double _prevt;
    private double _dt = 0.01;
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

        nextButton.SetActive(false);
        Debug.Log("Initializing AROM assessment");

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
        if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) != 4)
        {
            angLimit = AppData.offsetAtNeutral[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.selectedMechanism)];

            aromSlider.Setup(-angLimit, angLimit, AppData.oldAROM.aromTmin, AppData.oldAROM.aromTmax);
            aromSlider.maxAng = 0;
            aromSlider.minAng = 0;
            aromSlider.UpdateMinMaxvalues = false;
            Debug.Log($"Slider Min: {aromSlider.minAng}, Max: {aromSlider.maxAng}, arom:{AppData.oldAROM.aromTmin},{AppData.oldAROM.aromTmax}");

        }
        else
        {
            float minAng = 0;
            float maxAng = 90;

            angLimit = 140.42f;

            aromSlider.Setup(-angLimit, angLimit, AppData.oldAROM.aromTmin, AppData.oldAROM.aromTmax);


            aromSlider.minAng = 0;  // Set slider minimum to old AROM minimum
            aromSlider.maxAng = 0;
            aromSlider.UpdateMinMaxvalues = false;


        }
        if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
        {
            cText.gameObject.SetActive(true); // Show the C Text
            rText.gameObject.SetActive(true);

            lText.gameObject.SetActive(true);

            cText.text = "Closed"; // Set the C Text in the center
        }
        else
        {
            cText.gameObject.SetActive(false);
        }
        if (AppData.trainingSide == "right")
        {
            _rinx = 1;
            _linx = 0;
        }
        else
        {
            _rinx = 0;
            _linx = 1;
        }
        rText.gameObject.SetActive(true);
        lText.gameObject.SetActive(true);
        rText.text = DirectionText[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.selectedMechanism)][_rinx];
        lText.text = DirectionText[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.selectedMechanism)][_linx];

        _stpCount = 0;
        _tmin = 180f;
        _tmax = -180f;
        _tmin1 = 180f;
        _tmax1 = -180f;


        _strttime = AppData.CurrentTime;

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

            _t = AppData.CurrentTime - _strttime;
            switch (_state)
            {
                case AssessStates.INIT:
                    if (!isInteractable)
                    {

                        AppData.oldAROM = new ROM(AppData.selectedMechanism);
                        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
                        InitializeAssessment();
                        isInteractable = true;
                    }
                    startButton.SetActive(true);

                    //AppData.newPROM = new MechanismData(AppData.selectedMechanism);


                    float newPROM_tmin = AppData.promTmin;
                    float newPROM_tmax = AppData.promTmax;


                    prommin = AppData.promTmin;

                    prommax = AppData.promTmax;

                    if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                    {
                        nextButton.SetActive(true);
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
                        if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
                    {
                        float apertureMinCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldAROM.aromTmin * 6f);
                        float apertureMaxCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldAROM.aromTmax * 6f);
                        //relaxText.color = Color.black;
                        relaxText.text = "Prev Arom: " + apertureMinCM.ToString("0.0") + "cm : " + apertureMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(apertureMaxCM - apertureMinCM).ToString("0.0") + "cm)";

                    }
                    else
                    {
                        relaxText.text = "Prev AROM: " + (int)AppData.oldAROM.aromTmin + " : " + (int)AppData.oldAROM.aromTmax + " (" + (int)(AppData.oldAROM.aromTmax - AppData.oldAROM.aromTmin) + "°)";
                       
                    }
                    break;
                case AssessStates.ASSESS:

                    startButton.SetActive(false);
                    nextButton.SetActive(true);

                    assessmentSaved = false;
                    if ( isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                    {
                        _state = AssessStates.RELAX;
                        gameData.isPROMcompleted = true;
                        onSavePressed();
                        nextButton.SetActive(false);
                        isButtonPressed = false;
                        aromSlider.UpdateMinMaxvalues = false;
                    }
                    aromgreater();

                    break;
                case AssessStates.RELAX:
                    if (AssessmentValid)

                    {

                        if (!assessmentSaved)
                        {

                            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
                            {

                                float apertureMinCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldAROM.aromTmin * 6f);
                                float apertureMaxCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldAROM.aromTmax * 6f);
                                float currentMinCM = Mathf.Abs(Mathf.Deg2Rad * aromSlider.minAng * 6f);
                                float currentMaxCM = Mathf.Abs(Mathf.Deg2Rad * aromSlider.maxAng * 6f);
                                relaxText.color = Color.white;
                                relaxText.text = "Assessment Completed \n " + "Prev AROM: " + apertureMinCM.ToString("0.0") + "cm : " + apertureMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(apertureMaxCM - apertureMinCM).ToString("0.0") + "cm)\n" +
                                    "Current AROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
                            }
                            else
                            {
                                relaxText.color = Color.white;
                                relaxText.text = "Assessment Completed \n " + "Prev AROM: " + (int)AppData.oldAROM.aromTmin + " : " + (int)AppData.oldAROM.aromTmax + " (" + (int)(AppData.oldAROM.promTmax - AppData.oldAROM.promTmin) + "°)\n" +
                                    "Current AROM: " + (int)aromSlider.minAng + " : " + (int)aromSlider.maxAng + " (" + (int)(aromSlider.maxAng - aromSlider.minAng) + "°)\n";
                            }

                            if (isButtonPressed)
                            {
                                if(gameData.isPROMcompleted && gameData.isAROMcompleted)
                                {
                                    gameData.setNeutral = true;
                                    SceneManager.LoadScene("choosegame");
                                }
                                isButtonPressed = false;
                            }
                        }
                        else
                        {
                            aromSlider.UpdateMinMaxvalues = false;
                            //Debug.Log("Hello3");

                            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
                            {
                                float apertureMinCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldAROM.aromTmin * 6f);
                                float apertureMaxCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldAROM.aromTmax * 6f);
                                float currentMinCM = Mathf.Abs(Mathf.Deg2Rad * _tmin * 6f);
                                float currentMaxCM = Mathf.Abs(Mathf.Deg2Rad * -_tmax * 6f);
                                relaxText.text = "Assessment Completed \n" + "Prev AROM: " + apertureMinCM.ToString("0.0") + "cm : " + apertureMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(apertureMaxCM - apertureMinCM).ToString("0.0") + "cm)\n" +
                                "Current AROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
                                SceneManager.LoadScene("choosegame");
                            }
                            else
                            {
                                relaxText.text = relaxText.text = "Assessment Completed \n " + "Prev AROM: " + (int)AppData.oldAROM.aromTmin + " : " + (int)AppData.oldAROM.aromTmax + " (" + (int)(AppData.oldAROM.promTmax - AppData.oldAROM.promTmin) + "°)\n" +
                                    "Current AROM: " + (int)_tmin + " : " + (int)_tmax + " (" + (int)(_tmax - _tmin) + "°)\n";

                                SceneManager.LoadScene("choosegame");
                            }
                            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) < 4)
                            {
                            }
                            if (Input.GetKeyDown(KeyCode.Return))
                            {
                                Debug.Log("go to arom");
                            }

                        }
                    }
                    else
                    {
                        {
                            relaxText.text = "PROM should be greater than AROM\n " +
                                "Press ENTER Redo Assesment ";
                        }

                    }
                    break;

            }
            UpdateGUI();
        }
        else
        {
        }
    }

    private void aromgreater()
    {
        if (aromSlider._currePostion.value <= prommin || aromSlider._currePostion.value >= prommax)
        {

            aromSlider.UpdateMinMaxvalues = false;
            RestartAssessment();
            isRestarting = true;
            curreposition.SetActive(true);
            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
            {
                currepositionHoc.SetActive(true);
            }
            else
            {
                currepositionHoc.SetActive(false);
            }
            relaxText.text = " AROM Do not Exceed PROM \n " +
            "Please REDO PROM AGAIN";
        }
        else
        {
            aromSlider.UpdateMinMaxvalues = true;
            AssessmentValid = true;
            curreposition.SetActive(true);
            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
            {
                currepositionHoc.SetActive(true);
            }
            else
            {
                currepositionHoc.SetActive(false);
            }
            relaxText.text = "   ";
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
        AssessmentValid = true;
        _state = AssessStates.RELAX;
        aromgreater();
        onSavePressed();
        nextButton.SetActive(false);
        aromSlider.UpdateMinMaxvalues = false;
    }

    public void startAssessment()
    {
        _state = AssessStates.ASSESS;
        aromSlider.startAssessment(PlutoComm.angle);
        aromSlider.UpdateMinMaxvalues = true;
       // Debug.Log("Why Not");
    }

    bool validAssessment()
    {
        AppData.oldAROM = new ROM(AppData.selectedMechanism);
        if (_tmin <= AppData.oldAROM.aromTmin && _tmax >= AppData.oldAROM.aromTmax)
        {
            return true;
        }
        else
            return false;
    }

    public void onSavePressed()
    {
        _tmin = aromSlider.minAng;
        _tmax = aromSlider.maxAng;
        assessmentSaved = true;
        AppData.newAROM = new ROM(AppData.promTmin,AppData.promTmax,_tmin, _tmax,
         AppData.selectedMechanism, true);

        if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
        {
            float apertureMinCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldAROM.aromTmin * 6f);
            float apertureMaxCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldAROM.aromTmax * 6f);
            float currentMinCM = Mathf.Abs(Mathf.Deg2Rad * _tmin * 6f);
            float currentMaxCM = Mathf.Abs(Mathf.Deg2Rad * -_tmax * 6f);
            relaxText.color = Color.white;
            relaxText.text = "Assessment Completed \n" + "Prev AROM: " + apertureMinCM.ToString("0.0") + "cm : " + apertureMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(apertureMaxCM - apertureMinCM).ToString("0.0") + "cm)\n" +
                    "Current AROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
        }
        else
        {
            relaxText.color = Color.white;
            relaxText.text = "Assessment Completed \n " + "Prev AROM: " + (int)AppData.oldAROM.aromTmin + " : " + (int)AppData.oldAROM.aromTmax + " (" + (int)(AppData.oldAROM.promTmax - AppData.oldAROM.promTmin) + "°)\n" +

            "Currentt AROM: " + (int)_tmin + " : " + (int)_tmax + " (" + (int)(_tmax - _tmin) + "°)\n";
        }
        nextButton.SetActive(false);
        aromSlider.UpdateMinMaxvalues = false;
    }


 

    void OnApplicationQuit()
    {
    }

    public void On_Back_Click()
    {
        SceneManager.LoadScene(2);
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
            JointAngle.text = "Aperture" + Mathf.Abs((Mathf.Deg2Rad * PlutoComm.angle * 6f)).ToString("0.0") + "cm";
        JointAngleHoc.text = "Aperture" + Mathf.Abs((Mathf.Deg2Rad * PlutoComm.angle * 6f)).ToString("0.0") + "cm";

    }

}
