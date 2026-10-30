using System.Collections;
using Pixit.TMProVisualFX;
using TMPro;
using UnityEngine;

/// <summary>Short, readable Pixit glitch reveal for authored dialogue text.</summary>
[DisallowMultipleComponent]
public sealed class CaseZeroDialogueFX : MonoBehaviour
{
    [SerializeField] float revealCharactersPerSecond = 42f;
    [SerializeField] float glitchAmount = 1.1f;
    TMProVisualFX visualFx;
    Coroutine settle;

    void Awake()
    {
        visualFx = GetComponent<TMProVisualFX>();
        if (visualFx == null) visualFx = gameObject.AddComponent<TMProVisualFX>();
        visualFx.Stop();
    }

    public void Reveal()
    {
        StartReveal(0, glitchAmount, revealCharactersPerSecond);
    }

    public void Reveal(float amount, float speed)
    {
        StartReveal(0, amount, speed);
    }

    public void RevealInterview()
    {
        if (visualFx == null) return;
        if (settle != null) StopCoroutine(settle);
        visualFx.PlaySoftDialogue(58f);
        settle = StartCoroutine(Settle());
    }

    public void RevealRadioTransmission()
    {
        // Keep the weak transmission alive throughout tuning; the next clean radio
        // dialogue calls Reveal() and replaces this effect as soon as dispatch connects.
        StartReveal(0, 1.15f, 64f, true);
    }

    void StartReveal(int glitchCharacterCount, float amount, float speed, bool holdUntilReplaced = false)
    {
        if (visualFx == null) return;
        if (settle != null) StopCoroutine(settle);
        visualFx.PlayDialogueGlitch(amount, 4f, speed, glitchCharacterCount);
        settle = holdUntilReplaced ? null : StartCoroutine(Settle());
    }

    IEnumerator Settle()
    {
        TMP_Text text = visualFx.Text;
        if (text != null) text.ForceMeshUpdate();
        int count = text != null ? text.textInfo.characterCount : 0;
        yield return new WaitForSecondsRealtime(Mathf.Clamp(count / revealCharactersPerSecond + 0.18f, 0.25f, 8f));
        visualFx.Stop();
        settle = null;
    }

    void OnDisable()
    {
        if (settle != null) StopCoroutine(settle);
        settle = null;
        if (visualFx != null) visualFx.Stop();
    }
}
