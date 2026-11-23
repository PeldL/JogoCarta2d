// Names of the case-file pages and the map, as they are written in Game.unity
// (CaseZeroView.pages) and in save files. Use these constants instead of typing
// the strings: a typo in a page name breaks navigation without any error.
public static class CaseZeroPages
{
    public const string Map = "Mapa";
    public const string Evidence = "Evidências";
    public const string Interviews = "Interrogatório";
    public const string Phone = "Ligações";
    public const string Deduction = "Dedução";
    public const string Options = "Opções";
    public const string Saves = "Saves";
    public const string Suspects = "Suspeitos";
    public const string Introduction = "Introdução";
    public const string Confirmation = "Confirmar reinício";
    public const string Result = "Resultado";

    // Older saves may still contain these names.
    public const string LegacyPhone = "Telefone";
    public const string LegacyScene = "Local";
}
