using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The Interviews page as a person picker, like the Deduction suspects: a large photo with the
// name under it, side arrows to browse the people in the case and an "Interview" button that
// takes the selected person to the interrogation room. The questioning itself happens in the room.
// It reuses the page's authored portrait and name; the old dialogue widgets are hidden.
[DisallowMultipleComponent]
public sealed class CaseZeroInterviewPicker : MonoBehaviour
{
    static readonly Vector2 PhotoSize = new Vector2(330, 360);
    const float PhotoTop = -30, ArrowOffset = 270;

    CaseZeroView view;
    RectTransform photo, nameRect;
    TMP_Text status;
    Button previous, next, interview;
    System.Action<int> onStep;
    System.Action onInterview;

    public static CaseZeroInterviewPicker Attach(CaseZeroView view, System.Action<int> step, System.Action interviewSelected)
    {
        if (view == null || view.interviewPortrait == null) return null;
        var host = view.interviewPortrait.transform.parent;
        var picker = host.GetComponent<CaseZeroInterviewPicker>();
        if (picker == null)
        {
            picker = host.gameObject.AddComponent<CaseZeroInterviewPicker>();
            picker.Build(view);
        }
        picker.onStep = step;
        picker.onInterview = interviewSelected;
        return picker;
    }

    void Build(CaseZeroView owner)
    {
        view = owner;
        var area = (RectTransform)transform;
        float centre = area.rect.width * 0.5f;

        // The dialogue moved to the interrogation room: hide its widgets on this page.
        if (view.interviewMessage != null) view.interviewMessage.gameObject.SetActive(false);
        foreach (var choice in view.interviewChoices) if (choice != null) choice.gameObject.SetActive(false);
        foreach (var button in view.interviewSuspectButtons) if (button != null) button.gameObject.SetActive(false);
        foreach (Transform child in transform)
            if (child.name.StartsWith("Artwork") || child.name.StartsWith("Legenda")) child.gameObject.SetActive(false);

        view.ForgetPortraitRest();
        photo = view.interviewPortrait.rectTransform;
        photo.anchorMin = photo.anchorMax = new Vector2(0, 1);
        photo.pivot = new Vector2(0.5f, 1);
        photo.localScale = Vector3.one;
        photo.sizeDelta = PhotoSize;
        photo.anchoredPosition = new Vector2(centre, PhotoTop);
        view.interviewPortrait.preserveAspect = true;

        var nameLabel = view.interviewPortraitName;
        // Texts on this page use the game's typewriter font (the one on the question buttons).
        TMP_Text template = view.interviewChoiceLabels.Length > 0 && view.interviewChoiceLabels[0] != null ? view.interviewChoiceLabels[0] : nameLabel;
        if (nameLabel != null)
        {
            nameRect = nameLabel.rectTransform;
            nameRect.anchorMin = nameRect.anchorMax = new Vector2(0, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.sizeDelta = new Vector2(620, 46);
            nameRect.anchoredPosition = new Vector2(centre, PhotoTop - PhotoSize.y - 8);
            if (template != null && template != nameLabel) { nameLabel.font = template.font; nameLabel.fontSharedMaterial = template.fontSharedMaterial; }
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMax = Mathf.Max(28, nameLabel.fontSize);
            nameLabel.fontSizeMin = 18;
        }

        status = Text("Situação", template, 20, new Vector2(centre, PhotoTop - PhotoSize.y - 52), new Vector2(620, 30));
        status.alpha = 0.7f;

        // Arrows: same ">" glyph on both sides (mirrored on the left), like the Deduction carousel.
        float arrowY = PhotoTop - PhotoSize.y * 0.5f;
        previous = Arrow("Seta • pessoa anterior", new Vector2(centre - ArrowOffset, arrowY), template, true);
        next = Arrow("Seta • próxima pessoa", new Vector2(centre + ArrowOffset, arrowY), template, false);
        previous.onClick.AddListener(() => onStep?.Invoke(-1));
        next.onClick.AddListener(() => onStep?.Invoke(1));

        // "Interview" button styled like the page's question buttons.
        var style = view.interviewChoices.Length > 0 && view.interviewChoices[0] != null ? view.interviewChoices[0].targetGraphic as Image : null;
        var root = new GameObject("Botão • Interview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rect = (RectTransform)root.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(380, 70);
        rect.anchoredPosition = new Vector2(centre, PhotoTop - PhotoSize.y - 96);
        var image = root.GetComponent<Image>();
        if (style != null) { image.sprite = style.sprite; image.type = style.type; image.color = style.color; }
        interview = root.GetComponent<Button>();
        interview.targetGraphic = image;
        interview.onClick.AddListener(() => onInterview?.Invoke());
        root.AddComponent<CaseZeroButtonMotion>();
        var label = Text("Texto", template, 30, Vector2.zero, Vector2.zero, rect);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.text = "Interview";
    }

    /// <summary>Updates the arrows and the status line under the name.</summary>
    public void Refresh(int count, bool interviewed, bool rorschach)
    {
        bool several = count > 1;
        previous.gameObject.SetActive(several);
        next.gameObject.SetActive(several);
        status.text = interviewed ? (rorschach ? "Interviewed • Rorschach test done" : "Interviewed") : "Not interviewed yet";
    }

    Button Arrow(string name, Vector2 position, TMP_Text template, bool mirrored)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rect = (RectTransform)root.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(70, 70);
        rect.anchoredPosition = position;
        // Invisible click area; only the glyph shows.
        var image = root.GetComponent<Image>();
        image.color = new Color(1, 1, 1, 0);
        var button = root.GetComponent<Button>();
        button.targetGraphic = image;
        root.AddComponent<CaseZeroButtonMotion>();
        var glyph = Text("Seta", template, 48, Vector2.zero, Vector2.zero, rect);
        glyph.rectTransform.anchorMin = Vector2.zero;
        glyph.rectTransform.anchorMax = Vector2.one;
        glyph.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        glyph.rectTransform.offsetMin = glyph.rectTransform.offsetMax = Vector2.zero;
        glyph.fontStyle = FontStyles.Bold;
        glyph.text = ">";
        if (mirrored) glyph.rectTransform.localScale = new Vector3(-1, 1, 1);
        return button;
    }

    TMP_Text Text(string name, TMP_Text template, float size, Vector2 position, Vector2 box, Transform parent = null)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rect = (RectTransform)root.transform;
        rect.SetParent(parent != null ? parent : transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = box;
        rect.anchoredPosition = position;
        var label = root.GetComponent<TextMeshProUGUI>();
        if (template != null) { label.font = template.font; label.fontSharedMaterial = template.fontSharedMaterial; label.color = template.color; }
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }
}
