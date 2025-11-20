using TMPro;
using UnityEngine;

public class SeedController : MonoBehaviour
{
    [Header("Growth")]
    public GameObject[] growthStages; // 0=seed, 1=sprout, ...
    private int currentStage = 0;

    public bool IsFullyGrown => currentStage >= maxStages;

    private int maxStages = 4; // default

    [Header("Highlight")]
    public ParticleSystem highlightEffect;
    public GameObject highLighter;

    [HideInInspector] public bool IsBeingRainedOn = false;
    private bool rainingThisFrame = false;
    // private SeedHighlighter seed;
    public GameObject bloomEffectPrefab;
    public GameObject scorePopupPrefab;
    public Transform bloomSpawnPoint; // where effect should appear
    public Color baseHighlightColor;
    void Start()
    {
        // Decide number of stages: 3 by default, 4 if speed > 30
        // float moveSpeed = 21f;
        // if (moveSpeed > 30f)
        // {
        //     maxStages = Mathf.Min(growthStages.Length, 4);
        //     Debug.Log($"maxst :{maxStages}");
        // }
        // else
        //     maxStages = Mathf.Min(growthStages.Length, 3);

        // show only stage 0
        for (int i = 0; i < growthStages.Length; i++)
            growthStages[i].SetActive(i == 0);
    if (highlightEffect != null)
    {
        var renderer = highlightEffect.GetComponent<ParticleSystem>();
        if (renderer != null)
        {
            baseHighlightColor = renderer.startColor; // remember original
        }
    }
    }

    void Update()
    {
        IsBeingRainedOn = rainingThisFrame;
        rainingThisFrame = false;
     if (highlightEffect != null)
    {
        var main = highlightEffect.main;
        if (IsBeingRainedOn)
        {
            main.startColor = Color.red;   // Blue when raining
        }
        else
        {
            main.startColor = baseHighlightColor; // Reset to original
        }
    }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Rain"))
            rainingThisFrame = true;
    }

    public void SetHighlight(bool state)
    {
        if (highlightEffect == null) return;

        if (highLighter == null) return;
        if (state) highLighter.SetActive(state);
        else highLighter.SetActive(state);

        if (state && !highlightEffect.isPlaying)
            highlightEffect.Play();
        else if (!state && highlightEffect.isPlaying)
            highlightEffect.Stop();
    }

    public void Grow()
    {
        // if (IsFullyGrown) return;

        growthStages[currentStage].SetActive(false);
        if (!IsFullyGrown) currentStage++;
        else currentStage--;
        growthStages[currentStage].SetActive(true);
        // gm.TargetReached();
        //ShowBloomEffect();
        // 🌸 Bloom effect
        Instantiate(bloomEffectPrefab, bloomSpawnPoint.position, Quaternion.identity);

        ShowBloomEffect();

    }
       private void ShowBloomEffect()
    {
        // Bloom particle
        // GameObject bloom = Instantiate(bloomEffectPrefab, transform.position, Quaternion.identity);
        // Destroy(bloom, 1f);

        // Popup text in world space
        Vector3 popupPos = bloomSpawnPoint.position + Vector3.up * 1f;
        GameObject popup = Instantiate(scorePopupPrefab, popupPos, Quaternion.identity);

        // Make it face the camera
        popup.transform.LookAt(Camera.main.transform);
        popup.transform.Rotate(0, 180, 0); // correct facing

        TextMeshProUGUI txt = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.text = "+1";
        }

         Destroy(popup, 2.5f);
    }
}
