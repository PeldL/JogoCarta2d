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
    public SistemaInventario inventario;
    public GameManager gameManager;

    [Header("Efeitos")]
    public float fadeDuration = 0.5f;

    [Header("Feedback")]
    public GameObject painelFeedbackColeta;
    public TextMeshProUGUI textoFeedbackColeta;

    [Header("Canvas — arraste o Canvas da UI para garantir posicionamento correto")]
    public RectTransform canvasRectTransform; // necessário para converter posição de mundo → UI

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
                if (item.textoNomeFlutuante != null)
                {
                    Color c = item.textoNomeFlutuante.color;
                    c.a = 0f;
                    item.textoNomeFlutuante.color = c;
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

        Button btn = obj.GetComponent<Button>() ?? obj.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => StartCoroutine(ColetarItemComEfeito(item)));

        EventTrigger trigger = obj.GetComponent<EventTrigger>() ?? obj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        AddTrigger(trigger, EventTriggerType.PointerEnter, _ => StartCoroutine(MostrarNomeItem(item)));
        AddTrigger(trigger, EventTriggerType.PointerExit,  _ => StartCoroutine(EsconderNomeItem(item)));
    }

    void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    // ── Hover nome ────────────────────────────────────────────────────────────

    IEnumerator MostrarNomeItem(ItemInvestigacao item)
    {
        if (item.textoNomeFlutuante == null) yield break;

        PositionarTextoFlutuante(item);
        yield return StartCoroutine(FadeUtils.FadeTMP(this, item.textoNomeFlutuante, 0f, 1f, fadeDuration));
    }

    IEnumerator EsconderNomeItem(ItemInvestigacao item)
    {
        if (item.textoNomeFlutuante == null) yield break;
        yield return StartCoroutine(FadeUtils.FadeTMP(this, item.textoNomeFlutuante, 1f, 0f, fadeDuration));
    }

    // Corrigido: usa RectTransformUtility para funcionar com qualquer modo de Canvas
    void PositionarTextoFlutuante(ItemInvestigacao item)
    {
        if (item.textoNomeFlutuante == null || Camera.main == null) return;

        Vector3 posicaoTela = Camera.main.WorldToScreenPoint(item.objetoNoMundo.transform.position);
        posicaoTela.y += 50f;

        if (canvasRectTransform != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                posicaoTela,
                null, // null = Screen Space Overlay; passe a câmera do Canvas se for Screen Space Camera
                out Vector2 localPoint);
            item.textoNomeFlutuante.rectTransform.localPosition = localPoint;
        }
        else
        {
            // Fallback para Screen Space - Overlay
            item.textoNomeFlutuante.transform.position = posicaoTela;
        }
    }

    // ── Coleta ────────────────────────────────────────────────────────────────

    IEnumerator ColetarItemComEfeito(ItemInvestigacao item)
    {
        if (item.coletado) yield break;
        item.coletado = true;

        if (item.textoNomeFlutuante != null)
            yield return StartCoroutine(EsconderNomeItem(item));

        if (item.objetoNoMundo != null)
        {
            Vector3 startScale = item.objetoNoMundo.transform.localScale;
            SpriteRenderer sr  = item.objetoNoMundo.GetComponent<SpriteRenderer>();

            yield return FadeUtils.Fade(this, t =>
            {
                item.objetoNoMundo.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                if (sr != null)
                {
                    Color c = sr.color; c.a = 1f - t; sr.color = c;
                }
            }, 0f, 1f, fadeDuration);

            Destroy(item.objetoNoMundo);
        }

        inventario?.AdicionarItem(item.nomeItem, item.descricao, item.iconeItem);

        yield return StartCoroutine(MostrarFeedbackColeta(item.nomeItem));

        item.dicaAssociada?.sistemaPericia?.AdicionarLigacao();

        gameManager?.RegistrarPista(item.idItem, item.descricao);
    }

    IEnumerator MostrarFeedbackColeta(string nomeItem)
    {
        if (painelFeedbackColeta == null || textoFeedbackColeta == null) yield break;

        textoFeedbackColeta.text = $"✓ {nomeItem} coletado!";
        painelFeedbackColeta.SetActive(true);

        CanvasGroup cg = FadeUtils.GetOrAddCanvasGroup(painelFeedbackColeta);
        yield return StartCoroutine(FadeUtils.FadeCanvasGroup(this, cg, 0f, 1f, fadeDuration));
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(FadeUtils.FadeCanvasGroup(this, cg, 1f, 0f, fadeDuration));

        painelFeedbackColeta.SetActive(false);
    }
}
