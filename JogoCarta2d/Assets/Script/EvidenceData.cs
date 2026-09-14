using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EvidenceData
{
    public string id;
    public string evidenceName;
    [TextArea] public string description;
    public Sprite icon;
}


