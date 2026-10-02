using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All three dots and their label are authored in the scene and can be restyled there.
public sealed class CaseZeroPhonePresentation : MonoBehaviour
{
    [SerializeField] TMP_Text message;
    [SerializeField] GameObject typingIndicator;
    [SerializeField] RectTransform[] dots;
    [SerializeField] Button confirmButton;
    [SerializeField, Min(0.2f)] float thinkingDuration = 1.6f;
    [SerializeField, Min(0f)] float jumpHeight = 9f;
    [SerializeField, Min(0.1f)] float jumpDuration = 0.24f;
    Vector2[] restingPositions;
    Sequence[] dotAnimations;
    Sequence reveal;
    string pendingMessage;
    public bool IsRevealing { get; private set; }

    void Awake()
    {
        restingPositions = new Vector2[dots.Length];
        dotAnimations = new Sequence[dots.Length];
        for (int i = 0; i < dots.Length; i++) restingPositions[i] = dots[i].anchoredPosition;
    }

    public void PlayMessage(string text)
    {
        ShowImmediate(text);
        if (!isActiveAndEnabled) return;
        pendingMessage = text;
        IsRevealing = true;
        message.text = "";
        confirmButton.interactable = false;
        typingIndicator.SetActive(true);
        for (int i = 0; i < dots.Length; i++)
        {
            int index = i;
            Vector2 origin = restingPositions[i];
            dotAnimations[i] = DOTween.Sequence().SetUpdate(true)
                .AppendInterval(i * 0.12f)
                .Append(DOTween.To(() => dots[index].anchoredPosition,
                    value => dots[index].anchoredPosition = value,
                    origin + Vector2.up * jumpHeight, jumpDuration).SetEase(Ease.OutSine))
                .Append(DOTween.To(() => dots[index].anchoredPosition,
                    value => dots[index].anchoredPosition = value,
                    origin, jumpDuration).SetEase(Ease.InSine))
                .AppendInterval((dots.Length - 1 - i) * 0.12f + 0.15f)
                .SetLoops(-1);
        }
        reveal = DOTween.Sequence().SetUpdate(true)
            .AppendInterval(thinkingDuration)
            .AppendCallback(() => { StopDots(); message.text = pendingMessage; message.alpha = 0; })
            .Append(DOTween.To(() => message.alpha, value => message.alpha = value, 1f, 0.18f))
            .OnComplete(() => { IsRevealing = false; confirmButton.interactable = true; });
    }

    public void ShowImmediate(string text)
    {
        reveal?.Kill();
        reveal = null;
        StopDots();
        IsRevealing = false;
        pendingMessage = text;
        message.text = text;
        message.alpha = 1;
        confirmButton.interactable = true;
    }

    void StopDots()
    {
        if (dotAnimations != null)
            for (int i = 0; i < dotAnimations.Length; i++)
            {
                dotAnimations[i]?.Kill();
                dotAnimations[i] = null;
                dots[i].anchoredPosition = restingPositions[i];
            }
        typingIndicator.SetActive(false);
    }

    // Leaving the page skips the decorative delay; reopening never loses the message.
    void OnDisable() => ShowImmediate(pendingMessage ?? "");
}
