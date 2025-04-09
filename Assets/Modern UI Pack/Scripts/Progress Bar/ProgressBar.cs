//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//namespace Michsky.UI.ModernUIPack
//{
//    public class ProgressBar : MonoBehaviour
//    {
//        [Header("OBJECTS")]
//        public Transform loadingBar;
//        public Transform textPercent;

//        [Header("VARIABLES (IN-GAME)")]
//        public bool isOn;
//        public bool restart;
//        [Range(0, 100)] public float currentPercent;
//        [Range(0, 100)] public int speed;

//        [Header("SPECIFIED PERCENT")]
//        public bool enableSpecified;
//        public bool enableLoop;
//        [Range(0, 100)] public float specifiedValue;

//        void Update()
//        {
//            if (currentPercent <= 100 && isOn == true && enableSpecified == false)
//                currentPercent += speed * Time.deltaTime;

//            if (currentPercent <= 100 && isOn == true && enableSpecified == true)
//            {
//                if (currentPercent <= specifiedValue)
//                    currentPercent += speed * Time.deltaTime;

//                if (enableLoop == true && currentPercent >= specifiedValue)
//                    currentPercent = 0;
//            }

//            if (currentPercent == 100 || currentPercent >= 100 && restart == true)
//                currentPercent = 0;

//            if (enableSpecified == true && specifiedValue == 0)
//                currentPercent = 0;

//            loadingBar.GetComponent<Image>().fillAmount = currentPercent / 100;
//            textPercent.GetComponent<TextMeshProUGUI>().text = ((int)currentPercent).ToString("F0") + "%";
//        }
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    public class ProgressBar : MonoBehaviour
    {
        [Header("OBJECTS")]
        public Transform loadingBar;
        public Transform textPercent;

        [Header("VARIABLES (IN-GAME)")]
        public bool isOn;
        public bool restart;
        [Range(0, 100)] public float currentPercent;
        [Range(0, 100)] public int speed;

        [Header("SPECIFIED PERCENT")]
        public bool enableSpecified;
        public bool enableLoop;
        [Range(0, 100)] public float specifiedValue;

        private bool hasReachedMax; // Flag to track if action has been executed

        void Update()
        {
            if (currentPercent < 100 && isOn && !enableSpecified)
            {
                currentPercent += speed * Time.deltaTime;
            }

            if (currentPercent < 100 && isOn && enableSpecified)
            {
                if (currentPercent <= specifiedValue)
                {
                    currentPercent += speed * Time.deltaTime;
                }

                if (enableLoop && currentPercent >= specifiedValue)
                {
                    currentPercent = 0;
                }
            }

            if (currentPercent >= 100)
            {
                currentPercent = 100; // Clamp value to 100

                if (!hasReachedMax)
                {
                    hasReachedMax = true; // Set flag
                    isOn = false; // Stop further updates
                    PerformActionOnComplete(); // Execute the action
                }

                if (restart)
                {
                    currentPercent = 0;
                    hasReachedMax = false; // Reset the flag for next cycle
                }
            }
            else
            {
                hasReachedMax = false; // Reset flag when below 100
            }

            if (enableSpecified && specifiedValue == 0)
            {
                currentPercent = 0;
            }

            loadingBar.GetComponent<Image>().fillAmount = currentPercent / 100;
            textPercent.GetComponent<TextMeshProUGUI>().text = ((int)currentPercent).ToString("F0") + "%";
        }

        private void PerformActionOnComplete()
        {
            // Action to perform when progress reaches 100%
            Debug.Log("Progress completed at 100%!");
        }
    }
}
