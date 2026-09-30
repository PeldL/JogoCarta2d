using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class CaseZeroButtonMotion : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField, Range(1f, 1.1f)] float hoverScale = 1.025f;
    [SerializeField, Range(0.9f, 1f)] float pressedScale = 0.97f;
    [SerializeField, Min(0.01f)] float duration = 0.13f;
    Button button;
    Vector3 restingScale;
    Tween motion;
    bool hovered, selected, pressed;

    void Awake() { button = GetComponent<Button>(); restingScale = transform.localScale; }
    void Animate()
    {
        motion?.Kill();
        float scale = !button.IsInteractable() ? 1 : pressed ? pressedScale : hovered || selected ? hoverScale : 1;
        motion = transform.DOScale(restingScale * scale, duration).SetEase(Ease.OutQuad).SetUpdate(true);
    }
    public void OnPointerEnter(PointerEventData e) { hovered = true; Animate(); }
    public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; Animate(); }
    public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { pressed = true; Animate(); } }
    public void OnPointerUp(PointerEventData e) { pressed = false; Animate(); }
    public void OnSelect(BaseEventData e) { selected = true; Animate(); }
    public void OnDeselect(BaseEventData e) { selected = false; pressed = false; Animate(); }
    void Update()
    {
        if (!button.IsInteractable() && (hovered || selected || pressed))
        { hovered = selected = pressed = false; Animate(); }
    }
    void OnDisable()
    {
        motion?.Kill();
        hovered = selected = pressed = false;
        transform.localScale = restingScale;
    }
}
