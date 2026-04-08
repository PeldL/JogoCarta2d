using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Sistemas")]
    public MapaInterativo mapa;
    public SistemaPericia pericia;
    public SistemaItens inventario;
    public QuadroDeducao quadro;
    public IdentificacaoSuspeitos identificacao;

    [Header("Progresso")]
    public List<string> pistasEncontradas = new List<string>();
    public int progressoHistoria = 0;

    [Header("UI")]
    public GameObject painelPausa;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Inicializar jogo
        CarregarProgresso();
    }

    void Update()
    {
        // Tecla ESC para pausar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PausarJogo();
        }
    }

    public void RegistrarPista(string pistaId, string descricao)
    {
        if (!pistasEncontradas.Contains(pistaId))
        {
            pistasEncontradas.Add(pistaId);
            Debug.Log($"Nova pista encontrada: {descricao}");

            // Verificar se alguma identificação pode ser feita agora
            // (Implementar lógica)
        }
    }

    public void AvancarHistoria()
    {
        progressoHistoria++;

        switch (progressoHistoria)
        {
            case 1:
                Debug.Log("Fase 1: Investigar o bar");
                break;
            case 2:
                Debug.Log("Fase 2: Interrogar testemunhas");
                break;
            case 3:
                Debug.Log("Fase 3: Montar o quadro de dedução");
                break;
        }
    }

    void PausarJogo()
    {
        if (painelPausa != null)
        {
            bool pausado = !painelPausa.activeSelf;
            painelPausa.SetActive(pausado);
            Time.timeScale = pausado ? 0f : 1f;
        }
    }

    void CarregarProgresso()
    {
        // Carregar save game (implementar PlayerPrefs ou sistema de save)
        Debug.Log("Progresso carregado");
    }

    public void SalvarProgresso()
    {
        // Salvar progresso
        Debug.Log("Progresso salvo");
    }
}