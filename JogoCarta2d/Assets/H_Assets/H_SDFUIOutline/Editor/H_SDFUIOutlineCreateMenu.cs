#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Adds a GameObject > UI create entry so the outline is discoverable like the built-in UI elements,
// spawned under a Canvas (creating one, plus an EventSystem, if the scene has none).
static class H_SDFUIOutlineCreateMenu
{
    [MenuItem("GameObject/UI/H SDF UI Outline", false, 2205)]
    static void Create(MenuCommand menuCommand)
    {
        GameObject context = menuCommand.context as GameObject;
        GameObject parent = ResolveParent(context);

        var go = new GameObject("H SDF UI Outline", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create H SDF UI Outline");
        GameObjectUtility.SetParentAndAlign(go, parent);

        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(160f, 40f);
        rt.anchoredPosition = Vector2.zero;

        go.AddComponent<H_SDFUIOutline>();

        Selection.activeGameObject = go;
    }

    static GameObject ResolveParent(GameObject context)
    {
        if (context != null && context.GetComponentInParent<Canvas>() != null)
            return context;

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
            return canvas.gameObject;

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.layer = LayerMask.NameToLayer("UI");
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        return canvasGo;
    }
}
#endif
