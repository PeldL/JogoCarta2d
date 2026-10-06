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
            if (solved) return "Caso resolvido. Confira sua avaliação.";
            if (failed) return "Investigação encerrada. Tente novamente pelo resultado.";
            if (CaseIndex > 0)
            {
                if (callPending && !callRead) return "Atenda a ligação da central pelo telefone.";
                if (!Has("registro") || !Has("pistola") || !Has("capsula") || !Has("sangue"))
                    return "Examine as quatro pistas em " + (CaseIndex == 1 ? "Apartamento 302." : "Escritório.");
                if (!AllInterviewed) return "Ouça os quatro suspeitos na aba Interrogatório.";
                if (!Has("depoimento")) return "Compare as provas e confronte os depoimentos.";
                return "Abra Dedução e confirme a combinação sustentada pelas provas.";
            }
            if (callPending && !callRead) return "Atenda a ligação da perícia pelo telefone.";
            if (!Has("registro") || !Has("pistola") || !Has("capsula"))
                return location == "Bar" ? "Examine os objetos do bar e recolha as pistas." : "Vá ao bar pelo mapa e examine os objetos.";
            if (!Has("sangue")) return "No bar, examine os vestígios junto ao balcão.";
            if (!Has("depoimento")) return "No interrogatório, confronte o depoimento com as provas.";
            if (!callRead) return "Aguarde o laudo da perícia; revise as evidências enquanto isso.";
            return "Abra Dedução e compare culpado, arma e local com as provas.";
        }
    }
    public bool CanConclude => Has("pistola") && Has("capsula") && Has("registro") && Has("depoimento") && Has("sangue") && AllInterviewed;

    public bool Accuse()
    {
        if (Finished || !CanConclude) return solved;
        solved = suspect == Definition.culprit && weapon == 0 && place == 0;
        if (solved) { completedCases = Math.Max(completedCases, CaseIndex + 1); RecordResult(); }
        if (!solved) { errors++; failed = errors >= 3; }
        return solved;
    }

    public int Score => Math.Max(0, 1000 - errors * 100 - (int)(Math.Min(1, Math.Max(0, (elapsed - 300) / 600)) * 500));
    public string Rating => Score >= 900 ? "Detetive lendário" : Score >= 700 ? "Detetive exemplar" : Score >= 500 ? "Detetive competente" : "Caso resolvido";

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
