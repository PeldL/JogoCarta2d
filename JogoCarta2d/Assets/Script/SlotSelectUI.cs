using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Tela de seleção de slots de save (Menu Principal).
/// 
/// Setup na cena de menu:
///   - 3 painéis de slot, cada um com:
///       • TextMeshProUGUI textoNome    — nome do jogador ou "Slot Vazio"
///       • TextMeshProUGUI textoData    — data do último save
///       • TextMeshProUGUI textoFase    — "Fase X"
///       • Button botaoJogar            — continua / novo jogo
///       • Button botaoDeletar          — deleta o save (opcional)
///   - Um painel "painelNomeNovo" com:
///       • TMP_InputField campNome      — digitar o nome
///       • Button botaoConfirmarNome    — confirmar
/// </summary>
public class SlotSelectUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public TextMeshProUGUI textoNome;
        public TextMeshProUGUI textoData;
        public TextMeshProUGUI textoFase;
        public Button botaoJogar;
        public Button botaoDeletar;
    }

    [Header("Slots (3 slots)")]
    public SlotUI[] slots = new SlotUI[3];

    [Header("Painel de novo jogo — digitar nome")]
    public GameObject painelNomeNovo;
    public TMP_InputField campoNome;
    public Button botaoConfirmarNome;
    public TextMeshProUGUI textoErroNome;

    [Header("Cena do jogo")]
    public string nomeCenaJogo = "Cena_Bar"; // nome da primeira cena do jogo

    // Slot que o jogador está tentando iniciar (novo jogo)
    int slotPendente = -1;

    void Start()
    {
        if (painelNomeNovo != null)
            painelNomeNovo.SetActive(false);

        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i; // captura local para o lambda
            AtualizarSlotUI(idx);

            if (slots[i].botaoJogar != null)
                slots[i].botaoJogar.onClick.AddListener(() => OnClicarSlot(idx));

            if (slots[i].botaoDeletar != null)
                slots[i].botaoDeletar.onClick.AddListener(() => DeletarSlot(idx));
        }

        if (botaoConfirmarNome != null)
            botaoConfirmarNome.onClick.AddListener(ConfirmarNovoJogo);
    }

    // ── Atualiza a UI de cada slot ────────────────────────────────────────────

    void AtualizarSlotUI(int idx)
    {
        var ui = slots[idx];
        if (ui == null) return;

        if (GameSaveSystem.SlotExists(idx))
        {
            SaveData data = GameSaveSystem.PeekSlot(idx);

            if (ui.textoNome  != null) ui.textoNome.text  = data.nomeJogador;
            if (ui.textoData  != null) ui.textoData.text  = data.ultimoSalvamento;
            if (ui.textoFase  != null) ui.textoFase.text  = $"Fase {data.progressoHistoria}";
            if (ui.botaoDeletar != null) ui.botaoDeletar.gameObject.SetActive(true);
        }
        else
        {
            if (ui.textoNome  != null) ui.textoNome.text  = "Slot Vazio";
            if (ui.textoData  != null) ui.textoData.text  = "";
            if (ui.textoFase  != null) ui.textoFase.text  = "";
            if (ui.botaoDeletar != null) ui.botaoDeletar.gameObject.SetActive(false);
        }
    }

    // ── Clique no slot ────────────────────────────────────────────────────────

    void OnClicarSlot(int idx)
    {
        if (GameSaveSystem.SlotExists(idx))
        {
            // Continua jogo existente
            CarregarJogo(idx);
        }
        else
        {
            // Novo jogo — pede o nome
            slotPendente = idx;
            AbrirPainelNome();
        }
    }

    // ── Novo jogo — digitar nome ──────────────────────────────────────────────

    void AbrirPainelNome()
    {
        if (painelNomeNovo == null) return;
        painelNomeNovo.SetActive(true);
        if (campoNome != null) campoNome.text = "";
        if (textoErroNome != null) textoErroNome.text = "";
    }

    void ConfirmarNovoJogo()
    {
        string nome = campoNome != null ? campoNome.text.Trim() : "";

        if (string.IsNullOrEmpty(nome))
        {
            if (textoErroNome != null)
                textoErroNome.text = "Digite um nome para continuar!";
            return;
        }

        // Cria save inicial com o nome informado
        var novoSave = new SaveData
        {
            nomeJogador        = nome,
            progressoHistoria  = 0,
            cenaAtual          = nomeCenaJogo,
        };

        GameSaveSystem.SaveGame(novoSave, slotPendente);

        if (painelNomeNovo != null)
            painelNomeNovo.SetActive(false);

        CarregarJogo(slotPendente);
    }

    // ── Carregar jogo ─────────────────────────────────────────────────────────

    void CarregarJogo(int idx)
    {
        SaveData data = GameSaveSystem.LoadGame(idx);
        if (data == null) return;

        // Passa o slot e os dados para o AutoSaveManager via GameManager
        var gm = FindFirstObjectByType<AutoSaveManager>();
        if (gm != null)
        {
            gm.slotAtual = idx;
            gm.CarregarDados(data);
        }

        // Vai para a cena correta (ou começo se for novo jogo)
        string cena = string.IsNullOrEmpty(data.cenaAtual) ? nomeCenaJogo : data.cenaAtual;
        SceneManager.LoadScene(cena);
    }

    // ── Deletar slot ──────────────────────────────────────────────────────────

    void DeletarSlot(int idx)
    {
        GameSaveSystem.DeleteSlot(idx);
        AtualizarSlotUI(idx);
    }
}
