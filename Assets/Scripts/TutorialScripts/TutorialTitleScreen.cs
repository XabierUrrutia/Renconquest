using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialTitleScreen : MonoBehaviour
{
    [Header("UI")]
    public GameObject titlePanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    [Header("Settings")]
    public float displayDuration = 5f;
    [Tooltip("Fade out duration in seconds. 0 = instant hide.")]
    public float fadeDuration = 0.5f;
    public string title = "Tutorial 1";
    public string subtitle = "";

    private CanvasGroup canvasGroup;
    private GameObject inputBlocker;

    void Start()
    {
        if (titlePanel != null)
        {
            titlePanel.SetActive(true);
            canvasGroup = titlePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = titlePanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
        }

        if (titleText != null)    titleText.text = title;
        if (subtitleText != null) subtitleText.text = subtitle;

        CreateInputBlocker();
        Time.timeScale = 0f;
        StartCoroutine(HideAfterDelay());
    }

    void CreateInputBlocker()
    {
        // Full-screen transparent canvas on top of everything to block all input
        inputBlocker = new GameObject("TitleInputBlocker");
        DontDestroyOnLoad(inputBlocker);

        Canvas canvas = inputBlocker.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        inputBlocker.AddComponent<GraphicRaycaster>();

        GameObject img = new GameObject("Blocker");
        img.transform.SetParent(inputBlocker.transform, false);
        Image image = img.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0); // transparent but blocks raycasts
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    IEnumerator HideAfterDelay()
    {
        float elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (fadeDuration > 0f && canvasGroup != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - (t / fadeDuration);
                yield return null;
            }
        }

        Hide();
    }

    void Hide()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (inputBlocker != null) Destroy(inputBlocker);
        Time.timeScale = 1f;
    }

    public void SkipTitle()
    {
        StopAllCoroutines();
        Hide();
    }
}
