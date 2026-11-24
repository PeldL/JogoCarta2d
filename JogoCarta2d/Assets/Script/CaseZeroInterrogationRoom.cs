using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Lives in the SalaInterrogatorio scene, loaded additively by CaseZeroGame.
// The room, the in-room interview and the Rorschach test. Case state stays in CaseZeroGame.
// The UI is saved in the scene (right-click the component > "Gerar UI da sala na cena") so it can be
// moved and restyled in the editor. At runtime every part is looked up by its name; a part that is
// missing from the scene is created by code with its default look.
public sealed class CaseZeroInterrogationRoom : MonoBehaviour
{
    [Tooltip("Fundo da sala. Vazio = arte 'Sala de interrogatório' da biblioteca, ou uma sala desenhada pelo jogo.")]
    [SerializeField] Sprite background;

    const float PaperSize = 540;
    const float LensSize = 230;
    const float LensZoom = 3f;
    const float DetailRadius = 0.07f;
    const float DrawingTime = 3.6f;

    static readonly Color Ink = new Color(0.1f, 0.09f, 0.1f);
    static readonly Color PaperColor = new Color(0.95f, 0.93f, 0.86f);
    static readonly Color PanelColor = new Color(0.93f, 0.89f, 0.8f, 0.97f);
    static readonly Color ButtonColor = new Color(0.2f, 0.18f, 0.17f, 0.95f);
    static readonly Color ButtonText = new Color(0.96f, 0.92f, 0.82f);
    static readonly Color FoundColor = new Color(0.7f, 0.08f, 0.06f);

    enum Mode { Menu, Interview, Drawing, Paper }

    CaseZeroGame game;
    CaseZeroArtLibrary art;
    TMP_FontAsset font;
    RectTransform canvasRect;
    CanvasGroup curtain;
    GameObject room;
    Mode mode;
    // True while the UI is being built: parts already in the scene are reused instead of created.
    bool building;
    // Where the suspect sits, as placed in the scene; the intro and the test move it relative to this.
    Vector2 portraitHome;

    // Room
    Image portrait;
    RectTransform portraitRect;
    Sprite portraitSprite;
    TMP_Text nameLabel;
    GameObject dialoguePanel;
    TMP_Text message;
    readonly List<Button> buttons = new List<Button>();
    readonly List<TMP_Text> buttonLabels = new List<TMP_Text>();
    readonly List<UnityAction> buttonActions = new List<UnityAction>();

    // Rorschach
    GameObject sketch;
    Image sketchFill;
    RectTransform pencil;
    GameObject testRoot;
    RectTransform paper;
    Image paperImage;
    GameObject paperCover;
    RectTransform lens, lensFrame;
    RectTransform lensContent;
    Image lensImage;
    GameObject testPanel;
    TMP_Text testStatus, answerSpeaker, answerLabel, verdictLabel, hintLabel;
    readonly List<TMP_Text> detailLabels = new List<TMP_Text>();
    readonly List<GameObject> detailMarks = new List<GameObject>();
    Button[] questionButtons;
    Button compareButton;
    Texture2D blotTexture, circleTexture, glowTexture;
    Sprite blotSprite, circleSprite, ringSprite, glowSprite;
    Texture2D ringTexture;

    string caseId;
    int suspectIndex;
    string suspectName;
    RorschachProfile profile;
    CaseZeroArtLibrary.RorschachArt rorschachArt;
    bool[] found;
    bool[] asked;
    bool opened, compared;
    Coroutine drawing;
    Sequence pencilMotion;
    Tween portraitTween, breathing;
    readonly Dictionary<RectTransform, Vector2> restingPositions = new Dictionary<RectTransform, Vector2>();

    public bool IsOpen { get; private set; }

    public void Initialize(CaseZeroGame owner, TMP_FontAsset textFont)
    {
        game = owner;
        font = textFont;
        art = Resources.Load<CaseZeroArtLibrary>("ArtesCasoZero");
        circleTexture = MakeCircle(128, false);
        ringTexture = MakeCircle(128, true);
        glowTexture = MakeGlow(256);
        circleSprite = Sprite.Create(circleTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        ringSprite = Sprite.Create(ringTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        glowSprite = Sprite.Create(glowTexture, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        Build();
        curtain.transform.SetAsLastSibling(); // The fade covers the room.
        room.SetActive(false);
    }

    void Build()
    {
        building = true;
        BuildCanvas();
        BuildRoom();
        building = false;
        portraitHome = portraitRect.anchoredPosition;
    }

#if UNITY_EDITOR
    // Writes the room UI into the open scene so it can be edited. Run it again after deleting a
    // part to get that part back with its default look; existing parts are left as they are.
    [ContextMenu("Gerar UI da sala na cena")]
    void GenerateInScene()
    {
        if (Application.isPlaying) return;
        var guids = UnityEditor.AssetDatabase.FindAssets("Mom t:TMP_FontAsset");
        if (font == null && guids.Length > 0)
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        art = Resources.Load<CaseZeroArtLibrary>("ArtesCasoZero");
        circleSprite = SpriteAsset("Lupa - círculo", MakeCircle(128, false));
        ringSprite = SpriteAsset("Lupa - aro", MakeCircle(128, true));
        glowSprite = SpriteAsset("Luz da lâmpada", MakeGlow(256));
        Build();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    // Saves a generated texture as a sprite asset (Assets/Sprites/Sala de interrogatório) so the scene can keep it.
    static Sprite SpriteAsset(string name, Texture2D texture)
    {
        const string folder = "Assets/Sprites/Sala de interrogatório";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Sprites", "Sala de interrogatório");
        string path = folder + "/" + name + ".png";
        if (!System.IO.File.Exists(path))
        {
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEditor.AssetDatabase.ImportAsset(path);
            var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
            importer.textureType = UnityEditor.TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        DestroyImmediate(texture);
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
#endif

    void Awake()
    {
        var owner = FindFirstObjectByType<CaseZeroGame>();
        if (owner != null) owner.RegisterRoom(this);
        else Debug.LogWarning("[Caso Zero] Sala de interrogatório aberta sem CaseZeroGame. Abra pela cena Game.");
    }

    // ---------- Open / close (the scene load is handled by CaseZeroGame) ----------

    // Shows the room fully covered by the curtain; Reveal fades it in.
    public void Open(string caseKey, int index, string name)
    {
        caseId = caseKey;
        suspectIndex = index;
        suspectName = name;
        portraitSprite = art != null ? art.FindPortrait(caseKey, index) : null;
        portrait.sprite = portraitSprite;
        portrait.enabled = portraitSprite != null;
        portrait.preserveAspect = true;
        nameLabel.text = name;
        curtain.gameObject.SetActive(true);
        curtain.alpha = 1;
        curtain.blocksRaycasts = true;
        room.SetActive(true);
        IsOpen = true;
        ShowMenu();
        StartBreathing();
        CaseZeroSfx.SetRoomTone(true); // Fluorescent hum while inside.
        // The suspect is brought in: rises from behind the table while the room fades in.
        var portraitGroup = portrait.GetComponent<CanvasGroup>();
        if (portraitGroup == null) portraitGroup = portrait.gameObject.AddComponent<CanvasGroup>();
        portraitGroup.alpha = 0;
        portraitGroup.DOFade(1, 0.5f).SetDelay(0.25f).SetUpdate(true);
        portraitTween?.Kill();
        portraitRect.anchoredPosition = portraitHome + new Vector2(0, -70);
        portraitTween = portraitRect.DOAnchorPos(portraitHome, 0.7f).SetDelay(0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    public IEnumerator Reveal()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        yield return Fade(0, 0.4f);
    }

    public IEnumerator Cover()
    {
        CaseZeroSoundFeedback.Current?.PlayTransition();
        yield return Fade(1, 0.3f);
        StopDrawing();
        CaseZeroSfx.SetRoomTone(false);
        IsOpen = false;
    }

    IEnumerator Fade(float target, float duration)
    {
        curtain.gameObject.SetActive(true);
        curtain.blocksRaycasts = true;
        yield return curtain.DOFade(target, duration).SetEase(Ease.InOutSine).SetUpdate(true).WaitForCompletion();
        if (target <= 0) { curtain.blocksRaycasts = false; curtain.gameObject.SetActive(false); }
    }

    // ---------- Menu and interview ----------
    public void ShowMenu()
    {
        StopDrawing();
        mode = Mode.Menu;
        SetLayout(false);
        message.text = "The suspect is seated in the interrogation room. What do you want to do?";
        SetButtons(
            ("Interrogate", () => game.BeginRoomInterview(), true),
            ("Rorschach test", StartTest, true),
            ("Leave the room", () => game.LeaveInterrogationRoom(), true));
    }

    public void RenderInterview(string text, string[] labels, bool[] available)
    {
        mode = Mode.Interview;
        SetLayout(false);
        message.text = text;
        Reveal(message);
        var entries = new List<(string, UnityAction, bool)>();
        for (int i = 0; i < labels.Length; i++)
        {
            int choice = i;
            entries.Add((labels[i], () => game.SelectInterviewChoice(choice), available[i]));
        }
        entries.Add(("Back", ShowMenu, true));
        SetButtons(entries.ToArray());
    }

    void SetLayout(bool test)
    {
        bool showDialogue = !test, showTest = test && mode == Mode.Paper, showSketch = test && mode == Mode.Drawing;
        RevealIfOpening(dialoguePanel, showDialogue, new Vector2(-30, 0));
        dialoguePanel.SetActive(showDialogue);
        testRoot.SetActive(showTest);
        RevealIfOpening(testPanel, showTest, new Vector2(30, 0));
        testPanel.SetActive(showTest);
        RevealIfOpening(sketch, showSketch, new Vector2(0, -30));
        sketch.SetActive(showSketch);
        portraitTween?.Kill();
        var target = test ? portraitHome + new Vector2(110, 0) : portraitHome;
        portraitTween = portraitRect.DOAnchorPos(target, 0.45f).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    // Panels that appear slide a little and fade in instead of popping.
    void RevealIfOpening(GameObject panel, bool show, Vector2 from)
    {
        if (!show || panel.activeSelf) return;
        var rect = (RectTransform)panel.transform;
        var group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();
        if (!restingPositions.TryGetValue(rect, out var rest)) restingPositions[rect] = rest = rect.anchoredPosition;
        rect.DOKill();
        group.DOKill();
        rect.anchoredPosition = rest + from;
        group.alpha = 0;
        rect.DOAnchorPos(rest, 0.4f).SetEase(Ease.OutCubic).SetUpdate(true);
        group.DOFade(1, 0.3f).SetEase(Ease.OutSine).SetUpdate(true);
    }

    void SetButtons(params (string label, UnityAction action, bool enabled)[] entries)
    {
        int shown = 0;
        for (int i = 0; i < buttons.Count; i++)
        {
            bool exists = i < entries.Length;
            buttons[i].gameObject.SetActive(exists);
            if (!exists) continue;
            buttonLabels[i].text = entries[i].label;
            buttons[i].interactable = entries[i].enabled;
            buttonActions[i] = entries[i].action;
            // Options deal in one after another, like cards on the table.
            var rect = (RectTransform)buttons[i].transform;
            var group = buttons[i].GetComponent<CanvasGroup>();
            if (group == null) group = buttons[i].gameObject.AddComponent<CanvasGroup>();
            if (!restingPositions.TryGetValue(rect, out var rest)) restingPositions[rect] = rest = rect.anchoredPosition;
            rect.DOKill();
            group.DOKill();
            rect.anchoredPosition = rest + new Vector2(-24, 0);
            group.alpha = 0;
            float delay = 0.08f + shown++ * 0.05f;
            rect.DOAnchorPos(rest, 0.32f).SetDelay(delay).SetEase(Ease.OutCubic).SetUpdate(true);
            group.DOFade(1, 0.25f).SetDelay(delay).SetUpdate(true);
        }
    }

    void StartBreathing()
    {
        breathing?.Kill();
        portraitRect.localScale = Vector3.one;
        // Slow, soft breathing: the chest rises a little more than the shoulders widen.
        breathing = portraitRect.DOScale(new Vector3(1.012f, 1.032f, 1), 2.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
    }

    // ---------- Rorschach test ----------

    void StartTest()
    {
        profile = CaseZeroRorschach.Get(caseId, suspectIndex);
        rorschachArt = art != null ? art.FindRorschach(caseId, suspectIndex) : null;
        found = new bool[profile.details.Length];
        asked = new bool[CaseZeroRorschach.Questions.Length];
        opened = compared = false;
        var drawingSprite = rorschachArt != null && rorschachArt.desenho != null ? rorschachArt.desenho : BuildBlot();
        paperImage.sprite = lensImage.sprite = sketchFill.sprite = drawingSprite;
        foreach (var mark in detailMarks) Destroy(mark);
        detailMarks.Clear();
        mode = Mode.Drawing;
        SetLayout(true);
        drawing = StartCoroutine(PlayDrawing());
    }

    IEnumerator PlayDrawing()
    {
        message.text = "";
        sketchFill.fillAmount = 0;
        var label = sketch.GetComponentInChildren<TMP_Text>(true);
        label.text = "The suspect is drawing...";
        pencilMotion?.Kill();
        CaseZeroSfx.SetScribbling(true);
        pencilMotion = DOTween.Sequence().SetUpdate(true).SetLoops(-1)
            .Append(pencil.DOAnchorPos(new Vector2(60, 30), 0.22f))
            .Append(pencil.DOAnchorPos(new Vector2(-50, -10), 0.26f))
            .Append(pencil.DOAnchorPos(new Vector2(20, -40), 0.2f))
            .Append(pencil.DOAnchorPos(new Vector2(-30, 40), 0.24f));
        var frames = rorschachArt != null ? rorschachArt.quadrosDesenhando : null;
        float fps = rorschachArt != null ? Mathf.Max(1, rorschachArt.quadrosPorSegundo) : 8;
        float time = 0;
        while (time < DrawingTime)
        {
            time += Time.unscaledDeltaTime;
            sketchFill.fillAmount = Mathf.Clamp01(time / DrawingTime);
            if (frames != null && frames.Length > 0)
            {
                var frame = frames[(int)(time * fps) % frames.Length];
                if (frame != null) { portrait.sprite = frame; portrait.enabled = true; }
            }
            else portraitRect.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 9) * 1.2f);
            yield return null;
        }
        pencilMotion?.Kill();
        CaseZeroSfx.SetScribbling(false);
        portrait.sprite = portraitSprite;
        portrait.enabled = portraitSprite != null;
        portraitRect.localRotation = Quaternion.identity;
        label.text = "The drawing is ready.";
        yield return new WaitForSecondsRealtime(0.6f);
        drawing = null;
        ShowPaper();
    }

    void StopDrawing()
    {
        if (drawing != null) StopCoroutine(drawing);
        drawing = null;
        pencilMotion?.Kill();
        CaseZeroSfx.SetScribbling(false);
        if (portrait != null)
        {
            portrait.sprite = portraitSprite;
            portrait.enabled = portraitSprite != null;
            portraitRect.localRotation = Quaternion.identity;
        }
        SetLensVisible(false);
    }

    void SetLensVisible(bool visible)
    {
        if (lens != null && lens.gameObject.activeSelf != visible) lens.gameObject.SetActive(visible);
        if (lensFrame != null && lensFrame.gameObject.activeSelf != visible) lensFrame.gameObject.SetActive(visible);
    }

    void ShowPaper()
    {
        mode = Mode.Paper;
        SetLayout(true);
        paperCover.SetActive(true);
        SetLensVisible(false);
        paper.localScale = Vector3.one * 0.25f;
        paper.anchoredPosition = new Vector2(-40, -380);
        paper.localRotation = Quaternion.Euler(0, 0, 14);
        paper.DOScale(1, 0.55f).SetEase(Ease.OutBack).SetUpdate(true);
        paper.DOAnchorPos(new Vector2(-560, 20), 0.55f).SetEase(Ease.OutCubic).SetUpdate(true);
        paper.DOLocalRotate(new Vector3(0, 0, -2), 0.55f).SetUpdate(true);
        CaseZeroSfx.Paper();
        answerSpeaker.text = "";
        answerLabel.text = "Open the drawing and examine it with the magnifier.";
        verdictLabel.text = hintLabel.text = "";
        RefreshTestPanel();
    }

    void OpenDrawing()
    {
        if (opened) return;
        opened = true;
        CaseZeroSfx.Paper();
        var cover = paperCover.GetComponent<RectTransform>();
        DOTween.Sequence().SetUpdate(true)
            .Append(cover.DOScaleY(0, 0.3f).SetEase(Ease.InCubic))
            .AppendCallback(() => { paperCover.SetActive(false); cover.localScale = Vector3.one; });
        answerLabel.text = "Move the magnifier over the drawing to find hidden details. Then question the suspect.";
        RefreshTestPanel();
    }

    void Ask(int question)
    {
        if (!opened || compared) return;
        asked[question] = true;
        answerSpeaker.text = suspectName;
        answerLabel.text = profile.answers[question];
        Reveal(answerLabel);
        RefreshTestPanel();
    }

    void Compare()
    {
        if (compared) return;
        compared = true;
        answerSpeaker.text = "";
        answerLabel.text = "";
        if (profile.psychopath)
        {
            verdictLabel.text = "The answers do not match the drawing. The suspect shows psychopathic traits.";
            hintLabel.text = profile.hint;
            CaseZeroSoundFeedback.Current?.PlayNotification();
        }
        else
        {
            verdictLabel.text = "The answers match the drawing. No psychopathic traits were identified.";
            hintLabel.text = "";
        }
        Reveal(verdictLabel);
        game.RecordRorschach(suspectIndex, profile.psychopath, profile.hint);
        RefreshTestPanel();
    }

    void RefreshTestPanel()
    {
        int foundCount = 0;
        foreach (bool value in found) if (value) foundCount++;
        testStatus.text = !opened ? "Click the paper to open the drawing."
            : $"Details found: {foundCount}/{found.Length}";
        for (int i = 0; i < detailLabels.Count; i++)
        {
            bool exists = i < profile.details.Length;
            detailLabels[i].gameObject.SetActive(exists);
            if (exists) detailLabels[i].text = found[i] ? profile.details[i].text : "?";
        }
        for (int i = 0; i < questionButtons.Length; i++)
            questionButtons[i].interactable = opened && !compared;
        bool allAsked = true;
        foreach (bool value in asked) allAsked &= value;
        compareButton.interactable = opened && !compared && allAsked && foundCount == found.Length;
        compareButton.GetComponentInChildren<TMP_Text>(true).text = compared ? "Analysis recorded"
            : allAsked && foundCount == found.Length ? "Compare answers with the drawing"
            : "Find every detail and ask every question";
    }

    void Update()
    {
        if (!IsOpen || mode != Mode.Paper || !opened || Mouse.current == null) { SetLensVisible(false); return; }
        bool inside = RectTransformUtility.ScreenPointToLocalPointInRectangle(paper, Mouse.current.position.ReadValue(), null, out var local)
            && paper.rect.Contains(local);
        SetLensVisible(inside);
        if (!inside) return;
        lens.anchoredPosition = lensFrame.anchoredPosition = local;
        lensContent.anchoredPosition = -local * LensZoom;
        var normalized = new Vector2(local.x / paper.rect.width + 0.5f, local.y / paper.rect.height + 0.5f);
        for (int i = 0; i < profile.details.Length; i++)
        {
            if (found[i]) continue;
            var detail = profile.details[i];
            if (Vector2.Distance(normalized, new Vector2(detail.x, detail.y)) > DetailRadius) continue;
            found[i] = true;
            MarkDetail(detail);
            CaseZeroSfx.Discovery();
            RefreshTestPanel();
        }
    }

    void MarkDetail(RorschachDetail detail)
    {
        var mark = Img("Detalhe encontrado", paper, new Vector2((detail.x - 0.5f) * PaperSize, (detail.y - 0.5f) * PaperSize), new Vector2(64, 64), FoundColor, ringSprite);
        mark.raycastTarget = false;
        mark.transform.SetSiblingIndex(0); // Under the magnifier.
        mark.transform.localScale = Vector3.one * 1.8f;
        mark.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        detailMarks.Add(mark.gameObject);
    }

    // ---------- Construction ----------

    Image curtainImage0;

    void BuildCanvas()
    {
        const string canvasName = "Caso Zero • Sala de interrogatório";
        var saved = transform.Find(canvasName);
        if (saved != null)
        {
            canvasRect = (RectTransform)saved;
            curtainImage0 = Img("Cortina", canvasRect, Vector2.zero, Vector2.zero, Color.black);
            curtain = curtainImage0.GetComponent<CanvasGroup>();
            if (curtain == null) curtain = curtainImage0.gameObject.AddComponent<CanvasGroup>();
            curtain.alpha = 0;
            curtain.blocksRaycasts = false;
            curtainImage0.gameObject.SetActive(false);
            return;
        }
        var canvasObject = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000; // Below feedback overlays (32000) and loading curtains.
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = canvasObject.GetComponent<RectTransform>();

        var curtainImage = Img("Cortina", canvasRect, Vector2.zero, Vector2.zero, Color.black);
        Stretch(curtainImage.rectTransform);
        curtain = curtainImage.gameObject.AddComponent<CanvasGroup>();
        curtain.alpha = 0;
        curtain.blocksRaycasts = false;
        curtainImage.gameObject.SetActive(false);
    }

    void BuildRoom()
    {
        room = Group("Sala", canvasRect, out bool created);
        if (created)
        {
            room.transform.SetSiblingIndex(0);
            Stretch(room.GetComponent<RectTransform>());
        }

        if (background == null && art != null)
            background = art.salaInterrogatorio != null ? art.salaInterrogatorio : art.Find("Sala de interrogatório") ?? art.Find("Delegacia - sala");
        bool newWall = room.transform.Find("Parede") == null;
        var wall = Img("Parede", room.transform, Vector2.zero, Vector2.zero, background != null ? Color.white : new Color(0.13f, 0.14f, 0.15f), background);
        if (newWall) { Stretch(wall.rectTransform); wall.preserveAspect = false; }
        if (background == null && newWall)
        {
            var stripe = Img("Rodapé", room.transform, new Vector2(0, -200), new Vector2(1920, 6), new Color(0.08f, 0.08f, 0.09f));
            stripe.raycastTarget = false;
            var mirror = Img("Espelho falso", room.transform, new Vector2(-560, 260), new Vector2(520, 260), new Color(0.07f, 0.09f, 0.11f));
            mirror.raycastTarget = false;
            var mirrorFrame = mirror.gameObject.AddComponent<Outline>();
            mirrorFrame.effectColor = new Color(0.25f, 0.25f, 0.27f);
            mirrorFrame.effectDistance = new Vector2(6, -6);
        }
        var glow = Img("Luz da lâmpada", room.transform, new Vector2(120, 140), new Vector2(1500, 1100), new Color(1f, 0.86f, 0.55f, 0.32f), glowSprite);
        glow.raycastTarget = false;
        var lamp = Img("Lâmpada", room.transform, new Vector2(120, 520), new Vector2(160, 46), new Color(0.06f, 0.06f, 0.06f));
        lamp.raycastTarget = false;
        var cord = Img("Fio", room.transform, new Vector2(120, 560), new Vector2(4, 60), new Color(0.06f, 0.06f, 0.06f));
        cord.raycastTarget = false;

        portrait = Img("Suspeito", room.transform, new Vector2(540, -40), new Vector2(560, 780), Color.white);
        portrait.raycastTarget = false;
        portraitRect = portrait.rectTransform;

        var table = Img("Mesa", room.transform, new Vector2(0, -440), new Vector2(1920, 200), new Color(0.24f, 0.17f, 0.12f));
        table.raycastTarget = false;
        var tableEdge = Img("Borda da mesa", room.transform, new Vector2(0, -342), new Vector2(1920, 12), new Color(0.36f, 0.26f, 0.18f));
        tableEdge.raycastTarget = false;

        // The name sits just above the suspect's head.
        bool newName = room.transform.Find("Nome do suspeito") == null;
        nameLabel = Text("Nome do suspeito", room.transform, new Vector2(540, 385), new Vector2(620, 60), 36, ButtonText, TextAlignmentOptions.Center);
        if (newName) nameLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        bool newTitle = room.transform.Find("Título") == null;
        var title = Text("Título", room.transform, new Vector2(-560, 470), new Vector2(700, 60), 28, new Color(0.85f, 0.8f, 0.7f), TextAlignmentOptions.Left);
        if (newTitle) { title.text = "INTERROGATION ROOM"; title.characterSpacing = 6; }

        // Dialogue panel: menu and interview.
        var panel = Img("Diálogo", room.transform, new Vector2(-400, -40), new Vector2(900, 640), PanelColor);
        AddShadow(panel.gameObject);
        dialoguePanel = panel.gameObject;
        message = Text("Fala", panel.transform, new Vector2(0, 170), new Vector2(820, 260), 30, Ink, TextAlignmentOptions.TopLeft);
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            buttonActions.Add(null);
            var button = MakeButton("Opção " + (i + 1), panel.transform, new Vector2(0, -20 - i * 66), new Vector2(820, 58), "",
                () => buttonActions[index]?.Invoke());
            buttons.Add(button);
            buttonLabels.Add(button.GetComponentInChildren<TMP_Text>(true));
        }

        BuildSketch();
        BuildPaper();
        BuildTestPanel();
    }

    void BuildSketch()
    {
        sketch = Group("NPC desenhando", room.transform, out bool created);
        var rect = sketch.GetComponent<RectTransform>();
        if (created)
        {
            rect.anchoredPosition = new Vector2(-40, -380);
            rect.sizeDelta = new Vector2(220, 220);
        }
        bool newSheet = rect.Find("Folha") == null;
        var sheet = Img("Folha", rect, Vector2.zero, new Vector2(200, 200), PaperColor);
        if (newSheet) sheet.rectTransform.localRotation = Quaternion.Euler(0, 0, 8);
        AddShadow(sheet.gameObject);
        sketchFill = Img("Traços", sheet.transform, Vector2.zero, new Vector2(190, 190), Color.white);
        sketchFill.type = Image.Type.Filled; // The drawing appears top to bottom while the suspect draws.
        sketchFill.fillMethod = Image.FillMethod.Vertical;
        sketchFill.fillOrigin = (int)Image.OriginVertical.Top;
        bool newPencil = sheet.transform.Find("Lápis") == null;
        var pencilImage = Img("Lápis", sheet.transform, Vector2.zero, new Vector2(16, 130), new Color(0.92f, 0.72f, 0.2f));
        pencil = pencilImage.rectTransform;
        if (newPencil) { pencil.pivot = new Vector2(0.5f, 0); pencil.localRotation = Quaternion.Euler(0, 0, -30); }
        var tip = Img("Ponta", pencil, new Vector2(0, -59), new Vector2(10, 14), Ink);
        tip.raycastTarget = false;
        Text("Estado", rect, new Vector2(-300, 380), new Vector2(900, 60), 34, ButtonText, TextAlignmentOptions.Center);
        sketch.SetActive(false);
    }

    void BuildPaper()
    {
        testRoot = Group("Teste de Rorschach", room.transform, out bool created);
        if (created) Stretch(testRoot.GetComponent<RectTransform>());
        var paperObject = Img("Papel", testRoot.transform, new Vector2(-560, 20), new Vector2(PaperSize, PaperSize), Color.white);
        paper = paperObject.rectTransform;
        paperImage = paperObject;
        AddShadow(paperObject.gameObject);

        lens = Img("Lupa", paper, Vector2.zero, new Vector2(LensSize, LensSize), Color.white, circleSprite).rectTransform;
        lens.GetComponent<Image>().raycastTarget = false;
        var mask = lens.GetComponent<Mask>();
        if (mask == null) { mask = lens.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true; }
        lensImage = Img("Desenho ampliado", lens, Vector2.zero, new Vector2(PaperSize * LensZoom, PaperSize * LensZoom), Color.white);
        lensImage.raycastTarget = false;
        lensContent = lensImage.rectTransform;
        // Frame and handle live outside the mask so they are not clipped; Update keeps them on the lens.
        lensFrame = Group("Moldura da lupa", paper, out bool newFrame).GetComponent<RectTransform>();
        if (newFrame) lensFrame.sizeDelta = Vector2.zero;
        var frame = Img("Aro", lensFrame, Vector2.zero, new Vector2(LensSize + 22, LensSize + 22), new Color(0.15f, 0.13f, 0.12f), ringSprite);
        frame.raycastTarget = false;
        var handle = Img("Cabo", lensFrame, new Vector2(LensSize * 0.5f + 40, -LensSize * 0.5f - 40), new Vector2(34, 150), new Color(0.3f, 0.2f, 0.12f));
        handle.raycastTarget = false;
        if (newFrame) handle.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        SetLensVisible(false);

        var cover = Img("Papel dobrado", paper, Vector2.zero, new Vector2(PaperSize, PaperSize), new Color(0.9f, 0.87f, 0.79f));
        paperCover = cover.gameObject;
        var crease = Img("Dobra", cover.transform, Vector2.zero, new Vector2(PaperSize, 4), new Color(0.7f, 0.66f, 0.58f));
        crease.raycastTarget = false;
        var coverLabel = Text("Abrir", cover.transform, new Vector2(0, -80), new Vector2(PaperSize - 60, 120), 30, Ink, TextAlignmentOptions.Center);
        if (string.IsNullOrEmpty(coverLabel.text)) coverLabel.text = "Click to open the drawing";
        var coverButton = cover.GetComponent<Button>();
        if (coverButton == null) coverButton = cover.gameObject.AddComponent<Button>();
        if (Application.isPlaying) coverButton.onClick.AddListener(OpenDrawing);
        testRoot.SetActive(false);
    }

    void BuildTestPanel()
    {
        var panel = Img("Análise", room.transform, new Vector2(90, -20), new Vector2(600, 760), PanelColor);
        AddShadow(panel.gameObject);
        testPanel = panel.gameObject;
        var title = Text("Título", panel.transform, new Vector2(0, 340), new Vector2(560, 50), 30, Ink, TextAlignmentOptions.Left);
        if (string.IsNullOrEmpty(title.text)) { title.text = "RORSCHACH TEST"; title.fontStyle = FontStyles.Bold; }
        testStatus = Text("Detalhes", panel.transform, new Vector2(0, 298), new Vector2(560, 36), 22, new Color(0.35f, 0.3f, 0.27f), TextAlignmentOptions.Left);
        for (int i = 0; i < 4; i++)
        {
            var label = Text("Detalhe " + (i + 1), panel.transform, new Vector2(0, 260 - i * 34), new Vector2(560, 32), 21, FoundColor, TextAlignmentOptions.Left);
            label.fontStyle = FontStyles.Italic;
            detailLabels.Add(label);
        }
        questionButtons = new Button[CaseZeroRorschach.Questions.Length];
        for (int i = 0; i < questionButtons.Length; i++)
        {
            int question = i;
            questionButtons[i] = MakeButton("Pergunta " + (i + 1), panel.transform, new Vector2(0, 100 - i * 60), new Vector2(560, 52),
                CaseZeroRorschach.Questions[i], () => Ask(question));
        }
        answerSpeaker = Text("Quem responde", panel.transform, new Vector2(0, -95), new Vector2(560, 32), 22, new Color(0.35f, 0.3f, 0.27f), TextAlignmentOptions.Left);
        answerSpeaker.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        answerLabel = Text("Resposta", panel.transform, new Vector2(0, -150), new Vector2(560, 80), 24, Ink, TextAlignmentOptions.TopLeft);
        verdictLabel = Text("Veredito", panel.transform, new Vector2(0, -120), new Vector2(560, 70), 23, Ink, TextAlignmentOptions.TopLeft);
        verdictLabel.fontStyle = FontStyles.Bold;
        hintLabel = Text("Dica", panel.transform, new Vector2(0, -205), new Vector2(560, 100), 21, FoundColor, TextAlignmentOptions.TopLeft);
        compareButton = MakeButton("Comparar", panel.transform, new Vector2(0, -290), new Vector2(560, 52), "", Compare);
        MakeButton("Voltar", panel.transform, new Vector2(0, -350), new Vector2(560, 46), "Back", ShowMenu);
        testPanel.SetActive(false);
    }

    // ---------- Ink blot ----------

    Sprite BuildBlot()
    {
        const int size = 512;
        if (blotTexture == null)
        {
            blotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            blotSprite = Sprite.Create(blotTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        var random = new System.Random(profile.seed);
        int count = 18 + random.Next(8);
        var centers = new Vector3[count];
        for (int i = 0; i < count; i++)
            centers[i] = new Vector3(0.12f + (float)random.NextDouble() * 0.36f, 0.12f + (float)random.NextDouble() * 0.76f, 0.02f + (float)random.NextDouble() * 0.045f); // Many small blobs: wings, holes and drips instead of one mass.
        var pixels = new Color32[size * size];
        Color32 paperPixel = PaperColor, inkPixel = new Color(0.08f, 0.07f, 0.09f);
        float noiseOffset = (float)random.NextDouble() * 100;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float mu = u > 0.5f ? 1 - u : u; // Mirror symmetry.
                float field = 0;
                foreach (var c in centers)
                {
                    float dx = mu - c.x, dy = v - c.y;
                    field += c.z * c.z / (dx * dx + dy * dy + 0.0001f);
                }
                field += (Mathf.PerlinNoise(mu * 18 + noiseOffset, v * 18) - 0.5f) * 0.9f;
                float ink = Mathf.Clamp01((field - 1.35f) * 5f);
                var grain = Mathf.PerlinNoise(u * 90, v * 90) * 0.06f;
                var color = Color32.Lerp(paperPixel, inkPixel, ink);
                color.r = (byte)Mathf.Clamp(color.r - grain * 255 * (1 - ink), 0, 255);
                pixels[y * size + x] = color;
            }
        foreach (var detail in profile.details) DrawGlyph(pixels, size, detail);
        blotTexture.SetPixels32(pixels);
        blotTexture.Apply();
        return blotSprite;
    }

    // Hidden details: thin red strokes that read clearly only under the magnifier.
    static void DrawGlyph(Color32[] pixels, int size, RorschachDetail detail)
    {
        Color32 color = new Color(0.78f, 0.14f, 0.1f);
        int cx = Mathf.RoundToInt(detail.x * size), cy = Mathf.RoundToInt(detail.y * size);
        const int s = 7;
        // Pale backing keeps the mark readable on top of the ink.
        Disc(pixels, size, cx, cy, s + 3, new Color32(62, 18, 16, 255)); // Dark backing: blends into the ink at normal size.
        switch (detail.glyph)
        {
            case 0: Line(pixels, size, cx - s, cy - s, cx + s, cy + s, color); Line(pixels, size, cx - s + 1, cy - s, cx + s + 1, cy + s, color); break;
            case 1: Ring(pixels, size, cx, cy, s - 2, color); Ring(pixels, size, cx, cy, s - 5, color); break;
            case 2:
                Line(pixels, size, cx - s, cy - s, cx + s, cy - s, color); Line(pixels, size, cx + s, cy - s, cx + s, cy + s, color);
                Line(pixels, size, cx + s, cy + s, cx - s, cy + s, color); Line(pixels, size, cx - s, cy + s, cx - s, cy - s, color);
                Line(pixels, size, cx - s, cy - s, cx + s, cy + s, color); Line(pixels, size, cx - s, cy + s, cx + s, cy - s, color); break;
            case 3: for (int i = -s; i <= s; i += 4) Line(pixels, size, cx + i - 3, cy - s, cx + i + 3, cy + s, color); break;
            default:
                Ring(pixels, size, cx - s, cy, 3, color);
                Line(pixels, size, cx - s + 4, cy, cx + s, cy, color);
                Line(pixels, size, cx + s, cy, cx + s + 3, cy + 4, color); Line(pixels, size, cx + s, cy, cx + s + 3, cy - 4, color);
                Line(pixels, size, cx, cy, cx + 2, cy + 5, color); break;
        }
    }

    static void Set(Color32[] pixels, int size, int x, int y, Color32 color)
    {
        if (x >= 0 && y >= 0 && x < size && y < size) pixels[y * size + x] = color;
    }
    static void Line(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
        for (int i = 0; i <= steps; i++)
            Set(pixels, size, Mathf.RoundToInt(Mathf.Lerp(x0, x1, i / (float)steps)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, i / (float)steps)), color);
    }
    static void Ring(Color32[] pixels, int size, int cx, int cy, int r, Color32 color)
    {
        for (int i = 0; i < 64; i++)
        {
            float a = i / 64f * Mathf.PI * 2;
            Set(pixels, size, cx + Mathf.RoundToInt(Mathf.Cos(a) * r), cy + Mathf.RoundToInt(Mathf.Sin(a) * r), color);
        }
    }
    static void Disc(Color32[] pixels, int size, int cx, int cy, int r, Color32 color)
    {
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
                if (x * x + y * y <= r * r) Set(pixels, size, cx + x, cy + y, color);
    }

    static Texture2D MakeCircle(int size, bool ring)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float radius = size * 0.5f - 1;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                float alpha = Mathf.Clamp01(radius - d);
                if (ring) alpha *= Mathf.Clamp01(d - (radius - size * 0.08f));
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    static Texture2D MakeGlow(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // ---------- UI helpers ----------

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    // Finds a direct child saved in the scene, or creates an empty RectTransform group.
    GameObject Group(string name, Transform parent, out bool created)
    {
        var saved = parent.Find(name);
        created = saved == null;
        if (saved != null) return saved.gameObject;
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    Image Img(string name, Transform parent, Vector2 position, Vector2 size, Color color, Sprite sprite = null)
    {
        if (building && parent.Find(name) is Transform saved && saved.TryGetComponent<Image>(out var existing))
        {
            // Saved in the scene: keep its place and look; generated sprites are filled in if missing.
            if (sprite != null && existing.sprite == null) existing.sprite = sprite;
            return existing;
        }
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = obj.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        return image;
    }

    TMP_Text Text(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        if (building && parent.Find(name) is Transform saved && saved.TryGetComponent<TMP_Text>(out var existing)) return existing;
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
        text.text = "";
        return text;
    }

    Button MakeButton(string name, Transform parent, Vector2 position, Vector2 size, string label, UnityAction action)
    {
        if (building && parent.Find(name) is Transform saved && saved.TryGetComponent<Button>(out var existing))
        {
            if (Application.isPlaying)
            {
                existing.onClick.AddListener(() => CaseZeroSoundFeedback.Current?.PlayClick());
                existing.onClick.AddListener(action);
            }
            return existing;
        }
        var image = Img(name, parent, position, size, ButtonColor);
        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1.25f, 1.2f, 1.1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.55f);
        button.colors = colors;
        if (Application.isPlaying)
        {
            button.onClick.AddListener(() => CaseZeroSoundFeedback.Current?.PlayClick());
            button.onClick.AddListener(action);
        }
        var text = Text("Texto", image.transform, Vector2.zero, size - new Vector2(30, 8), 24, ButtonText, TextAlignmentOptions.Center);
        text.text = label;
        if (image.gameObject.GetComponent<CaseZeroButtonMotion>() == null) image.gameObject.AddComponent<CaseZeroButtonMotion>();
        return button;
    }

    static void AddShadow(GameObject target)
    {
        if (target.GetComponent<Shadow>() != null) return;
        var shadow = target.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.45f);
        shadow.effectDistance = new Vector2(8, -8);
    }

    static void Reveal(TMP_Text label)
    {
        var fx = label.GetComponent<CaseZeroDialogueFX>();
        if (fx == null) fx = label.gameObject.AddComponent<CaseZeroDialogueFX>();
        fx.RevealInterview();
    }

    void OnDestroy()
    {
        pencilMotion?.Kill();
        portraitTween?.Kill();
        breathing?.Kill();
        if (IsOpen) CaseZeroSfx.SetRoomTone(false);
        CaseZeroSfx.SetScribbling(false);
        foreach (var rect in restingPositions.Keys) if (rect != null) rect.DOKill();
        foreach (var obj in new Object[] { blotTexture, circleTexture, ringTexture, glowTexture, blotSprite, circleSprite, ringSprite, glowSprite })
            if (obj != null) Destroy(obj);
    }
}
