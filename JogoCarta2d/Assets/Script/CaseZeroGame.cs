using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Pages = CaseZeroPages;

// Runs the investigation in Game.unity. The class is split into partial files by subject:
//   CaseZeroGame.cs             state, start-up, loading a case, per-frame update
//   CaseZeroGame.Navigation.cs  case-file pages, map travel, presentations, results
//   CaseZeroGame.Evidence.cs    case content, scene markers, evidence board
//   CaseZeroGame.Interviews.cs  conversations and the challenge
//   CaseZeroGame.Phone.cs       forensic requests and calls
//   CaseZeroGame.Deduction.cs   the accusation
//   CaseZeroGame.Saves.cs       slots, confirmations, save and load
// Public methods are Button.OnClick targets in the scene: keep their names.
[DefaultExecutionOrder(100)]
public sealed partial class CaseZeroGame : MonoBehaviour
{
    [Header("Interface montada na cena")]
    [SerializeField] CaseZeroView view;
    [SerializeField] float autosaveInterval = 60;
    [SerializeField] float inactivityHintDelay = 120;

    CaseZeroState state;
    PhoneCallManager phone;
    InterrogationManager interrogation;
    CaseZeroCase CurrentCase => state == null ? CaseZeroCases.All[0] : state.Definition;
    float saveTimer;
    bool paused;
    bool restoring;
    bool startupInitialized;

    public CaseZeroState CampaignState => state;
    public bool Presenting => presenting;
    public bool ReadyForReveal => startupInitialized && !travelling &&
        (!presenting || SceneManager.GetSceneByName("ApresentacaoCaso").isLoaded);

    void Start()
    {
        phone = PhoneCallManager.Instance;
        interrogation = InterrogationManager.Instance;
        if (view == null || phone == null || interrogation == null)
        {
            Debug.LogError("[Caso Zero] Vincule a interface, o PhoneCallManager e o InterrogationManager na cena.");
            enabled = false;
            return;
        }
        // Question requirements read the saved investigation: one source of truth for evidence.
        interrogation.HasEvidence = id => state != null && state.Has(id);
        WireDynamicButtons();
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

    /// <summary>Makes <paramref name="next"/> the live investigation and rebuilds the scene for it.</summary>
    void Begin(CaseZeroState next)
    {
        view.Feedback?.ResetFeedback();
        view.ClearNotice();
        restoring = true;
        view.phonePresentation.ShowImmediate("");
        phone.ResetCalls();
        interrogation.EndInterrogation();
        state = next;
        evidencePage = 0;
        // Saves from an older version of a case use clue keys that no longer exist: restart it.
        bool hasCurrentClue = false;
        foreach (var key in state.Definition.evidenceKeys) hasCurrentClue |= state.Has(key);
        bool hasProgress = state.evidence.Count > 0 || state.callRead || state.callPending || state.interviewedMask != 0 || state.solved || state.failed;
        if (!hasCurrentClue && hasProgress) state = state.RestartCase();

        var caseDefinition = state.Definition;
        bool savedLocationIsValid = false;
        foreach (var allowedLocation in caseDefinition.locations)
            savedLocationIsValid |= allowedLocation == NormalizeLocation(state.location);
        if (!savedLocationIsValid) state.location = caseDefinition.location;
        if (state.interviewSuspect < 0 || state.interviewSuspect >= caseDefinition.interviewNames.Length)
            state.interviewSuspect = 0;
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
        if (view.mapInDocument && NormalizeLocation(state.location) == Pages.Map) state.location = caseDefinition.location;
        state.evidence.RemoveAll(id => !catalog.ContainsKey(id));
        if (state.callPending && !state.callRead) phone.QueueCall(forensicCall);
        paused = false;
        saveTimer = 0;
        selectedEvidence = null;
        interviewResumed = false;
        restoring = false;
        RefreshWorld();
        RefreshProgress();
        if (view.backgroundMap) { StartCoroutine(ChangeLocation(NormalizeLocation(state.location), true)); return; }
        if (state.Finished) OpenPage(Pages.Result);
        else if (state.documentOpen) OpenPage(NormalizePage(state.page));
        else view.CloseDocument();
        Say("Tab opens the case file. Click evidence in the scene to investigate.");
    }

    void Update()
    {
        if (state == null || travelling || presenting) return;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame) ToggleDocument();
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (view.IsDocumentOpen) CloseDocument();
                else OpenPage(Pages.Options);
            }
        }
        BrowseEvidenceWithMouseWheel();
        if (paused || state.Finished) return;
        state.elapsed += Time.unscaledDeltaTime;
        state.idleTime += Time.unscaledDeltaTime;
        saveTimer += Time.unscaledDeltaTime;
        UpdateForensicsTimer();
        // Repeats after every stretch of inactivity, not only once per case.
        if (state.idleTime >= inactivityHintDelay)
        {
            state.idleTime = 0;
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

    void OnApplicationPause(bool value) { if (value) Save(false); }
    void OnApplicationQuit() => Save(false);
    void OnDestroy()
    {
        if (phone != null) { phone.OnCallScreenUpdated -= CallUpdated; phone.OnCallEnded -= CallEnded; }
        if (interrogation != null)
        {
            interrogation.OnInterrogationUpdated -= InterviewUpdated;
            interrogation.HasEvidence = null;
        }
        DestroyCaseAssets();
    }

    /// <summary>Control hints shown once, only in the tutorial case.</summary>
    void TutorialHint(int flag, string message)
    {
        if (state == null || !CurrentCase.tutorial || state.Finished || !state.introductionSeen || (state.tutorialHints & flag) != 0) return;
        state.tutorialHints |= flag;
        Say("Dispatch: " + message);
        Save(false);
    }
    void Say(string message) { if (view != null) view.SetStatus(message); }
    static string TimeText(float time) => $"{(int)time / 60:00}:{(int)time % 60:00}";
}
