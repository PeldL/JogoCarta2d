// A one-use request across Menu -> UI. Progress remains in the JSON slot.
public static class CaseZeroSession
{
    public static bool ShowCredits;
    static int requestedSlot;
    static bool requestedNewGame;
    public static void Request(int slot, bool newGame)
    {
        requestedSlot = slot;
        requestedNewGame = newGame;
    }
    public static bool Consume(out int slot, out bool newGame)
    {
        slot = requestedSlot;
        newGame = requestedNewGame;
        requestedSlot = 0;
        return slot >= 1 && slot <= CaseZeroSave.SlotCount;
    }
}
