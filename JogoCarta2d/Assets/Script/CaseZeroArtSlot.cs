using UnityEngine;
using UnityEngine.UI;

// Visual mapping only: no clues, case ordering or suspect identities are encoded here.
[RequireComponent(typeof(Image))]
public sealed class CaseZeroArtSlot : MonoBehaviour
{
    public CaseZeroArtLibrary library;
    public string artName;
    [Tooltip("Optional local override; otherwise use the library entry.")]
    public Sprite spriteOverride;
    public bool preserveAspect = true;
    [Tooltip("Decorative draft objects hidden only when a replacement sprite exists.")]
    public GameObject[] draftObjects = new GameObject[0];
    Image target;
    Sprite original, applied;
    Color originalColor;
    bool originalAspect;
    bool[] originalVisibility;
    bool initialized;

    void Awake()
    {
        target = GetComponent<Image>();
        original = target.sprite;
        originalColor = target.color;
        originalAspect = target.preserveAspect;
        originalVisibility = new bool[draftObjects.Length];
        for (int i = 0; i < draftObjects.Length; i++)
            originalVisibility[i] = draftObjects[i] != null && draftObjects[i].activeSelf;
        Apply();
    }
    void LateUpdate()
    {
        var sprite = spriteOverride != null ? spriteOverride : library != null ? library.Find(artName) : null;
        if (!initialized || sprite != applied) Apply();
    }
    void Apply()
    {
        applied = spriteOverride != null ? spriteOverride : library != null ? library.Find(artName) : null;
        initialized = true;
        target.sprite = applied != null ? applied : original;
        target.color = applied != null ? Color.white : originalColor;
        target.preserveAspect = applied != null ? preserveAspect : originalAspect;
        for (int i = 0; i < draftObjects.Length; i++)
            if (draftObjects[i] != null) draftObjects[i].SetActive(applied == null && originalVisibility[i]);
    }
}
