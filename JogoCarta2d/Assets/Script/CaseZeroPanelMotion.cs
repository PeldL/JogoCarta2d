using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Owns only its own tweens; disabling always restores the authored layout.
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public sealed class CaseZeroPanelMotion : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float duration = 0.42f;
    [SerializeField, Range(0.9f, 1f)] float initialScale = 0.96f;
    [Header("Paper feel • relative to the authored layout")]
    [SerializeField, Min(0)] float entranceTravel = 14f;
    [SerializeField, Range(0, 2)] float paperTilt = 0.65f;
    [SerializeField] bool rigidObject;
    [SerializeField, Tooltip("Book contents: reveal in place, without paper travel or tilt.")]
    bool pageContent;
    [SerializeField, Tooltip("Map page: fade in place, without the book's paper movement.")]
    bool mapReveal;
    CanvasGroup group;
    Vector3 restingScale;
    RectTransform rect;
    Vector2 restingPosition;
    Quaternion restingRotation;
    Sequence motion;
    readonly List<CanvasGroup> childGroups = new List<CanvasGroup>();
    readonly List<float> childAlphas = new List<float>();
    bool captured;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        restingScale = transform.localScale;
        rect = transform as RectTransform;
        if (rect != null) restingPosition = rect.anchoredPosition;
        restingRotation = transform.localRotation;
        captured = true;
    }

    void OnEnable() => Play();

    public void UsePageReveal()
    {
        pageContent = true;
        if (isActiveAndEnabled) Play();
    }

    public void UseMapReveal()
    {
        mapReveal = true;
        pageContent = false;
        if (isActiveAndEnabled) Play();
    }

    public void Play()
    {
        if (!captured || !isActiveAndEnabled) return;
        Restore();
        Canvas.ForceUpdateCanvases();
        group.alpha = 0;
        if (mapReveal)
        {
            // Keep the location diagram fixed; only fade the map into view.
            transform.localScale = restingScale;
            motion = DOTween.Sequence().SetUpdate(true)
                .Append(group.DOFade(1, 0.34f).SetEase(Ease.InOutSine));
            return;
        }
        if (pageContent)
        {
            // One fade for the entire page, including nested scroll lists and buttons.
            // Only this parent owns the reveal scale; hover feedback stays on the buttons.
            transform.localScale = restingScale * 0.985f;
            motion = DOTween.Sequence().SetUpdate(true)
                .Append(group.DOFade(1, 0.32f).SetEase(Ease.InOutSine))
                .Join(transform.DOScale(restingScale, 0.36f).SetEase(Ease.OutSine));
            return;
        }
        transform.localScale = restingScale * (rigidObject ? 1 : Mathf.Clamp(initialScale, 0.985f, 1f));
        // Paper settles vertically; the phone slides in from the side, without a bounce.
        Vector2 offset = (rigidObject ? Vector2.right : Vector2.down) * entranceTravel;
        if (rect != null) rect.anchoredPosition = restingPosition + offset;
        if (!rigidObject) transform.localRotation = restingRotation * Quaternion.Euler(0, 0, -paperTilt);
        float time = rigidObject ? 0.28f : Mathf.Max(duration, 0.38f);
        motion = DOTween.Sequence().SetUpdate(true)
            .Join(group.DOFade(1f, time * 0.55f).SetEase(Ease.OutSine))
            .Join(transform.DOScale(restingScale, time).SetEase(Ease.OutBack, 0.4f));
        if (rect != null)
            motion.Join(rect.DOAnchorPos(restingPosition, time).SetEase(rigidObject ? Ease.OutCubic : Ease.OutBack, 0.5f));
        if (!rigidObject)
            motion.Join(transform.DOLocalRotateQuaternion(restingRotation, time).SetEase(Ease.OutSine));
        // Direct children only: do not double-animate text inside buttons or layout-driven rows.
        if (GetComponent<LayoutGroup>() != null) return;
        int order = 0;
        foreach (Transform child in transform)
        {
            var rect = child as RectTransform;
            if (rect == null || !child.gameObject.activeSelf ||
                child.GetComponent<CaseZeroPanelMotion>() != null ||
                (child.TryGetComponent<CaseZeroButtonMotion>(out var buttonMotion) && buttonMotion.IsFolderTab) ||
                (child.GetComponent<Selectable>() == null && child.GetComponent<TMPro.TMP_Text>() == null)) continue;
            // Content stays attached to its page. Only its opacity is staggered.
            float delay = 0.025f + Mathf.Min(order++, 5) * 0.022f;
            var childGroup = child.GetComponent<CanvasGroup>();
            if (childGroup == null) childGroup = child.gameObject.AddComponent<CanvasGroup>();
            float targetAlpha = childGroup.alpha;
            childGroups.Add(childGroup); childAlphas.Add(targetAlpha);
            childGroup.alpha = 0;
            motion.Insert(delay, childGroup.DOFade(targetAlpha, time * 0.75f).SetEase(Ease.OutSine));
        }
    }

    // Explicit close: callers retain immediate page switching, while dismissals can settle out.
    public void Hide()
    {
        if (!isActiveAndEnabled) { gameObject.SetActive(false); return; }
        motion?.Kill();
        motion = DOTween.Sequence().SetUpdate(true)
            .Append(group.DOFade(0, 0.16f).SetEase(Ease.InSine));
        if (rect != null && !pageContent)
            motion.Join(rect.DOAnchorPos(restingPosition + (rigidObject ? Vector2.right : Vector2.down) * entranceTravel * 0.7f, 0.18f).SetEase(Ease.InQuad));
        motion.OnComplete(() => gameObject.SetActive(false));
    }

    void Restore()
    {
        motion?.Kill(); motion = null;
        for (int i = 0; i < childGroups.Count; i++)
            if (childGroups[i] != null) childGroups[i].alpha = childAlphas[i];
        childGroups.Clear(); childAlphas.Clear();
        if (group != null) group.alpha = 1;
        if (captured)
        {
            transform.localScale = restingScale;
            transform.localRotation = restingRotation;
            if (rect != null) rect.anchoredPosition = restingPosition;
        }
    }
    void OnDisable() => Restore();
    void OnDestroy() => motion?.Kill();
}
