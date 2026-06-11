using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class MapaInterativo : MonoBehaviour
{
    [System.Serializable]
    public class AreaMapa
    {
        [Header("Configuração")]
        public string nomeArea;
        [TextArea(2, 4)]
        public string descricao;
        public string nomeCenaParaCarregar;

        [Header("Elementos Visuais")]
        public Button botaoArea;
        public Image outlineEffect;
        public CanvasGroup areaCanvasGroup;

        [Header("Textos (TextMeshPro)")]
        public TextMeshProUGUI textoNomeArea;
        public TextMeshProUGUI textoDescricaoArea;

        [Header("Fade")]
        public float fadeDuration = 0.3f;

        // Coroutines em andamento — necessário para cancelar antes de inverter
        [HideInInspector] public Coroutine hoverCoroutine;
        [HideInInspector] public bool isHovering;
    }

    [Header("Áreas")]
    public List<AreaMapa> areas = new List<AreaMapa>();

    [Header("UI Global")]
    public TextMeshProUGUI textoDescricaoGlobal;
    public Canvas canvasPrincipal; // Arraste o Canvas principal aqui — evita FindFirstObjectByType

    public float tempoResetDescricao = 3f;

    private Coroutine resetDescricaoCoroutine;

    void Start()
    {
        foreach (var area in areas)
        {
            InitArea(area);
            AddHoverEvents(area);
            area.botaoArea.onClick.AddListener(() => OnAreaClicada(area));
        }
    }

    void InitArea(AreaMapa area)
    {
        SetAlpha(area.outlineEffect, 0f);
        SetAlpha(area.textoNomeArea, 0f);
        SetAlpha(area.textoDescricaoArea, 0f);
        if (area.areaCanvasGroup != null) area.areaCanvasGroup.alpha = 0f;
    }

    // ── Hover ─────────────────────────────────────────────────────────────────

    void AddHoverEvents(AreaMapa area)
    {
        EventTrigger trigger = area.botaoArea.gameObject.GetComponent<EventTrigger>()
            ?? area.botaoArea.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        AddTrigger(trigger, EventTriggerType.PointerEnter, _ => StartHover(area, true));
        AddTrigger(trigger, EventTriggerType.PointerExit,  _ => StartHover(area, false));
    }

    void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    void StartHover(AreaMapa area, bool entering)
    {
        // Cancela coroutine anterior para evitar animações sobrepostas
        if (area.hoverCoroutine != null)
            StopCoroutine(area.hoverCoroutine);

        area.isHovering = entering;
        area.hoverCoroutine = StartCoroutine(entering ? HoverEnter(area) : HoverExit(area));
    }

    IEnumerator HoverEnter(AreaMapa area)
    {
        // Atualiza textos antes do fade para não ficar em branco durante a animação
        if (area.textoNomeArea != null)    area.textoNomeArea.text    = area.nomeArea;
        if (area.textoDescricaoArea != null) area.textoDescricaoArea.text = area.descricao;

        yield return FadeArea(area, 0f, 1f);

        // Texto global
        if (textoDescricaoGlobal != null)
        {
            yield return StartCoroutine(FadeUtils.FadeTMP(this, textoDescricaoGlobal, textoDescricaoGlobal.color.a, 0f, 0.2f));
            textoDescricaoGlobal.text = $"{area.nomeArea}: {area.descricao}";
            yield return StartCoroutine(FadeUtils.FadeTMP(this, textoDescricaoGlobal, 0f, 1f, 0.2f));

            if (resetDescricaoCoroutine != null)
            {
                StopCoroutine(resetDescricaoCoroutine);
                resetDescricaoCoroutine = null;
            }
        }
    }

    IEnumerator HoverExit(AreaMapa area)
    {
        yield return FadeArea(area, 1f, 0f);

        if (textoDescricaoGlobal != null && resetDescricaoCoroutine == null)
            resetDescricaoCoroutine = StartCoroutine(ResetarDescricaoComFade());
    }

    IEnumerator FadeArea(AreaMapa area, float from, float to)
    {
        yield return FadeUtils.Fade(this, a =>
        {
            SetAlpha(area.outlineEffect, a);
            SetAlpha(area.textoNomeArea, a);
            SetAlpha(area.textoDescricaoArea, a);
            if (area.areaCanvasGroup != null) area.areaCanvasGroup.alpha = a;
        }, from, to, area.fadeDuration);
    }

    // ── Click / cena ──────────────────────────────────────────────────────────

    void OnAreaClicada(AreaMapa area)
    {
        if (!string.IsNullOrEmpty(area.nomeCenaParaCarregar))
            StartCoroutine(TransicaoDeCena(area.nomeCenaParaCarregar));
    }

    IEnumerator TransicaoDeCena(string nomeCena)
    {
        GameObject fadePanel = CriarFadePanel();
        Image img = fadePanel.GetComponent<Image>();

        yield return FadeUtils.FadeImage(this, img, 0f, 1f, 0.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(nomeCena);
    }

    // ── Texto global ──────────────────────────────────────────────────────────

    IEnumerator ResetarDescricaoComFade()
    {
        yield return new WaitForSeconds(tempoResetDescricao);

        if (textoDescricaoGlobal != null)
        {
            yield return StartCoroutine(FadeUtils.FadeTMP(this, textoDescricaoGlobal, 1f, 0f, 0.3f));
            textoDescricaoGlobal.text = "Passe o mouse sobre as áreas para investigar...";
            yield return StartCoroutine(FadeUtils.FadeTMP(this, textoDescricaoGlobal, 0f, 1f, 0.3f));
        }

        resetDescricaoCoroutine = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color; c.a = a; img.color = c;
    }

    static void SetAlpha(TextMeshProUGUI tmp, float a)
    {
        if (tmp == null) return;
        Color c = tmp.color; c.a = a; tmp.color = c;
    }

    GameObject CriarFadePanel()
    {
        GameObject fadePanel = new GameObject("FadePanel");

        // Usa referência direta ao invés de FindFirstObjectByType para garantir o Canvas certo
        Canvas canvas = canvasPrincipal != null
            ? canvasPrincipal
            : FindFirstObjectByType<Canvas>();

        if (canvas != null)
            fadePanel.transform.SetParent(canvas.transform, false);

        fadePanel.transform.SetAsLastSibling();

        Image img = fadePanel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rect = fadePanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return fadePanel;
    }
}
