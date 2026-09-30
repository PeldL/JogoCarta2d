using TMPro;
using UnityEngine;

public sealed class CaseZeroSettingsPanel : MonoBehaviour
{
    public TMP_Text fullscreenLabel, musicLabel, sfxLabel;
    public static event System.Action VolumesChanged;
    public static float MusicVolume => PlayerPrefs.GetFloat("CaseZero.Music", 0.8f);
    public static float SfxVolume => PlayerPrefs.GetFloat("CaseZero.Sfx", 1f);
    void OnEnable() { Refresh(); }
    void Refresh()
    {
        fullscreenLabel.text = Screen.fullScreen ? "Tela cheia: ativada" : "Tela cheia: desativada";
        musicLabel.text = $"Música: {Mathf.RoundToInt(MusicVolume * 100)}%";
        sfxLabel.text = $"Efeitos: {Mathf.RoundToInt(SfxVolume * 100)}%";
    }
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
