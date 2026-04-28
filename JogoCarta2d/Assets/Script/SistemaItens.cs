using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

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
        public TextMeshProUGUI textoNomeFlutuante;
    }

    [System.Serializable]
    public class DicaAssociada
    {
        public string idDica;
        public SistemaPericia sistemaPericia;
    }

    [Header("Configurações")]
    public List<ItemInvestigacao> itens = new List<ItemInvestigacao>();

    [Header("Referências")]
    public SistemaInventario inventario; // Referência para o inventário separado
    public GameManager gameManager;

    [Header("Efeitos")]
    public float fadeDuration = 0.5f;

    [Header("Feedback")]
    public GameObject painelFeedbackColeta;
    public TextMeshProUGUI textoFeedbackColeta;

    void Start()
    {
        ConfigurarTodosItens();

        if (painelFeedbackColeta != null)
            painelFeedbackColeta.SetActive(false);
    }

    void ConfigurarTodosItens()
    {
        foreach (var item in itens)
        {
            if (!item.coletado && item.objetoNoMundo != null)
            {
                // Configura texto flutuante
                if (item.textoNomeFlutuante != null)
                {
                    Color cor = item.textoNomeFlutuante.color;
                    cor.a = 0f;
                    item.textoNomeFlutuante.color = cor;
                    item.textoNomeFlutuante.text = item.nomeItem;
                    item.textoNomeFlutuante.gameObject.SetActive(true);
                }

                AdicionarInteracaoAoItem(item);
            }
        }
    }

    void AdicionarInteracaoAoItem(ItemInvestigacao item)
    {
        GameObject obj = item.objetoNoMundo;

        Button btn = obj.GetComponent<Button>();
        if (btn == null) btn = obj.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => StartCoroutine(ColetarItemComEfeito(item)));

        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = obj.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => StartCoroutine(MostrarNomeItem(item)));
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => StartCoroutine(EsconderNomeItem(item)));
        trigger.triggers.Add(entryExit);
    }

    IEnumerator MostrarNomeItem(ItemInvestigacao item)
    {
        if (item.textoNomeFlutuante == null) yield break;

        Vector3 posicaoMundo = item.objetoNoMundo.transform.position;
        Vector3 posicaoTela = Camera.main.WorldToScreenPoint(posicaoMundo);
        posicaoTela.y += 50f;
        item.textoNomeFlutuante.transform.position = posicaoTela;

        float elapsed = 0f;
        Color cor = item.textoNomeFlutuante.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cor.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            item.textoNomeFlutuante.color = cor;
            yield return null;
        }

        cor.a = 1f;
        item.textoNomeFlutuante.color = cor;
    }

    IEnumerator EsconderNomeItem(ItemInvestigacao item)
    {
        if (item.textoNomeFlutuante == null) yield break;

        float elapsed = 0f;
        Color cor = item.textoNomeFlutuante.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cor.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            item.textoNomeFlutuante.color = cor;
            yield return null;
        }

        cor.a = 0f;
        item.textoNomeFlutuante.color = cor;
    }

    IEnumerator ColetarItemComEfeito(ItemInvestigacao item)
    {
        if (item.coletado) yield break;

        item.coletado = true;

        // 1. Esconde o nome
        if (item.textoNomeFlutuante != null)
        {
            yield return StartCoroutine(EsconderNomeItem(item));
        }

        // 2. Efeito no item do mundo
        if (item.objetoNoMundo != null)
        {
            float elapsed = 0f;
            Vector3 startScale = item.objetoNoMundo.transform.localScale;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                item.objetoNoMundo.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

                SpriteRenderer sr = item.objetoNoMundo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color cor = sr.color;
                    cor.a = Mathf.Lerp(1f, 0f, t);
                    sr.color = cor;
                }
                yield return null;
            }

            Destroy(item.objetoNoMundo);
        }

        // 3. ADICIONA AO INVENTÁRIO (chamando o sistema separado)
        if (inventario != null)
        {
            inventario.AdicionarItem(item.nomeItem, item.descricao, item.iconeItem);
        }

        // 4. Mostra feedback
        yield return StartCoroutine(MostrarFeedbackColeta(item.nomeItem));

        // 5. Desbloqueia dica
        if (item.dicaAssociada.sistemaPericia != null)
        {
            item.dicaAssociada.sistemaPericia.AdicionarLigacao();
        }

        // 6. Registra no GameManager
        if (gameManager != null)
        {
            gameManager.RegistrarPista(item.idItem, item.descricao);
        }
    }

    IEnumerator MostrarFeedbackColeta(string nomeItem)
    {
        if (painelFeedbackColeta != null && textoFeedbackColeta != null)
        {
            textoFeedbackColeta.text = $"✓ {nomeItem} coletado!";
            painelFeedbackColeta.SetActive(true);

            CanvasGroup cg = painelFeedbackColeta.GetComponent<CanvasGroup>();
            if (cg == null) cg = painelFeedbackColeta.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            cg.alpha = 1f;

            yield return new WaitForSeconds(2f);

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            cg.alpha = 0f;
            painelFeedbackColeta.SetActive(false);
        }
    }
}