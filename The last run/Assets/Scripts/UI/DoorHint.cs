using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DoorHint : MonoBehaviour
{
    public static DoorHint Instance;

    public float fadeDuration = 0.5f;
    public Font font;

    Canvas uiCanvas;
    Text messageText;
    CanvasGroup group;

    Coroutine currentToast;

    void Awake()
    {
        if (Instance == null) {
             Instance = this;
        }
        else if (Instance != this) {
            Destroy(gameObject);
        }

        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                Debug.LogWarning("DoorHint: failed to load built-in font LegacyRuntime.ttf, falling back to GUI.skin.font");
                font = GUI.skin != null ? GUI.skin.font : null;
            }
        }

        CreateUI();
    }

    void CreateUI()
    {
        uiCanvas = new GameObject("DoorHintCanvas").AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.gameObject.AddComponent<CanvasScaler>();
        uiCanvas.gameObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(uiCanvas.gameObject);

        GameObject go = new GameObject("DoorHintText");
        go.transform.SetParent(uiCanvas.transform, false);
        messageText = go.AddComponent<Text>();
        messageText.font = font;
        messageText.alignment = TextAnchor.LowerCenter;
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Truncate;
        messageText.fontSize = 24;
        messageText.color = new Color(1f, 1f, 1f, 1f);

        RectTransform rt = messageText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 30f); // 30px above bottom
        rt.sizeDelta = new Vector2(800f, 60f);

        group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
    }

    public void ShowMessage(string message, float displaySeconds = 2f)
    {
        if (currentToast != null) StopCoroutine(currentToast);
        currentToast = StartCoroutine(DoToast(message, displaySeconds));
    }

    IEnumerator DoToast(string message, float displaySeconds)
    {
        messageText.text = message;
        // fade in
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        group.alpha = 1f;

        // wait
        yield return new WaitForSeconds(displaySeconds);

        // fade out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        group.alpha = 0f;
        currentToast = null;
    }
}
