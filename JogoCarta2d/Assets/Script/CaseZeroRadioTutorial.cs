using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Radio conversation before each case. Only the tutorial case asks the player to tune the radio.
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
    // The conversation comes from the case data (CaseZeroCase.radioLines).
    string[] lines;
    CaseZeroCase data;

    float Target;
    readonly Image[] signalBars = new Image[7];

    public void Initialize(CaseZeroPresentation presentation, CaseZeroGame investigation)
    {
        view = presentation;
        game = investigation;
        data = game.CampaignState.Definition;
        Target = Random.Range(920, 1061) / 10f;
        lines = data.radioLines != null && data.radioLines.Length > 0
            ? data.radioLines
            : new[] { "DISPATCH\nDetective, we have a new case. " + data.introduction };
        HideSkipButton();
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
        if (!data.tutorial)
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
        labelRect.anchoredPosition = new Vector2(0, 120);
        labelRect.sizeDelta = new Vector2(798.9585f, 354.3582f);
        labelRect.localScale = Vector3.one * 0.69067f;
        frequency.enableAutoSizing = false;
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
        // Arrow keys are read in Update for fine steps; the default jump is 2 MHz.
        tuning.navigation = new Navigation { mode = Navigation.Mode.None };
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
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        // Centre pivot (same on-screen placement as a top-left pivot at x, -260)
        // so the speaker highlight scales and tilts in place.
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300, 300);
        rect.anchoredPosition = new Vector2(x + 150, -260 - 150);
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
        caption.enableAutoSizing = false;
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
        text.enableAutoSizing = false;
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

    // The radio briefing has no Skip: the player tunes in and listens to dispatch.
    void HideSkipButton()
    {
        if (view.back != null) view.back.gameObject.SetActive(false);
    }

    /// <summary>Back button during the radio: skip straight to the investigation.</summary>
    public void Skip()
    {
        if (finished) return;
        CaseZeroSoundFeedback.Current?.PlayClick();
        Finish();
    }

    void Finish()
    {
        if (finished) return;
        finished = true;
        view.primary.interactable = false;
        if (view.back != null) view.back.interactable = false;
        game.FinishIntroduction();
    }
    bool finished;

    void Update()
    {
        if (dialogue >= 0 || tuning == null || finished) return;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float direction = (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed ? 1 : 0)
                - (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed ? 1 : 0);
            if (direction != 0) tuning.value += direction * 3f * Time.unscaledDeltaTime;
        }
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
        if (dialogue < 0 || finished) return;
        CaseZeroSoundFeedback.Current?.PlayClick();
        if (++dialogue < lines.Length) ShowDialogue();
        else Finish();
    }

    void ShowDialogue()
    {
        textFade?.Kill();
        view.body.alpha = 0;
        view.title.text = "DISPATCH ↔ DETECTIVE";
        view.stepLabel.text = data.tutorial ? "RADIO • CONNECTION ESTABLISHED"
            : $"CASE {game.CampaignState.CaseIndex + 1} • {data.title.ToUpperInvariant()}";
        view.primaryLabel.text = dialogue == lines.Length - 1
            ? "Go to " + CaseZeroView.DisplayName(game.CampaignState.Definition.location)
            : "Continue conversation";
        view.body.text = lines[dialogue];
        var dialogueFx = view.body.GetComponent<CaseZeroDialogueFX>();
        if (dialogueFx == null) dialogueFx = view.body.gameObject.AddComponent<CaseZeroDialogueFX>();
        dialogueFx.Reveal();
        bool detectiveSpeaking = lines[dialogue].StartsWith("DETECTIVE\n", System.StringComparison.Ordinal);
        Spotlight(detectivePortrait, detectiveSpeaking);
        Spotlight(centralPortrait, !detectiveSpeaking);
        textFade = DOTween.To(() => view.body.alpha, value => view.body.alpha = value, 1, 0.35f).SetUpdate(true);
    }

    static readonly Color Listening = new Color(0.62f, 0.62f, 0.62f);
    // The speaker steps forward a little while the listener dims; never a hard cut.
    void Spotlight(Image portrait, bool speaking)
    {
        if (portrait == null) return;
        var target = portrait.transform;
        target.DOKill();
        portrait.DOKill();
        portrait.DOColor(speaking ? Color.white : Listening, 0.25f).SetEase(Ease.OutSine).SetUpdate(true);
        target.DOScale(speaking ? 1.04f : 0.97f, 0.3f).SetEase(Ease.OutBack, 1.2f).SetUpdate(true);
        if (speaking)
            target.DOPunchRotation(new Vector3(0, 0, 1.2f), 0.35f, 5, 0.5f).SetUpdate(true);
    }

    void OnDestroy()
    {
        textFade?.Kill();
        if (detectivePortrait != null) { detectivePortrait.DOKill(); detectivePortrait.transform.DOKill(); }
        if (centralPortrait != null) { centralPortrait.DOKill(); centralPortrait.transform.DOKill(); }
    }
}
