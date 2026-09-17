using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhoneChoiceButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI choiceLabel;
    [SerializeField] private Button button;

    public void Setup(PhoneDialogueChoice choice, PhoneScreenUI screen)
    {
        if (choiceLabel != null)
            choiceLabel.text = choice.choiceText;

        if (button == null)
            button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("[PhoneChoiceButtonUI] Nenhum Button atribuído no prefab.");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => screen.OnChoiceSelected(choice));
    }
}
