using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI objects are saved in the scenes. This component only changes their state.
public sealed class CaseZeroView : MonoBehaviour
{
    public static string DisplayName(string key)
    {
        switch (key)
        {
            case CaseZeroPages.Evidence: return "Evidence";
            case CaseZeroPages.Interviews: return "Interviews";
            case CaseZeroPages.Deduction: return "Deduction";
            case CaseZeroPages.Options: return "Settings";
            case CaseZeroPages.Suspects: return "Suspects";
            case CaseZeroPages.Introduction: return "Introduction";
            case CaseZeroPages.Confirmation: return "Confirm restart";
            case CaseZeroPages.Result: return "Results";
            case CaseZeroPages.Map: return "Map";
            case CaseZeroPages.Phone: case CaseZeroPages.LegacyPhone: return "Phone";
            case CaseZeroPages.LegacyScene: return "Crime scene";
            case "Apartamento": return "Apartment 302";
            case "Escritorio": return "Office";
            case "Delegacia": return "Police station";
            case "Laboratorio": return "Forensics laboratory";
            case "Rua": return "Street";
            case "Estacionamento": return "Parking lot";
            case "Padaria": return "Bakery";
            default: return key;
        }
    }

    [Serializable]
    public class Page
    {
        public string name;
        public GameObject panel;
        public Button tab;
        public GameObject selectedMark;
    }

    [Header("Documento • objetos já montados na cena")]
    public GameObject documentRoot;
    public Page[] pages;
    public Button closeDocumentButton;
    public TMP_Text documentTitle;
    public TMP_Text status;
    public TMP_Text progress;
    public TMP_Text phoneNotification;
    public Button phoneAttentionButton;
    public CaseZeroInvestigationFeedback Feedback { get; private set; }
    public SpankyBoy.JuiceUI.Free.Animate_Pulse_Free incomingCallPulse;

    [Header("Introdução")]
    public Button continueButton;

    [Header("Local do crime • fora do documento")]
    public Button[] evidenceMarkers;
    public TMP_Text[] evidenceMarkerLabels;
    public TMP_Text cameraLabel;
    [Header("Campanha • referências montadas na cena")]
    public Button[] interviewSuspectButtons = new Button[0];
    public TMP_Text[] suspectLabels = new TMP_Text[0], objectLabels = new TMP_Text[0], placeLabels = new TMP_Text[0], evidenceCardLabels = new TMP_Text[0];
    public TMP_Text objectQuestion, placeQuestion, suspectsSummary, caseBriefing, worldInterviewLabel;
    public TMP_Text solutionText, nextCaseLabel;
    public Button nextCaseButton;
    public Button[] caseMapButtons = new Button[0];
    public string investigationLocation = "Bar";

    [Header("Evidências • fichas fixas")]
    public Button[] evidenceCards;
    public GameObject emptyEvidenceNotice;
    public GameObject evidenceDetails;
    public TMP_Text evidenceName;
    public TMP_Text evidenceDescription;
    public TMP_Text evidenceType;
    [Tooltip("Opcional: foto da pista montada na cena. Vazio = uma polaroid é criada no canto de Detalhes_Evidencia ao jogar.")]
    public Image evidencePhoto;
    [Tooltip("Opcional: o objeto que mostra/esconde a foto (por exemplo a moldura). Vazio = o próprio evidencePhoto.")]
    public RectTransform evidencePhotoFrame;
    const float PhotoColumn = 190;
    bool photoCreatedAtRuntime;
    float descriptionWidth = -1;

    /// <summary>Shows the clue's drawing (art library name) in a polaroid on the evidence
    /// card, or hides it when the clue has no drawing.</summary>
    public void ShowEvidencePicture(string artName)
    {
        if (evidenceDetails == null) return;
        Sprite picture = artLibrary != null && !string.IsNullOrEmpty(artName) ? artLibrary.Find(artName) : null;
        if (picture != null) EnsureEvidencePhoto();
        bool show = picture != null && evidencePhoto != null;
        if (evidencePhotoFrame != null) evidencePhotoFrame.gameObject.SetActive(show);
        if (show) { evidencePhoto.sprite = picture; evidencePhoto.preserveAspect = true; }
        // The built-in polaroid sits beside the description, so narrow the text while it shows.
        if (photoCreatedAtRuntime && evidenceDescription != null)
        {
            var rect = evidenceDescription.rectTransform;
            if (descriptionWidth < 0) descriptionWidth = rect.rect.width;
            float left = rect.anchoredPosition.x - rect.pivot.x * rect.rect.width;
            float width = show ? descriptionWidth - PhotoColumn : descriptionWidth;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.anchoredPosition = new Vector2(left + rect.pivot.x * width, rect.anchoredPosition.y);
        }
    }

    void EnsureEvidencePhoto()
    {
        if (evidencePhoto != null)
        {
            if (evidencePhotoFrame == null) evidencePhotoFrame = evidencePhoto.rectTransform;
            return;
        }
        // Polaroid frame pinned to the top-right corner of the evidence card.
        var frameObject = new GameObject("Foto_Pista", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        evidencePhotoFrame = frameObject.GetComponent<RectTransform>();
        evidencePhotoFrame.SetParent(evidenceDetails.transform, false);
        evidencePhotoFrame.anchorMin = evidencePhotoFrame.anchorMax = evidencePhotoFrame.pivot = new Vector2(1, 1);
        evidencePhotoFrame.anchoredPosition = new Vector2(-4, -150);
        evidencePhotoFrame.sizeDelta = new Vector2(175, 242);
        evidencePhotoFrame.localRotation = Quaternion.Euler(0, 0, 3);
        var frame = frameObject.GetComponent<Image>();
        frame.sprite = artLibrary != null ? artLibrary.molduraPolaroid : null;
        frame.preserveAspect = false;
        frame.raycastTarget = false;
        // The drawing sits in the white window of the polaroid.
        var photoObject = new GameObject("Foto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var photoRect = photoObject.GetComponent<RectTransform>();
        photoRect.SetParent(evidencePhotoFrame, false);
        photoRect.anchorMin = new Vector2(0.10f, 0.30f);
        photoRect.anchorMax = new Vector2(0.86f, 0.90f);
        photoRect.offsetMin = photoRect.offsetMax = Vector2.zero;
        evidencePhoto = photoObject.GetComponent<Image>();
        evidencePhoto.raycastTarget = false;
        photoCreatedAtRuntime = true;
    }

    [Header("Interrogatório")]
    public TMP_Text interviewMessage;
    [Header("Retratos • arraste as artes na biblioteca")]
    public CaseZeroArtLibrary artLibrary;
    public Image interviewPortrait;
    public GameObject firstCaseVictim;
    public TMP_Text victimCaption;
    public TMP_Text interviewPortraitName;
    public Image[] deductionPortraits = new Image[0];
    [Tooltip("Setas da Dedução para trocar de suspeito. Vazias = criadas automaticamente ao jogar. " +
             "Use o menu ⋮ deste componente → \"Criar setas dos suspeitos na cena\" para poder editá-las.")]
    public Button suspectPreviousArrow, suspectNextArrow;

#if UNITY_EDITOR
    [ContextMenu("Criar setas dos suspeitos na cena")]
    void CreateSuspectArrowsInScene()
    {
        if (Application.isPlaying) { Debug.LogWarning("[Caso Zero] Saia do Play Mode para criar as setas."); return; }
        if (suspectPreviousArrow != null && suspectNextArrow != null)
        {
            UnityEditor.Selection.objects = new UnityEngine.Object[] { suspectPreviousArrow.gameObject, suspectNextArrow.gameObject };
            Debug.Log("[Caso Zero] As setas já existem na cena e foram selecionadas.", this);
            return;
        }
        var content = deductionPortraits.Length > 0 && deductionPortraits[0] != null
            ? deductionPortraits[0].transform.parent.parent as RectTransform : null;
        var viewport = content != null ? content.parent as RectTransform : null;
        if (viewport == null) { Debug.LogWarning("[Caso Zero] Retratos da Dedução não encontrados.", this); return; }
        var arrows = CaseZeroSuspectCarousel.CreateDefaultArrows(viewport, content, new Vector2(56, 56), 210);
        foreach (var arrow in arrows) UnityEditor.Undo.RegisterCreatedObjectUndo(arrow.gameObject, "Criar setas dos suspeitos");
        UnityEditor.Undo.RecordObject(this, "Criar setas dos suspeitos");
        suspectPreviousArrow = arrows[0];
        suspectNextArrow = arrows[1];
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        UnityEditor.Selection.objects = new UnityEngine.Object[] { arrows[0].gameObject, arrows[1].gameObject };
        Debug.Log("[Caso Zero] Setas criadas em \"" + viewport.name + "\". Edite-as à vontade e salve a cena.", this);
    }
#endif
    Tween portraitFade;
    string portraitIdentity;

    public void ShowPortraits(CaseZeroCase data, int selected)
    {
        if (artLibrary == null) return;
        if (firstCaseVictim != null)
        {
            // Reuse the existing portrait slot for each campaign victim.
            string victimArt = data.victimArt;
            Sprite victimSprite = string.IsNullOrEmpty(victimArt) ? null : artLibrary.Find(victimArt);
            var victimImage = firstCaseVictim.GetComponent<Image>();
            if (victimImage != null && victimSprite != null)
            {
                victimImage.sprite = victimSprite;
                victimImage.preserveAspect = true;
            }
            firstCaseVictim.SetActive(victimSprite != null);
            if (victimCaption != null)
            {
                int caseNumber = Mathf.Max(0, System.Array.IndexOf(CaseZeroCases.All, data) + 1);
                string label = data.victimLabel ?? "";
                victimCaption.text = victimSprite != null ? $"VICTIM • CASE {caseNumber:00}\n{label}" : string.Empty;
            }
        }
        if (deductionPortraits.Length > 0 && deductionPortraits[0] != null)
        {
            var content = deductionPortraits[0].transform.parent.parent as RectTransform;
            if (content != null) content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, data.suspects.Length * 162);
        }
        for (int i = 0; i < deductionPortraits.Length; i++)
            if (deductionPortraits[i] != null)
            {
                deductionPortraits[i].sprite = artLibrary.FindPortrait(data.id, data.SuspectAtSlot(i));
                deductionPortraits[i].preserveAspect = true;
                // Without artwork, hide the image instead of drawing a white box.
                deductionPortraits[i].enabled = deductionPortraits[i].sprite != null;
            }
        if (interviewPortraitName != null)
            interviewPortraitName.text = selected >= 0 && selected < data.interviewNames.Length ? data.interviewNames[selected] : "";
        if (interviewPortrait == null) return;
        var sprite = artLibrary.FindPortrait(data.id, selected);
        string identity = data.id + ":" + selected;
        bool changed = identity != portraitIdentity || interviewPortrait.sprite != sprite;
        portraitIdentity = identity;
        interviewPortrait.sprite = sprite;
        interviewPortrait.preserveAspect = true;
        interviewPortrait.enabled = sprite != null;
        if (changed && sprite != null)
        {
            // New speaker: fade in while settling from a slight lift, like a photo placed on the file.
            portraitFade?.Kill();
            var rect = interviewPortrait.rectTransform;
            if (!portraitRestCaptured) { portraitRest = rect.anchoredPosition; portraitRestCaptured = true; }
            var color = interviewPortrait.color; color.a = 0; interviewPortrait.color = color;
            rect.anchoredPosition = portraitRest + (nextPortraitOffset ?? Vector2.up * 10f);
            nextPortraitOffset = null;
            portraitFade = DOTween.Sequence().SetUpdate(true)
                .Append(interviewPortrait.DOFade(1, 0.28f).SetEase(Ease.OutSine))
                .Join(rect.DOAnchorPos(portraitRest, 0.34f).SetEase(Ease.OutCubic));
        }
    }
    Vector2 portraitRest;
    bool portraitRestCaptured;
    Vector2? nextPortraitOffset;

    /// <summary>The next portrait change slides in from this offset instead of the small lift.</summary>
    public void SlideNextPortraitFrom(Vector2 offset) => nextPortraitOffset = offset;

    /// <summary>The interview portrait was moved: settle any animation and take the new place as rest.</summary>
    public void ForgetPortraitRest()
    {
        portraitFade?.Kill(true);
        portraitRestCaptured = false;
    }
    void OnDestroy()
    {
        portraitFade?.Kill();
        closeMotion?.Kill();
        statusMotion?.Kill();
        choicesMotion?.Kill();
        selectionMotion?.Kill();
        markerMotion?.Kill();
        detailsMotion?.Kill();
    }
    public Button[] interviewChoices;
    public TMP_Text[] interviewChoiceLabels;

    [Header("Ligações")]
    public TMP_Text phoneState;
    public TMP_Text phoneMessage;
    public Button answerPhoneButton;
    public Button sendAnalysisButton;
    public Button endPhoneButton;
    public CaseZeroPhonePresentation phonePresentation;

    [Header("Dedução")]
    public GameObject[] suspectChecks;
    public GameObject[] weaponChecks;
    public GameObject[] placeChecks;
    public TMP_Text deductionHint;
    public Button concludeButton;

    [Header("Opções e resultado")]
    public Button loadButton;
    public TMP_Text resultStats;
    public GameObject resultSolution;
    public TMP_Text objective;
    public GameObject evidenceNotice;
    public TMP_Text evidenceNoticeText;
    Coroutine noticeRoutine;

    public void NotifyEvidence(string name)
    {
        if (evidenceNotice == null || evidenceNoticeText == null) return;
        ClearNotice();
        evidenceNoticeText.text = "NEW EVIDENCE\n" + name + "\nAdded to the case file.";
        evidenceNotice.SetActive(true);
        if (evidenceNotice.GetComponent<CaseZeroPanelMotion>() == null)
            evidenceNotice.AddComponent<CaseZeroPanelMotion>();
        noticeRoutine = StartCoroutine(HideNotice());
    }
    System.Collections.IEnumerator HideNotice()
    {
        yield return new WaitForSecondsRealtime(3.5f);
        var motion = evidenceNotice.GetComponent<CaseZeroPanelMotion>();
        if (motion != null) motion.Hide();
        else evidenceNotice.SetActive(false);
        noticeRoutine = null;
    }
    public void ClearNotice()
    {
        if (noticeRoutine != null) StopCoroutine(noticeRoutine);
        noticeRoutine = null;
        if (evidenceNotice != null) evidenceNotice.SetActive(false);
    }

    [Header("Slots • botões existentes na introdução e opções")]
    public TMP_Text[] slotLabels;
    public TMP_Text slotContext;
    public TMP_Text confirmationMessage;
    public TMP_Text confirmationButtonLabel;
    public Button[] deleteButtons;
    [Header("UI com mapa e telefone separados")]
    public bool separatePanels;
    public GameObject investigationRoot;
    public GameObject mapRoot;
    public GameObject phoneRoot;
    public GameObject modalRoot;
    public CanvasGroup worldControls;
    public Button[] launchButtons;
    [Header("Mapa de fundo e cenas de locais")]
    public bool backgroundMap;
    public bool mapInDocument;
    public CanvasGroup mapControls;
    public GameObject[] barControls;
    public CanvasGroup travelCurtain;
    public TMP_Text locationLabel;
    bool atMap = true;

    string[] availableLocations = new string[0];

    public void SetAvailableLocations(string[] locations)
    {
        availableLocations = locations ?? new string[0];
        if (mapRoot == null) return;
        foreach (var button in mapRoot.GetComponentsInChildren<Button>(true))
        {
            string objectName = button.gameObject.name;
            if (!objectName.StartsWith("Map_", StringComparison.Ordinal)) continue;
            string location = objectName.Substring(4);
            bool available = Array.IndexOf(availableLocations, location) >= 0;
            button.gameObject.SetActive(available);
        }
    }

    public void SetLocation(string location)
    {
        atMap = location == CaseZeroPages.Map;
        if (mapRoot != null) mapRoot.SetActive(!mapInDocument && atMap);
        bool isInvestigationLocation = Array.IndexOf(availableLocations, location) >= 0;
        foreach (var control in barControls)
            if (control != null) control.SetActive(isInvestigationLocation && !atMap);
        if (locationLabel != null && locationLabel.text != DisplayName(location))
        {
            locationLabel.text = DisplayName(location);
            // Arriving somewhere new: the place name pops in letter by letter (Pixit).
            var fx = locationLabel.GetComponent<CaseZeroDialogueFX>();
            if (fx == null) fx = locationLabel.gameObject.AddComponent<CaseZeroDialogueFX>();
            if (locationLabel.isActiveAndEnabled) fx.RevealHeadline();
        }
        if (mapControls != null) mapControls.interactable = mapInDocument || !IsDocumentOpen;
    }

    void Awake()
    {
        Feedback = gameObject.AddComponent<CaseZeroInvestigationFeedback>();
        Feedback.Initialize(this);
        // Suspect portraits: one at a time, switched with the side arrows.
        if (deductionPortraits.Length > 0 && deductionPortraits[0] != null)
            suspectCarousel = CaseZeroSuspectCarousel.Attach(deductionPortraits[0].transform.parent.parent as RectTransform,
                suspectPreviousArrow, suspectNextArrow);
        ReleaseInvisibleClickBlockers();
        KeepTabsClickable();
        HideScreenTexts();
        foreach (var page in pages)
        {
            if (page.tab == null) continue;
            if (page.panel != null)
            {
                var reveal = page.panel.GetComponent<CaseZeroPanelMotion>();
                if (reveal == null) reveal = page.panel.AddComponent<CaseZeroPanelMotion>();
                reveal.UsePageReveal();
                // Older content containers also carried the paper entrance effect.
                foreach (var nested in page.panel.GetComponentsInChildren<CaseZeroPanelMotion>(true))
                    if (nested != reveal) nested.UsePageReveal();
            }
            var motion = page.tab.GetComponent<CaseZeroButtonMotion>();
            if (motion == null) motion = page.tab.gameObject.AddComponent<CaseZeroButtonMotion>();
            motion.SetFolderTab(false);
        }
    }
    // Invisible images (alpha ~0) that still receive clicks act like hidden walls: the suspect
    // portrait area on the Deduction page was scaled up past the top of the page and swallowed
    // clicks on the folder tabs. Only images that are not buttons themselves are released.
    void ReleaseInvisibleClickBlockers()
    {
        foreach (var page in pages)
        {
            if (page.panel == null) continue;
            foreach (var graphic in page.panel.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.raycastTarget || graphic.color.a > 0.05f) continue;
                if (graphic.GetComponent<Selectable>() != null) continue;
                graphic.raycastTarget = false;
            }
        }
    }

    // Folder tabs share their parent with the pages, so page content can end up over them. Each tab
    // gets its own sorting above the book: a click on a tab always reaches the tab.
    // The scene stays clean: no case title, objective line or status/dispatch messages on screen.
    // The texts are still updated by the game, so they can be shown again by enabling them.
    void HideScreenTexts()
    {
        foreach (var label in new[] { progress, objective, status })
            if (label != null) label.gameObject.SetActive(false);
    }

    void KeepTabsClickable()
    {
        foreach (var page in pages)
        {
            if (page.tab == null || page.tab.GetComponent<Canvas>() != null) continue;
            var parentCanvas = page.tab.transform.parent != null ? page.tab.transform.parent.GetComponentInParent<Canvas>() : null;
            var canvas = page.tab.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = (parentCanvas != null ? parentCanvas.sortingOrder : 0) + 1;
            page.tab.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    string animatedPage;
    Tween closeMotion;
    CanvasGroup documentGroup;

    /// <summary>True while the case file is visible and not in the middle of closing.</summary>
    public bool IsDocumentOpen => documentRoot != null && documentRoot.activeSelf && closeMotion == null;

    CanvasGroup DocumentGroup()
    {
        if (documentGroup == null && documentRoot != null)
        {
            documentGroup = documentRoot.GetComponent<CanvasGroup>();
            if (documentGroup == null) documentGroup = documentRoot.AddComponent<CanvasGroup>();
        }
        return documentGroup;
    }

    void CancelClose()
    {
        if (closeMotion == null) return;
        closeMotion.Kill();
        closeMotion = null;
        var group = DocumentGroup();
        if (group != null) { group.alpha = 1; group.blocksRaycasts = true; }
    }

    public void ShowPage(string name, bool canNavigate)
    {
        bool reopening = closeMotion != null;
        CancelClose();
        bool changedPage = animatedPage != name || !documentRoot.activeSelf || reopening;
        animatedPage = name;
        documentRoot.SetActive(true);
        if (objective != null) objective.gameObject.SetActive(false);
        if (caseBriefing != null) caseBriefing.gameObject.SetActive(false);
        if (worldControls != null) worldControls.interactable = false;
        if (mapControls != null) mapControls.interactable = mapInDocument && name == CaseZeroPages.Map && canNavigate;
        if (launchButtons != null)
            foreach (var button in launchButtons)
            {
                button.interactable = canNavigate;
                if (separatePanels) button.gameObject.SetActive(false);
            }
        if (separatePanels)
        {
            bool modal = name == CaseZeroPages.Introduction || name == CaseZeroPages.Confirmation || name == CaseZeroPages.Result;
            investigationRoot.SetActive(!modal && (mapInDocument || name != CaseZeroPages.Map) && name != CaseZeroPages.Phone);
            if (!backgroundMap || mapInDocument) mapRoot.SetActive(name == CaseZeroPages.Map);
            phoneRoot.SetActive(name == CaseZeroPages.Phone);
            modalRoot.SetActive(modal);
        }
        foreach (var page in pages)
        {
            bool selected = page.name == name;
            if (page.panel != null && !(backgroundMap && !mapInDocument && page.name == CaseZeroPages.Map)) {
                bool wasActive = page.panel.activeInHierarchy;
                page.panel.SetActive(selected);
                if (selected && changedPage)
                {
                    var motion = page.panel.GetComponent<CaseZeroPanelMotion>();
                    if (motion == null) motion = page.panel.AddComponent<CaseZeroPanelMotion>();
                    if (page.name == CaseZeroPages.Map) motion.UseMapReveal();
                    else if (motion != null && wasActive) motion.Play();
                }
            }
            if (page.tab != null)
            {
                page.tab.interactable = canNavigate;
                var motion = page.tab.GetComponent<CaseZeroButtonMotion>();
                if (motion != null) motion.SetFolderTab(selected);
            }
            if (page.selectedMark != null) page.selectedMark.SetActive(selected);
        }
        closeDocumentButton.interactable = canNavigate;
        documentTitle.text = DisplayName(name).ToUpperInvariant();
    }

    public void CloseDocument() => CloseDocument(true);

    public void CloseDocument(bool animate)
    {
        CancelClose();
        var group = DocumentGroup();
        if (animate && group != null && documentRoot.activeInHierarchy && isActiveAndEnabled)
        {
            // Short fade so the file is put away instead of vanishing; input is released at once.
            group.blocksRaycasts = false;
            closeMotion = group.DOFade(0, 0.14f).SetEase(Ease.InSine).SetUpdate(true)
                .OnComplete(() =>
                {
                    closeMotion = null;
                    documentRoot.SetActive(false);
                    group.alpha = 1;
                    group.blocksRaycasts = true;
                });
        }
        else documentRoot.SetActive(false);
        // The objective line stays hidden (HideScreenTexts); the scene shows only the investigation.
        if (caseBriefing != null) caseBriefing.gameObject.SetActive(false);
        if (worldControls != null) worldControls.interactable = true;
        if (backgroundMap) mapRoot.SetActive(!mapInDocument && atMap);
        if (mapControls != null) mapControls.interactable = true;
        if (launchButtons != null)
            foreach (var button in launchButtons)
            {
                button.gameObject.SetActive(true);
                button.interactable = true;
            }
    }

    CaseZeroSuspectCarousel suspectCarousel;
    public void ShowSelection(GameObject[] checks, int selected)
    {
        // The red tint of the chosen option replaces the Marcador_Selecionado strip.
        bool suspectCarouselActive = checks == suspectChecks && suspectCarousel != null;
        for (int i = 0; i < checks.Length; i++)
            if (checks[i] != null) checks[i].SetActive(false);
        if (suspectCarouselActive)
        {
            suspectCarousel.MarkChosen(selected);
            suspectCarousel.Show(selected, true);
        }
        else TintChosen(checks, selected);
    }

    readonly System.Collections.Generic.Dictionary<Graphic, Color> restingColors = new System.Collections.Generic.Dictionary<Graphic, Color>();
    // Weapon and place options: keep the chosen button tinted with its own Selected colour.
    void TintChosen(GameObject[] checks, int selected)
    {
        for (int i = 0; i < checks.Length; i++)
        {
            if (checks[i] == null || checks[i].transform.parent == null) continue;
            var button = checks[i].transform.parent.GetComponentInParent<Button>(true);
            if (button == null || button.targetGraphic == null) continue;
            var graphic = button.targetGraphic;
            if (!restingColors.TryGetValue(graphic, out var resting)) restingColors[graphic] = resting = graphic.color;
            Color tint = button.transition == Selectable.Transition.ColorTint ? button.colors.selectedColor : new Color(1f, 0.78f, 0.78f);
            bool wasChosen = graphic.color != resting;
            graphic.color = i == selected ? resting * tint : resting;
            // Newly chosen weapon/place: a quick "stamp" on the option.
            if (i == selected && !wasChosen && isActiveAndEnabled)
            {
                selectionMotion?.Kill(true);
                selectionMotion = button.transform.DOPunchScale(Vector3.one * 0.06f, 0.28f, 5, 0.6f).SetUpdate(true);
            }
        }
    }

    Tween selectionMotion;
    Sequence choicesMotion;
    /// <summary>Interview answers fade in one after another once the line starts.</summary>
    public void AnimateInterviewChoices()
    {
        if (interviewChoices == null || !isActiveAndEnabled) return;
        choicesMotion?.Kill(true);
        choicesMotion = DOTween.Sequence().SetUpdate(true);
        int order = 0;
        foreach (var choice in interviewChoices)
        {
            if (choice == null || !choice.gameObject.activeInHierarchy) continue;
            var group = choice.GetComponent<CanvasGroup>();
            if (group == null) group = choice.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            choicesMotion.Insert(0.12f + order++ * 0.06f, group.DOFade(1, 0.22f).SetEase(Ease.OutSine));
        }
    }

    /// <summary>Shows the clue's drawing in place of its scene marker tag (the text tag is hidden),
    /// or the plain tag when the clue has no drawing. The drawing is the object itself, without a
    /// frame (the polaroid is only used on the Evidence card). Clicking it clicks the marker.</summary>
    public void ShowMarkerArt(int index, string artName, bool recorded)
    {
        if (evidenceMarkers == null || index < 0 || index >= evidenceMarkers.Length || evidenceMarkers[index] == null) return;
        var marker = evidenceMarkers[index].transform;
        Sprite picture = artLibrary != null && !string.IsNullOrEmpty(artName) ? artLibrary.Find(artName) : null;
        var existing = marker.Find("Desenho_Pista");
        bool show = picture != null;
        // The tag and its text only show for clues without a drawing.
        var tag = marker.GetComponent<Image>();
        if (tag != null) tag.enabled = !show;
        if (evidenceMarkerLabels != null && index < evidenceMarkerLabels.Length && evidenceMarkerLabels[index] != null)
            evidenceMarkerLabels[index].gameObject.SetActive(!show);
        if (!show) { if (existing != null) existing.gameObject.SetActive(false); return; }
        if (existing == null) existing = CreateMarkerArt(marker);
        existing.gameObject.SetActive(true);
        var drawing = existing.GetComponent<Image>();
        drawing.sprite = picture;
        // A recorded clue is slightly dimmed.
        drawing.color = recorded ? new Color(0.65f, 0.62f, 0.6f) : Color.white;
    }

    Transform CreateMarkerArt(Transform marker)
    {
        // Just the drawing of the object, standing where the marker tag was. It can be moved or
        // resized in play mode under the marker ("Desenho_Pista").
        var drawingObject = new GameObject("Desenho_Pista", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = drawingObject.GetComponent<RectTransform>();
        rect.SetParent(marker, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(190, 140);
        rect.SetAsFirstSibling();
        var drawing = drawingObject.GetComponent<Image>();
        drawing.preserveAspect = true;
        // A soft light edge keeps the dark ink readable against the dark scene.
        var outline = drawingObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.92f, 0.85f, 0.35f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return rect;
    }

    Tween markerMotion;
    /// <summary>Scene marker feedback when its clue is recorded.</summary>
    public void PulseMarker(int index)
    {
        if (evidenceMarkers == null || index < 0 || index >= evidenceMarkers.Length || evidenceMarkers[index] == null) return;
        markerMotion?.Kill(true);
        markerMotion = evidenceMarkers[index].transform.DOPunchScale(Vector3.one * 0.18f, 0.38f, 6, 0.6f).SetUpdate(true);
    }

    Tween detailsMotion;
    /// <summary>Quick crossfade on the evidence sheet when another record is shown.</summary>
    public void AnimateEvidenceDetails()
    {
        if (evidenceDetails == null || !evidenceDetails.activeInHierarchy) return;
        var group = evidenceDetails.GetComponent<CanvasGroup>();
        if (group == null) group = evidenceDetails.AddComponent<CanvasGroup>();
        detailsMotion?.Kill();
        group.alpha = 0.25f;
        detailsMotion = group.DOFade(1, 0.2f).SetEase(Ease.OutSine).SetUpdate(true);
    }

    SpankyBoy.JuiceUI.Free.Animate_Pulse_Free nextCasePulse;
    /// <summary>Result page: the headline pops in letter by letter (Pixit) and, after a
    /// win, the "next case" button keeps a gentle Juice UI pulse to draw the eye.</summary>
    public void AnimateResult(bool solved)
    {
        if (documentTitle != null)
        {
            var fx = documentTitle.GetComponent<CaseZeroDialogueFX>();
            if (fx == null) fx = documentTitle.gameObject.AddComponent<CaseZeroDialogueFX>();
            fx.RevealHeadline();
        }
        if (nextCaseButton == null) return;
        if (nextCasePulse == null)
        {
            bool wasActive = nextCaseButton.gameObject.activeSelf;
            nextCaseButton.gameObject.SetActive(false); // Configure before the pulse's OnEnable runs.
            nextCasePulse = nextCaseButton.gameObject.AddComponent<SpankyBoy.JuiceUI.Free.Animate_Pulse_Free>();
            nextCasePulse.pulseScale = 1.05f;
            nextCasePulse.duration = 0.9f;
            nextCasePulse.playOnEnable = false;
            nextCaseButton.gameObject.SetActive(wasActive);
        }
        if (solved && nextCaseButton.gameObject.activeInHierarchy) nextCasePulse.StartPulse();
        else nextCasePulse.StopPulse();
    }

    Tween statusMotion;
    /// <summary>Status line fades in whenever its message changes.</summary>
    public void SetStatus(string message)
    {
        if (status == null) return;
        message ??= "";
        if (status.text == message) return;
        status.text = message;
        statusMotion?.Kill();
        if (message.Length == 0 || !status.isActiveAndEnabled) { status.alpha = 1; return; }
        status.alpha = 0;
        statusMotion = DOTween.To(() => status.alpha, value => status.alpha = value, 1f, 0.3f)
            .SetEase(Ease.OutSine).SetUpdate(true);
    }
    // Inspector previews only activate existing objects; no generation or deletion.
    [ContextMenu("Pré-visualizar/Mapa")] void PreviewMap() => ShowPage(CaseZeroPages.Map, true);
    [ContextMenu("Pré-visualizar/Evidências")] void PreviewEvidence() => ShowPage(CaseZeroPages.Evidence, true);
    [ContextMenu("Pré-visualizar/Interrogatório")] void PreviewInterview() => ShowPage(CaseZeroPages.Interviews, true);
    [ContextMenu("Pré-visualizar/Ligações")] void PreviewPhone() => ShowPage(CaseZeroPages.Phone, true);
    [ContextMenu("Pré-visualizar/Dedução")] void PreviewDeduction() => ShowPage(CaseZeroPages.Deduction, true);
    [ContextMenu("Pré-visualizar/Opções")] void PreviewOptions() => ShowPage(CaseZeroPages.Options, true);
    [ContextMenu("Pré-visualizar/Fechar documento")] void PreviewClose() => CloseDocument();
}
