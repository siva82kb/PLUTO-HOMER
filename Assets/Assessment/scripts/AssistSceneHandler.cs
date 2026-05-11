using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TS.DoubleSlider;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;
using Unity.VisualScripting;


public class AssistsceneHandler : MonoBehaviour
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
    public TMP_Text cText, inst, inst1;
    public TMP_Text relaxText;
    
    public TMP_Text jointAngle;
    public TMP_Text jointAngleHoc;
    public TextMeshProUGUI mechName;

    private int _linx, _rinx;
    private float _tmin = 0f, _tmax  =0f;

    public GameObject CurrPositioncursor;
    public GameObject CurrPositioncursorHoc;
    public GameObject redoButton;
    private AssessStates _state;

    private float angLimit;
    public DoubleSlider apromSlider;
    public bool isSelected = false;
    public Image shadow;

    //public assessmentSceneHandler panelControl;

    private List<string[]> DirectionText = new List<string[]>
     {
         new string[] { "Flexion", "Extension" },
         new string[] { "Radial Dev" ,"Ulnar Dev"},
         new string[] { "Pronation", "Supination" },
         new string[] { "Open", "Open"},
         new string[] { "", "" },
         new string[] { "", "" }
     };


     float currentAngle = PlutoComm.angle;
    float targetPositiveEnd ;  // Positive limit
    float targetNegativeEnd ; // Negative limit
    float endpointTolerance = 5f;
    bool runOnce1 = false;
    float torque = 0f;

    // Track if reached both ends
    bool reachedPositive = false;
    bool reachedNegative = false;

    // Flags to track which side we're heading toward
    bool goingPositive = true;

    // Assessment tracking
    float previousAngle = 0f;
    float maxAngle = 0f;
    float minAngle = 0f;
    bool firstPositiveStart = true;
    bool firstNegativeStart = true;
    bool runOnce = false;


    void Start()
    {
        // Set mechanism name
        mechName.text = PlutoComm.MECHANISMSTEXT[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.Instance.selectedMechanism.name)];

        InitializeAssessment();
    }
    void ResetAssessment()
    {
        // PlutoComm.setControlType("NONE");
        runOnce1 = false;
        torque = 0f;
        goingPositive = true;
        reachedPositive = false;
        reachedNegative = false;
        firstPositiveStart = true;
        firstNegativeStart = true;
        minAngle = 0f;
        maxAngle = 0f;
        _tmin = 0f;
        _tmax = 0f;
        apromSlider.minAng = 0;
        apromSlider.maxAng = 0;
        inst.text = "";
        inst1.text = "";
        shadow.color = new Color(1f, 0.5f, 0f, 0.7f);

        redoButton.SetActive(false);
        runOnce = false;
}


    public void InitializeAssessment()
    {
        // Disable control.
        ResetAssessment();

        // Update the min and max values.
        angLimit = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? PlutoComm.CALIBANGLE[PlutoComm.mechanism] : PlutoComm.MECHOFFSETVALUE[PlutoComm.mechanism];
        targetNegativeEnd = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? AppData.Instance.selectedMechanism.newRom.promMin : AppData.Instance.selectedMechanism.newRom.promMin;
        targetPositiveEnd = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? 0.0f: AppData.Instance.selectedMechanism.newRom.promMax;


        float sliderPE = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? -AppData.Instance.selectedMechanism.newRom.promMin : AppData.Instance.selectedMechanism.newRom.promMax;
        float sliderNE = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? AppData.Instance.selectedMechanism.newRom.promMin : AppData.Instance.selectedMechanism.newRom.promMin;

       apromSlider.Setup(sliderNE, sliderPE, 0, 0);

        //apromSlider.Setup(-angLimit, angLimit, 0, 0);
        apromSlider.minAng = 0;
        apromSlider.maxAng = 0;
        // Update central text.
        cText.gameObject.SetActive(AppData.Instance.selectedMechanism.IsMechanism("HOC"));
        cText.text = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? "Closed" : "";
        inst1.text = "Press PLUTO button to start the AAN";

        // Update the left and right text.
        (_rinx, _linx) = AppData.Instance.IsTrainingSide("RIGHT") ? (1, 0) : (0, 1);
        rText.text = DirectionText[PlutoComm.mechanism - 1][_rinx];
        lText.text = DirectionText[PlutoComm.mechanism - 1][_linx];

        // Set the state to INIT.
        _state = AssessStates.INIT;
        inst.text = "";
        // inst1.text = "";

        // Attach callback for PLUTO button release.
        PlutoComm.OnButtonReleased +=    OnPlutoButtonReleased;

        UpdateStatusText();
    }

    IEnumerator RunAssessment()
    {
        float assessmentTimer = 0f;

        while (!reachedPositive || !reachedNegative)
        {
            assessmentTimer += Time.deltaTime;

            // 30-second total timeout
            if (assessmentTimer >= 30f)
            {
                if (!reachedPositive)
                {
                    maxAngle = currentAngle;
                    reachedPositive = true;
                }
                if (!reachedNegative)
                {
                    minAngle = currentAngle;
                    reachedNegative = true;
                }
                PlutoComm.setControlType("NONE");
                yield return new WaitForSeconds(0.1f);
                redoButton.SetActive(true);
                inst.text = $"APROM Assessment complete: {_tmin} to {_tmax}";
                inst1.text = "Press PLUTO button to move next scene";
                shadow.color = new Color(0.2f, 0.85f, 0.4f, 0.8f);
                yield return null;
                break;
            }

            if (goingPositive && !reachedPositive)
            {
                if (firstPositiveStart)
                {
                    torque = 1.0f;
                    firstPositiveStart = false;
                    PlutoComm.setControlTarget(torque);
                }

                // Button press to skip positive direction
                if (isButtonPressed && !reachedPositive)
                {
                    isButtonPressed = false;
                    reachedPositive = true;
                    maxAngle = currentAngle;
                    PlutoComm.setControlTarget(0);
                    goingPositive = false;
                    yield return null;
                    continue;
                }

                // Check if reached endpoint
                if (currentAngle >= targetPositiveEnd - endpointTolerance)
                {
                    reachedPositive = true;
                    maxAngle = currentAngle;
                    PlutoComm.setControlTarget(0);
                    goingPositive = false;
                }

                PlutoComm.setControlTarget(torque);
            }
            else if (!reachedNegative)
            {
                if (firstNegativeStart)
                {
                    torque = -1.0f;
                    firstNegativeStart = false;
                    PlutoComm.setControlTarget(torque);
                }

                // Button press to skip negative direction
                if (isButtonPressed && !reachedNegative)
                {
                    PlutoComm.setControlType("NONE");
                    yield return new WaitForSeconds(0.1f);
                    isButtonPressed = false;
                    reachedNegative = true;
                    minAngle = currentAngle;
                    torque = 0f;
                    redoButton.SetActive(true);
                    inst.text = $"APROM Assessment complete: {_tmin} to {_tmax}";
                    inst1.text = "Press PLUTO button to move next scene";
                    shadow.color = new Color(0.2f, 0.85f, 0.4f, 0.8f);
                    yield return null;
                    break;
                }

                // Check if reached endpoint
                if (currentAngle <= targetNegativeEnd + endpointTolerance)
                {
                    PlutoComm.setControlType("NONE");
                    yield return new WaitForSeconds(0.1f);
                    reachedNegative = true;
                    minAngle = currentAngle;
                    torque = 0f;
                    redoButton.SetActive(true);
                    inst.text = $"APROM Assessment complete: {_tmin} to {_tmax}";
                    inst1.text = "Press PLUTO button to move next scene";
                    shadow.color = new Color(0.2f, 0.85f, 0.4f, 0.8f);
                }

                PlutoComm.setControlTarget(torque);
            }

            previousAngle = currentAngle;
            yield return new WaitForSeconds(0.05f);
        }
}


    public void OnExit()
    {
        PlutoComm.setControlType("NONE");
        SceneManager.LoadScene("ASSESS");
    }

    void Update()
    {
        PlutoComm.sendHeartbeat();

        currentAngle = PlutoComm.angle;
        // jointAngle.text = $"{((int)PlutoComm.angle).ToString()} + Torque :{PlutoComm.target}";
        jointAngle.text = $"Angle: {((int)PlutoComm.angle).ToString()}";
        jointAngleHoc.text = $"Range:{((int)PlutoComm.getHOCDisplay(PlutoComm.angle)).ToString()}cm";
        runAssessmentStateMachine();
        // Debug.Log($" ct: {PlutoComm.CONTROLTYPE[PlutoComm.controlType]} + tor :{PlutoComm.target}");
    }

    void runAssessmentStateMachine()
    {
        Debug.Log($"state : {_state}");
        CurrPositioncursor.SetActive(true);
        CurrPositioncursorHoc.SetActive(AppData.Instance.selectedMechanism.IsMechanism("HOC"));
        switch (_state)
        {
            case AssessStates.INIT:
                if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                {
                    PlutoComm.setControlType("TORQUE");

                    //if(PlutoComm.CONTROLTYPE[PlutoComm.controlType]=="TORQUE") startAssessment();
                    startAssessment();
                    isButtonPressed = false;
                }
               // relaxText.text = FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax);
                break;
            case AssessStates.ASSESS:
                // runAssessment();
                if (!runOnce1)
                {
                    shadow.color = new Color(0f, 255f, 79f, 0.7f); // Orange with 70% opacity
               StartCoroutine(RunAssessment());
                    runOnce1 = true;
                }

                _tmin = apromSlider.minAng;
                _tmax = apromSlider.maxAng;
                 if (reachedNegative && reachedPositive)
                    {
                        PlutoComm.setControlType("NONE");

                        if (isButtonPressed)
                        {
                            AppData.Instance.selectedMechanism.SetNewAPromValues(_tmin, _tmax);
                            PlutoComm.setControlType("NONE");
                            OnSaveClick();
                            isButtonPressed = false;

if (AppData.Instance.selectedMechanism.apromCompleted)
                        {
                            if (AppData.isPlanSetup)
                            {
                                SceneManager.LoadScene("PLANSETUP");
                            }
                            else
                            {
                                SceneManager.LoadScene("CHGAME");
                                
                            }
                        }
                        }
                    }
                break;
        }
    }

    public void OnRedoPromClick()
    {
        InitializeAssessment();
        Debug.Log("Redo PROM: Reset to INIT state.");
    }

    public void OnPlutoButtonReleased()
    {
        isButtonPressed = true;
    }

    private float ConvertToCM(float value) => Mathf.Abs(Mathf.Deg2Rad * value * 6f);

    public void OnNextButtonClick()
    {
        PlutoComm.setControlType("NONE");
        OnSaveClick();

    }

    public void OnSaveClick()
    {
        AppData.Instance.selectedMechanism.SaveAssessmentData();
        apromSlider.UpdateMinMaxvalues = false;
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
        apromSlider.minAng = 0;
        apromSlider.maxAng = 0;
        Debug.Log("Assessment started");
        apromSlider.startAssessment(PlutoComm.angle);
        apromSlider.UpdateMinMaxvalues = true;
        inst1.text = "";
    }

    private void UpdateStatusText()
    {
        if (AppData.Instance.selectedMechanism.IsMechanism("HOC") == false)
        {
            jointAngle.text = $"{(PlutoComm.angle).ToString("0.0")}+ torque :{PlutoComm.target}";
        }
        else
        {
            jointAngle.text = "Aperture" + ConvertToCM(PlutoComm.angle).ToString("0.0") + "cm";
            jointAngleHoc.text = "Aperture" + ConvertToCM(PlutoComm.angle).ToString("0.0") + "cm";
        }
    }
}




