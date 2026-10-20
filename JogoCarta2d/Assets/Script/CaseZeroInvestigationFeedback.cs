using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Decorative feedback owns its tweens, audio and temporary overlay; it never changes case state.
public sealed class CaseZeroInvestigationFeedback : MonoBehaviour
{
    [SerializeField, Min(0)] float shakeStrength = 10;
    [SerializeField, Min(0.1f)] float shakeDuration = 0.42f;
    [SerializeField, Min(1)] float ringInterval = 3.2f;
    [SerializeField, Range(0, 12)] float phoneTilt = 7;
    CaseZeroView view;
    GameObject overlayRoot;
    Canvas overlayCanvas;
    CanvasGroup alert;
    RectTransform alertRect;
    TMP_Text alertTitle, alertMessage;
    Image accent, noticeBackground;
    Sequence notice;
    Tween shake;
    AudioSource ringSource, errorSource;
    AudioClip ringtone, errorTone, victoryTone;
    readonly List<GameObject> confetti = new List<GameObject>();
    readonly List<Sequence> confettiTweens = new List<Sequence>();
    bool ringing;
    float nextRing, ringTime;
    RectTransform phoneButton, phonePanel;
    Quaternion buttonRotation, panelRotation;
    readonly List<RectTransform> shakeRects = new List<RectTransform>();
    readonly List<Transform> shakeCameras = new List<Transform>();
    Vector2 lastOffset;

    public void Initialize(CaseZeroView owner)
    {
        view = owner;
        phoneButton = owner.phoneAttentionButton != null ? owner.phoneAttentionButton.transform as RectTransform : null;
        phonePanel = owner.phoneRoot != null ? owner.phoneRoot.transform as RectTransform : null;
        if (phoneButton != null) buttonRotation = phoneButton.localRotation;
        if (phonePanel != null) panelRotation = phonePanel.localRotation;
        ringSource = gameObject.AddComponent<AudioSource>();
        errorSource = gameObject.AddComponent<AudioSource>();
        ringSource.playOnAwake = errorSource.playOnAwake = false;
        ringSource.spatialBlend = errorSource.spatialBlend = 0;
        ringSource.loop = errorSource.loop = false;
        ringtone = MakeTone(true);
        errorTone = MakeTone(false);
        victoryTone = MakeVictoryTone();
        ringSource.clip = ringtone;
        errorSource.clip = errorTone;
        ApplyVolume();
        BuildAlert(owner.documentTitle != null ? owner.documentTitle.font : null);
    }

    void OnEnable() => CaseZeroSettingsPanel.VolumesChanged += ApplyVolume;
    void ApplyVolume()
    {
        if (ringSource != null) ringSource.volume = CaseZeroSettingsPanel.SfxVolume * 0.7f;
        if (errorSource != null) errorSource.volume = CaseZeroSettingsPanel.SfxVolume * 0.65f;
    }

    public void SetPhoneRinging(bool value)
    {
        if (ringing == value) return;
        ringing = value;
        ringTime = 0;
        nextRing = 0;
        if (!value)
        {
            if (ringSource != null) ringSource.Stop();
            RestorePhone();
        }
    }

    void Update()
    {
        if (!ringing) return;
        float delta = Time.unscaledDeltaTime;
        ringTime += delta;
        nextRing -= delta;
        if (nextRing <= 0)
        {
            if (ringSource != null) ringSource.Play();
            nextRing = ringInterval;
        }
        // Short vibration bursts rather than continuous rotation. Position and scale remain owned by UI motion.
        float burst = Mathf.Repeat(ringTime, ringInterval);
        float angle = burst < 0.8f ? Mathf.Sin(burst * 65) * phoneTilt : 0;
        if (phoneButton != null) phoneButton.localRotation = buttonRotation * Quaternion.Euler(0, 0, angle);
        if (phonePanel != null) phonePanel.localRotation = panelRotation * Quaternion.Euler(0, 0, angle * 0.35f);
    }

    void RestorePhone()
    {
        if (phoneButton != null) phoneButton.localRotation = buttonRotation;
        if (phonePanel != null) phonePanel.localRotation = panelRotation;
    }

    public void ShowWrongAccusation(int remaining, bool failed)
    {
        if (alert == null) return;
        notice?.Kill();
        ClearCelebration();
        noticeBackground.color = new Color(0.1f, 0.065f, 0.055f, 0.97f);
        alertTitle.color = new Color(1, 0.93f, 0.86f);
        alertTitle.text = failed ? "INVESTIGAÇÃO ENCERRADA" : "ACUSAÇÃO INCORRETA";
        alertMessage.text = failed ? "As três tentativas acabaram. Você pode tentar o caso novamente."
            : $"Restam {remaining} tentativa(s). Revise as provas e os depoimentos.";
        accent.color = failed ? new Color(0.75f, 0.14f, 0.14f) : new Color(0.86f, 0.29f, 0.12f);
        PlayNotice(failed ? 4.5f : 3.5f, false);
        if (errorSource != null) { errorSource.Stop(); errorSource.Play(); }
        ShakeScreen();
    }

    public void ShowVictory(int score, string rating, bool campaignComplete)
    {
        if (alert == null) return;
        StopShake();
        ClearCelebration();
        SetPhoneRinging(false);
        alertTitle.text = campaignComplete ? "CAMPANHA CONCLUÍDA!" : "CASO RESOLVIDO!";
        alertMessage.text = campaignComplete
            ? $"Parabéns, detetive! Os três casos estão resolvidos.\n{score}/1000 pontos neste caso • {rating}"
            : $"Excelente trabalho, detetive!\n{score}/1000 pontos • {rating}";
        noticeBackground.color = new Color(0.035f, 0.14f, 0.105f, 0.98f);
        accent.color = new Color(1, 0.78f, 0.25f);
        alertTitle.color = new Color(1, 0.86f, 0.42f);
        PlayNotice(campaignComplete ? 6 : 4.5f, true);
        Celebrate();
        if (errorSource != null && victoryTone != null)
        {
            errorSource.Stop();
            errorSource.PlayOneShot(victoryTone);
        }
    }

    void PlayNotice(float hold, bool victory)
    {
        notice?.Kill();
        alert.gameObject.SetActive(true);
        alert.alpha = 0;
        alertRect.anchoredPosition = new Vector2(0, -145);
        alertRect.localScale = Vector3.one * (victory ? 0.8f : 0.95f);
        notice = DOTween.Sequence().SetUpdate(true)
            .Append(alert.DOFade(1, 0.12f))
            .Join(alertRect.DOAnchorPos(new Vector2(0, -110), 0.3f).SetEase(Ease.OutCubic))
            .Join(alertRect.DOScale(1, victory ? 0.5f : 0.25f).SetEase(Ease.OutBack))
            .AppendInterval(hold)
            .Append(alert.DOFade(0, 0.3f))
            .OnComplete(() => alert.gameObject.SetActive(false));
    }

    void Celebrate()
    {
        for (int i = 0; i < 24; i++)
        {
            var piece = new GameObject("Confete de vitória", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            piece.transform.SetParent(overlayRoot.transform, false);
            piece.transform.SetAsFirstSibling(); // Keep the notice text clear.
            confetti.Add(piece);
            var rect = piece.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(i % 2 == 0 ? 9 : 6, 16);
            float x = (i - 11.5f) * 49;
            rect.anchoredPosition = new Vector2(x, 25 + (i % 4) * 18);
            var image = piece.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = i % 3 == 0 ? new Color(1, 0.8f, 0.3f) : i % 3 == 1
                ? new Color(0.3f, 0.9f, 0.65f) : new Color(1, 0.95f, 0.7f);
            float duration = 1.7f + (i % 5) * 0.13f;
            var sequence = DOTween.Sequence().SetUpdate(true)
                .AppendInterval((i % 6) * 0.04f)
                .Append(rect.DOAnchorPos(new Vector2(x + (i % 2 == 0 ? 85 : -85), -600), duration).SetEase(Ease.InQuad))
                .Join(rect.DOLocalRotate(new Vector3(0, 0, i % 2 == 0 ? 300 : -300), duration, RotateMode.FastBeyond360))
                .Insert(duration - 0.4f, image.DOFade(0, 0.6f))
                .OnComplete(() => { if (piece != null) Destroy(piece); });
            confettiTweens.Add(sequence);
        }
    }

    void ClearCelebration()
    {
        foreach (var tween in confettiTweens) tween?.Kill();
        confettiTweens.Clear();
        foreach (var piece in confetti) if (piece != null) Destroy(piece);
        confetti.Clear();
    }

    void ShakeScreen()
    {
        StopShake();
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas == overlayCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
            if (canvas.sortingOrder >= 32766) continue; // Loading and transition overlays.
            foreach (Transform child in canvas.transform)
                if (child is RectTransform rect && child.gameObject.activeInHierarchy && child != overlayRoot.transform)
                    shakeRects.Add(rect);
        }
        foreach (var camera in Camera.allCameras)
            if (camera.isActiveAndEnabled && !shakeCameras.Contains(camera.transform)) shakeCameras.Add(camera.transform);
        float elapsed = 0;
        shake = DOTween.To(() => elapsed, time =>
        {
            elapsed = time;
            float decay = 1 - time / shakeDuration;
            Vector2 offset = new Vector2(Mathf.Sin(time * 105), Mathf.Sin(time * 137)) * shakeStrength * decay;
            ApplyShake(offset);
        }, shakeDuration, shakeDuration).SetEase(Ease.Linear).SetUpdate(true)
            .OnComplete(StopShake);
    }

    void ApplyShake(Vector2 offset)
    {
        Vector2 delta = offset - lastOffset;
        foreach (var rect in shakeRects)
            if (rect != null) rect.anchoredPosition += delta;
        foreach (var camera in shakeCameras)
            if (camera != null) camera.localPosition += new Vector3(delta.x, delta.y, 0) * 0.002f;
        lastOffset = offset;
    }
    void StopShake()
    {
        shake?.Kill(); shake = null;
        ApplyShake(Vector2.zero);
        shakeRects.Clear(); shakeCameras.Clear();
    }

    public void ResetFeedback()
    {
        SetPhoneRinging(false);
        ClearCelebration();
        if (errorSource != null) errorSource.Stop();
        notice?.Kill(); notice = null;
        StopShake();
        if (alert != null) alert.gameObject.SetActive(false);
    }
    void OnDisable()
    {
        CaseZeroSettingsPanel.VolumesChanged -= ApplyVolume;
        ResetFeedback();
    }
    void OnDestroy()
    {
        ResetFeedback();
        if (overlayRoot != null) Destroy(overlayRoot);
        if (ringSource != null) Destroy(ringSource);
        if (errorSource != null) Destroy(errorSource);
        if (ringtone != null) Destroy(ringtone);
        if (errorTone != null) Destroy(errorTone);
        if (victoryTone != null) Destroy(victoryTone);
    }

    static AudioClip MakeTone(bool phone)
    {
        const int rate = 22050;
        var samples = new float[Mathf.RoundToInt(rate * (phone ? 0.85f : 0.28f))];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            if (phone)
            {
                float phase = Mathf.Repeat(t, 0.38f);
                float envelope = phase < 0.24f ? Mathf.Sin(Mathf.PI * phase / 0.24f) : 0;
                envelope *= Mathf.Clamp01((samples.Length - 1 - i) / (rate * 0.015f));
                samples[i] = envelope * 0.19f * (Mathf.Sin(2 * Mathf.PI * 480 * t) + Mathf.Sin(2 * Mathf.PI * 640 * t));
            }
            else
                samples[i] = Mathf.Sin(2 * Mathf.PI * (180 * t - 160 * t * t)) * 0.3f * Mathf.Sin(Mathf.PI * i / (samples.Length - 1));
        }
        var clip = AudioClip.Create(phone ? "Caso Zero • Telefone" : "Caso Zero • Acusação incorreta", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip MakeVictoryTone()
    {
        const int rate = 22050;
        var notes = new[] { 523.25f, 659.25f, 783.99f, 1046.5f };
        var samples = new float[rate];
        for (int i = 0; i < samples.Length; i++)
        {
            float time = i / (float)rate;
            float sample = 0;
            for (int note = 0; note < notes.Length; note++)
            {
                float local = time - note * 0.14f;
                float length = note == notes.Length - 1 ? 0.58f : 0.3f;
                if (local < 0 || local >= length) continue;
                float envelope = Mathf.Sin(Mathf.PI * local / length);
                sample += Mathf.Sin(2 * Mathf.PI * notes[note] * local) * envelope * 0.17f;
            }
            samples[i] = sample;
        }
        var clip = AudioClip.Create("Caso Zero • Vitória", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void BuildAlert(TMP_FontAsset font)
    {
        overlayRoot = new GameObject("Caso Zero • Aviso de investigação", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(overlayRoot, gameObject.scene);
        overlayCanvas = overlayRoot.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 32000;
        var scaler = overlayRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var panel = new GameObject("Aviso", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(overlayRoot.transform, false);
        alertRect = panel.GetComponent<RectTransform>();
        alertRect.anchorMin = alertRect.anchorMax = new Vector2(0.5f, 1);
        alertRect.sizeDelta = new Vector2(920, 175);
        noticeBackground = panel.GetComponent<Image>();
        noticeBackground.color = new Color(0.1f, 0.065f, 0.055f, 0.97f);
        panel.GetComponent<Image>().raycastTarget = false;
        alert = panel.GetComponent<CanvasGroup>();
        alert.interactable = alert.blocksRaycasts = false;
        var stripe = new GameObject("Faixa", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        stripe.transform.SetParent(panel.transform, false);
        var stripeRect = stripe.GetComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0, 0); stripeRect.anchorMax = new Vector2(0, 1);
        stripeRect.pivot = new Vector2(0, 0.5f); stripeRect.sizeDelta = new Vector2(9, 0);
        accent = stripe.GetComponent<Image>(); accent.raycastTarget = false;
        alertTitle = Label("Título", panel.transform, font, new Vector2(0, 38), new Vector2(850, 54), 35);
        alertTitle.fontStyle = FontStyles.Bold;
        alertMessage = Label("Mensagem", panel.transform, font, new Vector2(0, -28), new Vector2(850, 72), 26);
        panel.SetActive(false);
    }
    static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
        var label = root.GetComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1, 0.93f, 0.86f); label.raycastTarget = false;
        return label;
    }
}
