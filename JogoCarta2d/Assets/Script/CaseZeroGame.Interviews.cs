using System.Collections.Generic;
using UnityEngine;
using Pages = CaseZeroPages;

// Conversations with the people in a case and the challenge that produces the testimony.
// The dialogue itself is data: CaseZeroCase.interviews.
public sealed partial class CaseZeroGame
{
    const string ChallengeNode = "confronto";
    const string Testimony = "depoimento";

    readonly List<SuspectAsset> caseSuspects = new List<SuspectAsset>();
    SuspectAsset suspectAsset;
    // After the first exchange, returning to the opening node gets a short prompt
    // instead of the person repeating their whole opening statement.
    bool interviewResumed;
    // The challenge produced the testimony: show it in Evidence when the player leaves the room.
    bool showTestimonyOnExit;

    void CreateInterviews()
    {
        var data = CurrentCase;
        for (int i = 0; i < data.interviewNames.Length; i++)
        {
            var asset = ScriptableObject.CreateInstance<SuspectAsset>();
            asset.suspectName = data.interviewNames[i];
            asset.nodes = BuildInterviewNodes(data, i);
            caseSuspects.Add(asset);
        }
        int person = state == null ? 0 : Mathf.Clamp(state.interviewSuspect, 0, caseSuspects.Count - 1);
        suspectAsset = caseSuspects[person];
    }

    List<InterrogationNode> BuildInterviewNodes(CaseZeroCase data, int person)
    {
        var nodes = new List<InterrogationNode>();
        var talks = data.interviews != null && person < data.interviews.Length ? data.interviews[person] : null;
        if (talks == null || talks.Length == 0)
        {
            nodes.Add(Node("start", "I have nothing to add.", Choice("End conversation", "")));
            return nodes;
        }
        foreach (var talk in talks)
        {
            var choices = new List<InterrogationChoice>();
            foreach (var ask in talk.asks)
                choices.Add(Choice(ask.text, ask.next,
                    !string.IsNullOrEmpty(ask.requires) && catalog.TryGetValue(ask.requires, out var clue) ? clue : null));
            nodes.Add(new InterrogationNode { nodeId = talk.id, message = talk.text, choices = choices });
        }
        return nodes;
    }

    static InterrogationChoice Choice(string text, string next, EvidenceData required = null) => new InterrogationChoice { choiceText = text, nextNodeId = next, requiredEvidence = required };
    static InterrogationNode Node(string id, string message, params InterrogationChoice[] choices) => new InterrogationNode { nodeId = id, message = message, choices = new List<InterrogationChoice>(choices) };

    /// <summary>Opens the Interviews page on this person (old scene bindings and the
    /// person marker in the scene). Questioning starts from the page's Interview button.</summary>
    public void InterviewSuspect(int index)
    {
        if (state == null || state.Finished || confirming || travelling || index < 0 || index >= caseSuspects.Count) return;
        state.interviewSuspect = index;
        state.interviewNode = "start";
        suspectAsset = caseSuspects[index];
        OpenPage(Pages.Interviews);
        Save(false);
    }

    // The Interviews page picks who goes to the interrogation room.
    void ShowInterview()
    {
        var picker = CaseZeroInterviewPicker.Attach(view, StepInterviewee, () => OpenInterrogationRoom(state.interviewSuspect));
        view.ShowPortraits(CurrentCase, state.interviewSuspect);
        int person = state.interviewSuspect;
        picker?.Refresh(CurrentCase.interviewNames.Length, (state.interviewedMask & (1 << person)) != 0, (state.rorschachMask & (1 << person)) != 0);
        Save(false);
    }

    void StepInterviewee(int direction)
    {
        if (state == null || caseSuspects.Count < 2) return;
        int count = caseSuspects.Count;
        state.interviewSuspect = ((state.interviewSuspect + direction) % count + count) % count;
        suspectAsset = caseSuspects[state.interviewSuspect];
        state.interviewNode = "start";
        view.SlideNextPortraitFrom(new Vector2(90 * direction, 0));
        CaseZeroSoundFeedback.Current?.PlayClick();
        ShowInterview();
    }

    void InterviewUpdated(SuspectAsset suspect, InterrogationNode node)
    {
        if (state == null || suspect != suspectAsset || restoring) return;
        state.interviewNode = node.nodeId;
        if (RoomOpen) RenderRoomInterview(node);
    }

    bool IsChoiceAvailable(InterrogationChoice choice) =>
        interrogation.IsChoiceAvailable(choice) && (choice.nextNodeId != ChallengeNode || state.CanConfront);

    // Names the clue that unlocks a question; clues still behind forensics stay unnamed.
    string LockedChoiceText(InterrogationChoice choice)
    {
        var needed = new List<string>();
        if (choice.requiredEvidence != null && !string.IsNullOrEmpty(choice.requiredEvidence.id) && !state.Has(choice.requiredEvidence.id))
            needed.Add(choice.requiredEvidence.id);
        bool challenge = choice.nextNodeId == ChallengeNode;
        if (challenge)
            foreach (var key in state.MissingKeys(CurrentCase.confrontRequirements, false))
                if (!needed.Contains(key)) needed.Add(key);
        var named = new List<string>();
        bool forensic = false;
        foreach (var key in needed)
        {
            if (state.analysisStage >= CurrentCase.UnlockStage(key)) named.Add(CurrentCase.EvidenceName(key));
            else forensic = true;
        }
        if (forensic) named.Add("a forensic result");
        if (challenge && CurrentCase.confrontNeedsReport && !state.callRead) named.Add("the forensic report");
        string prefix = challenge ? "Challenge" : "Question";
        // The required evidence is no longer spelled out: the player works out what unlocks it.
        return prefix + " • locked";
    }

    public void SelectInterviewChoice(int index)
    {
        if (state == null || paused || interrogation.CurrentNode == null) return;
        var choices = interrogation.CurrentNode.choices;
        if (index < 0 || index >= choices.Count) return;
        var choice = choices[index];
        if (!IsChoiceAvailable(choice)) return;
        state.idleTime = 0;
        if (!state.questions.Contains(choice.choiceText)) state.questions.Add(choice.choiceText);
        if (choice.nextNodeId == ChallengeNode && state.interviewSuspect == CurrentCase.confrontInterview && !state.Has(Testimony))
        {
            Collect(Testimony);
            showTestimonyOnExit = true;
        }
        interviewResumed = true;
        interrogation.SelectChoice(choice);
        if (string.IsNullOrEmpty(choice.nextNodeId))
        {
            // Ending a conversation returns to the room's menu (question, Rorschach test or leave).
            state.interviewNode = "start";
            interrogation.EndInterrogation();
            if (RoomOpen) room.ShowMenu();
        }
        Save(false);
    }
}
