using UnityEngine;
using UnityEngine.UI;

public class ToggleGameObject : MonoBehaviour
{
    public Toggle toggle;

    public GameObject onImage;
    public GameObject offImage;

    public bool trajON =true;

    private void Start()
    {
        // Force default ON
        toggle.isOn = true;
        ApplyState(toggle.isOn);

        toggle.onValueChanged.AddListener(ApplyState);
    }

    private void ApplyState(bool isOn)
    {
        trajON = isOn;

        if (onImage != null) onImage.SetActive(isOn);
        if (offImage != null) offImage.SetActive(!isOn);
    }
}
