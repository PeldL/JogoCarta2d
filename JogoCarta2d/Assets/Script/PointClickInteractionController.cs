using UnityEngine;
using UnityEngine.InputSystem;

public class PointClickInteractionController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask interactableLayer = ~0;

    private IClickable2D currentHover;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);

        Collider2D hit = Physics2D.OverlapPoint(worldPos, interactableLayer);
        IClickable2D hitInteractable = hit != null ? hit.GetComponent<IClickable2D>() : null;

        if (hitInteractable != currentHover)
        {
            currentHover?.OnHoverExit();
            currentHover = hitInteractable;
            currentHover?.OnHoverEnter();
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && currentHover != null)
        {
            currentHover.OnClicked();
            currentHover = null;
        }
    }
}