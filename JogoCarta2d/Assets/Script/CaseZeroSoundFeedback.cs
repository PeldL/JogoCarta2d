using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Plays imported recordings through a scene-authored AudioSource.
public sealed class CaseZeroSoundFeedback : MonoBehaviour
{
    public static CaseZeroSoundFeedback Current { get; private set; }
    public AudioSource source;
    public AudioClip click, evidence, notification;
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
    void Play(AudioClip clip, float gain)
    {
        if (source != null && clip != null) source.PlayOneShot(clip, gain);
    }
    void OnDestroy()
    {
        foreach (var button in buttons) if (button != null) button.onClick.RemoveListener(PlayClick);
        if (Current == this) Current = null;
    }
}
