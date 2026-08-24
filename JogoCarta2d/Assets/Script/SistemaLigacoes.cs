using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sistema de ligações telefônicas — GDD:
/// "As ligações funcionam como um sistema de progresso da investigação."
/// Ex.: "Vi João saindo da padaria às 22h.", "Encontramos novas digitais na arma.",
///      "Uma câmera de segurança acabou de ser recuperada.", "Uma testemunha deseja falar com você."
///
/// Gatilhos automáticos (GDD):
///   • ~30s depois de encontrar uma pista importante.
///   • Depois de ficar muito tempo sem progresso (inatividade).
///   • Encontrar uma prova importante (chamada imediata, sem espera).
///
/// Diferente do SistemaPericia.cs, que é um recurso que o jogador GASTA para pedir
/// uma dica (mecânica válida, mas não é o "Sistema de Ligações" descrito no GDD).
/// Este script cobre a notificação automática/narrativa que empurra a história.
/// </summary>
public class SistemaLigacoes : MonoBehaviour
{
    public enum TipoGatilho
    {
        Automatica,       // dispara pelo código (ex.: ao entrar numa fase específica)
        AposPistaImportante, // ~30s após RegistrarPista de uma pista marcada como importante
        Inatividade       // dispara se o jogador ficar muito tempo sem progresso
    }

    [System.Serializable]
    public class Ligacao
    {
        public string idLigacao;
        [TextArea(2, 4)]
        public string textoLigacao;
        public TipoGatilho gatilho;

        [Tooltip("Se o gatilho for 'AposPistaImportante', qual idPista dispara esta ligação.")]
        public string idPistaGatilho;

        [HideInInspector] public bool jaRecebida;
    }

    [Header("Ligações do caso")]
    public List<Ligacao> ligacoes = new List<Ligacao>();

    [Header("Configuração de tempo")]
    [Tooltip("Delay entre encontrar a pista importante e receber a ligação (GDD: ~30s).")]
    public float delayAposPistaImportante = 30f;

    [Tooltip("Tempo sem nenhum progresso (pista nova ou avanço de história) até disparar uma ligação de inatividade.")]
    public float tempoParaInatividade = 90f;

    [Header("UI — chamada recebida")]
    public GameObject painelChamadaRecebida;
    public TextMeshProUGUI textoNomeChamador;
    public Button botaoAtender;

    [Header("UI — conteúdo da ligação")]
    public GameObject painelConteudoLigacao;
    public TextMeshProUGUI textoConteudoLigacao;
    public Button botaoFecharLigacao;

    [Header("Referências")]
    public GameManager gameManager;

    private Queue<Ligacao> filaDeLigacoes = new Queue<Ligacao>();
    private bool exibindoLigacao = false;
    private float tempoDesdeUltimoProgresso = 0f;
    private Coroutine inatividadeCoroutine;

    void Start()
    {
        if (painelChamadaRecebida != null) painelChamadaRecebida.SetActive(false);
        if (painelConteudoLigacao != null) painelConteudoLigacao.SetActive(false);

        if (botaoAtender != null) botaoAtender.onClick.AddListener(AtenderChamada);
        if (botaoFecharLigacao != null) botaoFecharLigacao.onClick.AddListener(FecharLigacao);

        inatividadeCoroutine = StartCoroutine(MonitorarInatividade());
    }

    void Update()
    {
        tempoDesdeUltimoProgresso += Time.unscaledDeltaTime;
    }

    // ── Chamado pelo GameManager/SistemaItens quando algo relevante acontece ──

    /// <summary>Registra que o jogador progrediu (zera o timer de inatividade).</summary>
    public void NotificarProgresso()
    {
        tempoDesdeUltimoProgresso = 0f;
    }

    /// <summary>
    /// Chame ao coletar uma pista. Se houver uma ligação associada a essa pista,
    /// ela é agendada para chegar ~30s depois (GDD).
    /// </summary>
    public void NotificarPistaEncontrada(string idPista, bool importante)
    {
        NotificarProgresso();

        Ligacao ligacao = ligacoes.Find(l =>
            !l.jaRecebida &&
            l.gatilho == TipoGatilho.AposPistaImportante &&
            l.idPistaGatilho == idPista);

        if (ligacao == null) return;

        if (importante)
            StartCoroutine(AgendarLigacao(ligacao, delayAposPistaImportante));
        else
            StartCoroutine(AgendarLigacao(ligacao, 2f)); // prova comum: chega quase na hora
    }

    /// <summary>Dispara manualmente uma ligação específica (ex.: ao entrar em uma nova fase).</summary>
    public void DispararLigacao(string idLigacao)
    {
        Ligacao ligacao = ligacoes.Find(l => l.idLigacao == idLigacao && !l.jaRecebida);
        if (ligacao != null)
            StartCoroutine(AgendarLigacao(ligacao, 1f));
    }

    IEnumerator AgendarLigacao(Ligacao ligacao, float delay)
    {
        yield return new WaitForSeconds(delay);
        EnfileirarLigacao(ligacao);
    }

    // ── Inatividade ───────────────────────────────────────────────────────────

    IEnumerator MonitorarInatividade()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            if (tempoDesdeUltimoProgresso >= tempoParaInatividade)
            {
                Ligacao ligacao = ligacoes.Find(l => !l.jaRecebida && l.gatilho == TipoGatilho.Inatividade);
                if (ligacao != null)
                {
                    EnfileirarLigacao(ligacao);
                    tempoDesdeUltimoProgresso = 0f;
                }
            }
        }
    }

    // ── Fila / exibição ───────────────────────────────────────────────────────

    void EnfileirarLigacao(Ligacao ligacao)
    {
        if (ligacao.jaRecebida) return;
        ligacao.jaRecebida = true;

        filaDeLigacoes.Enqueue(ligacao);

        if (!exibindoLigacao)
            StartCoroutine(ProcessarProximaLigacao());
    }

    IEnumerator ProcessarProximaLigacao()
    {
        exibindoLigacao = true;

        while (filaDeLigacoes.Count > 0)
        {
            Ligacao ligacao = filaDeLigacoes.Dequeue();

            if (painelChamadaRecebida != null)
            {
                if (textoNomeChamador != null)
                    textoNomeChamador.text = "Chamada recebida...";

                painelChamadaRecebida.SetActive(true);

                // Espera o jogador atender (ou atende automaticamente após alguns segundos)
                float espera = 0f;
                aguardandoResposta = true;
                while (aguardandoResposta && espera < 8f)
                {
                    espera += Time.unscaledDeltaTime;
                    yield return null;
                }
                aguardandoResposta = false;
                painelChamadaRecebida.SetActive(false);
            }

            MostrarConteudoLigacao(ligacao);

            // Aguarda o jogador fechar antes de mostrar a próxima
            fechandoLigacao = false;
            while (!fechandoLigacao)
                yield return null;
        }

        exibindoLigacao = false;
    }

    private bool aguardandoResposta = false;
    private bool fechandoLigacao = false;

    void AtenderChamada()
    {
        aguardandoResposta = false;
    }

    void MostrarConteudoLigacao(Ligacao ligacao)
    {
        if (painelConteudoLigacao != null)
        {
            if (textoConteudoLigacao != null)
                textoConteudoLigacao.text = ligacao.textoLigacao;

            painelConteudoLigacao.SetActive(true);
        }

        Debug.Log($"[Ligação] {ligacao.textoLigacao}");
        gameManager?.RegistrarLigacaoRecebida(ligacao.idLigacao);
    }

    void FecharLigacao()
    {
        if (painelConteudoLigacao != null)
            painelConteudoLigacao.SetActive(false);

        fechandoLigacao = true;
    }

    // ── Save/load ─────────────────────────────────────────────────────────────

    public List<string> ObterIdsRecebidas()
    {
        var lista = new List<string>();
        foreach (var l in ligacoes)
            if (l.jaRecebida) lista.Add(l.idLigacao);
        return lista;
    }

    public void RestaurarRecebidas(List<string> idsRecebidas)
    {
        if (idsRecebidas == null) return;
        foreach (var l in ligacoes)
            if (idsRecebidas.Contains(l.idLigacao)) l.jaRecebida = true;
    }
}
