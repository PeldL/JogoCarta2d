using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerencia o auto-save a cada 1 minuto e nos eventos importantes do jogo.
/// Coloque este componente no mesmo GameObject do GameManager (que já tem DontDestroyOnLoad).
/// </summary>
public class AutoSaveManager : MonoBehaviour
{
    [Header("Slot atual (definido ao escolher slot na tela de seleção)")]
    public int slotAtual = 0;

    [Header("Ícone de save (arraste o SaveIcon no Inspector)")]
    public SaveIcon saveIcon;

    const float INTERVALO_SAVE = 60f; // 1 minuto

    // Referência ao GameManager para coletar os dados
    GameManager gm;

    // Acumula o tempo de jogo
    float tempoJogo = 0f;

    void Start()
    {
        gm = GetComponent<GameManager>();
        StartCoroutine(AutoSaveLoop());
    }

    void Update()
    {
        // Só conta tempo quando o jogo não está pausado
        if (Time.timeScale > 0f)
            tempoJogo += Time.deltaTime;
    }

    // ── Loop de auto-save ─────────────────────────────────────────────────────

    IEnumerator AutoSaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(INTERVALO_SAVE);
            Salvar();
        }
    }

    // ── Salvar ────────────────────────────────────────────────────────────────

    public void Salvar()
    {
        if (gm == null) { Debug.LogWarning("[AutoSave] GameManager não encontrado!"); return; }

        SaveData data = ColetarDadosDoJogo();
        GameSaveSystem.SaveGame(data, slotAtual);
        saveIcon?.MostrarIcone();

        Debug.Log($"[AutoSave] Salvo no slot {slotAtual}.");
    }

    SaveData ColetarDadosDoJogo()
    {
        var data = new SaveData();

        // ── Progresso principal ───────────────────────────────────────────────
        data.progressoHistoria  = gm.progressoHistoria;
        data.pistasEncontradas  = new System.Collections.Generic.List<string>(gm.pistasEncontradas);
        data.cenaAtual          = SceneManager.GetActiveScene().name;
        data.tempoTotalJogo     = tempoJogo;

        // ── Inventário ────────────────────────────────────────────────────────
        if (gm.sistemaItens != null)
        {
            data.itensColetados = new System.Collections.Generic.List<string>();
            foreach (var item in gm.sistemaItens.itens)
                if (item.coletado)
                    data.itensColetados.Add(item.idItem);
        }

        // ── Sistema de Perícia ────────────────────────────────────────────────
        if (gm.pericia != null)
        {
            data.ligacoesDisponiveis = gm.pericia.ligacoesDisponiveis;
            data.dicasReveladas      = new System.Collections.Generic.List<string>();
            foreach (var dica in gm.pericia.dicas)
                if (dica.revelada)
                    data.dicasReveladas.Add(dica.idDica);
        }

        // ── Quadro de Dedução ─────────────────────────────────────────────────
        if (gm.quadro != null)
        {
            foreach (var slot in gm.quadro.slots)
            {
                if (!slot.preenchido || slot.itemColocado == null) continue;
                switch (slot.tipo)
                {
                    case TipoPista.Vitima:   data.slotVitima   = slot.itemColocado.nome; break;
                    case TipoPista.Suspeito: data.slotSuspeito = slot.itemColocado.nome; break;
                    case TipoPista.Arma:     data.slotArma     = slot.itemColocado.nome; break;
                }
            }
        }

        // ── Suspeitos identificados ───────────────────────────────────────────
        if (gm.identificacao != null)
        {
            data.suspeitosIdentificados = new System.Collections.Generic.List<string>();
            foreach (var suspeito in gm.identificacao.suspeitos)
                if (suspeito.identificado)
                    data.suspeitosIdentificados.Add(suspeito.nome);
        }

        return data;
    }

    // ── Carregar ──────────────────────────────────────────────────────────────

    public void CarregarDados(SaveData data)
    {
        if (gm == null || data == null) return;

        // Progresso principal
        gm.progressoHistoria = data.progressoHistoria;
        gm.pistasEncontradas = data.pistasEncontradas
            ?? new System.Collections.Generic.List<string>();

        tempoJogo = data.tempoTotalJogo;

        // Inventário — marca itens como coletados; a UI é reconstruída pela cena
        if (gm.sistemaItens != null && data.itensColetados != null)
            foreach (var item in gm.sistemaItens.itens)
                if (data.itensColetados.Contains(item.idItem))
                    item.coletado = true;

        // Perícia
        if (gm.pericia != null)
        {
            gm.pericia.ligacoesDisponiveis = data.ligacoesDisponiveis;
            if (data.dicasReveladas != null)
                foreach (var dica in gm.pericia.dicas)
                    if (data.dicasReveladas.Contains(dica.idDica))
                        dica.revelada = true;
        }

        // Suspeitos identificados
        if (gm.identificacao != null && data.suspeitosIdentificados != null)
            foreach (var suspeito in gm.identificacao.suspeitos)
                if (data.suspeitosIdentificados.Contains(suspeito.nome))
                    suspeito.identificado = true;

        // Quadro de dedução — restaura itens colocados nos slots
        RestaurarQuadro(data);

        Debug.Log($"[AutoSave] Dados carregados — fase {data.progressoHistoria}, " +
                  $"{data.pistasEncontradas.Count} pistas.");
    }

    void RestaurarQuadro(SaveData data)
    {
        if (gm.quadro == null) return;

        foreach (var slot in gm.quadro.slots)
        {
            string nomeParaRestaurar = slot.tipo switch
            {
                TipoPista.Vitima   => data.slotVitima,
                TipoPista.Suspeito => data.slotSuspeito,
                TipoPista.Arma     => data.slotArma,
                _                  => ""
            };

            if (string.IsNullOrEmpty(nomeParaRestaurar)) continue;

            var pistaItem = gm.quadro.todasPistas.Find(p => p.nome == nomeParaRestaurar);
            if (pistaItem == null) continue;

            slot.itemColocado = pistaItem;
            slot.preenchido   = true;

            if (slot.slotImage != null)
            {
                slot.slotImage.sprite = pistaItem.icone;
                slot.slotImage.color  = UnityEngine.Color.white;
            }
        }
    }

    // ── Salva automaticamente ao sair/pausar o app ────────────────────────────

    void OnApplicationQuit()  => Salvar();

    void OnApplicationPause(bool pausado)
    {
        if (pausado) Salvar();
    }
}
