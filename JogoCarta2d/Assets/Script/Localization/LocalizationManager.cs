using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class LocalizationManager : MonoBehaviour
{
    const string PreferenceKey = "game_language";
    public static LocalizationManager Instance { get; private set; }
    public GameLanguage CurrentLanguage { get; private set; }
    public event Action LanguageChanged;
    [SerializeField] GameLanguage defaultLanguage = GameLanguage.Portuguese;
    readonly Dictionary<GameLanguage, Dictionary<string, string>> translations = new();
    readonly Dictionary<TMP_Text, string> sceneTexts = new();
    readonly Dictionary<TMP_Text, string> appliedTexts = new();
    float scanTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap() { if (Instance == null) new GameObject("[Localization Manager]").AddComponent<LocalizationManager>(); }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        int saved = PlayerPrefs.GetInt(PreferenceKey, (int)defaultLanguage);
        CurrentLanguage = Enum.IsDefined(typeof(GameLanguage), saved) ? (GameLanguage)saved : defaultLanguage;
        foreach (GameLanguage language in Enum.GetValues(typeof(GameLanguage))) LoadLanguage(language);
        SceneManager.sceneLoaded += SceneLoaded;
    }

    void Start() => LocalizeScene();
    void OnDestroy() { SceneManager.sceneLoaded -= SceneLoaded; if (Instance == this) Instance = null; }
    void SceneLoaded(Scene scene, LoadSceneMode mode) { sceneTexts.Clear(); appliedTexts.Clear(); LocalizeScene(); }

    void Update()
    {
        scanTimer += Time.unscaledDeltaTime;
        if (scanTimer < 0.2f) return;
        scanTimer = 0;
        ScanDynamicTexts();
    }

    void LocalizeScene()
    {
        foreach (TMP_Text label in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            RegisterText(label);
    }

    void ScanDynamicTexts()
    {
        foreach (TMP_Text label in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (label == null || label.GetComponent<LocalizedText>() != null) continue;
            if (!sceneTexts.TryGetValue(label, out string source))
            {
                RegisterText(label);
                continue;
            }
            if (appliedTexts.TryGetValue(label, out string last) && label.text != last)
            {
                sceneTexts[label] = label.text;
                source = label.text;
                label.text = LocalizeText(source);
                appliedTexts[label] = label.text;
            }
        }
        var dead = new List<TMP_Text>();
        foreach (var pair in sceneTexts) if (pair.Key == null) dead.Add(pair.Key);
        foreach (var label in dead) { sceneTexts.Remove(label); appliedTexts.Remove(label); }
    }

    void RegisterText(TMP_Text label)
    {
        if (label == null || label.GetComponent<LocalizedText>() != null || string.IsNullOrWhiteSpace(label.text)) return;
        if (!sceneTexts.ContainsKey(label)) sceneTexts[label] = label.text;
        string translated = LocalizeText(sceneTexts[label]);
        label.text = translated;
        appliedTexts[label] = translated;
    }

    void RefreshSceneTexts()
    {
        var dead = new List<TMP_Text>();
        foreach (var pair in sceneTexts)
        {
            if (pair.Key == null) { dead.Add(pair.Key); continue; }
            string translated = LocalizeText(pair.Value);
            pair.Key.text = translated;
            appliedTexts[pair.Key] = translated;
        }
        foreach (var label in dead) { sceneTexts.Remove(label); appliedTexts.Remove(label); }
        ScanDynamicTexts();
    }

    public void SetLanguage(GameLanguage language)
    {
        if (!Enum.IsDefined(typeof(GameLanguage), language) || CurrentLanguage == language) return;
        CurrentLanguage = language;
        PlayerPrefs.SetInt(PreferenceKey, (int)language);
        PlayerPrefs.Save();
        RefreshSceneTexts();
        LanguageChanged?.Invoke();
    }
    public void SetLanguage(int index) { if (Enum.IsDefined(typeof(GameLanguage), index)) SetLanguage((GameLanguage)index); }
    public void CycleLanguage() => SetLanguage((GameLanguage)(((int)CurrentLanguage + 1) % 3));
    public string GetLanguageDisplayName() => CurrentLanguage == GameLanguage.Portuguese ? "Português" : CurrentLanguage == GameLanguage.Spanish ? "Español" : "English";
    public string GetLocalizedText(string key) => LocalizeText(key);

    public string LocalizeText(string source)
    {
        if (string.IsNullOrEmpty(source)) return string.Empty;
        if (!translations.TryGetValue(CurrentLanguage, out var table)) return source;
        if (table.TryGetValue(source, out string exact)) return exact;
        // Also translate phrases embedded in generated labels (case numbers, counters, notifications).
        var keys = new List<string>(table.Keys);
        keys.Sort((a, b) => b.Length.CompareTo(a.Length));
        string result = source;
        foreach (string key in keys)
        {
            if (key.Length < 4 || string.Equals(table[key], key, StringComparison.Ordinal)) continue;
            string pattern = @"(?<![\\p{L}\\p{N}])" + Regex.Escape(key) + @"(?![\\p{L}\\p{N}])";
            result = Regex.Replace(result, pattern, Match => table[key]);
        }
        return result;
    }

    void LoadLanguage(GameLanguage language)
    {
        string file = language == GameLanguage.Portuguese ? "pt" : language == GameLanguage.Spanish ? "es" : "en";
        TextAsset asset = Resources.Load<TextAsset>("Localization/" + file);
        var table = new Dictionary<string, string>(StringComparer.Ordinal);
        if (asset != null)
        {
            try
            {
                var data = JsonUtility.FromJson<LocalizationFile>(asset.text);
                if (data != null && data.entries != null)
                    foreach (var entry in data.entries)
                        if (entry != null && !string.IsNullOrWhiteSpace(entry.key)) table[entry.key] = entry.value ?? entry.key;
            }
            catch (Exception e) { Debug.LogError("Localization " + file + ": " + e.Message); }
            Resources.UnloadAsset(asset);
        }
        else Debug.LogError("Localization file missing: " + file);
        translations[language] = table;
    }

    [Serializable] class LocalizationFile { public LocalizationEntry[] entries; }
    [Serializable] class LocalizationEntry { public string key; public string value; }
}