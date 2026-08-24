using Pixit.TMProVisualFX;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Pixit.TMProVisualFX.Editor
{
    public static class TMProVisualFXMenu
    {
        [MenuItem("GameObject/Pixit/Add TMPro Visual FX", false, 20)]
        private static void AddVisualFX()
        {
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {
                Debug.LogWarning("Select a TextMeshPro object first, then use GameObject > Pixit > Add TMPro Visual FX.");
                return;
            }

            TMP_Text text = selectedObject.GetComponent<TMP_Text>();

            if (text == null)
            {
                Debug.LogWarning("TMPro Visual FX needs a TextMeshPro or TextMeshProUGUI component on the selected GameObject.");
                return;
            }

            Undo.AddComponent<TMProVisualFX>(selectedObject);
        }

        [MenuItem("GameObject/Pixit/Add TMPro Visual FX", true)]
        private static bool CanAddVisualFX()
        {
            GameObject selectedObject = Selection.activeGameObject;
            return selectedObject != null
                && selectedObject.GetComponent<TMP_Text>() != null
                && selectedObject.GetComponent<TMProVisualFX>() == null;
        }
    }
}
