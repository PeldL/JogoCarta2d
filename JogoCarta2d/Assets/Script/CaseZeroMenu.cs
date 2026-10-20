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
            rect.anchoredPosition = origin + Vector2.left * (child.name == "Titulo" ? 85 : 55);
            var fade = child.GetComponent<CanvasGroup>();
            if (fade == null) fade = child.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 0;
            float delay = child.name == "Titulo" ? 0 : 0.22f + order++ * 0.09f;
            entrance.Insert(delay, rect.DOAnchorPos(origin, 0.65f).SetEase(Ease.OutCubic));
            entrance.Insert(delay, fade.DOFade(1, 0.45f));
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

    void Start() { ClosePanels(); Refresh(); PlayEntrance(); if (CaseZeroSession.ShowCredits) { CaseZeroSession.ShowCredits = false; OpenCredits(); } }
    void Refresh()
    {
        bool any = false;
        for (int slot = 1; slot <= CaseZeroSave.SlotCount; slot++)
        {
            var saved = CaseZeroSave.Read(out _, slot);
            any |= saved != null;
            string summary = saved == null ? (CaseZeroSave.ExistsInSlot(slot) ? "Indisponível" : "Vazio")
                : saved.solved ? "Caso resolvido" : saved.failed ? "Caso encerrado • derrota" : $"{saved.evidence.Count}/5 provas";
            slotLabels[slot - 1].text = $"{(selectedSlot == slot ? "• " : "")}SLOT {slot}\n{(saved != null ? "Caso " + (saved.CaseIndex + 1) + " • " : "")}{summary}";
        }
        continueButton.interactable = any;
        proceedButton.interactable = newGame || CaseZeroSave.Read(out _, selectedSlot) != null;
    }
    public void NewGame() { OpenSlots(true); }
    public void ContinueGame() { OpenSlots(false); }
    void OpenSlots(bool fresh)
    {
        if (loading) return;
        ClosePanels(); newGame = fresh;
        if (!fresh)
            for (int slot = 1; slot <= CaseZeroSave.SlotCount; slot++)
                if (CaseZeroSave.Read(out _, slot) != null) { selectedSlot = slot; break; }
        title.text = fresh ? "NOVO JOGO • ESCOLHA UM SLOT" : "CONTINUAR • ESCOLHA UM SLOT";
        status.text = "Selecione o arquivo da investigação.";
        slotPanel.SetActive(true); Refresh();
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
            confirmation.text = $"Iniciar um novo jogo substituirá o SLOT {selectedSlot}. Deseja continuar?";
            confirmationPanel.SetActive(true);
        }
        else Launch();
    }
    public void ConfirmNewGame() { if (confirmationPanel.activeSelf) Launch(); }
    public void CancelConfirmation() { confirmationPanel.SetActive(false); }
    void Launch()
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded("UI")) { status.text = "Inclua a cena UI no perfil de build."; return; }
        if (!newGame && CaseZeroSave.Read(out _, selectedSlot) == null) { Refresh(); return; }
        loading = true;
        CaseZeroSession.Request(selectedSlot, newGame);
        FinishEntrance();
        canvasGroup.interactable = false;
        CaseZeroSceneLoading.Load("UI", transitionTemplate, sceneTransition, title != null ? title.font : null);
    }
    public void OpenOptions() { if (loading) return; ClosePanels(); optionsPanel.SetActive(true); }
    public void OpenCredits() { if (loading) return; ClosePanels(); creditsPanel.SetActive(true); }
    public void ToggleFullscreen() { Screen.fullScreen = !Screen.fullScreen; }
    public void ClosePanels()
    {
        if (loading) return;
        FinishEntrance();
        slotPanel.SetActive(false); optionsPanel.SetActive(false); confirmationPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
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
