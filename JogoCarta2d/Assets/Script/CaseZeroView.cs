using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI objects are saved in the scenes. This component only changes their state.
public sealed class CaseZeroView : MonoBehaviour
{
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
    public SpankyBoy.JuiceUI.Free.Animate_Pulse_Free incomingCallPulse;

    [Header("Introdução")]
    public Button continueButton;

    [Header("Local do crime • fora do documento")]
    public Button[] evidenceMarkers;
    public TMP_Text[] evidenceMarkerLabels;
    public TMP_Text cameraLabel;

    [Header("Evidências • fichas fixas")]
    public Button[] evidenceCards;
    public GameObject emptyEvidenceNotice;
    public GameObject evidenceDetails;
    public TMP_Text evidenceName;
    public TMP_Text evidenceDescription;
    public TMP_Text evidenceType;

    [Header("Interrogatório")]
    public TMP_Text interviewMessage;
    public Button[] interviewChoices;
    public TMP_Text[] interviewChoiceLabels;

    [Header("Ligações")]
    public TMP_Text phoneState;
    public TMP_Text phoneMessage;
    public Button answerPhoneButton;
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
        evidenceNoticeText.text = "NOVA EVIDÊNCIA\n" + name + "\nAdicionada ao documento.";
        evidenceNotice.SetActive(true);
        noticeRoutine = StartCoroutine(HideNotice());
    }
    System.Collections.IEnumerator HideNotice()
    {
        yield return new WaitForSecondsRealtime(3.5f);
        evidenceNotice.SetActive(false);
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

    public void SetLocation(string location)
    {
        atMap = location == "Mapa";
        mapRoot.SetActive(!mapInDocument && atMap);
        foreach (var control in barControls) control.SetActive(location == "Bar");
        if (locationLabel != null) locationLabel.text = location;
        if (mapControls != null) mapControls.interactable = mapInDocument || !documentRoot.activeSelf;
    }

    public void ShowPage(string name, bool canNavigate)
    {
        documentRoot.SetActive(true);
        if (objective != null) objective.gameObject.SetActive(false);
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
            if (page.panel != null && !(backgroundMap && !mapInDocument && page.name == "Mapa")) page.panel.SetActive(selected);
            if (page.tab != null) page.tab.interactable = canNavigate;
            if (page.selectedMark != null) page.selectedMark.SetActive(selected);
        }
        closeDocumentButton.interactable = canNavigate;
        documentTitle.text = name.ToUpperInvariant();
    }

    public void CloseDocument()
    {
        documentRoot.SetActive(false);
        if (objective != null) objective.gameObject.SetActive(true);
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
