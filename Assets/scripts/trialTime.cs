
using UnityEngine;
using TMPro;
public class trialTime : MonoBehaviour
{
    private TextMeshProUGUI trialNo;
    // Start is called before the first frame update
    void Start()
    {
         trialNo= transform.Find("trialNumber").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        trialNo.text =$"{AppData.Instance.selectedMechanism.trialNumberDay}/ {AppData.Instance.userData.mechMoveTimePrsc[AppData.Instance.selectedMechanism.name]}";
    }
}
