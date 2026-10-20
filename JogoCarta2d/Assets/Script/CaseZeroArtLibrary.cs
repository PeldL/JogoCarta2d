using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Caso Zero/Biblioteca de artes")]
public sealed class CaseZeroArtLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string name;
        public Sprite sprite;
    }
    public Entry[] personagens;
    public Entry[] cenarios;
    [Serializable]
    public class Portrait
    {
        public string name;
        public string caseId;
        public int suspectIndex;
        public string artName;
        public Sprite sprite;
    }
    [Header("Retratos por caso • arte independente da identidade provisória")]
    public Portrait[] retratos;
    [Header("Cursores opcionais • texturas importadas como Cursor")]
    public Texture2D cursorLupa, cursorInteracao, cursorEscrita;
    public Vector2 hotspotLupa, hotspotInteracao, hotspotEscrita;
    [Header("Rádio • retratos independentes dos suspeitos")]
    public Sprite radioDetetive, radioCentral;

    public Sprite FindPortrait(string caseId, int suspectIndex)
    {
        if (retratos != null)
            foreach (var entry in retratos)
                if (entry != null && entry.caseId == caseId && entry.suspectIndex == suspectIndex)
                    return entry.sprite != null ? entry.sprite : Find(entry.artName);
        return null;
    }

    public Sprite Find(string key)
    {
        foreach (var entries in new[] { personagens, cenarios })
            if (entries != null)
                foreach (var entry in entries)
                    if (entry != null && entry.name == key) return entry.sprite;
        return null;
    }
}
