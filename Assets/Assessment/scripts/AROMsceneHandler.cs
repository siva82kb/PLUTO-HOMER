
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TS.DoubleSlider;
// using Mono.Cecil.Cil;
using UnityEngine.SceneManagement;


public class AROMsceneHandler : MonoBehaviour
{
    enum AssessStates
    {
        INIT,
        MOVE_TO_EXTREME,
        ASSESS,
        AROM_LOCKED
    };
    private bool isButtonPressed = false;
    public TMP_Text lText;
    public TMP_Text rText;
    public TMP_Text insText;
    public TMP_Text cText;
    public TMP_Text relaxText;
    public TMP_Text feedbackText;
    public TMP_Text jointAngle;
    public TMP_Text jointAngleHoc;
    public TMP_Text directionArrow; // "→" or "←"
    private int _linx, _rinx;
    private float _tmin = 0f, _tmax = 0f;

    public GameObject nextButton;
    public GameObject startButton;
    public GameObject CurrPositioncursor;
    public GameObject CurrPositioncursorHoc;
    public UnityEngine.UI.Image aromLockedImage; // Reference to change background color

    private AssessStates _state;

    private float angLimit;
    public DoubleSlider aromSlider;
    public DoubleSlider aromSliderHOC;
    public bool isSelected = false;

    public assessmentSceneHandler panelControl;

    private bool aromConfirmed = false; // Track if AROM has been confirmed
    private const float MIN_AROM_RANGE = 5f; // Minimum 5 degrees to require confirmation
    private const float EXTREME_POINT_THRESHOLD = 2f; // Threshold to detect movement to extreme point

    private List<string[]> DirectionText = new List<string[]>
     {
         new string[] { "Flexion", "Extension" },
         new string[] { "Radial Dev" ,"Ulnar Dev"},
         new string[] { "Pronation", "Supination" },
         new string[] { "Open", "Open"},
         new string[] { "", "" },
         new string[] { "", "" }
     };


     // --- REAL-TIME TRACKING ---
    private float lastAngle = 0f;
    private float lastDirection = 0f;

    private float forwardLimit = 0f;
    private float backwardLimit = 0f;

    private int forwardReversals = 0;
    private int backwardReversals = 0;

    // Hybrid adaptive threshold parameters
    private const float THRESHOLD_MIN = 5f;      // Minimum threshold (filters jitter)
    private const float THRESHOLD_MAX = 15f;     // Maximum threshold (prevents false reversals)
    private const float THRESHOLD_PERCENT = 0.15f; // 15% of range

    private float directionThreshold = 2f;

    // Post-confirmation validation: track if patient can reach both points after AROM is locked
    private int postConfirmForwardReversals = 0;
    private int postConfirmBackwardReversals = 0;
    private float lockedMinPoint = 0f;
    private float lockedMaxPoint = 0f;


    void Start()
    {
        // Initialize the assessment data.
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

        // Disable the button to move to the next assessment.
        nextButton.SetActive(false);
        
        // Reset AROM confirmed flag
        aromConfirmed = false;

        // Reset image color
        if (aromLockedImage != null)
            aromLockedImage.color = Color.white;

        // Update the min and max values.
        angLimit = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? PlutoComm.CALIBANGLE[PlutoComm.mechanism] + 10.0f : PlutoComm.MECHOFFSETVALUE[PlutoComm.mechanism] + 10.0f;
        aromSlider.Setup(-angLimit, angLimit, AppData.Instance.selectedMechanism.oldRom.aromMin, AppData.Instance.selectedMechanism.oldRom.aromMax);
        
        // Initialize min and max to current position, not 0
        float startAngle = PlutoComm.angle;
        aromSlider.minAng = startAngle;
        aromSlider.maxAng = startAngle;

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

    private void DisablearomGameObjects()
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
        // jointAngle.text = ((int)PlutoComm.angle).ToString();
        // jointAngleHoc.text = ((int)PlutoComm.getHOCDisplay(PlutoComm.angle)).ToString();

        if (isSelected)
        {
            runaAssessmentStateMachine();
            UpdateStatusText();
        }
        else
        {
            if (AppData.Instance.selectedMechanism.IsMechanism("HOC"))
            {
                float currentMinCM = ConvertToCM(aromSlider.minAng);
                float currentMaxCM = ConvertToCM(aromSlider.maxAng);
                relaxText.text = "Assessment Completed \n"
                                 + FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.aromMin, AppData.Instance.selectedMechanism.oldRom.aromMax) 
                                 + "Current AROM: " + currentMinCM.ToString("0.0") + "cm : " 
                                 + currentMaxCM.ToString("0.0") + "cm (Aperture: "
                                 + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
            }
            else
            {
                relaxText.text = "Assessment Completed \n"
                                 + FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.aromMin, AppData.Instance.selectedMechanism.oldRom.aromMax)
                                 + "|| " + "Current AROM: " + (int)aromSlider.minAng + " : "
                                 + (int)aromSlider.maxAng + " (" + (int)(aromSlider.maxAng - aromSlider.minAng) + "°)\n";
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
                startButton.SetActive(false);

                if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                {
                    _state = AssessStates.MOVE_TO_EXTREME;
                    isButtonPressed = false;
                    AppLogger.LogInfo("Patient instructed to move to one extreme point");
                }
                relaxText.text = FormatRelaxText(AppData.Instance.selectedMechanism.oldRom.promMin, AppData.Instance.selectedMechanism.oldRom.promMax)
                    + "\n\nPress PLUTO button to start";
                break;

            case AssessStates.MOVE_TO_EXTREME:
                startButton.SetActive(false);

                string extremeUnit = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? "cm" : "°";
                float extremeAngle = PlutoComm.angle;
                float displayExtreme = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? ConvertToCM(extremeAngle) : extremeAngle;

                // Display current angle and instruction
                relaxText.text = $"Move to one extreme point\n"
                    + $"Current: {displayExtreme:F1}{extremeUnit}\n"
                    + $"Then press PLUTO button";

                feedbackText.text = "";
                directionArrow.text = "";

                // Detect if patient has moved from start position
                if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                {
                    // Check if patient has moved at least EXTREME_POINT_THRESHOLD from start
                    if (Mathf.Abs(extremeAngle - lastAngle) >= EXTREME_POINT_THRESHOLD || lastAngle != 0)
                    {
                        _state = AssessStates.ASSESS;
                        aromSlider.minAng = extremeAngle;
                        aromSlider.maxAng = extremeAngle;
                        aromSlider.startAssessment(extremeAngle);
                        aromSlider.UpdateMinMaxvalues = true;

                        // Reset tracking variables
                        postConfirmForwardReversals = 0;
                        postConfirmBackwardReversals = 0;
                        forwardLimit = extremeAngle;
                        backwardLimit = extremeAngle;
                        lastAngle = extremeAngle;

                        AppLogger.LogInfo($"AROM assessment started from extreme point: {displayExtreme:F1}{extremeUnit}");
                    }
                    else
                    {
                        relaxText.text = $"Please move to an extreme point first\n"
                            + $"Current: {displayExtreme:F1}{extremeUnit}";
                    }
                    isButtonPressed = false;
                }
                break;

            case AssessStates.ASSESS:
                TrackAROMWithFeedback();
                _tmin = aromSlider.minAng;
                _tmax = aromSlider.maxAng;

                startButton.SetActive(false);
                string unit = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? "cm" : "°";
                float displayMin = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? ConvertToCM(aromSlider.minAng) : aromSlider.minAng;
                float displayMax = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? ConvertToCM(aromSlider.maxAng) : aromSlider.maxAng;

                relaxText.text = $"Exploring AROM\n"
                    + $"Min: {displayMin:F1}{unit} | Max: {displayMax:F1}{unit}\n"
                    + $"Range: {(displayMax - displayMin):F1}{unit}\n"
                    + "Press PLUTO button to confirm";

                // Transition to AROM_LOCKED or skip to PROM when button pressed
                if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                {
                    float aromRange = _tmax - _tmin;

                    // If range is below 5 degrees, skip confirmation and go to PROM
                    if (Mathf.Abs(aromRange) < MIN_AROM_RANGE)
                    {
                        AppLogger.LogInfo($"AROM range below {MIN_AROM_RANGE} degrees ({aromRange:F1}{unit}) - Skipping confirmation, moving to PROM");
                        OnNextButtonClick();
                    }
                    else
                    {
                        _state = AssessStates.AROM_LOCKED;
                        aromSlider.UpdateMinMaxvalues = false;
                        aromConfirmed = true; // Mark AROM as confirmed
                        AppLogger.LogInfo($"AROM assessment complete - Range: {displayMin:F1}{unit} to {displayMax:F1}{unit}");
                    }
                    isButtonPressed = false;
                }
                break;

            case AssessStates.AROM_LOCKED:
                startButton.SetActive(false);

                float currentMin = _tmin;
                float currentMax = _tmax;
                string unit2 = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? "cm" : "°";
                float displayMin2 = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? ConvertToCM(currentMin) : currentMin;
                float displayMax2 = AppData.Instance.selectedMechanism.IsMechanism("HOC") ? ConvertToCM(currentMax) : currentMax;

                // Track patient reaching both set endpoints
                TrackPostConfirmationReach(currentMin, currentMax);

                // Check if patient can reach both set points
                bool bothPointsReachedPostConfirm = (postConfirmForwardReversals >= 1 && postConfirmBackwardReversals >= 1);

                if (bothPointsReachedPostConfirm)
                {
                    // Green background color when ready to proceed
                    if (aromLockedImage != null)
                        aromLockedImage.color = new Color(0.2f, 0.8f, 0.3f); // Green

                    // Text colors: white for good contrast
                    if (relaxText != null)
                        relaxText.color = Color.white;
                    if (feedbackText != null)
                        feedbackText.color = Color.white;
                    if (directionArrow != null)
                        directionArrow.color = Color.white;
                    if (lText != null)
                        lText.color = Color.white;
                    if (rText != null)
                        rText.color = Color.white;
                    if (insText != null)
                        insText.color = Color.white;
                    if (cText != null)
                        cText.color = Color.white;
                    if (jointAngle != null)
                        jointAngle.color = Color.white;
                    if (jointAngleHoc != null)
                        jointAngleHoc.color = Color.white;

                    feedbackText.text = "Press PLUTO button to proceed";
                    directionArrow.text = "";
                    relaxText.text = $"AROM Confirmed!\n"
                        + $"Min: {displayMin2:F1}{unit2} | Max: {displayMax2:F1}{unit2}\n"
                        + $"Range: {(displayMax2 - displayMin2):F1}{unit2}";
                    nextButton.SetActive(false); // Hidden - use PLUTO button instead
                }
                else
                {
                    // Orange background color when need to reach both points
                    if (aromLockedImage != null)
                        aromLockedImage.color = new Color(1f, 0.65f, 0f); // Orange

                    // Text colors: white for visibility
                    if (relaxText != null)
                        relaxText.color = Color.white;
                    if (feedbackText != null)
                        feedbackText.color = Color.yellow;

                    // Show which point(s) still need to be reached
                    bool minReached = postConfirmBackwardReversals >= 1;
                    bool maxReached = postConfirmForwardReversals >= 1;

                    string instruction;
                    if (!minReached && !maxReached)
                        instruction = $"Reach both points\nMin: {postConfirmBackwardReversals}/1 | Max: {postConfirmForwardReversals}/1";
                    else if (!minReached && maxReached)
                        instruction = $"Reach MIN point: {postConfirmBackwardReversals}/1";
                    else if (minReached && !maxReached)
                        instruction = $"Reach MAX point: {postConfirmForwardReversals}/1";
                    else
                        instruction = "Ready - Press PLUTO button to proceed";

                    relaxText.text = $"Confirming AROM\n"
                        + $"Min: {displayMin2:F1}{unit2} | Max: {displayMax2:F1}{unit2}\n"
                        + $"Range: {(displayMax2 - displayMin2):F1}{unit2}\n\n"
                        + instruction;

                    feedbackText.text = minReached && maxReached ? "Press PLUTO button to proceed" : "";
                    directionArrow.text = "";
                    nextButton.SetActive(false);
                }

                // Only allow proceed if both points reached
                if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                {
                    if (bothPointsReachedPostConfirm)
                    {
                        OnNextButtonClick();
                        nextButton.SetActive(false);
                        DisablearomGameObjects();
                        feedbackText.text = "";
                    }
                    // Always clear button press flag after handling it
                    isButtonPressed = false;
                }
                break;
        }
    }

    public void OnRedoAromClick()
    {
        _state = AssessStates.INIT;
        isButtonPressed = false;
        aromConfirmed = false;

        InitializeAssessment();

        UpdateStatusText();
        panelControl.SelectAROM();
        AppData.Instance.selectedMechanism.ResetPromValues();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnPlutoButtonReleased()
    {
        isButtonPressed = true;
    }

    private float ConvertToCM(float value) => Mathf.Abs(Mathf.Deg2Rad * value * 6f);

    public void OnNextButtonClick()
    {
        OnSaveClick();
        panelControl.SelectpROM();
        DisablearomGameObjects();
    }
    
    public void OnrestartButtonClick()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnSaveClick()
    {
        _tmin = (aromSlider.minAng < -angLimit) ? -angLimit : aromSlider.minAng;
        _tmax = (aromSlider.maxAng > angLimit) ? angLimit : aromSlider.maxAng;

        // Calculate CPM: true if range <= 5 degrees, false otherwise
        float aromRange = Mathf.Abs(_tmax - _tmin);
        bool isCPM = aromRange <= MIN_AROM_RANGE;

        // Update new AROM
        AppData.Instance.selectedMechanism.SetNewAromValues(_tmin, _tmax);
        AppData.Instance.selectedMechanism.SetAromCPM(isCPM);

        AppLogger.LogInfo($"AROM assessment saved - Range: {aromRange:F1}° - CPM: {isCPM}");

        nextButton.SetActive(false);
        aromSlider.UpdateMinMaxvalues = false;
        CurrPositioncursor.SetActive(false);
        CurrPositioncursorHoc.SetActive(false);

        // Reset image color
        if (aromLockedImage != null)
            aromLockedImage.color = new Color(0, 55, 52, 1);
    }

    private string FormatRelaxText(float min, float max)
    {
        return AppData.Instance.selectedMechanism.IsMechanism("HOC") ?
            $"Prev AROM: {ConvertToCM(min).ToString("0.0")}cm : {ConvertToCM(max).ToString("0.0")}cm (Aperture: {ConvertToCM(max - min).ToString("0.0")}cm)" :
            $"Prev AROM: {(int)min} : {(int)max} ({(int)(max - min)}°)";
    }

    public void startAssessment()
    {
        _state = AssessStates.ASSESS;

        float startAngle = PlutoComm.angle;

        // Reset post-confirmation tracking
        postConfirmForwardReversals = 0;
        postConfirmBackwardReversals = 0;
        lockedMinPoint = 0f;
        lockedMaxPoint = 0f;

        // Initialize limits to current position (not 0)
        forwardLimit = startAngle;
        backwardLimit = startAngle;
        
        aromSlider.minAng = startAngle;
        aromSlider.maxAng = startAngle;

        aromSlider.startAssessment(startAngle);
        aromSlider.UpdateMinMaxvalues = true;

        AppLogger.LogInfo("AROM assessment started - Patient exploring ROM");
    }

    void TrackAROMWithFeedback()
    {
        float currentAngle = PlutoComm.angle;
        float delta = currentAngle - lastAngle;

        if (Mathf.Abs(delta) < directionThreshold)
            return;

        float currentDirection = Mathf.Sign(delta);

        // Update display direction
        if (directionArrow != null)
            directionArrow.text = currentDirection > 0 ? "→" : "←";

        // Track min and max as patient moves (updates dynamically)
        if (currentDirection > 0 && currentAngle > forwardLimit)
        {
            forwardLimit = currentAngle;
        }
        else if (currentDirection < 0 && currentAngle < backwardLimit)
        {
            backwardLimit = currentAngle;
        }

        lastAngle = currentAngle;
        lastDirection = currentDirection;

        // Update slider display with tracked limits (not starting from 0)
        aromSlider.minAng = backwardLimit;
        aromSlider.maxAng = forwardLimit;
        aromSlider.SliderMin.setSliderVal(backwardLimit);
        aromSlider.SliderMax.setSliderVal(forwardLimit);

        // Update adaptive threshold
        float currentRange = forwardLimit - backwardLimit;
        directionThreshold = Mathf.Clamp(currentRange * THRESHOLD_PERCENT, THRESHOLD_MIN, THRESHOLD_MAX);
    }

    void TrackPostConfirmationReach(float currentMin, float currentMax)
    {
        float currentAngle = PlutoComm.angle;

        // Store locked points on first entry
        if (postConfirmForwardReversals == 0 && postConfirmBackwardReversals == 0)
        {
            lockedMinPoint = currentMin;
            lockedMaxPoint = currentMax;
        }

        // Check if patient has reached the locked min point (one time)
        if (currentAngle <= lockedMinPoint && postConfirmBackwardReversals < 1)
        {
            postConfirmBackwardReversals++;
        }

        // Check if patient has reached the locked max point (one time)
        if (currentAngle >= lockedMaxPoint && postConfirmForwardReversals < 1)
        {
            postConfirmForwardReversals++;
        }
    }
    
    private void UpdateStatusText()
    {
        if (AppData.Instance.selectedMechanism.IsMechanism("HOC") == false)
        {
            jointAngle.text = (PlutoComm.angle).ToString("0.0");
        }
        else
        {
            // jointAngle.text = "Aperture" + ConvertToCM(PlutoComm.angle).ToString("0.0") + "cm";
            // jointAngleHoc.text = "Aperture" + ConvertToCM(PlutoComm.angle).ToString("0.0") + "cm";
        }
    }
}
