using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Casos do Jogo")]
    public List<CasoData> todosCasos;
    public int casoAtualIndex = 0;

    [Header("Sistemas")]
    [HideInInspector] public MapaInterativo mapa;
    [HideInInspector] public SistemaPericia pericia;
    [HideInInspector] public SistemaItens sistemaItens;
    [HideInInspector] public QuadroDeducao quadro;
    [HideInInspector] public IdentificacaoSuspeitos identificacao;

    [Header("UI")]
    public GameObject painelPausa;

    [Header("Progresso")]
    public List<string> pistasEncontradas = new List<string>();
    public int progressoHistoria = 0;
    public int erros = 0;
    public int maxErros = 3;

    private CasoData casoAtual;

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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        mapa = FindFirstObjectByType<MapaInterativo>();
        pericia = FindFirstObjectByType<SistemaPericia>();
        sistemaItens = FindFirstObjectByType<SistemaItens>();
        quadro = FindFirstObjectByType<QuadroDeducao>();
        identificacao = FindFirstObjectByType<IdentificacaoSuspeitos>();
        painelPausa = GameObject.FindWithTag("PainelPausa");

        CarregarCasoAtual();
    }

    void CarregarCasoAtual()
    {
        if (todosCasos != null && casoAtualIndex < todosCasos.Count)
        {
            casoAtual = todosCasos[casoAtualIndex];
            Debug.Log($"Caso carregado: {casoAtual.nomeCaso}");
        }
        else
        {
            Debug.Log("Todos os casos concluídos!");
            // Tela de vitória
        }
    }

    public CasoData GetCasoAtual() => casoAtual;

    public void RegistrarPista(string pistaId, string descricao)
    {
        if (!pistasEncontradas.Contains(pistaId))
        {
            pistasEncontradas.Add(pistaId);
            Debug.Log($"Nova pista encontrada: {descricao}");

            // Salva automaticamente ao encontrar pista
            GetComponent<AutoSaveManager>()?.Salvar();
        }
    }

    public void AvancarHistoria()
    {
        casoAtualIndex++;
        progressoHistoria++;
        CarregarCasoAtual();
        GetComponent<AutoSaveManager>()?.Salvar();
    }

    public void PenalizarErro()
    {
        erros++;
        if (erros >= maxErros)
        {
            Debug.Log("Game Over - Muitos erros!");
            // Reiniciar caso ou mostrar tela de game over
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePausa();
    }

    void TogglePausa()
    {
        if (painelPausa == null) return;
        bool pausado = !painelPausa.activeSelf;
        painelPausa.SetActive(pausado);
        Time.timeScale = pausado ? 0f : 1f;
    }

    public void ReiniciarDados()
    {
        pistasEncontradas.Clear();
        progressoHistoria = 0;
        erros = 0;
        casoAtualIndex = 0;
        CarregarCasoAtual();
    }
}