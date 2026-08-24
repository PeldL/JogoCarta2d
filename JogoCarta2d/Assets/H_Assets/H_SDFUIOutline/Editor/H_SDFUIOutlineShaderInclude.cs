#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// The SDF material is created at runtime via Shader.Find, which the build's dependency walker can't see, so the
// shader would get stripped unless something references it. This registers it in Always Included Shaders (idempotent)
// on load and before every build, so the outline always works in a player regardless of how the user added the component.
[InitializeOnLoad]
class H_SDFUIOutlineShaderInclude : IPreprocessBuildWithReport
{
    const string ShaderName = "H_Shaders/H_UIRoundedRectOutlineSDF";

    static H_SDFUIOutlineShaderInclude() => EnsureIncluded();

    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) => EnsureIncluded();

    static void EnsureIncluded()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
            return;

        var settings = GraphicsSettings.GetGraphicsSettings();
        var so = new SerializedObject(settings);
        var list = so.FindProperty("m_AlwaysIncludedShaders");

        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                return;
        }

        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);
        list.GetArrayElementAtIndex(index).objectReferenceValue = shader;
        so.ApplyModifiedProperties();
    }
}
#endif
