using System.Collections.Generic;

/// <summary>
/// Dados de progresso de um slot de save.
/// </summary>
[System.Serializable]
public class SaveData
{
    // ── Identificação ─────────────────────────────────────────────────────────
    public string nomeJogador = "";
    public string ultimoSalvamento = ""; // ex: "11/06/2025 14:32"

    // ── Progresso principal ───────────────────────────────────────────────────
    public int progressoHistoria = 0;
    public string cenaAtual = "";

    // ── Pistas e investigação ─────────────────────────────────────────────────
    public List<string> pistasEncontradas = new List<string>();

    // ── Inventário (IDs dos itens coletados) ──────────────────────────────────
    public List<string> itensColetados = new List<string>();

    // ── Sistema de Perícia ────────────────────────────────────────────────────
    public int ligacoesDisponiveis = 3;
    public List<string> dicasReveladas = new List<string>(); // idDica das reveladas

    // ── Quadro de Dedução ─────────────────────────────────────────────────────
    // Salva qual PistaItem foi colocada em cada slot (por tipo: "Vitima", "Suspeito", "Arma")
    public string slotVitima = "";
    public string slotSuspeito = "";
    public string slotArma = "";

    // ── Suspeitos identificados ───────────────────────────────────────────────
    public List<string> suspeitosIdentificados = new List<string>();

    // ── Tempo de jogo ─────────────────────────────────────────────────────────
    public float tempoTotalJogo = 0f; // em segundos
}
