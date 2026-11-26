using System.Collections.Generic;
using UnityEngine;
using Pages = CaseZeroPages;

// Forensic requests: the player sends material by phone, forensics calls back a few
// seconds later, and ending the call records the report and advances the analysis stage.
public sealed partial class CaseZeroGame
{
    const float ForensicsDelay = 8;
    const string ForensicsCaller = "Forensics laboratory";
    PhoneCallAsset forensicCall;

    void CreateForensicCall()
    {
        forensicCall = ScriptableObject.CreateInstance<PhoneCallAsset>();
        forensicCall.callerName = ForensicsCaller;
        forensicCall.nodes = new List<PhoneDialogueNode> {
            new PhoneDialogueNode { nodeId = "start", message = CurrentReport(), choices = new List<PhoneDialogueChoice> { new PhoneDialogueChoice { choiceText = "Record report and end call" } } }
        };
    }

    /// <summary>The report for the analysis that was requested last (pending or already recorded).</summary>
    string CurrentReport()
    {
        var messages = CurrentCase.analysisMessages;
        bool hasReport = state != null && (state.callPending || state.callRead || state.callDelay >= 0) && messages != null && messages.Length > 0;
        return hasReport
            ? messages[Mathf.Clamp(state.pendingAnalysisStage, 0, messages.Length - 1)]
            : "No report is pending. Submit a collected item through the phone to request a forensic result.";
    }

    /// <summary>Counts down a submitted request and makes the phone ring when it ends.</summary>
    void UpdateForensicsTimer()
    {
        if (state.callDelay < 0 || state.callRead || state.callPending) return;
        state.callDelay -= Time.unscaledDeltaTime;
        if (state.callDelay > 0) return;
        state.callDelay = -1;
        state.callPending = true;
        phone.QueueCall(forensicCall);
        Say("Forensics is calling. Open the phone to answer.");
        if (view.IsDocumentOpen && state.page == Pages.Phone) RefreshPhone();
        Save(false);
    }

    void ShowPhone()
    {
        RefreshPhone();
        // Opening the phone while it rings answers the call: one click less.
        if (!paused && state.callPending && !state.callRead && phone.CurrentCall == null && phone.HasPendingCall)
            phone.AnswerPendingCall();
    }

    void RefreshPhone()
    {
        bool active = phone.CurrentCall != null;
        bool canRequest = state.CanRequestAnalysis;
        if (view.sendAnalysisButton != null) view.sendAnalysisButton.interactable = !state.Finished && canRequest && !active && !state.callPending && state.callDelay < 0;
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
            string where = CurrentCase.ReportsLocation;
            view.phoneState.text = "NO PENDING CALLS";
            view.phonePresentation.ShowImmediate(state.callDelay >= 0
                ? "Material submitted. Forensics is preparing its response. Keep investigating; the phone will alert you when the call arrives."
                : canRequest ? "You can request the next forensic examination. Results arrive by phone"
                    + (where != null ? " and are collected at the " + CaseZeroView.DisplayName(where) + "." : ".")
                : "DISPATCH AVAILABLE\nRead the documents or ask for guidance. Collect material at the scene to request a forensic examination.");
        }
    }

    public void SendForAnalysis()
    {
        if (state == null || state.Finished || !state.CanRequestAnalysis || state.callPending || state.callDelay >= 0 || phone.CurrentCall != null) return;
        state.pendingAnalysisStage = state.analysisStage;
        state.callRead = false;
        state.callDelay = ForensicsDelay;
        forensicCall.callerName = ForensicsCaller;
        forensicCall.nodes[0].message = CurrentReport();
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
        view.phoneState.text = "CONNECTED • CENTRAL DISPATCH";
        view.phonePresentation.PlayMessage("Dispatch: Detective, do you read me?\n\nYour current objective is: " + state.Objective + "\n\nGo over the recorded evidence and compare it with each account. Look for details that support or contradict what you have been told. Call me again if you need another briefing.");
    }

    public void OpenCaseDocuments()
    {
        if (!CanConsultPhone()) return;
        view.phoneState.text = "SHIFT DOCUMENTS";
        view.phonePresentation.ShowImmediate(CurrentCase.introduction
            + "\n\nReference location: " + CaseZeroView.DisplayName(CurrentCase.location) + ".\n\nOpen the case file with Tab or the on-screen button. Map: visit locations. Evidence: read collected records. Interviews: pick a person and question them in the interrogation room. Deduction: present your conclusion after recording the report.");
    }

    public void OpenAnalysisReport()
    {
        if (!CanConsultPhone()) return;
        if (state.callRead) { view.phoneState.text = "FORENSIC REPORT RECORDED"; view.phonePresentation.ShowImmediate(forensicCall.nodes[0].message); }
        else RefreshPhone();
    }

    public void AnswerPhone() { if (state != null && !paused) phone.AnswerPendingCall(); }

    public void EndPhone()
    {
        if (state == null || paused || phone.CurrentCall == null) return;
        // First click during the typing animation shows the whole report; the next one ends the call.
        if (view.phonePresentation.IsRevealing) { view.phonePresentation.Complete(); return; }
        phone.EndCall();
    }

    void CallUpdated(PhoneCallAsset call, PhoneDialogueNode node)
    {
        if (state == null || restoring) return;
        view.Feedback?.SetPhoneRinging(false);
        OpenPage(Pages.Phone);
        view.phonePresentation.PlayMessage(node.message);
    }

    void FileForensicReports()
    {
        var names = new List<string>();
        foreach (var key in CurrentCase.evidenceKeys)
        {
            int stage = CurrentCase.UnlockStage(key);
            if (stage <= 0 || stage > state.analysisStage || state.evidence.Contains(key)) continue;
            state.evidence.Add(key);
            names.Add(catalog[key].evidenceName);
        }
        if (names.Count == 0) return;
        view.NotifyEvidence(string.Join("\n", names));
        CaseZeroSoundFeedback.Current?.PlayEvidence();
    }

    void CallEnded()
    {
        if (state == null || restoring) return;
        state.callRead = true;
        state.callPending = false;
        // Recording a report unlocks the next stage and files its documents straight into Evidence.
        state.analysisStage = Mathf.Max(state.analysisStage, state.pendingAnalysisStage + 1);
        FileForensicReports();
        RefreshWorld();
        state.idleTime = 0;
        Save(false);
        RefreshProgress();
        if (state.page == Pages.Phone) RefreshPhone();
    }
}
