using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public static class FadeUtils
{
    // Fade genérico via callback — funciona com qualquer coisa que tenha alpha
    public static IEnumerator Fade(MonoBehaviour host, Action<float> setter, float from, float to, float duration)
    {
        float elapsed = 0f;
        setter(from);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            setter(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        setter(to);
    }

    // Fade de Image
    public static IEnumerator FadeImage(MonoBehaviour host, Image img, float from, float to, float duration)
    {
        yield return host.StartCoroutine(Fade(host, a =>
        {
            Color c = img.color; c.a = a; img.color = c;
        }, from, to, duration));
    }

    // Fade de TextMeshProUGUI
    public static IEnumerator FadeTMP(MonoBehaviour host, TextMeshProUGUI tmp, float from, float to, float duration)
    {
        yield return host.StartCoroutine(Fade(host, a =>
        {
            Color c = tmp.color; c.a = a; tmp.color = c;
        }, from, to, duration));
    }

    // Fade de CanvasGroup
    public static IEnumerator FadeCanvasGroup(MonoBehaviour host, CanvasGroup cg, float from, float to, float duration)
    {
        yield return host.StartCoroutine(Fade(host, a => cg.alpha = a, from, to, duration));
    }

    // Garante que CanvasGroup existe no GameObject
    public static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }
}
