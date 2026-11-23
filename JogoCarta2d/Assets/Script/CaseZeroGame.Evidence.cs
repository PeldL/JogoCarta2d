using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Pages = CaseZeroPages;

// Case content built from CaseZeroCases, the clickable markers in each location and the
// evidence board in the case file.
public sealed partial class CaseZeroGame
{
    const int EvidenceCardsPerPage = 5;
    readonly Dictionary<string, EvidenceData> catalog = new Dictionary<string, EvidenceData>();
    int evidencePage;
    string selectedEvidence;

    /// <summary>Builds the evidence catalog, the forensics call and the interviews for the current case.</summary>
    void CreateContent()
    {
        DestroyCaseAssets();
        catalog.Clear();
        var data = CurrentCase;
        for (int i = 0; i < data.evidenceKeys.Length; i++)
            AddEvidence(data.evidenceKeys[i], data.evidenceNames[i], data.evidenceDescriptions[i]);
        AddEvidence("depoimento", data.testimonyEvidenceName, data.testimonyEvidenceDescription);
        CreateForensicCall();
        CreateInterviews();
    }

    void DestroyCaseAssets()
    {
        if (forensicCall != null) Destroy(forensicCall);
        foreach (var asset in caseSuspects) Destroy(asset);
        caseSuspects.Clear();
        suspectAsset = null;
    }

    void AddEvidence(string id, string name, string description) => catalog.Add(id, new EvidenceData { id = id, evidenceName = name, description = description });

    void ConfigureCaseView()
    {
        var data = CurrentCase;
        view.investigationLocation = data.location;
        view.SetAvailableLocations(data.locations);
        view.ShowPortraits(data, state.interviewSuspect);
        for (int i = 0; i < view.suspectLabels.Length; i++)
        {
            if (view.suspectLabels[i] == null) continue;
            bool exists = i < data.suspects.Length;
            var row = view.suspectLabels[i].transform.parent;
            if (row != null) row.gameObject.SetActive(exists);
            if (exists) view.suspectLabels[i].text = data.suspects[data.SuspectAtSlot(i)];
        }
        FillLabels(view.objectLabels, data.objects);
        FillLabels(view.placeLabels, data.places);
        if (view.objectQuestion != null) view.objectQuestion.text = data.objectQuestion;
        if (view.placeQuestion != null) view.placeQuestion.text = data.placeQuestion;
        bool singlePerson = data.interviewNames.Length == 1;
        if (view.worldInterviewLabel != null) view.worldInterviewLabel.text = singlePerson ? data.interviewNames[0] : "Talk to people";
        if (view.caseBriefing != null) view.caseBriefing.text = $"CASE {state.CaseIndex + 1} • {data.title}\n{data.introduction}";
        if (view.suspectsSummary != null) view.suspectsSummary.text = "People to interview:\n"
            + string.Join("\n", System.Array.ConvertAll(data.interviewNames, n => "• " + n)) + "\n\nCompare their accounts with the evidence.";
        // With a single person there is nobody to switch to.
        for (int i = 0; i < view.interviewSuspectButtons.Length; i++)
        {
            bool exists = !singlePerson && i < data.interviewNames.Length;
            view.interviewSuspectButtons[i].gameObject.SetActive(exists);
            if (exists) view.interviewSuspectButtons[i].GetComponentInChildren<TMPro.TMP_Text>(true).text = data.interviewNames[i];
        }
    }

    static void FillLabels(TMPro.TMP_Text[] labels, string[] values)
    {
        if (labels == null) return;
        for (int i = 0; i < labels.Length; i++)
            if (labels[i] != null) labels[i].text = values != null && i < values.Length ? values[i] : "";
    }

    void RefreshWorld()
    {
        if (state == null) return;
        var keys = CurrentCase.EvidenceAt(state.location, state.analysisStage);
        for (int i = 0; i < view.evidenceMarkers.Length; i++)
        {
            bool exists = i < keys.Length;
            var marker = view.evidenceMarkers[i];
            marker.gameObject.SetActive(exists);
            if (!exists) continue;
            string id = keys[i];
            bool found = state.Has(id);
            view.evidenceMarkerLabels[i].text = catalog[id].evidenceName + (found ? " • recorded" : "");
            marker.interactable = !state.Finished || found;
        }
    }

    public void ExamineMarker(int markerIndex)
    {
        if (state == null || markerIndex < 0 || markerIndex >= view.evidenceMarkers.Length) return;
        var keys = CurrentCase.EvidenceAt(state.location, state.analysisStage);
        if (markerIndex >= keys.Length) return;
        ExamineEvidence(keys[markerIndex]);
    }

    public void ExamineEvidence(string id)
    {
        if (state == null || paused || travelling || !catalog.ContainsKey(id)) return;
        int clueIndex = CurrentCase.EvidenceIndex(id);
        if (clueIndex >= 0 && CurrentCase.evidenceLocations[clueIndex] != state.location) return;
        if (clueIndex >= 0 && state.analysisStage < CurrentCase.UnlockStage(id)) return;
        if (state.Has(id)) { SelectEvidence(id); return; }
        if (state.Finished) return;
        Collect(id);
        RefreshWorld();
    }

    // Kept for old scene bindings; evidence no longer needs a camera.
    public void ToggleCamera() { if (state != null) state.cameraMode = false; }

    void Collect(string id)
    {
        if (state.Has(id)) return;
        state.evidence.Add(id);
        view.PulseMarker(System.Array.IndexOf(CurrentCase.EvidenceAt(state.location, state.analysisStage), id));
        view.NotifyEvidence(catalog[id].evidenceName);
        CaseZeroSoundFeedback.Current?.PlayEvidence();
        state.idleTime = 0;
        Say("");
        Save(false);
        RefreshProgress();
        TutorialHint(2, "Clue recorded. Open the case file with Tab or the investigation button and check Evidence.");
    }

    public void SelectEvidence(string id)
    {
        if (state == null || !state.Has(id)) return;
        selectedEvidence = id;
        int index = CollectedEvidenceKeys().IndexOf(id);
        if (index >= 0) evidencePage = index / EvidenceCardsPerPage;
        OpenPage(Pages.Evidence);
    }

    void RefreshBoard()
    {
        var collected = CollectedEvidenceKeys();
        int pageCount = PageCount(collected);
        evidencePage = Mathf.Clamp(evidencePage, 0, pageCount - 1);
        int start = evidencePage * EvidenceCardsPerPage;
        for (int i = 0; i < view.evidenceCards.Length; i++)
        {
            int recordIndex = start + i;
            bool exists = recordIndex < collected.Count;
            view.evidenceCards[i].gameObject.SetActive(exists);
            if (exists) view.evidenceCardLabels[i].text = catalog[collected[recordIndex]].evidenceName;
        }
        bool hasEvidence = collected.Count > 0;
        view.emptyEvidenceNotice.SetActive(!hasEvidence);
        view.evidenceDetails.SetActive(hasEvidence);
        if (!hasEvidence) return;
        int selectedIndex = collected.IndexOf(selectedEvidence);
        if (selectedIndex < start || selectedIndex >= start + EvidenceCardsPerPage)
            selectedEvidence = collected[start];
        ShowEvidenceDetails(pageCount);
    }

    static int PageCount(List<string> collected) => Mathf.Max(1, Mathf.CeilToInt(collected.Count / (float)EvidenceCardsPerPage));

    void ShowEvidenceDetails(int pageCount)
    {
        var data = catalog[selectedEvidence];
        view.evidenceName.text = data.evidenceName;
        view.evidenceDescription.text = data.description;
        view.ShowEvidencePicture(CurrentCase.EvidenceArt(selectedEvidence));
        view.evidenceType.text = pageCount > 1
            ? $"EVIDENCE • CASE {state.CaseIndex + 1:00} • SCROLL THE LIST TO BROWSE"
            : $"EVIDENCE • CASE {state.CaseIndex + 1:00}";
    }

    List<string> CollectedEvidenceKeys()
    {
        var collected = new List<string>();
        foreach (var id in CurrentCase.evidenceKeys) if (state.Has(id)) collected.Add(id);
        if (state.Has("depoimento")) collected.Add("depoimento");
        return collected;
    }

    void SelectEvidenceCard(int slot)
    {
        if (state == null || slot < 0 || slot >= EvidenceCardsPerPage) return;
        var collected = CollectedEvidenceKeys();
        int index = evidencePage * EvidenceCardsPerPage + slot;
        if (index < 0 || index >= collected.Count || selectedEvidence == collected[index]) return;
        selectedEvidence = collected[index];
        ShowEvidenceDetails(PageCount(collected));
        view.AnimateEvidenceDetails();
    }

    void BrowseEvidenceWithMouseWheel()
    {
        if (state == null || !view.IsDocumentOpen || state.page != Pages.Evidence || Mouse.current == null) return;
        float wheel = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(wheel) < 0.01f || view.evidenceCards.Length == 0) return;
        var listRect = view.evidenceCards[0].transform.parent as RectTransform;
        if (listRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(listRect, Mouse.current.position.ReadValue(), null, out var local)) return;
        // The evidence list occupies the left-hand half of the open case file.
        if (local.x > listRect.rect.xMin + listRect.rect.width * 0.48f) return;
        int pageCount = PageCount(CollectedEvidenceKeys());
        if (pageCount <= 1) return;
        int next = Mathf.Clamp(evidencePage + (wheel < 0 ? 1 : -1), 0, pageCount - 1);
        if (next == evidencePage) return; // Already at the first/last page: no click, no redraw.
        evidencePage = next;
        RefreshBoard();
        view.AnimateEvidenceDetails();
        CaseZeroSoundFeedback.Current?.PlayClick();
    }

    void WireDynamicButtons()
    {
        for (int i = 0; i < view.evidenceCards.Length; i++)
        {
            int slot = i;
            view.evidenceCards[i].onClick.AddListener(() => SelectEvidenceCard(slot));
        }
    }
}
