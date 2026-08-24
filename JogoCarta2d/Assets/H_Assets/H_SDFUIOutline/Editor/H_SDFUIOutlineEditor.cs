#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

// Branded inspector for H_SDFUIOutline. Inherits GraphicEditor to keep the standard Color / Material /
// Raycast / Maskable controls, and shows only the outline fields that apply to the chosen render mode.
[CustomEditor(typeof(H_SDFUIOutline))]
[CanEditMultipleObjects]
public class H_SDFUIOutlineEditor : GraphicEditor
{
    SerializedProperty _texture;
    SerializedProperty _renderMode;
    SerializedProperty _edgeSoftness;
    SerializedProperty _outlineWidth;
    SerializedProperty _cornerRadius;
    SerializedProperty _cornerSegments;
    SerializedProperty _mappingBias;
    SerializedProperty _fillCenter;

    protected override void OnEnable()
    {
        base.OnEnable();
        _texture = serializedObject.FindProperty("m_Texture");
        _renderMode = serializedObject.FindProperty("_renderMode");
        _edgeSoftness = serializedObject.FindProperty("_edgeSoftness");
        _outlineWidth = serializedObject.FindProperty("_outlineWidth");
        _cornerRadius = serializedObject.FindProperty("_cornerRadius");
        _cornerSegments = serializedObject.FindProperty("_cornerSegments");
        _mappingBias = serializedObject.FindProperty("_mappingBias");
        _fillCenter = serializedObject.FindProperty("_fillCenter");
    }

    public override void OnInspectorGUI()
    {
        H_SDFUIOutlineBranding.DrawHeader();

        serializedObject.Update();

        EditorGUILayout.LabelField("Outline", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_renderMode, EditorGUIUtility.TrTextContent("Render Mode",
            "SDF stays crisp and never flickers/disappears at small sizes; Mesh Ring is a classic vertex outline."));

        bool sdf = _renderMode.enumValueIndex == (int)H_SDFUIOutline.OutlineRenderMode.ProceduralSdf;

        EditorGUILayout.PropertyField(_outlineWidth, EditorGUIUtility.TrTextContent("Outline Width",
            "Thickness of the outline, in UI units."));
        EditorGUILayout.PropertyField(_cornerRadius, EditorGUIUtility.TrTextContent("Corner Radius",
            "Rounds the corners of the outline, in UI units."));

        if (sdf)
        {
            EditorGUILayout.PropertyField(_edgeSoftness, EditorGUIUtility.TrTextContent("Edge Softness",
                "How soft the anti-aliased edge is. Lower is sharper."));
        }
        else
        {
            EditorGUILayout.PropertyField(_cornerSegments, EditorGUIUtility.TrTextContent("Corner Segments",
                "Segments per rounded corner. Higher is smoother, at the cost of more vertices."));
            EditorGUILayout.PropertyField(_mappingBias, EditorGUIUtility.TrTextContent("Mapping Bias",
                "Biases the outline's UV mapping between the inner and outer edge."));
        }

        EditorGUILayout.PropertyField(_fillCenter, EditorGUIUtility.TrTextContent("Fill Center",
            "Also fill the area inside the outline instead of drawing only the ring."));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_Color, EditorGUIUtility.TrTextContent("Color",
            "Outline colour (and fill colour when Fill Center is on)."));
        if (sdf)
        {
            EditorGUILayout.HelpBox("In SDF mode the material is generated from the SDF shader; the Material field is ignored.", MessageType.None);
        }
        else
        {
            EditorGUILayout.PropertyField(m_Material);
        }
        EditorGUILayout.PropertyField(_texture, EditorGUIUtility.TrTextContent("Texture",
            "Optional texture multiplied into the outline."));
        RaycastControlsGUI();
        MaskableControlsGUI();

        serializedObject.ApplyModifiedProperties();

        H_SDFUIOutlineBranding.DrawFooter();
    }
}
#endif
