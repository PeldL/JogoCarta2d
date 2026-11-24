using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 3D clue viewer ("Resident Evil" style): a found clue opens on a dark screen with only the
// object, which turns while the mouse button is held and dragged; the wheel zooms.
// Opens by itself right after a clue is found, again when a found clue is clicked in the scene,
// and from the "View in 3D" button on the Evidence page.
public sealed partial class CaseZeroGame
{
    CaseZeroEvidenceViewer evidenceViewer;
    Button view3DButton;

    bool EvidenceViewerOpen => evidenceViewer != null && evidenceViewer.IsOpen;

    void BuildEvidenceViewer()
    {
        if (evidenceViewer != null) return;
        evidenceViewer = new GameObject("Caso Zero • Pista em 3D").AddComponent<CaseZeroEvidenceViewer>();
        evidenceViewer.Initialize(RoomFont);
        if (view.evidenceDetails == null) return;
        // "View in 3D" button at the bottom-right of the clue details on the Evidence page.
        var buttonObject = new GameObject("Ver em 3D", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(view.evidenceDetails.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-12, 52);
        rect.sizeDelta = new Vector2(220, 46);
        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.2f, 0.18f, 0.17f, 0.95f);
        view3DButton = buttonObject.GetComponent<Button>();
        view3DButton.targetGraphic = image;
        view3DButton.onClick.AddListener(() => CaseZeroSoundFeedback.Current?.PlayClick());
        view3DButton.onClick.AddListener(() => ShowEvidence3D(selectedEvidence));
        buttonObject.AddComponent<CaseZeroButtonMotion>();
        var label = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();
        label.rectTransform.SetParent(rect, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        if (RoomFont != null) label.font = RoomFont;
        label.fontSize = 22;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.color = new Color(0.96f, 0.92f, 0.82f);
        label.raycastTarget = false;
        label.text = "View in 3D";
    }

    void ShowEvidence3D(string id, bool justFound = false)
    {
        if (evidenceViewer == null || restoring || string.IsNullOrEmpty(id) || !catalog.ContainsKey(id)) return;
        if (!CaseZeroEvidenceViewer.HasModel(id)) return;
        evidenceViewer.Show(id, catalog[id].evidenceName, catalog[id].description, justFound);
    }

    // Look at the item just found, once the "new evidence" notice has registered.
    IEnumerator ShowEvidence3DSoon(string id)
    {
        yield return new WaitForSecondsRealtime(0.35f);
        if (state == null || travelling || roomBusy || RoomOpen) yield break;
        ShowEvidence3D(id, true);
    }

    void Refresh3DButton()
    {
        if (view3DButton != null) view3DButton.gameObject.SetActive(CaseZeroEvidenceViewer.HasModel(selectedEvidence));
    }

    void DestroyEvidenceViewer()
    {
        if (evidenceViewer != null) Destroy(evidenceViewer.gameObject);
        evidenceViewer = null;
    }
}
