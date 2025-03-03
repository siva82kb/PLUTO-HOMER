using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TS.DoubleSlider;
using System.IO;


public class PROMsceneHandler : MonoBehaviour {

    enum AssessStates
    {
        INIT = 0,    
        ASSESS = 1,
        RELAX = 2
    };
    bool assessmentSaved;
    private bool isPaused = false;
    private bool isButtonPressed = false;
    public TMP_Text lText;
    public TMP_Text rText;
    public TMP_Text insText;
    public TMP_Text cText;
    //public TMP_Text statusText;
    public TMP_Text relaxText;

    public TMP_Text JointAngle;

    public TMP_Text JointAngleHoc;

    bool AssessmentValid;

    private int _stpCount;
    private int _stpCountTh = 00;
    private float _lowSpdTh = 20f;
    private float _tmin, _tmax;
    private float _tmin1, _tmax1;


    private double _strttime;
    private double _initDur = 0f;
    private double _assessDur = 180f;
    private double _relaxDur = 3.0f;

    // Assessment torque trajectory
    private float _winT =0;
    private float _freq = 0.1f;
    public static float _torqAmp = .7f;
    private float _currTorq = 0;
    private double _t;
    private double _prevt;
    private double _dt = 0.01;

    public GameObject nextButton;
    public GameObject startButton;
    public GameObject  CurrPositioncursor;
    public GameObject  CurrPositioncursorHoc;
    private AssessStates _state;

    private float angLimit;
    public DoubleSlider promSlider;

    public DoubleSlider promSliderHOC;

    //public DoubleSlider promSlider1;

    public bool isSelected = false;
    public bool inst = false;
    public assessmentSceneHandler panelControl;


    private List<string[]> DirectionText = new List<string[]>
    {
        new string[] { "Flexion", "Extension" },
        new string[] { "Ulnar Dev.", "Radial Dev."},
        new string[] { "Pronation", "Supination" },
        new string[]{"Open", "Open"},
        new string[] {"",""},
        new string[] {"",""}
    };

    private int _linx, _rinx;
    internal bool interactable;

    // Use this for initialization
    void Start () {
         InitializeAssessment();

    }
 
  public void InitializeAssessment()
  {       
       
        nextButton.SetActive(false);

         


        AppData.oldPROM = new ROM(AppData.selectedMechanism);

        //string dir = Path.Combine(DataManager.directoryAPROMData,AppData.selectedMechanism+".csv");
        //if (!Directory.Exists(DataManager.directoryAPROMData))
        //{
        //    Directory.CreateDirectory(DataManager.directoryAPROMData);
        //}
        //if (!File.Exists(dir)) {
        //    using (var writer = new StreamWriter(dir, false, Encoding.UTF8))
        //    {
        //        writer.WriteLine("datetime,side,tmin,tmax,aromTmin,aromTmax");
        //    }
        //}

        if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) != 4)
        {
         angLimit = AppData.offsetAtNeutral[PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.selectedMechanism)];

       // Debug.Log("prom:" + AppData.oldPROM.tmin + "," + AppData.oldPROM.tmax);
        promSlider.Setup(-angLimit, angLimit, AppData.oldPROM.promTmin, AppData.oldPROM.promTmax);
        promSlider.minAng = 0;
        promSlider.maxAng =0;
    }

        else{
            float minAng = 0;
            float maxAng = 90;

            //Debug.Log("prom:" + AppData.oldPROM.tmin + "," + AppData.oldPROM.tmax);
            // angLimit = 95.42f;
            angLimit = 120.42f;
            promSlider.Setup(-angLimit, angLimit, AppData.oldPROM.promTmin, AppData.oldPROM.promTmax);  // Centering the slider
   

        }

        ////ANN
        //AppData.oldANN = new AAN(AppData.subjHospNum,
        //    DataTypeDefinitions.PlutoMechanisms[0][AppData.plutoData.mechIndex]);



        //PlutoComm.OnButtonReleased += onPlutoButtonReleased;
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
 
    
    private void DisablePromGameObjects()
    {
        //redoButton.SetActive(true);
        startButton.SetActive(false);
        //aromButton.SetActive(false);
        nextButton.SetActive(false);
        //restartButton.SetActive(false);
        // Add other GameObjects to be disabled if necessary
    }
	
	// Update is called once per frame

    public void OnStartButtonClick()
{
    startAssessment(); // Starts the assessment
    startButton.SetActive(false);
    nextButton.SetActive(true);
    //restartButton.SetActive(true);
}


	void Update () {


        //Debug.Log(_state);

        JointAngle.text = ((int)PlutoComm.angle).ToString();

        //JointAngleHoc.text = ((int)PlutoComm.angle).ToString();
        JointAngleHoc.text =((int) PlutoComm.getHOCDisplay(PlutoComm.angle)).ToString();
        //Debug.Log(AppData.plutoData.angle);

        if (isSelected)
        {
            if(Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) ==4)
            {
                CurrPositioncursorHoc.SetActive(true);
                CurrPositioncursor.SetActive(true);
            }
            else{
                 CurrPositioncursor.SetActive(true);
            }
       
        

            _t = AppData.CurrentTime - _strttime;
            switch (_state)
            {

                case AssessStates.INIT:
                    //redoButton.SetActive(false);
                    startButton.SetActive(true);

                    
                    PlutoComm.OnButtonReleased += OnPlutoButtonReleased;
                    //restartButton.SetActive(false);
                    //if (!inst) {
                       // rText.text = "Place the mechanism to zero position and press K to set Zero";
                       // rText.color= Color.black;
                         //  }
                      
                    //aromButton.SetActive(false);
                    if (Input.GetKeyDown(KeyCode.K) && AppData.selectedMechanism != "HOC")
                    {
                            PlutoComm.calibrate(AppData.selectedMechanism);
                           // inst = false;
                       // rText.text = "";
                    }

                    if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                    {
                        nextButton.SetActive(true);
                       // Debug.Log(PlutoComm.angle);
                        
                        startAssessment();
                        isButtonPressed = false;
                    }
                    //Debug.Log("mech No:" + PlutoComm.GetPlutoCodeFromLabel(PlutoComm.MECHANISMS, AppData.selectedMechanism));
                    if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
                    {
                        // Convert tmin and tmax from degrees to centimeters
                        float max = AppData.oldPROM.promTmax / 2;
                        float apertureMinCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldPROM.promTmin * 6f);
                        float apertureMaxCM = Mathf.Abs(Mathf.Deg2Rad *  max * 6f);
                        //Debug.Log("time min: " + AppData.oldPROM.tmin);
                        //Debug.Log("time max: " + AppData.oldPROM.tmax);

                        // Update the relaxText with the aperture values in centimeters
                        relaxText.text = "Prev Prom: " + apertureMinCM.ToString("0.0") + "cm : " + apertureMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(apertureMaxCM - apertureMinCM).ToString("0.0") + "cm)";
                    }
                    else{
                    relaxText.text = "Prev PROM: " + (int)AppData.oldPROM.promTmin + " : " + (int)AppData.oldPROM.promTmax + " (" + (int)(AppData.oldPROM.promTmax - AppData.oldPROM.promTmin) + "°)";
                    }
                    break;
                case AssessStates.ASSESS:
                    //nextButton.SetActive(true);
                    
                    startButton.SetActive(false);
                    //restartButton.SetActive(true);
                    _tmin = promSlider.minAng;
                    _tmax = promSlider.maxAng;
                   // Debug.Log("tmin : " + _tmin + "," + "tmax : " + _tmax);

                    assessmentSaved = false;
                    relaxText.text = "Prev PROM: " + (int)AppData.oldPROM.promTmin + " : " + (int)AppData.oldPROM.promTmax + " (" + (int)(AppData.oldPROM.promTmax - AppData.oldPROM.promTmin) + "°)";

                    if (isButtonPressed || Input.GetKeyDown(KeyCode.Return))
                    {
                        //Debug.Log("Hello");
                        AssessmentValid = true;
                        _state = AssessStates.RELAX;
                        OnNextButtonClick();
                                nextButton.SetActive(false);
                                //go to arom;
                                DisablePromGameObjects();
                        isButtonPressed = false;
                        // PanelControl.SelectAROM();  
                        //Debug.Log("AROM Started");
                        //Debug.Log("go to arom");

                    }



                    break;
                case AssessStates.RELAX:
              

                    if (AssessmentValid)
                    {
                        Debug.Log("Assessment validated :" + AssessmentValid);

                        if (!assessmentSaved)
                        {

                            relaxText.text = "Press ENTER to Save";
                            // relaxText.color = Color.green;

                           
                            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
                           {

                            float apertureMinCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldPROM.promTmin * 6f);
                            float apertureMaxCM = Mathf.Abs(Mathf.Deg2Rad * -AppData.oldPROM.promTmax * 6f);
                            float currentMinCM = Mathf.Abs(Mathf.Deg2Rad * promSlider.minAng * 6f);
                            float currentMaxCM = Mathf.Abs(Mathf.Deg2Rad * -promSlider.maxAng * 6f);

            

                            relaxText.text = "Assessment Completed \n " + "Prev PROM: " + apertureMinCM.ToString("0.0") + "cm : " + apertureMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(apertureMaxCM - apertureMinCM).ToString("0.0") + "cm)\n"+
                                "Current PROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
                           }
                           else
                            {
                            relaxText.text = "Assessment Completed \n " + "Prev PROM: " + (int)AppData.oldPROM.promTmin + " : " + (int)AppData.oldPROM.promTmax + " (" + (int)(AppData.oldPROM.promTmax - AppData.oldPROM.promTmin) + "°)\n" +
                                "Currentt PROM: " + (int)promSlider.minAng + " : " + (int)promSlider.maxAng + " (" + (int)(promSlider.maxAng - promSlider.minAng) + "°)\n";
                            }
                            //if (Input.GetKeyDown(KeyCode.Return))
                            //{
                            //    OnSaveClick();
                            //    Debug.Log("DATA SAVED");
                            //    moveAROM    
                            //}
                        }
       
                        else
                        {
                            
                            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
                            {
                                float currentMinCM = Mathf.Abs(Mathf.Deg2Rad * _tmin * 6f);
                                float currentMaxCM = Mathf.Abs(Mathf.Deg2Rad * -_tmax * 6f);

                                relaxText.text = "Assessment Completed \n" +
                                "Current PROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
                            }
                            else
                            {
                            relaxText.text = relaxText.text = "Assessment Completed \n " +
                                "Currentt PROM: " + (int)_tmin + " : " + (int)_tmax + " (" + (int)(_tmax - _tmin) + "°)\n";
                            }
                            if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) < 5)
                            {

                                //AANScene.SetActive(true);

                            }
                            

                        }
                    }
                    else
                    {
                        
                        relaxText.text = "PROM should be greater than AROM\n " +
                            "Press ENTER Redo Assesment ";
           
                    }
                    break;

                   
            }


            UpdateGUI();
        }
        else
        {
            //Debug.Log(" PROM deselected");

        

            if(Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
            {
            float apertureMinCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldPROM.promTmin * 6f);
            float apertureMaxCM = Mathf.Abs(Mathf.Deg2Rad * AppData.oldPROM.promTmax * 6f);
            float currentMinCM = Mathf.Abs(Mathf.Deg2Rad * promSlider.minAng * 6f);
            float currentMaxCM = Mathf.Abs(Mathf.Deg2Rad * promSlider.maxAng * 6f);
            relaxText.text = "Assessment Completed \n " + "Prev PROM: " + apertureMinCM.ToString("0.0") + "cm : " + apertureMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(apertureMaxCM - apertureMinCM).ToString("0.0") + "cm)\n"+
                                "Current PROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
            }
            else{
            relaxText.text = "Assessment Completed \n "+"Prev PROM: " + (int)AppData.oldPROM.promTmin + " : " + (int)AppData.oldPROM.promTmax + " (" + (int)(AppData.oldPROM.promTmax - AppData.oldPROM.promTmin) + "°) ||" +
                              "Current PROM: " + (int)promSlider.minAng + " : " + (int)promSlider.maxAng + " (" + (int)(promSlider.maxAng - promSlider.minAng) + "°)\n";
            }
            //Debug.Log(" PROM deselected");
            //CurrPositioncursor.SetActive(false);
        
        }
    }
    //public void OnRedopromButtonClick()
    //{
    //    // Call the InitializeAssessment method to reset and reinitialize the assessment
    //    panelControl.SelectpROM();
    //    //nextButton.SetActive(true);
    //    _state = AssessStates.INIT;
    //    InitializeAssessment();
    //    Debug.Log("Assessment Restarted");
    //    //Start();
    //}


    public void OnRedoPromClick()
    {
        // Reset the state to INIT
        _state = AssessStates.INIT;
       
        // Reset assessment-related flags and data
        assessmentSaved = false;
        AssessmentValid = false;
        isButtonPressed = false;

        // Reinitialize the assessment process
        InitializeAssessment();

        // Update UI to reflect INIT state
        UpdateGUI();
        panelControl.SelectpROM();
        Debug.Log("Redo PROM: Reset to INIT state.");
    }

    public void OnPlutoButtonReleased()
    {
            isButtonPressed = true;
        
    }


    public void OnNextButtonClick()
   {
    OnSaveClick();
    Debug.Log("AROM moved");
    panelControl.SelectAROM(); 
    DisablePromGameObjects();
    _state = AssessStates.RELAX;

   }
    public void OnrestartButtonClick()
   {
    Start();
   }


    public void OnSaveClick()
    {
        nextButton.SetActive(false);
        _tmin = promSlider.minAng;
        _tmax = promSlider.maxAng;
        assessmentSaved = true;
        Debug.Log("Onsave : " + _tmin+" , "+ _tmax);
        gameData.isPROMcompleted= true;
        AppData.promTmin= _tmin;
        AppData.promTmax= _tmax;

        promSlider.UpdateMinMaxvalues = false;
        CurrPositioncursor.SetActive(false);
        CurrPositioncursorHoc.SetActive(false);
       
       
        promSlider.minAng = AppData.promTmin; 

        nextButton.SetActive(false);
       
        if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) == 4)
            {
                float currentMinCM = Mathf.Abs(Mathf.Deg2Rad * _tmin * 6f);
                float currentMaxCM = Mathf.Abs(Mathf.Deg2Rad * -_tmax * 6f);

                relaxText.text = "Assessment Completed \n" +
                                "Current PROM: " + currentMinCM.ToString("0.0") + "cm : " + currentMaxCM.ToString("0.0") + "cm (Aperture: " + Mathf.Abs(currentMaxCM - currentMinCM).ToString("0.0") + "cm)\n";
            }
        else
        {
        relaxText.text = "Assessment Completed \n " +
        "Current PROM: " + (int)_tmin + " : " + (int)_tmax + " (" + (int)(_tmax - _tmin) + " °)\n";
        }
    }
       



     public void startAssessment()
    {
        _state = AssessStates.ASSESS;
        promSlider.minAng = 0;
        promSlider.maxAng = 0;
       
         promSlider.startAssessment(PlutoComm.angle);
            promSlider.UpdateMinMaxvalues = true;
        
        

    }

    bool validAssessment()
    {
        AppData.oldAROM = new ROM(AppData.selectedMechanism);
        if (_tmin <= AppData.oldAROM.promTmin && _tmax >= AppData.oldAROM.promTmax)
        {
            return true;
        }
        else
            return false;
    }
    



    void OnApplicationQuit()
    {
       JediComm.Disconnect();
    }

    public void On_Back_Click()
    {
        SceneManager.LoadScene(2);
    }
    
    private void UpdateGUI()
    {
        UpdateStatusText();
    }


    public void OnFinishPressed()
    {
        _state = AssessStates.RELAX;
    }
    private void UpdateStatusText()
    {
        if (Array.IndexOf(PlutoComm.MECHANISMS, AppData.selectedMechanism) != 4)
        {
            JointAngle.text =  (PlutoComm.angle).ToString("0.0") ;
        }
        else {
            JointAngle.text = "Aperture" + Mathf.Abs((Mathf.Deg2Rad * PlutoComm.angle * 6f)).ToString("0.0") + "cm";

            JointAngleHoc.text = "Aperture" + Mathf.Abs((Mathf.Deg2Rad * PlutoComm.angle * 6f)).ToString("0.0") + "cm";
        }

    }


}
