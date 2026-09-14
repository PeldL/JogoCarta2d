using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class EvidenceBoardUI : MonoBehaviour
{
    [Header("Estrutura do painel")]
    [SerializeField] private GameObject boardPanelRoot; // o painel inteiro (pra abrir/fechar)
    [SerializeField] private Transform cardContainer;   // objeto com Grid Layout Group / Horizontal-Vertical Layout Group
    [SerializeField] private EvidenceCardUI cardPrefab;

    [Header("Painel de detalhes")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TextMeshProUGUI detailsNameText;
    [SerializeField] private TextMeshProUGUI detailsDescriptionText;
    [SerializeField] private Image detailsIconImage;

    private readonly List<EvidenceCardUI> spawnedCards = new List<EvidenceCardUI>();

    private void OnEnable()
    {
        // Espera o EvidenceManager existir (ele é DontDestroyOnLoad, deve já estar de pé)
        if (EvidenceManager.Instance != null)
        {
            EvidenceManager.Instance.OnEvidenceCollected += HandleEvidenceCollected;
            RebuildBoardFromExistingEvidence();
        }
        else
        {
            Debug.LogWarning("[EvidenceBoardUI] EvidenceManager não encontrado no OnEnable.");
        }
    }

    private void OnDisable()
    {
        if (EvidenceManager.Instance != null)
            EvidenceManager.Instance.OnEvidenceCollected -= HandleEvidenceCollected;
    }

    // Caso o painel seja aberto depois que evidências já foram coletadas
    private void RebuildBoardFromExistingEvidence()
    {
        foreach (var card in spawnedCards)
            Destroy(card.gameObject);
        spawnedCards.Clear();

        foreach (var evidence in EvidenceManager.Instance.GetCollectedEvidence())
        {
            SpawnCard(evidence);
        }
    }

    private void HandleEvidenceCollected(EvidenceData evidence)
    {
        SpawnCard(evidence);
    }

    private void SpawnCard(EvidenceData evidence)
    {
        if (cardPrefab == null || cardContainer == null)
        {
            Debug.LogError("[EvidenceBoardUI] cardPrefab ou cardContainer não atribuídos no Inspector.");
            return;
        }

        EvidenceCardUI card = Instantiate(cardPrefab, cardContainer);
        card.Setup(evidence, this);
        spawnedCards.Add(card);
    }

    public void ShowDetails(EvidenceData evidence)
    {
        if (detailsPanel == null) return;

        detailsPanel.SetActive(true);

        if (detailsNameText != null)
            detailsNameText.text = evidence.evidenceName;

        if (detailsDescriptionText != null)
            detailsDescriptionText.text = evidence.description;

        if (detailsIconImage != null)
            detailsIconImage.sprite = evidence.icon;
    }

    // Chame isso num botão "Fechar painel de investigação" ou numa tecla (ex: Tab)
    public void ToggleBoard()
    {
        if (boardPanelRoot != null)
            boardPanelRoot.SetActive(!boardPanelRoot.activeSelf);
    }
}