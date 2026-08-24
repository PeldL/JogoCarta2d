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

        [Header("Indicador de área não visitada")]
        [Tooltip("Ícone/objeto que pisca enquanto a área não foi clicada pela primeira vez. Some sozinho após a visita.")]
        public GameObject indicadorNaoVisitado;

        [Header("Som (opcional — se vazio, usa o som padrão global)")]
        public AudioClip somHoverArea;
        public AudioClip somCliqueArea;

        // Coroutines em andamento — necessário para cancelar antes de inverter
        [HideInInspector] public Coroutine hoverCoroutine;
        [HideInInspector] public Coroutine pulseCoroutine;
        [HideInInspector] public Coroutine indicadorCoroutine;
        [HideInInspector] public bool isHovering;
        [HideInInspector] public bool visitada;
    }

    [Header("Áreas")]
    public List<AreaMapa> areas = new List<AreaMapa>();

    [Header("UI Global")]
    public TextMeshProUGUI textoDescricaoGlobal;
    public Canvas canvasPrincipal; // Arraste o Canvas principal aqui — evita FindFirstObjectByType

    public float tempoResetDescricao = 3f;

    [Header("Cursor customizado — imagem de UI que segue o mouse (permite fade)")]
    [Tooltip("CanvasGroup do objeto de UI que representa o cursor customizado. Ele deve ser filho do canvasPrincipal, começar com alpha 0, e seu RectTransform é movido a cada frame para seguir o mouse.")]
    public CanvasGroup cursorCustomCanvasGroup;
    public RectTransform cursorCustomRect;
    public float cursorFadeDuration = 0.2f;

    [Tooltip("Fallback: se cursorCustomCanvasGroup estiver vazio, usa o cursor de sistema (sem transição/fade) com esta textura.")]
    public Texture2D cursorInvestigar;
    public Vector2 cursorHotspot = Vector2.zero;

    [Header("Som — arraste um AudioSource (pode ser deste mesmo objeto)")]
    public AudioSource audioSource;
    public AudioClip somHoverPadrao;
    public AudioClip somCliquePadrao;

    [Header("Pulso do outline (enquanto o mouse permanece em cima)")]
    public float pulseMinAlpha = 0.65f;
    public float pulseMaxAlpha = 1f;
    public float pulseVelocidade = 1.5f; // ciclos completos por segundo, aproximado

    [Header("Piscar do indicador de área não visitada")]
    public float indicadorVelocidade = 1f; // ciclos por segundo

    private Coroutine resetDescricaoCoroutine;
    private Coroutine cursorFadeCoroutine;

    void Start()
    {
        // Cursor de UI: não deve bloquear cliques nem ser "interagível"
        if (cursorCustomCanvasGroup != null)
        {
            cursorCustomCanvasGroup.alpha = 0f;
            cursorCustomCanvasGroup.blocksRaycasts = false;
            cursorCustomCanvasGroup.interactable = false;
        }

        foreach (var area in areas)
        {
            InitArea(area);
            AddHoverEvents(area);
            area.botaoArea.onClick.AddListener(() => OnAreaClicada(area));
        }
    }

    void Update()
    {
        // Cursor de UI segue a posição do mouse todo frame
        if (cursorCustomRect != null && canvasPrincipal != null)
        {
            Camera cam = canvasPrincipal.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvasPrincipal.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasPrincipal.transform as RectTransform,
                Input.mousePosition,
                cam,
                out Vector2 localPoint);

            cursorCustomRect.localPosition = localPoint;
        }
    }

    void InitArea(AreaMapa area)
    {
        SetAlpha(area.outlineEffect, 0f);
        SetAlpha(area.textoNomeArea, 0f);
        SetAlpha(area.textoDescricaoArea, 0f);
        if (area.areaCanvasGroup != null) area.areaCanvasGroup.alpha = 0f;

        // Indicador de "não visitado" — começa piscando se a área ainda não foi clicada
        if (area.indicadorNaoVisitado != null && !area.visitada)
        {
            area.indicadorNaoVisitado.SetActive(true);
            area.indicadorCoroutine = StartCoroutine(BlinkIndicador(area));
        }
        else if (area.indicadorNaoVisitado != null)
        {
            area.indicadorNaoVisitado.SetActive(false);
        }
    }

    // ── Hover ─────────────────────────────────────────────────────────────────

    void AddHoverEvents(AreaMapa area)
    {
        EventTrigger trigger = area.botaoArea.gameObject.GetComponent<EventTrigger>()
            ?? area.botaoArea.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        AddTrigger(trigger, EventTriggerType.PointerEnter, _ => StartHover(area, true));
        AddTrigger(trigger, EventTriggerType.PointerExit, _ => StartHover(area, false));
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
        // Cursor customizado — com transição se cursorCustomCanvasGroup estiver configurado
        MostrarCursorCustomizado();

        // Som de hover — usa o clip específico da área, senão o padrão global
        TocarSom(area.somHoverArea != null ? area.somHoverArea : somHoverPadrao);

        // Atualiza textos antes do fade para não ficar em branco durante a animação
        if (area.textoNomeArea != null) area.textoNomeArea.text = area.nomeArea;
        if (area.textoDescricaoArea != null) area.textoDescricaoArea.text = area.descricao;

        yield return FadeArea(area, 0f, 1f);

        // Pulso contínuo do outline enquanto o mouse permanecer em cima
        if (area.outlineEffect != null)
        {
            if (area.pulseCoroutine != null) StopCoroutine(area.pulseCoroutine);
            area.pulseCoroutine = StartCoroutine(PulseOutline(area));
        }

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
        // Cursor volta ao padrão — com transição se cursorCustomCanvasGroup estiver configurado
        EsconderCursorCustomizado();

        // Para o pulso e usa o alpha atual (não necessariamente 1) como ponto de partida do fade de saída
        if (area.pulseCoroutine != null)
        {
            StopCoroutine(area.pulseCoroutine);
            area.pulseCoroutine = null;
        }

        float alphaAtual = area.outlineEffect != null ? area.outlineEffect.color.a : 1f;
        yield return FadeArea(area, alphaAtual, 0f);

        if (textoDescricaoGlobal != null && resetDescricaoCoroutine == null)
            resetDescricaoCoroutine = StartCoroutine(ResetarDescricaoComFade());
    }

    // ── Pulso contínuo do outline (respiração) ──────────────────────────────────

    IEnumerator PulseOutline(AreaMapa area)
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * pulseVelocidade;
            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f);
            SetAlpha(area.outlineEffect, alpha);
            yield return null;
        }
    }

    // ── Piscar do indicador de área não visitada ────────────────────────────────

    IEnumerator BlinkIndicador(AreaMapa area)
    {
        CanvasGroup cg = area.indicadorNaoVisitado.GetComponent<CanvasGroup>();
        Image img = cg == null ? area.indicadorNaoVisitado.GetComponent<Image>() : null;

        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * indicadorVelocidade;
            float alpha = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f;

            if (cg != null) cg.alpha = alpha;
            else if (img != null) SetAlpha(img, alpha);
            // Se não tiver CanvasGroup nem Image, o objeto só fica ativo sem animação de alpha.

            yield return null;
        }
    }

    // ── Cursor customizado com transição ────────────────────────────────────────

    void MostrarCursorCustomizado()
    {
        if (cursorCustomCanvasGroup != null)
        {
            // Esconde o cursor de sistema e faz fade-in da imagem de UI
            Cursor.visible = false;

            if (cursorFadeCoroutine != null) StopCoroutine(cursorFadeCoroutine);
            cursorFadeCoroutine = StartCoroutine(
                FadeUtils.FadeCanvasGroup(this, cursorCustomCanvasGroup, cursorCustomCanvasGroup.alpha, 1f, cursorFadeDuration));
        }
        else if (cursorInvestigar != null)
        {
            // Fallback sem transição — cursor de sistema trocado instantaneamente
            Cursor.SetCursor(cursorInvestigar, cursorHotspot, CursorMode.Auto);
        }
    }

    void EsconderCursorCustomizado()
    {
        if (cursorCustomCanvasGroup != null)
        {
            if (cursorFadeCoroutine != null) StopCoroutine(cursorFadeCoroutine);
            cursorFadeCoroutine = StartCoroutine(FadeOutCursorEReativarSistema());
        }
        else if (cursorInvestigar != null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    IEnumerator FadeOutCursorEReativarSistema()
    {
        yield return FadeUtils.FadeCanvasGroup(this, cursorCustomCanvasGroup, cursorCustomCanvasGroup.alpha, 0f, cursorFadeDuration);

        // Só reativa o cursor de sistema depois que o fade-out terminou,
        // pra não dar aquele "flash" do cursor padrão no meio da transição.
        Cursor.visible = true;
    }

    // ── Som ───────────────────────────────────────────────────────────────────

    void TocarSom(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
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
        TocarSom(area.somCliqueArea != null ? area.somCliqueArea : somCliquePadrao);
        MarcarComoVisitada(area);

        if (!string.IsNullOrEmpty(area.nomeCenaParaCarregar))
            StartCoroutine(TransicaoDeCena(area.nomeCenaParaCarregar));
    }

    void MarcarComoVisitada(AreaMapa area)
    {
        if (area.visitada) return;
        area.visitada = true;

        if (area.indicadorCoroutine != null)
        {
            StopCoroutine(area.indicadorCoroutine);
            area.indicadorCoroutine = null;
        }

        if (area.indicadorNaoVisitado != null)
            area.indicadorNaoVisitado.SetActive(false);
    }

    // ── Save/load das áreas visitadas ───────────────────────────────────────────

    /// <summary>Chamado pelo AutoSaveManager ao salvar — retorna os nomes das áreas já visitadas.</summary>
    public List<string> ObterAreasVisitadas()
    {
        var lista = new List<string>();
        foreach (var area in areas)
            if (area.visitada) lista.Add(area.nomeArea);
        return lista;
    }

    /// <summary>
    /// Chamado pelo AutoSaveManager ao carregar. Funciona independente da ordem de execução:
    /// se chamado antes do Start(), InitArea() vai respeitar o "visitada" já restaurado e nem
    /// iniciar o pisca-pisca; se chamado depois, para a coroutine em andamento e esconde o ícone.
    /// </summary>
    public void RestaurarAreasVisitadas(List<string> nomesVisitados)
    {
        if (nomesVisitados == null) return;

        foreach (var area in areas)
        {
            if (!nomesVisitados.Contains(area.nomeArea)) continue;

            area.visitada = true;

            if (area.indicadorCoroutine != null)
            {
                StopCoroutine(area.indicadorCoroutine);
                area.indicadorCoroutine = null;
            }

            if (area.indicadorNaoVisitado != null)
                area.indicadorNaoVisitado.SetActive(false);
        }
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