//using UnityEngine;
//using UnityEngine.UI;

//public class SliderTransition : MonoBehaviour
//{
//    public PROMsceneHandler promSlider;  // Reference to the DoubleSlider
//    public GameObject promPanel;     // Reference to the Prom Panel GameObject
//    public GameObject aromPanel;     // Reference to the Arom Panel GameObject

//    private bool isPromComplete = false; // Flag to check if PromSlider is completed

//    void Update()
//    {
//        if (!isPromComplete)
//        {
//            CheckPromSliderCompletion();
//        }
//    }

//    // Method to check if the PromSlider is completed
//    void CheckPromSliderCompletion()
//    {
//        if (promSlider == null)
//        {
//            Debug.LogError("PromSlider is not assigned.");
//            return;
//        }

//        // Example condition to check if PromSlider is completed
//        // Adjust the condition based on your actual completion logic
//        // Assuming IsInteracting checks if interaction is completed
//        {
//            CompletePromSlider();
//        }
//    }

//    // Method to handle PromSlider completion
//    void CompletePromSlider()
//    {
//        // Set the flag to indicate completion
//        isPromComplete = true;

//        // Disable interaction with PromSlider
//        promSlider.interactable = false;

//        // Hide the PromPanel and show the AromPanel
//        if (promPanel != null) promPanel.SetActive(false);
//        if (aromPanel != null) aromPanel.SetActive(true);

//        // Optionally, you can perform additional operations here,
//        // like resetting values or updating UI elements.
//        Debug.Log("Prom Slider completed. Transitioning to Arom Panel.");
//    }
//}