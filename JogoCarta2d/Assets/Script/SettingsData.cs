/// <summary>
/// Configurações globais do jogo (áudio, vídeo, idioma).
/// Salvo num arquivo separado — independente dos slots.
/// </summary>
[System.Serializable]
public class SettingsData
{
    public float musicVolume = 1f;
    public float sfxVolume   = 1f;
    public bool  fullscreen  = true;
    public int   resolutionIndex = 0;
    public string language   = "pt-BR";
}
