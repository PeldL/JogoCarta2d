using System;
using System.Collections.Generic;
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
    readonly Dictionary<GameLanguage, Dictionary<string,string>> translations = new();
    readonly Dictionary<TMP_Text,string> sceneTexts = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap() { if (Instance == null) new GameObject("[Localization Manager]").AddComponent<LocalizationManager>(); }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        int saved = PlayerPrefs.GetInt(PreferenceKey, (int)defaultLanguage);
        CurrentLanguage = Enum.IsDefined(typeof(GameLanguage), saved) ? (GameLanguage)saved : defaultLanguage;
        foreach (GameLanguage language in Enum.GetValues(typeof(GameLanguage))) LoadLanguage(language);
        SceneManager.sceneLoaded += SceneLoaded;
    }
    void Start() => LocalizeScene();
    void OnDestroy() { SceneManager.sceneLoaded -= SceneLoaded; if (Instance == this) Instance = null; }
    void SceneLoaded(Scene scene, LoadSceneMode mode) => LocalizeScene();
    void LocalizeScene()
    {
        sceneTexts.Clear();
        foreach (TMP_Text label in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;
            sceneTexts[label] = label.text;
            label.text = LocalizeText(label.text);
        }
    }
    void RefreshSceneTexts()
    {
        foreach (var pair in sceneTexts) if (pair.Key != null) pair.Key.text = LocalizeText(pair.Value);
    }
    public void SetLanguage(GameLanguage language)
    {
        if (!Enum.IsDefined(typeof(GameLanguage), language) || CurrentLanguage == language) return;
        CurrentLanguage = language; PlayerPrefs.SetInt(PreferenceKey, (int)language); PlayerPrefs.Save();
        RefreshSceneTexts(); LanguageChanged?.Invoke();
    }
    public void SetLanguage(int index) { if (Enum.IsDefined(typeof(GameLanguage), index)) SetLanguage((GameLanguage)index); }
    public void CycleLanguage() => SetLanguage((GameLanguage)(((int)CurrentLanguage + 1) % 3));
    public string GetLanguageDisplayName() => CurrentLanguage == GameLanguage.Portuguese ? "Português" : CurrentLanguage == GameLanguage.Spanish ? "Español" : "English";
    public string GetLocalizedText(string key) => LocalizeText(key);
    public string LocalizeText(string source)
    {
        if (string.IsNullOrEmpty(source)) return string.Empty;
        return translations.TryGetValue(CurrentLanguage, out var table) && table.TryGetValue(source, out string value) ? value : source;
    }
    void LoadLanguage(GameLanguage language)
    {
        string file = language == GameLanguage.Portuguese ? "pt" : language == GameLanguage.Spanish ? "es" : "en";
        TextAsset asset = Resources.Load<TextAsset>("Localization/" + file);
        var table = new Dictionary<string,string>(StringComparer.Ordinal);
        if (asset != null)
        {
            try {
                var data = JsonUtility.FromJson<LocalizationFile>(asset.text);
                if (data != null && data.entries != null)
                    foreach (var entry in data.entries) if (entry != null && !string.IsNullOrWhiteSpace(entry.key)) table[entry.key] = entry.value ?? entry.key;
            } catch (Exception e) { Debug.LogError("Localization " + file + ": " + e.Message); }
            Resources.UnloadAsset(asset);
        } else Debug.LogError("Localization file missing: " + file);
        translations[language] = table;
    }
    [Serializable] class LocalizationFile { public LocalizationEntry[] entries; }
    [Serializable] class LocalizationEntry { public string key; public string value; }
}