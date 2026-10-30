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
        "DISPATCH\nDetective, do you copy? We have a report of a death at a bar. We need you at the scene.",
        "DETECTIVE\nI hear you. This is my first shift using this system. How do I access the case material?",
        "DISPATCH\nI am sending the shift briefing to your phone. Documents contains the incident report and instructions. Open the case file with Tab or the on-screen button; use the Map to choose a location.",
        "DISPATCH\nExamine the marked objects at the scene. Every discovery is stored in Evidence. Read the records and listen to those involved in Interviews. Some questions only become available when you find evidence that supports them.",
        "DETECTIVE\nAnd once I have collected the material?",
        "DISPATCH\nOpen your phone and request examinations of the material you find. Sending something to forensics does not end the search for clues. Keep investigating while they work. When the call arrives, answer, read the findings, and record the report. If you get stuck, you can ask for guidance through your phone.",
        "DISPATCH\nWith the evidence, testimony, and report recorded, use Deduction to present your conclusion. Check before confirming: an incorrect accusation costs one of your three attempts. Progress is saved automatically. Start by examining the scene."
    };
    static string[] BriefingLines(int index)
    {
        var data = CaseZeroCases.All[index];
        if (index == 1)
            return new[] {
                "DISPATCH\nDetective, we have a new call. A person was found dead in their apartment. The police officer is waiting at the scene.",
                "DETECTIVE\nUnderstood. What do we know so far?",
                "DISPATCH\n" + data.introduction,
                "DETECTIVE\nI'll start with the scene and the people connected to it. How do I keep the evidence organized?",
                "DISPATCH\nUse the map to move between locations. Examine each marked object, record it in Evidence, and compare what people say in Interviews. You can send useful material to forensics through your phone.",
                "DISPATCH\nThe results will arrive as calls. Collect them at the police station, then use Deduction when the statements, physical evidence, and reports fit together. The map also lets you return to the apartment or visit the closed bar.",
                "DETECTIVE\nCopy. I'll examine the apartment first and follow the evidence wherever it leads."
            };
        return new[] {
            "DISPATCH\nDetective, we need you at the closed bar. A person was found dead there before opening. An officer and a medical examiner are assisting.",
            "DETECTIVE\nDo we know what happened?",
            "DISPATCH\n" + data.introduction,
            "DETECTIVE\nI'll examine the scene and the people connected to it. What should I do if the cause is unclear?",
            "DISPATCH\nRecord the objects you find and compare the accounts. Use your phone to request an examination when you have relevant material; the result will come by call and be collected at the police station.",
            "DISPATCH\nThe map lets you move between the bar, the bakery, and the station. Keep the body's discovery location separate from the place indicated by the evidence.",
            "DETECTIVE\nUnderstood. I'll start at the bar and follow the evidence."
        };
    }

    float Target;
    readonly Image[] signalBars = new Image[7];

    public void Initialize(CaseZeroPresentation presentation, CaseZeroGame investigation)
    {
        view = presentation;
        game = investigation;
        caseIndex = game.CampaignState.CaseIndex;
        Target = Random.Range(920, 1061) / 10f;
        lines = caseIndex == 0 ? TutorialLines : BriefingLines(caseIndex);
        view.back.gameObject.SetActive(false);
        view.title.text = "AT HOME • A CALL ON THE RADIO";
        view.stepLabel.text = "FIRST SHIFT";
        ConfigureSingleLine(view.title, 18f, view.title.fontSize);
        ConfigureSingleLine(view.stepLabel, 22f, view.stepLabel.fontSize);
        view.body.text = "— — DISPATCH... Detective, do you copy? — —\n\nThe signal is weak. Adjust the frequency until you find dispatch. The clearer the signal, the closer you are. Hold the frequency steady for a moment.";
        var radioFx = view.body.GetComponent<CaseZeroDialogueFX>();
        if (radioFx == null) radioFx = view.body.gameObject.AddComponent<CaseZeroDialogueFX>();
        radioFx.RevealRadioTransmission();
        view.primaryLabel.text = "Waiting for signal…";
        view.primary.interactable = false;
        var art = Resources.Load<CaseZeroArtLibrary>("ArtesCasoZero");
        detectivePortrait = Portrait("Detective", 140, art != null ? art.radioDetetive : null);
        centralPortrait = Portrait("Dispatch", 1480, art != null ? art.radioCentral : null);
        // Dialogue size, padding and artwork are authored in ApresentacaoCaso.unity.
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
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0, 237);
        labelRect.sizeDelta = new Vector2(798.9585f, 354.3582f);
        labelRect.localScale = Vector3.one * 0.69067f;
        ConfigureSingleLine(frequency, 16f, 36f);
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
        for (int i = 0; i < signalBars.Length; i++)
        {
            var bar = Graphic("Signal bar " + (i + 1), root, Color.black);
            bar.raycastTarget = false;
            bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 0);
            bar.rectTransform.pivot = new Vector2(0.5f, 0);
            bar.rectTransform.anchoredPosition = new Vector2((i - 3) * 30, -50);
            bar.rectTransform.sizeDelta = new Vector2(20, 12 + i * 7);
            signalBars[i] = bar;
        }
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
        caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = new Vector2(0.5f, 1);
        caption.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        caption.rectTransform.anchoredPosition = new Vector2(name == "Dispatch" ? 640.1f : -670f, -226f);
        caption.rectTransform.sizeDelta = new Vector2(374.89f, 98.9149f);
        caption.rectTransform.localScale = Vector3.one * 0.65529f;
        ConfigureSingleLine(caption, 16f, 36f);
        return portrait;
    }

    static void ConfigureSingleLine(TMP_Text text, float minFontSize, float maxFontSize)
    {
        if (text == null) return;

        // Runtime-cloned labels must stay left-to-right and on one horizontal line.
        text.isRightToLeftText = false;
        text.enableWordWrapping = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableAutoSizing = true;
        text.fontSizeMin = minFontSize;
        text.fontSizeMax = maxFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.localRotation = Quaternion.identity;
        text.ForceMeshUpdate();
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
        int lit = distance <= 0.6f ? signalBars.Length : Mathf.Clamp(Mathf.FloorToInt((1 - distance / 10f) * 6), 0, 6);
        for (int i = 0; i < signalBars.Length; i++)
            signalBars[i].color = i < lit ? Color.black : new Color(0, 0, 0, 0.15f);
        frequency.text = $"{value:0.0} MHz • " + (distance <= 0.6f ? "CLEAR SIGNAL — hold here" : distance < 2.5f ? "Signal nearby" : "Weak signal");
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
        frequency.text = $"{tuning.value:0.0} MHz\nCONNECTION ESTABLISHED";
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
        view.title.text = "DISPATCH ↔ DETECTIVE";
        view.stepLabel.text = caseIndex == 0 ? "RADIO • CONNECTION ESTABLISHED"
            : $"CASE {caseIndex + 1} • {game.CampaignState.Definition.title.ToUpperInvariant()}";
        view.primaryLabel.text = dialogue == lines.Length - 1
            ? "Go to " + CaseZeroView.DisplayName(game.CampaignState.Definition.location)
            : "Continue conversation";
        view.body.text = lines[dialogue];
        var dialogueFx = view.body.GetComponent<CaseZeroDialogueFX>();
        if (dialogueFx == null) dialogueFx = view.body.gameObject.AddComponent<CaseZeroDialogueFX>();
        dialogueFx.Reveal();
        bool detectiveSpeaking = lines[dialogue].StartsWith("DETECTIVE\n", System.StringComparison.Ordinal);
        detectivePortrait.color = detectiveSpeaking ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        centralPortrait.color = detectiveSpeaking ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
        textFade = DOTween.To(() => view.body.alpha, value => view.body.alpha = value, 1, 0.35f).SetUpdate(true);
    }
    void OnDestroy() => textFade?.Kill();
}
