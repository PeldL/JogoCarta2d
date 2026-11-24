using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Turns the scrolling suspect list into a one-at-a-time selector with side arrows.
// The options themselves stay authored in the scene: an arrow simply presses the
// neighbouring option's own Button, so the existing SelectSuspect binding, click
// sound and selection marker keep working unchanged.
[DisallowMultipleComponent]
public sealed class CaseZeroSuspectCarousel : MonoBehaviour
{
    [SerializeField, Min(0.05f)] float slideDuration = 0.28f;
    [SerializeField] Vector2 arrowSize = new Vector2(56, 56);
    [SerializeField, Tooltip("Horizontal distance of each arrow from the centre of Opcao_0_0's portrait.")]
    float arrowOffset = 210;

    RectTransform viewport, content;
    Button[] options;
    CanvasGroup[] fades;
    Button previous, next;
    int current = -1;
    Sequence slide;
    RectTransform[] rects;
    Vector2 slot;            // Centred resting position shared by every option.
    float optionHeight = 150, travel = 550;

    public const string PreviousArrowName = "Seta • suspeito anterior";
    public const string NextArrowName = "Seta • próximo suspeito";

    /// <param name="authoredPrevious">Arrow placed in the scene (optional). When both arrows are
    /// given they are used as they are; otherwise default arrows are created at runtime.</param>
    public static CaseZeroSuspectCarousel Attach(RectTransform content, Button authoredPrevious = null, Button authoredNext = null)
    {
        if (content == null) return null;
        var scroll = content.GetComponentInParent<ScrollRect>(true);
        var host = scroll != null ? scroll.gameObject : content.parent != null ? content.parent.gameObject : null;
        if (host == null) return null;
        var carousel = host.GetComponent<CaseZeroSuspectCarousel>();
        if (carousel == null) carousel = host.AddComponent<CaseZeroSuspectCarousel>();
        carousel.Build(content, scroll, authoredPrevious, authoredNext);
        return carousel;
    }

    void Build(RectTransform list, ScrollRect scroll, Button authoredPrevious, Button authoredNext)
    {
        if (content != null) return;
        content = list;
        viewport = (RectTransform)transform;
        // Browsing is done with the arrows now; dragging would fight the slide.
        if (scroll != null) scroll.enabled = false;

        options = new Button[content.childCount];
        fades = new CanvasGroup[options.Length];
        rects = new RectTransform[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            var child = content.GetChild(i);
            options[i] = child.GetComponent<Button>();
            rects[i] = (RectTransform)child;
            fades[i] = child.GetComponent<CanvasGroup>();
            if (fades[i] == null) fades[i] = child.gameObject.AddComponent<CanvasGroup>();
        }
        if (options.Length == 0) return;
        // Opcao_0_0 is the reference laid out in the scene: every other option takes its
        // size, scale, place and background, so they all look identical while sliding.
        var reference = rects[0];
        var referenceImage = reference.GetComponent<Image>();
        slot = reference.anchoredPosition;
        travel = viewport.rect.width;
        for (int i = 0; i < rects.Length; i++)
        {
            var rect = rects[i];
            if (rect == null) continue;
            if (i > 0)
            {
                rect.anchorMin = reference.anchorMin;
                rect.anchorMax = reference.anchorMax;
                rect.pivot = reference.pivot;
                rect.sizeDelta = reference.sizeDelta;
                rect.localScale = reference.localScale;
                rect.localRotation = reference.localRotation;
                var image = rect.GetComponent<Image>();
                if (image != null && referenceImage != null)
                {
                    image.sprite = referenceImage.sprite;
                    image.type = referenceImage.type;
                    image.color = referenceImage.color;
                    image.material = referenceImage.material;
                    image.preserveAspect = referenceImage.preserveAspect;
                    image.pixelsPerUnitMultiplier = referenceImage.pixelsPerUnitMultiplier;
                }
            }
            rect.anchoredPosition = slot;
        }
        LayOutLikeFirstOption();

        if (authoredPrevious != null && authoredNext != null)
        {
            // Arrows edited in the scene: keep their look and place, only wire them up.
            previous = authoredPrevious;
            next = authoredNext;
            foreach (var arrow in new[] { previous, next })
                if (arrow.GetComponent<CaseZeroButtonMotion>() == null) arrow.gameObject.AddComponent<CaseZeroButtonMotion>();
        }
        else
        {
            var arrows = CreateDefaultArrows(viewport, content, arrowSize, arrowOffset);
            previous = arrows[0];
            next = arrows[1];
        }
        previousRest = ((RectTransform)previous.transform).anchoredPosition;
        nextRest = ((RectTransform)next.transform).anchoredPosition;
        var previousGlyph = Glyph(previous);
        var nextGlyph = Glyph(next);
        if (previousGlyph != null) previousGlyphRest = previousGlyph.anchoredPosition;
        if (nextGlyph != null) nextGlyphRest = nextGlyph.anchoredPosition;
        previous.onClick.AddListener(() => { Kick(previous, -1); Step(-1); ReleaseSelection(); });
        next.onClick.AddListener(() => { Kick(next, 1); Step(1); ReleaseSelection(); });
        if (isActiveAndEnabled) PlayArrowIntro();
    }

    // Every option shows its photo and the name written under it exactly like Opcao_0_0.
    // The photo is enlarged to fill the tag above the name (it was drawn very small).
    void LayOutLikeFirstOption()
    {
        var reference = rects[0];
        var referencePhoto = Photo(reference);
        var referenceName = Name(reference);
        if (referencePhoto != null)
        {
            // Width of the tag minus margins; height down to just above the name.
            float nameTop = referenceName != null ? -referenceName.rectTransform.anchoredPosition.y : reference.rect.height;
            float height = Mathf.Max(60, nameTop - 8);
            float width = Mathf.Min(reference.rect.width - 40, height * 1.6f);
            referencePhoto.anchorMin = referencePhoto.anchorMax = new Vector2(0, 1);
            referencePhoto.pivot = new Vector2(0.5f, 1);
            referencePhoto.localScale = Vector3.one;
            referencePhoto.sizeDelta = new Vector2(width, height);
            referencePhoto.anchoredPosition = new Vector2(reference.rect.width * 0.5f, -4);
            var image = referencePhoto.GetComponent<Image>();
            if (image != null) image.preserveAspect = true;
        }
        for (int i = 1; i < rects.Length; i++)
        {
            if (rects[i] == null) continue;
            Copy(referencePhoto, Photo(rects[i]));
            var name = Name(rects[i]);
            if (referenceName != null && name != null)
            {
                Copy(referenceName.rectTransform, name.rectTransform);
                name.font = referenceName.font;
                name.fontSharedMaterial = referenceName.fontSharedMaterial;
                name.fontStyle = referenceName.fontStyle;
                name.color = referenceName.color;
                name.characterSpacing = referenceName.characterSpacing;
                name.alignment = referenceName.alignment;
                name.fontSize = referenceName.fontSize;
                name.enableAutoSizing = referenceName.enableAutoSizing;
                name.fontSizeMin = referenceName.fontSizeMin;
                name.fontSizeMax = referenceName.fontSizeMax;
                name.textWrappingMode = referenceName.textWrappingMode;
            }
            var photoImage = Photo(rects[i]) != null ? Photo(rects[i]).GetComponent<Image>() : null;
            if (photoImage != null) photoImage.preserveAspect = true;
        }
    }

    static RectTransform Photo(RectTransform option)
    {
        foreach (Transform child in option)
            if (child.name.StartsWith("Retrato")) return child as RectTransform;
        return null;
    }

    static TMP_Text Name(RectTransform option)
    {
        foreach (Transform child in option)
            if (child.name.StartsWith("Texto") && child.TryGetComponent<TMP_Text>(out var text)) return text;
        return null;
    }

    static void Copy(RectTransform from, RectTransform to)
    {
        if (from == null || to == null) return;
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;
        to.sizeDelta = from.sizeDelta;
        to.anchoredPosition = from.anchoredPosition;
        to.localScale = from.localScale;
        to.localRotation = from.localRotation;
    }

    // ---- Arrow animation -------------------------------------------------------
    // Intro: the arrows glide in from the sides when the deduction page opens.
    // Idle: the arrow glyphs gently nudge outward, inviting a click.
    // Click: the pressed glyph kicks in its direction.
    Sequence arrowIntro, arrowIdle;
    Tween kickPrevious, kickNext;
    Vector2 previousRest, nextRest, previousGlyphRest, nextGlyphRest;

    // The animated "glyph" is the arrow's text (or first child image) inside the button.
    static RectTransform Glyph(Button arrow)
    {
        if (arrow == null) return null;
        var text = arrow.GetComponentInChildren<TMP_Text>(true);
        if (text != null && text.transform != arrow.transform) return text.rectTransform;
        return arrow.transform.childCount > 0 ? arrow.transform.GetChild(0) as RectTransform : null;
    }

    void PlayArrowIntro()
    {
        if (previous == null || next == null) return;
        StopArrowMotion();
        var left = (RectTransform)previous.transform;
        var right = (RectTransform)next.transform;
        var leftGroup = ArrowGroup(previous);
        var rightGroup = ArrowGroup(next);
        left.anchoredPosition = previousRest + Vector2.left * 22f;
        right.anchoredPosition = nextRest + Vector2.right * 22f;
        leftGroup.alpha = rightGroup.alpha = 0;
        arrowIntro = DOTween.Sequence().SetUpdate(true)
            .AppendInterval(0.18f)
            .Append(left.DOAnchorPos(previousRest, 0.42f).SetEase(Ease.OutBack, 1.4f))
            .Join(leftGroup.DOFade(1, 0.3f))
            .Insert(0.26f, right.DOAnchorPos(nextRest, 0.42f).SetEase(Ease.OutBack, 1.4f))
            .Insert(0.26f, rightGroup.DOFade(1, 0.3f))
            .OnComplete(StartArrowIdle);
    }

    static CanvasGroup ArrowGroup(Button arrow)
    {
        var group = arrow.GetComponent<CanvasGroup>();
        return group != null ? group : arrow.gameObject.AddComponent<CanvasGroup>();
    }

    void StartArrowIdle()
    {
        arrowIdle?.Kill();
        var left = Glyph(previous);
        var right = Glyph(next);
        if (left == null || right == null) return;
        left.anchoredPosition = previousGlyphRest;
        right.anchoredPosition = nextGlyphRest;
        arrowIdle = DOTween.Sequence().SetUpdate(true).SetLoops(-1, LoopType.Yoyo)
            .Append(left.DOAnchorPos(previousGlyphRest + Vector2.left * 5f, 0.75f).SetEase(Ease.InOutSine))
            .Join(right.DOAnchorPos(nextGlyphRest + Vector2.right * 5f, 0.75f).SetEase(Ease.InOutSine));
    }

    void Kick(Button arrow, int direction)
    {
        var glyph = Glyph(arrow);
        if (glyph == null) return;
        arrowIdle?.Kill();
        ref Tween kick = ref (direction < 0 ? ref kickPrevious : ref kickNext);
        kick?.Kill();
        glyph.anchoredPosition = direction < 0 ? previousGlyphRest : nextGlyphRest;
        kick = glyph.DOPunchAnchorPos(Vector2.right * direction * 12f, 0.32f, 6, 0.5f).SetUpdate(true)
            .OnComplete(StartArrowIdle);
    }

    // A clicked arrow would stay "selected" (and slightly enlarged by the hover motion);
    // release it so both arrows always rest at the same size.
    static void ReleaseSelection()
    {
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null) events.SetSelectedGameObject(null);
    }

    void StopArrowMotion()
    {
        arrowIntro?.Kill(); arrowIdle?.Kill(); kickPrevious?.Kill(); kickNext?.Kill();
        arrowIntro = arrowIdle = null; kickPrevious = kickNext = null;
        if (previous != null) { ((RectTransform)previous.transform).anchoredPosition = previousRest; ArrowGroup(previous).alpha = 1; var g = Glyph(previous); if (g != null) g.anchoredPosition = previousGlyphRest; }
        if (next != null) { ((RectTransform)next.transform).anchoredPosition = nextRest; ArrowGroup(next).alpha = 1; var g = Glyph(next); if (g != null) g.anchoredPosition = nextGlyphRest; }
    }

    /// <summary>Creates the two default arrows beside Opcao_0_0's portrait. Used at runtime
    /// when no arrows were placed in the scene, and by the Inspector button that puts
    /// editable arrows into the scene.</summary>
    public static Button[] CreateDefaultArrows(RectTransform viewport, RectTransform content, Vector2 size, float offset)
    {
        var reference = content.childCount > 0 ? (RectTransform)content.GetChild(0) : content;
        var template = content.GetComponentInChildren<TMP_Text>(true);
        var referenceButton = reference.GetComponent<Button>();
        var style = referenceButton != null ? referenceButton.targetGraphic as Image : null;
        Vector2 centre = PortraitCentre(viewport, reference);
        return new[]
        {
            // Same ">" glyph on both sides (mirrored on the left): fonts often draw "<" and ">"
            // at different sizes, which made the two arrows look unequal.
            CreateArrow(viewport, PreviousArrowName, ">", new Vector2(centre.x - offset, centre.y), size, template, style, true),
            CreateArrow(viewport, NextArrowName, ">", new Vector2(centre.x + offset, centre.y), size, template, style)
        };
    }

    // Centre of the option's portrait (or of the option itself), relative to the viewport centre.
    static Vector2 PortraitCentre(RectTransform viewport, RectTransform option)
    {
        // The portrait is the largest child image (the selection marker is a thin strip).
        RectTransform target = option;
        float best = 0;
        foreach (var image in option.GetComponentsInChildren<Image>(true))
        {
            if (image.transform == option) continue;
            float area = image.rectTransform.rect.width * image.rectTransform.rect.height;
            if (area > best) { best = area; target = image.rectTransform; }
        }
        Vector3 world = target.TransformPoint(target.rect.center);
        return (Vector2)viewport.InverseTransformPoint(world) - viewport.rect.center;
    }

    static Button CreateArrow(RectTransform viewport, string name, string glyph, Vector2 position, Vector2 size, TMP_Text template, Image style, bool mirrored = false)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.transform.SetParent(viewport, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        // No visible background: only the arrow glyph shows. The transparent image
        // stays as the click area so the arrow is easy to hit.
        var image = root.GetComponent<Image>();
        image.sprite = null;
        image.color = new Color(1, 1, 1, 0);
        var button = root.GetComponent<Button>();
        button.targetGraphic = image;
        root.AddComponent<CaseZeroButtonMotion>();

        var label = CreateText(glyph, root.transform, template, 40);
        label.gameObject.name = "Seta";
        if (mirrored) label.rectTransform.localScale = new Vector3(-1, 1, 1);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.fontStyle = FontStyles.Bold;
        return button;
    }

    static TMP_Text CreateText(string text, Transform parent, TMP_Text template, float size)
    {
        var root = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        root.transform.SetParent(parent, false);
        var label = root.GetComponent<TextMeshProUGUI>();
        if (template != null) { label.font = template.font; label.fontSharedMaterial = template.fontSharedMaterial; label.color = template.color; }
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = text;
        return label;
    }

    int ActiveCount()
    {
        int count = 0;
        for (int i = 0; i < options.Length; i++) if (options[i] != null && options[i].gameObject.activeSelf) count++;
        return count;
    }


    /// <summary>Arrow pressed: move to the neighbouring suspect (wrapping around) and select it.</summary>
    public void Step(int direction)
    {
        if (options == null || options.Length == 0) return;
        int start = current < 0 ? 0 : current;
        for (int tries = 1; tries <= options.Length; tries++)
        {
            int candidate = ((start + direction * tries) % options.Length + options.Length) % options.Length;
            var option = options[candidate];
            if (option == null || !option.gameObject.activeSelf) continue;
            if (option.IsInteractable()) option.onClick.Invoke(); // SelectSuspect(candidate) via the scene binding.
            Show(candidate, true, direction);
            return;
        }
    }

    /// <summary>Centre the given suspect. Direction follows the index unless an arrow says otherwise.</summary>
    public void Show(int index, bool animate) => Show(index, animate, 0);

    Color restingColor = Color.white;
    bool restingCaptured;
    /// <summary>Keeps the chosen suspect tinted with its button's Selected colour,
    /// even after the EventSystem selection moves to an arrow or another option.</summary>
    public void MarkChosen(int chosen)
    {
        if (options == null) return;
        if (!restingCaptured && options.Length > 0 && options[0] != null && options[0].targetGraphic != null)
        {
            restingColor = options[0].targetGraphic.color;
            restingCaptured = true;
        }
        for (int i = 0; i < options.Length; i++)
        {
            var button = options[i];
            if (button == null || button.targetGraphic == null) continue;
            Color tint = button.transition == Selectable.Transition.ColorTint ? button.colors.selectedColor : new Color(1f, 0.78f, 0.78f);
            button.targetGraphic.color = i == chosen ? restingColor * tint : restingColor;
        }
    }

    void Show(int index, bool animate, int direction)
    {
        if (content == null || options.Length == 0) return;
        if (index < 0 || index >= options.Length || !options[index].gameObject.activeSelf)
            index = current >= 0 && current < options.Length && options[current].gameObject.activeSelf ? current : FirstActive();
        if (index < 0) return;
        int previousIndex = current;
        bool moved = index != current;
        current = index;
        if (direction == 0) direction = index > previousIndex ? 1 : -1;

        slide?.Kill();
        bool play = animate && moved && previousIndex >= 0 && isActiveAndEnabled;
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] == null) continue;
            // Park everything that is not involved in the slide at the centre, invisible.
            if (!play || (i != index && i != previousIndex))
            {
                rects[i].anchoredPosition = slot;
                fades[i].alpha = i == index ? 1 : 0;
            }
        }
        if (play)
        {
            // Next (>) pushes the current suspect out to the left and brings the new one
            // in from the right; previous (<) does the mirror image.
            var outgoing = rects[previousIndex];
            var incoming = rects[index];
            incoming.anchoredPosition = slot + Vector2.right * travel * direction;
            fades[index].alpha = 0;
            slide = DOTween.Sequence().SetUpdate(true)
                .Join(outgoing.DOAnchorPos(slot + Vector2.left * travel * direction, slideDuration).SetEase(Ease.InOutCubic))
                .Join(fades[previousIndex].DOFade(0, slideDuration * 0.7f).SetEase(Ease.InSine))
                .Join(incoming.DOAnchorPos(slot, slideDuration).SetEase(Ease.OutCubic))
                .Join(fades[index].DOFade(1, slideDuration * 0.8f).SetEase(Ease.OutSine))
                .OnComplete(() => { outgoing.anchoredPosition = slot; });
        }
        // Only the centred option can be clicked; hidden ones must not steal the pointer.
        for (int i = 0; i < fades.Length; i++)
            if (fades[i] != null) fades[i].blocksRaycasts = fades[i].interactable = i == index;

        int count = ActiveCount();
        bool browsable = count > 1;
        previous.gameObject.SetActive(browsable);
        next.gameObject.SetActive(browsable);
    }

    int FirstActive()
    {
        for (int i = 0; i < options.Length; i++) if (options[i] != null && options[i].gameObject.activeSelf) return i;
        return -1;
    }

    void OnEnable()
    {
        if (content == null) return;
        Show(current, false);
        PlayArrowIntro();
    }
    void OnDisable() => StopArrowMotion();
    void OnDestroy() { slide?.Kill(); StopArrowMotion(); }
}
