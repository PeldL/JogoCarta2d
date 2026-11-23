using System;
using System.Collections.Generic;

// Plain data: the investigation can be saved independently of scene objects.
// Every rule here reads the case's data, so all cases follow the same flow.
[Serializable]
public sealed class CaseZeroState
{
    public int version = 1;
    public string caseId = "ultima-testemunha";
    public List<string> evidence = new List<string>();
    public List<string> photographs = new List<string>();
    public List<string> questions = new List<string>();
    public float elapsed;
    public float idleTime;
    public float callDelay = -1;
    public bool callPending;
    public bool callRead;
    public bool idleHintShown;
    public bool solved;
    public bool failed;
    public int errors;
    // -1 = not chosen yet. A new case starts with nothing selected in Deduction.
    public int suspect = -1;
    public int weapon = -1;
    public int place = -1;
    public string page = CaseZeroPages.LegacyScene;
    public bool documentOpen;
    public bool cameraMode;
    public string location = CaseZeroPages.Map;
    public string interviewNode = "start";
    public int interviewSuspect;
    public int interviewedMask;
    public int analysisStage;
    public int pendingAnalysisStage;
    public int completedCases;
    public bool introductionSeen;
    public int tutorialHints;
    public List<CaseZeroResult> results = new List<CaseZeroResult>();
    public int CaseIndex => CaseZeroCases.Index(caseId);
    public CaseZeroCase Definition => CaseZeroCases.Get(caseId);
    public bool AllInterviewed
    {
        get
        {
            int required = (1 << Definition.interviewNames.Length) - 1;
            return (interviewedMask & required) == required;
        }
    }
    // A retry keeps the briefing and tutorial already seen, so the radio does not replay.
    public CaseZeroState RestartCase() => new CaseZeroState { caseId = caseId, location = Definition.location, completedCases = completedCases, results = PreviousResults(), introductionSeen = introductionSeen, tutorialHints = tutorialHints };
    List<CaseZeroResult> PreviousResults() => (results ?? new List<CaseZeroResult>()).FindAll(r => r != null && r.caseId != caseId);
    public CaseZeroState NextCase()
    {
        if (!solved || CaseIndex < 0 || CaseIndex >= CaseZeroCases.All.Length - 1) return null;
        var next = CaseZeroCases.All[CaseIndex + 1];
        RecordResult();
        return new CaseZeroState { caseId = next.id, location = next.location, completedCases = CaseIndex + 1, results = new List<CaseZeroResult>(results) };
    }

    public bool Has(string id) => evidence.Contains(id);
    /// <summary>The player has picked a culprit, a weapon and a location.</summary>
    public bool HasTheory => suspect >= 0 && weapon >= 0 && place >= 0;
    public bool Finished => solved || failed;
    public int AttemptsRemaining => Math.Max(0, 3 - errors);

    public string Objective
    {
        get
        {
            if (solved) return "Case solved. View your evaluation.";
            if (failed) return "Investigation over. Try again from the results screen.";
            if (callPending && !callRead) return "Answer the call from forensics on your phone.";
            if (CanRequestAnalysis && callDelay < 0)
            {
                var material = new List<string>(Definition.analysisPrerequisites[analysisStage]);
                material.Remove("depoimento");
                string where = Definition.ReportsLocation;
                return (material.Count > 0 ? $"Send {Names(material)} to forensics through your phone." : "Request the next forensic analysis through your phone.")
                    + (where != null ? $" Results will be ready at the {CaseZeroView.DisplayName(where)}." : "");
            }
            string task = NextInvestigationTask();
            return callDelay >= 0 ? "Forensics is analyzing the material. Meanwhile: " + task : task;
        }
    }

    // Kept for the editor campaign checks; the required clues are the case's key evidence.
    public bool HasSceneEvidence => HasKeyEvidence;
    bool Unlocked(string key) => analysisStage >= Definition.UnlockStage(key);
    public bool HasKeyEvidence => MissingKeys(Definition.keyEvidence, false).Count == 0;
    public bool CanConfront => MissingKeys(Definition.confrontRequirements, false).Count == 0
        && (!Definition.confrontNeedsReport || callRead);

    /// <summary>Clues from the list not yet recorded; optionally only those already examinable.</summary>
    public List<string> MissingKeys(string[] keys, bool unlockedOnly)
    {
        var missing = new List<string>();
        if (keys == null) return missing;
        foreach (var key in keys)
            if (!Has(key) && (!unlockedOnly || Unlocked(key))) missing.Add(key);
        return missing;
    }

    string Names(List<string> keys) => string.Join(", ", keys.ConvertAll(Definition.EvidenceName));

    string Places(List<string> keys)
    {
        var places = new List<string>();
        foreach (var key in keys)
        {
            int index = Definition.EvidenceIndex(key);
            if (index < 0) continue;
            string place = CaseZeroView.DisplayName(Definition.evidenceLocations[index]);
            if (!places.Contains(place)) places.Add(place);
        }
        return string.Join(" and ", places);
    }

    string PendingInterviews()
    {
        var pending = new List<string>();
        for (int i = 0; i < Definition.interviewNames.Length; i++)
            if ((interviewedMask & (1 << i)) == 0) pending.Add(Definition.interviewNames[i]);
        return string.Join(", ", pending);
    }

    /// <summary>The next step that actually moves the case forward, naming what is missing and where.</summary>
    string NextInvestigationTask()
    {
        var data = Definition;
        var reports = new List<string>();
        foreach (var key in data.evidenceKeys)
            if (!Has(key) && data.UnlockStage(key) > 0 && Unlocked(key)) reports.Add(key);
        if (reports.Count > 0) return $"New forensic results are ready. Collect them at the {Places(reports)}.";

        if (analysisStage < data.analysisCount && analysisStage < data.analysisPrerequisites.Length)
        {
            var material = MissingKeys(data.analysisPrerequisites[analysisStage], true);
            material.Remove("depoimento");
            if (material.Count > 0) return $"Forensics needs more material. Examine {Names(material)} at the {Places(material)}.";
        }

        if (!Has("depoimento"))
        {
            var support = MissingKeys(data.confrontRequirements, true);
            if (support.Count > 0) return $"Find evidence to challenge the statements. Examine {Names(support)} at the {Places(support)}.";
            if (CanConfront)
                return AllInterviewed
                    ? "You have enough evidence. In Interviews, challenge the account that the evidence contradicts."
                    : "Interview " + PendingInterviews() + " and look for the account the evidence contradicts.";
        }

        var remaining = MissingKeys(data.keyEvidence, true);
        if (remaining.Count > 0) return $"Examine {Names(remaining)} at the {Places(remaining)}.";
        if (CanConclude) return "Open Deduction. Weigh every option against the evidence before you accuse.";
        if (!AllInterviewed) return "Interview " + PendingInterviews() + " and compare their accounts with the evidence.";
        return "Review the evidence and compare it with each account. Forensics will need more material to continue.";
    }

    /// <summary>What still blocks Deduction, one item per line, without revealing locked clues.</summary>
    public List<string> MissingForConclusion()
    {
        var missing = new List<string>();
        var available = MissingKeys(Definition.keyEvidence, true);
        if (available.Count > 0) missing.Add("Evidence: " + Names(available) + " (" + Places(available) + ")");
        int locked = MissingKeys(Definition.keyEvidence, false).Count - available.Count;
        if (locked > 0) missing.Add($"Forensic results not yet available: {locked}");
        if (!Has("depoimento")) missing.Add("Challenge the account that the evidence contradicts");
        if (callPending && !callRead) missing.Add("Answer the forensics call");
        else if (!callRead || analysisStage < Definition.analysisCount) missing.Add("Request and record the forensic report by phone");
        return missing;
    }

    public bool CanRequestAnalysis
    {
        get
        {
            var prerequisites = Definition.analysisPrerequisites;
            if (prerequisites == null || analysisStage < 0 || analysisStage >= Math.Min(Definition.analysisCount, prerequisites.Length)) return false;
            foreach (var key in prerequisites[analysisStage])
                if (!Has(key)) return false;
            return true;
        }
    }

    // Only the key clues are required; optional clues and witnesses add context.
    public bool CanConclude => Has("depoimento") && callRead && HasKeyEvidence && analysisStage >= Definition.analysisCount;

    public bool Accuse()
    {
        if (Finished || !CanConclude) return solved;
        solved = suspect == Definition.culprit && weapon == Definition.weaponSolution && place == Definition.placeSolution;
        if (solved) { completedCases = Math.Max(completedCases, CaseIndex + 1); RecordResult(); }
        if (!solved) { errors++; failed = errors >= 3; }
        return solved;
    }

    public int Score => Math.Max(0, 1000 - errors * 100 - (int)(Math.Min(1, Math.Max(0, (elapsed - 300) / 600)) * 500));
    public string Rating => Score >= 900 ? "Legendary detective" : Score >= 700 ? "Exemplary detective" : Score >= 500 ? "Skilled detective" : "Case solved";

    public void RecordResult()
    {
        if (!solved) return;
        if (results == null) results = new List<CaseZeroResult>();
        results.RemoveAll(r => r == null || r.caseId == caseId);
        results.Add(new CaseZeroResult { caseId = caseId, score = Score, elapsed = elapsed, errors = errors, evidenceCount = evidence.Count });
    }

    /// <summary>Brings a save made by an older version in line with the current case data,
    /// so it is loaded (or restarted by the game) instead of being rejected.</summary>
    public void Repair()
    {
        if (CaseIndex < 0) return;
        var data = Definition;
        int people = data.interviewNames.Length;
        if (interviewSuspect < 0 || interviewSuspect >= people) { interviewSuspect = 0; interviewNode = "start"; }
        interviewedMask &= (1 << people) - 1;
        if (suspect >= data.suspects.Length) suspect = -1;
        if (weapon >= data.objects.Length) weapon = -1;
        if (place >= data.places.Length) place = -1;
        // The first case used to record its report without advancing the analysis stage.
        if (callRead && !callPending && callDelay < 0 && analysisStage <= pendingAnalysisStage && analysisStage < data.analysisCount)
            analysisStage = Math.Min(data.analysisCount, pendingAnalysisStage + 1);
    }

    public bool IsValid()
    {
        if (version != 1 || CaseIndex < 0) return false;
        var data = Definition;
        return evidence != null && photographs != null && questions != null
            && !float.IsNaN(elapsed) && !float.IsInfinity(elapsed) && elapsed >= 0 && errors >= 0
            && !float.IsNaN(callDelay) && !float.IsInfinity(callDelay) && callDelay >= -1
            && !float.IsNaN(idleTime) && !float.IsInfinity(idleTime) && idleTime >= 0
            && suspect >= -1 && suspect < data.suspects.Length
            && weapon >= -1 && weapon < data.objects.Length && place >= -1 && place < data.places.Length
            && interviewSuspect >= 0 && interviewSuspect < data.interviewNames.Length
            && interviewedMask >= 0 && interviewedMask < (1 << data.interviewNames.Length)
            && analysisStage >= 0 && analysisStage <= data.analysisCount
            && pendingAnalysisStage >= 0 && pendingAnalysisStage < Math.Max(1, data.analysisCount)
            && completedCases >= 0 && completedCases <= CaseZeroCases.All.Length;
    }
}

[Serializable]
public sealed class CaseZeroResult
{
    public string caseId;
    public int score, errors, evidenceCount;
    public float elapsed;
}
