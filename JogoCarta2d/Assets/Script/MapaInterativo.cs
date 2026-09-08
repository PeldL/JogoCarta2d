using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using EasyTransition;

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
        public H_SDFUIOutline outlineEffect;

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
    public Canvas canvasPrincipal;

    public float tempoResetDescricao = 3f;

    [Header("Som")]
    public AudioSource audioSource;
    public AudioClip somHoverPadrao;
    public AudioClip somCliquePadrao;

    [Header("Pulso do outline")]
    public float pulseMinAlpha = 0.65f;
    public float pulseMaxAlpha = 1f;
    public float pulseVelocidade = 1.5f;

    [Header("Piscar do indicador de área não visitada")]
    public float indicadorVelocidade = 1f;

    [Header("Transição de cena (EasyTransitions)")]
    [Tooltip("Arraste aqui o TransitionSettings configurado no projeto.")]
    public TransitionSettings transitionSettings;
    public float transitionDelay = 0f;

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
        if (area.hoverCoroutine != null)
            StopCoroutine(area.hoverCoroutine);

        area.isHovering = entering;
        area.hoverCoroutine = StartCoroutine(entering ? HoverEnter(area) : HoverExit(area));
    }

    IEnumerator HoverEnter(AreaMapa area)
    {
        TocarSom(area.somHoverArea != null ? area.somHoverArea : somHoverPadrao);

        if (area.textoNomeArea != null) area.textoNomeArea.text = area.nomeArea;
        if (area.textoDescricaoArea != null) area.textoDescricaoArea.text = area.descricao;

        yield return FadeArea(area, 0f, 1f);

        if (area.outlineEffect != null)
        {
            if (area.pulseCoroutine != null) StopCoroutine(area.pulseCoroutine);
            area.pulseCoroutine = StartCoroutine(PulseOutline(area));
        }

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
        if (area.pulseCoroutine != null)
        {
            StopCoroutine(area.pulseCoroutine);
            area.pulseCoroutine = null;
        }

        float alphaAtual = area.outlineEffect != null ? GetAlpha(area.outlineEffect) : 1f;
        yield return FadeArea(area, alphaAtual, 0f);

        if (textoDescricaoGlobal != null && resetDescricaoCoroutine == null)
            resetDescricaoCoroutine = StartCoroutine(ResetarDescricaoComFade());
    }

    // ── Pulso contínuo do outline ────────────────────────────────────────────

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

    // ── Piscar do indicador de área não visitada ────────────────────────────

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

            yield return null;
        }
    }

    // ── Som ──────────────────────────────────────────────────────────────────

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
        }, from, to, area.fadeDuration);
    }

    // ── Click / cena ─────────────────────────────────────────────────────────

    void OnAreaClicada(AreaMapa area)
    {
        TocarSom(area.somCliqueArea != null ? area.somCliqueArea : somCliquePadrao);
        MarcarComoVisitada(area);

        if (!string.IsNullOrEmpty(area.nomeCenaParaCarregar))
            IniciarTransicaoDeCena(area.nomeCenaParaCarregar);
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

    // ── Save/load das áreas visitadas ──────────────────────────────────────────

    public List<string> ObterAreasVisitadas()
    {
        var lista = new List<string>();
        foreach (var area in areas)
            if (area.visitada) lista.Add(area.nomeArea);
        return lista;
    }

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

    // ── Transição de cena via EasyTransitions ───────────────────────────────

    void IniciarTransicaoDeCena(string nomeCena)
    {
        if (transitionSettings == null)
        {
            Debug.LogWarning("TransitionSettings não configurado no MapaInterativo — carregando cena sem transição.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(nomeCena);
            return;
        }

        TransitionManager.Instance().Transition(nomeCena, transitionSettings, transitionDelay);
    }

    // ── Texto global ─────────────────────────────────────────────────────────

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

    // ── Helpers ──────────────────────────────────────────────────────────────

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

    static void SetAlpha(H_SDFUIOutline outline, float a)
    {
        if (outline == null) return;
        Color c = outline.color; c.a = a; outline.color = c;
    }

    static float GetAlpha(H_SDFUIOutline outline)
    {
        return outline == null ? 1f : outline.color.a;
    }
}