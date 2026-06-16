using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Data;

public class SetupPlanSceneHandler : MonoBehaviour
{
    [Header("Mechanism")]
    public GameObject mehcanismSelectGroup;

    [Header("Mechanism Time")]
    public TextMeshProUGUI wfetime;
    public TextMeshProUGUI wurdtime;
    public TextMeshProUGUI fpstime;
    public TextMeshProUGUI hoctime;
    public TextMeshProUGUI fme1time;
    public TextMeshProUGUI fme2time;

    [Header("Mechanism Background")]
    public Image wfeBackground;
    public Image wurdBackground;
    public Image fpsBackground;
    public Image hocBackground;
    public Image fme1Background;
    public Image fme2Background;

    [Header("Buttons")]
    public Button nextButton;
    public Button exit;

    [Header("FME Sprites")]
    public Sprite[] knobs;

    [Header("FME Preview")]
    public Image fme1PreviewImage;
    
    public Image fme2PreviewImage;

    [Header("FME Popup")]
    public GameObject imageSelectionPopup;
    public Transform imageGridContainer;
    public GameObject imageButtonPrefab;
    public Button closePopupButton;

    private int selectedFME1Index = -1;
    private int selectedFME2Index = -1;
    private int selectingForFME = 0;

    private static bool changeScene = false;
    private string mechSelected = null;
    private string nextScene = "CALIB";
    [Header("Popup UI Extras")]
    public TextMeshProUGUI popupTitle;
    public TextMeshProUGUI mechDuration;
    public TextMeshProUGUI totalTimeText;
    public TextMeshProUGUI errorText;
     public GameObject fme1Button;
    public GameObject fme2Button;


    // void Start()
    // {
    //     PlutoComm.sendHeartbeat();
    //     // AppData.Instance.userData = new PlutoUserData(DataManager.configFile, DataManager.sessionFile);

    //     PlutoComm.calibrateStart("NOMECH");
    //     PlutoComm.setControlGain(1.0f);
    //     AppData.Instance.SetMechanism(null);

    //     // if (AppData.Instance.userData == null)
    //     // {
    //     //     AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
    //     // }

    //     Time.timeScale = Time.timeScale == 0 ? 1 : Time.timeScale;

    //     AttachCallbacks();
    //     UpdateMechanismToggleButtons();
    //     StartCoroutine(DelayedAttachListeners());

    //     // 🔥 FME INIT
    //     InitializeImageSelectionPopup();
    //     imageSelectionPopup.SetActive(false);

    //     if (closePopupButton != null)
    //         closePopupButton.onClick.AddListener(ClosePopup);
    // }



void Start()
{
    AppData.isPlanSetup=true;
        if (AppData.Instance.userData == null)
        {
            AppData.Instance.Initialize(SceneManager.GetActiveScene().name);
        }
    PlutoComm.sendHeartbeat();

            //  ConfigData.LoadFromConfig(DataManager.configFile);


    PlutoComm.calibrateStart("NOMECH");
    PlutoComm.setControlGain(1.0f);
    AppData.Instance.SetMechanism(null);

    Time.timeScale = Time.timeScale == 0 ? 1 : Time.timeScale;

    AttachCallbacks();
    UpdateMechanismToggleButtons();
    StartCoroutine(DelayedAttachListeners());

    InitializeImageSelectionPopup();
    imageSelectionPopup.SetActive(false);

    if (closePopupButton != null)
        closePopupButton.onClick.AddListener(ClosePopup);

    // 🔥 UPDATE UI HERE
    UpdateMechanismDurationUI();

    // 🔥 LOAD EXISTING FME IF ANY
    LoadFMEFromSession();
}

    private void OnEnable()
    {
        // Restore FME selections when scene becomes active (when returning from other scenes)
        LoadFMEFromSession();
    }

void LoadFMEFromSession()
{
    try
    {
        selectedFME1Index = ConfigData.FME1ID;
        selectedFME2Index = ConfigData.FME2ID;

        Debug.Log($"selectedFME1Index = {ConfigData.FME1ID}");
        Debug.Log($"selectedFME2Index = {ConfigData.FME2ID}");

        if (selectedFME1Index >= 0 && selectedFME1Index < knobs.Length)
            ApplyFMESprite(1, selectedFME1Index);

        if (selectedFME2Index >= 0 && selectedFME2Index < knobs.Length)
            ApplyFMESprite(2, selectedFME2Index);
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"LoadFMEFromSession error: {ex.Message}");
        selectedFME1Index = -1;
        selectedFME2Index = -1;
    }
}
    void Update()
    {
        PlutoComm.sendHeartbeat();

        // Update mechanism duration display and exit button state in real-time
        UpdateMechanismDurationUI();

        if (changeScene)
        {
            LoadNextScene();
            changeScene = false;
        }
    }

    // =========================
    // FME SELECTION SYSTEM
    // =========================

void InitializeImageSelectionPopup()
{
    foreach (Transform child in imageGridContainer)
        Destroy(child.gameObject);

    for (int i = 0; i < knobs.Length; i++)
    {
        GameObject btn = Instantiate(imageButtonPrefab, imageGridContainer);
        int index = i;

        // Set image
        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.sprite = knobs[i];

        // 🔥 Add number label (1–12)
        TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = (i + 1).ToString();

        // Click event
        Button button = btn.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                SelectFMEImage(index);
            });
        }
    }
}
public void OpenFME1Selection()
{
    selectingForFME = 1;

    if (popupTitle != null)
        popupTitle.text = "Select FME1";

    UpdatePopupAvailability();
    imageSelectionPopup.SetActive(true);
}

public void OpenFME2Selection()
{
    selectingForFME = 2;

    if (popupTitle != null)
        popupTitle.text = "Select FME2";

    UpdatePopupAvailability();
    imageSelectionPopup.SetActive(true);
}

    public void ClosePopup()
    {
        imageSelectionPopup.SetActive(false);
        selectingForFME = 0;
    }
    void UpdateMechanismDurationUI()
    {
        if (mechDuration == null) return;

        int totalTime = ConfigData.WFE + ConfigData.WURD + ConfigData.FPS + ConfigData.HOC + ConfigData.FME1Time + ConfigData.FME2Time;

        // Update individual mechanism times and backgrounds
        UpdateMechanismDisplay("WFE", ConfigData.WFE, wfetime, wfeBackground);
        UpdateMechanismDisplay("WURD", ConfigData.WURD, wurdtime, wurdBackground);
        UpdateMechanismDisplay("FPS", ConfigData.FPS, fpstime, fpsBackground);
        UpdateMechanismDisplay("HOC", ConfigData.HOC, hoctime, hocBackground);
        UpdateMechanismDisplay("FME1", ConfigData.FME1Time, fme1time, fme1Background);
        UpdateMechanismDisplay("FME2", ConfigData.FME2Time, fme2time, fme2Background);

        fme1Button.SetActive(ConfigData.FME1Time > 0);
        fme2Button.SetActive(ConfigData.FME2Time > 0);

        // Update total time display
        if (totalTimeText != null)
        {
            totalTimeText.text = $"Total: {totalTime}m / 60m";
            totalTimeText.color = (totalTime == 60) ? Color.green : Color.yellow;
        }

        // Validate all constraints
        bool isTotalValid = (totalTime == 60);
        bool fme1Valid = (ConfigData.FME1Time == 0) || (ConfigData.FME1Time > 0 && selectedFME1Index != -1);
        bool fme2Valid = (ConfigData.FME2Time == 0) || (ConfigData.FME2Time > 0 && selectedFME2Index != -1);
        bool fmeDifferent = (selectedFME1Index == -1 || selectedFME2Index == -1) || (selectedFME1Index != selectedFME2Index);

        // Update error message
        if (errorText != null)
        {
            string errorMsg = "";

            if (!isTotalValid)
            {
                errorMsg = $"Total must be 60m. Currently: {totalTime}m";
            }
            else if (!fme1Valid)
            {
                errorMsg = "FME1 has time but knob not selected";
            }
            else if (!fme2Valid)
            {
                errorMsg = "FME2 has time but knob not selected";
            }
            else if (!fmeDifferent)
            {
                errorMsg = "FME1 and FME2 cannot be the same knob";
            }

            if (errorMsg == "")
            {
                errorText.text = "";
            }
            else
            {
                errorText.text = errorMsg;
                errorText.color = Color.red;
            }
        }

        // Disable exit button if any validation fails
        bool isValid = isTotalValid && fme1Valid && fme2Valid && fmeDifferent;
        if (exit != null)
        {
            exit.interactable = isValid;
        }
    }

    // void UpdateMechanismDisplay(string mechName, int timeValue, TextMeshProUGUI timeText, Image background)
    // {
    //     if (timeText == null) return;

    //     // Update time display
    //     timeText.text = $"{timeValue} min";

    //     // Check if ROM file exists for this mechanism
    //     bool hasRomFile = HasRomFile(mechName);

    //     // Change background color based on ROM availability
    //     if (background != null)
    //     {
    //         if (hasRomFile)
    //         {
    //             // Green background if ROM file with AROM values exists
    //             background.color = new Color(0.2f, 0.8f, 0.3f); // Green
    //         }
    //         else
    //         {
    //             // White background if no AROM values or FME mechanism
    //             background.color = Color.white;
    //         }
    //     }
    // }

    void UpdateMechanismDisplay(string mechName, int timeValue, TextMeshProUGUI timeText, Image background)
    {
        if (timeText == null) return;

        // Update time display
        timeText.text = $"{timeValue} min";

        // Check if ROM file exists for this mechanism
        bool hasRomFile = HasRomFile(mechName);
        
        // Check if mechanism has time allocated
        bool hasTime = timeValue > 0;

        // Change background color based on ROM availability and time allocation
        if (background != null)
        {
            if (hasRomFile)
            {
                // Has AROM values
                if (hasTime)
                {
                    // Has AROM AND has time - bright green
                    background.color = new Color(0.2f, 0.8f, 0.3f); // Bright green
                }
                else
                {
                    // Has AROM but NO time - light green
                    background.color = new Color(0.6f, 0.9f, 0.6f); // Light green
                }
            }
            else
            {
                // No AROM values (or FME mechanisms)
                background.color = Color.white;
            }
        }
    }

    bool HasRomFile(string mechName)
    {
        // FME mechanisms don't require ROM files
        if (mechName == "FME1" || mechName == "FME2")
            return true;

        // Check if ROM file exists
        string romFile = DataManager.GetRomFileName(mechName);
        if (!System.IO.File.Exists(romFile))
            return false;

        // File exists, check if it contains valid AROM data
        try
        {
            DataTable romData = DataManager.loadCSV(romFile);
            if (romData == null || romData.Rows.Count == 0)
                return false;

            // Check the last row for valid AROM values
            DataRow lastRow = romData.Rows[romData.Rows.Count - 1];

            float aromMin = float.Parse(lastRow.Field<string>("AromMin"));
            float aromMax = float.Parse(lastRow.Field<string>("AromMax"));

            // AROM must have non-zero values
            bool hasArom = (aromMin != 0 || aromMax != 0);

            return hasArom;
        }
        catch
        {
            return false;
        }
    }

void SelectFMEImage(int index)
{
    if (selectingForFME == 1)
    {
        if (index == selectedFME2Index)
        {
            Debug.Log("Already used in FME2");
            return;
        }

        selectedFME1Index = index;
        ConfigData.SetFME1(index); // 🔥 STORE HERE
        ApplyFMESprite(1, index);
    }
    else if (selectingForFME == 2)
    {
        if (index == selectedFME1Index)
        {
            Debug.Log("Already used in FME1");
            return;
        }

        selectedFME2Index = index;
        ConfigData.SetFME2(index); // 🔥 STORE HERE
        ApplyFMESprite(2, index);
    }

    ClosePopup();
}
    void ApplyFMESprite(int fme, int index)
    {
        if (index < 0 || index >= knobs.Length)
        {
            Debug.LogWarning($"ApplyFMESprite: Invalid index {index} for FME{fme}");
            return;
        }

        try
        {
            if (fme == 1)
            {
                if (fme1PreviewImage != null)
                    fme1PreviewImage.GetComponent<Image>().sprite = knobs[index];
            }
            else if (fme == 2)
            {
                if (fme2PreviewImage != null)
                    fme2PreviewImage.GetComponent<Image>().sprite = knobs[index];
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ApplyFMESprite error: {ex.Message}");
        }
    }

void UpdatePopupAvailability()
{
    for (int i = 0; i < imageGridContainer.childCount; i++)
    {
        Transform child = imageGridContainer.GetChild(i);
        Button btn = child.GetComponent<Button>();
        Image img = child.GetComponent<Image>();

        // Disable duplicate selection
        if ((selectingForFME == 1 && i == selectedFME2Index) ||
            (selectingForFME == 2 && i == selectedFME1Index))
        {
            btn.interactable = false;
            if (img != null) img.color = Color.gray;
        }
        else
        {
            btn.interactable = true;

            // Highlight selected
            if ((selectingForFME == 1 && i == selectedFME1Index) ||
                (selectingForFME == 2 && i == selectedFME2Index))
            {
                if (img != null) img.color = Color.green;
            }
            else
            {
                if (img != null) img.color = Color.white;
            }
        }
    }
}


    // =========================
    // EXISTING LOGIC (UNCHANGED)
    // =========================

    private void AttachCallbacks()
    {
        PlutoComm.OnButtonReleased += OnPlutoButtonReleased;

        exit.onClick.AddListener(OnExitButtonClicked);
        // nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void UpdateMechanismToggleButtons()
    {
        foreach (Transform child in mehcanismSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            toggleComponent.interactable = true;
            toggleComponent.gameObject.SetActive(true);
        }
    }

    IEnumerator DelayedAttachListeners()
    {
        yield return new WaitForSeconds(0.3f);
        AttachToggleListeners();
    }

    void AttachToggleListeners()
    {
        foreach (Transform child in mehcanismSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null)
            {
                toggleComponent.onValueChanged.AddListener(delegate { CheckToggleStates(); });
            }
        }
    }

    void CheckToggleStates()
    {
        foreach (Transform child in mehcanismSelectGroup.transform)
        {
            Toggle toggleComponent = child.GetComponent<Toggle>();
            if (toggleComponent != null && toggleComponent.isOn)
            {
                mechSelected = child.name;
                AppData.Instance.SetMechanism(mechSelected);
                StartCoroutine(MoveToNextScene());
                return;
            }
        }
        mechSelected = null;
        AppData.Instance.SetMechanism(null);
    }

    private void OnPlutoButtonReleased()
    {
        if (mechSelected != null)
        {
            changeScene = true;
            mechSelected = null;
        }
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator MoveToNextScene()
    {
        yield return new WaitForSeconds(0.15f);
        LoadNextScene();
        mechSelected = null;
    }

    private void OnNextButtonClicked()
    {
        if (selectedFME1Index == -1 || selectedFME2Index == -1)
        {
            Debug.LogError("Select both FME1 and FME2");
            return;
        }

        if (selectedFME1Index == selectedFME2Index)
        {
            Debug.LogError("FME1 and FME2 cannot be same");
            return;
        }


        if (mechSelected != null)
        {
            LoadNextScene();
            mechSelected = null;
        }
    }
    public void setTime()
    {
        SceneManager.LoadScene("SETDUR");
    }

private void OnExitButtonClicked()
{
    // Check if FME1 has time but no knob selected
    if (ConfigData.FME1Time > 0 && selectedFME1Index == -1)
    {
        Debug.LogError("FME1 has time allocated but knob not selected");
        if (errorText != null)
        {
            errorText.text = "FME1 has time but knob not selected";
            errorText.color = Color.red;
        }
        return;
    }

    // Check if FME2 has time but no knob selected
    if (ConfigData.FME2Time > 0 && selectedFME2Index == -1)
    {
        Debug.LogError("FME2 has time allocated but knob not selected");
        if (errorText != null)
        {
            errorText.text = "FME2 has time but knob not selected";
            errorText.color = Color.red;
        }
        return;
    }

    // Only check that the FME with time has its knob selected
    // If FME1 has time, only require FME1 knob; if FME2 has time, only require FME2 knob
    // If both have time (shouldn't happen with new rules), then both are required
    if (ConfigData.FME1Time > 0 && ConfigData.FME2Time > 0)
    {
        // Both have time - require both knobs
        if (selectedFME1Index == -1 || selectedFME2Index == -1)
        {
            Debug.LogError("Both FME1 and FME2 have time but one or both knobs not selected");
            if (errorText != null)
            {
                errorText.text = "Select both FME1 and FME2 knobs";
                errorText.color = Color.red;
            }
            return;
        }
    }
    else if (ConfigData.FME1Time > 0)
    {
        // Only FME1 has time - only require FME1 knob
        if (selectedFME1Index == -1)
        {
            Debug.LogError("FME1 has time but knob not selected");
            if (errorText != null)
            {
                errorText.text = "Select FME1 knob";
                errorText.color = Color.red;
            }
            return;
        }
    }
    else if (ConfigData.FME2Time > 0)
    {
        // Only FME2 has time - only require FME2 knob
        if (selectedFME2Index == -1)
        {
            Debug.LogError("FME2 has time but knob not selected");
            if (errorText != null)
            {
                errorText.text = "Select FME2 knob";
                errorText.color = Color.red;
            }
            return;
        }
    }

    // Check if both knobs are selected (only matters if both have time)
    if (selectedFME1Index != -1 && selectedFME2Index != -1 && selectedFME1Index == selectedFME2Index)
    {
        Debug.LogError("FME1 and FME2 cannot be same");
        if (errorText != null)
        {
            errorText.text = "FME1 and FME2 cannot be the same knob";
            errorText.color = Color.red;
        }
        return;
    }

    // Validate total time is exactly 60 minutes
    int totalTime = ConfigData.WFE + ConfigData.WURD + ConfigData.FPS + ConfigData.HOC + ConfigData.FME1Time + ConfigData.FME2Time;
    if (totalTime != 60)
    {
        Debug.LogError($"Total mechanism time must be 60m. Currently: {totalTime}m");
        if (errorText != null)
        {
            errorText.text = $"Total must be 60m. Currently: {totalTime}m";
            errorText.color = Color.red;
        }
        return;
    }

    if (mechSelected != null)
    {
        LoadNextScene();
        mechSelected = null;
    }
    ConfigData.SaveToConfig(DataManager.configFile);
    AppData.isPlanSetup = false;
    SceneManager.LoadScene("CHMECH");
}

    private void OnDestroy()
    {
        if (ConnectToRobot.isPLUTO)
        {
            PlutoComm.OnButtonReleased -= OnPlutoButtonReleased;
        }
    }
}
