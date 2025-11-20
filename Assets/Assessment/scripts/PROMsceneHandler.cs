using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using TS.DoubleSlider;


public class PROMsceneHandler : MonoBehaviour
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

    
    public DoubleSlider promSlider;
    public DoubleSlider promSliderHOC;

    public bool isSelected = false;
    private bool isRestarting = false;
    public bool isButtonPressed = false;

    public bool runOnce = false;

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

    // private string nextScene = "CHGAME";
    private string nextScene = "ASSISTPROFILE";
   
    void Start()
    {
        // Attach callback for PLUTO button event.
        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
    }

    private void InitializeAssessment()
    {
        // Set control to NONE.
      //  PlutoComm.setControlType("TORQUE");

        promSlider.UpdateMinMaxvalues = false;
        nextButton.SetActive(false);

        angLimit = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? PlutoComm.CALIBANGLE[PlutoComm.mechanism] : PlutoComm.MECHOFFSETVALUE[PlutoComm.mechanism];
        promSlider.Setup(-angLimit, angLimit, AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax);
        promSlider.minAng = 0;
        promSlider.maxAng = 0;

        // Handle HOC and other mechanisms differently.
        cText.gameObject.SetActive(AppData.Instance.selectedMechanism.IsMechanism("HOC"));
        rText.gameObject.SetActive(true);
        lText.gameObject.SetActive(true);
        cText.text = AppData.Instance.selectedMechanism.IsMechanism("HOC")  ? "Closed" : "";

        // Handle the right and left sides differently.
        (_rinx, _linx) = AppData.Instance.trainingSide == "right" ? (1, 0) : (0, 1);
        rText.text = DirectionText[PlutoComm.mechanism - 1][_rinx];
        lText.text = DirectionText[PlutoComm.mechanism - 1][_linx];
        
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
                if(!runOnce){
                InitializeAssessment();
                runOnce = true;
                }
                if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                {
                    startAssessment();
                    isButtonPressed = false;
                }
                if (isRestarting)
                {
                    relaxText.color = Color.red;
                    relaxText.text = "PROM Should not below the range of AROM \n " +
                                     "Please REDO AROM AGAIN";
                }
                else relaxText.text = FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax);
                break;
            case AssessStates.ASSESS:
                startButton.SetActive(false);
                _tmin = promSlider.minAng;
                _tmax = promSlider.maxAng;
                //Debug.Log("max angle :" + _tmax);
                relaxText.color= Color.white;
                relaxText.text = FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax);
                nextButton.SetActive(true);
                if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                {
                    OnNextButtonClick();
                    isButtonPressed = false;
                }
                //checkAromLimits();
                break;
        }
    }
    
    private void checkPromLimits()
    {
        bool isHOC=AppData.Instance.selectedMechanism.IsMechanism("HOC");
        bool FME = AppData.Instance.selectedMechanism.IsMechanism("FME1") || AppData.Instance.selectedMechanism.IsMechanism("FME2");
        bool condition;

        if (isHOC)
        {
            Debug.Log($"prom min: {_tmin},{_tmax},  arom :{AppData.Instance.selectedMechanism.newRom.aromMin}, {AppData.Instance.selectedMechanism.newRom.aromMax}");
            condition = _tmax > AppData.Instance.selectedMechanism.newRom.aromMax;
        }
        else if (FME)
        {
            // Debug.Log($"prom :{_tmin}, {_tmax}, arom :{AppData.Instance.selectedMechanism.newRom.aromMin}, {AppData.Instance.selectedMechanism.newRom.aromMax}");
            // Debug.Log(_tmin <= (AppData.Instance.selectedMechanism.newRom.aromMin + 5.0f));
            // Debug.Log(_tmax >= (AppData.Instance.selectedMechanism.newRom.aromMax - 5.0f));

            condition = (_tmin > (AppData.Instance.selectedMechanism.newRom.aromMin + 5.0f)) || (_tmax < (AppData.Instance.selectedMechanism.newRom.aromMax - 5.0f));
            Debug.Log($"cnd: {condition}");
        }
        else
        {
            condition = _tmin >= AppData.Instance.selectedMechanism.newRom.aromMin || _tmax <= AppData.Instance.selectedMechanism.newRom.aromMax;
        }
        
        if (condition)
        {
            // Debug.Log($"prom min :{_tmin}, arom {AppData.Instance.selectedMechanism.newRom.aromMin},,{_tmin < AppData.Instance.selectedMechanism.newRom.aromMin}");
            // Debug.Log($" prom max :{_tmax}, arom {AppData.Instance.selectedMechanism.newRom.aromMax},,{_tmax > AppData.Instance.selectedMechanism.newRom.aromMax}");

            promSlider.UpdateMinMaxvalues = false;
            RestartAssessment();
            isButtonPressed = false;
            isRestarting = true;
            curreposition.SetActive(true);
            currepositionHoc.SetActive(AppData.Instance.selectedMechanism.IsMechanism("HOC"));
        }
        else
        {
            Debug.Log($" min :{promSlider._currePostion.value}, arom {AppData.Instance.selectedMechanism.newRom.aromMin},,{promSlider._currePostion.value <= AppData.Instance.selectedMechanism.newRom.aromMin}");
            Debug.Log($" min :{promSlider._currePostion.value}, arom {AppData.Instance.selectedMechanism.newRom.aromMax},,{promSlider._currePostion.value <= AppData.Instance.selectedMechanism.newRom.aromMax}");
            promSlider.UpdateMinMaxvalues = true;
            curreposition.SetActive(true);
            currepositionHoc.SetActive(AppData.Instance.selectedMechanism.IsMechanism("HOC"));

        }

    }

    public void OnRedoaromButtonClick()
    {
        InitializeAssessment();
        Debug.Log("Assessment Restarted");
        promSlider.UpdateMinMaxvalues = false;
    }

    public void OnNextButtonClick()
    {
        checkPromLimits();
        onSavePressed();
        nextButton.SetActive(false);
        promSlider.UpdateMinMaxvalues = false;
    }

    public void startAssessment()
    {
        _state = AssessStates.ASSESS;
        nextButton.SetActive(false);
        startButton.SetActive(false);
        promSlider.startAssessment(PlutoComm.angle);
        promSlider.UpdateMinMaxvalues = true;
    }

    public void onSavePressed()
    {
        bool mechFME = (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME1") && (PlutoComm.MECHANISMS[PlutoComm.mechanism] != "FME2");
        // Set the new PROM values in the selected mechanism.
        AppData.Instance.selectedMechanism.SetNewPromValues(promSlider.minAng, promSlider.maxAng);

        if (AppData.Instance.selectedMechanism.IsMechanism("HOC"))
        {
            float currentMinCM = ConvertToCM(_tmin);
            float currentMaxCM = ConvertToCM(_tmax);

            relaxText.text = " Assessment Completed \n" + FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax)
                             + "Current PROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0")
                             + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
        }
        else
        {
            relaxText.text = " Assessment Completed \n "
                             + FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax)
                             + "Current PROM: " + (int)_tmin + " : " + (int)_tmax + " (" + (int)(_tmax - _tmin) + " °)\n";
        }

        nextButton.SetActive(false);
        promSlider.UpdateMinMaxvalues = false;

        // Log full assessment detail in the log file.
        string logMessage = $"Mechanism: {AppData.Instance.selectedMechanism.name}";
        logMessage += $" | Old PROM: [{AppData.Instance.selectedMechanism.oldRom.promMin:F2}, {AppData.Instance.selectedMechanism.oldRom.promMax:F2}]";
        logMessage += $" | New PROM: [{AppData.Instance.selectedMechanism.newRom.promMin:F2}, {AppData.Instance.selectedMechanism.newRom.promMax:F2}]";
        logMessage += $" | Old AROM: [{AppData.Instance.selectedMechanism.oldRom.aromMin:F2} ,  {AppData.Instance.selectedMechanism.oldRom.aromMax:F2}]";
        logMessage += $" | New AROM: [{AppData.Instance.selectedMechanism.newRom.aromMin:F2} ,  {AppData.Instance.selectedMechanism.newRom.aromMax:F2}]";
        AppLogger.LogInfo(logMessage);

        // Switch scene if assessment is complete.
        if (AppData.Instance.selectedMechanism.promCompleted && AppData.Instance.selectedMechanism.aromCompleted && mechFME) SceneManager.LoadScene(nextScene);
        else if (AppData.Instance.selectedMechanism.promCompleted && AppData.Instance.selectedMechanism.aromCompleted && !mechFME)
        {
            AppData.Instance.selectedMechanism.SetNewAPromValues(promSlider.minAng, promSlider.maxAng);
            AppData.Instance.selectedMechanism.SaveAssessmentData();
            SceneManager.LoadScene("CHGAME");
        }
    }

    private string FormatRelaxText(float min, float max)
    {
        return AppData.Instance.selectedMechanism.IsMechanism("HOC") ?
            $"Prev PROM: {ConvertToCM(min).ToString("0.0")}cm : {ConvertToCM(max).ToString("0.0")}cm (Aperture: {ConvertToCM(max - min).ToString("0.0")}cm)" :
            $"Prev PROM: {(int)min} : {(int)max} ({(int)(max - min)}°)";
    }

    private float ConvertToCM(float value) => Mathf.Abs(Mathf.Deg2Rad * value * 6f);
   
    private void UpdateStatusText()
    {
        if (AppData.Instance.selectedMechanism.IsMechanism("HOC") == false)
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

