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
    readonly Dictionary<string, EvidenceData> catalog = new Dictionary<string, EvidenceData>();
    readonly string[] evidenceIds = { "registro", "pistola", "capsula", "sangue", "depoimento" };
    readonly string[] markerNames = { "Escala de funcionários", "Pistola sob o balcão", "Cápsula no chão", "Examinar vestígios" };
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
    }

    void CreateContent()
    {
        AddEvidence("pistola", "Pistola sob o balcão", "Uma pistola foi encontrada escondida sob o balcão. Preserve-a para confronto balístico.");
        AddEvidence("capsula", "Cápsula no chão", "Cápsula encontrada próxima ao corpo, na área de atendimento do bar. Indica um disparo neste local.");
        AddEvidence("registro", "Registro de funcionários", "A escala identifica o verdadeiro funcionário: ele saiu antes do crime. O homem visto no balcão não trabalha aqui.");
        AddEvidence("sangue", "Vestígios da cena do crime", "Marcas de sangue junto ao balcão. A posição é compatível com o relato da entrada da vítima.");
        AddEvidence("depoimento", "Depoimento confrontado", "Ao ser confrontado com a escala e a pistola, o homem admite que assaltava o bar. A vítima entrou durante o roubo.");
        forensicCall = ScriptableObject.CreateInstance<PhoneCallAsset>();
        forensicCall.callerName = "Laboratório de perícia";
        forensicCall.nodes = new List<PhoneDialogueNode> {
            new PhoneDialogueNode { nodeId = "start", message = "O confronto balístico é compatível: a cápsula recolhida no bar foi disparada pela pistola encontrada sob o balcão. Cruze esse resultado com o registro de funcionários e o depoimento.", choices = new List<PhoneDialogueChoice> { new PhoneDialogueChoice { choiceText = "Registrar laudo e encerrar" } } }
        };
        suspectAsset = ScriptableObject.CreateInstance<SuspectAsset>();
        suspectAsset.suspectName = "Homem do balcão";
        suspectAsset.nodes = new List<InterrogationNode> {
            Node("start", "Eu estava atrás do balcão quando aquela mulher entrou. Não tenho mais nada a dizer.",
                Choice("O que você fazia no bar?", "trabalho"), Choice("Você conhecia a vítima?", "vitima"), Choice("Confrontar a escala e a arma", "confronto", catalog["registro"])),
            Node("trabalho", "Eu... estava ajudando no atendimento. Não lembro quem me chamou.", Choice("Voltar às perguntas", "start")),
            Node("vitima", "Nunca a tinha visto. Ela entrou e pediu uma bebida. Foi tudo muito rápido.", Choice("Voltar às perguntas", "start")),
            Node("confronto", "Está bem. Eu não era funcionário. Estava roubando o bar. Ela entrou, viu meu rosto... atirei porque tive medo de que me denunciasse.", Choice("Encerrar depoimento", ""))
        };
    }

    void AddEvidence(string id, string name, string description) => catalog.Add(id, new EvidenceData { id = id, evidenceName = name, description = description });
    static InterrogationChoice Choice(string text, string next, EvidenceData required = null) => new InterrogationChoice { choiceText = text, nextNodeId = next, requiredEvidence = required };
    static InterrogationNode Node(string id, string message, params InterrogationChoice[] choices) => new InterrogationNode { nodeId = id, message = message, choices = new List<InterrogationChoice>(choices) };

    void ShowWelcome()
    {
        RefreshSlots();
        view.ShowPage("Introdução", false);
        Say("Inicie ou continue a investigação.");
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
        Say($"Slot {slot} selecionado. Escolha iniciar, salvar ou carregar.");
    }

    void RefreshSlots()
    {
        for (int slot = 1; slot <= CaseZeroSave.SlotCount; slot++)
        {
            var saved = CaseZeroSave.Read(out _, slot);
            string summary = saved != null ? (saved.solved ? "Caso resolvido" : saved.failed ? "Caso encerrado • derrota" : $"{saved.evidence.Count}/5 provas • {TimeText(saved.elapsed)}")
                : CaseZeroSave.ExistsInSlot(slot) ? "Save indisponível" : "Vazio";
            string label = $"{(slot == selectedSlot ? "• " : "")}SLOT {slot}\n{summary}";
            for (int i = slot - 1; i < view.slotLabels.Length; i += CaseZeroSave.SlotCount) view.slotLabels[i].text = label;
        }
        view.continueButton.interactable = view.loadButton.interactable = CaseZeroSave.Read(out _, selectedSlot) != null;
        foreach (var button in view.deleteButtons) button.interactable = CaseZeroSave.ExistsInSlot(selectedSlot);
        view.slotContext.text = state == null ? $"Destino selecionado: slot {selectedSlot}"
            : $"Automático: slot {activeSlot} • Salvar/carregar: slot {selectedSlot}";
    }

    void StartInSlot(int slot)
    {
        var fresh = new CaseZeroState();
        if (!CaseZeroSave.Write(fresh, out string error, slot)) { Say(error); return; }
        activeSlot = selectedSlot = slot;
        Begin(fresh);
        RefreshSlots();
        Say($"Nova investigação no slot {slot}. Salvamento automático neste slot.");
    }

    void Begin(CaseZeroState next)
    {
        view.ClearNotice();
        restoring = true;
        view.phonePresentation.ShowImmediate("");
        phone.ResetCalls();
        interrogation.EndInterrogation();
        state = next;
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
        Say("Tab abre o documento. Clique nas provas do cenário para investigar.");
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
        if (state == null || travelling) return;
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
                Say("Chamada da perícia. Abra o telefone para atender.");
                CaseZeroSoundFeedback.Current?.PlayNotification();
                if (view.documentRoot.activeSelf && state.page == "Ligações") RefreshPhone();
                Save(false);
            }
        }
        if (!state.idleHintShown && state.idleTime >= inactivityHintDelay)
        {
            state.idleHintShown = true;
            Say("Central: confira a escala, examine os vestígios e confronte o suspeito.");
        }
        if (saveTimer >= Mathf.Max(10, autosaveInterval)) Save(false);
        RefreshProgress();
    }

    void RefreshProgress()
    {
        if (view.objective != null) view.objective.text = "OBJETIVO • " + state.Objective;
        view.incomingCallPulse.enabled = state.callPending && !state.callRead && phone.CurrentCall == null;
        view.phoneNotification.text = state.callPending && !state.callRead ? "CHAMADA DA PERÍCIA • abra o telefone" : "";
        view.progress.text = $"PROVAS {state.evidence.Count}/5     TEMPO {TimeText(state.elapsed)}     ERROS {state.errors}";
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
    public void OpenSuspects() => OpenPage("Suspeitos");
    public void UnavailableLocation() => Say("Esse local ainda não está disponível neste caso.");
    public void ReturnToMenu()
    {
        if (travelling) return;
        if (!UnityEngine.Application.CanStreamedLevelBeLoaded("Menu")) { Say("Inclua a cena Menu no perfil de build."); return; }
        Save(false);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
    public void VisitBar() { if (view.backgroundMap) TravelTo("Bar"); else CloseDocument(); }

    public static string NormalizeLocation(string location)
    {
        switch (location)
        {
            case "Bar": case "Hospital": case "Delegacia": case "Joalheria": case "Mercadinho": return location;
            default: return "Mapa";
        }
    }

    public void TravelTo(string location)
    {
        if (state == null || travelling || confirming || !view.backgroundMap) return;
        StartCoroutine(ChangeLocation(NormalizeLocation(location), false));
    }

    IEnumerator ChangeLocation(string destination, bool restore)
    {
        if (destination != "Mapa" && !Application.CanStreamedLevelBeLoaded(destination))
        {
            Say("O local não está no perfil de build: " + destination);
            view.SetLocation(loadedLocation ?? "Mapa");
            yield break;
        }
        travelling = true;
        bool reopen = restore && state.documentOpen;
        string page = state.page;
        view.travelCurtain.gameObject.SetActive(true);
        view.travelCurtain.blocksRaycasts = true;
        yield return DOTween.To(() => view.travelCurtain.alpha, a => view.travelCurtain.alpha = a, 1f, 0.18f).SetUpdate(true).WaitForCompletion();
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
        Say(view.mapInDocument ? "" : destination == "Mapa" ? "Escolha um local no mapa. Tab abre a investigação."
            : destination == "Bar" ? "Investigue as pistas do bar. Use Mapa para sair."
            : destination + " • Use Mapa para voltar à cidade.");
        yield return DOTween.To(() => view.travelCurtain.alpha, a => view.travelCurtain.alpha = a, 0f, 0.18f).SetUpdate(true).WaitForCompletion();
        view.travelCurtain.blocksRaycasts = false;
        view.travelCurtain.gameObject.SetActive(false);
        travelling = false;
    }

    void OpenPage(string page)
    {
        if (state == null) { ShowWelcome(); return; }
        if (state.Finished && page != "Confirmar reinício") page = "Resultado";
        if (page == "Confirmar reinício") { ConfirmRestart(); return; }
        paused = page == "Opções";
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
            case "Opções": Save(false); RefreshSlots(); break;
            case "Resultado":
                view.ClearNotice();
                if (view.resultSolution != null) view.resultSolution.SetActive(state.solved);
                view.documentTitle.text = state.failed ? "CASO NÃO RESOLVIDO" : "CASO RESOLVIDO";
                view.resultStats.text = state.failed
                    ? $"As três acusações estavam incorretas.\nA investigação foi encerrada.\n{TimeText(state.elapsed)} • {state.errors} erros\nVocê pode recomeçar o caso ou voltar ao menu."
                    : $"{state.Rating}\n{state.Score} pontos  •  {TimeText(state.elapsed)}  •  {state.errors} erro(s)";
                view.closeDocumentButton.interactable = false;
                break;
        }
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
            view.evidenceMarkerLabels[i].text = markerNames[i] + (found ? " • registrado" : "");
            view.evidenceMarkers[i].interactable = !state.Finished || found;
        }
    }

    public void ExamineEvidence(string id)
    {
        if (state == null || paused || !catalog.ContainsKey(id)) return;
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
        if (state.Has("pistola") && state.Has("capsula") && state.callDelay < 0 && !state.callPending && !state.callRead)
            state.callDelay = 30;
        Say("");
        Save(false);
        RefreshProgress();
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
        view.evidenceType.text = "EVIDÊNCIA • CASO 01";
    }

    void ShowInterview()
    {
        if (interrogation.CurrentSuspect != suspectAsset)
        {
            string nodeId = state.interviewNode;
            interrogation.StartInterrogation(suspectAsset);
            if (nodeId != "start" && suspectAsset.GetNode(nodeId) != null)
                interrogation.SelectChoice(Choice("Retomar", nodeId));
        }
        else RenderInterview(interrogation.CurrentNode);
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
        view.interviewMessage.text = node.message;
        for (int i = 0; i < view.interviewChoices.Length; i++)
        {
            bool exists = i < node.choices.Count;
            view.interviewChoices[i].gameObject.SetActive(exists);
            if (!exists) continue;
            var choice = node.choices[i];
            bool available = interrogation.IsChoiceAvailable(choice) && (choice.nextNodeId != "confronto" || state.Has("pistola"));
            view.interviewChoiceLabels[i].text = available ? choice.choiceText : "Confrontar • requer escala e pistola";
            view.interviewChoices[i].interactable = available;
        }
    }

    public void SelectInterviewChoice(int index)
    {
        if (state == null || paused || interrogation.CurrentNode == null) return;
        var choices = interrogation.CurrentNode.choices;
        if (index < 0 || index >= choices.Count) return;
        var choice = choices[index];
        if (!interrogation.IsChoiceAvailable(choice) || (choice.nextNodeId == "confronto" && !state.Has("pistola"))) return;
        state.idleTime = 0;
        if (!state.questions.Contains(choice.choiceText)) state.questions.Add(choice.choiceText);
        if (choice.nextNodeId == "confronto") Collect("depoimento");
        interrogation.SelectChoice(choice);
        if (string.IsNullOrEmpty(choice.nextNodeId)) { state.interviewNode = "start"; CloseDocument(); }
        Save(false);
    }

    void RefreshPhone()
    {
        bool active = phone.CurrentCall != null;
        view.answerPhoneButton.gameObject.SetActive(state.callPending && !state.callRead && !active);
        view.endPhoneButton.gameObject.SetActive(active);
        if (state.callRead || active)
        {
            view.phoneState.text = active ? "EM LINHA • LABORATÓRIO DE PERÍCIA" : "LAUDO REGISTRADO";
            if (!view.phonePresentation.IsRevealing)
                view.phonePresentation.ShowImmediate(forensicCall.nodes[0].message);
        }
        else if (state.callPending)
        {
            view.phoneState.text = "CHAMADA RECEBIDA";
            view.phonePresentation.ShowImmediate("Laboratório de perícia.\nAtenda para receber o resultado do confronto balístico.");
        }
        else
        {
            view.phoneState.text = "SEM CHAMADAS PENDENTES";
            view.phonePresentation.ShowImmediate(state.callDelay >= 0
                ? "A perícia está comparando a arma e a cápsula. A ligação chega aproximadamente 30 segundos após a coleta."
                : "Recolha a pistola e a cápsula no bar para solicitar a análise.");
        }
    }

    public void AnswerPhone() { if (state != null && !paused) phone.AnswerPendingCall(); }
    public void EndPhone() { if (state != null && !paused && phone.CurrentCall != null && !view.phonePresentation.IsRevealing) phone.EndCall(); }

    void CallUpdated(PhoneCallAsset call, PhoneDialogueNode node)
    {
        if (state == null || restoring) return;
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

    public void SelectSuspect(int index) { if (state == null || index < 0 || index > 2) return; state.suspect = index; RefreshDeduction(); }
    public void SelectWeapon(int index) { if (state == null || index < 0 || index > 2) return; state.weapon = index; RefreshDeduction(); }
    public void SelectPlace(int index) { if (state == null || index < 0 || index > 2) return; state.place = index; RefreshDeduction(); }

    void RefreshDeduction()
    {
        view.ShowSelection(view.suspectChecks, state.suspect);
        view.ShowSelection(view.weaponChecks, state.weapon);
        view.ShowSelection(view.placeChecks, state.place);
        view.concludeButton.interactable = state.CanConclude && !state.Finished;
        view.deductionHint.text = state.Finished ? "Caso já encerrado. Consulte o resultado ou reinicie em Opções."
            : state.CanConclude ? $"Evidências suficientes. Restam {state.AttemptsRemaining} tentativa(s) de acusação."
            : "Reúna arma, cápsula, escala, vestígios e depoimento confrontado.";
    }

    public void ConcludeCase()
    {
        if (state == null || paused || state.Finished || !state.CanConclude || confirming) return;
        ShowSlotConfirmation(false);
        accusationConfirmation = true;
        view.documentTitle.text = "CONFIRMAR ACUSAÇÃO";
        view.confirmationButtonLabel.text = "Confirmar acusação";
        view.confirmationMessage.text = $"Você tem {state.AttemptsRemaining} tentativa(s).\n\nUma acusação incorreta consome uma tentativa. Após três erros, a investigação termina em derrota.\n\nConfirmar a combinação escolhida?";
    }

    void AccusationConfirmed()
    {
        accusationConfirmation = false;
        confirming = false;
        paused = false;
        bool solved = state.Accuse();
        OpenPage(state.Finished ? "Resultado" : "Dedução");
        Save(false);
        Say(solved ? "Caso encerrado. Sua avaliação foi salva." : state.failed ? "As tentativas acabaram. Você pode tentar novamente." : $"Acusação incorreta. Restam {state.AttemptsRemaining} tentativa(s). Revise as provas.");
    }

    public void ConfirmRestart()
    {
        if (state != null) selectedSlot = activeSlot;
        ShowSlotConfirmation(false);
    }

    void ShowSlotConfirmation(bool saveCopy)
    {
        accusationConfirmation = false;
        deleteConfirmation = false;
        confirming = true;
        saveCopyConfirmation = saveCopy;
        confirmationSlot = selectedSlot;
        returnPage = state == null ? "Introdução" : state.page;
        paused = true;
        view.ShowPage("Confirmar reinício", false);
        view.documentTitle.text = "CONFIRMAR SUBSTITUIÇÃO";
        view.confirmationButtonLabel.text = "Confirmar substituição";
        view.confirmationMessage.text = saveCopy
            ? $"Salvar a investigação atual no SLOT {confirmationSlot} substitui o progresso que já está nele.\n\nDepois de salvar, o automático continuará nesse slot. Deseja continuar?"
            : $"Iniciar um novo caso no SLOT {confirmationSlot} substitui o progresso que já está nele.\n\nOs outros slots permanecem intactos. Deseja continuar?";
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
        CancelRestart();
        if (saveCopy) SaveToSlot(slot);
        else StartInSlot(slot);
    }

    public void ConfirmDeleteSave()
    {
        if (confirming || !CaseZeroSave.ExistsInSlot(selectedSlot)) return;
        ShowSlotConfirmation(false);
        deleteConfirmation = true;
        view.documentTitle.text = "APAGAR SAVE";
        view.confirmationButtonLabel.text = "Apagar definitivamente";
        view.confirmationMessage.text = $"Apagar o SLOT {confirmationSlot} e suas cópias de segurança? Essa ação não pode ser desfeita.\n\n"
            + (state != null && confirmationSlot == activeSlot
                ? "A investigação atual será encerrada e você voltará à introdução."
                : "Os outros slots e a investigação atual permanecem intactos.");
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
            view.progress.text = "PROVAS 0/5     TEMPO 00:00     ERROS 0";
            view.phoneNotification.text = "Inicie ou carregue uma investigação.";
        }
        CancelRestart();
        RefreshSlots();
        Say($"Slot {slot} apagado, incluindo as cópias de segurança.");
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
        Say($"Investigação salva no slot {slot}. Automático vinculado a esse slot.");
    }

    void Save(bool manual)
    {
        if (state == null || restoring) return;
        saveTimer = 0;
        if (!CaseZeroSave.Write(state, out string error, activeSlot)) Say(error);
        else if (manual) Say($"Investigação salva no slot {activeSlot}.");
    }

    public void LoadGame()
    {
        if (confirming) return;
        var loaded = CaseZeroSave.Read(out string notice, selectedSlot);
        if (loaded == null) { Say(notice); return; }
        activeSlot = selectedSlot;
        Begin(loaded);
        RefreshSlots();
        Say(notice ?? $"Investigação restaurada do slot {activeSlot}.");
    }

    void OnApplicationPause(bool value) { if (value) Save(false); }
    void OnApplicationQuit() => Save(false);
    void OnDestroy()
    {
        if (phone != null) { phone.OnCallScreenUpdated -= CallUpdated; phone.OnCallEnded -= CallEnded; }
        if (interrogation != null) interrogation.OnInterrogationUpdated -= InterviewUpdated;
        if (forensicCall != null) Destroy(forensicCall);
        if (suspectAsset != null) Destroy(suspectAsset);
    }
    void Say(string message) { if (view != null) view.status.text = message; }
    static string TimeText(float time) => $"{(int)time / 60:00}:{(int)time % 60:00}";
}
