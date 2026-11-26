using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// "Examine item" screen in the style of survival-horror inventories: a dark screen with only the
// clue in 3D, turned by holding the mouse button and moving the mouse (with a little inertia).
// Its own camera renders the model into a texture. Models come from ArtesCasoZero or
// CaseZeroEvidenceModels (placeholders built from primitives).
public sealed class CaseZeroEvidenceViewer : MonoBehaviour
{
    static readonly Vector3 Stage = new Vector3(0, -5000, 0); // Far away from the 2D scene.
    const float ModelSize = 2.55f; // Diagonal: the model stays inside the frame at any angle.
    const float MinDistance = 3f, MaxDistance = 8.5f;
    const float RotateSpeed = 0.32f;  // Degrees per pixel of mouse movement.
    const float InertiaDamping = 5f;  // How fast the spin settles after letting go.
    static readonly Color TextColor = new Color(0.94f, 0.9f, 0.82f);
    static readonly Color Dim = new Color(0.94f, 0.9f, 0.82f, 0.55f);
    static readonly Color Accent = new Color(1f, 0.8f, 0.3f);

    CaseZeroArtLibrary art;
    Camera viewCamera;
    RenderTexture texture;
    Transform pivot;
    GameObject model;
    GameObject root;
    CanvasGroup group;
    RectTransform displayRect, backButton;
    TMP_Text title, description, tagLabel;
    Quaternion rotation = Quaternion.identity;
    Vector2 spin;            // Angular velocity carried after releasing the mouse.
    float distance = 5.2f, bob;
    bool dragging;
    Tween fade, intro;

    public bool IsOpen => root != null && root.activeSelf;

    public void Initialize(TMP_FontAsset font)
    {
        art = Resources.Load<CaseZeroArtLibrary>("ArtesCasoZero");
        texture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "Pista 3D" };
        texture.Create();

        pivot = new GameObject("Pista 3D • palco").transform;
        pivot.SetParent(transform, false);
        pivot.position = Stage;
        var cameraObject = new GameObject("Pista 3D • câmera");
        cameraObject.transform.SetParent(transform, false);
        viewCamera = cameraObject.AddComponent<Camera>();
        viewCamera.clearFlags = CameraClearFlags.SolidColor;
        viewCamera.backgroundColor = new Color(0, 0, 0, 0);
        viewCamera.orthographic = false;
        viewCamera.fieldOfView = 30;
        viewCamera.nearClipPlane = 0.1f;
        viewCamera.farClipPlane = 30;
        viewCamera.targetTexture = texture;
        viewCamera.depth = -50;
        viewCamera.enabled = false;

        BuildUI(font);
        root.SetActive(false);
    }

    public static bool HasModel(string key) => !string.IsNullOrEmpty(key) && key != "depoimento";

    public void Show(string key, string name, string text, bool justFound = false)
    {
        if (!HasModel(key)) return;
        if (model != null) Destroy(model);
        var custom = art != null ? art.FindModel(key) : null;
        model = custom != null ? Instantiate(custom.modelo) : CaseZeroEvidenceModels.Build(key);
        if (model == null) return;
        foreach (var collider in model.GetComponentsInChildren<Collider>()) Destroy(collider);
        pivot.rotation = Quaternion.identity;
        pivot.position = Stage;
        model.transform.SetParent(pivot, false);
        if (custom != null) model.transform.localRotation = Quaternion.Euler(custom.rotacaoInicial);
        Fit(model.transform);

        title.text = name;
        description.text = text;
        tagLabel.text = justFound ? "NEW EVIDENCE" : "EXAMINE";
        tagLabel.color = justFound ? Accent : Dim;
        CaseZeroSfx.Whoosh();

        // Entrance: the item turns in from the side and the camera eases closer.
        spin = Vector2.zero;
        dragging = false;
        var from = Quaternion.Euler(-40, -140, 0);
        var to = Quaternion.Euler(-16, 25, 0); // Slightly from above, so flat items show their face.
        rotation = from;
        distance = 7.5f;
        float t = 0;
        intro?.Kill();
        intro = DOTween.Sequence().SetUpdate(true)
            .Append(DOTween.To(() => t, v => { t = v; rotation = Quaternion.SlerpUnclamped(from, to, v); }, 1, 0.9f).SetEase(Ease.OutBack, 1.2f))
            .Join(DOTween.To(() => distance, v => distance = v, 5.2f, 0.8f).SetEase(Ease.OutCubic));
        ApplyView();

        viewCamera.enabled = true;
        root.SetActive(true);
        fade?.Kill();
        group.alpha = 0;
        fade = group.DOFade(1, 0.25f).SetUpdate(true);
        displayRect.DOKill();
        displayRect.localScale = Vector3.one * 0.85f;
        displayRect.DOScale(1, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void Hide()
    {
        if (!IsOpen) return;
        fade?.Kill();
        intro?.Kill();
        dragging = false;
        displayRect.DOKill();
        displayRect.DOScale(0.9f, 0.18f).SetEase(Ease.InQuad).SetUpdate(true);
        fade = group.DOFade(0, 0.18f).SetUpdate(true).OnComplete(() =>
        {
            root.SetActive(false);
            viewCamera.enabled = false;
            if (model != null) Destroy(model);
        });
    }

    // Centers the model on the stage and scales it to a common size.
    void Fit(Transform target)
    {
        target.localPosition = Vector3.zero;
        target.localScale = Vector3.one;
        var renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float largest = bounds.size.magnitude;
        float scale = largest > 0.0001f ? ModelSize / largest : 1;
        target.localScale = Vector3.one * scale;
        target.position += (Stage - bounds.center) * scale;
    }

    void Update()
    {
        if (!IsOpen || Mouse.current == null || group.alpha < 0.5f) return;
        var mouse = Mouse.current;
        float dt = Time.unscaledDeltaTime;
        var pointer = mouse.position.ReadValue();

        // Right click backs out, like closing an inventory screen.
        if (mouse.rightButton.wasPressedThisFrame) { Hide(); return; }

        if (mouse.leftButton.wasPressedThisFrame && !RectTransformUtility.RectangleContainsScreenPoint(backButton, pointer, null))
        {
            dragging = true;
            intro?.Kill();
        }
        if (!mouse.leftButton.isPressed) dragging = false;

        if (dragging)
        {
            // Hold and move: the object follows the hand. The last movement becomes the release spin.
            var delta = mouse.delta.ReadValue();
            Rotate(delta * RotateSpeed);
            spin = dt > 0 ? delta * RotateSpeed / dt : Vector2.zero;
        }
        else if (spin.sqrMagnitude > 0.01f)
        {
            Rotate(spin * dt);
            spin = Vector2.Lerp(spin, Vector2.zero, 1 - Mathf.Exp(-InertiaDamping * dt));
        }

        float wheel = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            intro?.Kill();
            distance = Mathf.Clamp(distance - Mathf.Sign(wheel) * 0.4f, MinDistance, MaxDistance);
        }
        bob += dt;
        ApplyView();
    }

    // Turns around the screen's axes, so dragging right always turns the item right, whatever its pose.
    void Rotate(Vector2 degrees)
    {
        rotation = Quaternion.AngleAxis(-degrees.x, Vector3.up) * Quaternion.AngleAxis(degrees.y, Vector3.right) * rotation;
    }

    void ApplyView()
    {
        pivot.rotation = rotation;
        pivot.position = Stage + Vector3.up * Mathf.Sin(bob * 1.4f) * 0.04f; // Floats gently in place.
        viewCamera.transform.position = Stage + new Vector3(0, 0, -distance);
        viewCamera.transform.rotation = Quaternion.identity;
    }

    void BuildUI(TMP_FontAsset font)
    {
        var canvasObject = new GameObject("Caso Zero • Examinar pista", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31500;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root = new GameObject("Examinar", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvasObject.transform, false);
        Stretch(root.GetComponent<RectTransform>());
        group = root.GetComponent<CanvasGroup>();

        // Near-black screen with a soft pool of light behind the item.
        var backdrop = Img("Fundo", root.transform, Vector2.zero, Vector2.zero, new Color(0.015f, 0.015f, 0.02f, 1f));
        Stretch(backdrop.rectTransform);
        var light = Img("Luz", root.transform, new Vector2(0, 60), new Vector2(1400, 1100), new Color(1f, 0.85f, 0.6f, 0.13f));
        light.sprite = CaseZeroUiFx.SoftDot;
        light.raycastTarget = false;

        var display = new GameObject("Pista 3D", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
        displayRect = display.rectTransform;
        displayRect.SetParent(root.transform, false);
        displayRect.anchoredPosition = new Vector2(0, 105); // Kept clear of the title below.
        displayRect.sizeDelta = new Vector2(700, 700);
        display.texture = texture;
        display.raycastTarget = false;

        tagLabel = Text("Etiqueta", root.transform, new Vector2(-760, 480), new Vector2(360, 40), 24, Dim, font, TextAlignmentOptions.Left);
        tagLabel.characterSpacing = 10;
        var divider = Img("Linha", root.transform, new Vector2(0, -350), new Vector2(900, 2), new Color(Accent.r, Accent.g, Accent.b, 0.45f));
        divider.raycastTarget = false;
        title = Text("Nome", root.transform, new Vector2(0, -310), new Vector2(1400, 64), 46, TextColor, font, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        description = Text("Descrição", root.transform, new Vector2(0, -420), new Vector2(1300, 110), 26, TextColor, font, TextAlignmentOptions.Top);
        var hint = Text("Controles", root.transform, new Vector2(0, -505), new Vector2(1600, 36), 20, Dim, font, TextAlignmentOptions.Center);
        hint.text = "Hold left mouse and move: rotate   •   Wheel: zoom   •   Right click / Esc: back";

        var back = Img("Voltar", root.transform, new Vector2(820, 470), new Vector2(180, 54), new Color(1, 1, 1, 0.06f));
        backButton = back.rectTransform;
        var button = back.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() => CaseZeroSoundFeedback.Current?.PlayClick());
        button.onClick.AddListener(Hide);
        back.gameObject.AddComponent<CaseZeroButtonMotion>();
        back.gameObject.AddComponent<CaseZeroHoverSound>();
        Text("Texto", back.transform, Vector2.zero, new Vector2(170, 50), 24, TextColor, font, TextAlignmentOptions.Center).text = "Back";
    }

    void OnDestroy()
    {
        fade?.Kill();
        intro?.Kill();
        if (displayRect != null) displayRect.DOKill();
        if (viewCamera != null) viewCamera.targetTexture = null;
        if (texture != null) { texture.Release(); Destroy(texture); }
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static Image Img(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    static TMP_Text Text(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, Color color, TMP_FontAsset font, TextAlignmentOptions alignment)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var text = obj.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(16, fontSize);
        text.fontSizeMax = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
}
