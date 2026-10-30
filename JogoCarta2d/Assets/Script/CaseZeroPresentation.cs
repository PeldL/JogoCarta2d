using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Case introductions use the radio; only the first case includes tuning and tutorial instructions.
[ExecuteAlways]
public sealed class CaseZeroPresentation : MonoBehaviour
{
    public bool finale;
    public TMP_Text title, body, primaryLabel, stepLabel;
    public Button primary, back;
    CaseZeroGame game;
    int step;
    CaseZeroRadioTutorial radio;
    public int Step => step;

    void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) EnsureRadioEditPreview();
#endif
    }

    void EnsureRadioEditPreview()
    {
        if (body == null || stepLabel == null) return;
        Transform parent = body.transform.parent;
        if (parent == null) return;

        var controls = parent.Find("Sintonia do rádio");
        if (controls == null)
        {
            var root = new GameObject("Sintonia do rádio", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 100);
            rect.sizeDelta = new Vector2(650, 180);
            controls = root.transform;
        }

        if (parent.Find("Frequência e intensidade") == null)
        {
            var label = Instantiate(stepLabel, parent);
            label.name = "Frequência e intensidade";
            label.text = "92.0 MHz • Weak signal";
            label.isRightToLeftText = false;
            label.enableWordWrapping = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = 36;
            label.alignment = TextAlignmentOptions.Center;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 237);
            rect.sizeDelta = new Vector2(798.9585f, 354.3582f);
            rect.localScale = Vector3.one * 0.69067f;
            rect.localRotation = Quaternion.identity;
        }

        if (controls.Find("Frequência") == null)
        {
            var sliderObject = new GameObject("Frequência", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(controls, false);
            var rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 60);
            rect.anchoredPosition = new Vector2(0, -25);
            var slider = sliderObject.GetComponent<Slider>();
            var track = CreateRadioPreviewGraphic("Trilho", rect, new Color(0.65f, 0.65f, 0.65f));
            track.rectTransform.anchorMin = new Vector2(0, 0.4f);
            track.rectTransform.anchorMax = new Vector2(1, 0.6f);
            track.rectTransform.offsetMin = track.rectTransform.offsetMax = Vector2.zero;
            var handle = CreateRadioPreviewGraphic("Marcador", rect, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(32, 60);
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 88;
            slider.maxValue = 108;
            slider.value = 90;
        }

        for (int i = 0; i < 7; i++)
        {
            string barName = "Signal bar " + (i + 1);
            if (controls.Find(barName) != null) continue;
            var bar = CreateRadioPreviewGraphic(barName, controls, Color.black);
            bar.raycastTarget = false;
            bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 0);
            bar.rectTransform.pivot = new Vector2(0.5f, 0);
            bar.rectTransform.anchoredPosition = new Vector2((i - 3) * 30, -50);
            bar.rectTransform.sizeDelta = new Vector2(20, 12 + i * 7);
        }

        var art = Resources.Load<CaseZeroArtLibrary>("ArtesCasoZero");
        CreateRadioPreviewPortrait(parent, "Detective", 140, art != null ? art.radioDetetive : null);
        CreateRadioPreviewPortrait(parent, "Dispatch", 1480, art != null ? art.radioCentral : null);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    void CreateRadioPreviewPortrait(Transform parent, string name, float x, Sprite sprite)
    {
        var portraitTransform = parent.Find("Retrato • " + name);
        if (portraitTransform == null)
        {
            var portrait = CreateRadioPreviewGraphic("Retrato • " + name, parent, Color.white);
            portraitTransform = portrait.transform;
            var rect = portrait.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -260);
            rect.sizeDelta = new Vector2(300, 300);
            portrait.sprite = sprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
        }
        else
        {
            var portrait = portraitTransform.GetComponent<Image>();
            if (portrait != null) portrait.sprite = sprite;
        }

        var captionTransform = parent.Find("Nome • " + name);
        if (captionTransform == null)
        {
            var caption = Instantiate(stepLabel, parent);
            caption.name = "Nome • " + name;
            captionTransform = caption.transform;
        }
        var text = captionTransform.GetComponent<TMP_Text>();
        text.text = name;
        text.isRightToLeftText = false;
        text.enableWordWrapping = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Center;
        var captionRect = (RectTransform)captionTransform;
        captionRect.anchorMin = captionRect.anchorMax = new Vector2(0.5f, 1);
        captionRect.pivot = new Vector2(0.5f, 0.5f);
        captionRect.anchoredPosition = new Vector2(name == "Dispatch" ? 640.1f : -670f, -226f);
        captionRect.sizeDelta = new Vector2(374.89f, 98.9149f);
        captionRect.localScale = Vector3.one * 0.65529f;
        captionRect.localRotation = Quaternion.identity;
    }

    Image CreateRadioPreviewGraphic(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    void ClearRadioEditPreview()
    {
        if (body == null || body.transform.parent == null) return;
        Transform parent = body.transform.parent;
        string[] names = { "Sintonia do rádio", "Frequência e intensidade", "Retrato • Detective", "Retrato • Dispatch", "Nome • Detective", "Nome • Dispatch" };
        foreach (string objectName in names)
        {
            var found = parent.Find(objectName);
            if (found == null) continue;
            if (Application.isPlaying) Destroy(found.gameObject);
            else DestroyImmediate(found.gameObject);
        }
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        ClearRadioEditPreview();
        game = FindFirstObjectByType<CaseZeroGame>();
        if (game == null || game.CampaignState == null)
        {
            title.text = "PRESENTATION PREVIEW";
            body.text = "Start a game from the Menu to test the full flow. The elements on this screen can be edited in the scene.";
            primary.interactable = false;
            back.gameObject.SetActive(false);
            return;
        }
        step = game.CampaignState.CaseIndex == 0 ? 0 : 3;
        if (!finale)
        {
            radio = gameObject.AddComponent<CaseZeroRadioTutorial>();
            radio.Initialize(this, game);
            return;
        }
        Refresh();
    }

    public void Advance()
    {
        if (game == null) return;
        if (radio != null) { radio.Advance(); return; }
        if (finale) { game.OpenCredits(); return; }
        if (step < 3) { step++; Refresh(); }
        else { primary.interactable = false; game.FinishIntroduction(); }
    }
    public void Back()
    {
        if (game == null) return;
        if (radio != null) return;
        if (finale) { game.ReturnToMenu(); return; }
        if (step > 0 && game.CampaignState.CaseIndex == 0) { step--; Refresh(); }
    }
    Sequence slide;
    void OnDestroy() => slide?.Kill(true);
    void AnimateSlide()
    {
        slide?.Kill(true);
        slide = DOTween.Sequence().SetUpdate(true);
        TMP_Text[] labels = { stepLabel, title, body };
        for (int i = 0; i < labels.Length; i++)
        {
            var label = labels[i];
            if (label == null) continue;
            var rect = label.rectTransform;
            Vector2 origin = rect.anchoredPosition;
            rect.anchoredPosition = origin + Vector2.right * 36;
            float alpha = label.alpha;
            label.alpha = 0;
            slide.Insert(i * 0.10f, rect.DOAnchorPos(origin, 0.48f).SetEase(Ease.OutCubic));
            slide.Insert(i * 0.10f, DOTween.To(() => label.alpha, value => label.alpha = value, alpha, 0.4f));
        }
    }
    void Refresh()
    {
        AnimateSlide();
        var state = game.CampaignState;
        if (finale)
        {
            title.text = "CAMPAIGN COMPLETE";
            stepLabel.text = "3 CASES SOLVED";
            body.text = Summary(state);
            primaryLabel.text = "View credits";
            back.gameObject.SetActive(true);
            RevealBody();
            return;
        }
        back.gameObject.SetActive(state.CaseIndex == 0 && step > 0);
        primaryLabel.text = step == 3 ? "Start investigation" : "Continue";
        stepLabel.text = step < 3 ? $"TUTORIAL • {step + 1}/3" : "CASE BRIEFING";
        if (step == 0)
        {
            title.text = "OBSERVE AND GATHER CLUES";
            body.text = "You are the detective in charge of the investigation.\n\nClick marked objects in the scene to examine them and collect evidence. A notification confirms each discovery.\n\nClues are stored in the case file. You do not need to take photographs.";
        }
        else if (step == 1)
        {
            title.text = "COMPARE EVIDENCE AND TESTIMONY";
            body.text = "Open the investigation using the on-screen button or Tab.\n\nIn Evidence, read what you have found. In Interviews, listen to those involved and compare their accounts with the evidence.\n\nThe Map lets you visit other locations. The phone alerts you to new information: open it, answer, and record the message.";
        }
        else if (step == 2)
        {
            title.text = "CONCLUDE WITH CARE";
            body.text = "When you have the necessary evidence, open Deduction and select the combination that explains the case.\n\nAn incorrect accusation costs one attempt. After three mistakes, you can restart the case. Canceling the confirmation does not use an attempt.\n\nProgress is saved automatically. Use Saves to save or load another slot; Settings contains fullscreen, music, and sound effects.";
        }
        else
        {
            title.text = $"CASE {state.CaseIndex + 1} • {state.Definition.title}";
            body.text = state.Definition.introduction + "\n\n" + (state.CaseIndex == 0
                ? "Your objective: identify the culprit, the weapon, and where the crime took place."
                : state.CaseIndex == 1 ? "Investigate the scene, interview those involved, and challenge their accounts. Find out who stole the item, what was taken, and where the theft happened."
                : "Examine the records, interview the four people involved, and challenge their accounts. Identify the organizer, the crime, and the shell company.");
        }
        RevealBody();
    }

    void RevealBody()
    {
        var fx = body.GetComponent<CaseZeroDialogueFX>();
        if (fx == null) fx = body.gameObject.AddComponent<CaseZeroDialogueFX>();
        fx.Reveal(0.55f, 56f);
    }

    public static string Summary(CaseZeroState state)
    {
        state.RecordResult();
        var text = new StringBuilder("You have completed every case in Detective: Case Zero!\n\n");
        int total = 0, errors = 0, known = 0; float elapsed = 0;
        for (int i = 0; i < CaseZeroCases.All.Length; i++)
        {
            var definition = CaseZeroCases.All[i];
            var result = state.results.Find(r => r != null && r.caseId == definition.id);
            if (result == null) { text.AppendLine($"Case {i + 1} • History unavailable in this older save.\n"); continue; }
            known++; total += result.score; errors += result.errors; elapsed += result.elapsed;
            text.AppendLine($"Case {i + 1} • {definition.title}");
            text.AppendLine($"{result.score}/1000 points ({result.score / 10f:0.#}%)  •  {Clock(result.elapsed)}  •  {result.errors} mistake(s)\n");
        }
        if (known == 3)
        {
            string award = total == 3000 ? "HIGHEST DISTINCTION • LEGENDARY DETECTIVE" : total >= 2400 ? "AWARD • EXEMPLARY DETECTIVE" : "AWARD • PERSISTENT DETECTIVE";
            text.AppendLine(award);
            text.AppendLine($"Total score: {total}/3000 ({total / 30f:0.#}%)");
            text.AppendLine($"Total time for completed cases: {Clock(elapsed)}  •  Mistakes: {errors}");
            if (total == 3000) text.AppendLine("A perfect score across all three cases!");
        }
        else text.AppendLine("CAMPAIGN COMPLETE • Overall statistics unavailable: this save did not record every earlier case.");
        text.Append("\nThe evaluation uses the completed attempt for each case. Replaying a case replaces its previous result.");
        return text.ToString();
    }
    static string Clock(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
}
