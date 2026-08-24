// Enum compartilhado entre QuadroDeducao, SistemaItens e IdentificacaoSuspeitos.
// Mantido em arquivo separado para evitar dependências circulares.
//
// Local adicionado: o GDD define a condição de vitória como
// Culpado + Arma + Local (nos moldes do jogo de tabuleiro Detetive/Clue).
public enum TipoPista
{
    Vitima,
    Suspeito,
    Arma,
    Local
}
