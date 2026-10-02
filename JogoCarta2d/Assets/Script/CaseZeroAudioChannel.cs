using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class CaseZeroAudioChannel : MonoBehaviour
{
    public enum Channel { Effects, Music }
    public Channel channel;
    [Range(0, 1)] public float baseVolume = 1;
    AudioSource source;
    void Awake() { source = GetComponent<AudioSource>(); }
    void OnEnable() { CaseZeroSettingsPanel.VolumesChanged += Apply; Apply(); }
    void OnDisable() { CaseZeroSettingsPanel.VolumesChanged -= Apply; }
    void Apply() { source.volume = baseVolume * (channel == Channel.Music ? CaseZeroSettingsPanel.MusicVolume : CaseZeroSettingsPanel.SfxVolume); }
}
