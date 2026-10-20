using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Optional art: without textures the operating-system cursor remains visible.
public sealed class CaseZeroCursor : MonoBehaviour
{
    CaseZeroArtLibrary art;
    readonly List<RaycastResult> hits = new List<RaycastResult>();
    Texture2D previous;
    Vector2 previousHotspot;
    bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindFirstObjectByType<CaseZeroCursor>() != null) return;
        var root = new GameObject("Caso Zero • Cursor");
        DontDestroyOnLoad(root);
        root.AddComponent<CaseZeroCursor>();
    }
    void Awake() => art = Resources.Load<CaseZeroArtLibrary>("ArtesCasoZero");
    void Update()
    {
        if (art == null || !Application.isFocused) return;
        int mode = 0;
        if (EventSystem.current != null && Mouse.current != null)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
            hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count > 0)
            {
                var target = hits[0].gameObject;
                var input = target.GetComponentInParent<TMP_InputField>();
                var legacy = target.GetComponentInParent<InputField>();
                var selectable = target.GetComponentInParent<Selectable>();
                if ((input != null && input.IsInteractable() && !input.readOnly) ||
                    (legacy != null && legacy.IsInteractable() && !legacy.readOnly)) mode = 2;
                else if (selectable != null && selectable.IsInteractable()) mode = 1;
            }
        }
        var texture = mode == 2 ? art.cursorEscrita : mode == 1 ? art.cursorInteracao : art.cursorLupa;
        var hotspot = mode == 2 ? art.hotspotEscrita : mode == 1 ? art.hotspotInteracao : art.hotspotLupa;
        if (texture != null) hotspot = new Vector2(Mathf.Clamp(hotspot.x, 0, texture.width - 1), Mathf.Clamp(hotspot.y, 0, texture.height - 1));
        if (applied && texture == previous && hotspot == previousHotspot) return;
        previous = texture; previousHotspot = hotspot; applied = true;
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
    }
    void ResetCursor() { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); applied = false; }
    void OnApplicationFocus(bool focused) { if (!focused) ResetCursor(); }
    void OnDisable() => ResetCursor();
}
