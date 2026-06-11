using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Menu de opções de áudio — integrado ao GameSaveSystem.
/// Salva junto com o settings.json do sistema de save, sem arquivo separado.
/// </summary>
public class OptionsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider sfxSlider;
    public Slider musicSlider;

    void Start()
    {
        // Carrega do settings.json (mesmo arquivo do sistema de save)
        SettingsData settings = GameSaveSystem.LoadSettings();

        if (sfxSlider != null)
        {
            sfxSlider.value = settings.sfxVolume;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            SetSFXVolume(settings.sfxVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.value = settings.musicVolume;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
            SetMusicVolume(settings.musicVolume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("SFX", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
        Salvar();
    }

    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MUSICS", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
        Salvar();
    }

    void Salvar()
    {
        var settings = new SettingsData
        {
            sfxVolume   = sfxSlider   != null ? sfxSlider.value   : 0.75f,
            musicVolume = musicSlider != null ? musicSlider.value : 0.75f
        };

        GameSaveSystem.SaveSettings(settings);
    }
}
