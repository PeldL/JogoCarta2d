using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SistemaPericia : MonoBehaviour
{
    [System.Serializable]
    public class Dica
    {
        public string idDica;
        [TextArea(3, 5)]
        public string textoDica;
        public bool revelada;
        public GameObject iconeDica;
    }

    [Header("Configurações")]
    public List<Dica> dicas = new List<Dica>();
    public int ligacoesDisponiveis = 3;

    [Header("UI Elements")]
    public Button botaoLigarPericia;
    public GameObject painelDica;
    public TextMeshProUGUI textoDicaRevelada;
    public TextMeshProUGUI textoLigacoesRestantes;
    public TextMeshProUGUI textoBotaoPericia;

    [Header("Fade Config")]
    public float fadeDuration = 0.5f;
    public float tempoExibicaoDica = 4f;

    private CanvasGroup painelCanvasGroup;
    private Coroutine exibicaoCoroutine;

    void Start()
    {
        painelCanvasGroup = FadeUtils.GetOrAddCanvasGroup(painelDica);
        painelCanvasGroup.alpha = 0f;
        painelDica.SetActive(false);

        botaoLigarPericia.onClick.AddListener(LigarParaPericia);
        AtualizarUILigacoes();
    }

    void LigarParaPericia()
    {
        if (ligacoesDisponiveis <= 0)
        {
            StartCoroutine(MostrarMensagemTemporaria("Sem ligações disponíveis!", 1.5f));
            return;
        }

        Dica dicaParaRevelar = dicas.Find(d => !d.revelada);
        if (dicaParaRevelar == null)
        {
            StartCoroutine(MostrarMensagemTemporaria("Todas as dicas já foram reveladas!", 1.5f));
            return;
        }

        dicaParaRevelar.revelada = true;
        ligacoesDisponiveis--;
        AtualizarUILigacoes();

        if (dicaParaRevelar.iconeDica != null)
            dicaParaRevelar.iconeDica.SetActive(true);

        // Cancela exibição anterior se ainda estiver ativa
        if (exibicaoCoroutine != null)
            StopCoroutine(exibicaoCoroutine);
        exibicaoCoroutine = StartCoroutine(MostrarDicaComFade(dicaParaRevelar));
    }

    IEnumerator MostrarDicaComFade(Dica dica)
    {
        // Texto definido ANTES do fade in — painel não aparece em branco
        textoDicaRevelada.text = dica.textoDica;

        painelDica.SetActive(true);
        yield return StartCoroutine(FadeUtils.FadeCanvasGroup(this, painelCanvasGroup, 0f, 1f, fadeDuration));

        yield return new WaitForSeconds(tempoExibicaoDica);

        yield return StartCoroutine(FadeUtils.FadeCanvasGroup(this, painelCanvasGroup, 1f, 0f, fadeDuration));
        painelDica.SetActive(false);
        exibicaoCoroutine = null;
    }

    IEnumerator MostrarMensagemTemporaria(string mensagem, float duracao)
    {
        if (textoBotaoPericia == null) yield break;

        string textoOriginal = textoBotaoPericia.text;
        Color corOriginal    = textoBotaoPericia.color;

        textoBotaoPericia.text  = mensagem;
        textoBotaoPericia.color = Color.yellow;

        yield return new WaitForSeconds(duracao);

        textoBotaoPericia.text  = textoOriginal;
        textoBotaoPericia.color = corOriginal;
    }

    void AtualizarUILigacoes()
    {
        if (textoLigacoesRestantes != null)
            textoLigacoesRestantes.text = $"Calls: {ligacoesDisponiveis}";
    }

    public void AdicionarLigacao()
    {
        ligacoesDisponiveis++;
        AtualizarUILigacoes();
        StartCoroutine(MostrarMensagemTemporaria("+1 Call disponível!", 1.5f));
    }
}
