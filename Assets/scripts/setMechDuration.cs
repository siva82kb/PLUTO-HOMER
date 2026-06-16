using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using System.Data;
using System.Linq;

public class MechanismDurationHandler : MonoBehaviour
{
    [System.Serializable]
    public class MechSlider
    {
        public string mechName;
        public Slider slider;
        public TMP_Text valueText;
        public GameObject root;
        public Image fillImage;
    }

    [System.Serializable]
    public class MechCheckbox
    {
        public string mechName;
        public Toggle checkbox;
        public GameObject root;
        public Text labelText; // Add this field for the checkbox label
    }

    [Header("Mechanism Selection")]
    public List<MechCheckbox> mechCheckboxes;

    [Header("Sliders")]
    public List<MechSlider> mechSliders;

    [Header("UI")]
    public TMP_Text remainingTimeText;
    public TMP_Text summaryText;
    public TMP_Text totalTimeText;
    public GameObject nextButton;
    public TMP_Text errorText;

    private const int MIN_DURATION = 10;
    private const int TOTAL_TIME = 60;
    private const int MAX_PER_MECH = 40;
    private const int MIN_MECHS = 3;
    private HashSet<string> assessedMechs = new HashSet<string>();
    private HashSet<string> selectedMechs = new HashSet<string>();
    private bool isUpdating = false;
    private string lastChangedMech = ""; // Track which slider was last changed by user

    void Start()
    {
        try
        {
            // Load config data
            ConfigData.LoadFromConfig(DataManager.configFile);

            // Safety checks
            if (mechCheckboxes == null || mechCheckboxes.Count == 0)
            {
                Debug.LogError("MechanismDurationHandler: mechCheckboxes not assigned in inspector!");
                return;
            }

            if (mechSliders == null || mechSliders.Count == 0)
            {
                Debug.LogError("MechanismDurationHandler: mechSliders not assigned in inspector!");
                return;
            }

            FindAssessedMechanisms();
            LoadValuesFromConfig();
            InitializeCheckboxes();
            InitializeSliders();
            UpdateUI();

            // Ensure button starts disabled until constraints are met
            if (nextButton != null)
            {
                Button btn = nextButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = false;
                }
                else
                {
                    nextButton.SetActive(false);
                }
            }else{
                Debug.LogError("MechanismDurationHandler: nextButton not assigned in inspector!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"MechanismDurationHandler.Start() error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // =========================
    // FIND ASSESSED MECHANISMS
    // =========================
    void FindAssessedMechanisms()
    {
        assessedMechs.Clear();

        // Check which mechanisms have ROM data (have been assessed)
        foreach (var mech in mechSliders)
        {
            string romFile = DataManager.GetRomFileName(mech.mechName);
            if (File.Exists(romFile))
            {
                DataTable romData = DataManager.loadCSV(romFile);
                // Check if this mechanism has assessment data (AROM, PROM, APROM all set)
                if (romData.Rows.Count > 0)
                {
                    DataRow lastRow = romData.Rows[romData.Rows.Count - 1];
                    float aromMin = float.Parse(lastRow.Field<string>("AromMin"));
                    float aromMax = float.Parse(lastRow.Field<string>("AromMax"));
                    float promMin = float.Parse(lastRow.Field<string>("PromMin"));
                    float promMax = float.Parse(lastRow.Field<string>("PromMax"));
                    float apromMin = float.Parse(lastRow.Field<string>("APromMin"));
                    float apromMax = float.Parse(lastRow.Field<string>("APromMax"));

                    // Mark as assessed if all ROM types have been set
                    if ((aromMin != 0 || aromMax != 0) &&
                        (promMin != 0 || promMax != 0) &&
                        (apromMin != 0 || apromMax != 0))
                    {
                        assessedMechs.Add(mech.mechName);
                    }
                }
            }
        }
    }

    // =========================
    // INITIALIZE CHECKBOXES
    // =========================
    void InitializeCheckboxes()
    {
        if (mechCheckboxes == null || mechCheckboxes.Count == 0)
            return;

        selectedMechs.Clear();

        // First pass: load previously selected and enable checkboxes
        foreach (var mechCb in mechCheckboxes)
        {
            if (mechCb == null || mechCb.checkbox == null || mechCb.root == null)
            {
                Debug.LogWarning("MechanismDurationHandler: Checkbox element is not properly configured");
                continue;
            }

            bool isAssessed = assessedMechs.Contains(mechCb.mechName);
            bool isFME = mechCb.mechName == "FME1" || mechCb.mechName == "FME2";

            // Enable checkbox only if mechanism is assessed OR if it's FME1/FME2
            mechCb.checkbox.interactable = isAssessed || isFME;

            // Auto-select mechanisms that have a previous duration set
            int previousDuration = GetConfigValue(mechCb.mechName);
            bool shouldSelect = previousDuration > 0;
            mechCb.checkbox.isOn = shouldSelect;

            // Add to selectedMechs if auto-selected
            if (shouldSelect)
                selectedMechs.Add(mechCb.mechName);

            // Visual feedback - find label text first
            if (mechCb.labelText == null)
            {
                // Try to find the label text if not assigned
                mechCb.labelText = mechCb.checkbox.GetComponentInChildren<Text>();
            }

            // Update label color based on initial state
            UpdateCheckboxLabelColor(mechCb, shouldSelect);

            // Add listener for checkbox changes
            mechCb.checkbox.onValueChanged.AddListener((isChecked) =>
            {
                OnMechanismToggled(mechCb.mechName, isChecked);
                UpdateCheckboxLabelColor(mechCb, isChecked);
            });

            mechCb.root.SetActive(true);
        }
    }

    // New method to update checkbox label color
    void UpdateCheckboxLabelColor(MechCheckbox mechCb, bool isChecked)
    {
        if (mechCb.labelText == null)
        {
            // Try to get the label text component if it's not assigned
            mechCb.labelText = mechCb.checkbox.GetComponentInChildren<Text>();
        }
        
        if (mechCb.labelText != null)
        {
            // Change label color to green if checkbox is on, otherwise set to default color
            if (isChecked)
            {
                mechCb.labelText.color = Color.green;
            }
            else
            {
                // Set to appropriate color based on interactability
                mechCb.labelText.color = mechCb.checkbox.interactable ? Color.white : Color.gray;
            }
        }
        else
        {
            Debug.LogWarning($"Could not find label text for checkbox: {mechCb.mechName}");
        }
    }

    void OnMechanismToggled(string mechName, bool isSelected)
    {
        // Prevent selecting more than 3 mechanisms
        if (isSelected && selectedMechs.Count >= 3 && !selectedMechs.Contains(mechName))
        {
            Debug.LogWarning("Cannot select more than 3 mechanisms");
            // Uncheck the checkbox to revert the selection
            foreach (var mechCb in mechCheckboxes)
            {
                if (mechCb != null && mechCb.mechName == mechName && mechCb.checkbox != null)
                {
                    mechCb.checkbox.isOn = false;
                    return;
                }
            }
            return;
        }

        foreach (var mech in mechSliders)
        {
            if (mech == null || mech.slider == null)
                continue;

            if (mech.mechName == mechName)
            {
                if (isSelected)
                {
                    // Enable the mechanism
                    selectedMechs.Add(mechName);
                    mech.slider.interactable = true;
                    mech.slider.minValue = MIN_DURATION;
                    mech.slider.maxValue = MAX_PER_MECH;

                    // Set to minimum if currently 0
                    if (mech.slider.value < MIN_DURATION)
                        mech.slider.value = MIN_DURATION;
                }
                else
                {
                    // Disable the mechanism
                    selectedMechs.Remove(mechName);
                    mech.slider.value = 0;
                    mech.slider.interactable = false;
                    mech.slider.minValue = 0;
                    mech.slider.maxValue = 0;
                }
                break;
            }
        }

        UpdateUI();
    }

    // =========================
    // LOAD VALUES FROM CONFIG
    // =========================
    void LoadValuesFromConfig()
    {
        // Values are loaded in InitializeSliders, this ensures proper order
        // (set minValue/maxValue first, then set value)
    }

    int GetConfigValue(string mechName)
    {
        switch (mechName)
        {
            case "WFE": return ConfigData.WFE;
            case "WURD": return ConfigData.WURD;
            case "FPS": return ConfigData.FPS;
            case "HOC": return ConfigData.HOC;
            case "FME1": return ConfigData.FME1Time;
            case "FME2": return ConfigData.FME2Time;
            default: return 0;
        }
    }

    void SetConfigValue(string mechName, int value)
    {
        switch (mechName)
        {
            case "WFE": ConfigData.WFE = value; break;
            case "WURD": ConfigData.WURD = value; break;
            case "FPS": ConfigData.FPS = value; break;
            case "HOC": ConfigData.HOC = value; break;
            case "FME1": ConfigData.FME1Time = value; break;
            case "FME2": ConfigData.FME2Time = value; break;
        }
    }

    // =========================
    // INIT SLIDERS
    // =========================
    void InitializeSliders()
    {
        if (mechSliders == null || mechSliders.Count == 0)
            return;

        foreach (var mech in mechSliders)
        {
            if (mech == null || mech.slider == null)
                continue;

            mech.slider.wholeNumbers = true;
            mech.root.SetActive(true); // Always show all sliders

            bool isSelected = selectedMechs.Contains(mech.mechName);

            if (isSelected)
            {
                // Selected mechanisms are interactive with minimum of 10 minutes
                mech.slider.interactable = true;
                mech.slider.minValue = MIN_DURATION; // Minimum 10 minutes
                mech.slider.maxValue = MAX_PER_MECH;
            }
            else
            {
                // Non-selected mechanisms are disabled (read-only, grayed out)
                mech.slider.interactable = false;
                mech.slider.minValue = 0;
                mech.slider.maxValue = 0;
            }

            int currentValue = GetConfigValue(mech.mechName);
            // Clamp value to minimum if selected
            if (isSelected && currentValue < MIN_DURATION)
                currentValue = MIN_DURATION;
            mech.slider.value = currentValue;

            // Add listener only once (listeners persist across calls)
            if (mech.slider.onValueChanged.GetPersistentEventCount() == 0)
            {
                string mechName = mech.mechName; // Capture mechName in closure
                mech.slider.onValueChanged.AddListener((v) =>
                {
                    lastChangedMech = mechName; // Track which slider was changed
                    UpdateUI();
                });
            }
        }
    }

    // =========================
    // UI UPDATE
    // =========================
    void UpdateUI()
    {
        if (isUpdating) return;
        isUpdating = true;

        if (mechSliders == null || mechSliders.Count == 0)
        {
            isUpdating = false;
            return;
        }

        int total = 0;
        int selectedCount = selectedMechs.Count;

        // Calculate total and update display for all sliders
        foreach (var mech in mechSliders)
        {
            if (mech == null || mech.slider == null)
                continue;

            bool isSelected = selectedMechs.Contains(mech.mechName);

            int value = (int)mech.slider.value;

            // Only calculate total for selected mechanisms
            if (isSelected)
            {
                total += value;
            }

            // Update text for all sliders
            if (mech.valueText != null)
                mech.valueText.text = value.ToString();

            // Color coding: selected = #DA4469 pink, non-selected = dark gray
            if (mech.fillImage != null)
            {
                if (isSelected)
                    mech.fillImage.color = new Color(0.855f, 0.267f, 0.412f); // #DA4469 pink for selected
                else
                    mech.fillImage.color = Color.gray; // Dark gray for non-selected
            }
        }

        // NOTE: We don't dynamically adjust maxValue to avoid slider clamping cascades.
        // Instead, we rely on visual feedback (colors, error messages) to guide the user
        // about constraints. All selected sliders keep maxValue = 40.

        // Update UI text
        if (remainingTimeText != null)
        {
            remainingTimeText.text = $"Total: {total}m / {TOTAL_TIME}m";
            remainingTimeText.color = (total <= TOTAL_TIME) ? new Color(0.2f, 0.8f, 0.3f) : Color.red;
        }

        if (totalTimeText != null)
        {
            totalTimeText.text = $"Total: {total}m / {TOTAL_TIME}m";
            totalTimeText.color = (total == TOTAL_TIME) ? Color.green : Color.yellow;
        }

        // Check constraints
        bool isValid = ValidateConstraints(total, selectedCount);
        if (nextButton != null)
        {
            Button btn = nextButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = isValid;
            }
            else
            {
                // If button component not found, disable the GameObject itself
                nextButton.SetActive(isValid);
            }
        }

        UpdateSummary(total, selectedCount);
        isUpdating = false;
    }

    // =========================
    // VALIDATE CONSTRAINTS
    // =========================
    bool ValidateConstraints(int total, int selectedCount)
    {
        if (selectedCount < MIN_MECHS)
        {
            if (errorText != null)
                errorText.text = $"Select at least {MIN_MECHS} mechanisms. Currently: {selectedCount}";
            return false;
        }

        foreach (var mech in mechSliders)
        {
            if (mech == null || mech.slider == null)
                continue;

            if (selectedMechs.Contains(mech.mechName))
            {
                int value = (int)mech.slider.value;

                // Check minimum (at least 1 minute)
                if (value <= 0)
                {
                    if (errorText != null)
                        errorText.text = $"{mech.mechName} must be at least 1m";
                    return false;
                }

                // Check maximum (max 40 minutes per mechanism)
                if (value > MAX_PER_MECH)
                {
                    if (errorText != null)
                        errorText.text = $"{mech.mechName} maximum is {MAX_PER_MECH}m";
                    return false;
                }
            }
        }

        if (total != TOTAL_TIME)
        {
            if (errorText != null)
                errorText.text = $"Total must be {TOTAL_TIME}m. Currently: {total}m";
            return false;
        }

        if (errorText != null)
            errorText.text = "";
        return true;
    }


    // =========================
    // SUMMARY
    // =========================
    void UpdateSummary(int total, int selectedCount)
    {
        string txt = $"Selected: {selectedCount}/{MIN_MECHS}\n";
        txt += $"Total: {total}m / {TOTAL_TIME}m\n\n";

        foreach (var mech in mechSliders)
        {
            if (mech == null || mech.slider == null)
                continue;

            if (selectedMechs.Contains(mech.mechName))
            {
                int val = (int)mech.slider.value;
                // txt += $"{mech.mechName}: {val}m\n";
            }
        }

        if (summaryText != null)
            summaryText.text = txt;
    }

    // =========================
    // CONFIRM
    // =========================
    public void OnConfirmClick()
    {
        int total = 0;

        foreach (var mech in mechSliders)
        {
            if (selectedMechs.Contains(mech.mechName))
            {
                int val = (int)mech.slider.value;
                total += val;
            }
        }
        ConfigData.TotalTime = TOTAL_TIME;

        if (!ValidateConstraints(total, selectedMechs.Count))
        {
            return;
        }

        // Save all selected mechanisms
        foreach (var mech in mechSliders)
        {
            if (selectedMechs.Contains(mech.mechName))
            {
                int val = (int)mech.slider.value;
                SetConfigValue(mech.mechName, val);
                Debug.Log($"Saving: {mech.mechName} = {val}m");
            }
            else
            {
                // Reset non-selected mechanisms to 0
                SetConfigValue(mech.mechName, 0);
            }
        }

        // ConfigData.SaveToConfig(DataManager.configFile);
        SceneManager.LoadScene("PLANSETUP");
    }
    public void OnBackClick()
    {
        // Don't save, just go back to PLANSETUP
        SceneManager.LoadScene("PLANSETUP");
    }

    private void OnDestroy()
    {
        // Cleanup listeners to prevent memory leaks
        if (mechCheckboxes != null)
        {
            foreach (var mechCb in mechCheckboxes)
            {
                if (mechCb != null && mechCb.checkbox != null)
                {
                    mechCb.checkbox.onValueChanged.RemoveAllListeners();
                }
            }
        }

        if (mechSliders != null)
        {
            foreach (var mech in mechSliders)
            {
                if (mech != null && mech.slider != null)
                {
                    mech.slider.onValueChanged.RemoveAllListeners();
                }
            }
        }
    }
}