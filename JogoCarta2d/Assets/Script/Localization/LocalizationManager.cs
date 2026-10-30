using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class LocalizationManager : MonoBehaviour
{
    private const string LanguagePreferenceKey = "game_language";
    private const string ResourceFolder = "Localization/";
    private const int LanguageCount = 3;

    public static LocalizationManager Instance { get; private set; }

    public GameLanguage CurrentLanguage { get; private set; }

    // Evento evita que cada texto precise verificar o idioma em Update().
    public event Action LanguageChanged;

    [Header("Configuração")]
    [SerializeField] private GameLanguage defaultLanguage = GameLanguage.Portuguese;

    private readonly Dictionary<GameLanguage, Dictionary<string, string>> translations =
        new Dictionary<GameLanguage, Dictionary<string, string>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentLanguage = LoadSavedLanguage();

        // O inglês é carregado desde o início para garantir o fallback.
        LoadLanguage(GameLanguage.English);
        LoadLanguage(CurrentLanguage);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetLanguage(GameLanguage language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        LoadLanguage(language);
        CurrentLanguage = language;

        PlayerPrefs.SetInt(LanguagePreferenceKey, (int)language);
        PlayerPrefs.Save();

        // Todos os LocalizedText registrados são atualizados uma única vez.
        LanguageChanged?.Invoke();
    }

    // Sobrecarga útil para UnityEvents, botões e TMP_Dropdown.
    public void SetLanguage(int languageIndex)
    {
        if (!Enum.IsDefined(typeof(GameLanguage), languageIndex))
        {
            Debug.LogWarning($"Índice de idioma inválido: {languageIndex}", this);
            return;
        }

        SetLanguage((GameLanguage)languageIndex);
    }

    public void CycleLanguage()
    {
        int nextLanguage = ((int)CurrentLanguage + 1) % LanguageCount;
        SetLanguage((GameLanguage)nextLanguage);
    }

    public string GetLocalizedText(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        if (translations.TryGetValue(
                CurrentLanguage,
                out Dictionary<string, string> selectedLanguage) &&
            selectedLanguage.TryGetValue(key, out string localizedText))
        {
            return localizedText;
        }

        // Fallback: usa inglês somente quando a chave falta no idioma atual.
        if (CurrentLanguage != GameLanguage.English &&
            translations.TryGetValue(
                GameLanguage.English,
                out Dictionary<string, string> englishLanguage) &&
            englishLanguage.TryGetValue(key, out string englishText))
        {
            return englishText;
        }

        // Facilita localizar chaves ausentes durante o desenvolvimento.
        return $"[{key}]";
    }

    private GameLanguage LoadSavedLanguage()
    {
        int savedValue = PlayerPrefs.GetInt(
            LanguagePreferenceKey,
            (int)defaultLanguage);

        if (!Enum.IsDefined(typeof(GameLanguage), savedValue))
        {
            return defaultLanguage;
        }

        return (GameLanguage)savedValue;
    }

    private void LoadLanguage(GameLanguage language)
    {
        // Cada idioma é carregado no máximo uma vez.
        if (translations.ContainsKey(language))
        {
            return;
        }

        string fileName = GetFileName(language);
        TextAsset jsonAsset = Resources.Load<TextAsset>(ResourceFolder + fileName);

        if (jsonAsset == null)
        {
            Debug.LogError(
                $"Arquivo de localização não encontrado: {ResourceFolder}{fileName}.json",
                this);

            translations[language] = CreateEmptyTable();
            return;
        }

        try
        {
            LocalizationFile file =
                JsonUtility.FromJson<LocalizationFile>(jsonAsset.text);

            Dictionary<string, string> table = CreateEmptyTable();

            if (file?.entries != null)
            {
                foreach (LocalizationEntry entry in file.entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    {
                        continue;
                    }

                    // Em caso de chave duplicada, a última entrada prevalece.
                    table[entry.key] = entry.value ?? string.Empty;
                }
            }

            translations[language] = table;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Falha ao processar o idioma {language}: {exception.Message}",
                this);

            translations[language] = CreateEmptyTable();
        }
        finally
        {
            // O JSON já está convertido em dicionário; o TextAsset pode ser liberado.
            Resources.UnloadAsset(jsonAsset);
        }
    }

    private static Dictionary<string, string> CreateEmptyTable()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static string GetFileName(GameLanguage language)
    {
        switch (language)
        {
            case GameLanguage.Portuguese:
                return "pt";
            case GameLanguage.Spanish:
                return "es";
            case GameLanguage.English:
                return "en";
            default:
                return "en";
        }
    }

    [Serializable]
    private sealed class LocalizationFile
    {
        public LocalizationEntry[] entries;
    }

    [Serializable]
    private sealed class LocalizationEntry
    {
        public string key;
        public string value;
    }
}
