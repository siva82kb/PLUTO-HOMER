
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class trialTime : MonoBehaviour
{
    private TextMeshProUGUI trialNo;
    public Image fillColor;
    // Start is called before the first frame update
    void Start()
    {
         trialNo= transform.Find("trialNumber").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        trialNo.text =$"{AppData.Instance.selectedMechanism.trialNumberDay:D2}/{AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name]}";
        fillColor.fillAmount = AppData.Instance.selectedMechanism.trialNumberDay / AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name];
    }
}
