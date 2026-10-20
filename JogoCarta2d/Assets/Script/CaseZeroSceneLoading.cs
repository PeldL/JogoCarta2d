using System.Collections;
using DG.Tweening;
using EasyTransition;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Persists across the menu unload and owns the cover/load/reveal sequence.
public sealed class CaseZeroSceneLoading : MonoBehaviour
{
    CanvasGroup curtain;
    TextMeshProUGUI label;
    Transition overlay;
    Tween fade;
    Image progress;
    static CaseZeroSceneLoading active;
    string loadingText;
    bool waiting;
    float progressTarget;

    void Update()
    {
        if (progress != null)
            progress.rectTransform.anchorMax = new Vector2(progress.fillAmount, 1);
        if (!waiting) return;
        label.text = loadingText + new string('.', 1 + (int)(Time.unscaledTime * 2) % 3);
        label.alpha = 0.82f + Mathf.Sin(Time.unscaledTime * 3f) * 0.18f;
        progress.fillAmount = Mathf.MoveTowards(progress.fillAmount, progressTarget, Time.unscaledDeltaTime * 1.4f);
    }

    public static void Load(string scene, GameObject template, TransitionSettings settings, TMP_FontAsset font)
    {
        if (active != null || !Application.CanStreamedLevelBeLoaded(scene)) return;
        CaseZeroSoundFeedback.Current?.PlayTransition();
        var root = new GameObject("Caso Zero • Carregamento", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(CaseZeroSceneLoading));
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32766; // EasyTransitions is drawn immediately above this curtain.
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var loader = root.GetComponent<CaseZeroSceneLoading>();
        active = loader;
        loader.loadingText = scene == "Menu" ? "RETURNING TO MENU" : "LOADING THE INVESTIGATION";
        loader.curtain = root.GetComponent<CanvasGroup>();
        loader.curtain.alpha = 0;
        loader.curtain.blocksRaycasts = true;
        var background = new GameObject("Fundo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(root.transform, false);
        var rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = Color.black;
        var text = new GameObject("Estado", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        text.transform.SetParent(root.transform, false);
        text.GetComponent<RectTransform>().sizeDelta = new Vector2(1100, 180);
        loader.label = text.GetComponent<TextMeshProUGUI>();
        if (font != null) loader.label.font = font;
        loader.label.fontSize = 30;
        loader.label.alignment = TextAlignmentOptions.Center;
        loader.label.color = new Color(0.94f, 0.9f, 0.8f);
        loader.label.raycastTarget = false;
        loader.label.text = loader.loadingText;
        var track = new GameObject("Progress track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        track.transform.SetParent(root.transform, false);
        var trackRect = track.GetComponent<RectTransform>();
        trackRect.sizeDelta = new Vector2(440, 3);
        trackRect.anchoredPosition = new Vector2(0, -65);
        track.GetComponent<Image>().color = new Color(0.94f, 0.9f, 0.8f, 0.15f);
        track.GetComponent<Image>().raycastTarget = false;
        var bar = new GameObject("Progress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(track.transform, false);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = Vector2.zero; barRect.anchorMax = Vector2.one;
        barRect.offsetMin = barRect.offsetMax = Vector2.zero;
        loader.progress = bar.GetComponent<Image>();
        loader.progress.color = new Color(0.94f, 0.9f, 0.8f);
        loader.progress.type = Image.Type.Simple;
        barRect.anchorMax = new Vector2(0, 1);
        loader.progress.fillAmount = 0;
        loader.progress.raycastTarget = false;
        loader.StartCoroutine(loader.Run(scene, template, settings));
    }

    IEnumerator Run(string scene, GameObject template, TransitionSettings settings)
    {
        bool useAsset = template != null && template.GetComponent<Transition>() != null && settings != null &&
            settings.transitionIn != null && settings.transitionOut != null;
        float revealTime = 0.45f;
        if (useAsset)
        {
            overlay = Instantiate(template).GetComponent<Transition>();
            overlay.transitionSettings = settings;
            // Let the asset initialize, then take ownership of its reveal timing.
            yield return null;
            SceneManager.sceneLoaded -= overlay.OnSceneLoad;
            foreach (var animator in overlay.GetComponentsInChildren<Animator>(true))
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            float speed = settings.autoAdjustTransitionTime ? Mathf.Max(0.01f, settings.transitionSpeed) : 1f;
            yield return new WaitForSecondsRealtime(settings.transitionTime / speed);
            curtain.alpha = 1;
            overlay.transitionPanelIN.gameObject.SetActive(false);
            revealTime = settings.destroyTime / speed;
        }
        else
        {
            fade = curtain.DOFade(1, 0.45f).SetUpdate(true);
            yield return fade.WaitForCompletion();
        }

        waiting = true;
        var operation = SceneManager.LoadSceneAsync(scene);
        while (operation != null && !operation.isDone)
        {
            progressTarget = Mathf.Clamp01(operation.progress / 0.9f) * 0.85f;
            yield return null;
        }
        progressTarget = 0.95f;
        // sceneLoaded fires before Start and before additive location/tutorial loads finish.
        yield return null;
        float deadline = Time.realtimeSinceStartup + 30;
        var game = FindFirstObjectByType<CaseZeroGame>();
        while (game != null && !game.ReadyForReveal && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (game != null && !game.ReadyForReveal)
            Debug.LogWarning("[Caso Zero] A inicialização não terminou em 30 segundos. Revelando a interface para permitir diagnóstico.");
        Canvas.ForceUpdateCanvases();
        waiting = false;
        label.alpha = 1;
        label.text = scene == "Menu" ? "MENU READY" : "INVESTIGATION READY";
        fade = progress.DOFillAmount(1, 0.2f).SetUpdate(true);
        yield return fade.WaitForCompletion();
        yield return new WaitForSecondsRealtime(0.12f);
        if (overlay != null)
        {
            overlay.OnSceneLoad(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            foreach (var animator in overlay.GetComponentsInChildren<Animator>(true))
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            // The OUT prefab starts covered; remove the loading curtain underneath it.
            curtain.alpha = 0;
            yield return new WaitForSecondsRealtime(revealTime);
        }
        else
        {
            fade = curtain.DOFade(0, revealTime).SetUpdate(true);
            yield return fade.WaitForCompletion();
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (active == this) active = null;
        fade?.Kill();
        if (overlay != null)
        {
            SceneManager.sceneLoaded -= overlay.OnSceneLoad;
            Destroy(overlay.gameObject);
        }
    }
}
