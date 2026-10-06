using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit editor-only smoke test. Uses native scene controls and isolated JSON slots.
[InitializeOnLoad]
public static class CaseZeroCampaignChecks
{
    const string Pending = "CaseZero.CampaignChecks";
    static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
    static readonly List<string> passed = new List<string>();
    static double deadline;
    static string report;
    static CaseZeroCampaignChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Caso Zero/Validar campanha (saves temporarios)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Salve suas cenas antes de executar a validação.");
        SessionState.SetBool(Pending, true);
        EditorSceneManager.OpenScene("Assets/Scenes/UI.unity");
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Caso Zero/Previsualizar Caso 2 (save temporario)")]
    public static void PreviewSecond() => Preview(1);
    [MenuItem("Tools/Caso Zero/Previsualizar Caso 3 (save temporario)")]
    public static void PreviewThird() => Preview(2);
    static void Preview(int index)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetInt("CaseZero.PreviewCase", index);
        Run();
    }

    static void Changed(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            CaseZeroSave.ValidationDirectory = Path.Combine(Path.GetTempPath(), "CasoZeroCampaign-" + Guid.NewGuid().ToString("N"));
            report = Path.Combine(Path.GetTempPath(), "CasoZeroCampaign-validation.txt");
            passed.Clear(); routines.Clear(); deadline = EditorApplication.timeSinceStartup + 120;
            int preview = SessionState.GetInt("CaseZero.PreviewCase", 0);
            SessionState.SetInt("CaseZero.PreviewCase", 0);
            routines.Push(preview > 0 ? PreviewRoutine(preview) : Exercise());
        }
        if (change == PlayModeStateChange.EnteredEditMode) { routines.Clear(); CaseZeroSave.ValidationDirectory = null; }
    }

    static T Field<T>(CaseZeroGame game, string name) => (T)typeof(CaseZeroGame).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
    static CaseZeroState State(CaseZeroGame game) => Field<CaseZeroState>(game, "state");
    static void Check(bool ok, string text)
    {
        if (!ok) throw new Exception(text);
        passed.Add(text);
    }
    static IEnumerator Travel(CaseZeroGame game)
    {
        yield return null;
        while (Field<bool>(game, "travelling")) yield return null;
    }
    static IEnumerator PreviewRoutine(int index)
    {
        yield return null;
        var data = CaseZeroCases.All[index];
        var state = new CaseZeroState { caseId = data.id, location = data.location, completedCases = index };
        Check(CaseZeroSave.Write(state, out _, 1), "Save de prévia isolado");
        var game = UnityEngine.Object.FindFirstObjectByType<CaseZeroGame>();
        game.SelectSlot(1); game.LoadGame(); yield return Travel(game);
    }
    static IEnumerator Exercise()
    {
        yield return null;
        var game = UnityEngine.Object.FindFirstObjectByType<CaseZeroGame>();
        Check(game != null && game.enabled, "Controlador da cena UI ativo");
        var view = Field<CaseZeroView>(game, "view");
        Check(view.interviewSuspectButtons.Length == 4 && view.suspectChecks.Length == 4, "Quatro botões e seleções serializados");
        game.SelectSlot(1); game.StartNewGame(); yield return Travel(game);
        Check(State(game).CaseIndex == 0, "Novo jogo começa no Caso 1");
        game.NextCase(); Check(State(game).CaseIndex == 0, "Avanço bloqueado sem vitória");
        for (int caseIndex = 0; caseIndex < 3; caseIndex++)
        {
            var state = State(game);
            Check(state.CaseIndex == caseIndex && state.location == state.Definition.location, "Local correto do Caso " + (caseIndex + 1));
            Check(!state.CanConclude, "Dedução bloqueada antes das pistas");
            for (int i = 0; i < 4; i++) { view.evidenceMarkers[i].onClick.Invoke(); yield return null; }
            Check(state.evidence.Count == 4, "Quatro pistas coletadas por botões nativos");
            game.OpenInterview();
            if (caseIndex == 0) game.SelectInterviewChoice(2);
            else
            {
                for (int i = 0; i < 4; i++) { view.interviewSuspectButtons[i].onClick.Invoke(); game.SelectInterviewChoice(0); }
                Check(state.AllInterviewed, "Quatro depoimentos registrados");
            }
            Check(state.CanConclude && state.evidence.Count == 5, "Confronto libera dedução");
            // Save and reload a pending call, interview selection and all collected evidence.
            game.SaveGame(); game.LoadGame(); yield return Travel(game);
            state = State(game);
            Check(state.CanConclude && state.CaseIndex == caseIndex, "Save/load preserva caso e evidências");
            if (caseIndex > 0) Check(state.interviewedMask == 15, "Save/load preserva quatro entrevistas");
            game.OpenDeduction(); game.SelectSuspect(state.Definition.culprit); game.SelectWeapon(0); game.SelectPlace(0);
            game.ConcludeCase(); game.CancelRestart(); Check(state.errors == 0 && !state.solved, "Cancelar acusação não gasta tentativa");
            if (caseIndex == 1)
            {
                game.SelectSuspect(0);
                for (int attempt = 0; attempt < 3; attempt++) { game.ConcludeCase(); game.RestartConfirmed(); }
                Check(state.failed && !state.solved, "Três erros encerram o Caso 2");
                game.NextCase(); Check(State(game).CaseIndex == 1, "Derrota não libera Caso 3");
                game.ConfirmRestart(); game.RestartConfirmed(); yield return Travel(game);
                state = State(game);
                Check(state.CaseIndex == 1 && state.completedCases == 1 && state.errors == 0 && state.evidence.Count == 0, "Repetir preserva Caso 1 e limpa somente Caso 2");
                for (int i = 0; i < 4; i++) view.evidenceMarkers[i].onClick.Invoke();
                for (int i = 0; i < 4; i++) { view.interviewSuspectButtons[i].onClick.Invoke(); game.SelectInterviewChoice(0); }
                game.OpenDeduction(); game.SelectSuspect(state.Definition.culprit);
            }
            game.ConcludeCase(); game.RestartConfirmed();
            Check(state.solved && state.completedCases == caseIndex + 1, "Vitória registra progresso da campanha");
            Check(view.nextCaseButton.gameObject.activeSelf, "Resultado oferece próximo passo");
            if (caseIndex < 2)
            {
                view.nextCaseButton.onClick.Invoke(); yield return Travel(game);
                Check(State(game).evidence.Count == 0 && State(game).errors == 0, "Próximo caso começa sem pistas ou erros anteriores");
            }
        }
        game.SaveGame(); game.LoadGame(); yield return Travel(game);
        Check(State(game).solved && State(game).CaseIndex == 2, "Campanha concluída restaura resultado final");
        view.nextCaseButton.onClick.Invoke(); yield return null; yield return null;
        var menu = UnityEngine.Object.FindFirstObjectByType<CaseZeroMenu>();
        Check(menu != null && menu.creditsPanel.activeSelf, "Fim da campanha abre os créditos no menu");
        File.WriteAllText(report, string.Join("\n", passed) + "\nPASS: " + passed.Count);
        Debug.Log("[Caso Zero] Campanha validada: " + passed.Count + " verificações. Saves reais preservados.");
    }
    static void Tick()
    {
        if (routines.Count == 0) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Validação excedeu 120 segundos.");
            var current = routines.Peek();
            if (!current.MoveNext()) routines.Pop();
            else if (current.Current is IEnumerator nested) routines.Push(nested);
        }
        catch (Exception ex)
        {
            routines.Clear(); File.WriteAllText(report, string.Join("\n", passed) + "\nFAIL: " + ex);
            Debug.LogException(ex);
        }
    }
}
