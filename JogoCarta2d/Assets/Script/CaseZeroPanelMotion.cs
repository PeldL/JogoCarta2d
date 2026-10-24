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
    [SerializeField, Min(0)] float childTravel = 22f;
    CanvasGroup group;
    Vector3 restingScale;
    Sequence motion;
    readonly List<RectTransform> children = new List<RectTransform>();
    readonly List<Vector2> positions = new List<Vector2>();
    readonly List<CanvasGroup> childGroups = new List<CanvasGroup>();
    readonly List<float> childAlphas = new List<float>();
    bool captured;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        restingScale = transform.localScale;
        captured = true;
    }

    void OnEnable() => Play();

    public void Play()
    {
        if (!captured || !isActiveAndEnabled) return;
        Restore();
        Canvas.ForceUpdateCanvases();
        group.alpha = 0;
        transform.localScale = restingScale * Mathf.Clamp(initialScale, 0.9f, 1f);
        float time = Mathf.Max(duration, 0.38f);
        motion = DOTween.Sequence().SetUpdate(true)
            .Join(group.DOFade(1f, time))
            .Join(transform.DOScale(restingScale, time).SetEase(Ease.OutCubic));
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
            Vector2 origin = rect.anchoredPosition;
            children.Add(rect); positions.Add(origin);
            rect.anchoredPosition = origin + Vector2.down * childTravel;
            float delay = 0.06f + Mathf.Min(order++, 6) * 0.045f;
            motion.Insert(delay, rect.DOAnchorPos(origin, time).SetEase(Ease.OutCubic));
            var childGroup = child.GetComponent<CanvasGroup>();
            if (childGroup == null) childGroup = child.gameObject.AddComponent<CanvasGroup>();
            float targetAlpha = childGroup.alpha;
            childGroups.Add(childGroup); childAlphas.Add(targetAlpha);
            childGroup.alpha = 0;
            motion.Insert(delay, childGroup.DOFade(targetAlpha, time * 0.75f).SetEase(Ease.OutSine));
        }
    }

    void Restore()
    {
        motion?.Kill(); motion = null;
        for (int i = 0; i < children.Count; i++)
            if (children[i] != null) children[i].anchoredPosition = positions[i];
        children.Clear(); positions.Clear();
        for (int i = 0; i < childGroups.Count; i++)
            if (childGroups[i] != null) childGroups[i].alpha = childAlphas[i];
        childGroups.Clear(); childAlphas.Clear();
        if (group != null) group.alpha = 1;
        if (captured) transform.localScale = restingScale;
    }
    void OnDisable() => Restore();
    void OnDestroy() => motion?.Kill();
}
