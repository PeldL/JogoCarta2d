using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public sealed class CaseZeroGame : MonoBehaviour
{
    [Header("Interface montada na cena")]
    [SerializeField] CaseZeroView view;
    [SerializeField] float autosaveInterval = 60;
    [SerializeField] float inactivityHintDelay = 120;
    CaseZeroState state;
    EvidenceManager evidenceManager;
    PhoneCallManager phone;
    InterrogationManager interrogation;
    PhoneCallAsset forensicCall;
    SuspectAsset suspectAsset;
    readonly List<SuspectAsset> caseSuspects = new List<SuspectAsset>();
    CaseZeroCase CurrentCase => state == null ? CaseZeroCases.All[0] : state.Definition;
    bool restartCurrentCase;
    readonly Dictionary<string, EvidenceData> catalog = new Dictionary<string, EvidenceData>();
    readonly string[] evidenceIds = { "registro", "pistola", "capsula", "sangue", "depoimento" };
    readonly string[] markerNames = { "Employee schedule", "Pistol under the counter", "Cartridge case on the floor", "Examine traces" };
    float saveTimer;
    bool paused;
    bool restoring;
    string selectedEvidence;
    string returnPage = "Mapa";
    int activeSlot = 1;
    int selectedSlot = 1;
    int confirmationSlot;
    bool confirming;
    bool saveCopyConfirmation;
    bool deleteConfirmation;
    bool accusationConfirmation;
    bool travelling;
    string loadedLocation;
    bool presenting;
    public CaseZeroState CampaignState => state;
    public bool Presenting => presenting;
    bool startupInitialized;
    public bool ReadyForReveal => startupInitialized && !travelling &&
        (!presenting || SceneManager.GetSceneByName("ApresentacaoCaso").isLoaded);

    void Start()
    {
        evidenceManager = EvidenceManager.Instance;
        phone = PhoneCallManager.Instance;
        interrogation = InterrogationManager.Instance;
        if (view == null || evidenceManager == null || phone == null || interrogation == null)
        {
            Debug.LogError("[Caso Zero] Vincule a interface e os três managers na cena.");
            enabled = false;
            return;
        }
        CreateContent();
        phone.OnCallScreenUpdated += CallUpdated;
        phone.OnCallEnded += CallEnded;
        interrogation.OnInterrogationUpdated += InterviewUpdated;
        ShowWelcome();
        if (CaseZeroSession.Consume(out int slot, out bool newGame))
        {
            selectedSlot = slot;
            if (newGame) StartInSlot(slot);
            else LoadGame();
        }
        startupInitialized = true;
    }

    void CreateContent()
    {
        catalog.Clear();
        if (forensicCall != null) Destroy(forensicCall);
        if (suspectAsset != null && !caseSuspects.Contains(suspectAsset)) Destroy(suspectAsset);
        foreach (var asset in caseSuspects) Destroy(asset);
        caseSuspects.Clear();
        if (state != null && state.CaseIndex > 0) { CreateLaterCase(); return; }
        AddEvidence("pistola", "Pistol under the counter", "A pistol was found hidden under the counter. Preserve it for ballistic comparison.");
        AddEvidence("capsula", "Cartridge case on the floor", "A cartridge case was found near the body in the bar service area. It indicates a shot was fired here.");
        AddEvidence("registro", "Employee records", "The schedule identifies the actual employee: he left before the crime. The man seen behind the counter does not work here.");
        AddEvidence("sangue", "Crime scene traces", "Bloodstains by the counter. Their position is consistent with the account of the victim's arrival.");
        AddEvidence("depoimento", "Challenged testimony", "Confronted with the schedule and the pistol, the man admits he was robbing the bar. The victim entered during the robbery.");
        forensicCall = ScriptableObject.CreateInstance<PhoneCallAsset>();
        forensicCall.callerName = "Forensics laboratory";
        forensicCall.nodes = new List<PhoneDialogueNode> {
            new PhoneDialogueNode { nodeId = "start", message = "The ballistic comparison is a match: the cartridge case recovered in the bar was fired by the pistol found under the counter. Compare this finding with the employee records and the testimony.", choices = new List<PhoneDialogueChoice> { new PhoneDialogueChoice { choiceText = "Record report and end call" } } }
        };
        suspectAsset = ScriptableObject.CreateInstance<SuspectAsset>();
        suspectAsset.suspectName = "Man behind the counter";
        suspectAsset.nodes = new List<InterrogationNode> {
            Node("start", "I was behind the counter when that woman came in. I have nothing else to say.",
                Choice("What were you doing at the bar?", "trabalho"), Choice("Did you know the victim?", "vitima"), Choice("Challenge with the schedule and weapon", "confronto", catalog["registro"])),
            Node("trabalho", "I... was helping with the service. I do not remember who called me.", Choice("Back to questions", "start")),
            Node("vitima", "I had never seen her before. She came in and ordered a drink. It all happened so fast.", Choice("Back to questions", "start")),
            Node("confronto", "All right. I was not an employee. I was robbing the bar. She came in, saw my face... I shot her because I was afraid she would report me.", Choice("End interview", ""))
        };
    }

    void AddEvidence(string id, string name, string description) => catalog.Add(id, new EvidenceData { id = id, evidenceName = name, description = description });
    static InterrogationChoice Choice(string text, string next, EvidenceData required = null) => new InterrogationChoice { choiceText = text, nextNodeId = next, requiredEvidence = required };
    static InterrogationNode Node(string id, string message, params InterrogationChoice[] choices) => new InterrogationNode { nodeId = id, message = message, choices = new List<InterrogationChoice>(choices) };

    void CreateLaterCase()
    {
        var data = CurrentCase;
        for (int i = 0; i < evidenceIds.Length; i++) AddEvidence(evidenceIds[i], data.evidenceNames[i], data.evidenceDescriptions[i]);
        forensicCall = ScriptableObject.CreateInstance<PhoneCallAsset>();
        forensicCall.callerName = "Investigation dispatch";
        forensicCall.nodes = new List<PhoneDialogueNode> { new PhoneDialogueNode { nodeId = "start", message = data.call,
            choices = new List<PhoneDialogueChoice> { new PhoneDialogueChoice { choiceText = "Record and end call" } } } };
        for (int i = 0; i < data.suspects.Length; i++)
        {
            var asset = ScriptableObject.CreateInstance<SuspectAsset>();
            asset.suspectName = data.suspects[i];
            asset.nodes = new List<InterrogationNode> {
                Node("start", data.statements[i], Choice("Compare testimony with evidence", "confronto", catalog["registro"]), Choice("End conversation", "")),
                Node("confronto", data.explanations[i], Choice("Back to testimony", "start"), Choice("End conversation", ""))
            };
            caseSuspects.Add(asset);
        }
        suspectAsset = caseSuspects[state.interviewSuspect];
    }

    void ConfigureCaseView()
    {
        var data = CurrentCase;
        view.investigationLocation = data.location;
        view.ShowPortraits(data, state.interviewSuspect);
        for (int i = 0; i < view.suspectLabels.Length; i++)
        {
            bool exists = i < data.suspects.Length;
            view.suspectLabels[i].transform.parent.gameObject.SetActive(exists);
            if (exists) view.suspectLabels[i].text = data.suspects[i];
        }
        for (int i = 0; i < view.objectLabels.Length; i++) view.objectLabels[i].text = data.objects[i];
        for (int i = 0; i < view.placeLabels.Length; i++) view.placeLabels[i].text = data.places[i];
        for (int i = 0; i < view.evidenceCardLabels.Length; i++) view.evidenceCardLabels[i].text = catalog[evidenceIds[i]].evidenceName;
        if (view.objectQuestion != null) view.objectQuestion.text = data.objectQuestion;
        if (view.placeQuestion != null) view.placeQuestion.text = data.placeQuestion;
        if (view.worldInterviewLabel != null) view.worldInterviewLabel.text = state.CaseIndex == 0 ? "Man behind the counter" : "Talk to suspects";
        if (view.caseBriefing != null) view.caseBriefing.text = $"CASE {state.CaseIndex + 1} • {data.title}\n{data.introduction}";
        if (view.suspectsSummary != null) view.suspectsSummary.text = state.CaseIndex == 0
            ? "Man behind the counter: claims he worked at the bar.\nEmployee: check the schedule.\nUnknown customer: compare this possibility with the evidence."
            : string.Join("\n\n", System.Array.ConvertAll(data.suspects, n => n)) + "\n\nInterview everyone in the Interviews tab and compare their accounts with the evidence.";
        for (int i = 0; i < view.caseMapButtons.Length; i++) view.caseMapButtons[i].gameObject.SetActive(true);
        for (int i = 0; i < view.interviewSuspectButtons.Length; i++)
        {
            view.interviewSuspectButtons[i].gameObject.SetActive(state.CaseIndex > 0);
            if (state.CaseIndex > 0) view.interviewSuspectButtons[i].GetComponentInChildren<TMPro.TMP_Text>(true).text = data.suspects[i];
        }
    }

    public void InterviewSuspect(int index)
    {
        if (state == null || state.Finished || confirming || travelling || index < 0 || index >= caseSuspects.Count) return;
        state.interviewSuspect = index;
        state.interviewNode = "start";
        interrogation.EndInterrogation();
        suspectAsset = caseSuspects[index];
        OpenPage("Interrogatório");
        Save(false);
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

    void ShowWelcome()
    {
        view.Feedback?.ResetFeedback();
        RefreshSlots();
        view.ShowPage("Introdução", false);
        Say("Start or continue an investigation.");
    }

    public void StartNewGame()
    {
        if (CaseZeroSave.ExistsInSlot(selectedSlot)) ShowSlotConfirmation(false);
        else StartInSlot(selectedSlot);
    }

    public void SelectSlot(int slot)
    {
        if (slot < 1 || slot > CaseZeroSave.SlotCount || confirming) return;
        selectedSlot = slot;
        RefreshSlots();
        Say($"Slot {slot} selected. Choose to start, save, or load.");
    }

    void RefreshSlots()
    {
        for (int slot = 1; slot <= CaseZeroSave.SlotCount; slot++)
        {
            var saved = CaseZeroSave.Read(out _, slot);
            string summary = saved != null ? (saved.solved ? "Case solved" : saved.failed ? "Case closed • defeat" : $"Under investigation • {TimeText(saved.elapsed)}")
                : CaseZeroSave.ExistsInSlot(slot) ? "Save unavailable" : "Empty";
            string label = $"{(slot == selectedSlot ? "• " : "")}SLOT {slot}\n{(saved != null ? "Case " + (saved.CaseIndex + 1) + " • " : "")}{summary}";
            for (int i = slot - 1; i < view.slotLabels.Length; i += CaseZeroSave.SlotCount) view.slotLabels[i].text = label;
        }
        view.continueButton.interactable = view.loadButton.interactable = CaseZeroSave.Read(out _, selectedSlot) != null;
        foreach (var button in view.deleteButtons) button.interactable = CaseZeroSave.ExistsInSlot(selectedSlot);
        view.slotContext.text = state == null ? $"Selected destination: slot {selectedSlot}"
            : $"Autosave: slot {activeSlot} • Save/load: slot {selectedSlot}";
    }

    void StartInSlot(int slot)
    {
        var fresh = new CaseZeroState();
        if (!CaseZeroSave.Write(fresh, out string error, slot)) { Say(error); return; }
        activeSlot = selectedSlot = slot;
        Begin(fresh);
        RefreshSlots();
        Say($"New investigation in slot {slot}. Autosave uses this slot.");
    }

    void Begin(CaseZeroState next)
    {
        view.Feedback?.ResetFeedback();
        view.ClearNotice();
        restoring = true;
        view.phonePresentation.ShowImmediate("");
        phone.ResetCalls();
        interrogation.EndInterrogation();
        state = next;
        // Existing saves with progress predate the introductory scenes.
        if (state.evidence.Count > 0 || state.elapsed > 0 || state.Finished) state.introductionSeen = true;
        // Cover before rebuilding the next case UI, not after its first visible frame.
        if (!state.Finished && !state.introductionSeen && view.travelCurtain != null)
        {
            view.travelCurtain.gameObject.SetActive(true);
            view.travelCurtain.alpha = 1;
            view.travelCurtain.blocksRaycasts = true;
        }
        CreateContent();
        ConfigureCaseView();
        state.cameraMode = false; // Legacy saves can contain an active camera mode.
        if (view.mapInDocument && NormalizeLocation(state.location) == "Mapa") state.location = "Bar";
        state.evidence.RemoveAll(id => !catalog.ContainsKey(id));
        evidenceManager.RestoreEvidence(state.evidence.ConvertAll(id => catalog[id]));
        if (state.callPending && !state.callRead) phone.QueueCall(forensicCall);
        paused = false;
        saveTimer = 0;
        selectedEvidence = null;
        restoring = false;
        RefreshWorld();
        RefreshProgress();
        if (view.backgroundMap) { StartCoroutine(ChangeLocation(NormalizeLocation(state.location), true)); return; }
        if (state.Finished) OpenPage("Resultado");
        else if (state.documentOpen) OpenPage(NormalizePage(state.page));
        else view.CloseDocument();
        Say("Tab opens the case file. Click evidence in the scene to investigate.");
    }

    string NormalizePage(string page)
    {
        if (page == "Telefone") return "Ligações";
        if (page == "Local" || page == "Introdução" || page == "Confirmar reinício") return "Mapa";
        foreach (var entry in view.pages) if (entry.name == page) return page;
        return "Mapa";
    }

    void Update()
    {
        if (state == null || travelling || presenting) return;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame) ToggleDocument();
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (view.documentRoot.activeSelf) CloseDocument();
                else OpenPage("Opções");
            }
        }
        if (paused || state.Finished) return;
        state.elapsed += Time.unscaledDeltaTime;
        state.idleTime += Time.unscaledDeltaTime;
        saveTimer += Time.unscaledDeltaTime;
        if (state.callDelay >= 0 && !state.callRead && !state.callPending)
        {
            state.callDelay -= Time.unscaledDeltaTime;
            if (state.callDelay <= 0)
            {
                state.callDelay = -1;
                state.callPending = true;
                phone.QueueCall(forensicCall);
                Say("Dispatch is calling. Open the phone to answer.");
                if (view.documentRoot.activeSelf && state.page == "Ligações") RefreshPhone();
                Save(false);
            }
        }
        if (!state.idleHintShown && state.idleTime >= inactivityHintDelay)
        {
            state.idleHintShown = true;
            Say("Dispatch: " + state.Objective);
        }
        if (saveTimer >= Mathf.Max(10, autosaveInterval)) Save(false);
        RefreshProgress();
    }

    void RefreshProgress()
    {
        if (view.objective != null) view.objective.text = "OBJECTIVE • " + state.Objective;
        bool ringing = !state.Finished && state.callPending && !state.callRead && phone.CurrentCall == null;
        if (view.incomingCallPulse != null) view.incomingCallPulse.enabled = ringing;
        view.Feedback?.SetPhoneRinging(ringing);
        view.phoneNotification.text = !state.Finished && state.callPending && !state.callRead ? "INCOMING CALL • open the phone" : "";
        view.progress.text = $"CASE {state.CaseIndex + 1} • {CurrentCase.title.ToUpperInvariant()}";
    }

    public void ToggleDocument()
    {
        if (state == null) return;
        if (view.documentRoot.activeSelf) CloseDocument();
        else OpenPage(view.separatePanels ? "Evidências" : NormalizePage(state.page));
    }

    public void CloseDocument()
    {
        if (confirming) { CancelRestart(); return; }
        if (state == null) return;
        if (state.Finished) { OpenPage("Resultado"); return; }
        if (state.page == "Confirmar reinício") state.page = returnPage;
        if (state.page == "Opções") state.page = "Mapa";
        paused = false;
        state.documentOpen = false;
        view.CloseDocument();
    }

    // These methods are persistent Button.OnClick targets, visible in the Inspector.
    public void OpenMap() { if (view.backgroundMap && !view.mapInDocument) TravelTo("Mapa"); else OpenPage("Mapa"); }
    public void OpenEvidence() => OpenPage("Evidências");
    public void OpenInterview() => OpenPage("Interrogatório");
    public void OpenPhone() => OpenPage("Ligações");
    public void OpenDeduction() => OpenPage("Dedução");
    public void OpenOptions() => OpenPage("Opções");
    public void OpenSaves() => OpenPage("Saves");
    public void OpenCredits() { CaseZeroSession.ShowCredits = true; ReturnToMenu(); }
    public void OpenSuspects() => OpenPage("Suspeitos");
    public void UnavailableLocation() => Say("This location is not available in the current case.");
    public void ReturnToMenu()
    {
        if (travelling) return;
        if (!UnityEngine.Application.CanStreamedLevelBeLoaded("Menu")) { Say("Add the Menu scene to the build profile."); return; }
        Save(false);
        travelling = true;
        CaseZeroSceneLoading.Load("Menu", null, null, view.documentTitle.font);
    }
    public void VisitBar() { if (view.backgroundMap) TravelTo("Bar"); else CloseDocument(); }

    public static string NormalizeLocation(string location)
    {
        switch (location)
        {
            case "Bar": case "Hospital": case "Delegacia": case "Padaria": case "Laboratorio": case "Rua": case "Estacionamento": case "Apartamento": case "Escritorio": return location;
            case "Joalheria": return "Laboratorio";
            case "Mercadinho": return "Padaria";
            default: return "Mapa";
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
        if (destination != "Mapa" && !Application.CanStreamedLevelBeLoaded(destination))
        {
            Say("Location not included in the build profile: " + destination);
            view.SetLocation(loadedLocation ?? "Mapa");
            yield break;
        }
        travelling = true;
        if (!restore) CaseZeroSoundFeedback.Current?.PlayTransition();
        bool reopen = restore && state.documentOpen;
        string page = state.page;
        view.travelCurtain.gameObject.SetActive(true);
        view.travelCurtain.blocksRaycasts = true;
        yield return DOTween.To(() => view.travelCurtain.alpha, a => view.travelCurtain.alpha = a, 1f, 0.28f).SetEase(Ease.InOutSine).SetUpdate(true).WaitForCompletion();
        if (destination != "Mapa" && destination != loadedLocation)
            yield return SceneManager.LoadSceneAsync(destination, LoadSceneMode.Additive);
        if (!string.IsNullOrEmpty(loadedLocation) && loadedLocation != destination)
        {
            var old = SceneManager.GetSceneByName(loadedLocation);
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
        }
        loadedLocation = destination == "Mapa" ? null : destination;
        state.location = destination;
        CloseDocument();
        view.SetLocation(destination);
        if (restore && state.Finished) OpenPage("Resultado");
        else if (reopen && (view.mapInDocument || page != "Mapa") && page != "Local") OpenPage(NormalizePage(page));
        Save(false);
        Say(view.mapInDocument ? "" : destination == "Mapa" ? "Choose a location on the map. Tab opens the investigation."
            : destination == "Bar" ? "Investigate the bar. Use the Map to leave."
            : CaseZeroView.DisplayName(destination) + " • Use the Map to return to town.");
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

    void OpenPage(string page)
    {
        if (state == null) { ShowWelcome(); return; }
        if (state.Finished && page != "Confirmar reinício") page = "Resultado";
        if (page == "Confirmar reinício") { ConfirmRestart(); return; }
        paused = page == "Opções" || page == "Saves";
        state.page = page;
        state.documentOpen = true;
        view.ShowPage(page, true);
        RefreshProgress();
        switch (page)
        {
            case "Evidências": RefreshBoard(); break;
            case "Interrogatório": ShowInterview(); break;
            case "Ligações": RefreshPhone(); break;
            case "Dedução": RefreshDeduction(); break;
            case "Opções": break;
            case "Saves": Save(false); RefreshSlots(); break;
            case "Resultado":
                view.ClearNotice();
                if (view.solutionText != null) view.solutionText.text = CurrentCase.ending;
                if (view.nextCaseButton != null) view.nextCaseButton.gameObject.SetActive(state.solved);
                if (view.nextCaseLabel != null) view.nextCaseLabel.text = state.CaseIndex < 2 ? "Go to Case " + (state.CaseIndex + 2) : "View campaign results";
                if (view.resultSolution != null) view.resultSolution.SetActive(state.solved);
                view.documentTitle.text = state.failed ? "CASE UNSOLVED" : "CASE SOLVED";
                view.resultStats.text = state.failed
                    ? $"All three accusations were incorrect.\nThe investigation is over.\n{TimeText(state.elapsed)} • {state.errors} mistakes\nYou can restart this case or return to the menu."
                    : $"{state.Rating}\n{state.Score} points  •  {TimeText(state.elapsed)}  •  {state.errors} mistake(s)";
                view.closeDocumentButton.interactable = false;
                break;
        }
        if (page == "Evidências") TutorialHint(4, "Read the recorded clues. Then interview the people involved and compare their accounts.");
        else if (page == "Interrogatório") TutorialHint(8, "Choose a question. Some questions become available only after you find supporting evidence.");
        else if (page == "Dedução") TutorialHint(16, "Choose the combination supported by the evidence. Confirming an incorrect accusation costs an attempt; canceling does not. You have three attempts.");
        else if (page == "Ligações") TutorialHint(32, "Answer incoming calls and read the information from dispatch.");
    }

    public void ToggleCamera()
    {
        // Kept for old scene bindings; evidence no longer needs a camera.
        if (state != null) state.cameraMode = false;
    }

    void RefreshWorld()
    {
        for (int i = 0; i < view.evidenceMarkers.Length; i++)
        {
            bool found = state.Has(evidenceIds[i]);
            view.evidenceMarkerLabels[i].text = (state.CaseIndex == 0 ? markerNames[i] : CurrentCase.evidenceNames[i]) + (found ? " • recorded" : "");
            view.evidenceMarkers[i].interactable = !state.Finished || found;
        }
    }

    public void ExamineEvidence(string id)
    {
        if (state == null || paused || travelling || !catalog.ContainsKey(id)) return;
        if (view.backgroundMap && state.location != CurrentCase.location) return;
        if (state.Has(id)) { SelectEvidence(id); return; }
        if (state.Finished) return;
        Collect(id);
        RefreshWorld();
    }

    void Collect(string id)
    {
        if (state.Has(id)) return;
        state.evidence.Add(id);
        view.NotifyEvidence(catalog[id].evidenceName);
        CaseZeroSoundFeedback.Current?.PlayEvidence();
        evidenceManager.CollectEvidence(catalog[id]);
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
        OpenPage("Evidências");
    }

    void RefreshBoard()
    {
        for (int i = 0; i < view.evidenceCards.Length; i++)
            view.evidenceCards[i].gameObject.SetActive(state.Has(evidenceIds[i]));
        bool hasEvidence = state.evidence.Count > 0;
        view.emptyEvidenceNotice.SetActive(!hasEvidence);
        view.evidenceDetails.SetActive(hasEvidence);
        if (!hasEvidence) return;
        if (selectedEvidence == null || !state.Has(selectedEvidence)) selectedEvidence = state.evidence[0];
        var data = catalog[selectedEvidence];
        view.evidenceName.text = data.evidenceName;
        view.evidenceDescription.text = data.description;
        view.evidenceType.text = "EVIDENCE • CASE 01";
    }

    void ShowInterview()
    {
        state.interviewedMask |= 1 << state.interviewSuspect;
        for (int i = 0; i < view.interviewSuspectButtons.Length; i++)
        {
            view.interviewSuspectButtons[i].interactable = i != state.interviewSuspect;
            if (state.CaseIndex > 0) view.interviewSuspectButtons[i].GetComponentInChildren<TMPro.TMP_Text>(true).text = CurrentCase.suspects[i] + ((state.interviewedMask & (1 << i)) != 0 ? " • interviewed" : "");
        }
        if (interrogation.CurrentSuspect != suspectAsset)
        {
            string nodeId = state.interviewNode;
            interrogation.StartInterrogation(suspectAsset);
            if (nodeId != "start" && suspectAsset.GetNode(nodeId) != null)
                interrogation.SelectChoice(Choice("Resume", nodeId));
        }
        else RenderInterview(interrogation.CurrentNode);
        Save(false);
    }

    void InterviewUpdated(SuspectAsset suspect, InterrogationNode node)
    {
        if (state == null || suspect != suspectAsset || restoring) return;
        state.interviewNode = node.nodeId;
        if (state.page == "Interrogatório") RenderInterview(node);
    }

    void RenderInterview(InterrogationNode node)
    {
        if (node == null) return;
        view.ShowPortraits(CurrentCase, state.interviewSuspect);
        view.interviewMessage.text = view.interviewPortraitName != null ? node.message : suspectAsset.suspectName + "\n\n" + node.message;
        for (int i = 0; i < view.interviewChoices.Length; i++)
        {
            bool exists = i < node.choices.Count;
            view.interviewChoices[i].gameObject.SetActive(exists);
            if (!exists) continue;
            var choice = node.choices[i];
            bool available = interrogation.IsChoiceAvailable(choice) && (choice.nextNodeId != "confronto" || CanConfront());
            view.interviewChoiceLabels[i].text = available ? choice.choiceText : state.CaseIndex == 0 ? "Challenge • find evidence that contradicts the account" : "Compare • investigate records and contradictions";
            view.interviewChoices[i].interactable = available;
        }
    }

    public void SelectInterviewChoice(int index)
    {
        if (state == null || paused || interrogation.CurrentNode == null) return;
        var choices = interrogation.CurrentNode.choices;
        if (index < 0 || index >= choices.Count) return;
        var choice = choices[index];
        if (!interrogation.IsChoiceAvailable(choice) || (choice.nextNodeId == "confronto" && !CanConfront())) return;
        state.idleTime = 0;
        if (!state.questions.Contains(choice.choiceText)) state.questions.Add(choice.choiceText);
        if (choice.nextNodeId == "confronto" && (state.CaseIndex == 0 || state.interviewSuspect == CurrentCase.culprit)) Collect("depoimento");
        interrogation.SelectChoice(choice);
        if (string.IsNullOrEmpty(choice.nextNodeId)) { state.interviewNode = "start"; CloseDocument(); }
        Save(false);
    }

    void RefreshPhone()
    {
        bool active = phone.CurrentCall != null;
        if (view.sendAnalysisButton != null) view.sendAnalysisButton.interactable = !state.Finished && state.CanRequestAnalysis && !active && !state.callRead && !state.callPending && state.callDelay < 0;
        view.answerPhoneButton.gameObject.SetActive(state.callPending && !state.callRead && !active);
        view.endPhoneButton.gameObject.SetActive(active);
        if (state.callRead || active)
        {
            view.phoneState.text = active ? "ON THE LINE • " + forensicCall.callerName.ToUpperInvariant() : "INFORMATION RECORDED";
            if (!view.phonePresentation.IsRevealing)
                view.phonePresentation.ShowImmediate(forensicCall.nodes[0].message);
        }
        else if (state.callPending)
        {
            view.phoneState.text = "INCOMING CALL";
            view.phonePresentation.ShowImmediate(forensicCall.callerName + ".\nAnswer to receive new information about the case.");
        }
        else
        {
            view.phoneState.text = "NO PENDING CALLS";
            view.phonePresentation.ShowImmediate(state.callDelay >= 0
                ? "Material submitted. Forensics is preparing its response. Keep investigating; the phone will alert you when the call arrives."
                : state.CanRequestAnalysis ? "You can request an examination of the available material. This does not mean the investigation is complete: keep looking for clues and comparing accounts."
                : (state.CaseIndex == 0
                    ? "DISPATCH AVAILABLE\nRead the documents or ask for guidance. For a ballistic comparison, preserve the weapon and the material from the shooting."
                    : "DISPATCH AVAILABLE\nRead the documents or ask for guidance. Preserve the records and traces at the scene to request an examination."));
        }
    }

    public void SendForAnalysis()
    {
        if (state == null || state.Finished || !state.CanRequestAnalysis || state.callRead || state.callPending || state.callDelay >= 0) return;
        state.callDelay = 8;
        CaseZeroSoundFeedback.Current?.PlayTransmission();
        state.idleTime = 0;
        Save(false);
        RefreshPhone();
        Say("Material sent to forensics. Keep investigating and answer when the phone rings.");
    }
    bool CanConsultPhone()
    {
        if (state == null || state.Finished) return false;
        if (phone.CurrentCall == null) return true;
        Say("Record the report and end the call before opening another document.");
        return false;
    }
    public void AskCentral()
    {
        if (!CanConsultPhone()) return;
        view.phoneState.text = "DISPATCH • GUIDANCE";
        view.phonePresentation.PlayMessage("Dispatch: " + state.Objective + "\n\nRead what each piece of evidence establishes and compare it with the statements. A suspicion should become an accusation only when the information fits.");
    }
    public void OpenCaseDocuments()
    {
        if (!CanConsultPhone()) return;
        view.phoneState.text = "SHIFT DOCUMENTS";
        view.phonePresentation.ShowImmediate((state.CaseIndex == 0 ? "Initial incident: a woman was found shot dead in a bar. The circumstances need to be investigated." : CurrentCase.introduction)
            + "\n\nReference location: " + CaseZeroView.DisplayName(CurrentCase.location) + ".\n\nOpen the case file with Tab or the on-screen button. Map: visit locations. Evidence: read collected records. Interviews: listen and compare accounts. Deduction: present your conclusion after recording the report.");
    }
    public void OpenAnalysisReport()
    {
        if (!CanConsultPhone()) return;
        if (state.callRead) { view.phoneState.text = "FORENSIC REPORT RECORDED"; view.phonePresentation.ShowImmediate(forensicCall.nodes[0].message); }
        else RefreshPhone();
    }

    public void AnswerPhone() { if (state != null && !paused) phone.AnswerPendingCall(); }
    public void EndPhone() { if (state != null && !paused && phone.CurrentCall != null && !view.phonePresentation.IsRevealing) phone.EndCall(); }

    void CallUpdated(PhoneCallAsset call, PhoneDialogueNode node)
    {
        if (state == null || restoring) return;
        view.Feedback?.SetPhoneRinging(false);
        OpenPage("Ligações");
        view.phonePresentation.PlayMessage(node.message);
    }

    void CallEnded()
    {
        if (state == null || restoring) return;
        state.callRead = true;
        state.callPending = false;
        state.idleTime = 0;
        Save(false);
        RefreshProgress();
        if (state.page == "Ligações") RefreshPhone();
    }

    bool CanConfront() => state.Has("pistola") && (state.CaseIndex == 0 || (state.Has("capsula") && state.Has("sangue")));
    public void SelectSuspect(int index) { if (state == null || state.Finished || confirming || index < 0 || index >= CurrentCase.suspects.Length) return; state.suspect = index; RefreshDeduction(); }
    public void SelectWeapon(int index) { if (state == null || index < 0 || index > 2) return; state.weapon = index; RefreshDeduction(); }
    public void SelectPlace(int index) { if (state == null || index < 0 || index > 2) return; state.place = index; RefreshDeduction(); }

    void RefreshDeduction()
    {
        view.ShowSelection(view.suspectChecks, state.suspect);
        view.ShowSelection(view.weaponChecks, state.weapon);
        view.ShowSelection(view.placeChecks, state.place);
        view.concludeButton.interactable = state.CanConclude && !state.Finished;
        view.deductionHint.text = state.Finished ? "This case is already closed. View the result or restart through Settings."
            : state.CanConclude ? $"Review your theory before confirming. {state.AttemptsRemaining} accusation attempt(s) remaining."
            : state.HasSceneEvidence && state.Has("depoimento") && state.AllInterviewed && !state.callRead ? "The forensic report is missing: submit the material by phone, answer, and record the response."
            : state.CaseIndex == 0 ? "Investigate the scene, compare statements, and consult forensics." : "Examine the records, interview those involved, and challenge contradictions.";
    }

    public void ConcludeCase()
    {
        if (state == null || paused || state.Finished || !state.CanConclude || confirming) return;
        ShowSlotConfirmation(false);
        accusationConfirmation = true;
        view.documentTitle.text = "CONFIRM ACCUSATION";
        view.confirmationButtonLabel.text = "Confirm accusation";
        view.confirmationMessage.text = $"You have {state.AttemptsRemaining} attempt(s).\n\nAn incorrect accusation costs one attempt. After three mistakes, the investigation ends in defeat.\n\nConfirm the selected combination?";
    }

    void AccusationConfirmed()
    {
        accusationConfirmation = false;
        confirming = false;
        paused = false;
        bool solved = state.Accuse();
        OpenPage(state.Finished ? "Resultado" : "Dedução");
        Save(false);
        if (solved) view.Feedback?.ShowVictory(state.Score, state.Rating, state.CaseIndex == CaseZeroCases.All.Length - 1);
        else view.Feedback?.ShowWrongAccusation(state.AttemptsRemaining, state.failed);
        Say(solved ? "Case closed. Your evaluation has been saved." : state.failed ? "No attempts remaining. You can try again." : $"Incorrect accusation. {state.AttemptsRemaining} attempt(s) remaining. Review the evidence.");
    }

    public void ConfirmRestart()
    {
        if (state != null) selectedSlot = activeSlot;
        ShowSlotConfirmation(false);
        restartCurrentCase = state != null;
        if (restartCurrentCase) view.confirmationMessage.text = "Restart only the current case? Its clues and attempts will be reset. Previously completed cases will be preserved.";
    }

    void ShowSlotConfirmation(bool saveCopy)
    {
        restartCurrentCase = false;
        accusationConfirmation = false;
        deleteConfirmation = false;
        confirming = true;
        saveCopyConfirmation = saveCopy;
        confirmationSlot = selectedSlot;
        returnPage = state == null ? "Introdução" : state.page;
        paused = true;
        view.ShowPage("Confirmar reinício", false);
        view.documentTitle.text = "CONFIRM OVERWRITE";
        view.confirmationButtonLabel.text = "Confirm overwrite";
        view.confirmationMessage.text = saveCopy
            ? $"Saving the current investigation to SLOT {confirmationSlot} overwrites its existing progress.\n\nAutosave will then use this slot. Continue?"
            : $"Starting a new case in SLOT {confirmationSlot} overwrites its existing progress.\n\nThe other slots will remain intact. Continue?";
        if (state != null) state.documentOpen = true;
    }

    public void CancelRestart()
    {
        accusationConfirmation = false;
        confirming = false;
        deleteConfirmation = false;
        paused = false;
        if (state == null) ShowWelcome();
        else OpenPage(NormalizePage(returnPage));
    }

    public void RestartConfirmed()
    {
        if (!confirming) return;
        if (accusationConfirmation) { AccusationConfirmed(); return; }
        if (deleteConfirmation) { DeleteConfirmed(); return; }
        int slot = confirmationSlot;
        bool saveCopy = saveCopyConfirmation;
        var retry = restartCurrentCase && state != null ? state.RestartCase() : null;
        restartCurrentCase = false;
        CancelRestart();
        if (saveCopy) SaveToSlot(slot);
        else if (retry != null)
        {
            if (!CaseZeroSave.Write(retry, out string error, slot)) { Say(error); return; }
            Begin(retry);
        }
        else StartInSlot(slot);
    }

    public void ConfirmDeleteSave()
    {
        if (confirming || !CaseZeroSave.ExistsInSlot(selectedSlot)) return;
        ShowSlotConfirmation(false);
        deleteConfirmation = true;
        view.documentTitle.text = "DELETE SAVE";
        view.confirmationButtonLabel.text = "Delete permanently";
        view.confirmationMessage.text = $"Delete SLOT {confirmationSlot} and its backup files? This cannot be undone.\n\n"
            + (state != null && confirmationSlot == activeSlot
                ? "The current investigation will end and you will return to the introduction."
                : "The other slots and the current investigation will remain intact.");
    }

    void DeleteConfirmed()
    {
        int slot = confirmationSlot;
        if (!CaseZeroSave.Delete(slot, out string error)) { Say(error); return; }
        // Clear the live state BEFORE navigation or pause/quit can autosave it again.
        if (state != null && slot == activeSlot)
        {
            state = null;
            restoring = true;
            view.phonePresentation.ShowImmediate("");
            phone.ResetCalls();
            interrogation.EndInterrogation();
            evidenceManager.RestoreEvidence(new List<EvidenceData>());
            restoring = false;
            saveTimer = 0;
            selectedEvidence = null;
            view.incomingCallPulse.enabled = false;
            view.Feedback?.ResetFeedback();
            view.progress.text = "NO ACTIVE INVESTIGATION";
            view.phoneNotification.text = "Start or load an investigation.";
        }
        CancelRestart();
        RefreshSlots();
        Say($"Slot {slot} deleted, including its backups.");
    }

    public void SaveGame()
    {
        if (state == null || confirming) return;
        if (selectedSlot != activeSlot && CaseZeroSave.ExistsInSlot(selectedSlot)) ShowSlotConfirmation(true);
        else SaveToSlot(selectedSlot);
    }

    void SaveToSlot(int slot)
    {
        if (!CaseZeroSave.Write(state, out string error, slot)) { Say(error); return; }
        activeSlot = selectedSlot = slot;
        saveTimer = 0;
        RefreshSlots();
        Say($"Investigation saved to slot {slot}. Autosave now uses this slot.");
    }

    void Save(bool manual)
    {
        if (state == null || restoring) return;
        saveTimer = 0;
        if (!CaseZeroSave.Write(state, out string error, activeSlot)) Say(error);
        else if (manual) Say($"Investigation saved to slot {activeSlot}.");
    }

    public void LoadGame()
    {
        if (confirming) return;
        var loaded = CaseZeroSave.Read(out string notice, selectedSlot);
        if (loaded == null) { Say(notice); return; }
        activeSlot = selectedSlot;
        Begin(loaded);
        RefreshSlots();
        Say(notice ?? $"Investigation restored from slot {activeSlot}.");
    }

    void OnApplicationPause(bool value) { if (value) Save(false); }
    void OnApplicationQuit() => Save(false);
    void OnDestroy()
    {
        if (phone != null) { phone.OnCallScreenUpdated -= CallUpdated; phone.OnCallEnded -= CallEnded; }
        if (interrogation != null) interrogation.OnInterrogationUpdated -= InterviewUpdated;
        if (forensicCall != null) Destroy(forensicCall);
        if (suspectAsset != null && !caseSuspects.Contains(suspectAsset)) Destroy(suspectAsset);
        foreach (var asset in caseSuspects) Destroy(asset);
    }
    void TutorialHint(int flag, string message)
    {
        if (state == null || state.CaseIndex != 0 || state.Finished || !state.introductionSeen || (state.tutorialHints & flag) != 0) return;
        state.tutorialHints |= flag;
        Say("Dispatch: " + message);
        Save(false);
    }
    void Say(string message) { if (view != null) view.status.text = message; }
    static string TimeText(float time) => $"{(int)time / 60:00}:{(int)time % 60:00}";
}
