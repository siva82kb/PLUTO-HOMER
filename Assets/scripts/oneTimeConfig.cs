using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Data;
using System.Globalization;
using SimpleJSON; // Make sure you have SimpleJSON in your project

public class OneTimeConfig : MonoBehaviour
{
    // ... (keep all your existing variable declarations)
    public TMP_InputField homerIdField;
    public TMP_InputField startDateField;
    public TMP_InputField endDateField;
    public TMP_Dropdown groupField;
    public TMP_InputField wfeField;
    public TMP_InputField wurdField;
    public TMP_InputField fpsField;
    public TMP_InputField hocField;
    
    // FME1 components - Time input + Image selection
    public TMP_InputField fme1TimeField;
    public Image fme1PreviewImage;
    public Button fme1SelectButton;
    
    // FME2 components - Time input + Image selection
    public TMP_InputField fme2TimeField;
    public Image fme2PreviewImage;
    public Button fme2SelectButton;
    
    // Popup components
    public GameObject imageSelectionPopup;
    public Transform imageGridContainer;
    public GameObject imageButtonPrefab;
    public TextMeshProUGUI popupTitle;
    
    // Text displays for selected images
    public TextMeshProUGUI fme1SelectedText, fme2SelectedText, loginButtonText;
    
    // Sprites for the 12 mechanism images
    public Sprite[] mechanismSprites = new Sprite[12];
    
    public TMP_Dropdown affectedSideDropdown;
    public TMP_Dropdown location;

    public TextMeshProUGUI totalDurationText;
    public TMP_Text msg;

    private int selectedFME1Index = -1;
    private int selectedFME2Index = -1;
    private int selectingForFME = 0;
    private Color defaultPreviewColor = new Color(1, 1, 1, 0.3f);
    private Color selectedPreviewColor = Color.black;
    private DateTime startDate, endDate;

    // Verification Panel - NEW
    public GameObject verifyPanel;
    public GameObject popUpPanel;
    public TMP_Dropdown verifyLocation;
    public TMP_InputField HOMERID;
    public TextMeshProUGUI popUpConfirmationPatientID;
    public TextMeshProUGUI messageText;
    public Button popupOk;
    public Button popupCancel;
    public Button verifyButton;

    // AWS Configuration - NEW
    private string awsBucketName = "homerclouds";
    private string homerDetailsFileName = "homerIdDetails.json";
    private string awsProfile = "default"; // AWS CLI profile

    // Patient Data - NEW
    private string currentPatientID;
    private string currentLocation;
    private string currentTrainingSide;

    private void Start()
    {
        // Initialize verification panel (hidden by default)
        if (verifyPanel != null)
            verifyPanel.SetActive(false);
        if (popUpPanel != null)
            popUpPanel.SetActive(false);

        // Automatically set startDateField and endDateField
        startDate = DateTime.Now;
        endDate = startDate.AddDays(30);

        if (File.Exists(DataManager.configFile))
        {
            LoadExistingConfig();
        }
        else
        {
            InitializePreviewImages();
            verifyPanel.SetActive(true);

        }
        
        startDateField.text = startDate.ToString("dd-MM-yyyy");
        endDateField.text = endDate.ToString("dd-MM-yyyy");

        // Add listeners
        wfeField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        wurdField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        fpsField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        hocField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        fme1TimeField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        fme2TimeField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });

        fme1SelectButton.onClick.AddListener(() => OpenImageSelectionPopup(1));
        fme2SelectButton.onClick.AddListener(() => OpenImageSelectionPopup(2));
        
        InitializeImageSelectionPopup();
        imageSelectionPopup.SetActive(false);

        // Add verify button listener - NEW
        if (verifyButton != null){
            verifyButton.onClick.AddListener(OnVerifyButtonClick);
            Debug.Log($"Verify ButtonInitialized");
            }
        if (popupOk != null)
            popupOk.onClick.AddListener(OnPopupOkClick);
        if (popupCancel != null)
            popupCancel.onClick.AddListener(OnPopupCancelClick);
    }

    // Called when verify button is clicked
private void OnVerifyButtonClick()
{
    Debug.Log("Verify button clicked");
    
    if (HOMERID == null)
    {
        Debug.LogError("HOMERID is not assigned in the Inspector!");
        return;
    }
    
    if (string.IsNullOrWhiteSpace(HOMERID.text))
    {
        Debug.Log("Homer ID is empty");
        msg.text = "Please enter Homer ID";
        return;
    }

    Debug.Log($"Homer ID entered: {HOMERID.text}");
    
    currentPatientID = HOMERID.text;
    currentLocation = location.options[location.value].text;
    currentTrainingSide = affectedSideDropdown.options[affectedSideDropdown.value].text;

    Debug.Log($"Current Patient ID: {currentPatientID}, Location: {currentLocation}, Side: {currentTrainingSide}");

    if (homerIdField == null)
    {
        Debug.LogError("HOMERID TextMeshProUGUI is not assigned!");
    }
    else
    {
        homerIdField.text = currentPatientID;
    }

    if (verifyPanel == null)
    {
        Debug.LogError("verifyPanel is not assigned!");
    }
    else
    {
        verifyPanel.SetActive(true);
        Debug.Log("Verify panel activated");
    }
    
    // Start verification process
    StartCoroutine(VerifyHomerID(currentPatientID, currentLocation));
}
    // NEW: Coroutine to verify HomerID from AWS
    private IEnumerator VerifyHomerID(string homerID, string location)
    {
        messageText.text = "Verifying HomerID...";
        
        // Construct S3 path
        string s3Path = $"s3://{awsBucketName}/{location}/HOCMCV231/Pluto/{homerDetailsFileName}";
        
        // Download file from S3 using AWS CLI
        string tempFilePath = Path.Combine(Application.temporaryCachePath, "HomerDetails_temp.json");
        
        // Use AWS CLI to download the file
        string arguments = $"s3 cp {s3Path} \"{tempFilePath}\" --profile {awsProfile}";
        
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = "aws";
        startInfo.Arguments = arguments;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        using (System.Diagnostics.Process process = new System.Diagnostics.Process())
        {
            process.StartInfo = startInfo;
            process.Start();
            
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            yield return null;

            if (process.ExitCode != 0)
            {
                Debug.LogError($"AWS CLI Error: {error}");
                messageText.text = "Error connecting to AWS. Check internet connection.";
                yield break;
            }
        }

        // Check if file was downloaded successfully
        if (File.Exists(tempFilePath))
        {
            string jsonContent = File.ReadAllText(tempFilePath);
            ProcessHomerDetails(jsonContent, homerID);
            
            // Clean up temp file
            File.Delete(tempFilePath);
        }
        else
        {
            messageText.text = $"Could not find HomerDetails for location: {location}";
        }
    }

    // NEW: Process the HomerDetails JSON - FIXED VERSION
    private void ProcessHomerDetails(string jsonContent, string searchHomerID)
    {
        var json = JSON.Parse(jsonContent);
        
        if (json == null || json["details"] == null)
        {
            messageText.text = "Invalid HomerDetails format";
            return;
        }

        var details = json["details"].AsArray;
        bool found = false;

        // FIX: Correct way to iterate through JSON array in SimpleJSON
        for (int i = 0; i < details.Count; i++)
        {
            var item = details[i];
            string homerID = item["homerID"];
            string hospID = item["hospitalId"];
            if (homerID == searchHomerID)
            {
                found = true;
                
                // Check status for Pluto
                var status = item["status"];
                bool isActive = false;
                
                if (status != null && !status.IsNull)
                {
                    // Check if status is an object with "pluto" field
                    if (status["pluto"] != null && !status["pluto"].IsNull)
                    {
                        isActive = status["pluto"].Value.ToLower() == "active";
                    }
                    // Check if status is directly a string
                    else if (status.IsString)
                    {
                        isActive = status.Value.ToLower() == "active";
                    }
                    // Check if status is an object (like in your JSON structure)
                    else if (status.IsObject)
                    {
                        // Check if "pluto" exists in the status object
                        var plutoStatus = status["pluto"];
                        if (plutoStatus != null && !plutoStatus.IsNull)
                        {
                            isActive = plutoStatus.Value.ToLower() == "active";
                        }
                    }
                }

                if (isActive)
                {
                    // Already activated
                    messageText.text = $"HomerID {searchHomerID} is already activated. Cannot assign to new patient.";
                    popUpPanel.SetActive(false);
                }
                else
                {
                    // Not activated - show popup with patient ID
                    popUpConfirmationPatientID.text = $"HomerID : {searchHomerID} is assigned to Patient id: {hospID}, Are you sure?";;
                    messageText.text = "";
                    popUpPanel.SetActive(true);
                }
                break;
            }
        }

        if (!found)
        {
            messageText.text = $"HomerID {searchHomerID} not found in the system";
        }
    }

    // NEW: Called when OK button is clicked in popup
    private void OnPopupOkClick()
    {
        popUpPanel.SetActive(false);
        verifyPanel.SetActive(false);
        
        // Set the saved values back to fields
        homerIdField.text = currentPatientID;
        affectedSideDropdown.value = GetDropdownIndexForSide(currentTrainingSide);
        location.value = GetDropdownIndexForLocation(currentLocation);
        
        // Proceed to configuration scene
        // saveConfig();
    }

    // NEW: Called when Cancel button is clicked in popup
    private void OnPopupCancelClick()
    {
        popUpPanel.SetActive(false);
        verifyPanel.SetActive(true);
        
        // Clear fields
        homerIdField.text = "";
        messageText.text = "Verification cancelled";
    }

    // NEW: Helper to get dropdown index for training side
    private int GetDropdownIndexForSide(string side)
    {
        for (int i = 0; i < affectedSideDropdown.options.Count; i++)
        {
            if (affectedSideDropdown.options[i].text.ToLower() == side.ToLower())
                return i;
        }
        return 0;
    }

    // NEW: Helper to get dropdown index for location
    private int GetDropdownIndexForLocation(string loc)
    {
        for (int i = 0; i < location.options.Count; i++)
        {
            if (location.options[i].text.ToLower() == loc.ToLower())
                return i;
        }
        return 0;
    }

    // Modified: Load existing config method
    private void LoadExistingConfig()
    {
        DataTable configData = DataManager.loadCSV(DataManager.configFile);
        DataRow lastRow = configData.Rows[configData.Rows.Count - 1];
        
        string hospNumber = lastRow.Field<string>("HomerID");
        bool rightHand = lastRow.Field<string>("TrainingSide") == "right";
        int FME1 = int.Parse(lastRow.Field<string>("FME1K"));
        int FME2 = int.Parse(lastRow.Field<string>("FME2K"));
        endDate = DateTime.ParseExact(lastRow.Field<string>("endDate"), "dd-MM-yyyy", CultureInfo.InvariantCulture);

        homerIdField.text = hospNumber;
        affectedSideDropdown.value = rightHand ? 0 : 1;
        location.value = GetDropdownIndexForLocation(lastRow.Field<string>("Location"));
    
        wfeField.text = lastRow.Field<string>("WFE");
        wurdField.text = lastRow.Field<string>("WURD");
        fpsField.text = lastRow.Field<string>("FPS");
        hocField.text = lastRow.Field<string>("HOC");
        fme1TimeField.text = lastRow.Field<string>("FME1");
        fme2TimeField.text = lastRow.Field<string>("FME2");
        totalDurationText.text = lastRow.Field<string>("TotalTime");

        loginButtonText.text = "Login";

        if (fme1PreviewImage != null && FME1 < mechanismSprites.Length && FME1 >= 0)
        {
            selectedFME1Index = FME1;
            fme1PreviewImage.sprite = mechanismSprites[FME1];
            fme1PreviewImage.color = selectedPreviewColor;
            if (fme1SelectedText != null)
                fme1SelectedText.text = FME1 >= 0 ? $"Selected: Knob {FME1 + 1}" : "Click to select FME1 knob";
        }

        if (fme2PreviewImage != null && FME2 < mechanismSprites.Length && FME2 >= 0)
        {
            fme2PreviewImage.sprite = mechanismSprites[FME2];
            fme2PreviewImage.color = selectedPreviewColor;
            selectedFME2Index = FME2;
            if (fme2SelectedText != null)
                fme2SelectedText.text = FME2 >= 0 ? $"Selected: Knob {FME2 + 1}" : "Click to select FME2 knob";
        }
    }

    private void InitializePreviewImages()
    {
        if (fme1PreviewImage != null)
        {
            fme1PreviewImage.color = defaultPreviewColor;
            fme1PreviewImage.sprite = null;
        }
        
        if (fme2PreviewImage != null)
        {
            fme2PreviewImage.color = defaultPreviewColor;
            fme2PreviewImage.sprite = null;
        }
        
        UpdateSelectionTexts();
    }

    private void InitializeImageSelectionPopup()
    {
        if (imageGridContainer == null || imageButtonPrefab == null || mechanismSprites.Length < 12)
        {
            Debug.LogError("Missing popup components or sprites!");
            return;
        }
        
        // Clear existing buttons
        foreach (Transform child in imageGridContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create 12 image buttons
        for (int i = 0; i < 12; i++)
        {
            GameObject buttonObj = Instantiate(imageButtonPrefab, imageGridContainer);
            int index = i; // Capture index for closure
            
            // Set button image
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null && i < mechanismSprites.Length)
            {
                buttonImage.sprite = mechanismSprites[i];
            }
            
            // Add number label
            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = (i + 1).ToString();
            }
            
            // Add click listener
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectImageFromPopup(index));
            }
            
            // Add hover effect component (optional)
            AddHoverEffect(buttonObj);
        }
    }

    private void AddHoverEffect(GameObject buttonObj)
    {
        // Add hover effect script if not present
        ButtonHoverEffect hoverEffect = buttonObj.GetComponent<ButtonHoverEffect>();
        if (hoverEffect == null)
        {
            hoverEffect = buttonObj.AddComponent<ButtonHoverEffect>();
        }
    }

    public void OpenImageSelectionPopup(int forFME)
    {
        selectingForFME = forFME;
        
        // Set popup title
        if (popupTitle != null)
        {
            popupTitle.text = $"Select Image for FME{forFME}";
        }
        
        // Update button interactability based on current selections
        UpdatePopupButtonAvailability();
        
        // Show popup
        imageSelectionPopup.SetActive(true);
    }

    public void CloseImageSelectionPopup()
    {
        imageSelectionPopup.SetActive(false);
        selectingForFME = 0;
    }

    private void UpdatePopupButtonAvailability()
    {
        if (imageGridContainer == null) return;
        
        int buttonCount = imageGridContainer.childCount;
        for (int i = 0; i < buttonCount && i < 12; i++)
        {
            Transform child = imageGridContainer.GetChild(i);
            Button button = child.GetComponent<Button>();
            
            if (button != null)
            {
                // Disable button if:
                // 1. Selecting for FME1 and this image is already selected for FME2
                // 2. Selecting for FME2 and this image is already selected for FME1
                if ((selectingForFME == 1 && i == selectedFME2Index) ||
                    (selectingForFME == 2 && i == selectedFME1Index))
                {
                    button.interactable = false;
                    Image buttonImage = child.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                    }
                }
                else
                {
                    button.interactable = true;
                    Image buttonImage = child.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = Color.white;
                    }
                }
                
                // Highlight if already selected for this FME
                Image highlightImage = child.Find("Highlight")?.GetComponent<Image>();
                if (highlightImage != null)
                {
                    if ((selectingForFME == 1 && i == selectedFME1Index) ||
                        (selectingForFME == 2 && i == selectedFME2Index))
                    {
                        highlightImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        highlightImage.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void SelectImageFromPopup(int selectedIndex)
    {
        if (selectingForFME == 1)
        {
            // Check if this image is already selected for FME2
            if (selectedIndex == selectedFME2Index)
            {
                msg.text = "This image is already selected for FME2!";
                return;
            }
            
            selectedFME1Index = selectedIndex;
            
            // Update FME1 preview
            if (fme1PreviewImage != null && selectedIndex < mechanismSprites.Length)
            {
                fme1PreviewImage.sprite = mechanismSprites[selectedIndex];
                fme1PreviewImage.color = selectedPreviewColor;
            }
        }
        else if (selectingForFME == 2)
        {
            // Check if this image is already selected for FME1
            if (selectedIndex == selectedFME1Index)
            {
                msg.text = "This image is already selected for FME1!";
                return;
            }
            
            selectedFME2Index = selectedIndex;
            
            // Update FME2 preview
            if (fme2PreviewImage != null && selectedIndex < mechanismSprites.Length)
            {
                fme2PreviewImage.sprite = mechanismSprites[selectedIndex];
                fme2PreviewImage.color = selectedPreviewColor;
            }
        }
        
        msg.text = "";
        UpdateSelectionTexts();
        CloseImageSelectionPopup();
    }

    private void UpdateSelectionTexts()
    {
        if (fme1SelectedText != null)
        {
            fme1SelectedText.text = selectedFME1Index >= 0 ? 
                $"Selected: Knob {selectedFME1Index + 1}" : 
                "Click to select FME1 knob";
        }
        
        if (fme2SelectedText != null)
        {
            fme2SelectedText.text = selectedFME2Index >= 0 ? 
                $"Selected: Knob {selectedFME2Index + 1}" : 
                "Click to select FME2 knob";
        }
    }

    // Method to clear FME selection
    public void ClearFME1Selection()
    {
        selectedFME1Index = -1;
        if (fme1PreviewImage != null)
        {
            fme1PreviewImage.sprite = null;
            fme1PreviewImage.color = defaultPreviewColor;
        }
        UpdateSelectionTexts();
    }
    
    public void ClearFME2Selection()
    {
        selectedFME2Index = -1;
        if (fme2PreviewImage != null)
        {
            fme2PreviewImage.sprite = null;
            fme2PreviewImage.color = defaultPreviewColor;
        }
        UpdateSelectionTexts();
    }

    private void UpdateTotalDuration()
    {
        int totalDuration = 0;

        totalDuration += ParseField(wfeField);
        totalDuration += ParseField(wurdField);
        totalDuration += ParseField(fpsField);
        totalDuration += ParseField(hocField);
        totalDuration += ParseField(fme1TimeField);
        totalDuration += ParseField(fme2TimeField);

        totalDurationText.text = totalDuration.ToString();
    }

    private int ParseField(TMP_InputField field)
    {
        if (int.TryParse(field.text, out int value))
        {
            return value;
        }
        return 0; 
    }

    public void saveConfig()
    {
        List<string> emptyFields = new List<string>();

        if (string.IsNullOrWhiteSpace(homerIdField.text)) emptyFields.Add("HOMER ID");
        if (string.IsNullOrWhiteSpace(startDateField.text)) emptyFields.Add("Start Date");
        if (string.IsNullOrWhiteSpace(endDateField.text)) emptyFields.Add("End Date");

        // Check FME selections
        string fme1T = string.IsNullOrEmpty(fme1TimeField.text) ? "0" : fme1TimeField.text;
        if (int.Parse(fme1T) > 0)
        {
            if (selectedFME1Index == -1) emptyFields.Add("FME1 Image");
        }

        string fme2T = string.IsNullOrEmpty(fme2TimeField.text) ? "0" : fme2TimeField.text;
        if (int.Parse(fme2T) > 0)
        {
            if (selectedFME2Index == -1) emptyFields.Add("FME2 Image");
        }

        if (emptyFields.Count > 0)
        {
            string missing = string.Join(", ", emptyFields);
            string message = $"{missing} field{(emptyFields.Count > 1 ? "s are" : " is")} required!";
            msg.text = message;
            return;
        }

        // Check if FMEs are the same
        if (selectedFME1Index == selectedFME2Index && selectedFME2Index != -1)
        {
            msg.text = "FME1 and FME2 cannot be the same image!";
            return;
        }

        string homerID = homerIdField.text;
        AppData.Instance.setUser(homerID);
        string startDate = startDateField.text;
        string endDate = endDateField.text;
        
        // Set null to "10"
        string wfe = string.IsNullOrEmpty(wfeField.text) ? "0" : wfeField.text;
        string wurd = string.IsNullOrEmpty(wurdField.text) ? "0" : wurdField.text;
        string fps = string.IsNullOrEmpty(fpsField.text) ? "0" : fpsField.text;
        string hoc = string.IsNullOrEmpty(hocField.text) ? "0" : hocField.text;
        
        // FME times
        string fme1Time = string.IsNullOrEmpty(fme1TimeField.text) ? "0" : fme1TimeField.text;
        string fme2Time = string.IsNullOrEmpty(fme2TimeField.text) ? "0" : fme2TimeField.text;
        
        // FME selected indices (1-12 for display, 0-11 for mechanism index)
        string fme1 = (selectedFME1Index + 1).ToString(); // Display number (1-12)
        string fme2 = (selectedFME2Index + 1).ToString(); // Display number (1-12)
        
        // Mechanism index numbers (0-11)
        string fme1k = selectedFME1Index.ToString();
        string fme2k = selectedFME2Index.ToString();
        
        string totalDuration = totalDurationText.text;
        string trainingSide = affectedSideDropdown.options[affectedSideDropdown.value].text;
        string Location = location.options[location.value].text;
        string group = "Experimental";

        // Updated headers to include all fields
        string headers = "HomerID,StartDate,EndDate,TotalTime,WFE,WURD,FPS,HOC,FME1,FME2,TrainingSide,Location,Group,FME1K,FME2K";
        string data = $"{homerID},{startDate},{endDate},{totalDuration},{wfe},{wurd},{fps},{hoc},{fme1Time},{fme2Time},{trainingSide},{Location},{group},{fme1k},{fme2k}";

        string directoryPath = Path.Combine(Application.dataPath, "data", AppData.Instance.userID, "data");
        string datapath = Path.Combine(directoryPath, "configdata.csv");
        
        // Ensure directory exists
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        if (!File.Exists(datapath))
        {
            File.WriteAllText(datapath, headers + Environment.NewLine);
            Debug.Log("Data saved to CSV: " + datapath);
        }
        File.AppendAllText(datapath, data + Environment.NewLine);
        SceneManager.LoadScene("MAIN");
    }

    public void LoginScreen()
    {
        SceneManager.LoadScene("LOGIN");
    }
}

// Optional: Simple hover effect script
public class ButtonHoverEffect : MonoBehaviour
{
    private Button button;
    private Image image;
    private Color normalColor = Color.white;
    private Color hoverColor = new Color(0.9f, 0.9f, 1f, 1f);

    private void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
    }

    private void Start()
    {
        if (button != null)
        {
            // Add event triggers for hover effect
            var trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            // Pointer Enter event
            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { OnPointerEnter(); });
            trigger.triggers.Add(pointerEnter);
            
            // Pointer Exit event
            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { OnPointerExit(); });
            trigger.triggers.Add(pointerExit);
        }
    }

    private void OnPointerEnter()
    {
        if (image != null && button.interactable)
        {
            image.color = hoverColor;
        }
    }

    private void OnPointerExit()
    {
        if (image != null && button.interactable)
        {
            image.color = normalColor;
        }
    }
}