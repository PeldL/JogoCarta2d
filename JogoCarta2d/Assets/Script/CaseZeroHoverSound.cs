using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Quiet tick when the pointer reaches something clickable (clues, characters).
public sealed class CaseZeroHoverSound : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        var selectable = GetComponent<Selectable>();
        if (selectable == null || selectable.IsInteractable()) CaseZeroSfx.Hover();
    }
}
