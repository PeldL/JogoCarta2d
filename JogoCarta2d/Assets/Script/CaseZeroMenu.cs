using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Every control and panel is authored in Menu.unity; no runtime UI generation.
public sealed class CaseZeroMenu : MonoBehaviour
{
    public GameObject slotPanel, optionsPanel, confirmationPanel;
    public GameObject creditsPanel;
    public Button continueButton, proceedButton;
    public TMP_Text[] slotLabels;
    public TMP_Text title, status, confirmation;
    public CanvasGroup canvasGroup;
    [Header("Easy Transitions • abertura da investigação")]
    [SerializeField] GameObject transitionTemplate;
    [SerializeField] EasyTransition.TransitionSettings sceneTransition;
    int selectedSlot = 1;
    bool newGame, loading;
    Sequence entrance;
    readonly System.Collections.Generic.Dictionary<RectTransform, Vector2> entrancePositions = new System.Collections.Generic.Dictionary<RectTransform, Vector2>();

    void Awake()
    {
        // Hide before the first rendered frame; Start prepares the individual controls.
        if (canvasGroup != null) canvasGroup.alpha = 0;
    }

    void PlayEntrance()
    {
        if (canvasGroup == null) return;
        entrance = DOTween.Sequence().SetUpdate(true);
        entrance.Insert(0, canvasGroup.DOFade(1, 0.7f).SetEase(Ease.InOutSine));
        int order = 0;
        foreach (Transform child in canvasGroup.transform)
        {
            var rect = child as RectTransform;
            if (rect == null || !child.gameObject.activeSelf ||
                (child.GetComponent<Button>() == null && child.name != "Titulo")) continue;
            Vector2 origin = rect.anchoredPosition;
            entrancePositions[rect] = origin;
            rect.anchoredPosition = origin + Vector2.left * (child.name == "Titulo" ? 24 : 16);
            var fade = child.GetComponent<CanvasGroup>();
            if (fade == null) fade = child.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0;
            float delay = child.name == "Titulo" ? 0 : 0.10f + order++ * 0.065f;
            entrance.Insert(delay, rect.DOAnchorPos(origin, 0.62f).SetEase(Ease.OutBack, 0.35f));
            entrance.Insert(delay, fade.DOFade(1, 0.6f).SetEase(Ease.InOutSine));
        }
    }
    void FinishEntrance()
    {
        entrance?.Complete();
        entrance?.Kill();
        foreach (var entry in entrancePositions)
            if (entry.Key != null) entry.Key.anchoredPosition = entry.Value;
    }
    void OnDestroy() => entrance?.Kill();

    void Start()
    {
        ResetPanels();
        EnsureButtonMotion();
        Refresh();
        PlayEntrance();
        if (CaseZeroSession.ShowCredits) { CaseZeroSession.ShowCredits = false; OpenCredits(); }
    }

    void EnsureButtonMotion()
    {
        if (canvasGroup == null) return;
        foreach (var button in canvasGroup.GetComponentsInChildren<Button>(true))
            if (button.GetComponent<CaseZeroButtonMotion>() == null)
                button.gameObject.AddComponent<CaseZeroButtonMotion>();
    }

    void Refresh()
    {
        bool any = false;
        for (int slot = 1; slot <= CaseZeroSave.SlotCount; slot++)
        {
            var saved = CaseZeroSave.Read(out _, slot);
            any |= saved != null;
            string summary = saved == null ? (CaseZeroSave.ExistsInSlot(slot) ? "Unavailable" : "Empty")
                : saved.solved ? "Case solved" : saved.failed ? "Case closed • defeat" : "Under investigation";
            if (slotLabels != null && slot - 1 < slotLabels.Length && slotLabels[slot - 1] != null)
                slotLabels[slot - 1].text = $"Slot {slot}\n{(saved != null ? "Case " + (saved.CaseIndex + 1) + " • " : "")}{summary}";
        }
        if (continueButton != null) continueButton.interactable = any;
        if (proceedButton != null) proceedButton.interactable = newGame || CaseZeroSave.Read(out _, selectedSlot) != null;
    }
    public void NewGame() { OpenSlots(true); }
    public void ContinueGame() { OpenSlots(false); }
    void OpenSlots(bool fresh)
    {
        if (loading) return;
        ResetPanels(); newGame = fresh;
        if (!fresh)
            for (int slot = 1; slot <= CaseZeroSave.SlotCount; slot++)
                if (CaseZeroSave.Read(out _, slot) != null) { selectedSlot = slot; break; }
        if (title != null) title.text = fresh ? "NEW GAME • CHOOSE A SLOT" : "CONTINUE • CHOOSE A SLOT";
        if (status != null) status.text = "Select an investigation file.";
        if (slotPanel != null) slotPanel.SetActive(true);
        SetMainMenuButtonsVisible(false);
        Refresh();
    }
    public void SelectSlot(int slot)
    {
        if (loading || slot < 1 || slot > CaseZeroSave.SlotCount) return;
        selectedSlot = slot; Refresh();
    }
    public void Proceed()
    {
        if (loading) return;
        if (newGame && CaseZeroSave.ExistsInSlot(selectedSlot))
        {
            if (confirmation == null || confirmationPanel == null)
            {
                if (status != null) status.text = $"Slot {selectedSlot} already has a save. The overwrite confirmation panel is not connected.";
                return;
            }
            confirmation.text = $"Starting a new game will overwrite Slot {selectedSlot}. Continue?";
            confirmationPanel.SetActive(true);
        }
        else Launch();
    }
    public void ConfirmNewGame() { if (confirmationPanel != null && confirmationPanel.activeSelf) Launch(); }
    public void CancelConfirmation() { if (confirmationPanel != null) confirmationPanel.SetActive(false); }
    void Launch()
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded("Game")) { if (status != null) status.text = "Add the Game scene to the build profile."; return; }
        if (!newGame && CaseZeroSave.Read(out _, selectedSlot) == null) { Refresh(); return; }
        loading = true;
        CaseZeroSession.Request(selectedSlot, newGame);
        FinishEntrance();
        canvasGroup.interactable = false;
        CaseZeroSceneLoading.Load("Game", transitionTemplate, sceneTransition, title != null ? title.font : null);
    }
    public void OpenOptions() { if (loading) return; ResetPanels(); if (optionsPanel != null) optionsPanel.SetActive(true); SetMainMenuButtonsVisible(false); }
    public void OpenCredits() { if (loading) return; ResetPanels(); if (creditsPanel != null) creditsPanel.SetActive(true); SetMainMenuButtonsVisible(false); }
    public void ToggleFullscreen() { Screen.fullScreen = !Screen.fullScreen; }
    public void ClosePanels()
    {
        if (loading) return;
        FinishEntrance();
        HidePanel(slotPanel); HidePanel(optionsPanel); HidePanel(confirmationPanel); HidePanel(creditsPanel);
        SetMainMenuButtonsVisible(true);
    }
    static void HidePanel(GameObject panel)
    {
        if (panel == null || !panel.activeSelf) return;
        var motion = panel.GetComponent<CaseZeroPanelMotion>();
        if (motion != null) motion.Hide(); else panel.SetActive(false);
    }
    void ResetPanels()
    {
        if (loading) return;
        FinishEntrance();
        if (slotPanel != null) slotPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        SetMainMenuButtonsVisible(true);
    }

    void SetMainMenuButtonsVisible(bool visible)
    {
        if (canvasGroup == null) return;
        foreach (Transform child in canvasGroup.transform)
        {
            var button = child.GetComponent<Button>();
            if (button != null) button.gameObject.SetActive(visible);
        }
    }
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
