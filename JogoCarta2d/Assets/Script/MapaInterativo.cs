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
        [Header("Configuração da Área")]
        public string nomeArea;

        [Header("Elementos Visuais")]
        public Button botaoArea;
        public Image outlineEffect;
        public CanvasGroup areaCanvasGroup;

        [Header("Textos (TextMeshPro)")]
        public TextMeshProUGUI textoNomeArea;
        public TextMeshProUGUI textoDescricaoArea;

        [Header("Configurações")]
        [TextArea(2, 4)]
        public string descricao;
        public string nomeCenaParaCarregar;

        [Header("Fade Config")]
        public float fadeDuration = 0.3f;

        [HideInInspector] public bool isHovering = false;
    }

    public List<AreaMapa> areas = new List<AreaMapa>();
    public TextMeshProUGUI textoDescricaoGlobal;
    public float tempoResetDescricao = 3f;

    private Coroutine resetDescricaoCoroutine;

    void Start()
    {
        foreach (var area in areas)
        {
            // Configura outlines invisíveis
            if (area.outlineEffect != null)
            {
                Color cor = area.outlineEffect.color;
                cor.a = 0f;
                area.outlineEffect.color = cor;
                area.outlineEffect.gameObject.SetActive(true);
            }

            // Configura textos - TODOS começam invisíveis e desativados
            if (area.textoNomeArea != null)
            {
                Color cor = area.textoNomeArea.color;
                cor.a = 0f;
                area.textoNomeArea.color = cor;
                area.textoNomeArea.gameObject.SetActive(true); // Ativo mas invisível
            }

            if (area.textoDescricaoArea != null)
            {
                Color cor = area.textoDescricaoArea.color;
                cor.a = 0f;
                area.textoDescricaoArea.color = cor;
                area.textoDescricaoArea.gameObject.SetActive(true); // Ativo mas invisível
            }

            // Configura CanvasGroup
            if (area.areaCanvasGroup != null)
                area.areaCanvasGroup.alpha = 0f;

            // Adiciona eventos
            AddHoverEvents(area);
            area.botaoArea.onClick.AddListener(() => OnAreaClicada(area));
        }
    }

    void AddHoverEvents(AreaMapa area)
    {
        EventTrigger trigger = area.botaoArea.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = area.botaoArea.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { StartCoroutine(OnHoverEnter(area)); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { StartCoroutine(OnHoverExit(area)); });
        trigger.triggers.Add(entryExit);
    }

    IEnumerator OnHoverEnter(AreaMapa area)
    {
        if (area.isHovering) yield break;
        area.isHovering = true;

        // Atualiza os textos ANTES do fade
        if (area.textoNomeArea != null)
            area.textoNomeArea.text = area.nomeArea;

        if (area.textoDescricaoArea != null)
            area.textoDescricaoArea.text = area.descricao;

        // ===== FADE SINCRONIZADO =====
        // Todos os elementos fazem fade IN juntos

        float elapsed = 0f;

        while (elapsed < area.fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / area.fadeDuration; // 0 a 1
            float alpha = Mathf.Lerp(0f, 1f, t);

            // 1. Fade do outline
            if (area.outlineEffect != null)
            {
                Color cor = area.outlineEffect.color;
                cor.a = alpha;
                area.outlineEffect.color = cor;
            }

            // 2. Fade do CanvasGroup
            if (area.areaCanvasGroup != null)
                area.areaCanvasGroup.alpha = alpha;

            // 3. Fade do NOME (TMP) - SINCRONIZADO
            if (area.textoNomeArea != null)
            {
                Color cor = area.textoNomeArea.color;
                cor.a = alpha;
                area.textoNomeArea.color = cor;
            }

            // 4. Fade da DESCRIÇÃO (TMP) - SINCRONIZADO
            if (area.textoDescricaoArea != null)
            {
                Color cor = area.textoDescricaoArea.color;
                cor.a = alpha;
                area.textoDescricaoArea.color = cor;
            }

            yield return null;
        }

        // Garante que todos terminaram em alpha = 1
        if (area.outlineEffect != null)
        {
            Color cor = area.outlineEffect.color;
            cor.a = 1f;
            area.outlineEffect.color = cor;
        }

        if (area.areaCanvasGroup != null)
            area.areaCanvasGroup.alpha = 1f;

        if (area.textoNomeArea != null)
        {
            Color cor = area.textoNomeArea.color;
            cor.a = 1f;
            area.textoNomeArea.color = cor;
        }

        if (area.textoDescricaoArea != null)
        {
            Color cor = area.textoDescricaoArea.color;
            cor.a = 1f;
            area.textoDescricaoArea.color = cor;
        }

        // Texto global (opcional)
        if (textoDescricaoGlobal != null)
        {
            yield return StartCoroutine(FadeTextMeshProGlobal(textoDescricaoGlobal, 1f, 0f, 0.2f));
            textoDescricaoGlobal.text = $"{area.nomeArea}: {area.descricao}";
            yield return StartCoroutine(FadeTextMeshProGlobal(textoDescricaoGlobal, 0f, 1f, 0.2f));

            if (resetDescricaoCoroutine != null)
                StopCoroutine(resetDescricaoCoroutine);
        }
    }

    IEnumerator OnHoverExit(AreaMapa area)
    {
        if (!area.isHovering) yield break;
        area.isHovering = false;

        // ===== FADE SINCRONIZADO DE SAÍDA =====
        // Todos os elementos fazem fade OUT juntos

        float elapsed = 0f;

        while (elapsed < area.fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / area.fadeDuration; // 0 a 1
            float alpha = Mathf.Lerp(1f, 0f, t);

            // 1. Fade do outline
            if (area.outlineEffect != null)
            {
                Color cor = area.outlineEffect.color;
                cor.a = alpha;
                area.outlineEffect.color = cor;
            }

            // 2. Fade do CanvasGroup
            if (area.areaCanvasGroup != null)
                area.areaCanvasGroup.alpha = alpha;

            // 3. Fade do NOME (TMP) - SINCRONIZADO
            if (area.textoNomeArea != null)
            {
                Color cor = area.textoNomeArea.color;
                cor.a = alpha;
                area.textoNomeArea.color = cor;
            }

            // 4. Fade da DESCRIÇÃO (TMP) - SINCRONIZADO
            if (area.textoDescricaoArea != null)
            {
                Color cor = area.textoDescricaoArea.color;
                cor.a = alpha;
                area.textoDescricaoArea.color = cor;
            }

            yield return null;
        }

        // Garante que todos terminaram em alpha = 0
        if (area.outlineEffect != null)
        {
            Color cor = area.outlineEffect.color;
            cor.a = 0f;
            area.outlineEffect.color = cor;
        }

        if (area.areaCanvasGroup != null)
            area.areaCanvasGroup.alpha = 0f;

        if (area.textoNomeArea != null)
        {
            Color cor = area.textoNomeArea.color;
            cor.a = 0f;
            area.textoNomeArea.color = cor;
        }

        if (area.textoDescricaoArea != null)
        {
            Color cor = area.textoDescricaoArea.color;
            cor.a = 0f;
            area.textoDescricaoArea.color = cor;
        }

        // Reseta texto global
        if (textoDescricaoGlobal != null && resetDescricaoCoroutine == null)
        {
            resetDescricaoCoroutine = StartCoroutine(ResetarDescricaoComFade());
        }
    }

    void OnAreaClicada(AreaMapa area)
    {
        Debug.Log($"Clicou em: {area.nomeArea}");

        if (!string.IsNullOrEmpty(area.nomeCenaParaCarregar))
        {
            StartCoroutine(TransicaoDeCena(area.nomeCenaParaCarregar));
        }
    }

    IEnumerator TransicaoDeCena(string nomeCena)
    {
        GameObject fadePanel = CriarFadePanel();
        Image img = fadePanel.GetComponent<Image>();

        float elapsed = 0f;
        Color cor = img.color;

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            cor.a = Mathf.Lerp(0f, 1f, elapsed / 0.5f);
            img.color = cor;
            yield return null;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(nomeCena);
    }

    IEnumerator ResetarDescricaoComFade()
    {
        yield return new WaitForSeconds(tempoResetDescricao);

        if (textoDescricaoGlobal != null)
        {
            yield return StartCoroutine(FadeTextMeshProGlobal(textoDescricaoGlobal, 1f, 0f, 0.3f));
            textoDescricaoGlobal.text = "Passe o mouse sobre as áreas para investigar...";
            yield return StartCoroutine(FadeTextMeshProGlobal(textoDescricaoGlobal, 0f, 1f, 0.3f));
        }

        resetDescricaoCoroutine = null;
    }

    // Fade específico para o texto global (separado para não interferir)
    IEnumerator FadeTextMeshProGlobal(TextMeshProUGUI tmp, float startAlpha, float endAlpha, float duration)
    {
        if (tmp == null) yield break;

        float elapsed = 0f;
        Color cor = tmp.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            cor.a = alpha;
            tmp.color = cor;
            yield return null;
        }

        cor.a = endAlpha;
        tmp.color = cor;
    }

    GameObject CriarFadePanel()
    {
        GameObject fadePanel = new GameObject("FadePanel");
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas != null)
            fadePanel.transform.SetParent(canvas.transform);

        fadePanel.transform.SetAsLastSibling();

        Image img = fadePanel.AddComponent<Image>();
        img.color = Color.black;

        RectTransform rect = fadePanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return fadePanel;
    }
}