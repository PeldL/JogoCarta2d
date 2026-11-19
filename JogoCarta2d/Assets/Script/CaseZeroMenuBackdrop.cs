using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Animated main-menu background: Resources/Menu/menu.mp4 loops behind the menu.
// The clip itself flashes white and reveals the killer; this component syncs a
// thunder clap and a brief lightning flicker over the whole menu with that moment.
[DisallowMultipleComponent]
public sealed class CaseZeroMenuBackdrop : MonoBehaviour
{
    [SerializeField, Tooltip("Second of menu.mp4 where the lightning flash starts.")]
    float flashTime = 7.75f;
    [SerializeField, Tooltip("Start points (seconds) of different claps inside Thunder.mp3; one is used per flash, in turn.")]
    float[] thunderStarts = { 17.15f, 2.15f, 27.7f, 38.6f, 33.9f };
    [SerializeField, Min(0.5f)] float thunderLength = 3.6f;
    [SerializeField, Range(0, 1)] float thunderVolume = 0.9f;
    [SerializeField, Range(0, 0.6f), Tooltip("How strongly the flash also lights up the menu buttons and title.")]
    float menuFlash = 0.22f;
    [SerializeField, Tooltip("Flip the video left-right (puts the girl on the right side).")]
    bool mirror = true;

    VideoPlayer player;
    RenderTexture target;
    RectTransform frame;
    RawImage screen;
    Image flicker;
    AudioSource thunderSource;
    double lastTime = -1;
    int thunderIndex;
    Tween breathe, reveal, thunderFade;
    Sequence flash;

    /// <summary>Puts the animated background behind everything in the menu canvas.</summary>
    /// <param name="clip">Background video (CaseZeroMenu's Inspector field); falls back to Resources/Menu/menu.</param>
    /// <param name="thunderClip">Thunder sound (Inspector field); falls back to Resources/Audio/Thunder.</param>
    public static CaseZeroMenuBackdrop Install(Transform canvasRoot, VideoClip clip = null, AudioClip thunderClip = null)
    {
        if (canvasRoot == null) return null;
        if (clip == null) clip = Resources.Load<VideoClip>("Menu/menu");
        if (clip == null) { Debug.LogWarning("[Caso Zero] Vídeo do menu não definido (campo Menu Video do CaseZeroMenu); menu sem vídeo de fundo."); return null; }
        var existing = canvasRoot.GetComponentInChildren<CaseZeroMenuBackdrop>(true);
        if (existing != null) return existing;

        var root = new GameObject("Fundo animado • vídeo", typeof(RectTransform), typeof(CaseZeroMenuBackdrop));
        root.transform.SetParent(canvasRoot, false);
        root.transform.SetAsFirstSibling();
        var backdrop = root.GetComponent<CaseZeroMenuBackdrop>();
        backdrop.Build(clip, canvasRoot, thunderClip);
        return backdrop;
    }

    void Build(VideoClip clip, Transform canvasRoot, AudioClip thunderClip)
    {
        var rect = (RectTransform)transform;
        Stretch(rect);
        // Solid black under the video: covers the moments before the first frame arrives.
        var black = gameObject.AddComponent<Image>();
        black.color = Color.black;
        black.raycastTarget = false;

        // The frame keeps 16:9 and covers the whole screen (cropping instead of letterboxing).
        var frameObject = new GameObject("Vídeo", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
        frameObject.transform.SetParent(transform, false);
        frame = (RectTransform)frameObject.transform;
        var fitter = frameObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = clip.width > 0 && clip.height > 0 ? clip.width / (float)clip.height : 16f / 9f;
        screen = frameObject.GetComponent<RawImage>();
        screen.raycastTarget = false;
        // Mirrored horizontally so the girl sits on the right, clear of the menu buttons.
        screen.uvRect = mirror ? new Rect(1, 0, -1, 1) : new Rect(0, 0, 1, 1);
        screen.color = new Color(1, 1, 1, 0); // Revealed once the first frame is ready.

        target = new RenderTexture((int)Mathf.Max(16, clip.width), (int)Mathf.Max(16, clip.height), 0) { name = "Menu video" };
        target.Create();
        screen.texture = target;

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = true;
        player.skipOnDrop = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = target;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.clip = clip;
        player.prepareCompleted += OnPrepared;
        player.Prepare();

        // Full-screen white layer drawn above the menu for the lightning flicker.
        var flickerObject = new GameObject("Relâmpago", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        flickerObject.transform.SetParent(canvasRoot, false);
        Stretch((RectTransform)flickerObject.transform);
        flicker = flickerObject.GetComponent<Image>();
        flicker.color = new Color(1, 1, 1, 0);
        flicker.raycastTarget = false;

        var thunder = thunderClip != null ? thunderClip : Resources.Load<AudioClip>("Audio/Thunder");
        if (thunder != null)
        {
            thunderSource = gameObject.AddComponent<AudioSource>();
            thunderSource.playOnAwake = false;
            thunderSource.loop = false;
            thunderSource.spatialBlend = 0;
            thunderSource.clip = thunder;
            thunder.LoadAudioData();
        }
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    void OnPrepared(VideoPlayer source)
    {
        source.Play();
        reveal = screen.DOFade(1, 0.8f).SetEase(Ease.InOutSine).SetUpdate(true);
        // Very slow push-in and back, so the still parts of the shot never feel frozen.
        breathe = frame.DOScale(1.035f, 9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
    }

    void Update()
    {
        if (player == null || !player.isPlaying) return;
        double time = player.time;
        // Fires once per loop, when playback crosses the flash moment.
        if (lastTime >= 0 && lastTime < flashTime && time >= flashTime) Lightning();
        lastTime = time;
    }

    void Lightning()
    {
        PlayThunder();
        flash?.Kill(true);
        flicker.transform.SetAsLastSibling(); // Stay above panels opened after the menu started.
        // Two quick blips, like a real strike, then the menu settles back into the dark.
        flash = DOTween.Sequence().SetUpdate(true)
            .Append(flicker.DOFade(menuFlash, 0.05f))
            .Append(flicker.DOFade(menuFlash * 0.25f, 0.07f))
            .Append(flicker.DOFade(menuFlash * 0.9f, 0.05f))
            .Append(flicker.DOFade(0, 0.45f).SetEase(Ease.OutQuad))
            .Insert(0, frame.DOPunchScale(Vector3.one * 0.025f, 0.5f, 4, 0.6f));
    }

    void PlayThunder()
    {
        if (thunderSource == null || thunderSource.clip == null || thunderStarts == null || thunderStarts.Length == 0) return;
        float start = Mathf.Clamp(thunderStarts[thunderIndex++ % thunderStarts.Length], 0, Mathf.Max(0, thunderSource.clip.length - 0.1f));
        float volume = thunderVolume * CaseZeroSettingsPanel.SfxVolume;
        thunderFade?.Kill();
        thunderSource.Stop();
        thunderSource.volume = volume;
        thunderSource.time = start;
        thunderSource.Play();
        float fade = Mathf.Min(1.2f, thunderLength * 0.4f);
        thunderFade = DOTween.Sequence().SetUpdate(true)
            .AppendInterval(thunderLength - fade)
            .Append(DOTween.To(() => thunderSource.volume, value => thunderSource.volume = value, 0f, fade))
            .OnComplete(() => { if (thunderSource != null) thunderSource.Stop(); });
    }

    void OnDestroy()
    {
        breathe?.Kill(); reveal?.Kill(); flash?.Kill(); thunderFade?.Kill();
        if (player != null) player.prepareCompleted -= OnPrepared;
        if (flicker != null) Destroy(flicker.gameObject);
        if (target != null) { target.Release(); Destroy(target); }
    }
}
