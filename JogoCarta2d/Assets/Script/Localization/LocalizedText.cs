using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;

    private TMP_Text label;
    private LocalizationManager localizationManager;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        localizationManager = LocalizationManager.Instance;

        if (localizationManager == null)
        {
            Debug.LogWarning(
                "LocalizationManager não encontrado. " +
                "Adicione-o à primeira cena do jogo.",
                this);
            return;
        }

        localizationManager.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        // O cancelamento da inscrição evita referências a objetos destruídos.
        if (localizationManager != null)
        {
            localizationManager.LanguageChanged -= Refresh;
            localizationManager = null;
        }
    }

    public void SetKey(string newKey)
    {
        if (key == newKey)
        {
            return;
        }

        key = newKey;
        Refresh();
    }

    private void Refresh()
    {
        if (localizationManager == null ||
            label == null ||
            string.IsNullOrEmpty(key))
        {
            return;
        }

        string localizedText = localizationManager.GetLocalizedText(key);

        // Evita atualização desnecessária da malha do TextMeshPro.
        if (label.text != localizedText)
        {
            label.text = localizedText;
        }
    }
}
