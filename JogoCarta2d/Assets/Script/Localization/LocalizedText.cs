using TMPro;
using UnityEngine;
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedText : MonoBehaviour
{
    [SerializeField] string key;
    TMP_Text label;
    LocalizationManager manager;
    void Awake() => label = GetComponent<TMP_Text>();
    void OnEnable() { Attach(); Refresh(); }
    void OnDisable() { if (manager != null) manager.LanguageChanged -= Refresh; manager = null; }
    void Attach() {
        if (manager == LocalizationManager.Instance) return;
        if (manager != null) manager.LanguageChanged -= Refresh;
        manager = LocalizationManager.Instance;
        if (manager != null) manager.LanguageChanged += Refresh;
    }
    public void SetKey(string value) { key = value; Attach(); Refresh(); }
    void Refresh() { if (label == null || string.IsNullOrEmpty(key)) return; Attach(); if (manager != null) label.text = manager.LocalizeText(key); }
}