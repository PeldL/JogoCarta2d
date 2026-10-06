using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All controls are authored in ApresentacaoCaso / FinalCampanha, editable in the scene.
public sealed class CaseZeroPresentation : MonoBehaviour
{
    public bool finale;
    public TMP_Text title, body, primaryLabel, stepLabel;
    public Button primary, back;
    CaseZeroGame game;
    int step;
    public int Step => step;

    void Start()
    {
        game = FindFirstObjectByType<CaseZeroGame>();
        if (game == null || game.CampaignState == null)
        {
            title.text = "PRÉVIA DA APRESENTAÇÃO";
            body.text = "Inicie uma partida pelo Menu para testar o fluxo completo. Os elementos desta tela podem ser editados na cena.";
            primary.interactable = false;
            back.gameObject.SetActive(false);
            return;
        }
        step = game.CampaignState.CaseIndex == 0 ? 0 : 3;
        Refresh();
    }

    public void Advance()
    {
        if (game == null) return;
        if (finale) { game.OpenCredits(); return; }
        if (step < 3) { step++; Refresh(); }
        else { primary.interactable = false; game.FinishIntroduction(); }
    }
    public void Back()
    {
        if (game == null) return;
        if (finale) { game.ReturnToMenu(); return; }
        if (step > 0 && game.CampaignState.CaseIndex == 0) { step--; Refresh(); }
    }
    void Refresh()
    {
        var state = game.CampaignState;
        if (finale)
        {
            title.text = "CAMPANHA CONCLUÍDA";
            stepLabel.text = "3 CASOS RESOLVIDOS";
            body.text = Summary(state);
            primaryLabel.text = "Ver créditos";
            back.gameObject.SetActive(true);
            return;
        }
        back.gameObject.SetActive(state.CaseIndex == 0 && step > 0);
        primaryLabel.text = step == 3 ? "Iniciar investigação" : "Continuar";
        stepLabel.text = step < 3 ? $"TUTORIAL • {step + 1}/3" : "APRESENTAÇÃO DO CASO";
        if (step == 0)
        {
            title.text = "OBSERVE E RECOLHA PISTAS";
            body.text = "Você é o detetive responsável pela investigação.\n\nClique nos objetos identificados no cenário para examinar e coletar pistas. Um aviso confirma cada nova evidência.\n\nAs pistas ficam no documento de investigação. Não é necessário tirar fotos.";
        }
        else if (step == 1)
        {
            title.text = "COMPARE PROVAS E DEPOIMENTOS";
            body.text = "Abra a investigação pelo botão na tela ou pela tecla Tab.\n\nEm Evidências, leia o que foi encontrado. Em Interrogatório, ouça os envolvidos e compare seus relatos com as provas.\n\nO Mapa permite visitar outros locais. O telefone avisa quando há novas informações: abra, atenda e registre a mensagem.";
        }
        else if (step == 2)
        {
            title.text = "CONCLUA COM CUIDADO";
            body.text = "Quando reunir as provas necessárias, abra Dedução e selecione a combinação que explica o caso.\n\nUma acusação errada consome uma tentativa. Após três erros, você pode repetir o caso. Cancelar a confirmação não consome tentativas.\n\nO progresso é salvo automaticamente. Use a aba Saves para salvar ou carregar outro slot; Opções reúne tela cheia, música e efeitos.";
        }
        else
        {
            title.text = $"CASO {state.CaseIndex + 1} • {state.Definition.title}";
            body.text = state.Definition.introduction + "\n\n" + (state.CaseIndex == 0
                ? "Seu objetivo: descobrir quem cometeu o crime, qual arma foi usada e onde aconteceu."
                : state.CaseIndex == 1 ? "Reúna as quatro pistas, ouça os quatro suspeitos e confronte os relatos. Descubra quem roubou, o que levou e onde ocorreu o roubo."
                : "Examine os registros, ouça os quatro envolvidos e confronte os relatos. Identifique o responsável, o crime e a empresa de fachada.");
        }
    }

    public static string Summary(CaseZeroState state)
    {
        state.RecordResult();
        var text = new StringBuilder("Você concluiu todos os casos de Detetive: Caso Zero!\n\n");
        int total = 0, errors = 0, clues = 0, known = 0; float elapsed = 0;
        for (int i = 0; i < CaseZeroCases.All.Length; i++)
        {
            var definition = CaseZeroCases.All[i];
            var result = state.results.Find(r => r != null && r.caseId == definition.id);
            if (result == null) { text.AppendLine($"Caso {i + 1} • Histórico não disponível neste save antigo.\n"); continue; }
            known++; total += result.score; errors += result.errors; clues += result.evidenceCount; elapsed += result.elapsed;
            text.AppendLine($"Caso {i + 1} • {definition.title}");
            text.AppendLine($"{result.score}/1000 pontos ({result.score / 10f:0.#}%)  •  {Clock(result.elapsed)}  •  {result.errors} erro(s)  •  {result.evidenceCount}/5 provas\n");
        }
        if (known == 3)
        {
            string award = total == 3000 ? "DISTINÇÃO MÁXIMA • DETETIVE LENDÁRIO" : total >= 2400 ? "PRÊMIO • DETETIVE EXEMPLAR" : "PRÊMIO • DETETIVE PERSISTENTE";
            text.AppendLine(award);
            text.AppendLine($"Pontuação total: {total}/3000 ({total / 30f:0.#}%)  •  Provas: {clues}/15");
            text.AppendLine($"Tempo total dos casos concluídos: {Clock(elapsed)}  •  Erros: {errors}");
            if (total == 3000) text.AppendLine("100% da pontuação nos três casos!");
        }
        else text.AppendLine("CAMPANHA CONCLUÍDA • Estatísticas totais indisponíveis: este save não registrou todos os casos anteriores.");
        text.Append("\nA avaliação considera a tentativa concluída de cada caso. Repetir um caso substitui o resultado desse caso.");
        return text.ToString();
    }
    static string Clock(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
}
