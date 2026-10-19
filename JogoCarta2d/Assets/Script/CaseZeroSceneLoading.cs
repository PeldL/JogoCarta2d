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

    public static void Load(string scene, GameObject template, TransitionSettings settings, TMP_FontAsset font)
    {
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
        loader.label.text = "CARREGANDO A INVESTIGAÇÃO…";
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

        var operation = SceneManager.LoadSceneAsync(scene);
        if (operation != null) yield return operation;
        // sceneLoaded fires before Start and before additive location/tutorial loads finish.
        yield return null;
        float deadline = Time.realtimeSinceStartup + 30;
        var game = FindFirstObjectByType<CaseZeroGame>();
        while (game != null && !game.ReadyForReveal && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (game != null && !game.ReadyForReveal)
            Debug.LogWarning("[Caso Zero] A inicialização não terminou em 30 segundos. Revelando a interface para permitir diagnóstico.");
        Canvas.ForceUpdateCanvases();
        label.text = "INVESTIGAÇÃO PRONTA";
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
        fade?.Kill();
        if (overlay != null)
        {
            SceneManager.sceneLoaded -= overlay.OnSceneLoad;
            Destroy(overlay.gameObject);
        }
    }
}
