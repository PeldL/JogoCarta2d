using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Radio conversations for each case. Tuning and gameplay instructions belong only to the first case.
public sealed class CaseZeroRadioTutorial : MonoBehaviour
{
    CaseZeroPresentation view;
    CaseZeroGame game;
    Slider tuning;
    TMP_Text frequency;
    GameObject controls;
    float stableTime;
    int dialogue = -1;
    Tween textFade;
    float nextTuningSound;
    Image detectivePortrait, centralPortrait;
    string[] lines;
    int caseIndex;
    static readonly string[] TutorialLines = {
        "CENTRAL\nDetetive, está na escuta? Recebemos um chamado de uma morte em um bar. Precisamos de você no local.",
        "DETETIVE\nEstou ouvindo. É meu primeiro plantão com esse sistema. Como acesso o material da investigação?",
        "CENTRAL\nEstou encaminhando o resumo do plantão para o seu celular. Em Documentos você pode consultar a ocorrência e as instruções. Abra a pasta de investigação com Tab ou pelo botão na tela; use o Mapa para escolher um local.",
        "CENTRAL\nNo cenário, examine os objetos identificados. Cada descoberta fica em Evidências. Consulte os registros e ouça os envolvidos no Interrogatório. Algumas perguntas só aparecem quando você encontra uma prova que permite fazê-las.",
        "DETETIVE\nE quando eu reunir o material?",
        "CENTRAL\nAbra o celular e envie as provas para análise. Enquanto a perícia trabalha, continue investigando. Quando a ligação chegar, atenda, leia o retorno e registre o laudo. Se ficar sem saber como avançar, pode pedir orientação pelo celular.",
        "CENTRAL\nCom as provas, os depoimentos e o laudo registrados, use Dedução para apresentar sua conclusão. Confira antes de confirmar: uma acusação errada gasta uma das três tentativas. O progresso é salvo automaticamente. Vamos começar pela observação do local."
    };
    static string[] BriefingLines(int index)
    {
        var data = CaseZeroCases.All[index];
        if (index == 1)
            return new[] {
                "CENTRAL\nDetetive, quanto tempo! Pensei que tivesse esquecido esta frequência. Bom trabalho no bar. Temos uma nova ocorrência para você.",
                "DETETIVE\nCentral, bom ouvir sua voz de novo. O que aconteceu desta vez?",
                "CENTRAL\n" + data.introduction,
                "DETETIVE\nEntendido. Vou apurar o desaparecimento do pendrive. Já sabemos quem esteve no apartamento?",
                "CENTRAL\nQuatro pessoas precisam ser ouvidas: Marina, amiga de Helena; Lucas, o vizinho; Carlos, o síndico; e André, o técnico que consertou o notebook.",
                "CENTRAL\nA ocorrência é no Apartamento 302. Precisamos descobrir quem levou o pendrive, o que foi retirado e onde aconteceu o roubo. Ainda não temos uma conclusão sobre a autoria do ataque contra Helena.",
                "DETETIVE\nRecebido. Estou a caminho do apartamento. Vamos descobrir o que aconteceu."
            };
        return new[] {
            "CENTRAL\nDetetive, está na escuta? O caso de Helena ainda não acabou. A investigação do pendrive abriu uma nova frente.",
            "DETETIVE\nEstou ouvindo. Carlos admitiu o roubo, mas alguém deu a ordem. O que temos sobre essa ligação?",
            "CENTRAL\nHelena acordou e confirmou que o pendrive guardava provas contra a Vértice. Agora precisamos seguir o rastro dos pagamentos no escritório da empresa.",
            "CENTRAL\n" + data.introduction,
            "DETETIVE\nQuem pode esclarecer a movimentação no escritório?",
            "CENTRAL\nBeatriz, a contadora; Augusto, o diretor; Renato, o segurança; e Fábio, o técnico. Existem registros financeiros, acessos ao sistema e ordens que precisam ser comparados com o que eles dizem.",
            "CENTRAL\nSeu destino é o escritório. A ocorrência agora é identificar quem comandava o esquema, qual era o crime e qual empresa era usada como fachada. Não tome um nome na planilha como prova de culpa.",
            "DETETIVE\nEntendido. Vou ao escritório seguir essas pistas e esclarecer a ligação com Helena."
        };
    }

    const float Target = 104.5f;

    public void Initialize(CaseZeroPresentation presentation, CaseZeroGame investigation)
    {
        view = presentation;
        game = investigation;
        caseIndex = game.CampaignState.CaseIndex;
        lines = caseIndex == 0 ? TutorialLines : BriefingLines(caseIndex);
        view.back.gameObject.SetActive(false);
        view.title.text = "EM CASA • UM CHAMADO NO RÁDIO";
        view.stepLabel.text = "PRIMEIRO PLANTÃO";
        view.body.text = "O rádio interrompe o silêncio da casa. Há uma voz do outro lado, mas o sinal está fraco.\n\nAjuste a frequência até encontrar a central. Quanto melhor o sinal, mais perto você está. Mantenha a sintonia por um instante.";
        view.primaryLabel.text = "Aguardando sinal…";
        view.primary.interactable = false;
        var art = Resources.Load<CaseZeroArtLibrary>("ArtesCasoZero");
        detectivePortrait = Portrait("Detetive", 140, art != null ? art.radioDetetive : null);
        centralPortrait = Portrait("Central", 1480, art != null ? art.radioCentral : null);
        view.body.rectTransform.anchoredPosition = new Vector2(140, -640);
        view.body.rectTransform.sizeDelta = new Vector2(1640, 245);
        if (caseIndex > 0)
        {
            // A familiar frequency reconnects immediately; no repeated tuning or tutorial.
            dialogue = 0;
            view.primary.interactable = true;
            CaseZeroSoundFeedback.Current?.PlayTransmission();
            ShowDialogue();
            view.primary.Select();
            return;
        }
        controls = new GameObject("Sintonia do rádio", typeof(RectTransform));
        controls.transform.SetParent(view.body.transform.parent, false);
        var root = controls.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0, 100);
        root.sizeDelta = new Vector2(650, 180);

        frequency = Instantiate(view.stepLabel, root);
        frequency.name = "Frequência e intensidade";
        var labelRect = frequency.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 1);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(650, 90);
        frequency.alignment = TextAlignmentOptions.Center;
        frequency.alpha = 1;

        var sliderObject = new GameObject("Frequência", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(root, false);
        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(600, 60);
        sliderRect.anchoredPosition = new Vector2(0, -25);
        tuning = sliderObject.GetComponent<Slider>();
        var track = Graphic("Trilho", sliderRect, new Color(0.65f, 0.65f, 0.65f));
        track.rectTransform.anchorMin = new Vector2(0, 0.4f);
        track.rectTransform.anchorMax = new Vector2(1, 0.6f);
        track.rectTransform.offsetMin = track.rectTransform.offsetMax = Vector2.zero;
        var handle = Graphic("Marcador", sliderRect, Color.white);
        handle.rectTransform.sizeDelta = new Vector2(32, 60);
        tuning.handleRect = handle.rectTransform;
        tuning.targetGraphic = handle;
        tuning.minValue = 88;
        tuning.maxValue = 108;
        tuning.value = 90;
        tuning.onValueChanged.AddListener(UpdateSignal);
        UpdateSignal(tuning.value);
        tuning.Select();
    }

    Image Portrait(string name, float x, Sprite sprite)
    {
        var portrait = Graphic("Retrato • " + name, view.body.transform.parent, Color.white);
        var rect = portrait.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -260);
        rect.sizeDelta = new Vector2(300, 300);
        portrait.sprite = sprite;
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        var caption = Instantiate(view.stepLabel, portrait.transform.parent);
        caption.name = "Nome • " + name;
        caption.text = name;
        caption.alignment = TextAlignmentOptions.Center;
        caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = caption.rectTransform.pivot = new Vector2(0, 1);
        caption.rectTransform.anchoredPosition = new Vector2(x, -570);
        caption.rectTransform.sizeDelta = new Vector2(300, 45);
        return portrait;
    }

    static Image Graphic(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    void UpdateSignal(float value)
    {
        if (Time.unscaledTime >= nextTuningSound)
        {
            CaseZeroSoundFeedback.Current?.PlayRadioTick();
            nextTuningSound = Time.unscaledTime + 0.16f;
        }
        stableTime = 0;
        float distance = Mathf.Abs(value - Target);
        frequency.text = $"{value:0.0} MHz • " + (distance <= 0.6f ? "SINAL LIMPO — mantenha aqui" : distance < 2.5f ? "Sinal próximo" : "Sinal fraco");
    }

    void Update()
    {
        if (dialogue >= 0 || tuning == null) return;
        if (Mathf.Abs(tuning.value - Target) > 0.6f) { stableTime = 0; return; }
        stableTime += Time.unscaledDeltaTime;
        if (stableTime < 0.7f) return;
        dialogue = 0;
        CaseZeroSoundFeedback.Current?.PlayTransmission();
        tuning.gameObject.SetActive(false);
        frequency.text = "104,5 MHz\nCONEXÃO ESTABELECIDA";
        view.primary.interactable = true;
        view.primary.Select();
        ShowDialogue();
    }

    public void Advance()
    {
        if (dialogue < 0) return;
        CaseZeroSoundFeedback.Current?.PlayClick();
        if (++dialogue < lines.Length) ShowDialogue();
        else
        {
            view.primary.interactable = false;
            game.FinishIntroduction();
        }
    }

    void ShowDialogue()
    {
        textFade?.Kill();
        view.body.alpha = 0;
        view.title.text = "CENTRAL ↔ DETETIVE";
        view.stepLabel.text = caseIndex == 0 ? "RÁDIO • CONEXÃO ESTABELECIDA"
            : $"CASO {caseIndex + 1} • {game.CampaignState.Definition.title.ToUpperInvariant()}";
        view.primaryLabel.text = dialogue == lines.Length - 1
            ? caseIndex == 1 ? "Ir ao apartamento" : caseIndex == 2 ? "Ir ao escritório" : "Ir ao local"
            : "Continuar conversa";
        view.body.text = lines[dialogue];
        bool detectiveSpeaking = lines[dialogue].StartsWith("DETETIVE\n", System.StringComparison.Ordinal);
        detectivePortrait.color = detectiveSpeaking ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        centralPortrait.color = detectiveSpeaking ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
        textFade = DOTween.To(() => view.body.alpha, value => view.body.alpha = value, 1, 0.35f).SetUpdate(true);
    }
    void OnDestroy() => textFade?.Kill();
}
