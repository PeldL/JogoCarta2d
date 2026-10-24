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
                "DISPATCH\nDetective, it has been a while! I thought you had forgotten this frequency. Good work at the bar. We have a new incident for you.",
                "DETECTIVE\nDispatch, good to hear your voice again. What happened this time?",
                "DISPATCH\n" + data.introduction,
                "DETECTIVE\nUnderstood. I will investigate the missing USB drive. Do we know who visited the apartment?",
                "DISPATCH\nWe need to hear from four people: Marina, Helena's friend; Lucas, the neighbor; Carlos, the building manager; and André, the technician who repaired the laptop.",
                "DISPATCH\nThe incident took place in Apartment 302. We need to establish who took the USB drive, what was removed, and where the theft happened. Responsibility for the assault on Helena remains unresolved.",
                "DETECTIVE\nCopy that. I am on my way to the apartment. Let us find out what happened."
            };
        return new[] {
            "DISPATCH\nDetective, do you copy? Helena's case is not over yet. The missing USB drive has opened a new line of inquiry.",
            "DETECTIVE\nI hear you. Carlos admitted the theft, but someone gave the order. What do we know about that connection?",
            "DISPATCH\nHelena is awake and confirmed that the USB drive held evidence against Vértice. We now need to follow the payments at the company office.",
            "DISPATCH\n" + data.introduction,
            "DETECTIVE\nWho can explain what was happening at the office?",
            "DISPATCH\nBeatriz, the accountant; Augusto, the director; Renato, the security guard; and Fábio, the technician. Financial records, system access logs, and instructions need to be compared with their statements.",
            "DISPATCH\nYour destination is the office. Establish who ran the scheme, what the crime was, and which company was used as a front. A name on a spreadsheet is not proof of guilt.",
            "DETECTIVE\nUnderstood. I will follow those leads at the office and clarify the connection to Helena."
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
        view.body.text = "The radio breaks the silence at home. There is a voice on the other end, but the signal is weak.\n\nAdjust the frequency until you find dispatch. The clearer the signal, the closer you are. Hold the frequency steady for a moment.";
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
            ? caseIndex == 1 ? "Go to the apartment" : caseIndex == 2 ? "Go to the office" : "Go to the scene"
            : "Continue conversation";
        view.body.text = lines[dialogue];
        bool detectiveSpeaking = lines[dialogue].StartsWith("DETECTIVE\n", System.StringComparison.Ordinal);
        detectivePortrait.color = detectiveSpeaking ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        centralPortrait.color = detectiveSpeaking ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
        textFade = DOTween.To(() => view.body.alpha, value => view.body.alpha = value, 1, 0.35f).SetUpdate(true);
    }
    void OnDestroy() => textFade?.Kill();
}
