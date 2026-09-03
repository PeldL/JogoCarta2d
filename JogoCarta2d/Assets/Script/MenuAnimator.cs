using UnityEngine;
using DG.Tweening;

public class MenuAnimator : MonoBehaviour
{
    [Header("Botões do Menu (na ordem que devem aparecer)")]
    [SerializeField] private RectTransform[] botoes;

    [Header("Configuração de Entrada")]
    [SerializeField] private float duracaoPorBotao = 0.4f;
    [SerializeField] private float intervaloEntreBotoes = 0.08f;
    [SerializeField] private float deslocamentoY = -40f;
    [SerializeField] private float reducaoEscalaInicial = 0.2f; // quanto menor ele começa, em relação à escala original
    [SerializeField] private Ease easeEntrada = Ease.OutBack;

    [Header("Configuração de Saída")]
    [SerializeField] private float duracaoSaida = 0.2f;
    [SerializeField] private Ease easeSaida = Ease.InBack;

    private Vector2[] posicoesOriginais;
    private Vector3[] escalasOriginais;
    private Sequence sequenciaAtual;

    void Awake()
    {
        posicoesOriginais = new Vector2[botoes.Length];
        escalasOriginais = new Vector3[botoes.Length];

        for (int i = 0; i < botoes.Length; i++)
        {
            posicoesOriginais[i] = botoes[i].anchoredPosition;
            escalasOriginais[i] = botoes[i].localScale; // pega a escala que já está no Inspector
        }
    }

    void OnEnable()
    {
        AnimarEntrada();
    }

    public void AnimarEntrada()
    {
        sequenciaAtual?.Kill();
        sequenciaAtual = DOTween.Sequence();

        for (int i = 0; i < botoes.Length; i++)
        {
            RectTransform botao = botoes[i];
            Vector2 posFinal = posicoesOriginais[i];
            Vector2 posInicial = posFinal + new Vector2(0f, deslocamentoY);
            Vector3 escalaFinal = escalasOriginais[i];
            Vector3 escalaInicial = escalaFinal * (1f - reducaoEscalaInicial);

            CanvasGroup canvasGroup = ObterOuCriarCanvasGroup(botao);

            botao.anchoredPosition = posInicial;
            botao.localScale = escalaInicial;
            canvasGroup.alpha = 0f;

            Sequence entradaBotao = DOTween.Sequence();
            entradaBotao.Join(botao.DOAnchorPos(posFinal, duracaoPorBotao).SetEase(easeEntrada));
            entradaBotao.Join(botao.DOScale(escalaFinal, duracaoPorBotao).SetEase(easeEntrada));
            entradaBotao.Join(canvasGroup.DOFade(1f, duracaoPorBotao * 0.7f));

            sequenciaAtual.Insert(i * intervaloEntreBotoes, entradaBotao);
        }
    }

    public void AnimarSaida(System.Action aoFinalizar = null)
    {
        sequenciaAtual?.Kill();
        sequenciaAtual = DOTween.Sequence();

        for (int i = botoes.Length - 1; i >= 0; i--)
        {
            RectTransform botao = botoes[i];
            Vector3 escalaFinal = escalasOriginais[i];
            Vector3 escalaSaida = escalaFinal * (1f - reducaoEscalaInicial);
            CanvasGroup canvasGroup = ObterOuCriarCanvasGroup(botao);

            Sequence saidaBotao = DOTween.Sequence();
            saidaBotao.Join(botao.DOScale(escalaSaida, duracaoSaida).SetEase(easeSaida));
            saidaBotao.Join(canvasGroup.DOFade(0f, duracaoSaida));

            int indiceInverso = botoes.Length - 1 - i;
            sequenciaAtual.Insert(indiceInverso * (intervaloEntreBotoes * 0.5f), saidaBotao);
        }

        sequenciaAtual.OnComplete(() => aoFinalizar?.Invoke());
    }

    private CanvasGroup ObterOuCriarCanvasGroup(RectTransform botao)
    {
        CanvasGroup cg = botao.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = botao.gameObject.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    void OnDestroy()
    {
        sequenciaAtual?.Kill();
    }
}