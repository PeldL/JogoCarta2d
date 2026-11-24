using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Screen-space effects shared by the whole game: discovery sparks, flashes, location title
// cards, drifting dust and the pulse on clickable clues. Everything runs on unscaled time.
public static class CaseZeroUiFx
{
    // Palette taken from the game's existing UI: warm paper, amber accent, dark ink.
    public static readonly Color Paper = new Color(0.96f, 0.92f, 0.82f);
    public static readonly Color Amber = new Color(1f, 0.8f, 0.3f);
    public static readonly Color Ember = new Color(0.95f, 0.45f, 0.25f);
    public static readonly Color Alarm = new Color(0.8f, 0.12f, 0.1f);

    static RectTransform overlay;
    static Sprite softDot;
    static readonly Stack<Image> pool = new Stack<Image>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { overlay = null; softDot = null; pool.Clear(); }

    // Top-most effects canvas (below loading screens); survives scene changes.
    static RectTransform Overlay
    {
        get
        {
            if (overlay != null) return overlay;
            var root = new GameObject("Caso Zero • Efeitos de tela", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Object.DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31800;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            overlay = root.GetComponent<RectTransform>();
            return overlay;
        }
    }

    public static Sprite SoftDot
    {
        get
        {
            if (softDot != null) return softDot;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - d), 1.6f));
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            softDot = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return softDot;
        }
    }

    // ---------- Discovery sparks ----------

    /// 10 sparks burst from a screen point (pooled images, 0.6 s).
    public static void Burst(Vector2 screenPosition)
    {
        var parent = Overlay;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null, out var center);
        var colors = new[] { Amber, Paper, Ember };
        for (int i = 0; i < 10; i++)
        {
            var spark = Take(parent);
            var rect = spark.rectTransform;
            float size = Random.Range(10f, 22f);
            rect.sizeDelta = Vector2.one * size;
            rect.anchoredPosition = center;
            rect.localScale = Vector3.one;
            spark.color = colors[i % colors.Length];
            float angle = i / 10f * Mathf.PI * 2 + Random.Range(-0.3f, 0.3f);
            var target = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(70f, 140f) + Vector2.down * 20;
            float time = Random.Range(0.45f, 0.65f);
            DOTween.Sequence().SetUpdate(true).SetTarget(spark)
                .Append(rect.DOAnchorPos(target, time).SetEase(Ease.OutCubic))
                .Join(rect.DOScale(0, time).SetEase(Ease.InQuad))
                .Join(spark.DOFade(0, time).SetEase(Ease.InQuad))
                .OnComplete(() => Release(spark));
        }
        // A soft ring expanding from the click.
        var ring = Take(parent);
        ring.rectTransform.anchoredPosition = center;
        ring.rectTransform.sizeDelta = Vector2.one * 40;
        ring.rectTransform.localScale = Vector3.one;
        ring.color = new Color(Amber.r, Amber.g, Amber.b, 0.7f);
        DOTween.Sequence().SetUpdate(true).SetTarget(ring)
            .Append(ring.rectTransform.DOScale(6, 0.5f).SetEase(Ease.OutCubic))
            .Join(ring.DOFade(0, 0.5f).SetEase(Ease.OutQuad))
            .OnComplete(() => Release(ring));
    }

    static Image Take(RectTransform parent)
    {
        Image image = null;
        while (pool.Count > 0 && image == null) image = pool.Pop();
        if (image == null)
        {
            image = new GameObject("Faísca", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.sprite = SoftDot;
            image.raycastTarget = false;
        }
        image.transform.SetParent(parent, false);
        image.gameObject.SetActive(true);
        return image;
    }

    static void Release(Image image)
    {
        if (image == null) return;
        image.gameObject.SetActive(false);
        pool.Push(image);
    }

    // ---------- Flash ----------

    /// Full-screen colored flash (milestones: victory, wrong accusation).
    public static void Flash(Color color, float alpha, float duration)
    {
        var flash = Take(Overlay);
        flash.sprite = null;
        var rect = flash.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        flash.color = new Color(color.r, color.g, color.b, alpha);
        flash.DOFade(0, duration).SetEase(Ease.OutCubic).SetUpdate(true).SetTarget(flash).OnComplete(() =>
        {
            // Back to a pooled spark shape.
            flash.sprite = SoftDot;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            Release(flash);
        });
    }

    // ---------- Location title card ----------

    static TMP_Text cardTitle, cardSubtitle;
    static CanvasGroup card;
    static RectTransform cardRect, cardLineLeft, cardLineRight;
    static Sequence cardMotion;

    /// "BAR" style card: big spaced title, small subtitle, amber lines drawing out from the center.
    /// y is the vertical position on a 1920x1080 screen (default: upper third).
    public static void TitleCard(string title, string subtitle, TMP_FontAsset font, float y = 300)
    {
        if (card == null)
        {
            var root = new GameObject("Título", typeof(RectTransform), typeof(CanvasGroup));
            cardRect = root.GetComponent<RectTransform>();
            cardRect.SetParent(Overlay, false);
            cardRect.sizeDelta = new Vector2(1200, 160);
            card = root.GetComponent<CanvasGroup>();
            card.blocksRaycasts = card.interactable = false;
            var shade = new GameObject("Sombra", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            shade.rectTransform.SetParent(cardRect, false);
            shade.rectTransform.sizeDelta = new Vector2(1300, 280);
            shade.sprite = SoftDot;
            shade.color = new Color(0.05f, 0.04f, 0.04f, 0.6f);
            shade.raycastTarget = false;
            cardTitle = CardText(cardRect, new Vector2(0, 18), 64);
            cardSubtitle = CardText(cardRect, new Vector2(0, -38), 26);
            cardSubtitle.alpha = 0.75f;
            cardLineLeft = Line(cardRect, new Vector2(-1, 0));
            cardLineRight = Line(cardRect, new Vector2(1, 0));
        }
        if (font != null) cardTitle.font = cardSubtitle.font = font;
        cardRect.anchoredPosition = new Vector2(0, y);
        cardTitle.text = title.ToUpperInvariant();
        cardSubtitle.text = subtitle ?? "";
        cardMotion?.Kill();
        card.alpha = 0;
        cardTitle.characterSpacing = 30;
        cardLineLeft.sizeDelta = cardLineRight.sizeDelta = new Vector2(0, 2);
        cardMotion = DOTween.Sequence().SetUpdate(true)
            .Append(card.DOFade(1, 0.35f))
            .Join(DOTween.To(() => cardTitle.characterSpacing, v => cardTitle.characterSpacing = v, 12, 0.9f).SetEase(Ease.OutCubic))
            .Join(cardLineLeft.DOSizeDelta(new Vector2(220, 2), 0.6f).SetEase(Ease.OutCubic))
            .Join(cardLineRight.DOSizeDelta(new Vector2(220, 2), 0.6f).SetEase(Ease.OutCubic))
            .AppendInterval(1.2f)
            .Append(card.DOFade(0, 0.5f).SetEase(Ease.InSine));
    }

    static TMP_Text CardText(RectTransform parent, Vector2 position, float size)
    {
        var text = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(parent, false);
        text.rectTransform.sizeDelta = new Vector2(1200, size * 1.4f);
        text.rectTransform.anchoredPosition = position;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = size;
        text.color = Paper;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    static RectTransform Line(RectTransform parent, Vector2 side)
    {
        var line = new GameObject("Linha", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        line.color = new Color(Amber.r, Amber.g, Amber.b, 0.8f);
        line.raycastTarget = false;
        var rect = line.rectTransform;
        rect.SetParent(parent, false);
        rect.pivot = new Vector2(side.x > 0 ? 0 : 1, 0.5f);
        rect.anchoredPosition = new Vector2(side.x * 40, -72); // Under the subtitle, growing outward.
        rect.sizeDelta = new Vector2(0, 2);
        return rect;
    }

    // ---------- Ambient dust ----------

    /// Slow dust drifting through the light: makes still scenes feel inhabited.
    /// sortingOrder decides what it floats over (between the scene art and the game UI).
    public static void AmbientMotes(Scene scene, int sortingOrder, int count, float opacity)
    {
        var root = new GameObject("Poeira no ar", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(root, scene);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        for (int i = 0; i < count; i++)
        {
            var mote = new GameObject("Partícula", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            mote.rectTransform.SetParent(root.transform, false);
            mote.sprite = SoftDot;
            mote.raycastTarget = false;
            float size = Random.Range(4f, 11f);
            mote.rectTransform.sizeDelta = Vector2.one * size;
            mote.color = new Color(Paper.r, Paper.g, Paper.b, 0);
            mote.gameObject.AddComponent<Mote>().Begin(opacity * Random.Range(0.4f, 1f));
        }
    }

    // One drifting dust particle: slow diagonal drift with a gentle wobble, wraps around the screen.
    sealed class Mote : MonoBehaviour
    {
        Image image;
        RectTransform rect;
        Vector2 velocity;
        float phase, alpha;

        public void Begin(float targetAlpha)
        {
            image = GetComponent<Image>();
            rect = (RectTransform)transform;
            alpha = targetAlpha;
            phase = Random.value * 10;
            velocity = new Vector2(Random.Range(6f, 18f), Random.Range(-10f, 4f));
            rect.anchoredPosition = new Vector2(Random.Range(-960f, 960f), Random.Range(-540f, 540f));
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            phase += dt;
            var p = rect.anchoredPosition + (velocity + new Vector2(0, Mathf.Sin(phase * 0.7f) * 6)) * dt;
            if (p.x > 1000) p.x = -1000;
            if (p.y < -580) p.y = 580;
            if (p.y > 580) p.y = -580;
            rect.anchoredPosition = p;
            // Twinkles slowly as it passes through the light.
            var c = image.color;
            c.a = alpha * (0.55f + 0.45f * Mathf.Sin(phase * 0.9f));
            image.color = c;
        }
    }

    // ---------- Clickable clue pulse ----------

    /// Soft amber glow breathing behind a clue that has not been examined yet.
    public static void SetCluePulse(RectTransform marker, bool active)
    {
        if (marker == null) return;
        var existing = marker.Find("Brilho da pista");
        if (!active)
        {
            if (existing != null) { existing.DOKill(); Object.Destroy(existing.gameObject); }
            return;
        }
        if (existing != null) return;
        var glow = new GameObject("Brilho da pista", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        var rect = glow.rectTransform;
        rect.SetParent(marker, false);
        rect.SetAsFirstSibling();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-28, -22); rect.offsetMax = new Vector2(28, 22);
        glow.sprite = SoftDot;
        glow.type = Image.Type.Simple;
        glow.raycastTarget = false;
        glow.color = new Color(Amber.r, Amber.g, Amber.b, 0.05f);
        glow.DOFade(0.32f, 1.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetDelay(Random.value * 0.6f);
    }
}
