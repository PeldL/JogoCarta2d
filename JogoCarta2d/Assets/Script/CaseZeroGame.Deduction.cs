using Pages = CaseZeroPages;

// The accusation: culprit, weapon and place, confirmed with up to three attempts.
public sealed partial class CaseZeroGame
{
    // Scene buttons pass the on-screen slot; the state keeps the suspect index.
    public void SelectSuspect(int slot)
    {
        if (!CanChangeTheory() || slot < 0 || slot >= CurrentCase.suspects.Length) return;
        state.suspect = CurrentCase.SuspectAtSlot(slot);
        RefreshDeduction();
    }

    public void SelectWeapon(int index)
    {
        if (!CanChangeTheory() || index < 0 || index >= CurrentCase.objects.Length) return;
        state.weapon = index;
        RefreshDeduction();
    }

    public void SelectPlace(int index)
    {
        if (!CanChangeTheory() || index < 0 || index >= CurrentCase.places.Length) return;
        state.place = index;
        RefreshDeduction();
    }

    bool CanChangeTheory() => state != null && !state.Finished && !confirming;

    void RefreshDeduction()
    {
        view.ShowSelection(view.suspectChecks, CurrentCase.SlotOfSuspect(state.suspect));
        view.ShowSelection(view.weaponChecks, state.weapon);
        view.ShowSelection(view.placeChecks, state.place);
        view.concludeButton.interactable = state.CanConclude && state.HasTheory && !state.Finished;
        view.deductionHint.text = state.Finished ? "This case is already closed. Use the results screen to restart it or continue."
            : state.CanConclude && !state.HasTheory ? "Choose the culprit, the weapon, and the location before confirming."
            : state.CanConclude ? $"Review your theory before confirming. {state.AttemptsRemaining} accusation attempt(s) remaining."
            : "Still missing before an accusation:\n• " + string.Join("\n• ", state.MissingForConclusion());
    }

    public void ConcludeCase()
    {
        if (state == null || paused || state.Finished || !state.CanConclude || !state.HasTheory || confirming) return;
        ShowSlotConfirmation(false);
        accusationConfirmation = true;
        view.documentTitle.text = "CONFIRM ACCUSATION";
        view.confirmationButtonLabel.text = "Confirm accusation";
        view.confirmationMessage.text = $"You have {state.AttemptsRemaining} attempt(s).\n\nAn incorrect accusation costs one attempt. After three mistakes, the investigation ends in defeat.\n\nConfirm the selected combination?";
    }

    void AccusationConfirmed()
    {
        accusationConfirmation = false;
        confirming = false;
        paused = false;
        bool solved = state.Accuse();
        OpenPage(state.Finished ? Pages.Result : Pages.Deduction);
        Save(false);
        if (solved) view.Feedback?.ShowVictory(state.Score, state.Rating, state.CaseIndex == CaseZeroCases.All.Length - 1);
        else view.Feedback?.ShowWrongAccusation(state.AttemptsRemaining, state.failed);
        Say(solved ? "Case closed. Your evaluation has been saved." : state.failed ? "No attempts remaining. You can try again." : $"Incorrect accusation. {state.AttemptsRemaining} attempt(s) remaining. Review the evidence.");
    }
}
