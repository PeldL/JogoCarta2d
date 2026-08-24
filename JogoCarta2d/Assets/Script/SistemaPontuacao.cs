using UnityEngine;

/// <summary>
/// Sistema de pontuação do caso.
///
/// GDD: "Quanto menos erros cometer e mais rapidamente solucionar o caso,
/// maior será sua pontuação e avaliação como detetive."
///
/// Este script não existia no projeto — a condição de vitória só verificava
/// se a combinação estava certa, sem medir desempenho. Coloque este componente
/// na mesma cena do QuadroDeducao e arraste-o no campo "sistemaPontuacao".
/// </summary>
public class SistemaPontuacao : MonoBehaviour
{
    [Header("Configuração de tempo (segundos)")]
    [Tooltip("Tempo até o qual o jogador ganha a avaliação máxima.")]
    public float tempoParaAvaliacaoMaxima = 300f; // 5 min

    [Tooltip("Tempo a partir do qual a avaliação cai para o nível mínimo.")]
    public float tempoParaAvaliacaoMinima = 900f; // 15 min

    [Header("Configuração de erros")]
    [Tooltip("Pontos perdidos por cada tentativa incorreta no quadro de dedução.")]
    public int penalidadePorErro = 100;

    [Header("Estado (visível para debug / save)")]
    public int quantidadeErros = 0;

    private float tempoInicioCaso;
    private bool cronometroAtivo = false;
    private bool finalizado = false;

    void Start()
    {
        IniciarCronometro();
    }

    /// <summary>Chame ao carregar/começar um caso para começar a medir o tempo.</summary>
    public void IniciarCronometro()
    {
        tempoInicioCaso = Time.realtimeSinceStartup;
        cronometroAtivo = true;
        finalizado = false;
    }

    /// <summary>Chamado pelo QuadroDeducao a cada tentativa de dedução incorreta.</summary>
    public void RegistrarErro()
    {
        if (finalizado) return;
        quantidadeErros++;
        Debug.Log($"[Pontuação] Erro registrado. Total: {quantidadeErros}");
    }

    /// <summary>Usado ao restaurar um save — evita zerar o progresso de erros/tempo já feito.</summary>
    public void RestaurarEstado(int erros, float tempoJaDecorrido)
    {
        quantidadeErros = erros;
        tempoInicioCaso = Time.realtimeSinceStartup - tempoJaDecorrido;
        cronometroAtivo = true;
        finalizado = false;
    }

    public float TempoDecorrido =>
        cronometroAtivo ? Time.realtimeSinceStartup - tempoInicioCaso : 0f;

    /// <summary>
    /// Para o cronômetro, calcula a pontuação final e retorna um texto de avaliação
    /// pronto para exibir na tela de vitória.
    /// </summary>
    public string FinalizarEObterAvaliacao()
    {
        finalizado = true;
        cronometroAtivo = false;

        float tempo = TempoDecorrido;
        int pontuacaoBase = 1000;
        int pontuacaoFinal = Mathf.Max(0, pontuacaoBase - (quantidadeErros * penalidadePorErro) - CalcularPenalidadeTempo(tempo));

        string avaliacao = ObterTituloAvaliacao(pontuacaoFinal);

        int minutos = Mathf.FloorToInt(tempo / 60f);
        int segundos = Mathf.FloorToInt(tempo % 60f);

        return $"Tempo: {minutos:00}:{segundos:00}  |  Erros: {quantidadeErros}\n" +
               $"Pontuação: {pontuacaoFinal}  —  Avaliação: {avaliacao}";
    }

    int CalcularPenalidadeTempo(float tempo)
    {
        if (tempo <= tempoParaAvaliacaoMaxima) return 0;

        float excedente = tempo - tempoParaAvaliacaoMaxima;
        float faixa = Mathf.Max(1f, tempoParaAvaliacaoMinima - tempoParaAvaliacaoMaxima);
        float proporcao = Mathf.Clamp01(excedente / faixa);

        return Mathf.RoundToInt(proporcao * 500); // até 500 pontos de penalidade por demora
    }

    string ObterTituloAvaliacao(int pontuacao)
    {
        if (pontuacao >= 900) return "Detetive Lendário";
        if (pontuacao >= 700) return "Detetive Exemplar";
        if (pontuacao >= 500) return "Detetive Competente";
        if (pontuacao >= 250) return "Detetive Iniciante";
        return "Caso Resolvido no Limite";
    }
}
