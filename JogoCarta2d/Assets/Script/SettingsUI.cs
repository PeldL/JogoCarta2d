using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu de configurações de áudio/vídeo.
/// Salva automaticamente no settings.json ao alterar qualquer valor.
/// 
/// Setup:
///   - Slider sliderMusica    — volume da música (0 a 1)
///   - Slider sliderSFX       — volume dos efeitos (0 a 1)
///   - Toggle toggleFullscreen — tela cheia
///   - Arraste um AudioMixer se quiser controle mais preciso
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Sliders de áudio")]
    public Slider sliderMusica;
    public Slider sliderSFX;

    [Header("Vídeo")]
    public Toggle toggleFullscreen;

    // Dados carregados
    SettingsData dadosAtuais;

    void Start()
    {
        dadosAtuais = GameSaveSystem.LoadSettings();
        AplicarDadosNaUI();
        AplicarAoJogo();

        // Registra listeners DEPOIS de aplicar para não disparar save desnecessário
        if (sliderMusica    != null) sliderMusica.onValueChanged.AddListener(OnMusicaAlterada);
        if (sliderSFX       != null) sliderSFX.onValueChanged.AddListener(OnSFXAlterado);
        if (toggleFullscreen != null) toggleFullscreen.onValueChanged.AddListener(OnFullscreenAlterado);
    }

    // ── Aplicar dados carregados na UI ────────────────────────────────────────

    void AplicarDadosNaUI()
    {
        if (sliderMusica     != null) sliderMusica.value     = dadosAtuais.musicVolume;
        if (sliderSFX        != null) sliderSFX.value        = dadosAtuais.sfxVolume;
        if (toggleFullscreen != null) toggleFullscreen.isOn  = dadosAtuais.fullscreen;
    }

    // ── Aplicar ao jogo (AudioListener, Screen) ───────────────────────────────

    void AplicarAoJogo()
    {
        // Volume global — substitua por AudioMixer se preferir controle por canal
        AudioListener.volume = dadosAtuais.musicVolume;
        Screen.fullScreen    = dadosAtuais.fullscreen;
    }

    // ── Callbacks dos controles ───────────────────────────────────────────────

    void OnMusicaAlterada(float valor)
    {
        dadosAtuais.musicVolume = valor;
        AudioListener.volume    = valor; // ou AudioMixer.SetFloat("MusicaVol", ...)
        Salvar();
    }

    void OnSFXAlterado(float valor)
    {
        dadosAtuais.sfxVolume = valor;
        // AudioMixer.SetFloat("SFXVol", Mathf.Log10(valor) * 20); // se usar mixer
        Salvar();
    }

    void OnFullscreenAlterado(bool ativo)
    {
        dadosAtuais.fullscreen = ativo;
        Screen.fullScreen      = ativo;
        Salvar();
    }

    // ── Salvar ────────────────────────────────────────────────────────────────

    void Salvar() => GameSaveSystem.SaveSettings(dadosAtuais);
}
