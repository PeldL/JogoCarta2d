using Pages = CaseZeroPages;

// Save slots, the confirmation page (overwrite, delete, restart, accusation), save and load.
public sealed partial class CaseZeroGame
{
    int activeSlot = 1;
    int selectedSlot = 1;
    int confirmationSlot;
    bool confirming;
    bool saveCopyConfirmation;
    bool deleteConfirmation;
    bool accusationConfirmation;
    bool restartCurrentCase;
    string returnPage = Pages.Map;

    void ShowWelcome()
    {
        view.Feedback?.ResetFeedback();
        RefreshSlots();
        view.ShowPage(Pages.Introduction, false);
        Say("Start or continue an investigation.");
    }

    public void StartNewGame()
    {
        if (CaseZeroSave.ExistsInSlot(selectedSlot)) ShowSlotConfirmation(false);
        else StartInSlot(selectedSlot);
    }

    public void SelectSlot(int slot)
    {
        if (slot < 1 || slot > CaseZeroSave.SlotCount || confirming) return;
        selectedSlot = slot;
        RefreshSlots();
        Say($"Slot {slot} selected. Choose to start, save, or load.");
    }

    void RefreshSlots()
    {
        for (int slot = 1; slot <= CaseZeroSave.SlotCount; slot++)
        {
            var saved = CaseZeroSave.Read(out _, slot);
            string summary = saved != null ? (saved.solved ? "Case solved" : saved.failed ? "Case closed • defeat" : $"Under investigation • {TimeText(saved.elapsed)}")
                : CaseZeroSave.ExistsInSlot(slot) ? "Save unavailable" : "Empty";
            string label = $"{(slot == selectedSlot ? "• " : "")}SLOT {slot}\n{(saved != null ? "Case " + (saved.CaseIndex + 1) + " • " : "")}{summary}";
            for (int i = slot - 1; i < view.slotLabels.Length; i += CaseZeroSave.SlotCount) view.slotLabels[i].text = label;
        }
        view.continueButton.interactable = view.loadButton.interactable = CaseZeroSave.Read(out _, selectedSlot) != null;
        foreach (var button in view.deleteButtons) button.interactable = CaseZeroSave.ExistsInSlot(selectedSlot);
        view.slotContext.text = state == null ? $"Selected destination: slot {selectedSlot}"
            : $"Autosave: slot {activeSlot} • Save/load: slot {selectedSlot}";
    }

    void StartInSlot(int slot)
    {
        var fresh = new CaseZeroState();
        if (!CaseZeroSave.Write(fresh, out string error, slot)) { Say(error); return; }
        activeSlot = selectedSlot = slot;
        Begin(fresh);
        RefreshSlots();
        Say($"New investigation in slot {slot}. Autosave uses this slot.");
    }

    public void ConfirmRestart()
    {
        if (state != null) selectedSlot = activeSlot;
        ShowSlotConfirmation(false);
        restartCurrentCase = state != null;
        if (restartCurrentCase) view.confirmationMessage.text = "Restart only the current case? Its clues and attempts will be reset. Previously completed cases will be preserved.";
    }

    void ShowSlotConfirmation(bool saveCopy)
    {
        restartCurrentCase = false;
        accusationConfirmation = false;
        deleteConfirmation = false;
        confirming = true;
        saveCopyConfirmation = saveCopy;
        confirmationSlot = selectedSlot;
        returnPage = state == null ? Pages.Introduction : state.page;
        paused = true;
        view.ShowPage(Pages.Confirmation, false);
        view.documentTitle.text = "CONFIRM OVERWRITE";
        view.confirmationButtonLabel.text = "Confirm overwrite";
        view.confirmationMessage.text = saveCopy
            ? $"Saving the current investigation to SLOT {confirmationSlot} overwrites its existing progress.\n\nAutosave will then use this slot. Continue?"
            : $"Starting a new case in SLOT {confirmationSlot} overwrites its existing progress.\n\nThe other slots will remain intact. Continue?";
        if (state != null) state.documentOpen = true;
    }

    public void CancelRestart()
    {
        accusationConfirmation = false;
        confirming = false;
        deleteConfirmation = false;
        paused = false;
        if (state == null) ShowWelcome();
        else OpenPage(NormalizePage(returnPage));
    }

    /// <summary>The confirm button of the confirmation page, whatever it is confirming.</summary>
    public void RestartConfirmed()
    {
        if (!confirming) return;
        if (accusationConfirmation) { AccusationConfirmed(); return; }
        if (deleteConfirmation) { DeleteConfirmed(); return; }
        int slot = confirmationSlot;
        bool saveCopy = saveCopyConfirmation;
        var retry = restartCurrentCase && state != null ? state.RestartCase() : null;
        restartCurrentCase = false;
        CancelRestart();
        if (saveCopy) SaveToSlot(slot);
        else if (retry != null)
        {
            if (!CaseZeroSave.Write(retry, out string error, slot)) { Say(error); return; }
            Begin(retry);
        }
        else StartInSlot(slot);
    }

    public void ConfirmDeleteSave()
    {
        if (confirming || !CaseZeroSave.ExistsInSlot(selectedSlot)) return;
        ShowSlotConfirmation(false);
        deleteConfirmation = true;
        view.documentTitle.text = "DELETE SAVE";
        view.confirmationButtonLabel.text = "Delete permanently";
        view.confirmationMessage.text = $"Delete SLOT {confirmationSlot} and its backup files? This cannot be undone.\n\n"
            + (state != null && confirmationSlot == activeSlot
                ? "The current investigation will end and you will return to the introduction."
                : "The other slots and the current investigation will remain intact.");
    }

    void DeleteConfirmed()
    {
        int slot = confirmationSlot;
        if (!CaseZeroSave.Delete(slot, out string error)) { Say(error); return; }
        // Clear the live state BEFORE navigation or pause/quit can autosave it again.
        if (state != null && slot == activeSlot)
        {
            state = null;
            restoring = true;
            view.phonePresentation.ShowImmediate("");
            phone.ResetCalls();
            interrogation.EndInterrogation();
            restoring = false;
            saveTimer = 0;
            selectedEvidence = null;
            if (view.incomingCallPulse != null) view.incomingCallPulse.enabled = false;
            view.Feedback?.ResetFeedback();
            view.progress.text = "NO ACTIVE INVESTIGATION";
            view.phoneNotification.text = "Start or load an investigation.";
        }
        CancelRestart();
        RefreshSlots();
        Say($"Slot {slot} deleted, including its backups.");
    }

    public void SaveGame()
    {
        if (state == null || confirming) return;
        if (selectedSlot != activeSlot && CaseZeroSave.ExistsInSlot(selectedSlot)) ShowSlotConfirmation(true);
        else SaveToSlot(selectedSlot);
    }

    void SaveToSlot(int slot)
    {
        if (!CaseZeroSave.Write(state, out string error, slot)) { Say(error); return; }
        activeSlot = selectedSlot = slot;
        saveTimer = 0;
        RefreshSlots();
        Say($"Investigation saved to slot {slot}. Autosave now uses this slot.");
    }

    /// <summary>Autosave to the active slot.</summary>
    void Save(bool manual)
    {
        if (state == null || restoring) return;
        saveTimer = 0;
        if (!CaseZeroSave.Write(state, out string error, activeSlot)) Say(error);
        else if (manual) Say($"Investigation saved to slot {activeSlot}.");
    }

    public void LoadGame()
    {
        if (confirming) return;
        var loaded = CaseZeroSave.Read(out string notice, selectedSlot);
        if (loaded == null) { Say(notice); return; }
        activeSlot = selectedSlot;
        Begin(loaded);
        RefreshSlots();
        Say(notice ?? $"Investigation restored from slot {activeSlot}.");
    }
}
