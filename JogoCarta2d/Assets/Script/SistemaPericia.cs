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

    [Header("Gatilhos Automáticos")]
    public float tempoSemProgresso = 60f; // 1 minuto
    public bool dicaAutomaticaAtiva = true;
    private float temporizadorProgresso;

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
    private float ultimoProgresso;

    void Start()
    {
        painelCanvasGroup = FadeUtils.GetOrAddCanvasGroup(painelDica);
        painelCanvasGroup.alpha = 0f;
        painelDica.SetActive(false);

        botaoLigarPericia.onClick.AddListener(LigarParaPericia);
        AtualizarUILigacoes();

        ultimoProgresso = Time.time;
        temporizadorProgresso = tempoSemProgresso;
    }

    void Update()
    {
        if (!dicaAutomaticaAtiva) return;

        // Gatilho por tempo sem progresso
        if (!AtualizandoProgresso())
        {
            temporizadorProgresso -= Time.deltaTime;
            if (temporizadorProgresso <= 0)
            {
                // Não revela se já tem dica ativa
                if (exibicaoCoroutine == null)
                {
                    Dica dicaNaoRevelada = dicas.Find(d => !d.revelada);
                    if (dicaNaoRevelada != null && ligacoesDisponiveis > 0)
                    {
                        // Usa uma ligação automaticamente
                        ligacoesDisponiveis--;
                        dicaNaoRevelada.revelada = true;
                        AtualizarUILigacoes();

                        exibicaoCoroutine = StartCoroutine(MostrarDicaComFade(dicaNaoRevelada));
                        ultimoProgresso = Time.time; // Reset após dica automática
                    }
                }
            }
        }
        else
        {
            temporizadorProgresso = tempoSemProgresso;
        }
    }

    bool AtualizandoProgresso()
    {
        // Verificar se o jogador coletou novas pistas recentemente
        return false; // Implementar com sistema de eventos
    }

    public void GatilhoEvento(string nomeEvento)
    {
        switch (nomeEvento)
        {
            case "coletou_primeira_pista":
                MostrarDicaManual("Boa! Agora vá ao quadro de deduções para organizar as pistas.");
                break;
            case "visitou_3_cenarios":
                MostrarDicaManual("Você já visitou vários locais. Compare os álibis dos suspeitos.");
                break;
            case "suspeito_mentiu":
                MostrarDicaManual("Alguém não está sendo sincero. Quebre o álibi confrontando com as evidências.");
                break;
            case "all_slots_filled":
                MostrarDicaManual("Todos os slots estão preenchidos! Confirme sua dedução.");
                break;
        }
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

        if (exibicaoCoroutine != null)
            StopCoroutine(exibicaoCoroutine);
        exibicaoCoroutine = StartCoroutine(MostrarDicaComFade(dicaParaRevelar));
    }

    // Dica manual sem gastar ligação
    public void MostrarDicaManual(string texto)
    {
        if (exibicaoCoroutine != null)
            StopCoroutine(exibicaoCoroutine);

        exibicaoCoroutine = StartCoroutine(MostrarDicaTexto(texto));
    }

    IEnumerator MostrarDicaTexto(string texto)
    {
        textoDicaRevelada.text = texto;
        painelDica.SetActive(true);
        yield return StartCoroutine(FadeUtils.FadeCanvasGroup(this, painelCanvasGroup, 0f, 1f, fadeDuration));
        yield return new WaitForSeconds(tempoExibicaoDica);
        yield return StartCoroutine(FadeUtils.FadeCanvasGroup(this, painelCanvasGroup, 1f, 0f, fadeDuration));
        painelDica.SetActive(false);
        exibicaoCoroutine = null;
    }

    IEnumerator MostrarDicaComFade(Dica dica)
    {
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
        Color corOriginal = textoBotaoPericia.color;

        textoBotaoPericia.text = mensagem;
        textoBotaoPericia.color = Color.yellow;

        yield return new WaitForSeconds(duracao);

        textoBotaoPericia.text = textoOriginal;
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