using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Responsável por ler e escrever os arquivos JSON no disco.
/// Não tem MonoBehaviour — é chamado por outros sistemas.
///
/// Arquivos gerados:
///   save_slot_0.json  /  save_slot_1.json  /  save_slot_2.json
///   settings.json
/// </summary>
public static class GameSaveSystem
{
    // ── Caminhos ──────────────────────────────────────────────────────────────

    static string SlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");

    static string SettingsPath =>
        Path.Combine(Application.persistentDataPath, "settings.json");

    // ── Progresso ─────────────────────────────────────────────────────────────

    /// <summary>Salva o progresso num slot. Escrita assíncrona para não travar o jogo.</summary>
    public static async void SaveGame(SaveData data, int slot)
    {
        data.ultimoSalvamento = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        string json = JsonUtility.ToJson(data, true);
        string path = SlotPath(slot);

        await Task.Run(() => File.WriteAllText(path, json));
        Debug.Log($"[Save] Slot {slot} salvo em {path}");
    }

    /// <summary>Carrega um slot. Retorna null se o slot estiver vazio.</summary>
    public static SaveData LoadGame(int slot)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }

    /// <summary>Verifica se um slot possui save gravado.</summary>
    public static bool SlotExists(int slot) => File.Exists(SlotPath(slot));

    /// <summary>Deleta um slot de save.</summary>
    public static void DeleteSlot(int slot)
    {
        string path = SlotPath(slot);
        if (File.Exists(path)) File.Delete(path);
        Debug.Log($"[Save] Slot {slot} deletado.");
    }

    /// <summary>
    /// Retorna um SaveData com apenas os metadados (nome, data, progresso)
    /// para exibir na tela de seleção de slots sem carregar tudo.
    /// </summary>
    public static SaveData PeekSlot(int slot) => LoadGame(slot);

    // ── Configurações ─────────────────────────────────────────────────────────

    /// <summary>Salva as configurações globais.</summary>
    public static void SaveSettings(SettingsData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SettingsPath, json);
        Debug.Log("[Save] Settings salvas.");
    }

    /// <summary>Carrega as configurações. Retorna valores padrão se não existir.</summary>
    public static SettingsData LoadSettings()
    {
        if (!File.Exists(SettingsPath)) return new SettingsData();

        string json = File.ReadAllText(SettingsPath);
        return JsonUtility.FromJson<SettingsData>(json);
    }
}
