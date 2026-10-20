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
    public int suspect;
    public int weapon;
    public int place;
    public string page = "Local";
    public bool documentOpen;
    public bool cameraMode;
    public string location = "Mapa";
    public string interviewNode = "start";
    public int interviewSuspect;
    public int interviewedMask;
    public int completedCases;
    public bool introductionSeen;
    public int tutorialHints;
    public List<CaseZeroResult> results = new List<CaseZeroResult>();
    public int CaseIndex => CaseZeroCases.Index(caseId);
    public CaseZeroCase Definition => CaseZeroCases.Get(caseId);
    public bool AllInterviewed => CaseIndex == 0 || (interviewedMask & 15) == 15;
    public CaseZeroState RestartCase() => new CaseZeroState { caseId = caseId, location = Definition.location, completedCases = completedCases, results = PreviousResults() };
    List<CaseZeroResult> PreviousResults() => (results ?? new List<CaseZeroResult>()).FindAll(r => r != null && r.caseId != caseId);
    public CaseZeroState NextCase()
    {
        if (!solved || CaseIndex < 0 || CaseIndex >= 2) return null;
        var next = CaseZeroCases.All[CaseIndex + 1];
        RecordResult();
        return new CaseZeroState { caseId = next.id, location = next.location, completedCases = CaseIndex + 1, results = new List<CaseZeroResult>(results) };
    }

    public bool Has(string id) => evidence.Contains(id);
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
                if (callPending && !callRead) return "Answer the call from dispatch on your phone.";
                if (!HasSceneEvidence)
                    return "Investigate the records and traces at " + (CaseIndex == 1 ? "Apartment 302." : "the office.");
                if (!AllInterviewed) return "Interview the four suspects in the Interviews tab.";
                if (!Has("depoimento")) return "Compare the evidence and challenge the statements.";
                if (!callRead) return callDelay >= 0 ? "Forensics is analyzing the material. Review the statements while awaiting the call." : "Submit material for analysis through your phone.";
                return "Open Deduction and confirm the combination supported by the evidence.";
            }
            if (callPending && !callRead) return "Answer the call from forensics on your phone.";
            if (!Has("registro") || !Has("pistola") || !Has("capsula"))
                return location == "Bar" ? "Examine the objects in the bar and collect evidence." : "Travel to the bar using the map and examine the objects.";
            if (!Has("sangue")) return "Examine the traces at the scene and compare them with the statements.";
            if (!Has("depoimento")) return "In Interviews, challenge the testimony with evidence.";
            if (!callRead) return callDelay >= 0 ? "Forensics is analyzing the material. Review the evidence while awaiting the call." : "Submit material for analysis through your phone.";
            return "Open Deduction and compare the culprit, weapon, and location.";
        }
    }
    public bool HasSceneEvidence => Has("pistola") && Has("capsula") && Has("registro") && Has("sangue");
    // The lab can examine suitable material while the detective keeps investigating.
    public bool CanRequestAnalysis => CaseIndex == 0 ? Has("pistola") && Has("capsula") : Has("registro") && Has("pistola");
    public bool CanConclude => HasSceneEvidence && Has("depoimento") && AllInterviewed && callRead;

    public bool Accuse()
    {
        if (Finished || !CanConclude) return solved;
        solved = suspect == Definition.culprit && weapon == 0 && place == 0;
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

    public bool IsValid()
    {
        return version == 1 && CaseIndex >= 0 && evidence != null && photographs != null && questions != null
            && !float.IsNaN(elapsed) && !float.IsInfinity(elapsed) && elapsed >= 0 && errors >= 0
            && !float.IsNaN(callDelay) && !float.IsInfinity(callDelay) && callDelay >= -1
            && !float.IsNaN(idleTime) && !float.IsInfinity(idleTime) && idleTime >= 0
            && suspect >= 0 && suspect < Definition.suspects.Length && weapon >= 0 && weapon < 3 && place >= 0 && place < 3
            && interviewSuspect >= 0 && interviewSuspect < (CaseIndex == 0 ? 1 : 4)
            && interviewedMask >= 0 && interviewedMask <= 15 && completedCases >= 0 && completedCases <= 3;
    }
}

[Serializable]
public sealed class CaseZeroResult
{
    public string caseId;
    public int score, errors, evidenceCount;
    public float elapsed;
}
