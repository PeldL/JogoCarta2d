using UnityEngine;

/// <summary>
/// Demo helper for the example scene: press Tab to flip every H_SDFUIOutline between the SDF and mesh-ring
/// render modes, and shows which mode is currently active.
/// </summary>
public class H_SDFUIOutlineDebugWindow : MonoBehaviour
{
    public TMPro.TextMeshProUGUI debugWindowSdfModeActiveText;

    [Header("Runtime")]
    public bool isSdfModeActive = true;
    public H_SDFUIOutline[] H_SDFUIOutlines;

    void Awake()
    {
        H_SDFUIOutlines = FindObjectsByType<H_SDFUIOutline>(FindObjectsSortMode.None);
    }

    // Push the starting mode onto the outlines so the label and what's on screen agree from the first frame. Start, not
    // Awake, so every outline has initialised first.
    void Start()
    {
        ApplyMode();
    }

    void Update()
    {
        if (TabPressed())
        {
            isSdfModeActive = !isSdfModeActive;
            ApplyMode();
        }

        debugWindowSdfModeActiveText.text = isSdfModeActive ? "SDF" : "Mesh Ring";
    }

    void ApplyMode()
    {
        var mode = isSdfModeActive
            ? H_SDFUIOutline.OutlineRenderMode.ProceduralSdf
            : H_SDFUIOutline.OutlineRenderMode.LegacyMeshRing;

        // Go through the property, not the field, so each outline rebuilds and the switch shows.
        for (int i = 0; i < H_SDFUIOutlines.Length; i++)
            H_SDFUIOutlines[i].RenderMode = mode;
    }

    // Works under either input backend; legacy Input.* throws in Input-System-only projects, so it's guarded out there.
    static bool TabPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Tab);
#elif ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.tabKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
