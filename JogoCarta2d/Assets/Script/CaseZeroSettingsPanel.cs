using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CaseZeroSettingsPanel : MonoBehaviour
{
    public TMP_Text fullscreenLabel, musicLabel, sfxLabel;
    TMP_Text languageLabel;
    Button languageButton;
    public static event System.Action VolumesChanged;
    public static float MusicVolume => PlayerPrefs.GetFloat("CaseZero.Music", 0.8f);
    public static float SfxVolume => PlayerPrefs.GetFloat("CaseZero.Sfx", 1f);

    void OnEnable()
    {
        EnsureLanguageControl();
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.LanguageChanged += Refresh;
        Refresh();
    }
    void OnDisable()
    {
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.LanguageChanged -= Refresh;
    }

    void EnsureLanguageControl()
    {
        if (languageButton != null || sfxLabel == null) return;
        Transform parent = sfxLabel.transform.parent;
        var root = new GameObject("Language Selector", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        var sourceRect = sfxLabel.rectTransform;
        rect.anchorMin = sourceRect.anchorMin; rect.anchorMax = sourceRect.anchorMax;
        rect.pivot = sourceRect.pivot; rect.sizeDelta = sourceRect.sizeDelta;
        rect.anchoredPosition = sourceRect.anchoredPosition + Vector2.down * 62f;
        var image = root.GetComponent<Image>();
        image.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
        languageButton = root.GetComponent<Button>();
        languageButton.targetGraphic = image;
        var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 2); textRect.offsetMax = new Vector2(-8, -2);
        languageLabel = textObject.GetComponent<TextMeshProUGUI>();
        languageLabel.font = sfxLabel.font;
        languageLabel.fontSize = sfxLabel.fontSize;
        languageLabel.color = sfxLabel.color;
        languageLabel.alignment = TextAlignmentOptions.Midline;
        languageButton.onClick.AddListener(CycleLanguage);
    }

    void Refresh()
    {
        if (fullscreenLabel != null) fullscreenLabel.text = Localize(Screen.fullScreen ? "Fullscreen: on" : "Fullscreen: off");
        if (musicLabel != null) musicLabel.text = $"{Localize("Music")}: {Mathf.RoundToInt(MusicVolume * 100)}%";
        if (sfxLabel != null) sfxLabel.text = $"{Localize("Sound effects")}: {Mathf.RoundToInt(SfxVolume * 100)}%";
        if (languageLabel != null) languageLabel.text = $"{Localize("Language")}: {LocalizationManager.Instance?.GetLanguageDisplayName() ?? "Português"}  ▾";
    }
    static string Localize(string value) => LocalizationManager.Instance != null ? LocalizationManager.Instance.LocalizeText(value) : value;
    void CycleLanguage() { if (LocalizationManager.Instance != null) LocalizationManager.Instance.CycleLanguage(); Refresh(); }

    public void ToggleFullscreen()
    {
        bool enabled = !Screen.fullScreen;
        Screen.fullScreen = enabled;
        PlayerPrefs.SetInt("CaseZero.Fullscreen", enabled ? 1 : 0);
        PlayerPrefs.Save();
        Invoke(nameof(Refresh), 0.15f);
    }
    public void MusicUp() => SetVolume("CaseZero.Music", MusicVolume + 0.1f);
    public void MusicDown() => SetVolume("CaseZero.Music", MusicVolume - 0.1f);
    public void SfxUp() => SetVolume("CaseZero.Sfx", SfxVolume + 0.1f);
    public void SfxDown() => SetVolume("CaseZero.Sfx", SfxVolume - 0.1f);
    void SetVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save(); VolumesChanged?.Invoke(); Refresh();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyDisplayPreference()
    {
        VolumesChanged = null;
        if (PlayerPrefs.HasKey("CaseZero.Fullscreen")) Screen.fullScreen = PlayerPrefs.GetInt("CaseZero.Fullscreen") != 0;
    }
}