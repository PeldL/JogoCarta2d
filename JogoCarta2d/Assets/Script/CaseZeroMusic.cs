using UnityEngine;

// One continuous music bed across menu, locations and additive presentation scenes.
public sealed class CaseZeroMusic : MonoBehaviour
{
    static CaseZeroMusic current;
    AudioSource source;
    float fade;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => current = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        if (current != null) return;
        var clip = Resources.Load<AudioClip>("Audio/UnsolvedInvestigation");
        if (clip == null) return;
        var root = new GameObject("Case Zero - Background Music");
        DontDestroyOnLoad(root);
        current = root.AddComponent<CaseZeroMusic>();
        current.source = root.AddComponent<AudioSource>();
        current.source.playOnAwake = false;
        current.source.loop = true;
        current.source.spatialBlend = 0;
        current.source.volume = 0;
        current.source.clip = clip;
        current.source.Play();
    }

    void Update()
    {
        if (source == null) return;
        fade = Mathf.MoveTowards(fade, 1, Time.unscaledDeltaTime / 2f);
        source.volume = 0.25f * fade * CaseZeroSettingsPanel.MusicVolume;
    }

    void OnDestroy() { if (current == this) current = null; }
}
