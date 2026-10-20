using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Plays imported recordings through a scene-authored AudioSource.
public sealed class CaseZeroSoundFeedback : MonoBehaviour
{
    public static CaseZeroSoundFeedback Current { get; private set; }
    public AudioSource source;
    public AudioClip click, evidence, notification;
    AudioClip transition;
    readonly List<Button> buttons = new List<Button>();

    void Awake() { Current = this; }
    void Start()
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                buttons.Add(button);
                button.onClick.AddListener(PlayClick);
            }
    }
    public void PlayClick() => Play(click, 0.45f);
    public void PlayEvidence() => Play(evidence, 0.65f);
    public void PlayNotification() => Play(notification, 0.75f);
    public void PlayRadioTick() => Play(click, 0.12f);
    public void PlayTransmission() => Play(notification, 0.4f);
    public void PlayTransition()
    {
        if (source == null) return;
        if (transition == null)
        {
            // A short filtered air sweep, generated once without an external asset.
            const int rate = 22050;
            var samples = new float[(int)(rate * 0.24f)];
            var random = new System.Random(73);
            float filtered = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float phase = i / (float)(samples.Length - 1);
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2 - 1, 0.06f + phase * 0.12f);
                samples[i] = filtered * Mathf.Sin(Mathf.PI * phase) * 0.65f;
            }
            transition = AudioClip.Create("File sweep", samples.Length, 1, rate, false);
            transition.SetData(samples, 0);
        }
        Play(transition, 0.35f);
    }
    void Play(AudioClip clip, float gain)
    {
        if (source != null && clip != null) source.PlayOneShot(clip, gain);
    }
    void OnDestroy()
    {
        foreach (var button in buttons) if (button != null) button.onClick.RemoveListener(PlayClick);
        if (Current == this) Current = null;
        if (transition != null) Destroy(transition);
    }
}
