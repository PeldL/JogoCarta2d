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
    Tween motion, liftMotion;
    RectTransform rect;
    Vector2 restingPosition;
    bool folderTab, pageSelected;
    const float TabLift = 3f;
    Camera pointerCamera;

    public bool IsFolderTab => folderTab;
    public void SetFolderTab(bool activePage)
    {
        folderTab = true;
        pageSelected = activePage;
        if (button != null && isActiveAndEnabled) Animate();
    }
    bool hovered, selected, pressed;

    void Awake()
    {
        button = GetComponent<Button>();
        restingScale = transform.localScale;
        rect = transform as RectTransform;
        if (rect != null) restingPosition = rect.anchoredPosition;
    }
    void Animate()
    {
        motion?.Kill();
        float scale = !button.IsInteractable() ? 1 : pressed ? pressedScale : hovered || selected ? hoverScale : 1;
        if (folderTab && rect != null)
        {
            liftMotion?.Kill();
            bool raised = button.IsInteractable() && (hovered || selected || pageSelected);
            liftMotion = rect.DOAnchorPos(restingPosition + Vector2.up * (raised ? TabLift : 0), 0.22f)
                .SetEase(Ease.OutCubic).SetUpdate(true);
        }
        motion = transform.DOScale(restingScale * scale, duration).SetEase(pressed ? Ease.OutQuad : Ease.OutBack).SetUpdate(true);
    }
    void OnEnable()
    {
        if (button != null) { transform.localScale = restingScale; if (folderTab) Animate(); }
    }
    public void OnPointerEnter(PointerEventData e) { pointerCamera = e.enterEventCamera; hovered = true; Animate(); }
    public void OnPointerExit(PointerEventData e)
    {
        // Keep the original hit area while the post-it moves away from the pointer.
        hovered = folderTab && ContainsRestingPoint(e.position);
        pressed = false; Animate();
    }
    bool ContainsRestingPoint(Vector2 point)
    {
        if (rect == null || rect.parent == null) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, point, pointerCamera, out Vector2 local)) return false;
        Vector3 displacement = rect.parent.TransformVector((Vector3)(rect.anchoredPosition - restingPosition));
        Vector3 offset = rect.InverseTransformVector(displacement);
        return rect.rect.Contains(local + new Vector2(offset.x, offset.y));
    }
    public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { pressed = true; Animate(); } }
    public void OnPointerUp(PointerEventData e) { pressed = false; Animate(); }
    public void OnSelect(BaseEventData e) { selected = true; Animate(); }
    public void OnDeselect(BaseEventData e) { selected = false; pressed = false; Animate(); }
    void Update()
    {
        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (folderTab && hovered && pointer != null && !ContainsRestingPoint(pointer.position.ReadValue()) &&
            !RectTransformUtility.RectangleContainsScreenPoint(rect, pointer.position.ReadValue(), pointerCamera))
        { hovered = false; pressed = false; Animate(); }
        if (!button.IsInteractable() && (hovered || selected || pressed))
        { hovered = selected = pressed = false; Animate(); }
    }
    void OnDisable()
    {
        motion?.Kill();
        liftMotion?.Kill();
        if (folderTab && rect != null) rect.anchoredPosition = restingPosition;
        hovered = selected = pressed = false;
        transform.localScale = restingScale;
    }
}
