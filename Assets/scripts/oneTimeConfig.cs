using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Data;
using System.Globalization;

public class OneTimeConfig : MonoBehaviour
{
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
    public Image fme1PreviewImage; // Display selected image
    public Button fme1SelectButton; // Button to open popup
    
    // FME2 components - Time input + Image selection
    public TMP_InputField fme2TimeField;
    public Image fme2PreviewImage; // Display selected image
    public Button fme2SelectButton; // Button to open popup
    
    // Popup components
    public GameObject imageSelectionPopup;
    public Transform imageGridContainer; // GridLayoutGroup parent for images
    public GameObject imageButtonPrefab; // Prefab with Button + Image
    public TextMeshProUGUI popupTitle;
    
    // Text displays for selected images
    public TextMeshProUGUI fme1SelectedText;
    public TextMeshProUGUI fme2SelectedText, loginButtonText;
    
    // Sprites for the 12 mechanism images (assign in inspector)
    public Sprite[] mechanismSprites = new Sprite[12];
    
    public TMP_Dropdown affectedSideDropdown;
    public TMP_Dropdown location;

    public TextMeshProUGUI totalDurationText;
    public TMP_Text msg;

    // Variables to store selected indices (0-11)
    private int selectedFME1Index = -1;
    private int selectedFME2Index = -1;
    
    // Track which FME we're selecting for (1 or 2)
    private int selectingForFME = 0; // 0 = none, 1 = FME1, 2 = FME2

    // Selection colors
    private Color defaultPreviewColor = new Color(1, 1, 1, 0.3f);
    private Color selectedPreviewColor = Color.black;
    private DateTime startDate, endDate;

    private void Start()
    {
        // Automatically set startDateField and endDateField
        startDate = DateTime.Now;
        endDate = startDate.AddDays(30);

         if (File.Exists(DataManager.configFile))
        {
            DataTable configData = DataManager.loadCSV(DataManager.configFile);

            DataRow lastRow = configData.Rows[configData.Rows.Count - 1];
            string hospNumber = lastRow.Field<string>("HomerId");
            bool rightHand = lastRow.Field<string>("TrainingSide") == "right";
            Debug.Log(lastRow.Field<string>("FME1K"));
            int FME1 = int.Parse(lastRow.Field<string>("FME1K"));
            int FME2 = int.Parse(lastRow.Field<string>("FME2K"));
            //AppData.trainingSide = ; // lastRow.Field<string>("TrainingSide");
            // startDate = DateTime.ParseExact(lastRow.Field<string>("StartDate"), "dd-MM-yyyy", CultureInfo.InvariantCulture);
            endDate = DateTime.ParseExact(lastRow.Field<string>("endDate"), "dd-MM-yyyy", CultureInfo.InvariantCulture);

            homerIdField.text = hospNumber;
            affectedSideDropdown.options[affectedSideDropdown.value].text = rightHand ? "right":"left";
            location.options[location.value].text = lastRow.Field<string>("Location");
        
            wfeField.text =lastRow.Field<string>("WFE");
            wurdField.text= lastRow.Field<string>("WURD");
            fpsField.text= lastRow.Field<string>("FPS");
            hocField.text= lastRow.Field<string>("HOC");
            fme1TimeField.text= lastRow.Field<string>("FME1");
            fme2TimeField.text= lastRow.Field<string>("FME2");
            totalDurationText.text= lastRow.Field<string>("TotalTime");

            loginButtonText.text ="Login";

            if (fme1PreviewImage != null && FME1 < mechanismSprites.Length && FME1>=0)
            {
                selectedFME1Index= FME1;
                fme1PreviewImage.sprite = mechanismSprites[FME1];
                fme1PreviewImage.color = selectedPreviewColor;
                if (fme1SelectedText != null)
                {
                    
                    fme1SelectedText.text = FME1 >= 0 ? 
                        $"Selected: Knob {FME1}" : 
                        "Click to select FME1 knob";
                }
        

            }
            else
            {
                if (fme1PreviewImage != null)
                {
                    fme1PreviewImage.color = defaultPreviewColor;
                    fme1PreviewImage.sprite = null;
                }
                if (fme1SelectedText != null)
                {
                    fme1SelectedText.text = FME1 >= 0 ? 
                        $"Selected: Knob {FME1}" : 
                        "Click to select FME1 knob";
                }
            }
            // Update FME2 preview
            if (fme2PreviewImage != null && FME2 < mechanismSprites.Length && FME2>=0)
            {
                fme2PreviewImage.sprite = mechanismSprites[ FME2];
                fme2PreviewImage.color = selectedPreviewColor;
                selectedFME2Index= FME2;


                if (fme2SelectedText != null)
                {
                    fme2SelectedText.text = FME2 >= 0 ? 
                        $"Selected: Knob {FME2}" : 
                        "Click to select FME2 knob";
                }
            }
            else
            {
                if (fme2PreviewImage != null)
                {
                    fme2PreviewImage.color = defaultPreviewColor;
                    fme2PreviewImage.sprite = null;
                }
                 if (fme2SelectedText != null)
                {
                    fme2SelectedText.text = FME2 >= 0 ? 
                        $"Selected: Knob {FME2}" : 
                        "Click to select FME2 knob";
                }
            }

        }
        else
        {
                    // Initialize preview images
        InitializePreviewImages();
            
        }
        
        startDateField.text = startDate.ToString("dd-MM-yyyy");
        endDateField.text = endDate.ToString("dd-MM-yyyy");

        // Add listeners for time fields
        wfeField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        wurdField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        fpsField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        hocField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        fme1TimeField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });
        fme2TimeField.onValueChanged.AddListener(delegate { UpdateTotalDuration(); });

        // Add listeners for FME selection buttons
        fme1SelectButton.onClick.AddListener(() => OpenImageSelectionPopup(1));
        fme2SelectButton.onClick.AddListener(() => OpenImageSelectionPopup(2));


        
        // Initialize popup (but don't show it yet)
        InitializeImageSelectionPopup();
        
        // Close popup initially
        imageSelectionPopup.SetActive(false);
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
        string fme1T = string.IsNullOrEmpty(fme1TimeField.text)? "0": fme1TimeField.text;
        Debug.Log($"TIME {fme1T}");

        if (int.Parse(fme1T) > 0)
        {
        if (selectedFME1Index == -1) emptyFields.Add("FME1 Image");
        Debug.Log($"TIME {fme1T}");

        }

        string fme2T = string.IsNullOrEmpty(fme2TimeField.text)? "0": fme2TimeField.text;
        Debug.Log($"TIME {fme2T}");

        if (int.Parse(fme2T) > 0)
        {
        if (selectedFME2Index == -1) emptyFields.Add("FME2 Image");
        Debug.Log($"TIME {fme1T}");

        }
        // if (string.IsNullOrWhiteSpace(fme1TimeField.text)) emptyFields.Add("FME1 Time");
        // if (string.IsNullOrWhiteSpace(fme2TimeField.text)) emptyFields.Add("FME2 Time");

        if (emptyFields.Count > 0)
        {
            string missing = string.Join(", ", emptyFields);
            string message = $"{missing} field{(emptyFields.Count > 1 ? "s are" : " is")} required!";
            msg.text = message;
            return;
        }

        // Check if FMEs are the same
        if (selectedFME1Index == selectedFME2Index && selectedFME2Index != -1f)
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
        string fme1Time = string.IsNullOrEmpty(fme1TimeField.text)? "0": fme1TimeField.text;
        string fme2Time = string.IsNullOrEmpty(fme2TimeField.text)? "0": fme2TimeField.text;
        
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
        string headers = "HomerId,StartDate,EndDate,TotalTime,WFE,WURD,FPS,HOC,FME1,FME2,TrainingSide,Location,Group,FME1K,FME2K";
        string data = $"{homerID},{startDate},{endDate},{totalDuration},{wfe},{wurd},{fps},{hoc},{fme1Time},{fme2Time},{trainingSide},{Location},{group},{fme1k},{fme2k}";

        string directoryPath = Path.Combine(Application.dataPath,"data",AppData.Instance.userID,"data");
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

    public void LoginScreen(){
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