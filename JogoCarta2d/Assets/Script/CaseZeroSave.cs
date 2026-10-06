using System;
using System.IO;
using UnityEngine;

public static class CaseZeroSave
{
#if UNITY_EDITOR
    // Editor validation uses isolated temporary files, never the player's slots.
    public static string ValidationDirectory
    {
        get => UnityEditor.SessionState.GetString("CaseZero.ValidationDirectory", "");
        set => UnityEditor.SessionState.SetString("CaseZero.ValidationDirectory", value ?? "");
    }
#endif
    public const int SlotCount = 3;
    public static string DirectoryPath
    {
        get
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(ValidationDirectory)) return ValidationDirectory;
            // Preserve existing editor tests, but never import them into a player.
            return Application.persistentDataPath;
#else
            return Path.Combine(Application.persistentDataPath, "PlayerSaves");
#endif
        }
    }
    public static string PathName => SlotPath(1);
    static string LegacyPath => Path.Combine(DirectoryPath, "caso-zero-v1.json");
    public static bool Exists => ExistsInSlot(1);

    public static string SlotPath(int slot)
    {
        if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(DirectoryPath, $"caso-zero-slot-{slot}.json");
    }

    public static bool ExistsInSlot(int slot)
    {
        string path = SlotPath(slot);
        return File.Exists(path) || File.Exists(path + ".bak")
            || (slot == 1 && (File.Exists(LegacyPath) || File.Exists(LegacyPath + ".bak")));
    }

    public static bool Write(CaseZeroState state, out string error, int slot = 1)
    {
        error = null;
        try
        {
            if (state == null || !state.IsValid()) throw new InvalidDataException("Estado de investigação inválido.");
            string path = SlotPath(slot);
            Directory.CreateDirectory(DirectoryPath);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(state, true));
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
            return true;
        }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException)
        {
            error = "Não foi possível salvar: " + ex.Message;
            return false;
        }
    }

    public static bool Delete(int slot, out string error)
    {
        error = null;
        try
        {
            string path = SlotPath(slot);
            // Remove every recovery source before the primary, including pre-slot saves.
            // Deletion uses exactly the same environment directory as read/write.
            string[] paths = slot == 1
                ? new[] { LegacyPath + ".tmp", LegacyPath + ".bak", LegacyPath, path + ".tmp", path + ".bak", path }
                : new[] { path + ".tmp", path + ".bak", path };
            foreach (string file in paths) File.Delete(file);
            foreach (string file in paths)
                if (File.Exists(file)) throw new IOException("Um arquivo do slot ainda existe.");
            return true;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
        {
            error = "Não foi possível apagar completamente o slot: " + ex.Message;
            return false;
        }
    }

    public static CaseZeroState Read(out string error, int slot = 1)
    {
        error = null;
        string source;
        try { source = SlotPath(slot); }
        catch (ArgumentOutOfRangeException) { error = "Slot inválido."; return null; }
        // An old single-slot save is exposed as slot 1 until the first successful write.
        // Never fall back to it over a newer (possibly damaged) slot file.
        if (slot == 1 && !File.Exists(source) && !File.Exists(source + ".bak")) source = LegacyPath;
        foreach (string path in new[] { source, source + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                var state = JsonUtility.FromJson<CaseZeroState>(File.ReadAllText(path));
                if (state != null && state.IsValid())
                {
                    error = path.EndsWith(".bak") ? "Save recuperado pela cópia de segurança." : null;
                    return state;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) { }
        }
        error = "Não foi encontrado um save válido. O arquivo existente foi preservado.";
        return null;
    }
}
