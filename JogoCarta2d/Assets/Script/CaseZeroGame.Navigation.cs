using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using Pages = CaseZeroPages;

// Case-file pages, travel between locations, the briefing/finale scenes and the results page.
public sealed partial class CaseZeroGame
{
    bool travelling;
    string loadedLocation;
    bool presenting;

    // These methods are persistent Button.OnClick targets, visible in the Inspector.
    public void OpenMap() { if (view.backgroundMap && !view.mapInDocument) TravelTo(Pages.Map); else OpenPage(Pages.Map); }
    public void OpenEvidence() => OpenPage(Pages.Evidence);
    public void OpenInterview() => OpenPage(Pages.Interviews);
    public void OpenPhone() => OpenPage(Pages.Phone);
    public void OpenDeduction() => OpenPage(Pages.Deduction);
    public void OpenOptions() => OpenPage(Pages.Options);
    public void OpenSaves() => OpenPage(Pages.Saves);
    public void OpenCredits() { CaseZeroSession.ShowCredits = true; ReturnToMenu(); }
    public void OpenSuspects() => OpenPage(Pages.Suspects);
    public void UnavailableLocation() => Say("This location is not available in the current case.");
    public void VisitBar() { if (view.backgroundMap) TravelTo("Bar"); else CloseDocument(); }

    public void ToggleDocument()
    {
        if (state == null) return;
        if (view.IsDocumentOpen) CloseDocument();
        else OpenPage(view.separatePanels ? Pages.Evidence : NormalizePage(state.page));
    }

    public void CloseDocument()
    {
        if (confirming) { CancelRestart(); return; }
        if (state == null) return;
        if (state.Finished) { OpenPage(Pages.Result); return; }
        if (state.page == Pages.Confirmation) state.page = returnPage;
        if (state.page == Pages.Options) state.page = Pages.Map;
        paused = false;
        state.documentOpen = false;
        view.CloseDocument();
    }

    public void ReturnToMenu()
    {
        if (travelling) return;
        if (!Application.CanStreamedLevelBeLoaded("Menu")) { Say("Add the Menu scene to the build profile."); return; }
        Save(false);
        travelling = true;
        CaseZeroSceneLoading.Load("Menu", null, null, view.documentTitle.font);
    }

    string NormalizePage(string page)
    {
        if (page == Pages.LegacyPhone) return Pages.Phone;
        if (page == Pages.LegacyScene || page == Pages.Introduction || page == Pages.Confirmation) return Pages.Map;
        foreach (var entry in view.pages) if (entry.name == page) return page;
        return Pages.Map;
    }

    void OpenPage(string page)
    {
        if (state == null) { ShowWelcome(); return; }
        if (state.Finished && page != Pages.Confirmation) page = Pages.Result;
        if (page == Pages.Confirmation) { ConfirmRestart(); return; }
        paused = page == Pages.Options || page == Pages.Saves;
        state.page = page;
        state.documentOpen = true;
        view.ShowPage(page, true);
        RefreshProgress();
        switch (page)
        {
            case Pages.Evidence:
                RefreshBoard();
                TutorialHint(4, "Read the recorded clues. Then, in Interviews, pick a person and take them to the interrogation room.");
                break;
            case Pages.Interviews:
                ShowInterview();
                TutorialHint(8, "Choose a question. Some questions become available only after you find supporting evidence.");
                break;
            case Pages.Phone:
                ShowPhone();
                TutorialHint(32, "Send material to forensics here. When the phone rings, open it to answer, read the report, and end the call to record it.");
                break;
            case Pages.Deduction:
                RefreshDeduction();
                TutorialHint(16, "Choose the combination supported by the evidence. Confirming an incorrect accusation costs an attempt; canceling does not. You have three attempts.");
                break;
            case Pages.Saves: Save(false); RefreshSlots(); break;
            case Pages.Result: ShowResult(); break;
        }
    }

    void ShowResult()
    {
        view.ClearNotice();
        bool lastCase = state.CaseIndex >= CaseZeroCases.All.Length - 1;
        if (view.solutionText != null) view.solutionText.text = CurrentCase.ending;
        if (view.nextCaseButton != null) view.nextCaseButton.gameObject.SetActive(state.solved);
        if (view.nextCaseLabel != null) view.nextCaseLabel.text = lastCase ? "View campaign results" : "Go to Case " + (state.CaseIndex + 2);
        if (view.resultSolution != null) view.resultSolution.SetActive(state.solved);
        view.documentTitle.text = state.failed ? "CASE UNSOLVED" : "CASE SOLVED";
        view.AnimateResult(state.solved);
        view.resultStats.text = state.failed
            ? $"All three accusations were incorrect.\nThe investigation is over.\n{TimeText(state.elapsed)} • {state.errors} mistakes\nYou can restart this case or return to the menu."
            : $"{state.Rating}\n{state.Score} points  •  {TimeText(state.elapsed)}  •  {state.errors} mistake(s)";
        view.closeDocumentButton.interactable = false;
    }

    public void NextCase()
    {
        if (state == null || !state.solved || travelling || confirming) return;
        var next = state.NextCase();
        if (next == null) { state.RecordResult(); Save(false); StartCoroutine(ShowPresentation("FinalCampanha")); return; }
        if (!Application.CanStreamedLevelBeLoaded(next.Definition.location)) { Say("Add the next location to the build profile."); return; }
        if (!CaseZeroSave.Write(next, out string error, activeSlot)) { Say(error); return; }
        Begin(next);
    }

    public static string NormalizeLocation(string location)
    {
        switch (location)
        {
            case "Bar": case "Hospital": case "Delegacia": case "Padaria": case "Laboratorio": case "Rua": case "Estacionamento": case "Apartamento": case "Escritorio": return location;
            case "Joalheria": return "Laboratorio";
            case "Mercadinho": return "Padaria";
            default: return Pages.Map;
        }
    }

    public void TravelTo(string location)
    {
        if (state == null || travelling || confirming || !view.backgroundMap) return;
        if (state.Finished) return;
        StartCoroutine(ChangeLocation(NormalizeLocation(location), false));
    }

    IEnumerator ChangeLocation(string destination, bool restore)
    {
        if (destination != Pages.Map && !Application.CanStreamedLevelBeLoaded(destination))
        {
            Say("Location not included in the build profile: " + destination);
            view.SetLocation(loadedLocation ?? Pages.Map);
            yield break;
        }
        travelling = true;
        if (!restore) CaseZeroSoundFeedback.Current?.PlayTransition();
        bool reopen = restore && state.documentOpen;
        string page = state.page;
        view.travelCurtain.gameObject.SetActive(true);
        view.travelCurtain.blocksRaycasts = true;
        yield return DOTween.To(() => view.travelCurtain.alpha, a => view.travelCurtain.alpha = a, 1f, 0.28f).SetEase(Ease.InOutSine).SetUpdate(true).WaitForCompletion();
        if (destination != Pages.Map && destination != loadedLocation)
            yield return SceneManager.LoadSceneAsync(destination, LoadSceneMode.Additive);
        if (!string.IsNullOrEmpty(loadedLocation) && loadedLocation != destination)
        {
            var old = SceneManager.GetSceneByName(loadedLocation);
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
        }
        loadedLocation = destination == Pages.Map ? null : destination;
        state.location = destination;
        CloseDocument();
        view.SetLocation(destination);
        RefreshWorld();
        if (restore && state.Finished) OpenPage(Pages.Result);
        else if (reopen && (view.mapInDocument || page != Pages.Map) && page != Pages.LegacyScene) OpenPage(NormalizePage(page));
        Save(false);
        Say(view.mapInDocument ? "" : destination == Pages.Map ? "Choose a location on the map. Tab opens the investigation."
            : CaseZeroView.DisplayName(destination) + " • Use the Map to visit another location.");
        // Prepare the briefing while the location is still covered.
        if (!state.Finished && !state.introductionSeen)
        {
            yield return ShowPresentation("ApresentacaoCaso");
            yield return null; // Presentation.Start builds the radio before revealing it.
            Canvas.ForceUpdateCanvases();
        }
        yield return DOTween.To(() => view.travelCurtain.alpha, a => view.travelCurtain.alpha = a, 0f, 0.35f).SetEase(Ease.InOutSine).SetUpdate(true).WaitForCompletion();
        view.travelCurtain.blocksRaycasts = false;
        view.travelCurtain.gameObject.SetActive(false);
        travelling = false;
    }

    IEnumerator ShowPresentation(string scene)
    {
        if (presenting) yield break;
        if (!Application.CanStreamedLevelBeLoaded(scene)) { Say("Add the presentation to the build profile: " + scene); yield break; }
        presenting = true;
        view.ClearNotice();
        yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
    }

    /// <summary>Called by the radio when the briefing ends or is skipped.</summary>
    public void FinishIntroduction()
    {
        if (!presenting || state == null) return;
        state.introductionSeen = true;
        presenting = false;
        CloseDocument();
        Save(false);
        var scene = SceneManager.GetSceneByName("ApresentacaoCaso");
        if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
        TutorialHint(1, "Start by clicking a marked object in the scene to examine it.");
    }
}
