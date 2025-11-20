using UnityEngine;
using TMPro;

public class ScorePopup : MonoBehaviour
{
    public float moveUpSpeed = 1f;
    public float fadeDuration = 1f;

    private TextMeshProUGUI textMesh;
    private Color originalColor;
    private float timer;

    // void Awake()
    // {
    //     textMesh = GetComponent<TextMeshProUGUI>();
    //     originalColor = textMesh.color;
    // }

    public void Setup(int scoreValue)
    {
        textMesh.text = scoreValue.ToString();
        Debug.Log(scoreValue);
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Move upwards
        transform.position += Vector3.up * moveUpSpeed * Time.deltaTime;

        // Fade out
        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
//        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        // if (timer >= fadeDuration)
        //     Destroy(gameObject);
    }
}
