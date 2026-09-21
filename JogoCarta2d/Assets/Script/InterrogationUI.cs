using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class InterrogationUI : MonoBehaviour
{
    [Header("Painel raiz")]
    [SerializeField] private GameObject interrogationRoot;

    [Header("Conteúdo")]
    [SerializeField] private TextMeshProUGUI suspectNameText;
    [SerializeField] private Image suspectPortraitImage;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    private readonly List<GameObject> spawnedButtons = new List<GameObject>();
    private bool isSubscribed = false;

    private void Start()
    {
        TrySubscribe();

        if (interrogationRoot != null)
            interrogationRoot.SetActive(false);
    }

    private void OnEnable() => TrySubscribe();

    private void TrySubscribe()
    {
        if (isSubscribed || InterrogationManager.Instance == null)
            return;

        InterrogationManager.Instance.OnInterrogationUpdated += HandleUpdated;
        InterrogationManager.Instance.OnInterrogationEnded += HandleEnded;
        isSubscribed = true;
    }

    private void OnDisable()
    {
        if (isSubscribed && InterrogationManager.Instance != null)
        {
            InterrogationManager.Instance.OnInterrogationUpdated -= HandleUpdated;
            InterrogationManager.Instance.OnInterrogationEnded -= HandleEnded;
            isSubscribed = false;
        }
    }

    private void HandleUpdated(SuspectAsset suspect, InterrogationNode node)
    {
        if (interrogationRoot != null)
            interrogationRoot.SetActive(true);

        if (suspectNameText != null)
            suspectNameText.text = suspect.suspectName;

        if (suspectPortraitImage != null && suspect.portrait != null)
            suspectPortraitImage.sprite = suspect.portrait;

        if (messageText != null)
            messageText.text = node.message;

        RebuildChoices(node.choices);
    }

    private void RebuildChoices(List<InterrogationChoice> choices)
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        spawnedButtons.Clear();

        if (choicesContainer == null || choiceButtonPrefab == null)
        {
            Debug.LogError("[InterrogationUI] choicesContainer ou choiceButtonPrefab não atribuídos.");
            return;
        }

        foreach (var choice in choices)
        {
            bool available = InterrogationManager.Instance.IsChoiceAvailable(choice);

            GameObject instance = Instantiate(choiceButtonPrefab, choicesContainer);
            InterrogationChoiceButtonUI btnScript = instance.GetComponent<InterrogationChoiceButtonUI>();

            if (btnScript == null)
            {
                Debug.LogError($"[InterrogationUI] O prefab '{choiceButtonPrefab.name}' não tem InterrogationChoiceButtonUI anexado.");
                Destroy(instance);
                continue;
            }

            btnScript.Setup(choice, this, available);
            spawnedButtons.Add(instance);
        }
    }

    public void OnChoiceSelected(InterrogationChoice choice)
    {
        InterrogationManager.Instance.SelectChoice(choice);
    }

    private void HandleEnded()
    {
        if (interrogationRoot != null)
            interrogationRoot.SetActive(false);
    }
}