using UnityEngine;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class EvidenceInteractable : MonoBehaviour, IClickable2D
{
    [Header("Dados da evidência")]
    public EvidenceData evidence;

    [Header("Feedback visual (opcional)")]
    public GameObject highlightEffect;

    [Header("Comportamento")]
    public bool destroyAfterCollect = true;

    public void OnHoverEnter()
    {
        if (highlightEffect != null)
            highlightEffect.SetActive(true);
    }

    public void OnHoverExit()
    {
        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }

    public void OnClicked()
    {
        if (evidence == null)
        {
            Debug.LogWarning($"[EvidenceInteractable] '{gameObject.name}' não tem EvidenceData atribuído.");
            return;
        }

        if (EvidenceManager.Instance == null)
        {
            Debug.LogError("[EvidenceInteractable] Nenhum EvidenceManager encontrado na cena.");
            return;
        }

        EvidenceManager.Instance.CollectEvidence(evidence);

        if (destroyAfterCollect)
            gameObject.SetActive(false);
    }
}