using DG.Tweening;
using UnityEngine;

// Animates the existing scene objects, without creating interface elements.
[RequireComponent(typeof(CanvasGroup))]
public sealed class CaseZeroPanelMotion : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float duration = 0.22f;
    [SerializeField, Range(0.9f, 1f)] float initialScale = 0.985f;
    CanvasGroup group;
    Vector3 restingScale;
    Sequence motion;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        restingScale = transform.localScale;
    }

    void OnEnable()
    {
        motion?.Kill();
        group.alpha = 0;
        transform.localScale = restingScale * initialScale;
        motion = DOTween.Sequence().SetUpdate(true)
            .Join(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, duration))
            .Join(transform.DOScale(restingScale, duration).SetEase(Ease.OutCubic));
    }

    void OnDisable()
    {
        motion?.Kill();
        if (group != null) group.alpha = 1;
        transform.localScale = restingScale;
    }
}
