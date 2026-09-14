using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
public class PointClickInteractionController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask interactableLayer = ~0; // por padrão, todas as layers

    private EvidenceInteractable currentHover;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null) return; // sem mouse conectado (ex: build mobile)

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);

        Collider2D hit = Physics2D.OverlapPoint(worldPos, interactableLayer);
        EvidenceInteractable hitInteractable = hit != null ? hit.GetComponent<EvidenceInteractable>() : null;

        // Gerencia troca de hover
        if (hitInteractable != currentHover)
        {
            if (currentHover != null)
                currentHover.OnHoverExit();

            currentHover = hitInteractable;

            if (currentHover != null)
                currentHover.OnHoverEnter();
        }

        // Clique
        if (Mouse.current.leftButton.wasPressedThisFrame && currentHover != null)
        {
            currentHover.OnClicked();
            currentHover = null; // evita chamar hover exit num objeto já desativado
        }
    }
}