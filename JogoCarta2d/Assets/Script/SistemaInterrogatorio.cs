using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Interrogatório com escolha de perguntas — GDD:
/// "Interrogar suspeitos.", "Conversar com testemunhas.", "Escolha de perguntas
/// durante os interrogatórios." Não existia no projeto: IdentificacaoSuspeitos.cs
/// só cobre a etapa final de "apontar o culpado", não a conversa em si.
///
/// Estrutura simples de diálogo por nós: cada Personagem tem uma lista de
/// Perguntas; cada Pergunta tem uma resposta e, opcionalmente, libera uma pista
/// (integrando com GameManager.RegistrarPista) ou desbloqueia novas perguntas.
/// </summary>
public class SistemaInterrogatorio : MonoBehaviour
{
    [System.Serializable]
    public class Pergunta
    {
        public string idPergunta;
        public string textoPergunta;
        [TextArea(2, 5)]
        public string textoResposta;

        [Tooltip("Se preenchido, registra esta pista no GameManager ao ouvir a resposta.")]
        public string idPistaLiberada;
        public string descricaoPistaLiberada;
        public bool pistaImportante;

        [Tooltip("IDs de perguntas que só ficam disponíveis depois desta ser feita.")]
        public List<string> desbloqueiaPerguntas = new List<string>();

        [HideInInspector] public bool jaFeita;
        [HideInInspector] public bool desbloqueada = true;
    }

    [System.Serializable]
    public class Personagem
    {
        public string idPersonagem;
        public string nomeExibido;
        public List<Pergunta> perguntas = new List<Pergunta>();
    }

    [Header("Personagens interrogáveis nesta cena")]
    public List<Personagem> personagens = new List<Personagem>();

    [Header("Referências")]
    public GameManager gameManager;

    [Header("UI")]
    public GameObject painelInterrogatorio;
    public TextMeshProUGUI textoNomePersonagem;
    public TextMeshProUGUI textoRespostaAtual;
    public Transform containerBotoesPerguntas;
    public GameObject prefabBotaoPergunta; // precisa de Button + TextMeshProUGUI filho
    public Button botaoEncerrar;

    private Personagem personagemAtual;
    private List<GameObject> botoesInstanciados = new List<GameObject>();

    void Start()
    {
        if (painelInterrogatorio != null) painelInterrogatorio.SetActive(false);
        if (botaoEncerrar != null) botaoEncerrar.onClick.AddListener(EncerrarInterrogatorio);
    }

    /// <summary>Chame ao clicar num personagem interativo no mapa.</summary>
    public void IniciarInterrogatorio(string idPersonagem)
    {
        personagemAtual = personagens.Find(p => p.idPersonagem == idPersonagem);
        if (personagemAtual == null)
        {
            Debug.LogWarning($"Personagem '{idPersonagem}' não encontrado no SistemaInterrogatorio.");
            return;
        }

        if (textoNomePersonagem != null) textoNomePersonagem.text = personagemAtual.nomeExibido;
        if (textoRespostaAtual != null) textoRespostaAtual.text = "Sobre o que você quer perguntar?";

        painelInterrogatorio?.SetActive(true);
        AtualizarBotoesDePergunta();

        gameManager?.ligacoes?.NotificarProgresso();
    }

    void AtualizarBotoesDePergunta()
    {
        foreach (var b in botoesInstanciados) Destroy(b);
        botoesInstanciados.Clear();

        if (containerBotoesPerguntas == null || prefabBotaoPergunta == null || personagemAtual == null)
            return;

        foreach (var pergunta in personagemAtual.perguntas)
        {
            if (!pergunta.desbloqueada) continue;

            GameObject botaoObj = Instantiate(prefabBotaoPergunta, containerBotoesPerguntas);
            botoesInstanciados.Add(botaoObj);

            var texto = botaoObj.GetComponentInChildren<TextMeshProUGUI>();
            if (texto != null) texto.text = pergunta.textoPergunta;

            var botao = botaoObj.GetComponent<Button>();
            if (botao != null)
            {
                botao.interactable = !pergunta.jaFeita; // permite reler perguntas já feitas, mas desabilitado por padrão
                Pergunta capturada = pergunta;
                botao.onClick.AddListener(() => FazerPergunta(capturada));
            }
        }
    }

    void FazerPergunta(Pergunta pergunta)
    {
        if (textoRespostaAtual != null)
            textoRespostaAtual.text = pergunta.textoResposta;

        if (!pergunta.jaFeita)
        {
            pergunta.jaFeita = true;

            if (!string.IsNullOrEmpty(pergunta.idPistaLiberada))
            {
                gameManager?.RegistrarPista(pergunta.idPistaLiberada, pergunta.descricaoPistaLiberada, pergunta.pistaImportante);
            }

            foreach (var idDesbloqueio in pergunta.desbloqueiaPerguntas)
            {
                var alvo = personagemAtual.perguntas.Find(p => p.idPergunta == idDesbloqueio);
                if (alvo != null) alvo.desbloqueada = true;
            }
        }

        AtualizarBotoesDePergunta();
    }

    void EncerrarInterrogatorio()
    {
        painelInterrogatorio?.SetActive(false);
        personagemAtual = null;
    }

    // ── Save/load ─────────────────────────────────────────────────────────────

    public List<string> ObterIdsPerguntasFeitas()
    {
        var lista = new List<string>();
        foreach (var personagem in personagens)
            foreach (var pergunta in personagem.perguntas)
                if (pergunta.jaFeita) lista.Add($"{personagem.idPersonagem}:{pergunta.idPergunta}");
        return lista;
    }

    public void RestaurarPerguntasFeitas(List<string> idsCompostos)
    {
        if (idsCompostos == null) return;

        foreach (var idComposto in idsCompostos)
        {
            var partes = idComposto.Split(':');
            if (partes.Length != 2) continue;

            var personagem = personagens.Find(p => p.idPersonagem == partes[0]);
            var pergunta = personagem?.perguntas.Find(p => p.idPergunta == partes[1]);
            if (pergunta != null)
            {
                pergunta.jaFeita = true;
                foreach (var idDesbloqueio in pergunta.desbloqueiaPerguntas)
                {
                    var alvo = personagem.perguntas.Find(p => p.idPergunta == idDesbloqueio);
                    if (alvo != null) alvo.desbloqueada = true;
                }
            }
        }
    }
}
