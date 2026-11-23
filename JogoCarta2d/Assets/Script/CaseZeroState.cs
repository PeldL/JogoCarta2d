using System;
using System.Collections.Generic;

// Plain data: the investigation can be saved independently of scene objects.
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
    public string page = "Local";
    public bool documentOpen;
    public bool cameraMode;
    public string location = "Mapa";
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
        if (!solved || CaseIndex < 0 || CaseIndex >= 2) return null;
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
            if (CaseIndex > 0)
            {
                if (callPending && !callRead) return "Answer the call from forensics on your phone.";
                if (CanRequestAnalysis && callDelay < 0) return "Submit the requested material through your phone. Results will be available at the police station.";
                string task = NextInvestigationTask();
                if (callDelay >= 0) return "Forensics is analyzing the material. Meanwhile: " + task;
                return task;
            }
            if (callPending && !callRead) return "Answer the call from forensics on your phone.";
            if (!Has("registro") || !Has("pistola") || !Has("capsula"))
                return location == "Bar" ? "Examine the objects in the bar and collect evidence." : "Travel to the bar using the map and examine the objects.";
            // Ballistics first: the report is what breaks the man's story.
            if (!callRead)
            {
                if (callDelay < 0) return "Send the pistol and the cartridge case to forensics through your phone.";
                return Has("sangue")
                    ? "Forensics is comparing the pistol and the cartridge case. Meanwhile, question the man behind the counter."
                    : "Forensics is comparing the pistol and the cartridge case. Meanwhile, examine the traces at the scene.";
            }
            if (!Has("sangue")) return "Examine the traces at the scene.";
            if (!Has("depoimento")) return "In Interviews, challenge the man behind the counter with the records and the ballistic report.";
            return "Open Deduction. Weigh every option against the evidence before you accuse.";
        }
    }
    public bool HasSceneEvidence
    {
        get
        {
            if (CaseIndex == 0) return Has("pistola") && Has("capsula") && Has("registro") && Has("sangue");
            foreach (var key in Definition.evidenceKeys) if (!Has(key)) return false;
            return true;
        }
    }
    public bool HasAllCaseEvidence => HasSceneEvidence;
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

    /// <summary>The next step that actually moves a later case forward, naming what is missing and where.</summary>
    string NextInvestigationTask()
    {
        var data = Definition;
        var reports = new List<string>();
        for (int i = 0; i < data.evidenceKeys.Length; i++)
        {
            string key = data.evidenceKeys[i];
            if (!Has(key) && data.UnlockStage(key) > 0 && Unlocked(key)) reports.Add(key);
        }
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
        if (CanConclude) return "Open Deduction and confirm the combination supported by the evidence.";
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
            if (CaseIndex == 0) return Has("pistola") && Has("capsula");
            if (analysisStage < 0 || analysisStage >= Definition.analysisPrerequisites.Length) return false;
            foreach (var key in Definition.analysisPrerequisites[analysisStage])
                if (!Has(key)) return false;
            return true;
        }
    }
    public bool CanConclude
    {
        get
        {
            // Only the key clues are required; optional clues and witnesses add context.
            return Has("depoimento") && callRead && HasKeyEvidence && analysisStage >= Definition.analysisCount;
        }
    }

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

    /// <summary>Clamps interview data from saves made before a case's cast changed,
    /// so an older save is restarted by the game instead of being rejected.</summary>
    public void Repair()
    {
        if (CaseIndex < 0) return;
        int people = Definition.interviewNames.Length;
        if (interviewSuspect < 0 || interviewSuspect >= people) { interviewSuspect = 0; interviewNode = "start"; }
        interviewedMask &= (1 << people) - 1;
        if (suspect >= Definition.suspects.Length) suspect = -1;
    }

    public bool IsValid()
    {
        return version == 1 && CaseIndex >= 0 && evidence != null && photographs != null && questions != null
            && !float.IsNaN(elapsed) && !float.IsInfinity(elapsed) && elapsed >= 0 && errors >= 0
            && !float.IsNaN(callDelay) && !float.IsInfinity(callDelay) && callDelay >= -1
            && !float.IsNaN(idleTime) && !float.IsInfinity(idleTime) && idleTime >= 0
            && suspect >= -1 && suspect < Definition.suspects.Length && weapon >= -1 && weapon < 3 && place >= -1 && place < 3
            && interviewSuspect >= 0 && interviewSuspect < Definition.interviewNames.Length
            && interviewedMask >= 0 && interviewedMask < (1 << Definition.interviewNames.Length)
            && analysisStage >= 0 && analysisStage <= Definition.analysisCount
            && pendingAnalysisStage >= 0 && pendingAnalysisStage < Math.Max(1, Definition.analysisCount)
            && completedCases >= 0 && completedCases <= 3;
    }
}

[Serializable]
public sealed class CaseZeroResult
{
    public string caseId;
    public int score, errors, evidenceCount;
    public float elapsed;
}
