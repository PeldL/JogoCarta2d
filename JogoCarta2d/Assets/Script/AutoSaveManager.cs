using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSaveManager : MonoBehaviour
{
    [Header("Slot atual (definido ao escolher slot na tela de seleção)")]
    public int slotAtual = 0;

    [Header("Ícone de save (arraste o SaveIcon no Inspector)")]
    public SaveIcon saveIcon;

    const float INTERVALO_SAVE = 60f;

    GameManager gm;
    float tempoJogo = 0f;

    void Start()
    {
        gm = GetComponent<GameManager>();
        StartCoroutine(AutoSaveLoop());
    }

    void Update()
    {
        if (Time.timeScale > 0f)
            tempoJogo += Time.deltaTime;
    }

    IEnumerator AutoSaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(INTERVALO_SAVE);
            Salvar();
        }
    }

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

        data.progressoHistoria = gm.progressoHistoria;
        data.pistasEncontradas = new List<string>(gm.pistasEncontradas);
        data.cenaAtual = SceneManager.GetActiveScene().name;
        data.tempoTotalJogo = tempoJogo;

        // Inventário
        if (gm.sistemaItens != null)
        {
            data.itensColetados = new List<string>();
            foreach (var item in gm.sistemaItens.itens)
                if (item.coletado)
                    data.itensColetados.Add(item.idItem);
        }

        // Perícia
        if (gm.pericia != null)
        {
            data.ligacoesDisponiveis = gm.pericia.ligacoesDisponiveis;
            data.dicasReveladas = new List<string>();
            foreach (var dica in gm.pericia.dicas)
                if (dica.revelada)
                    data.dicasReveladas.Add(dica.idDica);
        }

        // Quadro de Dedução — incluindo LOCAL
        if (gm.quadro != null)
        {
            foreach (var slot in gm.quadro.slots)
            {
                if (!slot.preenchido || slot.itemColocado == null) continue;
                switch (slot.tipo)
                {
                    case TipoPista.Vitima: data.slotVitima = slot.itemColocado.nome; break;
                    case TipoPista.Suspeito: data.slotSuspeito = slot.itemColocado.nome; break;
                    case TipoPista.Arma: data.slotArma = slot.itemColocado.nome; break;
                    case TipoPista.Local: data.slotLocal = slot.itemColocado.nome; break; // NOVO
                }
            }
        }

        // Suspeitos identificados
        if (gm.identificacao != null)
        {
            data.suspeitosIdentificados = new List<string>();
            foreach (var suspeito in gm.identificacao.suspeitos)
                if (suspeito.identificado)
                    data.suspeitosIdentificados.Add(suspeito.nome);
        }

        return data;
    }

    public void CarregarDados(SaveData data)
    {
        if (gm == null || data == null) return;

        gm.progressoHistoria = data.progressoHistoria;
        gm.pistasEncontradas = data.pistasEncontradas ?? new List<string>();
        tempoJogo = data.tempoTotalJogo;

        // Inventário
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

        // Quadro de dedução — restaura todos os slots incluindo LOCAL
        RestaurarQuadro(data);

        Debug.Log($"[AutoSave] Dados carregados — fase {data.progressoHistoria}, " +
                  $"{data.pistasEncontradas.Count} pistas.");
    }

    void RestaurarQuadro(SaveData data)
    {
        if (gm.quadro == null) return;

        gm.quadro.RestaurarSlot(TipoPista.Vitima, data.slotVitima);
        gm.quadro.RestaurarSlot(TipoPista.Suspeito, data.slotSuspeito);
        gm.quadro.RestaurarSlot(TipoPista.Arma, data.slotArma);
        gm.quadro.RestaurarSlot(TipoPista.Local, data.slotLocal); // NOVO
    }

    void OnApplicationQuit() => Salvar();
    void OnApplicationPause(bool pausado) { if (pausado) Salvar(); }
}