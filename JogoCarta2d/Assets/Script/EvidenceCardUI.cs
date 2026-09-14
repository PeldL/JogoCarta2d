using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EvidenceCardUI : MonoBehaviour
{
    [Header("Referências (arraste no prefab)")]
    public Image iconImage;
    public TextMeshProUGUI nameText;

    private EvidenceData boundEvidence;
    private EvidenceBoardUI board;

    public void Setup(EvidenceData evidence, EvidenceBoardUI ownerBoard)
    {
        boundEvidence = evidence;
        board = ownerBoard;

        if (nameText != null)
            nameText.text = evidence.evidenceName;

        if (iconImage != null && evidence.icon != null)
            iconImage.sprite = evidence.icon;
    }

    // Chame isso a partir de um Button OnClick no prefab (ou de um EventTrigger)
    public void OnCardClicked()
    {
        board.ShowDetails(boundEvidence);
    }
}
