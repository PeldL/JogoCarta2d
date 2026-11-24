using System.Collections.Generic;
using UnityEngine;

// Small synthesized sound effects (no audio files). All of them follow the Sound effects volume.
public sealed class CaseZeroSfx : MonoBehaviour
{
    const int Rate = 22050;
    static CaseZeroSfx instance;
    AudioSource oneShots;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, AudioSource> loops = new Dictionary<string, AudioSource>();
    readonly Dictionary<string, float> loopGains = new Dictionary<string, float>();
    readonly System.Random random = new System.Random();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => instance = null;

    static CaseZeroSfx Instance
    {
        get
        {
            if (instance != null) return instance;
            var root = new GameObject("Caso Zero • Efeitos sonoros");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<CaseZeroSfx>();
            instance.oneShots = root.AddComponent<AudioSource>();
            instance.oneShots.playOnAwake = false;
            instance.oneShots.spatialBlend = 0;
            return instance;
        }
    }

    // ---------- Public sounds ----------

    /// Rising blip with a sparkle: a clue was found.
    public static void Discovery() => Instance.Play("discovery", 0.5f, 0.06f);
    /// Very short tick when the pointer reaches something clickable.
    public static void Hover() => Instance.Play("hover", 0.18f, 0.1f);
    /// Paper being handled or unfolded.
    public static void Paper() => Instance.Play("paper", 0.55f, 0.08f);
    /// Low whoosh for panels and the 3D viewer.
    public static void Whoosh() => Instance.Play("whoosh", 0.35f, 0.05f);
    /// Distant thunder: crack, then a long rolling rumble (dramatic moments such as a confession).
    public static void Thunder() => Instance.Play("thunder", 0.8f, 0.04f);
    /// Quick page turn when the case file opens or closes.
    public static void PageTurn() => Instance.Play("page", 0.4f, 0.1f);
    /// Pencil scratching while an NPC draws.
    public static void SetScribbling(bool on) => Instance.SetLoop("scribble", on, 0.35f);
    /// Fluorescent-light hum and room tone of the interrogation room.
    public static void SetRoomTone(bool on) => Instance.SetLoop("roomtone", on, 0.28f);

    // ---------- Playback ----------

    void Play(string name, float gain, float pitchJitter)
    {
        var clip = Clip(name);
        if (clip == null) return;
        oneShots.pitch = 1 + ((float)random.NextDouble() * 2 - 1) * pitchJitter; // Repeats never sound identical.
        oneShots.PlayOneShot(clip, gain * CaseZeroSettingsPanel.SfxVolume);
    }

    void SetLoop(string name, bool on, float gain)
    {
        if (!loops.TryGetValue(name, out var source) || source == null)
        {
            if (!on) return;
            source = gameObject.AddComponent<AudioSource>();
            source.clip = Clip(name);
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0;
            loops[name] = source;
        }
        loopGains[name] = on ? gain : 0;
        if (on && !source.isPlaying) { source.volume = 0; source.Play(); }
    }

    void Update()
    {
        // Loops fade in and out instead of cutting.
        foreach (var entry in loops)
        {
            var source = entry.Value;
            if (source == null) continue;
            float target = loopGains.TryGetValue(entry.Key, out var gain) ? gain * CaseZeroSettingsPanel.SfxVolume : 0;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * 0.8f);
            if (target <= 0 && source.volume <= 0 && source.isPlaying) source.Stop();
        }
    }

    void OnDestroy()
    {
        foreach (var clip in clips.Values) if (clip != null) Destroy(clip);
        if (instance == this) instance = null;
    }

    // ---------- Synthesis ----------

    AudioClip Clip(string name)
    {
        if (clips.TryGetValue(name, out var cached)) return cached;
        float[] samples;
        var noise = new System.Random(name.GetHashCode());
        switch (name)
        {
            case "discovery":
            {
                samples = new float[(int)(Rate * 0.42f)];
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = i / (float)Rate;
                    // Blip 880 → 1320 Hz, then a soft fifth above that rings out.
                    float f = Mathf.Lerp(880, 1320, Mathf.Clamp01(t / 0.09f));
                    float blip = Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 18);
                    float ring = t > 0.07f ? Mathf.Sin(2 * Mathf.PI * 1980 * t) * Mathf.Exp(-(t - 0.07f) * 9) * 0.35f : 0;
                    samples[i] = (blip + ring) * 0.5f;
                }
                break;
            }
            case "hover":
            {
                samples = new float[(int)(Rate * 0.03f)];
                float filtered = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    filtered = Mathf.Lerp(filtered, (float)noise.NextDouble() * 2 - 1, 0.35f);
                    float t = i / (float)Rate;
                    samples[i] = (filtered * 0.6f + Mathf.Sin(2 * Mathf.PI * 2400 * t) * 0.4f) * Mathf.Exp(-t * 160);
                }
                break;
            }
            case "paper":
            {
                samples = new float[(int)(Rate * 0.32f)];
                float filtered = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    float p = i / (float)(samples.Length - 1);
                    filtered = Mathf.Lerp(filtered, (float)noise.NextDouble() * 2 - 1, 0.5f);
                    // Crinkles: noise gated by a fast, irregular envelope.
                    float crackle = Mathf.PerlinNoise(p * 40, 0.5f) > 0.45f ? 1 : 0.3f;
                    samples[i] = filtered * crackle * Mathf.Sin(Mathf.PI * p) * 0.55f;
                }
                break;
            }
            case "whoosh":
            {
                samples = new float[(int)(Rate * 0.35f)];
                float filtered = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    float p = i / (float)(samples.Length - 1);
                    filtered = Mathf.Lerp(filtered, (float)noise.NextDouble() * 2 - 1, 0.03f + p * 0.1f);
                    samples[i] = filtered * Mathf.Sin(Mathf.PI * p) * 1.6f;
                }
                break;
            }
            case "thunder":
            {
                samples = new float[(int)(Rate * 3.2f)];
                float brown = 0, crackle = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = i / (float)Rate;
                    float white = (float)noise.NextDouble() * 2 - 1;
                    brown = Mathf.Clamp(brown + white * 0.04f, -1, 1) * 0.998f; // Deep rumble.
                    crackle = Mathf.Lerp(crackle, white, 0.35f);                  // Bright initial crack.
                    float crack = t < 0.25f ? crackle * Mathf.Exp(-t * 14) * 0.9f : 0;
                    // Rumble swells in, rolls (slow beating) and fades over ~3 s.
                    float swell = Mathf.Clamp01(t / 0.12f) * Mathf.Exp(-t * 1.1f);
                    float roll = 0.6f + 0.4f * Mathf.Sin(t * 7.3f + Mathf.Sin(t * 2.1f) * 2);
                    samples[i] = crack + brown * swell * roll * 3.2f;
                }
                break;
            }
            case "page":
            {
                samples = new float[(int)(Rate * 0.2f)];
                float filtered = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    float p = i / (float)(samples.Length - 1);
                    filtered = Mathf.Lerp(filtered, (float)noise.NextDouble() * 2 - 1, 0.15f + p * 0.5f); // Swish brightens as the page lands.
                    samples[i] = filtered * Mathf.Pow(Mathf.Max(0, Mathf.Sin(Mathf.PI * p)), 1.5f) * 1.1f;
                }
                break;
            }
            case "scribble":
            {
                samples = new float[Rate * 2];
                float filtered = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = i / (float)Rate;
                    filtered = Mathf.Lerp(filtered, (float)noise.NextDouble() * 2 - 1, 0.6f);
                    // Strokes: bursts at an uneven rhythm, like a hand moving a pencil.
                    float stroke = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 9.5f + Mathf.Sin(t * 3.1f) * 2)), 3);
                    samples[i] = filtered * stroke * 0.5f;
                }
                break;
            }
            case "roomtone":
            {
                samples = new float[Rate * 4];
                float brown = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = i / (float)Rate;
                    brown = Mathf.Clamp(brown + ((float)noise.NextDouble() * 2 - 1) * 0.02f, -1, 1);
                    // 60 Hz mains hum with harmonics (fluorescent tube) over a soft brown-noise room tone.
                    float hum = Mathf.Sin(2 * Mathf.PI * 60 * t) * 0.18f + Mathf.Sin(2 * Mathf.PI * 120 * t) * 0.12f + Mathf.Sin(2 * Mathf.PI * 180 * t) * 0.04f;
                    samples[i] = hum * (0.85f + Mathf.Sin(2 * Mathf.PI * 0.5f * t) * 0.15f) + brown * 0.35f;
                }
                // Seamless loop: crossfade the tail into the head.
                int fade = Rate / 4;
                for (int i = 0; i < fade; i++)
                {
                    float a = i / (float)fade;
                    samples[i] = samples[i] * a + samples[samples.Length - fade + i] * (1 - a);
                }
                System.Array.Resize(ref samples, samples.Length - fade);
                var loop = AudioClip.Create("Caso Zero • roomtone", samples.Length, 1, Rate, false);
                loop.SetData(samples, 0);
                clips[name] = loop;
                return loop;
            }
            default: return null;
        }
        ApplyEdges(samples, name == "scribble");
        var clip = AudioClip.Create("Caso Zero • " + name, samples.Length, 1, Rate, false);
        clip.SetData(samples, 0);
        clips[name] = clip;
        return clip;
    }

    // 5 ms fades on both ends so clips never click (loops keep their ends continuous).
    static void ApplyEdges(float[] samples, bool loop)
    {
        // Never NaN, never above full scale: normalise anything louder than -1 dBFS.
        float peak = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            if (float.IsNaN(samples[i]) || float.IsInfinity(samples[i])) samples[i] = 0;
            peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
        }
        if (peak > 0.89f) for (int i = 0; i < samples.Length; i++) samples[i] *= 0.89f / peak;
        if (loop) return;
        int edge = Mathf.Min(samples.Length / 2, Rate / 200);
        for (int i = 0; i < edge; i++)
        {
            float a = i / (float)edge;
            samples[i] *= a;
            samples[samples.Length - 1 - i] *= a;
        }
    }
}
