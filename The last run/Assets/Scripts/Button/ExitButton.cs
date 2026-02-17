using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider))]
public class ExitButton : MonoBehaviour
{
    [Header("Gaze Highlight")]
    [Tooltip("Color applied when player looks at the button")]
    public Color highlightColor = Color.yellow;
    [Tooltip("Emission intensity multiplier when highlighted")]
    public float emissionIntensity = 2f;
    [Tooltip("Scale multiplier when highlighted")]
    public float highlightScaleMultiplier = 1.08f;
    [Tooltip("Max distance for gaze detection (meters)")]
    public float gazeDistance = 10f;

    [Header("UI (optional)")]
    [Tooltip("Assign a TextMeshProUGUI (placed at bottom of screen) to display key count")]
    public TextMeshProUGUI uiTMPText;
    [Tooltip("Distance (m) at which the UI starts to appear via fade")]
    public float uiProximityDistance = 3f;
    [Tooltip("Duration (s) for UI fade in/out")]
    public float uiFadeDuration = 0.4f;

    [Header("Inventory (optional)")]
    [Tooltip("Assign the player's Inventory to report key counts. If empty, will search in scene.")]
    public Inventory playerInventory;
    [Tooltip("Keys collected (can be updated from other scripts at runtime)")]
    private int keysCollected = 0;
    [Tooltip("Total keys required")]
    private int keysTotal = 3;

    Renderer rend;
    Color originalColor;
    Vector3 originalScale;
    bool isHighlighted = false;

    bool hasEmission = false;
    Color originalEmissionColor = Color.black;

    // UI/TMP fade state
    Color uiOriginalColor = Color.white;
    Coroutine uiFadeCoroutine = null;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
            hasEmission = rend.material.HasProperty("_EmissionColor");
            if (hasEmission)
            {
                originalEmissionColor = rend.material.GetColor("_EmissionColor");
            }
        }
        originalScale = transform.localScale;

        if (uiTMPText != null)
        {
            uiOriginalColor = uiTMPText.color;
            Color c = uiOriginalColor;
            c.a = 0f;
            uiTMPText.color = c;
            uiTMPText.gameObject.SetActive(false);
        }
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<Inventory>();
        }
    }

    void Update()
    {
        var cam = Camera.main;
        if (cam == null) {
            return;
        }

        UpdateUIProximity(cam);
    }

    void UpdateUIProximity(Camera cam)
    {
        if (uiTMPText == null) return;
        float dist = Vector3.Distance(cam.transform.position, transform.position);
        bool near = dist <= uiProximityDistance;

        int keyCount = 0;
        int keyTotal = 0;
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<Inventory>();
        }
        if (playerInventory != null)
        {
            keyTotal = playerInventory.maxSlots;
            foreach (var item in playerInventory.GetAllItems())
            {
                if (item is Key)
                {
                    keyCount++;
                }
            }
        }

        string msg;
        if (keyTotal > 0 && keyCount >= keyTotal)
        {
            msg = $"you collected all the {keyTotal} keys — press F to trigger the button and exit";
        }
        else
        {
            msg = $"you have {keyCount} out of {keyTotal} keys, door cannot be opened";
        }
        uiTMPText.text = msg;
        StartUIFade(near ? 1f : 0f);
    }

    void StartUIFade(float targetAlpha)
    {
        if (uiTMPText == null) return;
        if (uiFadeCoroutine != null)
        {
            StopCoroutine(uiFadeCoroutine);
            uiFadeCoroutine = null;
        }
        if (targetAlpha > 0f)
        {
            uiTMPText.gameObject.SetActive(true);
        }
        uiFadeCoroutine = StartCoroutine(FadeTMPTo(targetAlpha));
    }

    System.Collections.IEnumerator FadeTMPTo(float targetAlpha)
    {
        float startAlpha = uiTMPText.color.a;
        float elapsed = 0f;
        while (elapsed < uiFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / uiFadeDuration);
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            Color c = uiOriginalColor;
            c.a = a;
            uiTMPText.color = c;
            yield return null;
        }
        Color final = uiOriginalColor;
        final.a = targetAlpha;
        uiTMPText.color = final;
        if (targetAlpha == 0f)
        {
            uiTMPText.gameObject.SetActive(false);
        }
        uiFadeCoroutine = null;
    }

    void ApplyHighlight()
    {
        if (isHighlighted) return;
        if (rend != null)
        {
            if (hasEmission)
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", highlightColor * emissionIntensity);
            }
            else
            {
                rend.material.color = highlightColor;
            }
        }
        transform.localScale = originalScale * highlightScaleMultiplier;
        isHighlighted = true;
    }

    void RemoveHighlight()
    {
        if (!isHighlighted) return;
        if (rend != null)
        {
            if (hasEmission)
            {
                rend.material.SetColor("_EmissionColor", originalEmissionColor);
            }
            else
            {
                rend.material.color = originalColor;
            }
        }
        transform.localScale = originalScale;
        isHighlighted = false;
    }

    void ShowUIText()
    {
        string msg = $"you have {keysCollected} out of {keysTotal} keys";
        if (uiTMPText != null)
        {
            uiTMPText.text = msg;
            uiTMPText.gameObject.SetActive(true);
        }
    }

    void HideUIText()
    {
        if (uiTMPText != null)
        {
            uiTMPText.gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        RemoveHighlight();
        HideUIText();
    }
}
