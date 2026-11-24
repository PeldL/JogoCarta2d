using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ApresentacaoCaso: the radio briefing before each case (CaseZeroRadioTutorial).
// FinalCampanha (finale = true): the campaign summary after the last case.
[ExecuteAlways]
public sealed class CaseZeroPresentation : MonoBehaviour
{
    public bool finale;
    public TMP_Text title, body, primaryLabel, stepLabel;
    public Button primary, back;
    CaseZeroGame game;
    CaseZeroRadioTutorial radio;

    void OnEnable()
    {
#if UNITY_EDITOR
        // The radio preview belongs to the case briefing only, not to the campaign finale.
        if (!Application.isPlaying)
        {
            if (finale) ClearRadioEditPreview();
            else EnsureRadioEditPreview();
        }
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
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 120);
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
        text.enableAutoSizing = false;
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
        if (finale) game.OpenCredits();
    }
    public void Back()
    {
        if (game == null) return;
        if (radio != null) { radio.Skip(); return; }
        if (finale) game.ReturnToMenu();
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
    // Only the finale uses this screen's own text; case briefings are drawn by the radio.
    void Refresh()
    {
        AnimateSlide();
        int cases = CaseZeroCases.All.Length;
        title.text = "CAMPAIGN COMPLETE";
        stepLabel.text = cases + " CASES SOLVED";
        body.text = Summary(game.CampaignState);
        primaryLabel.text = "View credits";
        back.gameObject.SetActive(true);
        RevealBody();
    }

    void RevealBody()
    {
        var fx = body.GetComponent<CaseZeroDialogueFX>();
        if (fx == null) fx = body.gameObject.AddComponent<CaseZeroDialogueFX>();
        // The finale summary keeps a light tremble for as long as it is on screen.
        fx.RevealAndKeepTrembling(0.55f, 56f);
    }

    public static string Summary(CaseZeroState state)
    {
        state.RecordResult();
        int cases = CaseZeroCases.All.Length;
        var text = new StringBuilder("You have completed every case in Detective: Case Zero!\n\n");
        int total = 0, errors = 0, known = 0; float elapsed = 0;
        for (int i = 0; i < cases; i++)
        {
            var definition = CaseZeroCases.All[i];
            var result = state.results.Find(r => r != null && r.caseId == definition.id);
            if (result == null) { text.AppendLine($"Case {i + 1} • History unavailable in this older save.\n"); continue; }
            known++; total += result.score; errors += result.errors; elapsed += result.elapsed;
            text.AppendLine($"Case {i + 1} • {definition.title}");
            text.AppendLine($"{result.score}/1000 points ({result.score / 10f:0.#}%)  •  {Clock(result.elapsed)}  •  {result.errors} mistake(s)\n");
        }
        int maximum = cases * 1000;
        if (known == cases)
        {
            string award = total == maximum ? "HIGHEST DISTINCTION • LEGENDARY DETECTIVE" : total >= maximum * 0.8f ? "AWARD • EXEMPLARY DETECTIVE" : "AWARD • PERSISTENT DETECTIVE";
            text.AppendLine(award);
            text.AppendLine($"Total score: {total}/{maximum} ({total * 100f / maximum:0.#}%)");
            text.AppendLine($"Total time for completed cases: {Clock(elapsed)}  •  Mistakes: {errors}");
            if (total == maximum) text.AppendLine("A perfect score across every case!");
        }
        else text.AppendLine("CAMPAIGN COMPLETE • Overall statistics unavailable: this save did not record every earlier case.");
        text.Append("\nThe evaluation uses the completed attempt for each case. Replaying a case replaces its previous result.");
        return text.ToString();
    }
    static string Clock(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
}
