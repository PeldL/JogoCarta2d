using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// GameManager atualizado — integrado ao sistema de save por arquivos JSON.
/// O save via PlayerPrefs foi removido; toda persistência passa por GameSaveSystem.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Sistemas — reatribuídos automaticamente a cada cena")]
    [HideInInspector] public MapaInterativo mapa;
    [HideInInspector] public SistemaPericia pericia;
    [HideInInspector] public SistemaItens sistemaItens;
    [HideInInspector] public QuadroDeducao quadro;
    [HideInInspector] public IdentificacaoSuspeitos identificacao;

    [Header("UI — painel de pausa (atribuir no Inspector da cena)")]
    public GameObject painelPausa;

    [Header("Progresso")]
    public List<string> pistasEncontradas = new List<string>();
    public int progressoHistoria = 0;

    // ── Singleton + DontDestroyOnLoad ─────────────────────────────────────────

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        mapa          = FindFirstObjectByType<MapaInterativo>();
        pericia       = FindFirstObjectByType<SistemaPericia>();
        sistemaItens  = FindFirstObjectByType<SistemaItens>();
        quadro        = FindFirstObjectByType<QuadroDeducao>();
        identificacao = FindFirstObjectByType<IdentificacaoSuspeitos>();
        painelPausa   = GameObject.FindWithTag("PainelPausa");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePausa();
    }

    // ── Registrar pista ───────────────────────────────────────────────────────

    public void RegistrarPista(string pistaId, string descricao)
    {
        if (!pistasEncontradas.Contains(pistaId))
        {
            pistasEncontradas.Add(pistaId);
            Debug.Log($"Nova pista encontrada: {descricao}");

            // Salva automaticamente ao encontrar pista importante
            GetComponent<AutoSaveManager>()?.Salvar();
        }
    }

    // ── Avançar história ──────────────────────────────────────────────────────

    public void AvancarHistoria()
    {
        progressoHistoria++;
        switch (progressoHistoria)
        {
            case 1: Debug.Log("Fase 1: Investigar o bar"); break;
            case 2: Debug.Log("Fase 2: Interrogar testemunhas"); break;
            case 3: Debug.Log("Fase 3: Montar o quadro de dedução"); break;
        }

        // Salva automaticamente ao avançar na história
        GetComponent<AutoSaveManager>()?.Salvar();
    }

    // ── Pausa ─────────────────────────────────────────────────────────────────

    void TogglePausa()
    {
        if (painelPausa == null) return;
        bool pausado = !painelPausa.activeSelf;
        painelPausa.SetActive(pausado);
        Time.timeScale = pausado ? 0f : 1f;
    }

    // ── Reiniciar save ────────────────────────────────────────────────────────

    /// <summary>Limpa os dados em memória (o slot no disco deve ser deletado via SlotSelectUI).</summary>
    public void ReiniciarDados()
    {
        pistasEncontradas.Clear();
        progressoHistoria = 0;
    }
}
