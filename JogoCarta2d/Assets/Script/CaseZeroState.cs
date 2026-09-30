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

    public bool Has(string id) => evidence.Contains(id);
    public bool Finished => solved || failed;
    public int AttemptsRemaining => Math.Max(0, 3 - errors);
    public string Objective
    {
        get
        {
            if (solved) return "Caso resolvido. Confira sua avaliação.";
            if (failed) return "Investigação encerrada. Tente novamente pelo resultado.";
            if (callPending && !callRead) return "Atenda a ligação da perícia pelo telefone.";
            if (!Has("registro") || !Has("pistola") || !Has("capsula"))
                return location == "Bar" ? "Examine os objetos do bar e recolha as pistas." : "Vá ao bar pelo mapa e examine os objetos.";
            if (!Has("sangue")) return "No bar, examine os vestígios junto ao balcão.";
            if (!Has("depoimento")) return "No interrogatório, confronte o depoimento com as provas.";
            if (!callRead) return "Aguarde o laudo da perícia; revise as evidências enquanto isso.";
            return "Abra Dedução e compare culpado, arma e local com as provas.";
        }
    }
    public bool CanConclude => Has("pistola") && Has("capsula") && Has("registro") && Has("depoimento") && Has("sangue");

    public bool Accuse()
    {
        if (Finished || !CanConclude) return solved;
        solved = suspect == 0 && weapon == 0 && place == 0;
        if (!solved) { errors++; failed = errors >= 3; }
        return solved;
    }

    public int Score => Math.Max(0, 1000 - errors * 100 - (int)(Math.Min(1, Math.Max(0, (elapsed - 300) / 600)) * 500));
    public string Rating => Score >= 900 ? "Detetive lendário" : Score >= 700 ? "Detetive exemplar" : Score >= 500 ? "Detetive competente" : "Caso resolvido";

    public bool IsValid()
    {
        return version == 1 && caseId == "ultima-testemunha" && evidence != null && photographs != null && questions != null
            && !float.IsNaN(elapsed) && !float.IsInfinity(elapsed) && elapsed >= 0 && errors >= 0
            && !float.IsNaN(callDelay) && !float.IsInfinity(callDelay) && callDelay >= -1
            && !float.IsNaN(idleTime) && !float.IsInfinity(idleTime) && idleTime >= 0
            && suspect >= 0 && suspect < 3 && weapon >= 0 && weapon < 3 && place >= 0 && place < 3;
    }
}
