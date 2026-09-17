using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class PhoneScreenUI : MonoBehaviour
{
    [Header("Painel raiz da tela do celular")]
    [SerializeField] private GameObject phoneScreenRoot;

    [Header("Conteúdo")]
    [SerializeField] private TextMeshProUGUI callerNameText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    private readonly List<GameObject> spawnedButtons = new List<GameObject>();
    private bool isSubscribed = false;

    private void Start()
    {
        TrySubscribe();

        if (phoneScreenRoot != null)
            phoneScreenRoot.SetActive(false);
    }

    private void OnEnable() => TrySubscribe();

    private void TrySubscribe()
    {
        if (isSubscribed || PhoneCallManager.Instance == null)
            return;

        PhoneCallManager.Instance.OnCallScreenUpdated += HandleScreenUpdated;
        PhoneCallManager.Instance.OnCallEnded += HandleCallEnded;
        isSubscribed = true;
    }

    private void OnDisable()
    {
        if (isSubscribed && PhoneCallManager.Instance != null)
        {
            PhoneCallManager.Instance.OnCallScreenUpdated -= HandleScreenUpdated;
            PhoneCallManager.Instance.OnCallEnded -= HandleCallEnded;
            isSubscribed = false;
        }
    }

    private void HandleScreenUpdated(PhoneCallAsset call, PhoneDialogueNode node)
    {
        if (phoneScreenRoot != null)
            phoneScreenRoot.SetActive(true);

        if (callerNameText != null)
            callerNameText.text = call.callerName;

        if (messageText != null)
            messageText.text = node.message;

        RebuildChoices(node.choices);
    }

    private void RebuildChoices(List<PhoneDialogueChoice> choices)
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        spawnedButtons.Clear();

        if (choicesContainer == null)
        {
            Debug.LogError("[PhoneScreenUI] choicesContainer não atribuído.");
            return;
        }

        if (choiceButtonPrefab == null)
        {
            Debug.LogError("[PhoneScreenUI] choiceButtonPrefab não atribuído.");
            return;
        }

        foreach (var choice in choices)
        {
            GameObject instance = Instantiate(choiceButtonPrefab, choicesContainer);

            PhoneChoiceButtonUI btnScript = instance.GetComponent<PhoneChoiceButtonUI>();
            if (btnScript == null)
            {
                Debug.LogError($"[PhoneScreenUI] O prefab '{choiceButtonPrefab.name}' não tem o componente PhoneChoiceButtonUI anexado.");
                Destroy(instance);
                continue;
            }

            btnScript.Setup(choice, this);
            spawnedButtons.Add(instance);
        }
    }

    public void OnChoiceSelected(PhoneDialogueChoice choice)
    {
        if (PhoneCallManager.Instance != null)
            PhoneCallManager.Instance.SelectChoice(choice);
    }

    private void HandleCallEnded()
    {
        if (phoneScreenRoot != null)
            phoneScreenRoot.SetActive(false);
    }
}