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
            case "Evidências": return "Evidence";
            case "Interrogatório": return "Interviews";
            case "Dedução": return "Deduction";
            case "Opções": return "Settings";
            case "Suspeitos": return "Suspects";
            case "Introdução": return "Introduction";
            case "Confirmar reinício": return "Confirm restart";
            case "Resultado": return "Results";
            case "Mapa": return "Map";
            case "Ligações": case "Telefone": return "Phone";
            case "Local": return "Crime scene";
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

    [Header("Interrogatório")]
    public TMP_Text interviewMessage;
    [Header("Retratos • arraste as artes na biblioteca")]
    public CaseZeroArtLibrary artLibrary;
    public Image interviewPortrait;
    public GameObject firstCaseVictim;
    public TMP_Text victimCaption;
    public TMP_Text interviewPortraitName;
    public Image[] deductionPortraits = new Image[0];
    Tween portraitFade;
    string portraitIdentity;

    public void ShowPortraits(CaseZeroCase data, int selected)
    {
        if (artLibrary == null) return;
        if (firstCaseVictim != null)
        {
            // Reuse the existing portrait slot for each campaign victim.
            string victimArt = data.id == "ultima-testemunha" ? "Vítima 1 - Mulher no bar"
                : data.id == "apartamento-302" ? "Vítima 2"
                : data.id == "arquivo-vertice" ? "Vítima 3" : null;
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
                string label = data.id == "ultima-testemunha" ? "Woman from the bar"
                    : data.id == "apartamento-302" ? "Victim 2" : "Victim 3";
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
                deductionPortraits[i].sprite = artLibrary.FindPortrait(data.id, i);
                deductionPortraits[i].preserveAspect = true;
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
        if (changed)
        {
            portraitFade?.Kill();
            var color = interviewPortrait.color; color.a = 0; interviewPortrait.color = color;
            portraitFade = interviewPortrait.DOFade(1, 0.25f).SetUpdate(true);
        }
    }
    void OnDestroy() => portraitFade?.Kill();
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
        atMap = location == "Mapa";
        if (mapRoot != null) mapRoot.SetActive(!mapInDocument && atMap);
        bool isInvestigationLocation = Array.IndexOf(availableLocations, location) >= 0;
        foreach (var control in barControls)
            if (control != null) control.SetActive(isInvestigationLocation && !atMap);
        if (locationLabel != null) locationLabel.text = DisplayName(location);
        if (mapControls != null) mapControls.interactable = mapInDocument || !documentRoot.activeSelf;
    }

    void Awake()
    {
        Feedback = gameObject.AddComponent<CaseZeroInvestigationFeedback>();
        Feedback.Initialize(this);
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
    string animatedPage;
    public void ShowPage(string name, bool canNavigate)
    {
        bool changedPage = animatedPage != name || !documentRoot.activeSelf;
        animatedPage = name;
        documentRoot.SetActive(true);
        if (objective != null) objective.gameObject.SetActive(false);
        if (caseBriefing != null) caseBriefing.gameObject.SetActive(false);
        if (worldControls != null) worldControls.interactable = false;
        if (mapControls != null) mapControls.interactable = mapInDocument && name == "Mapa" && canNavigate;
        if (launchButtons != null)
            foreach (var button in launchButtons)
            {
                button.interactable = canNavigate;
                if (separatePanels) button.gameObject.SetActive(false);
            }
        if (separatePanels)
        {
            bool modal = name == "Introdução" || name == "Confirmar reinício" || name == "Resultado";
            investigationRoot.SetActive(!modal && (mapInDocument || name != "Mapa") && name != "Ligações");
            if (!backgroundMap || mapInDocument) mapRoot.SetActive(name == "Mapa");
            phoneRoot.SetActive(name == "Ligações");
            modalRoot.SetActive(modal);
        }
        foreach (var page in pages)
        {
            bool selected = page.name == name;
            if (page.panel != null && !(backgroundMap && !mapInDocument && page.name == "Mapa")) {
                bool wasActive = page.panel.activeInHierarchy;
                page.panel.SetActive(selected);
                if (selected && changedPage)
                {
                    var motion = page.panel.GetComponent<CaseZeroPanelMotion>();
                    if (motion == null) motion = page.panel.AddComponent<CaseZeroPanelMotion>();
                    if (page.name == "Mapa") motion.UseMapReveal();
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

    public void CloseDocument()
    {
        documentRoot.SetActive(false);
        if (objective != null) objective.gameObject.SetActive(true);
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

    public void ShowSelection(GameObject[] checks, int selected)
    {
        for (int i = 0; i < checks.Length; i++) checks[i].SetActive(i == selected);
    }

    // Inspector previews only activate existing objects; no generation or deletion.
    [ContextMenu("Pré-visualizar/Mapa")] void PreviewMap() => ShowPage("Mapa", true);
    [ContextMenu("Pré-visualizar/Evidências")] void PreviewEvidence() => ShowPage("Evidências", true);
    [ContextMenu("Pré-visualizar/Interrogatório")] void PreviewInterview() => ShowPage("Interrogatório", true);
    [ContextMenu("Pré-visualizar/Ligações")] void PreviewPhone() => ShowPage("Ligações", true);
    [ContextMenu("Pré-visualizar/Dedução")] void PreviewDeduction() => ShowPage("Dedução", true);
    [ContextMenu("Pré-visualizar/Opções")] void PreviewOptions() => ShowPage("Opções", true);
    [ContextMenu("Pré-visualizar/Fechar documento")] void PreviewClose() => CloseDocument();
}
