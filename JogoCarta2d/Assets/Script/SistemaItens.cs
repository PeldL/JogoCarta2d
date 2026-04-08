using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SistemaItens : MonoBehaviour
{
    [System.Serializable]
    public class ItemInvestigacao
    {
        public string nomeItem;
        public string idItem;
        [TextArea(2, 3)]
        public string descricao;
        public Sprite iconeItem;
        public GameObject objetoNoMundo;
        public bool coletado;
        public DicaAssociada dicaAssociada;

        [Header("UI Elements TMP")]
        public TextMeshProUGUI textoDescricaoFloating; // Texto que aparece flutuando
    }

    [System.Serializable]
    public class DicaAssociada
    {
        public string idDica;
        public SistemaPericia sistemaPericia;
    }

    [Header("Configurações")]
    public List<ItemInvestigacao> itens = new List<ItemInvestigacao>();
    public Transform gridInventario;
    public GameObject prefabItemUI;
    public TextMeshProUGUI textoDescricaoItem;

    [Header("Efeitos")]
    public float fadeDuration = 0.3f;
    public float floatUpDuration = 1f;

    void Start()
    {
        foreach (var item in itens)
        {
            if (!item.coletado && item.objetoNoMundo != null)
            {
                AdicionarClickAoItem(item);
            }
        }
    }

    void AdicionarClickAoItem(ItemInvestigacao item)
    {
        Button btn = item.objetoNoMundo.GetComponent<Button>();
        if (btn == null)
            btn = item.objetoNoMundo.AddComponent<Button>();

        btn.onClick.AddListener(() => StartCoroutine(ColetarItemComEfeito(item)));

        // Efeito hover
        EventTrigger trigger = item.objetoNoMundo.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = item.objetoNoMundo.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => StartCoroutine(MostrarDescricaoFloating(item)));
        trigger.triggers.Add(entryEnter);
    }

    IEnumerator ColetarItemComEfeito(ItemInvestigacao item)
    {
        if (item.coletado) yield break;

        item.coletado = true;

        // Efeito de scale e fade out
        if (item.objetoNoMundo != null)
        {
            CanvasGroup cg = item.objetoNoMundo.GetComponent<CanvasGroup>();
            if (cg == null) cg = item.objetoNoMundo.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            Vector3 startScale = item.objetoNoMundo.transform.localScale;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                cg.alpha = Mathf.Lerp(1f, 0f, t);
                item.objetoNoMundo.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            Destroy(item.objetoNoMundo);
        }

        AdicionarAoInventarioUI(item);

        if (item.dicaAssociada.sistemaPericia != null)
        {
            item.dicaAssociada.sistemaPericia.AdicionarLigacao();
        }

        yield return StartCoroutine(MostrarMensagemColeta(item.nomeItem));
    }

    IEnumerator MostrarDescricaoFloating(ItemInvestigacao item)
    {
        if (item.textoDescricaoFloating != null)
        {
            item.textoDescricaoFloating.text = item.descricao;
            item.textoDescricaoFloating.gameObject.SetActive(true);

            CanvasGroup cg = item.textoDescricaoFloating.GetComponent<CanvasGroup>();
            if (cg == null) cg = item.textoDescricaoFloating.gameObject.AddComponent<CanvasGroup>();

            // Fade in e sobe
            float elapsed = 0f;
            Vector3 startPos = item.textoDescricaoFloating.transform.position;

            while (elapsed < floatUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / floatUpDuration;
                cg.alpha = Mathf.Lerp(0f, 1f, t);
                item.textoDescricaoFloating.transform.position = Vector3.Lerp(startPos, startPos + Vector3.up * 30f, t);
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            item.textoDescricaoFloating.gameObject.SetActive(false);
            item.textoDescricaoFloating.transform.position = startPos;
        }
    }

    IEnumerator MostrarMensagemColeta(string nomeItem)
    {
        if (textoDescricaoItem != null)
        {
            Color corOriginal = textoDescricaoItem.color;
            textoDescricaoItem.text = $"✅ {nomeItem} coletado!";
            textoDescricaoItem.color = Color.green;

            yield return new WaitForSeconds(2f);

            textoDescricaoItem.text = "Clique nos itens para ver detalhes...";
            textoDescricaoItem.color = corOriginal;
        }
    }

    void AdicionarAoInventarioUI(ItemInvestigacao item)
    {
        if (prefabItemUI != null && gridInventario != null)
        {
            GameObject novoItem = Instantiate(prefabItemUI, gridInventario);

            Image iconImage = novoItem.GetComponent<Image>();
            if (iconImage != null && item.iconeItem != null)
                iconImage.sprite = item.iconeItem;

            Button btn = novoItem.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => MostrarDescricaoItem(item.descricao));
            }

            // Efeito de entrada do item no inventário
            CanvasGroup cg = novoItem.GetComponent<CanvasGroup>();
            if (cg == null) cg = novoItem.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            StartCoroutine(FadeInObject(cg, 0.3f));
        }
    }

    void MostrarDescricaoItem(string descricao)
    {
        if (textoDescricaoItem != null)
        {
            StartCoroutine(MostrarDescricaoComFade(descricao));
        }
    }

    IEnumerator MostrarDescricaoComFade(string descricao)
    {
        yield return StartCoroutine(FadeTextMeshPro(textoDescricaoItem, 1f, 0f, 0.2f));
        textoDescricaoItem.text = descricao;
        yield return StartCoroutine(FadeTextMeshPro(textoDescricaoItem, 0f, 1f, 0.2f));

        yield return new WaitForSeconds(5f);

        yield return StartCoroutine(FadeTextMeshPro(textoDescricaoItem, 1f, 0f, 0.2f));
        textoDescricaoItem.text = "Clique nos itens para ver detalhes...";
        yield return StartCoroutine(FadeTextMeshPro(textoDescricaoItem, 0f, 1f, 0.2f));
    }

    IEnumerator FadeTextMeshPro(TextMeshProUGUI tmp, float startAlpha, float endAlpha, float duration)
    {
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

    IEnumerator FadeInObject(CanvasGroup cg, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }
}