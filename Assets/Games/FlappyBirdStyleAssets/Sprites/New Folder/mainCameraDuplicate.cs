using UnityEngine;

public class mainCameraDuplicate : MonoBehaviour
{
    public Camera duplicateCamera;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        duplicateCamera.transform.position = Camera.main.transform.position;
        duplicateCamera.transform.rotation = Camera.main.transform.rotation;


    }
}
