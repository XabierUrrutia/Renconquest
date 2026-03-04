using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class VideoOverlayPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Defaults")]
    public VideoClip defaultClip;
    public bool allowSkip = true;

    [Header("Skip Safety")]
    [SerializeField] private float skipInputDelay = 0.15f; // evita o clique inicial dar skip
    private float skipBlockUntil = 0f;

    [Header("Force End (optional)")]
    [Tooltip("0 = desativado. Ex: 16 para forçar terminar aos 16s.")]
    [SerializeField] private float forceEndAfterSeconds = 0f;

    [Tooltip("Se o vídeo ficar com o frame preso por mais de X segundos, termina (0 = desativado).")]
    [SerializeField] private float stuckFrameTimeoutSeconds = 0.75f;

    [Header("Loading UI (optional)")]
    [SerializeField] private GameObject loadingRoot;   // LoadingUI
    [SerializeField] private Slider loadingBar;        // LoadingBar (Slider)
    [SerializeField] private TMP_Text loadingText;     // Texto "LOADING"

    [Header("Disable while playing (optional)")]
    public GameObject[] disableWhilePlaying;

    private Action onFinish;
    private bool isPlaying;
    private bool keepOverlayOnFinish;
    private bool finished;

    // timers/watch
    private float playStartRealtime;
    private long lastVideoFrame = -1;
    private float lastFrameChangeRealtime = 0f;

    // async load tracking (para barra)
    private AsyncOperation currentLoadOp;

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        Hide();

        if (loadingRoot) loadingRoot.SetActive(false);
        if (loadingBar) loadingBar.value = 0f;
        if (loadingText) loadingText.text = "";
    }

    void Update()
    {
        // Atualiza barra se estiver a carregar
        if (currentLoadOp != null)
        {
            float p = Mathf.Clamp01(currentLoadOp.progress / 0.9f);
            if (loadingBar) loadingBar.value = p;
            if (loadingText) loadingText.text = $"LOADING... {Mathf.RoundToInt(p * 100f)}%";
        }

        if (!isPlaying) return;

        // 1) FORCE END por relógio real (o que pediste)
        if (forceEndAfterSeconds > 0f && (Time.realtimeSinceStartup - playStartRealtime) >= forceEndAfterSeconds)
        {
            Finish();
            return;
        }

        // 2) Watchdog: se o frame ficar preso, termina (útil quando o vídeo não dispara eventos)
        if (stuckFrameTimeoutSeconds > 0f && videoPlayer != null)
        {
            long f = videoPlayer.frame;
            if (f != lastVideoFrame && f >= 0)
            {
                lastVideoFrame = f;
                lastFrameChangeRealtime = Time.realtimeSinceStartup;
            }
            else
            {
                // só começa a contar depois do vídeo ter realmente “andado”
                if (lastVideoFrame > 0 && (Time.realtimeSinceStartup - lastFrameChangeRealtime) >= stuckFrameTimeoutSeconds)
                {
                    Finish();
                    return;
                }
            }
        }

        // 3) Skip (opcional)
        if (!allowSkip) return;
        if (Time.unscaledTime < skipBlockUntil) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            Finish();
    }

    public void PlayDefaultAndLoad(string sceneName)
    {
        if (!defaultClip) { Debug.LogWarning("[VideoOverlayPlayer] Default clip em falta."); return; }
        PlayInternal(defaultClip, () => SceneManager.LoadScene(sceneName), keepOverlayUntilSceneLoads: false);
    }

    public void PlayDefaultAndLoadAsync(string sceneName)
    {
        if (!defaultClip) { Debug.LogWarning("[VideoOverlayPlayer] Default clip em falta."); return; }
        PlayInternal(defaultClip, () => StartAsyncLoad(sceneName), keepOverlayUntilSceneLoads: true);
    }

    public void Play(VideoClip clip, Action after)
    {
        PlayInternal(clip, after, keepOverlayUntilSceneLoads: false);
    }

    private void PlayInternal(VideoClip clip, Action after, bool keepOverlayUntilSceneLoads)
    {
        if (!clip) { Debug.LogWarning("[VideoOverlayPlayer] Clip em falta."); return; }

        finished = false;
        keepOverlayOnFinish = keepOverlayUntilSceneLoads;
        onFinish = after;

        Show();

        // reset loading
        currentLoadOp = null;
        if (loadingRoot) loadingRoot.SetActive(false);
        if (loadingBar) loadingBar.value = 0f;
        if (loadingText) loadingText.text = "";

        // desativar coisas
        foreach (var go in disableWhilePlaying)
            if (go) go.SetActive(false);

        // setup vídeo
        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;

        // proteção de clique inicial (OnMouseDown)
        skipBlockUntil = Time.unscaledTime + skipInputDelay;

        // timers/watch
        playStartRealtime = Time.realtimeSinceStartup;
        lastVideoFrame = -1;
        lastFrameChangeRealtime = Time.realtimeSinceStartup;

        videoPlayer.loopPointReached -= OnVideoEnded;
        videoPlayer.loopPointReached += OnVideoEnded;

        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.prepareCompleted += OnPrepared;

        isPlaying = true;
        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer vp) => vp.Play();

    private void OnVideoEnded(VideoPlayer vp) => Finish();

    private void StartAsyncLoad(string sceneName)
    {
        if (loadingRoot) loadingRoot.SetActive(true);
        if (loadingBar) loadingBar.value = 0f;
        if (loadingText) loadingText.text = "LOADING... 0%";

        currentLoadOp = SceneManager.LoadSceneAsync(sceneName);
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;

        isPlaying = false;

        // stop vídeo (safe)
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnded;
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.Stop();
        }

        // Se vamos carregar cena: mantém overlay e mantém tudo desativado
        if (keepOverlayOnFinish)
        {
            if (loadingRoot) loadingRoot.SetActive(true);
            if (loadingBar) loadingBar.value = 0f;
            if (loadingText) loadingText.text = "LOADING... 0%";

            onFinish?.Invoke();
            onFinish = null;
            return;
        }

        // caso normal
        Hide();

        foreach (var go in disableWhilePlaying)
            if (go) go.SetActive(true);

        if (loadingRoot) loadingRoot.SetActive(false);
        if (loadingBar) loadingBar.value = 0f;
        if (loadingText) loadingText.text = "";

        onFinish?.Invoke();
        onFinish = null;
    }

    private void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
