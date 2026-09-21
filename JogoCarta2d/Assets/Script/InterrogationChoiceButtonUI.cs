using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InterrogationChoiceButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI choiceLabel;
    [SerializeField] private Button button;

    public void Setup(InterrogationChoice choice, InterrogationUI ui, bool available)
    {
        string label = choice.choiceText;

        if (!available && choice.requiredEvidence != null)
            label += $" (precisa: {choice.requiredEvidence.evidenceName})";

        if (choiceLabel != null)
            choiceLabel.text = label;

        button.interactable = available;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ui.OnChoiceSelected(choice));
    }
}