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

    public void InterviewSuspect(int index)
    {
        if (state == null || state.Finished || confirming || travelling || index < 0 || index >= caseSuspects.Count) return;
        state.interviewSuspect = index;
        state.interviewNode = "start";
        interviewResumed = false;
        interrogation.EndInterrogation();
        suspectAsset = caseSuspects[index];
        OpenPage(Pages.Interviews);
        Save(false);
    }

    void ShowInterview()
    {
        state.interviewedMask |= 1 << state.interviewSuspect;
        var names = CurrentCase.interviewNames;
        for (int i = 0; i < view.interviewSuspectButtons.Length; i++)
        {
            view.interviewSuspectButtons[i].interactable = i != state.interviewSuspect;
            if (names.Length > 1 && i < names.Length)
                view.interviewSuspectButtons[i].GetComponentInChildren<TMPro.TMP_Text>(true).text = names[i] + ((state.interviewedMask & (1 << i)) != 0 ? " • interviewed" : "");
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
        if (state.page == Pages.Interviews) RenderInterview(node);
    }

    void RenderInterview(InterrogationNode node)
    {
        if (node == null) return;
        view.ShowPortraits(CurrentCase, state.interviewSuspect);
        string message = node.nodeId == "start" && interviewResumed ? "Anything else, detective?" : node.message;
        view.interviewMessage.text = view.interviewPortraitName != null ? message : suspectAsset.suspectName + "\n\n" + message;
        var dialogueFx = view.interviewMessage.GetComponent<CaseZeroDialogueFX>();
        if (dialogueFx == null) dialogueFx = view.interviewMessage.gameObject.AddComponent<CaseZeroDialogueFX>();
        dialogueFx.RevealInterview();
        for (int i = 0; i < view.interviewChoices.Length; i++)
        {
            bool exists = i < node.choices.Count;
            view.interviewChoices[i].gameObject.SetActive(exists);
            if (!exists) continue;
            var choice = node.choices[i];
            bool available = IsChoiceAvailable(choice);
            view.interviewChoiceLabels[i].text = available ? choice.choiceText : LockedChoiceText(choice);
            view.interviewChoices[i].interactable = available;
        }
        view.AnimateInterviewChoices();
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
        return named.Count == 0 ? prefix + " • locked" : $"{prefix} • requires: {string.Join(", ", named)}";
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
        if (choice.nextNodeId == ChallengeNode && state.interviewSuspect == CurrentCase.confrontInterview) Collect(Testimony);
        bool leavingChallenge = interrogation.CurrentNode.nodeId == ChallengeNode;
        interviewResumed = true;
        interrogation.SelectChoice(choice);
        if (string.IsNullOrEmpty(choice.nextNodeId))
        {
            // Ending a conversation keeps the file open: a fresh statement goes to Evidence,
            // otherwise the player stays in Interviews to pick someone else.
            state.interviewNode = "start";
            if (leavingChallenge && state.Has(Testimony)) SelectEvidence(Testimony);
            else
            {
                interrogation.StartInterrogation(suspectAsset);
                if (CurrentCase.interviewNames.Length > 1) Say("Ask something else or choose another person to talk to.");
            }
        }
        Save(false);
    }
}
